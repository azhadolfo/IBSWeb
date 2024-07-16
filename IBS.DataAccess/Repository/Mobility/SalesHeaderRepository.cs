﻿using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

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
                var fuelSales = await _db.FuelSalesViews
                    .ToListAsync(cancellationToken);

                var lubeSales = await _db.MobilityLubes
                    .Where(l => !l.IsProcessed && l.BusinessDate.Month == 6)
                    .ToListAsync(cancellationToken);

                var safeDropDeposits = await _db.MobilitySafeDrops
                    .Where(s => !s.IsProcessed && s.BusinessDate.Month == 6)
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
                        FuelSalesTotalAmount = Math.Round(fuel.Liters * fuel.Price, 4),
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
                            Closing = fuel.Closing,
                            Opening = fuel.Opening,
                            Liters = fuel.Liters,
                            Calibration = fuel.Calibration,
                            LitersSold = fuel.LitersSold,
                            TransactionCount = fuel.TransactionCount,
                            Price = fuel.Price,
                            Sale = fuel.Sale,
                            Value = Math.Round(fuel.Liters * fuel.Price, 4)
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
                       StationCodeWithName = $"{header.StationCode} - {station.StationName}"
                   }.ToExpando();
        }

        public async Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default)
        {
            try
            {
                SalesVM salesVM = new()
                {
                    Header = await _db.MobilitySalesHeaders.FirstOrDefaultAsync(sh => sh.SalesNo == id && sh.StationCode == stationCode, cancellationToken),
                    Details = await _db.MobilitySalesDetails.Where(sd => sd.SalesNo == id && sd.StationCode == stationCode).ToListAsync(cancellationToken),
                };

                if (salesVM.Header == null || salesVM.Details == null)
                {
                    throw new InvalidOperationException($"Sales with id '{id}' not found.");
                }

                if (salesVM.Header.SafeDropTotalAmount == 0 && salesVM.Header.ActualCashOnHand == 0)
                {
                    throw new InvalidOperationException("Indicate the cashier's cash on hand before posting.");
                }

                var salesList = await _db.MobilitySalesHeaders
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

                    var (salesAcctNo, salesAcctTitle) = GetSalesAccountTitle(product.Key);
                    var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(product.Key);
                    var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(product.Key);

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = salesAcctNo,
                        AccountTitle = salesAcctTitle,
                        Debit = 0,
                        Credit = Math.Round(product.Sum(p => p.Value) / 1.12m, 4),
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    }); ;

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = "2010301",
                        AccountTitle = "Vat Output",
                        Debit = 0,
                        Credit = Math.Round(product.Sum(p => p.Value) / 1.12m * 0.12m, 4),
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
                        throw new ArgumentException($"Beginning inventory for the month of '{salesVM.Header.Date:MMMM}' in this product '{product.Key} on station '{station.StationCode}' was not found!");
                    }

                    var previousInventory = sortedInventory.FirstOrDefault();

                    decimal quantity = product.Sum(p => p.Liters);

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
                        AccountNumber = cogsAcctNo,
                        AccountTitle = cogsAcctTitle,
                        Debit = Math.Round(cogs, 4),
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

                        var journalEntries = await _db.GeneralLedgers
                            .Where(j => j.Reference == transaction.TransactionNo && j.ProductCode == transaction.ProductCode &&
                                        (j.AccountNumber.StartsWith("50101") || j.AccountNumber.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

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

        public async Task UpdateAsync(SalesVM model, decimal[] closing, decimal[] opening, CancellationToken cancellationToken = default)
        {
            MobilitySalesHeader existingSalesHeader = await _db.MobilitySalesHeaders
                .FirstOrDefaultAsync(sh => sh.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken) ?? throw new InvalidOperationException($"Sales header with id '{model.Header.SalesHeaderId}' not found.");

            IEnumerable<MobilitySalesDetail> existingSalesDetail = await _db.MobilitySalesDetails
                .Where(sd => sd.SalesHeaderId == model.Header.SalesHeaderId)
                .OrderBy(sd => sd.SalesDetailId)
                .ToListAsync(cancellationToken);

            bool headerModified = false;

            for (int i = 0; i < existingSalesDetail.Count(); i++)
            {
                MobilitySalesDetail existingDetail = existingSalesDetail.ElementAt(i);

                if (existingDetail.Closing != closing[i] || existingDetail.Opening != opening[i])
                {
                    headerModified = true;
                    existingSalesHeader.IsModified = true;
                    existingDetail.Closing = closing[i];
                    existingDetail.Opening = opening[i];
                    existingDetail.Liters = existingDetail.Closing - existingDetail.Opening;
                    existingDetail.Value = existingDetail.Liters * existingDetail.Price;
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
            else
            {
                return "DSR0000000001";
            }
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
            var stationCode = await _db.Stations.FirstOrDefaultAsync(s => s.PosCode == lube.xSITECODE.ToString(), cancellationToken);

            var lubeSalesHeader = new MobilitySalesHeader
            {
                SalesNo = await GenerateSeriesNumber(stationCode.StationCode),
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

        public async Task<(int fuelCount, bool hasPoSales)> ProcessFuel(string file, CancellationToken cancellationToken = default)
        {
            var records = ReadFuelRecords(file);
            var (newRecords, hasPoSales) = await AddNewFuelRecords(records, cancellationToken);
            return (newRecords.Count, hasPoSales);
        }

        private List<MobilityFuel> ReadFuelRecords(string file)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(stream);
            using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            });

            return csv.GetRecords<MobilityFuel>()
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
            string itemCode = "";
            int detailCount = 0;
            bool hasPoSales = false;
            int fuelsCount = 0;

            foreach (var record in records)
            {
                if (!existingNozdownSet.Contains(record.nozdown))
                {
                    hasPoSales |= !string.IsNullOrEmpty(record.cust) && !string.IsNullOrEmpty(record.plateno) && !string.IsNullOrEmpty(record.pono);

                    record.BusinessDate = record.INV_DATE == DateOnly.FromDateTime(DateTime.Now)
                        ? record.INV_DATE.AddDays(-1)
                        : record.INV_DATE;

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

        public async Task<(int lubeCount, bool hasPoSales)> ProcessLube(string file, CancellationToken cancellationToken = default)
        {
            var records = ReadLubeRecords(file);
            var (newRecords, hasPoSales) = await AddNewLubeRecords(records, cancellationToken);
            return (newRecords.Count, hasPoSales);
        }

        private List<MobilityLube> ReadLubeRecords(string file)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
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

                    record.BusinessDate = record.INV_DATE == DateOnly.FromDateTime(DateTime.Now)
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

        public async Task<int> ProcessSafeDrop(string file, CancellationToken cancellationToken = default)
        {
            var records = ReadSafeDropRecords(file);
            var newRecords = await AddNewSafeDropRecords(records, cancellationToken);
            return newRecords.Count;
        }

        private List<MobilitySafeDrop> ReadSafeDropRecords(string file)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
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
                    record.BusinessDate = record.INV_DATE == DateOnly.FromDateTime(DateTime.Now)
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

        public override async Task<MobilitySalesHeader> GetAsync(Expression<Func<MobilitySalesHeader, bool>> filter, CancellationToken cancellationToken = default)
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
    }
}