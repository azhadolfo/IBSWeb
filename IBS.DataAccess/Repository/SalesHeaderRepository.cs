using IBS.DataAccess.Data;
using IBS.DataAccess.Migrations;
using IBS.DataAccess.Repository.IRepository;
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
                        SalesNo = Guid.NewGuid().ToString(),
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

        public async Task PostAsync(string id, CancellationToken cancellationToken = default)
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

                Station station = await _db.Stations
                    .FirstOrDefaultAsync(s => s.PosCode == salesVM.Header.StationPosCode, cancellationToken) ?? throw new InvalidOperationException($"Station with POS code {salesVM.Header.StationPosCode} not found.");

                salesVM.Header.PostedBy = "Ako";
                salesVM.Header.PostedDate = DateTime.Now;

                var journals = new List<GeneralLedger>();
                var inventories = new List<Inventory>();
                var cogsJournals = new List<GeneralLedger>();

                journals.Add(new GeneralLedger
                {
                    TransactionDate = salesVM.Header.Date,
                    Reference = salesVM.Header.SalesNo,
                    Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                    AccountNumber = "10100005",
                    AccountTitle = "Cash-on-Hand",
                    Debit = salesVM.Header.ActualCashOnHand > 0 ? salesVM.Header.ActualCashOnHand : salesVM.Header.SafeDropTotalAmount,
                    Credit = 0,
                    StationCode = station.StationCode,
                    JournalReference = nameof(JournalType.Sales),
                    IsValidated = true
                });

                foreach(var product in salesVM.Details.GroupBy(d => d.Product))
                {
                    Product productDetails = await _db.Products
                        .FirstOrDefaultAsync(p => p.ProductCode == product.Key, cancellationToken) ?? throw new InvalidOperationException($"Product with code '{product.Key}' not found.");

                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = "40100005",
                        AccountTitle = product.Key == "PET001" ? "Sales-Bio" : product.Key == "PET002" ? "Sales-Econo" : "Sales-Enviro",
                        Debit = 0,
                        Credit = !salesVM.Header.IsModified ? product.Sum(p => p.Sale) / 1.12m : product.Sum(p => p.Value) / 1.12m,
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
                        AccountNumber = "20100065",
                        AccountTitle = "Output VAT",
                        Debit = 0,
                        Credit = !salesVM.Header.IsModified ? (product.Sum(p => p.Sale) / 1.12m) * 0.12m : (product.Sum(p => p.Value) / 1.12m) * 0.12m,
                        StationCode = station.StationCode,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });

                    Inventory previousInventory = await _db
                    .Inventories
                    .OrderByDescending(i => i.InventoryId)
                    .FirstOrDefaultAsync(i => i.ProductCode == product.Key && i.StationCode == station.StationCode, cancellationToken) ?? throw new ArgumentException($"Beginning inventory for {product.Key} in station {station.StationCode} not found!");

                    decimal quantity = (decimal)product.Sum(p => p.Liters);
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
                        AccountNumber = "50100005",
                        AccountTitle = "Cost of Goods Sold",
                        Debit = cogs,
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
                        AccountNumber = "10100033",
                        AccountTitle = "Merchandise Inventory",
                        Debit = 0,
                        Credit = cogs,
                        StationCode = station.StationCode,
                        ProductCode = product.Key,
                        JournalReference = nameof(JournalType.Sales),
                        IsValidated = true
                    });
                }

                if (salesVM.Header.GainOrLoss != 0)
                {
                    journals.Add(new GeneralLedger
                    {
                        TransactionDate = salesVM.Header.Date,
                        Reference = salesVM.Header.SalesNo,
                        Particular = $"Cashier: {salesVM.Header.Cashier}, Shift:{salesVM.Header.Shift}",
                        AccountNumber = "45300013",
                        AccountTitle = "Cash Short/(Over) - Handling",
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
                existingSalesHeader!.GainOrLoss = existingSalesHeader.TotalSales - existingSalesHeader.SafeDropTotalAmount;
            }

            existingSalesHeader!.Particular = model.Header.Particular;

            if (existingSalesHeader.ActualCashOnHand != model.Header.ActualCashOnHand)
            {
                existingSalesHeader!.ActualCashOnHand = model.Header.ActualCashOnHand;
                existingSalesHeader!.GainOrLoss = model.Header.ActualCashOnHand - existingSalesHeader.TotalSales;
            }

            if (_db.ChangeTracker.HasChanges())
            {
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
