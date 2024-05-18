using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using IBS.Utility.Extensions;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class SalesHeaderRepository : Repository<SalesHeader>, ISalesHeaderRepository
    {
        private ApplicationDbContext _db;

        public SalesHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task ComputeSalesPerCashier(bool hasPoSales, CancellationToken cancellationToken = default)
        {
            try
            {
                var fuelSales = await _db.FuelSalesViews
                    .ToListAsync(cancellationToken);

                var lubeSales = await _db.Lubes
                    .Where(f => !f.IsProcessed)
                    .ToListAsync(cancellationToken);

                var safeDropDeposits = await _db.SafeDrops
                    .Where(f => !f.IsProcessed)
                    .ToListAsync(cancellationToken);

                var fuelPoSales = Enumerable.Empty<Fuel>();
                var lubePoSales = Enumerable.Empty<Lube>();

                if (hasPoSales)
                {
                    fuelPoSales = await _db.Fuels
                        .Where(f => !f.IsProcessed && (!String.IsNullOrEmpty(f.cust) || !String.IsNullOrEmpty(f.pono) || !String.IsNullOrEmpty(f.plateno)))
                        .ToListAsync(cancellationToken);

                    lubePoSales = await _db.Lubes
                        .Where(f => !f.IsProcessed && (!String.IsNullOrEmpty(f.cust) || !String.IsNullOrEmpty(f.pono) || !String.IsNullOrEmpty(f.plateno)))
                        .ToListAsync(cancellationToken);
                }

                var salesHeaders = fuelSales
                    .Select(fuel => new SalesHeader
                    {
                        Date = fuel.BusinessDate,
                        StationCode = fuel.StationCode,
                        Cashier = fuel.xONAME,
                        Shift = fuel.Shift,
                        CreatedBy = "System Generated",
                        FuelSalesTotalAmount = Math.Round((decimal)fuel.Liters * fuel.Price, 2),
                        LubesTotalAmount = Math.Round(lubeSales.Where(l => (l.Cashier == fuel.xONAME) && (l.Shift == fuel.Shift) && (l.BusinessDate == fuel.BusinessDate)).Sum(l => l.Amount), 2),
                        SafeDropTotalAmount = Math.Round(safeDropDeposits.Where(s => (s.xONAME == fuel.xONAME) && (s.Shift == fuel.Shift) && (s.BusinessDate == fuel.BusinessDate)).Sum(s => s.Amount), 2),
                        POSalesTotalAmount = hasPoSales ? Math.Round(fuelPoSales.Where(s => (s.xONAME == fuel.xONAME) && (s.Shift == fuel.Shift) && (s.BusinessDate == fuel.BusinessDate)).Sum(s => s.Amount) + lubePoSales.Where(l => (l.Cashier == fuel.xONAME) && (l.Shift == fuel.Shift)).Sum(l => l.Amount), 2) : 0,
                        POSalesAmount = hasPoSales ? fuelPoSales
                            .Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.BusinessDate == fuel.BusinessDate)
                            .Select(s => s.Amount)
                            .Concat(lubePoSales
                                .Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift && l.BusinessDate == fuel.BusinessDate)
                                .Select(l => l.Amount))
                            .ToArray() : new decimal[0],
                        Customers = hasPoSales ? fuelPoSales
                            .Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.BusinessDate == fuel.BusinessDate)
                            .Select(s => s.cust)
                            .Concat(lubePoSales
                                .Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift && l.BusinessDate == fuel.BusinessDate)
                                .Select(l => l.cust))
                            .ToArray() : new string[0],
                        TimeIn = fuel.TimeIn,
                        TimeOut = fuel.TimeOut
                    })
                    .GroupBy(s => new { s.Date, s.StationCode, s.Cashier, s.Shift, s.LubesTotalAmount, s.SafeDropTotalAmount, s.POSalesTotalAmount, s.CreatedBy })
                    .Select(g => new SalesHeader
                    {
                        Date = g.Key.Date,
                        Cashier = g.Key.Cashier,
                        Shift = g.Key.Shift,
                        FuelSalesTotalAmount = g.Sum(g => g.FuelSalesTotalAmount),
                        LubesTotalAmount = g.Key.LubesTotalAmount,
                        SafeDropTotalAmount = g.Key.SafeDropTotalAmount,
                        POSalesTotalAmount = g.Key.POSalesTotalAmount,
                        POSalesAmount = g.Select(s => s.POSalesAmount).First(),
                        Customers = g.Select(s => s.Customers).First(),
                        TotalSales = (g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount) - g.Key.POSalesTotalAmount,
                        GainOrLoss = g.Key.SafeDropTotalAmount - ((g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount) - g.Key.POSalesTotalAmount),
                        CreatedBy = g.Key.CreatedBy,
                        TimeIn = g.Min(s => s.TimeIn),
                        TimeOut = g.Max(s => s.TimeOut),
                        StationCode = g.Key.StationCode,
                        Source = "POS"
                    })
                    .ToList();

                foreach (var dsr in salesHeaders)
                {
                    dsr.SalesNo = await GenerateSeriesNumber(dsr.StationCode);
                    await _db.SalesHeaders.AddAsync(dsr, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                double previousClosing = 0;
                string previousNo = string.Empty;
                DateOnly previousStartDate = new();
                foreach (var group in fuelSales.GroupBy(f => new { f.ItemCode, f.xPUMP }))
                {

                    foreach (var fuel in group)
                    {
                        SalesHeader? salesHeader = salesHeaders.Find(s => s.Cashier == fuel.xONAME && s.Shift == fuel.Shift && s.Date == fuel.BusinessDate) ?? throw new InvalidOperationException($"Sales Header with {fuel.xONAME} shift#{fuel.Shift} on {fuel.BusinessDate} not found!");

                        var salesDetail = new SalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
                            StationCode = salesHeader.StationCode,
                            Product = fuel.ItemCode,
                            Particular = $"{fuel.Particulars} (P{fuel.xPUMP})",
                            Closing = fuel.Closing,
                            Opening = fuel.Opening,
                            Liters = fuel.Liters,
                            Calibration = fuel.Calibration,
                            LitersSold = fuel.LitersSold,
                            TransactionCount = fuel.TransactionCount,
                            Price = fuel.Price,
                            Sale = fuel.Sale,
                            Value = Math.Round((decimal)fuel.Liters * fuel.Price, 2)
                        };

                        if (previousClosing != 0 && !string.IsNullOrEmpty(previousNo) && previousClosing != fuel.Opening)
                        {
                            salesHeader.IsTransactionNormal = false;
                            salesDetail.IsTransactionNormal = false;
                            salesDetail.ReferenceNo = previousNo;

                            Offline offline = new(fuel.StationCode, previousStartDate, fuel.BusinessDate, fuel.Particulars, fuel.xPUMP, fuel.Opening, previousClosing)
                            {
                                SeriesNo = await GenerateOfflineNo(),
                                ClosingDSRNo = previousNo,
                                OpeningDSRNo = salesDetail.SalesNo
                            };

                            await _db.AddAsync(offline, cancellationToken);
                            await _db.SaveChangesAsync(cancellationToken);

                        }

                        await _db.SalesDetails.AddAsync(salesDetail, cancellationToken);

                        previousClosing = fuel.Closing;
                        previousNo = salesHeader.SalesNo;
                        previousStartDate = fuel.BusinessDate;

                    }

                    previousClosing = 0;
                    previousNo = string.Empty;

                }

                if (lubeSales.Count != 0)
                {
                    foreach (var lube in lubeSales)
                    {
                        var salesHeader = salesHeaders.Find(l => l.Cashier == lube.Cashier && l.Shift == lube.Shift && l.Date == lube.BusinessDate) ?? throw new InvalidOperationException($"Sales Header with {lube.Cashier} shift#{lube.Shift} on {lube.BusinessDate} not found!");

                        var salesDetail = new SalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
                            Product = lube.ItemCode,
                            Particular = $"{lube.Particulars}",
                            Liters = (double)lube.LubesQty,
                            Price = lube.Price,
                            Sale = lube.Amount,
                            Value = Math.Round(lube.Amount, 2)
                        };
                        lube.IsProcessed = true;
                        await _db.SalesDetails.AddAsync(salesDetail, cancellationToken);
                    }
                }

                if (fuelSales.Count != 0)
                {
                    // Bulk update directly in the database without fetching
                    await _db.Fuels
                        .Where(f => !f.IsProcessed)
                        .ExecuteUpdateAsync(
                            f => f.SetProperty(p => p.IsProcessed, true),
                            cancellationToken
                        );
                }

                if (safeDropDeposits.Count != 0)
                {
                    foreach (var safedrop in safeDropDeposits)
                    {
                        safedrop.IsProcessed = true;
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"An error occurred: {ex.Message}");

                LogMessage logMessage = new("Error", "ComputeSalesPerCashier", $"Error : '{ex.Message}'");

                await _db.LogMessages.AddAsync(logMessage, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public IEnumerable<dynamic> GetSalesHeaderJoin(IEnumerable<SalesHeader> salesHeaders, CancellationToken cancellationToken = default)
        {
            return from header in salesHeaders
                   join station in _db.Stations
                   on header.StationCode equals station.StationCode
                   select new
                   {
                       header.SalesNo,
                       header.Date,
                       header.Cashier,
                       header.StationCode,
                       header.Shift,
                       header.TimeIn,
                       header.TimeOut,
                       header.PostedBy,
                       header.SafeDropTotalAmount,
                       header.ActualCashOnHand,
                       header.IsTransactionNormal,
                       station.StationName
                   }.ToExpando();
        }

        public async Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {

                SalesVM salesVM = new()
                {
                    Header = await _db.SalesHeaders.FirstOrDefaultAsync(sh => sh.SalesNo == id && sh.StationCode == stationCode, cancellationToken),
                    Details = await _db.SalesDetails.Where(sd => sd.SalesNo == id && sd.StationCode == stationCode).ToListAsync(cancellationToken),
                };

                if (salesVM.Header == null || salesVM.Details == null)
                {
                    throw new InvalidOperationException($"Sales with id '{id}' not found.");
                }

                if (salesVM.Header.SafeDropTotalAmount == 0 && salesVM.Header.ActualCashOnHand == 0)
                {
                    throw new InvalidOperationException("Indicate the cashier's cash on hand before posting.");
                }

                var salesList = await _db.SalesHeaders
                    .Where(s => s.StationCode == salesVM.Header.StationCode && s.Date <= salesVM.Header.Date && s.CreatedDate < salesVM.Header.CreatedDate && s.PostedBy == null)
                    .OrderBy(s => s.SalesNo)
                    .ToListAsync(cancellationToken);

                if (salesList.Count > 0)
                {
                    throw new InvalidOperationException($"Can't proceed to post, you have unposted {salesList.First().SalesNo}");
                }

                StationDto station = await MapStationToDTO(salesVM.Header.StationCode, cancellationToken) ?? throw new InvalidOperationException($"Station with code {salesVM.Header.StationCode} not found.");

                salesVM.Header.PostedBy = postedBy;
                salesVM.Header.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();
                var inventories = new List<Inventory>();
                var cogsJournals = new List<GeneralLedger>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = salesVM.Header.Date,
                    Reference = salesVM.Header.SalesNo,
                    Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                    AccountNumber = "1010102",
                    AccountTitle = "Cash on Hand",
                    Debit = salesVM.Header.ActualCashOnHand > 0 ? salesVM.Header.ActualCashOnHand : salesVM.Header.SafeDropTotalAmount,
                    Credit = 0,
                    StationCode = station.StationCode,
                    JournalReference = nameof(JournalType.Sales),
                    IsValidated = true
                });

                if (salesVM.Header.POSalesTotalAmount > 0)
                {
                    for (int i = 0; i < salesVM.Header.Customers.Length; i++)
                    {
                        journals.Add(new GeneralLedger
                        {
                            TransactionDate = salesVM.Header.Date,
                            Reference = salesVM.Header.SalesNo,
                            Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                            AccountNumber = "1010201",
                            AccountTitle = "AR-Trade Receivable",
                            Debit = salesVM.Header.POSalesAmount[i],
                            Credit = 0,
                            StationCode = station.StationCode,
                            CustomerCode = salesVM.Header.Customers[i],
                            JournalReference = nameof(JournalType.Sales),
                            IsValidated = true
                        });
                    }
                }

                foreach (var product in salesVM.Details.GroupBy(d => d.Product))
                {
                    ProductDto productDetails = await MapProductToDTO(product.Key, cancellationToken) ?? throw new InvalidOperationException($"Product with code '{product.Key}' not found.");

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = product.Key.StartsWith("PET") ? "4010101" : "4011001",
                        AccountTitle = product.Key.StartsWith("PET") ? "Sales - Fuel" : "Sales - Lubes",
                        Debit = 0,
                        Credit = Math.Round(product.Sum(p => p.Value) / 1.12m, 2),
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = "2010301",
                        AccountTitle = "Vat Output",
                        Debit = 0,
                        Credit = Math.Round((product.Sum(p => p.Value) / 1.12m) * 0.12m, 2),
                        StationCode = station.StationCode,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    var sortedInventory = _db
                        .Inventories
                        .OrderBy(i => i.Date)
                        .Where(i => i.ProductCode == product.Key && i.StationCode == station.StationCode)
                        .ToList();

                    var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= salesVM.Header.Date);
                    if (lastIndex >= 0)
                    {
                        sortedInventory = sortedInventory.Skip(lastIndex).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"Beginning inventory for {product.Key} in station {station.StationCode} not found!");
                    }

                    var previousInventory = sortedInventory.FirstOrDefault();


                    decimal quantity = (decimal)product.Sum(p => p.Liters);

                    if (quantity > previousInventory.InventoryBalance)
                    {
                        throw new InvalidOperationException("The quantity exceeds the available inventory.");
                    }

                    decimal totalCost = quantity * previousInventory.UnitCostAverage;
                    decimal runningCost = previousInventory.RunningCost - totalCost;
                    decimal inventoryBalance = previousInventory.InventoryBalance - quantity;
                    decimal unitCostAverage = runningCost / inventoryBalance;
                    decimal cogs = unitCostAverage * quantity;

                    inventories.Add(new Inventory
                    {
                        Particulars = nameof(JournalType.Sales),
                        Date = salesVM.Header.Date,
                        Reference = $"POS Sales Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        ProductCode = product.Key,
                        StationCode = station.StationCode,
                        Quantity = quantity,
                        UnitCost = previousInventory.UnitCostAverage,
                        TotalCost = totalCost,
                        InventoryBalance = inventoryBalance,
                        RunningCost = runningCost,
                        UnitCostAverage = unitCostAverage,
                        InventoryValue = runningCost,
                        CostOfGoodsSold = cogs,
                        ValidatedBy = salesVM.Header.PostedBy,
                        ValidatedDate = salesVM.Header.PostedDate,
                        TransactionNo = salesVM.Header.SalesNo
                    });

                    cogsJournals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"COGS:{productDetails.ProductCode} {productDetails.ProductName} Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = product.Key.StartsWith("PET") ? "5010101" : "5011001",
                        AccountTitle = product.Key.StartsWith("PET") ? "COGS - Fuel" : "COGS - Lubes",
                        Debit = Math.Round(cogs, 2),
                        Credit = 0,
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    cogsJournals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"COGS:{productDetails.ProductCode} {productDetails.ProductName} Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = product.Key.StartsWith("PET") ? "1010401" : "1010410",
                        AccountTitle = product.Key.StartsWith("PET") ? "Inventory - Fuel" : "Inventory - Lubes",
                        Debit = 0,
                        Credit = Math.Round(cogs, 2),
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    foreach (var transaction in sortedInventory.Skip(1))
                    {

                        if (transaction.Particulars == nameof(JournalType.Sales))
                        {
                            transaction.UnitCost = unitCostAverage;
                            transaction.TotalCost = transaction.Quantity * unitCostAverage;
                            transaction.RunningCost = runningCost - transaction.TotalCost;
                            transaction.InventoryBalance = inventoryBalance - transaction.Quantity;
                            transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                            transaction.CostOfGoodsSold = transaction.UnitCostAverage * transaction.Quantity;
                            transaction.InventoryValue = transaction.RunningCost;

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }
                        else if (transaction.Particulars == nameof(JournalType.Purchase))
                        {
                            transaction.RunningCost = runningCost + transaction.TotalCost;
                            transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                            transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                            transaction.InventoryValue = transaction.RunningCost;

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }

                        var journalEntries = _db.GeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber == (product.Key.StartsWith("PET") ? "5010101" : "5011001") || j.AccountNumber == (product.Key.StartsWith("PET") ? "1010401" : "1010410")))
                            .ToList();

                        if (journalEntries.Count != 0)
                        {
                            foreach (var journal in journalEntries)
                            {
                                if (journal.Debit != 0)
                                {
                                    if (journal.Debit != transaction.CostOfGoodsSold)
                                    {
                                        journal.Debit = transaction.CostOfGoodsSold;
                                        journal.Credit = 0;
                                    }
                                }
                                else
                                {
                                    if (journal.Credit != transaction.CostOfGoodsSold)
                                    {
                                        journal.Credit = transaction.CostOfGoodsSold;
                                        journal.Debit = 0;
                                    }
                                }
                            }
                        }

                        _db.GeneralLedgers.UpdateRange(journalEntries);
                    }

                    _db.Inventories.UpdateRange(sortedInventory);
                }

                if (salesVM.Header.GainOrLoss != 0)
                {
                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = salesVM.Header.GainOrLoss < 0 ? "6100102" : "6010102",
                        AccountTitle = salesVM.Header.GainOrLoss < 0 ? "Cash Short - Handling" : "Cash Over - Handling",
                        Debit = salesVM.Header.GainOrLoss < 0 ? Math.Abs(salesVM.Header.GainOrLoss) : 0,
                        Credit = salesVM.Header.GainOrLoss > 0 ? salesVM.Header.GainOrLoss : 0,
                        StationCode = station.StationCode,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });
                }

                journals.AddRange(cogsJournals);

                if (IsJournalEntriesBalanced(journals))
                {
                    await _db.Inventories.AddRangeAsync(inventories, cancellationToken);
                    await _db.GeneralLedgers.AddRangeAsync(journals, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

            }
            catch (Exception ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }

        }

        public async Task UpdateAsync(SalesVM model, double[] closing, double[] opening, CancellationToken cancellationToken = default)
        {
            SalesHeader existingSalesHeader = await _db.SalesHeaders
                .FirstOrDefaultAsync(sh => sh.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken) ?? throw new InvalidOperationException($"Sales header with id '{model.Header.SalesHeaderId}' not found.");

            IEnumerable<SalesDetail> existingSalesDetail = await _db.SalesDetails
                .Where(sd => sd.SalesHeaderId == model.Header.SalesHeaderId)
                .OrderBy(sd => sd.SalesDetailId)
                .ToListAsync(cancellationToken);

            bool headerModified = false;

            for (int i = 0; i < existingSalesDetail.Count(); i++)
            {
                SalesDetail existingDetail = existingSalesDetail.ElementAt(i);

                if (existingDetail.Closing != closing[i] || existingDetail.Opening != opening[i])
                {
                    headerModified = true;
                    existingSalesHeader.IsModified = true;
                    existingDetail.Closing = closing[i];
                    existingDetail.Opening = opening[i];
                    existingDetail.Liters = existingDetail.Closing - existingDetail.Opening;
                    existingDetail.Value = (decimal)existingDetail.Liters * existingDetail.Price;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            if (headerModified)
            {
                existingSalesHeader!.FuelSalesTotalAmount = existingSalesDetail.Sum(d => d.Value);
                existingSalesHeader!.TotalSales = existingSalesHeader.FuelSalesTotalAmount + existingSalesHeader.LubesTotalAmount;
                existingSalesHeader!.GainOrLoss = existingSalesHeader.SafeDropTotalAmount - existingSalesHeader.TotalSales;
            }

            existingSalesHeader!.Particular = model.Header.Particular;

            if (existingSalesHeader.ActualCashOnHand != model.Header.ActualCashOnHand)
            {
                existingSalesHeader!.ActualCashOnHand = model.Header.ActualCashOnHand;
                existingSalesHeader!.GainOrLoss = model.Header.ActualCashOnHand - existingSalesHeader.TotalSales;
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingSalesHeader.EditedBy = model.Header.EditedBy;
                existingSalesHeader.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No data changes!");
            }
        }

        private async Task<string> GenerateSeriesNumber(string stationCode)
        {
            var lastCashierReport = await _db.SalesHeaders
                .OrderBy(s => s.SalesNo)
                .Where(s => s.StationCode == stationCode)
                .LastOrDefaultAsync();

            if (lastCashierReport != null)
            {
                string lastSeries = lastCashierReport.SalesNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "DSR0000000001";
            }
        }

        private async Task<int> GenerateOfflineNo()
        {
            var lastRecord = await _db.Offlines
                .OrderByDescending(o => o.OfflineId)
                .FirstOrDefaultAsync();

            if (lastRecord != null)
            {
                return lastRecord.SeriesNo + 1;
            }

            return 1;
        }
    }
}
