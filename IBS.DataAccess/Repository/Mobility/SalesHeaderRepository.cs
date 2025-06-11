using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using CsvHelper;
using IBS.DTOs;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.DataAccess.Repository.Mobility
{
    public class SalesHeaderRepository : Repository<MobilitySalesHeader>, ISalesHeaderRepository
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
                // ALREADY SHOW PROBLEMS HERE: THROWS EXCEPTION
                var fuelSales = await _db.FuelSalesViews
                    .Where(f => f.BusinessDate.Year == DateTime.UtcNow.Year)
                    .ToListAsync(cancellationToken);

                var lubeSales = await _db.MobilityLubes
                    .Where(l => !l.IsProcessed)
                    .Where(f => f.BusinessDate.Year == DateTime.UtcNow.Year)
                    .ToListAsync(cancellationToken);

                var safeDropDeposits = await _db.MobilitySafeDrops
                    .Where(s => !s.IsProcessed)
                    .Where(f => f.BusinessDate.Year == DateTime.UtcNow.Year)
                    .ToListAsync(cancellationToken);

                var fuelPoSales = Enumerable.Empty<MobilityFuel>();
                var lubePoSales = Enumerable.Empty<MobilityLube>();

                if (hasPoSales)
                {
                    fuelPoSales = await _db.MobilityFuels
                        .Where(f => !f.IsProcessed && (!string.IsNullOrEmpty(f.cust) || !string.IsNullOrEmpty(f.pono) || !string.IsNullOrEmpty(f.plateno)))
                        .ToListAsync(cancellationToken);

                    lubePoSales = await _db.MobilityLubes
                        .Where(f => !f.IsProcessed && (!string.IsNullOrEmpty(f.cust) || !string.IsNullOrEmpty(f.pono) || !string.IsNullOrEmpty(f.plateno)))
                        .ToListAsync(cancellationToken);
                }

                var salesHeaders = fuelSales
                    .Select(fuel => new MobilitySalesHeader
                    {
                        Date = fuel.BusinessDate,
                        StationCode = fuel.StationCode,
                        Cashier = fuel.xONAME,
                        Shift = fuel.Shift,
                        CreatedBy = "System Generated",
                        FuelSalesTotalAmount = fuel.Calibration == 0 ? Math.Round(fuel.Liters * fuel.Price, 4) : Math.Round((fuel.Liters - fuel.Calibration) * fuel.Price, 4),
                        LubesTotalAmount = Math.Round(lubeSales.Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift && l.BusinessDate == fuel.BusinessDate).Sum(l => l.Amount), 4),
                        SafeDropTotalAmount = Math.Round(safeDropDeposits.Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.BusinessDate == fuel.BusinessDate).Sum(s => s.Amount), 4),
                        POSalesTotalAmount = hasPoSales ? Math.Round(fuelPoSales.Where(s => s.xONAME == fuel.xONAME && s.Shift == fuel.Shift && s.BusinessDate == fuel.BusinessDate).Sum(s => s.Amount) + lubePoSales.Where(l => l.Cashier == fuel.xONAME && l.Shift == fuel.Shift).Sum(l => l.Amount), 4) : 0,
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
                    .Select(g => new MobilitySalesHeader
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
                        TotalSales = g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount - g.Key.POSalesTotalAmount,
                        GainOrLoss = g.Key.SafeDropTotalAmount - (g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount - g.Key.POSalesTotalAmount),
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
                    await _db.MobilitySalesHeaders.AddAsync(dsr, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }

                decimal previousClosing = 0;
                decimal previousOpening = 0;
                string previousNo = string.Empty;
                DateOnly previousStartDate = new();
                foreach (var group in fuelSales.GroupBy(f => new { f.ItemCode, f.xPUMP }))
                {
                    foreach (var fuel in group)
                    {
                        MobilitySalesHeader? salesHeader = salesHeaders.Find(s => s.Cashier == fuel.xONAME && s.Shift == fuel.Shift && s.Date == fuel.BusinessDate) ?? throw new InvalidOperationException($"Sales Header with {fuel.xONAME} shift#{fuel.Shift} on {fuel.BusinessDate} not found!");

                        var salesDetail = new MobilitySalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
                            StationCode = salesHeader.StationCode,
                            Product = fuel.ItemCode,
                            Particular = $"{fuel.Particulars} (P{fuel.xPUMP})",
                            PumpNumber = fuel.xPUMP,
                            Closing = fuel.Closing,
                            Opening = fuel.Opening,
                            Liters = fuel.Liters,
                            Calibration = fuel.Calibration,
                            LitersSold = fuel.LitersSold,
                            TransactionCount = fuel.TransactionCount,
                            Price = fuel.Price,
                            Sale = fuel.Sale,
                            Value = fuel.Calibration == 0 ? Math.Round(fuel.Liters * fuel.Price, 4) : Math.Round((fuel.Liters - fuel.Calibration) * fuel.Price, 4)
                        };

                        if (previousClosing != 0 && !string.IsNullOrEmpty(previousNo) && previousClosing != fuel.Opening)
                        {
                            salesHeader.IsTransactionNormal = false;
                            salesDetail.ReferenceNo = previousNo;

                            MobilityOffline offline = new(fuel.StationCode, previousStartDate, fuel.BusinessDate, fuel.Particulars, fuel.xPUMP, previousOpening, previousClosing,
                                fuel.Opening, fuel.Closing)
                            {
                                SeriesNo = await GenerateOfflineNo(fuel.StationCode),
                                FirstDsrNo = previousNo,
                                SecondDsrNo = salesDetail.SalesNo
                            };

                            await _db.MobilityOfflines.AddAsync(offline, cancellationToken);
                            await _db.SaveChangesAsync(cancellationToken);
                        }

                        await _db.MobilitySalesDetails.AddAsync(salesDetail, cancellationToken);

                        previousClosing = fuel.Closing;
                        previousOpening = fuel.Opening;
                        previousNo = salesHeader.SalesNo;
                        previousStartDate = fuel.BusinessDate;
                    }

                    previousClosing = 0;
                    previousNo = string.Empty;
                }

                foreach (var lube in lubeSales)
                {
                    var salesHeader = salesHeaders.Find(l => l.Cashier == lube.Cashier && l.Shift == lube.Shift && l.Date == lube.BusinessDate);

                    if (salesHeader != null)
                    {
                        var salesDetail = new MobilitySalesDetail
                        {
                            SalesHeaderId = salesHeader.SalesHeaderId,
                            SalesNo = salesHeader.SalesNo,
                            Product = lube.ItemCode,
                            StationCode = salesHeader.StationCode,
                            Particular = $"{lube.Particulars}",
                            Liters = lube.LubesQty,
                            Price = lube.Price,
                            Sale = lube.Amount,
                            Value = Math.Round(lube.Amount, 4)
                        };
                        lube.IsProcessed = true;
                        await _db.MobilitySalesDetails.AddAsync(salesDetail, cancellationToken);
                    }
                    else
                    {
                        var safeDrop = Math.Round(safeDropDeposits.Where(s => s.xONAME == lube.Cashier && s.Shift == lube.Shift && s.BusinessDate == lube.BusinessDate).Sum(s => s.Amount), 4);

                        await CreateSalesHeaderForLubes(lube, safeDrop, cancellationToken);
                    }
                }

                if (fuelSales.Count != 0)
                {
                    // Bulk update directly in the database without fetching
                    await _db.MobilityFuels
                        .Where(f => !f.IsProcessed)
                        .ExecuteUpdateAsync(
                            f => f.SetProperty(p => p.IsProcessed, true),
                            cancellationToken
                        );
                }

                foreach (var safedrop in safeDropDeposits)
                {
                    safedrop.IsProcessed = true;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"An error occurred: {ex.Message}");

                throw new InvalidOperationException("Error in ComputeSalesPerCashier:", ex);
            }
        }

        public IEnumerable<dynamic> GetSalesHeaderJoin(IEnumerable<MobilitySalesHeader> salesHeaders, CancellationToken cancellationToken = default)
        {
            return from header in salesHeaders
                   join station in _db.MobilityStations
                   on header.StationCode equals station.StationCode
                   select new
                   {
                       salesNo = header.SalesNo,
                       date = header.Date,
                       cashier = header.Cashier,
                       stationCode = header.StationCode,
                       shift = header.Shift,
                       timeIn = header.TimeIn,
                       timeOut = header.TimeOut,
                       postedBy = header.PostedBy,
                       safeDropTotalAmount = header.SafeDropTotalAmount,
                       actualCashOnHand = header.ActualCashOnHand,
                       isTransactionNormal = header.IsTransactionNormal,
                       stationCodeWithName = $"{header.StationCode} - {station.StationName}"
                   }.ToExpando();
        }

        public async Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                SalesVM salesVm = new()
                {
                    Header = await _db.MobilitySalesHeaders.FirstOrDefaultAsync(sh => sh.SalesNo == id && sh.StationCode == stationCode, cancellationToken),
                    Details = await _db.MobilitySalesDetails.Where(sd => sd.SalesNo == id && sd.StationCode == stationCode).ToListAsync(cancellationToken),
                };

                if (salesVm.Header == null || salesVm.Details == null)
                {
                    throw new InvalidOperationException($"Sales with id '{id}' not found.");
                }

                if (salesVm.Header.SafeDropTotalAmount == 0 && salesVm.Header.ActualCashOnHand == 0)
                {
                    throw new InvalidOperationException("Indicate the cashier's cash on hand before posting.");
                }

                var salesList = await _db.MobilitySalesHeaders
                    .Where(s => s.StationCode == salesVm.Header.StationCode && s.Date <= salesVm.Header.Date && s.CreatedDate < salesVm.Header.CreatedDate && s.PostedBy == null)
                    .OrderBy(s => s.SalesNo)
                    .ToListAsync(cancellationToken);

                if (salesList.Count > 0)
                {
                    throw new InvalidOperationException($"Can't proceed to post, you have unposted {salesList.First().SalesNo}");
                }

                StationDto station = await MapStationToDTO(salesVm.Header.StationCode, cancellationToken) ?? throw new InvalidOperationException($"Station with code {salesVm.Header.StationCode} not found.");

                salesVm.Header.PostedBy = postedBy;
                salesVm.Header.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var journals = new List<MobilityGeneralLedger>();
                var inventories = new List<MobilityInventory>();
                var cogsJournals = new List<MobilityGeneralLedger>();
                var journalEntriesToUpdate = new List<MobilityGeneralLedger>();

                journals.Add(new MobilityGeneralLedger
                {
                    TransactionDate = salesVm.Header.Date,
                    Reference = salesVm.Header.SalesNo,
                    Particular = $"Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                    AccountNumber = "1010102",
                    AccountTitle = "Cash on Hand",
                    Debit = salesVm.Header.ActualCashOnHand > 0 ? salesVm.Header.ActualCashOnHand : salesVm.Header.SafeDropTotalAmount,
                    Credit = 0,
                    StationCode = station.StationCode,
                    JournalReference = nameof(JournalType.Sales),
                    IsValidated = true
                });

                if (salesVm.Header.POSalesTotalAmount > 0)
                {
                    for (int i = 0; i < salesVm.Header.Customers.Length; i++)
                    {
                        journals.Add(new MobilityGeneralLedger
                        {
                            TransactionDate = salesVm.Header.Date,
                            Reference = salesVm.Header.SalesNo,
                            Particular = $"Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                            AccountNumber = "1010201",
                            AccountTitle = "AR-Trade Receivable",
                            Debit = salesVm.Header.POSalesAmount[i],
                            Credit = 0,
                            StationCode = station.StationCode,
                            CustomerCode = salesVm.Header.Customers[i],
                            JournalReference = nameof(JournalType.Sales),
                            IsValidated = true
                        });
                    }
                }

                foreach (var product in salesVm.Details.GroupBy(d => d.Product))
                {
                    ProductDto productDetails = await MapProductToDTO(product.Key, cancellationToken) ?? throw new InvalidOperationException($"Product with code '{product.Key}' not found.");

                    var (salesAcctNo, salesAcctTitle) = MobilityGetSalesAccountTitle(product.Key);
                    var (cogsAcctNo, cogsAcctTitle) = MobilityGetCogsAccountTitle(product.Key);
                    var (inventoryAcctNo, inventoryAcctTitle) = MobilityGetInventoryAccountTitle(product.Key);

                    journals.Add(new MobilityGeneralLedger
                    {
                        TransactionDate = salesVm.Header.Date,
                        Reference = salesVm.Header.SalesNo,
                        Particular = $"Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                        AccountNumber = salesAcctNo,
                        AccountTitle = salesAcctTitle,
                        Debit = 0,
                        Credit = Math.Round(product.Sum(p => p.Value) / 1.12m, 4),
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    journals.Add(new MobilityGeneralLedger
                    {
                        TransactionDate = salesVm.Header.Date,
                        Reference = salesVm.Header.SalesNo,
                        Particular = $"Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                        AccountNumber = "2010301",
                        AccountTitle = "Vat Output",
                        Debit = 0,
                        Credit = Math.Round(product.Sum(p => p.Value) / 1.12m * 0.12m, 4),
                        StationCode = station.StationCode,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    var sortedInventory = await _db
                        .MobilityInventories
                        .Where(i => i.ProductCode == product.Key && i.StationCode == station.StationCode)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.InventoryId)
                        .ToListAsync(cancellationToken);

                    var lastIndex = sortedInventory.FindLastIndex(s => s.Date <= salesVm.Header.Date);
                    if (lastIndex >= 0)
                    {
                        sortedInventory = sortedInventory.Skip(lastIndex).ToList();
                    }
                    else
                    {
                        throw new ArgumentException($"Beginning inventory for the month of '{salesVm.Header.Date:MMMM}' in this product '{product.Key} on station '{station.StationCode}' was not found!");
                    }

                    var previousInventory = sortedInventory.FirstOrDefault();

                    decimal quantity = product.Sum(p => p.Liters - p.Calibration);

                    if (quantity > previousInventory!.InventoryBalance)
                    {
                        throw new InvalidOperationException("The quantity exceeds the available inventory.");
                    }

                    decimal totalCost = quantity * previousInventory.UnitCostAverage;
                    decimal runningCost = previousInventory.RunningCost - totalCost;
                    decimal inventoryBalance = previousInventory.InventoryBalance - quantity;
                    decimal unitCostAverage = runningCost / inventoryBalance;
                    decimal cogs = unitCostAverage * quantity;

                    inventories.Add(new MobilityInventory
                    {
                        Particulars = nameof(JournalType.Sales),
                        Date = salesVm.Header.Date,
                        Reference = $"POS Sales Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
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
                        ValidatedBy = salesVm.Header.PostedBy,
                        ValidatedDate = salesVm.Header.PostedDate,
                        TransactionNo = salesVm.Header.SalesNo
                    });

                    cogsJournals.Add(new MobilityGeneralLedger
                    {
                        TransactionDate = salesVm.Header.Date,
                        Reference = salesVm.Header.SalesNo,
                        Particular = $"COGS:{productDetails.ProductCode} {productDetails.ProductName} Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                        AccountNumber = cogsAcctNo,
                        AccountTitle = cogsAcctTitle,
                        Debit = Math.Round(cogs, 4),
                        Credit = 0,
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    cogsJournals.Add(new MobilityGeneralLedger
                    {
                        TransactionDate = salesVm.Header.Date,
                        Reference = salesVm.Header.SalesNo,
                        Particular = $"COGS:{productDetails.ProductCode} {productDetails.ProductName} Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                        AccountNumber = inventoryAcctNo,
                        AccountTitle = inventoryAcctTitle,
                        Debit = 0,
                        Credit = Math.Round(cogs, 4),
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

                        journalEntriesToUpdate = await _db.MobilityGeneralLedgers
                            .Where(j => j.Particular == nameof(JournalType.Sales) && j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber.StartsWith("50101") || j.AccountNumber.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                        if (journalEntriesToUpdate.Count != 0)
                        {
                            foreach (var journal in journalEntriesToUpdate)
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

                        _db.MobilityGeneralLedgers.UpdateRange(journalEntriesToUpdate);
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    _db.MobilityInventories.UpdateRange(sortedInventory);
                }

                if (salesVm.Header.GainOrLoss != 0)
                {
                    journals.Add(new MobilityGeneralLedger
                    {
                        TransactionDate = salesVm.Header.Date,
                        Reference = salesVm.Header.SalesNo,
                        Particular = $"Cashier: {salesVm.Header.Cashier}, Shift:{salesVm.Header.Shift}",
                        AccountNumber = salesVm.Header.GainOrLoss < 0 ? "6100102" : "6010102",
                        AccountTitle = salesVm.Header.GainOrLoss < 0 ? "Cash Short - Handling" : "Cash Over - Handling",
                        Debit = salesVm.Header.GainOrLoss < 0 ? Math.Abs(salesVm.Header.GainOrLoss) : 0,
                        Credit = salesVm.Header.GainOrLoss > 0 ? salesVm.Header.GainOrLoss : 0,
                        StationCode = station.StationCode,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });
                }

                journals.AddRange(cogsJournals);

                ///TODO: waiting for actual journal entries
                if (true)//IsJournalEntriesBalanced(journals)
                {
                    await _db.MobilityInventories.AddRangeAsync(inventories, cancellationToken);
                    //await _db.MobilityGeneralLedgers.AddRangeAsync(journals, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
        }

        public async Task UpdateAsync(MobilitySalesHeader model, CancellationToken cancellationToken = default)
        {
            var existingSalesHeader = await _db.MobilitySalesHeaders
                .Include(sh => sh.SalesDetails)
                .FirstOrDefaultAsync(sh => sh.SalesHeaderId == model.SalesHeaderId, cancellationToken)
                ?? throw new InvalidOperationException($"Sales header with id '{model.SalesHeaderId}' not found.");

            var existingSalesDetails = existingSalesHeader.SalesDetails
                .OrderBy(sd => sd.SalesDetailId)
                .ToList();

            bool headerModified = false;

            for (int i = 0; i < existingSalesDetails.Count; i++)
            {
                var existingDetail = existingSalesDetails[i];
                var updatedDetail = model.SalesDetails[i];

                var changes = new Dictionary<string, (string OriginalValue, string NewValue)>();

                if (existingDetail.Closing != updatedDetail.Closing)
                {
                    changes["Closing"] = (existingDetail.Closing.ToString(SD.Four_Decimal_Format), updatedDetail.Closing.ToString(SD.Four_Decimal_Format));
                    existingDetail.Closing = updatedDetail.Closing;
                }

                if (existingDetail.Opening != updatedDetail.Opening)
                {
                    changes["Opening"] = (existingDetail.Opening.ToString(SD.Four_Decimal_Format), updatedDetail.Opening.ToString(SD.Four_Decimal_Format));
                    existingDetail.Opening = updatedDetail.Opening;
                }

                if (existingDetail.Calibration != updatedDetail.Calibration)
                {
                    changes["Calibration"] = (existingDetail.Calibration.ToString(SD.Four_Decimal_Format), updatedDetail.Calibration.ToString(SD.Four_Decimal_Format));
                    existingDetail.Calibration = updatedDetail.Calibration;
                }

                if (existingDetail.Price != updatedDetail.Price)
                {
                    changes["Price"] = (existingDetail.Price.ToString(SD.Four_Decimal_Format), updatedDetail.Price.ToString(SD.Four_Decimal_Format));
                    existingDetail.PreviousPrice = existingDetail.Price;
                    existingDetail.Price = updatedDetail.Price;
                }

                if (changes.Any())
                {
                    var salesDetailRepo = new SalesDetailRepository(_db);
                    await salesDetailRepo.LogChangesAsync(existingDetail.SalesDetailId, changes, model.EditedBy!, cancellationToken);

                    headerModified = true;
                    existingSalesHeader.IsModified = true;
                    existingDetail.Liters = existingDetail.Closing - existingDetail.Opening;
                    existingDetail.Value = existingDetail.Calibration == 0 ? existingDetail.Liters * existingDetail.Price : (existingDetail.Liters - existingDetail.Calibration) * existingDetail.Price;
                }
            }

            var headerChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();

            if (existingSalesHeader.Particular != model.Particular)
            {
                headerChanges["Particular"] = (existingSalesHeader.Particular!, model.Particular!);
                existingSalesHeader.Particular = model.Particular;
            }

            if (existingSalesHeader.ActualCashOnHand != model.ActualCashOnHand)
            {
                headerChanges["ActualCashOnHand"] = (existingSalesHeader.ActualCashOnHand.ToString(SD.Four_Decimal_Format), model.ActualCashOnHand.ToString(SD.Four_Decimal_Format));
                existingSalesHeader.ActualCashOnHand = model.ActualCashOnHand;
                existingSalesHeader.GainOrLoss = model.ActualCashOnHand - existingSalesHeader.TotalSales;
            }

            if (existingSalesHeader.Date != model.Date)
            {
                headerChanges["Date"] = (existingSalesHeader.Date.ToString(), model.Date.ToString());
                existingSalesHeader.Date = model.Date;
            }

            if (headerChanges.Any())
            {
                await LogChangesAsync(existingSalesHeader.SalesHeaderId, headerChanges, model.EditedBy!, cancellationToken);
                headerModified = true;
            }

            if (headerModified)
            {
                existingSalesHeader.FuelSalesTotalAmount = existingSalesDetails.Sum(d => d.Value);
                existingSalesHeader.TotalSales = existingSalesHeader.FuelSalesTotalAmount + existingSalesHeader.LubesTotalAmount;
                existingSalesHeader.GainOrLoss = existingSalesHeader.SafeDropTotalAmount - existingSalesHeader.TotalSales;
            }

            if (_db.ChangeTracker.HasChanges())
            {
                existingSalesHeader.EditedBy = model.EditedBy;
                existingSalesHeader.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No data changes!");
            }
        }

        private async Task<string> GenerateSeriesNumber(string stationCode)
        {
            var lastCashierReport = await _db.MobilitySalesHeaders
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

            return "POS0000000001";
        }

        private async Task<int> GenerateOfflineNo(string stationCode)
        {
            var lastRecord = await _db.MobilityOfflines
                .OrderByDescending(o => o.OfflineId)
                .FirstOrDefaultAsync(o => o.StationCode == stationCode);

            if (lastRecord != null)
            {
                return lastRecord.SeriesNo + 1;
            }

            return 1;
        }

        private async Task CreateSalesHeaderForLubes(MobilityLube lube, decimal safeDrop, CancellationToken cancellationToken)
        {
            var stationCode = await _db.MobilityStations.FirstOrDefaultAsync(s => s.PosCode == lube.xSITECODE.ToString(), cancellationToken);

            var lubeSalesHeader = new MobilitySalesHeader
            {
                SalesNo = await GenerateSeriesNumber(stationCode!.StationCode),
                Date = lube.BusinessDate,
                Cashier = lube.Cashier,
                Shift = lube.Shift,
                LubesTotalAmount = lube.Amount,
                CreatedBy = "System Generated",
                StationCode = stationCode.StationCode,
                POSalesAmount = [],
                Customers = [],
                SafeDropTotalAmount = safeDrop,
                Source = "POS",
            };

            lubeSalesHeader.TotalSales = lubeSalesHeader.LubesTotalAmount;
            lubeSalesHeader.GainOrLoss = lubeSalesHeader.SafeDropTotalAmount - lubeSalesHeader.LubesTotalAmount;

            await _db.MobilitySalesHeaders.AddAsync(lubeSalesHeader, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var salesDetails = new MobilitySalesDetail
            {
                SalesHeaderId = lubeSalesHeader.SalesHeaderId,
                SalesNo = lubeSalesHeader.SalesNo,
                StationCode = lubeSalesHeader.StationCode,
                Product = lube.ItemCode,
                Particular = $"{lube.Particulars}",
                Liters = lube.LubesQty,
                Price = lube.Price,
                Sale = lube.Amount,
                Value = Math.Round(lube.Amount, 4)
            };

            lubeSalesHeader.IsTransactionNormal = true;
            lube.IsProcessed = true;
            await _db.MobilitySalesDetails.AddAsync(salesDetails, cancellationToken);
        }

        public async Task<(int fuelCount, bool hasPoSales)> ProcessFuelGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            var records = ReadFuelRecordsGoogleDrive(file.FileContent);
            var (newRecords, hasPoSales) = await AddNewFuelRecords(records, cancellationToken);
            return (newRecords.Count, hasPoSales);
        }

        private List<MobilityFuel> ReadFuelRecordsGoogleDrive(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });
            return csv.GetRecords<MobilityFuel>()
                .Where(r => r.Opening.HasValue && r.Closing.HasValue)
                .OrderBy(r => r.INV_DATE)
                .ThenBy(r => r.ItemCode)
                .ThenBy(r => r.xPUMP)
                .ThenBy(r => r.Opening)
                .ToList();
        }

        private async Task<(List<MobilityFuel> newRecords, bool hasPoSales)> AddNewFuelRecords(List<MobilityFuel> records, CancellationToken cancellationToken)
        {
            var newRecords = new List<MobilityFuel>();
            var existingNozdownList = await _db.Set<MobilityFuel>().Select(r => r.nozdown).ToListAsync(cancellationToken);
            var existingNozdownSet = new HashSet<string>(existingNozdownList);

            DateOnly date = new();
            int shift = 0;
            decimal price = 0;
            int pump = 0;
            string itemCode = string.Empty;
            int detailCount = 0;
            bool hasPoSales = false;
            int fuelsCount = 0;
            string xTicketId = string.Empty;

            foreach (var record in records)
            {
                if (!existingNozdownSet.Contains(record.nozdown))
                {
                    hasPoSales |= !string.IsNullOrEmpty(record.cust) && !string.IsNullOrEmpty(record.plateno) && !string.IsNullOrEmpty(record.pono);

                    xTicketId = record.xTicketID;

                    if (record.xTicketID == xTicketId && record.INV_DATE == date && date != default)
                    {
                        record.BusinessDate = date;
                    }
                    else
                    {
                        record.BusinessDate = record.INV_DATE;
                    }

                    if (record.BusinessDate == date && record.Shift == shift && record.Price == price && record.xPUMP == pump && record.ItemCode == itemCode)
                    {
                        record.DetailGroup = detailCount;
                    }
                    else
                    {
                        detailCount++;
                        record.DetailGroup = detailCount;
                        date = record.BusinessDate;
                        shift = record.Shift;
                        price = record.Price;
                        pump = record.xPUMP;
                        itemCode = record.ItemCode;
                    }

                    newRecords.Add(record);
                    fuelsCount++;
                }
            }

            if (newRecords.Count != 0)
            {
                await _db.AddRangeAsync(newRecords, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return (newRecords, hasPoSales);
        }

        public async Task<(int lubeCount, bool hasPoSales)> ProcessLubeGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            var records = ReadLubeRecordsGoogleDrive(file.FileContent);
            var (newRecords, hasPoSales) = await AddNewLubeRecords(records, cancellationToken);
            return (newRecords.Count, hasPoSales);
        }

        private List<MobilityLube> ReadLubeRecordsGoogleDrive(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            return csv.GetRecords<MobilityLube>().ToList();
        }

        private async Task<(List<MobilityLube> newRecords, bool hasPoSales)> AddNewLubeRecords(List<MobilityLube> records, CancellationToken cancellationToken)
        {
            var newRecords = new List<MobilityLube>();
            var existingNozdownList = await _db.Set<MobilityLube>().Select(r => r.xStamp).ToListAsync(cancellationToken);
            var existingNozdownSet = new HashSet<string>(existingNozdownList);

            bool hasPoSales = false;
            int lubesCount = 0;

            foreach (var record in records)
            {
                if (!existingNozdownSet.Contains(record.xStamp))
                {
                    hasPoSales |= !string.IsNullOrEmpty(record.cust) && !string.IsNullOrEmpty(record.plateno) && !string.IsNullOrEmpty(record.pono);

                    record.BusinessDate = record.INV_DATE == DateOnly.FromDateTime(DateTime.UtcNow)
                        ? record.INV_DATE.AddDays(-1)
                        : record.INV_DATE;

                    newRecords.Add(record);
                    lubesCount++;
                }
            }

            if (newRecords.Count != 0)
            {
                await _db.AddRangeAsync(newRecords, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return (newRecords, hasPoSales);
        }

        public async Task<int> ProcessSafeDropGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            var records = ReadSafeDropRecordsGoogleDrive(file.FileContent);
            var newRecords = await AddNewSafeDropRecords(records, cancellationToken);
            return newRecords.Count;
        }

        private List<MobilitySafeDrop> ReadSafeDropRecordsGoogleDrive(byte[] file)
        {
            using var stream = new MemoryStream(file);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            return csv.GetRecords<MobilitySafeDrop>().ToList();
        }

        private async Task<List<MobilitySafeDrop>> AddNewSafeDropRecords(List<MobilitySafeDrop> records, CancellationToken cancellationToken)
        {
            var newRecords = new List<MobilitySafeDrop>();
            var existingNozdownList = await _db.Set<MobilitySafeDrop>().Select(r => r.xSTAMP).ToListAsync(cancellationToken);
            var existingNozdownSet = new HashSet<string>(existingNozdownList);

            foreach (var record in records)
            {
                if (!existingNozdownSet.Contains(record.xSTAMP))
                {
                    record.BusinessDate = record.INV_DATE == DateOnly.FromDateTime(DateTime.UtcNow)
                        ? record.INV_DATE.AddDays(-1)
                        : record.INV_DATE;

                    newRecords.Add(record);
                }
            }

            if (newRecords.Count != 0)
            {
                await _db.AddRangeAsync(newRecords, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return newRecords;
        }

        public override async Task<MobilitySalesHeader?> GetAsync(Expression<Func<MobilitySalesHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet
                .Include(sh => sh.SalesDetails)
                .FirstOrDefaultAsync(filter, cancellationToken);
        }

        public override async Task<IEnumerable<MobilitySalesHeader>> GetAllAsync(Expression<Func<MobilitySalesHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilitySalesHeader> query = dbSet
                .Include(sh => sh.SalesDetails);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        #region--Log Processing

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string modifiedBy, CancellationToken cancellationToken)
        {
            foreach (var change in changes)
            {
                var logReport = new MobilityLogReport
                {
                    Id = Guid.NewGuid(),
                    Reference = nameof(MobilitySalesHeader),
                    ReferenceId = id,
                    Description = change.Key,
                    Module = "Cashier Report",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModifiedBy = modifiedBy
                };
                await _db.MobilityLogReports.AddAsync(logReport, cancellationToken);
            }
        }

        #endregion

        public async Task<List<SelectListItem>> GetPostedDsrList(CancellationToken cancellationToken = default)
        {
            return await _db.MobilitySalesHeaders
                .OrderBy(dsr => dsr.SalesHeaderId)
                .Where(dsr => dsr.PostedBy != null)
                .Select(c => new SelectListItem
                {
                    Value = c.SalesHeaderId.ToString(),
                    Text = c.SalesNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetUnpostedDsrList(string stationCode, CancellationToken cancellationToken = default)
        {
            var query = await _db.MobilitySalesHeaders
                .Where(dsr => dsr.PostedBy == null)
                .OrderBy(dsr => dsr.SalesHeaderId)
                .ToListAsync(cancellationToken);

            return query
                .Where(dsr => dsr.StationCode == stationCode)
                .Select(c => new SelectListItem
                {
                    Value = c.SalesHeaderId.ToString(),
                    Text = c.SalesNo
                })
                .ToList();
        }

        public async Task ProcessCustomerInvoicing(CustomerInvoicingViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingSalesHeader = await GetAsync(s => s.SalesHeaderId == viewModel.SalesHeaderId, cancellationToken);

            if (existingSalesHeader != null)
            {
                var changes = new Dictionary<string, (string OriginalValue, string NewValue)>();

                if (viewModel.IncludePo)
                {
                    var updatedCustomers = new List<string>(existingSalesHeader.Customers!);
                    var updatedPOSalesAmount = new List<decimal>(existingSalesHeader.POSalesAmount);

                    for (int i = 0; i < viewModel.CustomerPos.Count; i++)
                    {
                        string customerCodeName = viewModel.CustomerPos[i].CustomerCodeName;
                        decimal poAmount = viewModel.CustomerPos[i].PoAmount;

                        if (!updatedCustomers.Contains(customerCodeName))
                        {
                            updatedCustomers.Add(customerCodeName);
                            updatedPOSalesAmount.Add(poAmount);
                            existingSalesHeader.POSalesTotalAmount += poAmount;

                            changes[$"Customers[{updatedCustomers.Count - 1}]"] = (string.Empty, customerCodeName);
                            changes[$"POSalesAmount[{updatedPOSalesAmount.Count - 1}]"] = ("0", poAmount.ToString());
                        }
                    }

                    existingSalesHeader.Customers = updatedCustomers.ToArray();
                    existingSalesHeader.POSalesAmount = updatedPOSalesAmount.ToArray();
                }

                if (viewModel.IncludeLubes)
                {
                    decimal totalLubeSales = 0;
                    for (int i = 0; i < viewModel.ProductDetails.Count; i++)
                    {
                        var product = await _db.Products.FindAsync(viewModel.ProductDetails[i].LubesId, cancellationToken);

                        var totalAmount = viewModel.ProductDetails[i].Quantity * viewModel.ProductDetails[i].Price;

                        var salesDetail = new MobilitySalesDetail
                        {
                            SalesHeaderId = existingSalesHeader.SalesHeaderId,
                            SalesNo = existingSalesHeader.SalesNo,
                            Product = product!.ProductCode,
                            StationCode = existingSalesHeader.StationCode,
                            Particular = $"{product.ProductName}",
                            Liters = viewModel.ProductDetails[i].Quantity,
                            Price = viewModel.ProductDetails[i].Price,
                            Sale = totalAmount,
                            Value = totalAmount
                        };

                        totalLubeSales += totalAmount;

                        await _db.AddAsync(salesDetail, cancellationToken);
                    }

                    changes[$"LubesTotalAmount"] = (existingSalesHeader.LubesTotalAmount.ToString(SD.Four_Decimal_Format), totalLubeSales.ToString(SD.Four_Decimal_Format));
                    existingSalesHeader.LubesTotalAmount = totalLubeSales;
                }

                var originalTotalSales = existingSalesHeader.TotalSales;
                var originalGainOrLoss = existingSalesHeader.GainOrLoss;

                existingSalesHeader.TotalSales = existingSalesHeader.FuelSalesTotalAmount + existingSalesHeader.LubesTotalAmount - existingSalesHeader.POSalesTotalAmount;
                existingSalesHeader.GainOrLoss = (existingSalesHeader.ActualCashOnHand > 0 ? existingSalesHeader.ActualCashOnHand : existingSalesHeader.SafeDropTotalAmount) - existingSalesHeader.TotalSales;

                if (changes.Count > 0)
                {
                    await LogChangesAsync(existingSalesHeader.SalesHeaderId, changes, viewModel.User!, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> ProcessFmsFuelSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSFuelSalesRawViewModel>();
            var fuelSales = new List<MobilityFMSFuelSales>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFMSFuelSales
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.shiftrecid))
                    continue;

                fuelSales.Add(new MobilityFMSFuelSales
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.shiftrecid,
                    PumpNumber = record.pumpnumber,
                    ProductCode = record.productcode,
                    Opening = record.opening,
                    Closing = record.closing,
                    Price = record.price,
                    ShiftDate = record.shiftdate,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber
                });
            }

            if (fuelSales.Count > 0)
            {
                await _db.MobilityFMSFuelSales.AddRangeAsync(fuelSales, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return fuelSales.Count;
        }

        public async Task<int> ProcessFmsLubeSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSLubeSalesRawViewModel>();
            var lubesSales = new List<MobilityFMSLubeSales>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFMSLubeSales
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.shiftrecid))
                    continue;

                lubesSales.Add(new MobilityFMSLubeSales
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.shiftrecid,
                    ProductCode = record.productcode,
                    Quantity = record.quantity,
                    Price = record.price,
                    ActualPrice = record.actualprice,
                    Cost = record.cost,
                    ShiftDate = record.shiftdate,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber
                });
            }

            if (lubesSales.Count > 0)
            {
                await _db.MobilityFMSLubeSales.AddRangeAsync(lubesSales, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return lubesSales.Count;
        }

        public async Task ProcessFmsCalibrationGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSCalibrationRawViewModel>();
            var calibrations = new List<MobilityFMSCalibration>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFmsCalibrations
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.shiftrecid))
                    continue;

                calibrations.Add(new MobilityFMSCalibration
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.shiftrecid,
                    PumpNumber = record.pumpnumber,
                    ProductCode = record.productcode,
                    Quantity = record.quantity,
                    Price = record.price,
                    ShiftDate = record.shiftdate,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber
                });
            }

            if (calibrations.Count > 0)
            {
                await _db.MobilityFmsCalibrations.AddRangeAsync(calibrations, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ProcessFmsCashierShiftGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSCashierShiftRawViewModel>();
            var cashiers = new List<MobilityFMSCashierShift>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFmsCashierShifts
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.recid))
                    continue;

                cashiers.Add(new MobilityFMSCashierShift
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.recid,
                    Date = record.date,
                    EmployeeNumber = record.empno,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber,
                    TimeIn = record.timein,
                    TimeOut = record.timeout,
                    NextDay = record.nextday == "T",
                    CashOnHand = record.cashonhand,
                    BiodieselPrice = record.pricebio,
                    EconogasPrice = record.priceeco,
                    EnvirogasPrice = record.priceenv,

                });
            }

            if (cashiers.Count > 0)
            {
                await _db.MobilityFmsCashierShifts.AddRangeAsync(cashiers, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ProcessFmsPoSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSPoSalesRawViewModel>();
            var poSales = new List<MobilityFMSPoSales>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFmsPoSales
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.shiftrecid))
                    continue;

                poSales.Add(new MobilityFMSPoSales
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.shiftrecid,
                    CustomerCode = record.customercode,
                    TripTicket = record.tripticket,
                    DrNumber = record.drno,
                    Driver = record.driver,
                    PlateNo = record.plateno,
                    ProductCode = record.productcode,
                    Quantity = record.quantity,
                    Price = record.price,
                    ContractPrice = record.contractprice,
                    Time = TimeOnly.TryParse(record.time, out var time) ? time : TimeOnly.MinValue,
                    Date = record.date,
                    ShiftDate = record.shiftdate,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber,
                });
            }

            if (poSales.Count > 0)
            {
                await _db.MobilityFmsPoSales.AddRangeAsync(poSales, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> ProcessFmsDepositGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(new MemoryStream(file.FileContent));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // Set to false if your file doesn't have headers
            });
            var records = csv.GetRecords<FMSDepositRawViewModel>();
            var deposits = new List<MobilityFMSDeposit>();

            // Get existing ShiftRecordIds from the database
            var existingShiftRecordIds = await _db.MobilityFmsDeposits
                .Select(f => f.ShiftRecordId)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if the ShiftRecordId already exists
                if (existingShiftRecordIds.Contains(record.shiftrecid))
                    continue;

                deposits.Add(new MobilityFMSDeposit
                {
                    StationCode = record.stncode,
                    ShiftRecordId = record.shiftrecid,
                    Date = record.date,
                    AccountNumber = record.accountno,
                    Amount = record.amount,
                    ShiftDate = record.shiftdate,
                    ShiftNumber = record.shiftnumber,
                    PageNumber = record.pagenumber,
                });
            }

            if (deposits.Count > 0)
            {
                await _db.MobilityFmsDeposits.AddRangeAsync(deposits, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return deposits.Count;
        }

        public async Task ComputeSalesReportForFms(CancellationToken cancellationToken = default)
        {
            var fmsCashierShifts = await _db.MobilityFmsCashierShifts
                .Where(f => !f.IsProcessed)
                .OrderBy(c => c.Date)
                .ThenBy(c => c.ShiftNumber)
                .ToListAsync(cancellationToken);

            var fmsFuelSales = await _db.MobilityFMSFuelSales
                .Where(f => !f.IsProcessed)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync(cancellationToken);

            var fmsLubeSales = await _db.MobilityFMSLubeSales
                .Where(f => !f.IsProcessed)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync(cancellationToken);

            var fmsCalibrations = await _db.MobilityFmsCalibrations
                .Where(f => !f.IsProcessed)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync(cancellationToken);

            var fmsPoSales = await _db.MobilityFmsPoSales
                .Where(x => !x.IsProcessed && x.ShiftDate.Year == DateTime.UtcNow.Year)
                .OrderBy(x => x.ShiftDate)
                .ThenBy(x => x.ShiftNumber)
                .ToListAsync(cancellationToken);


            var fmsDataByShift = fmsCashierShifts.Select(shift => new
            {
                Shift = shift,
                FuelSales = fmsFuelSales.Where(f => f.ShiftRecordId == shift.ShiftRecordId).ToList(),
                LubeSales = fmsLubeSales.Where(l => l.ShiftRecordId == shift.ShiftRecordId).ToList(),
                Calibrations = fmsCalibrations.Where(c => c.ShiftRecordId == shift.ShiftRecordId).ToList(),
                POSales = fmsPoSales.Where(p => p.ShiftRecordId == shift.ShiftRecordId).ToList(),
            }).ToList();

            foreach (var data in fmsDataByShift)
            {
                var employee = await _db.MobilityStationEmployees
                    .FirstOrDefaultAsync(e => e.EmployeeNumber == data.Shift.EmployeeNumber, cancellationToken);



                var salesHeader = new MobilitySalesHeader()
                {
                    SalesNo = await GenerateSeriesNumberForFmsSales(data.Shift.StationCode),
                    Date = data.Shift.Date,
                    StationCode = data.Shift.StationCode,
                    Cashier = employee?.FirstName ?? data.Shift.EmployeeNumber,
                    Shift = data.Shift.ShiftNumber,
                    PageNumber = data.Shift.PageNumber,
                    CreatedBy = "System Generated",
                    TimeIn = data.Shift.TimeIn,
                    TimeOut = data.Shift.TimeOut,
                    FuelSalesTotalAmount = data.Calibrations.Count == 0
                        ? data.FuelSales.Sum(f => (f.Closing - f.Opening) * f.Price)
                        : data.FuelSales.Sum(f => ((f.Closing - f.Opening) - data.Calibrations.Sum(x => x.Quantity)) * f.Price),
                    LubesTotalAmount = data.LubeSales.Sum(l => l.Quantity * l.Price),
                    SafeDropTotalAmount = data.Shift.CashOnHand,
                    POSalesTotalAmount = data.POSales.Sum(p => p.Price * p.Quantity),
                    POSalesAmount = data.POSales
                        .Select(p => p.Price * p.Quantity)
                        .ToArray(),
                    Customers = data.POSales
                        .Select(p => p.CustomerCode)
                        .ToArray(),
                    Source = "FMS"
                };

                salesHeader.TotalSales = salesHeader.FuelSalesTotalAmount + salesHeader.LubesTotalAmount - salesHeader.POSalesTotalAmount;
                salesHeader.GainOrLoss = salesHeader.SafeDropTotalAmount - salesHeader.TotalSales;

                await _db.MobilitySalesHeaders.AddAsync(salesHeader, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);


                foreach (var fuel in data.FuelSales.OrderBy(f => f.ProductCode))
                {
                    var product = await _db.MobilityProducts
                        .FirstOrDefaultAsync(p => p.ProductCode == fuel.ProductCode, cancellationToken) ?? throw new NullReferenceException($"Product {fuel.ProductCode} not found in {salesHeader.StationCode}.");

                    var posPumpNo = await _db.MobilityStationPumps
                        .FirstOrDefaultAsync(p => p.StationCode == salesHeader.StationCode &&
                                                  p.FmsPump == fuel.PumpNumber && p.ProductCode.ToUpper() == fuel.ProductCode.ToUpper(),
                            cancellationToken) ?? throw new NullReferenceException($"Pump {fuel.PumpNumber} not found in {salesHeader.StationCode}.");

                    var salesDetail = new MobilitySalesDetail()
                    {
                        SalesHeaderId = salesHeader.SalesHeaderId,
                        SalesNo = salesHeader.SalesNo,
                        StationCode = salesHeader.StationCode,
                        Product = product.ProductCode,
                        Particular = $"{product.ProductName} (P{posPumpNo.PosPump})",
                        PumpNumber = posPumpNo.PosPump,
                        Closing = fuel.Closing,
                        Opening = fuel.Opening,
                        Liters = fuel.Closing - fuel.Opening,
                        Calibration = data.Calibrations.Sum(c => c.Quantity),
                        LitersSold = fuel.Closing - fuel.Opening,
                        TransactionCount = 0,
                        Price = fuel.Price,
                    };

                    salesDetail.Liters = fuel.Closing - fuel.Opening;
                    salesDetail.LitersSold = salesDetail.Liters;
                    salesDetail.Sale = salesDetail.Calibration == 0 ? salesDetail.Liters * salesDetail.Price : (salesDetail.Liters - salesDetail.Calibration) * salesDetail.Price;
                    salesDetail.Value = salesDetail.Sale;
                    fuel.IsProcessed = true;

                    await _db.MobilitySalesDetails.AddAsync(salesDetail, cancellationToken);

                }

                foreach (var lube in data.LubeSales.OrderBy(l => l.ProductCode))
                {
                    var salesDetail = new MobilitySalesDetail()
                    {
                        SalesHeaderId = salesHeader.SalesHeaderId,
                        SalesNo = salesHeader.SalesNo,
                        StationCode = lube.StationCode,
                        Product = lube.ProductCode,
                        Particular = lube.ProductCode,
                        Liters = lube.Quantity,
                        Price = lube.Price,
                        Sale = lube.Quantity * lube.Price,
                        Value = lube.Quantity * lube.Price,
                    };

                    lube.IsProcessed = true;
                    await _db.MobilitySalesDetails.AddAsync(salesDetail, cancellationToken);

                }

                foreach (var calibration in data.Calibrations)
                {
                    calibration.IsProcessed = true;
                }

                data.Shift.IsProcessed = true;

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<string> GenerateSeriesNumberForFmsSales(string stationCode)
        {
            var lastCashierReport = await _db.MobilitySalesHeaders
                .OrderBy(s => s.SalesNo)
                .Where(s => s.StationCode == stationCode && s.Source == "FMS")
                .LastOrDefaultAsync();

            if (lastCashierReport != null)
            {
                string lastSeries = lastCashierReport.SalesNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
            }

            return "DSR0000000001";
        }
    }
}
