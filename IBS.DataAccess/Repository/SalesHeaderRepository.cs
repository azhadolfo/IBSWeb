﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
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

        public async Task ComputeSalesPerCashier(DateOnly yesterday, CancellationToken cancellationToken = default)
        {
            try
            {
                var fuelSales = await _db.Fuels
                    .Where(f => f.INV_DATE == yesterday)
                    .GroupBy(f => new { f.xSITECODE, f.xONAME, f.INV_DATE, f.xPUMP, f.Particulars, f.ItemCode, f.Price, f.Shift, f.Calibration })
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
                        TimeOut = g.Max(f => f.OutTime)
                    })
                    .OrderBy(g => g.Shift)
                    .ThenBy(g => g.xSITECODE)
                    .ThenBy(g => g.Particulars)
                    .ThenBy(g => g.xPUMP)
                    .ToListAsync(cancellationToken);

                var lubeSales = await _db.Lubes
                    .Where(f => f.INV_DATE == yesterday)
                    .ToListAsync(cancellationToken);

                var safeDropDeposits = await _db.SafeDrops
                    .Where(f => f.INV_DATE == yesterday)
                    .ToListAsync(cancellationToken);

                var salesHeaders = fuelSales
                    .Select(fuel => new SalesHeader
                    {
                        SalesNo = DateTime.Now.ToString("yyyyMMddHHmmssfffffff"),
                        Date = fuel.INV_DATE,
                        StationPosCode = fuel.xSITECODE.ToString(),
                        Cashier = fuel.xONAME,
                        Shift = fuel.Shift,
                        CreatedBy = "Ako",
                        FuelSalesTotalAmount = fuel.Sale,
                        LubesTotalAmount = lubeSales.Where(l => (l.Cashier == fuel.xONAME) && (l.Shift == fuel.Shift)).Sum(l => l.Amount),
                        SafeDropTotalAmount = safeDropDeposits.Where(s => (s.xONAME == fuel.xONAME) && (s.Shift == fuel.Shift)).Sum(s => s.Amount),
                        TimeIn = fuel.TimeIn,
                        TimeOut = fuel.TimeOut
                    })
                    .GroupBy(s => new { s.Date, s.StationPosCode, s.Cashier, s.Shift, s.LubesTotalAmount, s.SafeDropTotalAmount, s.CreatedBy })
                    .Select(g => new SalesHeader
                    {
                        SalesNo = g.Min(s => s.SalesNo),
                        Date = g.Key.Date,
                        StationPosCode = g.Key.StationPosCode,
                        Cashier = g.Key.Cashier,
                        Shift = g.Key.Shift,
                        FuelSalesTotalAmount = g.Sum(g => g.FuelSalesTotalAmount),
                        LubesTotalAmount = g.Key.LubesTotalAmount,
                        SafeDropTotalAmount = g.Key.SafeDropTotalAmount,
                        TotalSales = g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount,
                        GainOrLoss = g.Key.SafeDropTotalAmount - (g.Sum(g => g.FuelSalesTotalAmount) + g.Key.LubesTotalAmount),
                        CreatedBy = g.Key.CreatedBy,
                        TimeIn = g.Min(s => s.TimeIn),
                        TimeOut = g.Max(s => s.TimeOut)
                    })
                    .ToList();

                await _db.SalesHeaders.AddRangeAsync(salesHeaders, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                foreach (var fuel in fuelSales)
                {
                    var salesHeader = salesHeaders.Find(s => s.Cashier == fuel.xONAME);

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
                        Value = (decimal)fuel.Liters * fuel.Price
                    };

                    await _db.SalesDetails.AddAsync(salesDetail, cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public async Task PostAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {

                SalesVM? salesVM = new()
                {
                    Header = await _db.SalesHeaders.FindAsync(id, cancellationToken),
                    Details = await _db.SalesDetails.Where(sd => sd.SalesHeaderId == id).ToListAsync(cancellationToken),
                };

                Station? station = await _db
                    .Stations
                    .FirstOrDefaultAsync(s => s.PosCode == salesVM.Header.StationPosCode);

                salesVM.Header.PostedBy = "Ako";
                salesVM.Header.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();
                var inventories = new List<Inventory>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = salesVM.Header.Date,
                    Reference = salesVM.Header.SalesNo,
                    Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                    AccountNumber = 10100005,
                    AccountTitle = "Cash-on-Hand",
                    Debit = salesVM.Header.ActualCashOnHand > 0 ? salesVM.Header.ActualCashOnHand : salesVM.Header.SafeDropTotalAmount,
                    Credit = 0,
                    StationCode = station.StationCode,
                    JournalReference = "SALES",
                    IsValidated = true
                });

                foreach(var product in salesVM.Details.GroupBy(d => d.Product))
                {
                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = 40100005,
                        AccountTitle = product.Key == "PET001" ? "Sales-Bio" : product.Key == "PET002" ? "Sales-Econo" : "Sales-Enviro",
                        Debit = 0,
                        Credit = product.Sum(p => p.Sale) / 1.12m,
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = "SALES",
                        IsValidated = true
                    });

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = 20100065,
                        AccountTitle = "Output VAT",
                        Debit = 0,
                        Credit = (product.Sum(p => p.Sale) / 1.12m) * 0.12m,
                        StationCode = station.StationCode,
                        JournalReference = "SALES",
                        IsValidated = true
                    });

                    Inventory? previousInventory = await _db
                    .Inventories
                    .OrderByDescending(i => i.InventoryId)
                    .FirstOrDefaultAsync(i => i.ProductCode == product.Key,cancellationToken);

                    var quantity = (decimal)product.Sum(p => p.Liters);
                    var totalCost = quantity * previousInventory.UnitCostAverage;
                    var runningCost = previousInventory.RunningCost - totalCost;
                    var inventoryBalance = previousInventory.InventoryBalance - quantity;
                    var unitCostAverage = runningCost / inventoryBalance;

                    inventories.Add(new Inventory
                    {
                        Particulars = "Sales",
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
                        ValidatedBy = salesVM.Header.PostedBy,
                        ValidatedDate = salesVM.Header.PostedDate
                    });
                }

                if (salesVM.Header.GainOrLoss != 0)
                {
                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = 45300013,
                        AccountTitle = "Cash Short/(Over) - Handling",
                        Debit = salesVM.Header.GainOrLoss < 0 ? Math.Abs(salesVM.Header.GainOrLoss) : 0,
                        Credit = salesVM.Header.GainOrLoss > 0 ? salesVM.Header.GainOrLoss : 0,
                        StationCode = station.StationCode,
                        JournalReference = "SALES",
                        IsValidated = true
                    });
                }

                if (IsJournalEntriesBalance(journals))
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

        public async Task UpdateAsync(SalesHeader model, CancellationToken cancellationToken = default)
        {
            SalesHeader? existingSalesHeader = await _db
                .SalesHeaders
                .FindAsync(model.SalesHeaderId, cancellationToken);

            existingSalesHeader!.ActualCashOnHand = model.ActualCashOnHand;
            existingSalesHeader!.Particular = model.Particular;

            if (_db.ChangeTracker.HasChanges())
            {
                existingSalesHeader!.GainOrLoss = model.ActualCashOnHand - existingSalesHeader.TotalSales;
                existingSalesHeader.EditedBy = "Ako";
                existingSalesHeader.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No data changes!");
            }
        }
    }
}
