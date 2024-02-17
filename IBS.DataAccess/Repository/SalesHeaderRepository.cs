using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task ComputeSalesPerCashier(DateTime yesterday)
        {
            try
            {
                var fuelSales = _db.Fuels
                    .Where(f => f.INV_DATE == yesterday)
                    .GroupBy(f => new { f.xSITECODE, f.xONAME, f.INV_DATE, f.xPUMP, f.Particulars, f.Price, f.Shift, f.Calibration })
                    .Select(g => new
                    {
                        g.Key.xSITECODE,
                        g.Key.xONAME,
                        g.Key.INV_DATE,
                        g.Key.xPUMP,
                        g.Key.Particulars,
                        g.Key.Price,
                        g.Key.Shift,
                        g.Key.Calibration,
                        AmountDB = g.Sum(f => f.AmountDB),
                        Sale = g.Sum(f => f.Amount),
                        LitersSold = g.Sum(f => f.Volume),
                        Liters = g.Sum(f => f.Liters),
                        TransactionCount = g.Sum(f => f.TransCount),
                        Closing = g.Max(f => f.Closing),
                        Opening = g.Min(f => f.Opening)
                    })
                    .OrderBy(g => g.Shift)
                    .ThenBy(g => g.xSITECODE)
                    .ThenBy(g => g.Particulars)
                    .ThenBy(g => g.xPUMP)
                    .ToList();

                var lubeSales = _db.Lubes
                    .Where(f => f.INV_DATE == yesterday);

                var safeDropDeposits = _db.SafeDrops
                    .Where(f => f.INV_DATE == yesterday);

                var salesHeaders = fuelSales
                    .Select(fuel => new SalesHeader
                    {
                        SalesNo = DateTime.Now.ToString("yyyyMMddHHmmssfffffff"),
                        Date = fuel.INV_DATE,
                        StationPosCode = fuel.xSITECODE,
                        Cashier = fuel.xONAME,
                        Shift = fuel.Shift,
                        CreatedBy = "Ako",
                        FuelSalesTotalAmount = fuel.Sale,
                        LubesTotalAmount = lubeSales.Where(l => l.Cashier == fuel.xONAME).Sum(l => l.Amount),
                        SafeDropTotalAmount = safeDropDeposits.Where(s => s.xONAME == fuel.xONAME).Sum(s => s.Amount)
                    })
                    .GroupBy(s => new { s.Date, s.StationPosCode, s.Cashier, s.Shift, s.LubesTotalAmount, s.SafeDropTotalAmount, s.CreatedBy })
                    .Select(g => new SalesHeader
                    {
                        Date = g.Key.Date,
                        StationPosCode = g.Key.StationPosCode,
                        Cashier = g.Key.Cashier,
                        Shift = g.Key.Shift,
                        FuelSalesTotalAmount = g.Sum(g => g.FuelSalesTotalAmount),
                        LubesTotalAmount = g.Key.LubesTotalAmount,
                        SafeDropTotalAmount = g.Key.SafeDropTotalAmount,
                        CreatedBy = g.Key.CreatedBy,
                        SalesNo = g.Min(s => s.SalesNo)
                    })
                    .ToList();

                await _db.SalesHeaders.AddRangeAsync(salesHeaders);
                await _db.SaveChangesAsync();

                foreach (var fuel in fuelSales)
                {
                    var salesHeader = salesHeaders.Find(s => s.Cashier == fuel.xONAME);

                    var salesDetail = new SalesDetail
                    {
                        SalesHeaderId = salesHeader.SalesHeaderId,
                        SalesNo = salesHeader.SalesNo,
                        Product = fuel.Particulars,
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

                    await _db.SalesDetails.AddAsync(salesDetail);
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}