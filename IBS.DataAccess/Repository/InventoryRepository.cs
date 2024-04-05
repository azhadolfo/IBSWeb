using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace IBS.DataAccess.Repository
{
    public class InventoryRepository : Repository<Inventory>, IInventoryRepository
    {
        private ApplicationDbContext _db;

        public InventoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task CalculateTheBeginningInventory(Inventory model, CancellationToken cancellationToken = default)
        {
            if (model.Quantity <= 0 || model.UnitCost <= 0)
            {
                throw new ArgumentException("Quantity and Unit Cost must be greater than zero.");
            }

            if (await _db.Inventories.AnyAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode, cancellationToken))
            {
                throw new InvalidOperationException($"{model.ProductCode} in {model.StationCode} had already beginning inventory.");
            }

            model.Particulars = "Beginning Inventory";
            model.Reference = "Beginning Inventory";
            model.TotalCost = model.Quantity * model.UnitCost;
            model.RunningCost = model.TotalCost;
            model.InventoryBalance = model.Quantity;
            model.UnitCostAverage = model.UnitCost;
            model.InventoryValue = model.RunningCost;
            model.ValidatedBy = "N/A";
            model.TransactionNo = Guid.NewGuid().ToString();

            await _db.AddAsync(model, cancellationToken);

            #region--General Ledger Entries

            var journals = new List<GeneralLedger>
            {
                new() {
                    TransactionDate = model.Date,
                    Reference = model.TransactionNo,
                    Particular = $"Beginning Inventory for {model.ProductCode}",
                    AccountNumber = "50100005",
                    AccountTitle = "Cost of Goods Sold",
                    Debit = Math.Round(model.TotalCost, 2),
                    Credit = 0,
                    StationCode = model.StationCode,
                    ProductCode = model.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                },
                new() {
                    TransactionDate = model.Date,
                    Reference = model.TransactionNo,
                    Particular = $"Beginning Inventory for {model.ProductCode}",
                    AccountNumber = "30200005",
                    AccountTitle = "Retained Earnings",
                    Debit = 0,
                    Credit = Math.Round(model.TotalCost, 2),
                    StationCode = model.StationCode,
                    ProductCode = model.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                }
            };

            await _db.GeneralLedgers.AddRangeAsync(journals, cancellationToken);

            #endregion


            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
