using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DTOs;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class PurchaseOrderRepository : Repository<FilpridePurchaseOrder>, IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .FilpridePurchaseOrders
                .AsNoTracking()
                .OrderByDescending(x => x.PurchaseOrderNo!.Length)
                .ThenByDescending(x => x.PurchaseOrderNo)
                .FirstOrDefaultAsync(x =>
                    
                    x.Type == nameof(DocumentType.Documented) &&
                    !x.PurchaseOrderNo!.Contains("POBEG"),
                    cancellationToken);

            if (lastPo == null)
            {
                return "PO0000000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .FilpridePurchaseOrders
                .AsNoTracking()
                .OrderByDescending(x => x.PurchaseOrderNo!.Length)
                .ThenByDescending(x => x.PurchaseOrderNo)
                .FirstOrDefaultAsync(x =>
                        
                        x.Type == nameof(DocumentType.Undocumented) &&
                        !x.PurchaseOrderNo!.Contains("POBEG"),
                    cancellationToken);

            if (lastPo == null)
            {
                return "POU000000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public override async Task<FilpridePurchaseOrder?> GetAsync(Expression<Func<FilpridePurchaseOrder, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .Include(p => p.PickUpPoint)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilpridePurchaseOrder>> GetAllAsync(Expression<Func<FilpridePurchaseOrder, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilpridePurchaseOrder> query = dbSet
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .Include(p => p.PickUpPoint);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override IQueryable<FilpridePurchaseOrder> GetAllQuery(Expression<Func<FilpridePurchaseOrder, bool>>? filter = null)
        {
            IQueryable<FilpridePurchaseOrder> query = dbSet
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .Include(p => p.PickUpPoint)
                .Include(po => po.ActualPrices)
                .AsSplitQuery()
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncByCode(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo,
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncById(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .Where(p => !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
                .OrderBy(p => p.PurchaseOrderNo)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncBySupplier(int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.SupplierId == supplierId && !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncBySupplierAndProduct(int supplierId, int productId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.SupplierId == supplierId && p.ProductId == productId && !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateCodeForSubPoAsync(string purchaseOrderNo, string company, CancellationToken cancellationToken = default)
        {
            var latestSubPoCode = await _db.FilpridePurchaseOrders
                .Where(po => po.IsSubPo && po.SubPoSeries!.Contains(purchaseOrderNo))
                .OrderByDescending(po => po.SubPoSeries)
                .Select(po => po.SubPoSeries)
                .FirstOrDefaultAsync(cancellationToken);

            var nextLetter = 'A';
            if (!string.IsNullOrEmpty(latestSubPoCode))
            {
                nextLetter = (char)(latestSubPoCode[^1] + 1);
            }

            return $"{purchaseOrderNo}{nextLetter}";
        }

        public async Task UpdateActualCostOnSalesAndReceiptsAsync(FilpridePOActualPrice model, CancellationToken cancellationToken = default)
        {
            // Early validation
            if (model.AppliedVolume >= model.TriggeredVolume)
            {
                return; // Nothing to process
            }

            // Single query to get all required data with optimized includes
            var receivingReports = await _db.FilprideReceivingReports
                .Include(rr => rr.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
                .Include(rr => rr.PurchaseOrder)
                    .ThenInclude(po => po!.Customer)
                .Include(rr => rr.DeliveryReceipt)
                    .ThenInclude(dr => dr!.CustomerOrderSlip)
                .Where(r => r.POId == model.PurchaseOrderId
                            && r.Status == nameof(Status.Posted))
                .OrderBy(r => r.ReceivingReportId)
                .ToListAsync(cancellationToken);

            if (!receivingReports.Any())
            {
                return; // No receiving reports to process
            }

            // Get inventories and purchase books in parallel
            var inventories = await _db.FilprideInventories
                .Where(i => i.POId == model.PurchaseOrderId)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Particular == "Purchases" ? 0 : 1)
                .ToListAsync(cancellationToken);

            // Create lookup dictionaries for better performance
            var inventoryLookup = inventories
                .ToLookup(inv => inv.Reference);

            var unitOfWork = new UnitOfWork(_db);
            var normalizedTriggeredPrice = DecimalRoundingHelper.RoundToFour(model.TriggeredPrice);
            var remainingVolume = model.TriggeredVolume - model.AppliedVolume;

            // Process receiving reports
            foreach (var receivingReport in receivingReports)
            {
                if (remainingVolume <= 0)
                {
                    break;
                }

                // Calculate effective volume
                var effectiveVolume = Math.Min(receivingReport.QuantityReceived, remainingVolume);
                var updatedAmount = DecimalRoundingHelper.ComputeAmountFromUnitPrice(effectiveVolume, normalizedTriggeredPrice);
                var difference = updatedAmount - receivingReport.Amount;
                var oldUnitCost = receivingReport.PurchaseOrder!.VatType == SD.VatType_Vatable
                    ? DecimalRoundingHelper.ComputeNetUnitValue(receivingReport.Amount, receivingReport.QuantityReceived)
                    : DecimalRoundingHelper.DivideOrZero(receivingReport.Amount, receivingReport.QuantityReceived);

                // Update receiving report
                receivingReport.Amount = updatedAmount;
                receivingReport.IsCostUpdated = true;
                model.AppliedVolume += effectiveVolume;
                remainingVolume -= effectiveVolume;

                // Update inventory
                var inventory = inventoryLookup[receivingReport.ReceivingReportNo]
                    .FirstOrDefault();

                if (inventory != null)
                {
                    inventory.VatType = receivingReport.PurchaseOrder.VatType;
                    inventory.Cost = DecimalRoundingHelper.DivideOrZero(updatedAmount, receivingReport.QuantityReceived);
                    inventory.Total = updatedAmount;
                    inventory.NetOfVatAmount = receivingReport.PurchaseOrder!.VatType == SD.VatType_Vatable
                        ? ComputeNetOfVat(updatedAmount)
                        : updatedAmount;

                    // Update first inventory's average cost and total balance
                    if (inventories.FirstOrDefault()?.InventoryId == inventory.InventoryId)
                    {
                        inventory.AverageCost = inventory.Cost;
                        var grossTotalBalance = DecimalRoundingHelper.ComputeAmountFromUnitPrice(inventory.InventoryBalance, inventory.AverageCost);
                        inventory.TotalBalance = receivingReport.PurchaseOrder.VatType == SD.VatType_Vatable
                            ? ComputeNetOfVat(grossTotalBalance)
                            : grossTotalBalance;
                    }
                }

                // Create GL entries for cost update
                await unitOfWork.FilprideReceivingReport.CreateEntriesForUpdatingCost(
                    receivingReport, difference, model.ApprovedBy!, cancellationToken);

                await unitOfWork.LockedPeriodAdjustment.AddIfPeriodPostedAsync(new LockedPeriodAdjustmentRequestDto
                {
                    Module = Module.ReceivingReport,
                    TransactionDate = receivingReport.Date,
                    EntityType = Module.ReceivingReport,
                    EntityNo = receivingReport.ReceivingReportNo!,
                    CustomerId = receivingReport.DeliveryReceipt?.CustomerId ?? receivingReport.PurchaseOrder?.CustomerId,
                    CustomerName = receivingReport.DeliveryReceipt?.CustomerOrderSlip?.CustomerName
                                   ?? receivingReport.PurchaseOrder?.Customer?.CustomerName,
                    SupplierId = receivingReport.PurchaseOrder?.SupplierId,
                    SupplierName = receivingReport.PurchaseOrder?.SupplierName,
                    AdjustmentType = LockedPeriodAdjustmentType.UnitCost,
                    OldValue = oldUnitCost,
                    NewValue = receivingReport.PurchaseOrder!.VatType == SD.VatType_Vatable
                        ? DecimalRoundingHelper.ComputeNetUnitValue(updatedAmount, effectiveVolume)
                        : normalizedTriggeredPrice,
                    AdjustmentValue = difference,
                    AffectedQuantity = effectiveVolume,
                    Reason = "Update approved unit cost in PO",
                    CreatedBy = model.ApprovedBy!
                }, cancellationToken);
            }

            // Recalculate inventory once at the end
            await unitOfWork.FilprideInventory.ReCalculateInventoryAsync(inventories, cancellationToken);

            // Single save operation
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<decimal> GetPurchaseOrderCost(int purchaseOrderId, CancellationToken cancellationToken = default)
        {
            var purchaseOrder = await _db.FilpridePurchaseOrders
                .Include(p => p.ActualPrices)
                .FirstOrDefaultAsync(x => x.PurchaseOrderId == purchaseOrderId, cancellationToken)
                                ?? throw new NullReferenceException("PurchaseOrder not found");

            var hasTriggeredPrice = purchaseOrder.ActualPrices?.Count > 0 && purchaseOrder.ActualPrices.Any(x => x.IsApproved);

            return DecimalRoundingHelper.RoundToFour(hasTriggeredPrice
                ? purchaseOrder.ActualPrices!
                    .OrderByDescending(x => x.ApprovedDate)
                    .First(x => x.IsApproved)
                    .TriggeredPrice
                : purchaseOrder.Price);
        }
    }
}
