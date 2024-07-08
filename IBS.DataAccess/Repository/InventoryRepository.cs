using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;

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

            var (cogsAcctNo, cogsAcctTitle) = GetCogsAccountTitle(model.ProductCode);
            var journals = new List<GeneralLedger>
            {
                new() {
                    TransactionDate = model.Date,
                    Reference = model.TransactionNo,
                    Particular = $"Beginning Inventory for {model.ProductCode}",
                    AccountNumber = cogsAcctNo,
                    AccountTitle = cogsAcctTitle,
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
                    AccountNumber = "3020101",
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

        public async Task<Inventory> GetLastInventoryAsync(string productCode, string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.Inventories
                .Where(i => i.ProductCode == productCode && i.StationCode == stationCode)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.InventoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task CalculateTheActualInventory(Inventory model, ActualInventoryViewModel viewModel, CancellationToken cancellationToken = default)
        {
            decimal totalCost = viewModel.Variance * model.UnitCostAverage;
            decimal runningCost = model.RunningCost + totalCost;
            decimal inventoryBalance = model.InventoryBalance + viewModel.Variance;
            decimal unitCostAverage = runningCost / inventoryBalance;

            Inventory inventory = new()
            {
                Particulars = viewModel.Variance > 0 ? "Actual Inventory (Gain)" : "Actual Inventory (Loss)",
                Date = viewModel.Date,
                Reference = viewModel.Variance > 0 ? "Actual Inventory (Gain)" : "Actual Inventory (Loss)",
                ProductCode = viewModel.ProductCode,
                StationCode = model.StationCode,
                Quantity = viewModel.Variance,
                UnitCost = model.UnitCost,
                TotalCost = totalCost,
                InventoryBalance = inventoryBalance,
                RunningCost = runningCost,
                UnitCostAverage = unitCostAverage,
                InventoryValue = runningCost,
                ValidatedBy = "N/A",
                TransactionNo = Guid.NewGuid().ToString()
            };

            await _db.AddAsync(inventory, cancellationToken);

            #region--General Ledger Entries

            var (inventoryAcctNo, inventoryAcctTitle) = GetInventoryAccountTitle(inventory.ProductCode);

            //PENDING Accounting entries for actual inventory
            var journals = new List<GeneralLedger>
            {
                new() {
                    TransactionDate = inventory.Date,
                    Reference = inventory.TransactionNo,
                    Particular = $"Actual Inventory for {inventory.ProductCode}",
                    AccountNumber = inventory.TotalCost > 0 ? inventoryAcctNo : "1010204",
                    AccountTitle = inventory.TotalCost > 0 ? inventoryAcctTitle : "Advances from Officers and Employees",
                    Debit = Math.Round(Math.Abs(inventory.TotalCost), 2),
                    Credit = 0,
                    StationCode = inventory.StationCode,
                    ProductCode = inventory.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                },
                new() {
                    TransactionDate = inventory.Date,
                    Reference = inventory.TransactionNo,
                    Particular = $"Actual Inventory for {inventory.ProductCode}",
                    AccountNumber = inventory.TotalCost > 0 ? "6010103" : "1010401",
                    AccountTitle = inventory.TotalCost > 0 ? "Gain on Inventory - Fuel" : "Inventory - Fuel",
                    Debit = 0,
                    Credit = Math.Round(Math.Abs(inventory.TotalCost), 2),
                    StationCode = inventory.StationCode,
                    ProductCode = inventory.ProductCode,
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