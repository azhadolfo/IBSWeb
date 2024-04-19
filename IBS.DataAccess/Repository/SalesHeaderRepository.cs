using IBS.DataAccess.Data;
using IBS.DataAccess.Migrations;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class SalesHeaderRepository : Repository<SalesHeader>, ISalesHeaderRepository
    {
        private ApplicationDbContext _db;

        public SalesHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task ComputeSalesPerCashier(bool HasPoSales, string createdBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {

                var fuels = await _db.Fuels
                    .Where(f => !f.IsProcessed)
                    .ToListAsync(cancellationToken);

                var fuelSales = fuels
                    .GroupBy(f => new { f.xSITECODE, f.xONAME, f.INV_DATE, f.xPUMP, f.Particulars, f.ItemCode, f.Price, f.Shift, f.Calibration, f.IsProcessed })
                    .Select(g => new
                    {
                        g.Key.xSITECODE,
                        g.Key.xONAME,
                        g.Key.INV_DATE,
                        g.Key.xPUMP,
                        g.Key.Particulars,
                        g.Key.ItemCode,
                        g.Key.Price,
                        g.Key.Shift,
                        g.Key.Calibration,
                        AmountDB = g.Sum(f => f.AmountDB),
                        Sale = g.Sum(f => f.Amount),
                        LitersSold = g.Sum(f => f.Volume),
                        Liters = g.Sum(f => f.Liters),
                        TransactionCount = g.Sum(f => f.TransCount),
                        Closing = g.Max(f => f.Closing),
                        Opening = g.Min(f => f.Opening),
                        TimeIn = g.Min(f => f.InTime),
                        TimeOut = g.Max(f => f.OutTime),
                        g.Key.IsProcessed
                    })
                    .OrderBy(g => g.INV_DATE)
                    .ThenBy(g => g.Shift)
                    .ThenBy(g => g.xSITECODE)
                    .ThenBy(g => g.Particulars)
                    .ThenBy(g => g.xPUMP);

                var lubeSales = await _db.Lubes
                    .Where(f => !f.IsProcessed)
                    .ToListAsync(cancellationToken);

                var safeDropDeposits = await _db.SafeDrops
                    .Where(f => !f.IsProcessed)
                    .ToListAsync(cancellationToken);

                var fuelPoSales = Enumerable.Empty<Fuel>();
                var lubePoSales = Enumerable.Empty<Lube>();

                if (HasPoSales)
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
                        Date = fuel.INV_DATE,
                        StationPosCode = fuel.xSITECODE.ToString(),
                        Cashier = fuel.xONAME,
                        Shift = fuel.Shift,
                        CreatedBy = createdBy,
                        FuelSalesTotalAmount = Math.Round((decimal)fuel.Liters * fuel.Price, 2),
                        LubesTotalAmount = Math.Round(lubeSales.Where(l => (l.Cashier == fuel.xONAME) && (l.Shift == fuel.Shift) && (l.INV_DATE == fuel.INV_DATE)).Sum(l => l.Amount), 2),
                        SafeDropTotalAmount = Math.Round(safeDropDeposits.Where(s => (s.xONAME == fuel.xONAME) && (s.Shift == fuel.Shift) && (s.INV_DATE == fuel.INV_DATE)).Sum(s => s.Amount), 2),
                        POSalesTotalAmount = HasPoSales ? Math.Round(fuelPoSales.Where(s => (s.xONAME == fuel.xONAME) && (s.Shift == fuel.Shift) && (s.INV_DATE == fuel.INV_DATE)).Sum(s => s.Amount) + lubePoSales.Where(l => (l.Cashier == fuel.xONAME) && (l.Shift == fuel.Shift)).Sum(l => l.Amount), 2) : 0,
                        POSalesAmount = HasPoSales ? fuelPoSales
                            .Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.INV_DATE == fuel.INV_DATE)
                            .Select(s => s.Amount)
                            .Concat(lubePoSales
                                .Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift && l.INV_DATE == fuel.INV_DATE)
                                .Select(l => l.Amount))
                            .ToArray() : new decimal[0],
                        Customers = HasPoSales ? fuelPoSales
                            .Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.INV_DATE == fuel.INV_DATE)
                            .Select(s => s.cust)
                            .Concat(lubePoSales
                                .Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift && l.INV_DATE == fuel.INV_DATE)
                                .Select(l => l.cust))
                            .ToArray() : new string[0],
                        TimeIn = fuel.TimeIn,
                        TimeOut = fuel.TimeOut
                    })
                    .GroupBy(s => new { s.Date, s.StationPosCode, s.Cashier, s.Shift, s.LubesTotalAmount, s.SafeDropTotalAmount, s.POSalesTotalAmount, s.CreatedBy })
                    .Select(g => new SalesHeader
                    {
                        Date = g.Key.Date,
                        StationPosCode = g.Key.StationPosCode,
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
                        StationCode = stationCode
                    })
                    .ToList();

                foreach (var dsr in salesHeaders)
                {
                    dsr.SalesNo = await GenerateSeriesNumber(dsr.StationCode);
                    await _db.SalesHeaders.AddAsync(dsr, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                double previousClosing = 0;
                foreach (var group in fuelSales.GroupBy(f => new {f.INV_DATE, f.ItemCode, f.xPUMP }))
                {

                    foreach (var fuel in group)
                    {
                        SalesHeader? salesHeader = salesHeaders.Find(s => s.Cashier == fuel.xONAME && s.Shift == fuel.Shift && s.Date == fuel.INV_DATE) ?? throw new InvalidOperationException($"Sales Header with {fuel.xONAME} shift#{fuel.Shift} not found!");

                        var salesDetail = new SalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
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

                        if (previousClosing != 0 && previousClosing != fuel.Opening)
                        {
                            salesHeader.IsTransactionNormal = false;
                            salesDetail.IsTransactionNormal = false;
                        }

                        await _db.SalesDetails.AddAsync(salesDetail, cancellationToken);

                        if (previousClosing == 0)
                        {
                            previousClosing = fuel.Closing;
                        }
                    }

                    previousClosing = 0;

                }

                if (lubeSales.Count != 0)
                {
                    foreach (var lube in lubeSales)
                    {
                        var salesHeader = salesHeaders.Find(l => l.Cashier == lube.Cashier && l.Shift == lube.Shift && l.Date == lube.INV_DATE);

                        var salesDetail = new SalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
                            Product = lube.ItemCode,
                            Particular = $"{lube.Particulars}",
                            Liters = lube.LubesQty,
                            Price = lube.Price,
                            Sale = lube.Amount,
                            Value = Math.Round(lube.Amount, 2)
                        };
                        lube.IsProcessed = true;
                        await _db.SalesDetails.AddAsync(salesDetail, cancellationToken);
                    }
                }

                if (fuelSales.Any())
                {
                    foreach(var fuel in fuels)
                    {
                        fuel.IsProcessed = true;
                    }
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
            }
        }

        public async Task PostAsync(string id, string postedBy, CancellationToken cancellationToken = default)
        {
            try
            {

                SalesVM salesVM = new()
                {
                    Header = await _db.SalesHeaders.FirstOrDefaultAsync(sd => sd.SalesNo == id, cancellationToken),
                    Details = await _db.SalesDetails.Where(sd => sd.SalesNo == id).ToListAsync(cancellationToken),
                };

                if (salesVM.Header == null || salesVM.Details == null)
                {
                    throw new InvalidOperationException($"Sales with id '{id}' not found.");
                }

                if (salesVM.Header.SafeDropTotalAmount == 0)
                {
                    throw new InvalidOperationException("Indicate the cashier's cash on hand before posting.");
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
                        AccountNumber = product.Key.Contains("PET") ? "4010101" : "4011001",
                        AccountTitle = product.Key.Contains("PET") ? "Sales - Fuel" : "Sales - Lubes",
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
                        AccountNumber = product.Key.Contains("PET") ? "5010101" : "5011001",
                        AccountTitle = product.Key.Contains("PET") ? "COGS - Fuel" : "COGS - Lubes",
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
                        AccountNumber = product.Key.Contains("PET") ? "1010401" : "1010410",
                        AccountTitle = product.Key.Contains("PET") ? "Inventory - Fuel" : "Inventory - Lubes",
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

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }
                        else if (transaction.Particulars == nameof(JournalType.Purchase))
                        {
                            transaction.RunningCost = runningCost + transaction.TotalCost;
                            transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                            transaction.UnitCostAverage = transaction.RunningCost / transaction.InventoryBalance;
                            transaction.CostOfGoodsSold = transaction.UnitCostAverage * transaction.Quantity;

                            unitCostAverage = transaction.UnitCostAverage;
                            runningCost = transaction.RunningCost;
                            inventoryBalance = transaction.InventoryBalance;
                        }

                        var journalEntries = _db.GeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber == (product.Key.Contains("PET") ? "5010101" : "5011001") || j.AccountNumber == (product.Key.Contains("PET") ? "1010401" : "1010410")))
                            .ToList();

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
                .FindAsync(model.Header.SalesHeaderId, cancellationToken) ?? throw new InvalidOperationException($"Sales header with id '{model.Header.SalesHeaderId}' not found.");

            IEnumerable<SalesDetail> existingSalesDetail = await _db.SalesDetails
                .Where(s => s.SalesHeaderId == model.Header.SalesHeaderId)
                .OrderBy(s => s.SalesDetailId)
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
    }
}
