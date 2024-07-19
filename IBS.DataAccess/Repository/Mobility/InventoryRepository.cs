using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class InventoryRepository : Repository<MobilityInventory>, IInventoryRepository
    {
        private ApplicationDbContext _db;

        public InventoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task CalculateTheBeginningInventory(MobilityInventory model, CancellationToken cancellationToken = default)
        {
            if (model.Quantity <= 0 || model.UnitCost <= 0)
            {
                throw new ArgumentException("Quantity and Unit Cost must be greater than zero.");
            }

            if (await _db.MobilityInventories.AnyAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode, cancellationToken))
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
            var journals = new List<MobilityGeneralLedger>
            {
                new() {
                    TransactionDate = model.Date,
                    Reference = model.TransactionNo,
                    Particular = $"Beginning Inventory for {model.ProductCode}",
                    AccountNumber = cogsAcctNo,
                    AccountTitle = cogsAcctTitle,
                    Debit = Math.Round(model.TotalCost, 4),
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
                    Credit = Math.Round(model.TotalCost, 4),
                    StationCode = model.StationCode,
                    ProductCode = model.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                }
            };

            await _db.MobilityGeneralLedgers.AddRangeAsync(journals, cancellationToken);

            #endregion

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<MobilityInventory> GetLastInventoryAsync(string productCode, string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityInventories
                .Where(i => i.ProductCode == productCode && i.StationCode == stationCode)
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.InventoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task CalculateTheActualSounding(MobilityInventory model, ActualSoundingViewModel viewModel, CancellationToken cancellationToken = default)
        {
            decimal totalCost = viewModel.Variance * model.UnitCostAverage;
            decimal runningCost = model.RunningCost + totalCost;
            decimal inventoryBalance = model.InventoryBalance + viewModel.Variance;
            decimal unitCostAverage = runningCost / inventoryBalance;

            MobilityInventory inventory = new()
            {
                Particulars = viewModel.Variance > 0 ? "Actual Sounding (Gain)" : "Actual Sounding (Loss)",
                Date = viewModel.Date,
                Reference = viewModel.Variance > 0 ? "Actual Sounding (Gain)" : "Actual Sounding (Loss)",
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

            var journals = new List<MobilityGeneralLedger>
            {
                new() {
                    TransactionDate = inventory.Date,
                    Reference = inventory.TransactionNo,
                    Particular = $"Actual Sounding for {inventory.ProductCode}",
                    AccountNumber = inventory.TotalCost > 0 ? inventoryAcctNo : "1010204",
                    AccountTitle = inventory.TotalCost > 0 ? inventoryAcctTitle : "Advances from Officers and Employees",
                    Debit = Math.Round(Math.Abs(inventory.TotalCost), 4),
                    Credit = 0,
                    StationCode = inventory.StationCode,
                    ProductCode = inventory.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                },
                new() {
                    TransactionDate = inventory.Date,
                    Reference = inventory.TransactionNo,
                    Particular = $"Actual Sounding for {inventory.ProductCode}",
                    AccountNumber = inventory.TotalCost > 0 ? "6010103" : "1010401",
                    AccountTitle = inventory.TotalCost > 0 ? "Gain on Inventory - Fuel" : "Inventory - Fuel",
                    Debit = 0,
                    Credit = Math.Round(Math.Abs(inventory.TotalCost), 4),
                    StationCode = inventory.StationCode,
                    ProductCode = inventory.ProductCode,
                    JournalReference = nameof(JournalType.Inventory),
                    IsValidated = true
                }
            };

            await _db.MobilityGeneralLedgers.AddRangeAsync(journals, cancellationToken);

            #endregion

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}