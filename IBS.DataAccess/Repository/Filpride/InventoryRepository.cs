using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
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

        public async Task AddPurchaseToInventoryAsync(FilprideReceivingReport receivingReport, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
                .Where(i => i.Company == receivingReport.Company &&
                            i.ProductId == receivingReport.PurchaseOrder!.Product!.ProductId &&
                            i.POId == receivingReport.POId)
                .ToListAsync(cancellationToken);

            sortedInventory = OrderInventoryTransactions(sortedInventory).ToList();

            var lastIndex = -1;
            for (int i = 0; i < sortedInventory.Count; i++)
            {
                if (sortedInventory[i].Date > receivingReport.Date)
                {
                    break;
                }

                if (sortedInventory[i].Date < receivingReport.Date || IsPurchase(sortedInventory[i]))
                {
                    lastIndex = i;
                }
            }

            var previousInventory = lastIndex >= 0 ? sortedInventory[lastIndex] : null;
            var subsequentTransactions = sortedInventory.Skip(lastIndex + 1).ToList();

            // Calculate initial values

            var total = ComputeVatAwareAmount(receivingReport.Amount, receivingReport.PurchaseOrder!.VatType);
            var cost = GetRoundedUnitValue(total, receivingReport.QuantityReceived);

            var inventoryBalance = (previousInventory?.InventoryBalance ?? 0) + receivingReport.QuantityReceived;
            var averageCost = cost;
            var totalBalance = inventoryBalance * averageCost;

            // Create new inventory entry
            var inventory = new FilprideInventory
            {
                Date = receivingReport.Date,
                ProductId = receivingReport.PurchaseOrder!.ProductId,
                POId = receivingReport.POId,
                Particular = "Purchases",
                Reference = receivingReport.ReceivingReportNo,
                Quantity = receivingReport.QuantityReceived,
                Cost = cost,
                IsValidated = true,
                ValidatedBy = receivingReport.CreatedBy, // Add this if available
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(), // Add this if available
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost,
                Company = receivingReport.Company
            };

            await RecalculateTransactionsAsync(inventory, subsequentTransactions, cancellationToken);

            // Batch updates for better performance
            if (subsequentTransactions.Count != 0)
            {
                _db.FilprideInventories.UpdateRange(subsequentTransactions);
            }

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
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
            if (deliveryReceipt.Details.Any())
            {
                foreach (var detail in deliveryReceipt.Details)
                {
                    await AddSalesLineToInventoryAsync(
                        deliveryReceipt,
                        detail.ProductId,
                        detail.PurchaseOrderId,
                        detail.Quantity,
                        detail.CustomerOrderSlip,
                        detail.PurchaseOrder,
                        cancellationToken);
                }

                return;
            }

            await AddSalesLineToInventoryAsync(
                deliveryReceipt,
                deliveryReceipt.CustomerOrderSlip!.ProductId,
                (int)deliveryReceipt.PurchaseOrderId!,
                deliveryReceipt.Quantity,
                deliveryReceipt.CustomerOrderSlip,
                deliveryReceipt.PurchaseOrder,
                cancellationToken);
        }

        private async Task AddSalesLineToInventoryAsync(
            FilprideDeliveryReceipt deliveryReceipt,
            int productId,
            int purchaseOrderId,
            decimal quantity,
            FilprideCustomerOrderSlip? customerOrderSlip,
            FilpridePurchaseOrder? purchaseOrder,
            CancellationToken cancellationToken)
        {
            var sortedInventory = await _db.FilprideInventories
                .Where(i => i.Company == deliveryReceipt.Company &&
                            i.ProductId == productId &&
                            i.POId == purchaseOrderId)
                .ToListAsync(cancellationToken);

            sortedInventory = OrderInventoryTransactions(sortedInventory).ToList();

            var lastIndex = -1;
            for (int i = 0; i < sortedInventory.Count; i++)
            {
                if (sortedInventory[i].Date > deliveryReceipt.DeliveredDate)
                {
                    break;
                }

                lastIndex = i;
            }

            var previousInventory = lastIndex >= 0 ? sortedInventory[lastIndex] : null;
            var subsequentTransactions = sortedInventory.Skip(lastIndex + 1).ToList();
            decimal cost;
            purchaseOrder ??= await _db.FilpridePurchaseOrders
                                 .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId, cancellationToken)
                             ?? throw new NullReferenceException("Purchase order not found");

            if (previousInventory == null)
            {
                var unitOfWork = new UnitOfWork(_db);

                var freight = customerOrderSlip?.DeliveryOption == SD.DeliveryOption_DirectDelivery
                    ? (decimal)customerOrderSlip.Freight!
                    : 0;

                var grossPoPrice = await unitOfWork.FilpridePurchaseOrder
                    .GetPurchaseOrderCost(purchaseOrder.PurchaseOrderId, cancellationToken) + freight;

                cost = ComputeVatAwareCost(RoundToFourDecimalPlaces(grossPoPrice), purchaseOrder.VatType);
            }
            else
            {
                cost = previousInventory.AverageCost;
            }

            // Calculate initial values for new inventory entry
            var inventoryBalance = (previousInventory?.InventoryBalance ?? 0) - quantity;
            var averageCost = cost;
            var total = quantity * cost;
            var totalBalance = inventoryBalance * averageCost;

            // Create new inventory entry
            var inventory = new FilprideInventory
            {
                Date = (DateOnly)deliveryReceipt.DeliveredDate!,
                ProductId = productId,
                Particular = "Sales",
                Reference = deliveryReceipt.DeliveryReceiptNo,
                Quantity = quantity,
                Cost = cost,
                POId = purchaseOrderId,
                IsValidated = true,
                ValidatedBy = deliveryReceipt.CreatedBy,
                ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                Total = total,
                InventoryBalance = inventoryBalance,
                TotalBalance = totalBalance,
                AverageCost = averageCost,
                Company = deliveryReceipt.Company
            };

            await RecalculateTransactionsAsync(inventory, subsequentTransactions, cancellationToken);

            if (subsequentTransactions.Count != 0)
            {
                _db.FilprideInventories.UpdateRange(subsequentTransactions);
            }

            await _db.FilprideInventories.AddAsync(inventory, cancellationToken);
        }

        public async Task VoidInventory(FilprideInventory model, CancellationToken cancellationToken = default)
        {
            var sortedInventory = await _db.FilprideInventories
            .Where(i => i.Company == model.Company
                        && i.ProductId == model.ProductId
                        && i.POId == model.POId)
            .ToListAsync(cancellationToken);

            sortedInventory = OrderInventoryTransactions(sortedInventory).ToList();
            var voidedIndex = sortedInventory.FindIndex(i => i.InventoryId == model.InventoryId);
            var previousInventory = voidedIndex > 0 ? sortedInventory[voidedIndex - 1] : null;
            var subsequentTransactions = voidedIndex >= 0
                ? sortedInventory.Skip(voidedIndex + 1).ToList()
                : [];

            if (subsequentTransactions.Count != 0)
            {
                await RecalculateTransactionsAsync(previousInventory, subsequentTransactions, cancellationToken);
                _db.FilprideInventories.UpdateRange(subsequentTransactions);
            }

            _db.FilprideInventories.Remove(model);

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReCalculateInventoryAsync(List<FilprideInventory> inventories, CancellationToken cancellationToken = default)
        {
            if (inventories.Count == 0)
            {
                return;
            }

            var orderedInventories = OrderInventoryTransactions(inventories).ToList();
            var previousInventory = orderedInventories.First();

            if (!IsPurchase(previousInventory))
            {
                previousInventory.Total = previousInventory.Quantity * previousInventory.Cost;
            }

            previousInventory.AverageCost = previousInventory.Cost;
            previousInventory.TotalBalance = previousInventory.InventoryBalance * previousInventory.AverageCost;

            await RecalculateTransactionsAsync(previousInventory, orderedInventories.Skip(1), cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task RecalculateTransactionsAsync(
            FilprideInventory? previousInventory,
            IEnumerable<FilprideInventory> transactions,
            CancellationToken cancellationToken)
        {
            var runningInventoryBalance = previousInventory?.InventoryBalance ?? 0m;
            var runningAverageCost = previousInventory?.AverageCost ?? 0m;
            var orderedTransactions = OrderInventoryTransactions(transactions).ToList();

            foreach (var transaction in orderedTransactions)
            {
                if (IsSales(transaction))
                {
                    transaction.Cost = runningAverageCost != 0
                        ? RoundToFourDecimalPlaces(runningAverageCost)
                        : RoundToFourDecimalPlaces(transaction.Cost);
                    transaction.Total = transaction.Quantity * transaction.Cost;
                    transaction.InventoryBalance = runningInventoryBalance - transaction.Quantity;
                    transaction.AverageCost = RoundToFourDecimalPlaces(transaction.Cost);
                    transaction.TotalBalance = transaction.InventoryBalance * transaction.AverageCost;
                }
                else if (IsPurchase(transaction))
                {
                    transaction.Cost = RoundToFourDecimalPlaces(transaction.Cost);
                    transaction.InventoryBalance = runningInventoryBalance + transaction.Quantity;
                    transaction.AverageCost = RoundToFourDecimalPlaces(transaction.Cost);
                    transaction.TotalBalance = transaction.InventoryBalance * transaction.AverageCost;
                }

                runningAverageCost = transaction.AverageCost;
                runningInventoryBalance = transaction.InventoryBalance;
            }

            await Task.CompletedTask;
        }

        private decimal ComputeVatAwareCost(decimal grossCost, string vatType)
        {
            return vatType == SD.VatType_Vatable
                ? ComputeNetOfVat(grossCost)
                : grossCost;
        }

        private decimal ComputeVatAwareAmount(decimal grossAmount, string vatType)
        {
            return vatType == SD.VatType_Vatable
                ? ComputeNetOfVat(grossAmount)
                : grossAmount;
        }

        private static IOrderedEnumerable<FilprideInventory> OrderInventoryTransactions(IEnumerable<FilprideInventory> inventories)
        {
            return inventories
                .OrderBy(i => i.Date)
                .ThenBy(i => IsPurchase(i) ? 0 : 1)
                .ThenBy(i => i.InventoryId);
        }

        private static bool IsPurchase(FilprideInventory inventory)
        {
            return inventory.Particular == "Purchases" || inventory.Particular == "Beginning Balance";
        }

        private static bool IsSales(FilprideInventory inventory)
        {
            return inventory.Particular == "Sales";
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
