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
            var receivingReports = await _db.FilprideReceivingReports
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Where(r => r.POId == model.PurchaseOrderId && r.Status == nameof(Status.Posted) && !r.IsCostUpdated)
                .OrderBy(r => r.ReceivingReportId)
                .ToListAsync(cancellationToken);

            var inventories = await _db.FilprideInventories
                .Where(i => i.POId == model.PurchaseOrderId)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Particular)
                .ToListAsync(cancellationToken);

            if (receivingReports.Count > 0)
            {
                for (var i = 0; i < receivingReports.Count; i++)
                {

                    var isSupplierVatable = receivingReports[i].PurchaseOrder!.VatType == SD.VatType_Vatable;
                    var isSupplierTaxable = receivingReports[i].PurchaseOrder!.TaxType == SD.TaxType_WithTax;

                    #region Update RR Amount

                    // Calculate the effective volume
                    var effectiveVolume = Math.Min(receivingReports[i].QuantityReceived, model.TriggeredVolume - model.AppliedVolume);

                    // Update the RR Amount based on the effective volume
                    receivingReports[i].Amount = effectiveVolume * model.TriggeredPrice;
                    receivingReports[i].IsCostUpdated = true;
                    model.AppliedVolume += effectiveVolume;

                    #endregion Update RR Amount

                    #region Update RR Inventory

                    var inventory = inventories.FirstOrDefault(inv => inv.Reference == receivingReports[i].ReceivingReportNo
                                                                      && inv.Company == receivingReports[i].Company);

                    inventory!.Cost = ComputeNetOfVat(model.TriggeredPrice);
                    inventory.Total = inventory.Quantity * inventory.Cost;

                    if (inventories[0].InventoryId == inventory.InventoryId)
                    {
                        inventory.AverageCost = inventory.Cost;
                        inventory.TotalBalance = inventory.Total;
                    }

                    #endregion Update RR Inventory

                    #region Update Purchase Book

                    var purchaseBook = await _db.FilpridePurchaseBooks
                        .FirstOrDefaultAsync(p => p.Company == receivingReports[i].Company && p.DocumentNo == receivingReports[i].ReceivingReportNo, cancellationToken);

                    purchaseBook!.Amount = receivingReports[i].Amount;
                    purchaseBook.NetPurchases = isSupplierVatable ? ComputeNetOfVat(receivingReports[i].Amount) : receivingReports[i].Amount;
                    purchaseBook.VatAmount = isSupplierVatable ? ComputeVatAmount(purchaseBook.NetPurchases) : purchaseBook.NetPurchases;
                    purchaseBook.WhtAmount = isSupplierTaxable ? ComputeEwtAmount(purchaseBook.NetPurchases, 0.01m) : 0;

                    #endregion Update Purchase Book

                    var isNibitForThisPeriodLocked = await _db.FilprideMonthlyNibits
                        .AnyAsync(x => x.Year == receivingReports[i].Date.Year
                                       && x.Month == receivingReports[i].Date.Year, cancellationToken);

                    if (!isNibitForThisPeriodLocked)
                    {
                        #region Update RR General Ledger

                        var index = i;
                        var journalEntries = await _db.FilprideGeneralLedgerBooks
                            .Where(g => g.Company == receivingReports[index].Company
                                        && g.Reference == receivingReports[index].ReceivingReportNo)
                            .OrderBy(g => g.GeneralLedgerBookId)
                            .ToListAsync(cancellationToken);

                        foreach (var journalEntry in journalEntries)
                        {
                            if (journalEntry.AccountNo.StartsWith("10104"))
                            {
                                journalEntry.Debit = purchaseBook.NetPurchases;
                            }
                            else if (journalEntry.AccountNo.StartsWith("1010602"))
                            {
                                journalEntry.Debit = purchaseBook.VatAmount;
                            }
                            else if (journalEntry.AccountNo.StartsWith("2010302"))
                            {
                                journalEntry.Credit = purchaseBook.WhtAmount;
                            }
                            else
                            {
                                journalEntry.Credit = purchaseBook.WhtAmount > 0
                                    ? ComputeNetOfEwt(purchaseBook.Amount, purchaseBook.WhtAmount)
                                    : purchaseBook.Amount;
                            }
                        }

                        if (!IsJournalEntriesBalanced(journalEntries))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        #endregion Update RR General Ledger
                    }

                    // Break the loop if TriggeredQuantity is met
                    if (model.AppliedVolume >= model.TriggeredVolume)
                    {
                        break;
                    }

                }

                #region ReCalculate Inventory

                var unitOfWork = new UnitOfWork(_db);

                await unitOfWork.FilprideInventory.ReCalculateInventoryAsync(inventories, cancellationToken);

                #endregion ReCalculate Inventory

                await _db.SaveChangesAsync(cancellationToken);
            }
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
