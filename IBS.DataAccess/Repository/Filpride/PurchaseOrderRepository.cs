using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Constants;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class PurchaseOrderRepository : Repository<FilpridePurchaseOrder>, IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _db;

        public PurchaseOrderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<DateOnly> ComputeDueDateAsync(int poId, CancellationToken cancellationToken = default)
        {
            var po = await _db
                .FilpridePurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == poId, cancellationToken);

            if (po == null)
            {
                throw new ArgumentException("No record found.");
            }

            var poDate = DateOnly.FromDateTime(po.CreatedDate);

            DateOnly dueDate;

            switch (po.Terms)
            {
                case "7D":
                case "10D":
                    return poDate.AddDays(7);

                case "15D":
                    return poDate.AddDays(15);

                case "30D":
                    return poDate.AddDays(30);

                case "45D":
                case "45PDC":
                    return poDate.AddDays(45);

                case "60D":
                case "60PDC":
                    return poDate.AddDays(60);

                case "90D":
                    return poDate.AddDays(90);

                case "M15":
                    return poDate.AddMonths(1).AddDays(15 - poDate.Day);

                case "M30":
                    if (poDate.Month == 1)
                    {
                        dueDate = new DateOnly(poDate.Year, poDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(poDate.Year, poDate.Month, 1).AddMonths(2).AddDays(-1);

                        if (dueDate.Day == 31)
                        {
                            dueDate = dueDate.AddDays(-1);
                        }
                    }
                    return dueDate;

                case "M29":
                    if (poDate.Month == 1)
                    {
                        dueDate = new DateOnly(poDate.Year, poDate.Month, 1).AddMonths(2).AddDays(-1);
                    }
                    else
                    {
                        dueDate = new DateOnly(poDate.Year, poDate.Month, 1).AddMonths(2).AddDays(-1);

                        switch (dueDate.Day)
                        {
                            case 31:
                                dueDate = dueDate.AddDays(-2);
                                break;
                            case 30:
                                dueDate = dueDate.AddDays(-1);
                                break;
                        }
                    }
                    return dueDate;

                default:
                    return poDate;
            }
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .FilpridePurchaseOrders
                .Where(c => c.Company == company && !c.PurchaseOrderNo!.StartsWith("POBEG") && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo == null)
            {
                return "PO0000000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            var lastPo = await _db
                .FilpridePurchaseOrders
                .Where(c => c.Company == company && !c.PurchaseOrderNo!.StartsWith("POBEG") && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.PurchaseOrderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastPo == null)
            {
                return "POU000000001";
            }

            var lastSeries = lastPo.PurchaseOrderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

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

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsyncByCode(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == company && !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
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
                .Where(p => p.Company == company && !p.IsReceived && !p.IsSubPo && p.Status == nameof(Status.Posted))
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
                .Where(po => po.IsSubPo && po.Company == company && po.SubPoSeries!.Contains(purchaseOrderNo))
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
                .Include(r => r.DeliveryReceipt)
                .Where(r => r.POId == model.PurchaseOrderId
                            && r.Status == nameof(Status.Posted)
                            && !r.IsCostUpdated)
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

            var rrNumbers = receivingReports.Select(rr => rr.ReceivingReportNo).ToList();
            var companies = receivingReports.Select(rr => rr.Company).Distinct().ToList();

            var purchaseBooks = await _db.FilpridePurchaseBooks
                .Where(p => companies.Contains(p.Company) && rrNumbers.Contains(p.DocumentNo))
                .ToListAsync(cancellationToken);

            // Create lookup dictionaries for better performance
            var inventoryLookup = inventories
                .ToLookup(inv => new { inv.Reference, inv.Company });
            var purchaseBookLookup = purchaseBooks
                .ToDictionary(pb => new { pb.Company, pb.DocumentNo }, pb => pb);

            var unitOfWork = new UnitOfWork(_db);
            var netOfVatPrice = ComputeNetOfVat(model.TriggeredPrice);
            var remainingVolume = model.TriggeredVolume - model.AppliedVolume;

            // Process receiving reports
            foreach (var receivingReport in receivingReports)
            {
                if (remainingVolume <= 0)
                {
                    break;
                }

                var purchaseOrder = receivingReport.PurchaseOrder!;
                var isSupplierVatable = purchaseOrder.VatType == SD.VatType_Vatable;
                var isSupplierTaxable = purchaseOrder.TaxType == SD.TaxType_WithTax;

                // Calculate effective volume
                var effectiveVolume = Math.Min(receivingReport.QuantityReceived, remainingVolume);
                var updatedAmount = effectiveVolume * model.TriggeredPrice;
                var difference = updatedAmount - receivingReport.Amount;

                // Update receiving report
                receivingReport.Amount = updatedAmount;
                receivingReport.IsCostUpdated = true;
                model.AppliedVolume += effectiveVolume;
                remainingVolume -= effectiveVolume;

                // Update inventory
                var inventory = inventoryLookup[new { Reference = receivingReport.ReceivingReportNo, receivingReport.Company }]
                    .FirstOrDefault();

                if (inventory != null)
                {
                    inventory.Cost = netOfVatPrice;
                    inventory.Total = inventory.Quantity * inventory.Cost;

                    // Update first inventory's average cost and total balance
                    if (inventories.FirstOrDefault()?.InventoryId == inventory.InventoryId)
                    {
                        inventory.AverageCost = inventory.Cost;
                        inventory.TotalBalance = inventory.Total;
                    }
                }

                // Update purchase book
                var purchaseBookKey = new { receivingReport.Company, DocumentNo = receivingReport.ReceivingReportNo };
                if (purchaseBookLookup.TryGetValue(purchaseBookKey!, out var purchaseBook))
                {
                    purchaseBook.Amount = receivingReport.Amount;
                    purchaseBook.NetPurchases = isSupplierVatable
                        ? ComputeNetOfVat(receivingReport.Amount)
                        : receivingReport.Amount;
                    purchaseBook.VatAmount = isSupplierVatable
                        ? ComputeVatAmount(purchaseBook.NetPurchases)
                        : purchaseBook.NetPurchases;
                    purchaseBook.WhtAmount = isSupplierTaxable
                        ? ComputeEwtAmount(purchaseBook.NetPurchases, 0.01m)
                        : 0;
                }

                // Create GL entries for cost update
                await unitOfWork.FilprideReceivingReport.CreateEntriesForUpdatingCost(
                    receivingReport, difference, model.ApprovedBy!, cancellationToken);
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

            return purchaseOrder.ActualPrices!.Count != 0
                ? purchaseOrder.ActualPrices!.First().TriggeredPrice
                : purchaseOrder.Price;
        }
    }
}
