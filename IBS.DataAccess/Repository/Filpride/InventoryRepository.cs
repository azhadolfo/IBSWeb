using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class InventoryRepository : Repository<FilprideInventory>, IInventoryRepository
    {
        private readonly ApplicationDbContext _db;

        public InventoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddActualInventory(ActualInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default)
        {
            #region -- Actual Inventory Entry --

            var total = viewModel.Variance * viewModel.AverageCost;
            var inventoryBalance = viewModel.Variance + viewModel.PerBook;
            var totalBalance = viewModel.TotalBalance + total;
            var particular = viewModel.Variance < 0 ? "Actual Inventory(Loss)" : "Actual Inventory(Gain)";

            FilprideInventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                Quantity = Math.Abs(viewModel.Variance),
                Cost = viewModel.AverageCost,
                Particular = particular,
                Total = Math.Abs(total),
                InventoryBalance = inventoryBalance,
                AverageCost = totalBalance / inventoryBalance,
                TotalBalance = totalBalance,
                POId = viewModel.POId,
                Company = company
            };
            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion -- Actual Inventory Entry --

            #region -- General Book Entry --

            var ledger = new List<FilprideGeneralLedgerBook>();
            for (var i = 0; i < viewModel.AccountNumber.Length; i++)
            {
                ledger.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = viewModel.Date,
                        Reference = inventory.InventoryId.ToString(),
                        AccountNo = viewModel.AccountNumber[i],
                        AccountTitle = viewModel.AccountTitle[i],
                        Description = particular,
                        Debit = Math.Abs(viewModel.Debit[i]),
                        Credit = Math.Abs(viewModel.Credit[i]),
                        CreatedBy = viewModel.CurrentUser,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        IsPosted = false
                    });
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledger, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion -- General Book Entry --
        }

        public async Task AddBeginningInventory(BeginningInventoryViewModel viewModel, string company, CancellationToken cancellationToken = default)
        {
            FilprideInventory inventory = new()
            {
                Date = viewModel.Date,
                ProductId = viewModel.ProductId,
                POId = viewModel.POId,
                Quantity = viewModel.Quantity,
                Cost = viewModel.Cost,
                Particular = "Beginning Balance",
                Total = viewModel.Quantity * viewModel.Cost,
                InventoryBalance = viewModel.Quantity,
                AverageCost = viewModel.Cost,
                TotalBalance = viewModel.Quantity * viewModel.Cost,
                IsValidated = true,
                ValidatedBy = viewModel.CurrentUser,
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Company = company
            };

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task AddPurchaseToInventoryAsync(FilprideReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
                .Where(i => i.Company == receivingReport.Company &&
                            i.ProductId == receivingReport.PurchaseOrder!.Product!.ProductId &&
                            i.POId == receivingReport.POId)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Particular == "Purchases" ? 0 : 1) // Purchases first, then Sales
                .ThenBy(i => i.Particular)
                .ToListAsync(cancellationToken);

            // Find the correct insertion point - after all purchases on the same date, but before any sales
            var lastIndex = -1;
            for (int i = 0; i < sortedInventory.Count; i++)
            {
                if (sortedInventory[i].Date > receivingReport.Date)
                    break;

                if (sortedInventory[i].Date == receivingReport.Date)
                {
                    // If same date, include all purchases as "previous"
                    if (sortedInventory[i].Particular == "Purchases")
                    {
                        lastIndex = i;
                    }
                    // Don't advance for sales on the same date - they should be subsequent
                }
                else if (sortedInventory[i].Date < receivingReport.Date)
                {
                    lastIndex = i;
                }
            }

            var previousInventory = lastIndex >= 0 ? sortedInventory[lastIndex] : null;
            var subsequentTransactions = sortedInventory.Skip(lastIndex + 1).ToList();

            // Calculate initial values
            var total = receivingReport.PurchaseOrder?.VatType == SD.VatType_Vatable
                ? ComputeNetOfVat(receivingReport.Amount)
                : receivingReport.Amount;

            var inventoryBalance = (previousInventory?.InventoryBalance ?? 0) + receivingReport.QuantityReceived;
            var totalBalance = (previousInventory?.TotalBalance ?? 0) + total;

            var averageCost = Math.Round(totalBalance, 4) == 0 || Math.Round(inventoryBalance, 4) == 0
                ? total / receivingReport.QuantityReceived
                : totalBalance / inventoryBalance;

            // Create new inventory entry
            var inventory = new FilprideInventory
            {
                Date = receivingReport.Date,
                ProductId = receivingReport.PurchaseOrder!.ProductId,
                POId = receivingReport.POId,
                Particular = "Purchases",
                Reference = receivingReport.ReceivingReportNo,
                Quantity = receivingReport.QuantityReceived,
                Cost = total / receivingReport.QuantityReceived, // unit cost
                IsValidated = true,
                ValidatedBy = receivingReport.CreatedBy, // Add this if available
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(), // Add this if available
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost,
                Company = receivingReport.Company
            };

            // Update subsequent transactions with running totals
            var runningInventoryBalance = inventoryBalance;
            var runningTotalBalance = totalBalance;
            var runningAverageCost = averageCost;

            foreach (var transaction in subsequentTransactions)
            {
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = runningAverageCost;
                    transaction.Total = transaction.Quantity * runningAverageCost;
                    transaction.TotalBalance = runningTotalBalance - transaction.Total;
                    transaction.InventoryBalance = runningInventoryBalance - transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 ||
                                              Math.Round(transaction.InventoryBalance, 4) == 0
                        ? transaction.Cost
                        : transaction.TotalBalance / transaction.InventoryBalance;

                    var costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    // Update running totals
                    runningAverageCost = transaction.AverageCost;
                    runningTotalBalance = transaction.TotalBalance;
                    runningInventoryBalance = transaction.InventoryBalance;

                    // Update journal entries efficiently
                    await UpdateJournalEntriesForCostOfGoodsSoldAsync(transaction.Reference!, costOfGoodsSold, cancellationToken);
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = runningTotalBalance + transaction.Total;
                    transaction.InventoryBalance = runningInventoryBalance + transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 ||
                                              Math.Round(transaction.InventoryBalance, 4) == 0
                        ? transaction.Cost
                        : transaction.TotalBalance / transaction.InventoryBalance;

                    // Update running totals
                    runningAverageCost = transaction.AverageCost;
                    runningTotalBalance = transaction.TotalBalance;
                    runningInventoryBalance = transaction.InventoryBalance;
                }
            }

            // Batch updates for better performance
            if (subsequentTransactions.Count != 0)
            {
                _db.FilprideInventories.UpdateRange(subsequentTransactions);
            }

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateJournalEntriesForCostOfGoodsSoldAsync(string reference, decimal costOfGoodsSold, CancellationToken cancellationToken)
        {
            var journalEntries = await _db.FilprideGeneralLedgerBooks
                .Where(j => j.Reference == reference &&
                           (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                .ToListAsync(cancellationToken);

            if (!journalEntries.Any())
            {
                return;
            }

            foreach (var journal in journalEntries)
            {
                if (journal.Debit != 0 && Math.Abs(journal.Debit - costOfGoodsSold) > 0.01m) // Use small tolerance for decimal comparison
                {
                    journal.Debit = costOfGoodsSold;
                    journal.Credit = 0;
                }
                else if (journal.Credit != 0 && Math.Abs(journal.Credit - costOfGoodsSold) > 0.01m)
                {
                    journal.Credit = costOfGoodsSold;
                    journal.Debit = 0;
                }
            }

            _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
        }

        public async Task AddSalesToInventoryAsync(FilprideDeliveryReceipt deliveryReceipt, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
                .Where(i => i.Company == deliveryReceipt.Company &&
                            i.ProductId == deliveryReceipt.CustomerOrderSlip!.ProductId &&
                            i.POId == deliveryReceipt.PurchaseOrderId)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Particular == "Purchases" ? 0 : 1) // Purchases first, then Sales
                .ThenBy(i => i.Particular)
                .ToListAsync(cancellationToken);

            // Find the correct insertion point - after all purchases on the same date
            var lastIndex = -1;
            for (int i = 0; i < sortedInventory.Count; i++)
            {
                if (sortedInventory[i].Date > deliveryReceipt.DeliveredDate)
                    break;

                if (sortedInventory[i].Date == deliveryReceipt.DeliveredDate)
                {
                    // If same date, only consider it as "previous" if it's a purchase
                    if (sortedInventory[i].Particular == "Purchases")
                    {
                        lastIndex = i;
                    }
                    // Don't update lastIndex for sales on the same date
                }
                else if (sortedInventory[i].Date < deliveryReceipt.DeliveredDate)
                {
                    lastIndex = i;
                }
            }

            var previousInventory = lastIndex >= 0 ? sortedInventory[lastIndex] : null;
            var subsequentTransactions = sortedInventory.Skip(lastIndex + 1).ToList();
            decimal cost;

            if (previousInventory == null)
            {
                var purchaseOrder = await _db.FilpridePurchaseOrders
                    .FirstOrDefaultAsync(x => x.PurchaseOrderId == deliveryReceipt.PurchaseOrderId, cancellationToken)
                                    ?? throw new NullReferenceException("Purchase order not found");

                var unitOfWork = new UnitOfWork(_db);

                var poPrice = await unitOfWork.FilpridePurchaseOrder
                    .GetPurchaseOrderCost(purchaseOrder.PurchaseOrderId, cancellationToken);

                var netOfVat = purchaseOrder.VatType == SD.VatType_Vatable
                    ? ComputeNetOfVat(poPrice)
                    : poPrice;

                cost = netOfVat;
            }
            else
            {
                cost = previousInventory.AverageCost;
            }

            // Calculate initial values for new inventory entry
            var total = deliveryReceipt.Quantity * cost;
            var inventoryBalance = (previousInventory?.InventoryBalance ?? 0) - deliveryReceipt.Quantity;
            var totalBalance = (previousInventory?.TotalBalance ?? 0) - total;

            var averageCost = Math.Round(totalBalance, 4) == 0 || Math.Round(inventoryBalance, 4) == 0
                ? cost
                : totalBalance / inventoryBalance;

            // Create new inventory entry
            var inventory = new FilprideInventory
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                ProductId = deliveryReceipt.CustomerOrderSlip!.ProductId,
                Particular = "Sales",
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Quantity = deliveryReceipt.Quantity,
                Cost = cost,
                POId = deliveryReceipt.PurchaseOrderId,
                IsValidated = true,
                ValidatedBy = deliveryReceipt.CreatedBy,
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost,
                Company = deliveryReceipt.Company
            };

            // Update subsequent transactions
            var runningInventoryBalance = inventoryBalance;
            var runningTotalBalance = totalBalance;
            var runningAverageCost = averageCost;

            foreach (var transaction in subsequentTransactions)
            {
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = runningAverageCost;
                    transaction.Total = transaction.Quantity * runningAverageCost;
                    transaction.TotalBalance = runningTotalBalance - transaction.Total;
                    transaction.InventoryBalance = runningInventoryBalance - transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 ||
                                              Math.Round(transaction.InventoryBalance, 4) == 0
                        ? transaction.Cost
                        : transaction.TotalBalance / transaction.InventoryBalance;

                    var costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    // Update running totals
                    runningAverageCost = transaction.AverageCost;
                    runningTotalBalance = transaction.TotalBalance;
                    runningInventoryBalance = transaction.InventoryBalance;

                    // Update journal entries if needed
                    await UpdateJournalEntriesAsync(transaction.Reference!, costOfGoodsSold, cancellationToken);
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = runningTotalBalance + transaction.Total;
                    transaction.InventoryBalance = runningInventoryBalance + transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 ||
                                              Math.Round(transaction.InventoryBalance, 4) == 0
                        ? transaction.Cost
                        : transaction.TotalBalance / transaction.InventoryBalance;

                    // Update running totals
                    runningAverageCost = transaction.AverageCost;
                    runningTotalBalance = transaction.TotalBalance;
                    runningInventoryBalance = transaction.InventoryBalance;
                }
            }

            if (subsequentTransactions.Count != 0)
            {
                _db.FilprideInventories.UpdateRange(subsequentTransactions);
            }

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateJournalEntriesAsync(string reference, decimal costOfGoodsSold, CancellationToken cancellationToken)
        {
            var journalEntries = await _db.FilprideGeneralLedgerBooks
                .Where(j => j.Reference == reference &&
                            (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                .ToListAsync(cancellationToken);

            foreach (var journal in journalEntries)
            {
                if (journal.Debit != 0 && journal.Debit != costOfGoodsSold)
                {
                    journal.Debit = costOfGoodsSold;
                    journal.Credit = 0;
                }
                else if (journal.Credit != 0 && journal.Credit != costOfGoodsSold)
                {
                    journal.Credit = costOfGoodsSold;
                    journal.Debit = 0;
                }
            }

            if (journalEntries.Count != 0)
            {
                _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
            }
        }

        public async Task<bool> HasAlreadyBeginningInventory(int productId, int poId, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideInventories
                .AnyAsync(i => i.Company == company && i.ProductId == productId && i.POId == poId, cancellationToken);
        }

        public async Task VoidInventory(FilprideInventory model, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
            .Where(i => i.Company == model.Company && i.ProductId == model.ProductId && i.POId == model.POId && i.Date >= model.Date)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.InventoryId)
            .ToListAsync(cancellationToken);

            sortedInventory.Remove(model);

            if (sortedInventory.Count != 0)
            {
                var previousInventory = sortedInventory.FirstOrDefault();

                var inventoryBalance = previousInventory!.InventoryBalance;
                var totalBalance = previousInventory.TotalBalance;
                var averageCost = Math.Round(inventoryBalance, 4) == 0 || Math.Round(totalBalance, 4) == 0
                    ? previousInventory.AverageCost
                    : totalBalance / inventoryBalance;

                foreach (var transaction in sortedInventory.Skip(1))
                {
                    if (transaction.Particular == "Sales")
                    {
                        transaction.Cost = averageCost;
                        transaction.Total = transaction.Quantity * averageCost;
                        transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                        transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 || transaction.InventoryBalance == 0 ? previousInventory.AverageCost : transaction.TotalBalance / transaction.InventoryBalance;
                        var costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;

                        var journalEntries = await _db.FilprideGeneralLedgerBooks
                            .Where(j => j.Reference == transaction.Reference &&
                                        (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                            .ToListAsync(cancellationToken);

                        if (journalEntries.Count != 0)
                        {
                            foreach (var journal in journalEntries)
                            {
                                if (journal.Debit != 0)
                                {
                                    if (journal.Debit == costOfGoodsSold)
                                    {
                                        continue;
                                    }

                                    journal.Debit = costOfGoodsSold;
                                    journal.Credit = 0;
                                }
                                else
                                {
                                    if (journal.Credit == costOfGoodsSold)
                                    {
                                        continue;
                                    }

                                    journal.Credit = costOfGoodsSold;
                                    journal.Debit = 0;
                                }
                            }
                        }

                        _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                    }
                    else if (transaction.Particular == "Purchases")
                    {
                        transaction.TotalBalance = totalBalance + transaction.Total;
                        transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                        transaction.AverageCost = transaction.TotalBalance == 0 || transaction.InventoryBalance == 0
                            ? transaction.Cost
                            : transaction.TotalBalance / transaction.InventoryBalance;

                        averageCost = transaction.AverageCost;
                        totalBalance = transaction.TotalBalance;
                        inventoryBalance = transaction.InventoryBalance;
                    }
                }
            }

            _db.FilprideInventories.UpdateRange(sortedInventory);
            _db.FilprideInventories.Remove(model);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReCalculateInventoryAsync(List<FilprideInventory> inventories, CancellationToken cancellationToken = default)
        {
            var previousInventory = inventories.FirstOrDefault();

            var inventoryBalance = previousInventory!.InventoryBalance;
            var totalBalance = previousInventory.TotalBalance;
            var averageCost = Math.Round(inventoryBalance, 4) == 0 || Math.Round(totalBalance, 4) == 0
                ? previousInventory.AverageCost
                : totalBalance / inventoryBalance;

            foreach (var transaction in inventories.Skip(1))
            {
                if (transaction.Particular == "Sales")
                {
                    transaction.Cost = averageCost;
                    transaction.Total = transaction.Quantity * averageCost;
                    transaction.TotalBalance = totalBalance != 0 ? totalBalance - transaction.Total : transaction.Total;
                    transaction.InventoryBalance = inventoryBalance != 0 ? inventoryBalance - transaction.Quantity : transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 || Math.Round(transaction.InventoryBalance, 4) == 0
                        ? previousInventory.AverageCost
                        : transaction.TotalBalance / transaction.InventoryBalance;
                    var costOfGoodsSold = transaction.AverageCost * transaction.Quantity;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;

                    var journalEntries = await _db.FilprideGeneralLedgerBooks
                        .Where(j => j.Reference == transaction.Reference &&
                                    (j.AccountNo.StartsWith("50101") || j.AccountNo.StartsWith("10104")))
                        .ToListAsync(cancellationToken);

                    if (journalEntries.Count != 0)
                    {
                        foreach (var journal in journalEntries)
                        {
                            if (journal.Debit != 0)
                            {
                                if (journal.Debit == costOfGoodsSold)
                                {
                                    continue;
                                }

                                journal.Debit = costOfGoodsSold;
                                journal.Credit = 0;
                            }
                            else
                            {
                                if (journal.Credit == costOfGoodsSold)
                                {
                                    continue;
                                }

                                journal.Credit = costOfGoodsSold;
                                journal.Debit = 0;
                            }
                        }
                    }

                    _db.FilprideGeneralLedgerBooks.UpdateRange(journalEntries);
                }
                else if (transaction.Particular == "Purchases")
                {
                    transaction.TotalBalance = totalBalance + transaction.Total;
                    transaction.InventoryBalance = inventoryBalance + transaction.Quantity;
                    transaction.AverageCost = Math.Round(transaction.TotalBalance, 4) == 0 || Math.Round(transaction.InventoryBalance, 4) == 0
                        ? transaction.Cost
                        : transaction.TotalBalance / transaction.InventoryBalance;

                    averageCost = transaction.AverageCost;
                    totalBalance = transaction.TotalBalance;
                    inventoryBalance = transaction.InventoryBalance;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<FilprideInventory?> GetAsync(Expression<Func<FilprideInventory, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(i => i.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideInventory>> GetAllAsync(Expression<Func<FilprideInventory, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideInventory> query = dbSet
                .Include(i => i.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
