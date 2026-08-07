using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Enums;
using IBS.Models.Filpride.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReportRepository : Repository<FilprideGeneralLedgerBook>, IReportRepository
    {
        private readonly ApplicationDbContext _db;

        public ReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var generalLedgerBooks = await _db
                .FilprideGeneralLedgerBooks
                .IgnoreQueryFilters()
                .Where(i => i.Company == company && i.Date >= dateFrom && i.Date <= dateTo && i.IsPosted)
                .Include(i => i.Account)
                .OrderBy(i => i.ModuleType)
                .ThenBy(i => i.Date)
                .ThenBy(i => i.Reference)
                .ThenBy(i => i.GeneralLedgerBookId)
                .ToListAsync(cancellationToken);

            return generalLedgerBooks;
        }

        public List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var inventoryBooks = _db
             .FilprideInventories
             .Include(i => i.Product)
             .AsEnumerable()
             .Where(i => i.Company == company && i.Date >= dateFrom && i.Date <= dateTo)
             .OrderBy(i => i.InventoryId)
             .ToList();

            return inventoryBooks;
        }

        public async Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var receivingReportRepo = new ReceivingReportRepository(_db);
            var receivingReport = new List<FilprideReceivingReport>();

            switch (selectedFiltering)
            {
                case "UnpostedRR":
                    receivingReport = (List<FilprideReceivingReport>)await receivingReportRepo
                        .GetAllAsync(rr => rr.Company == company && rr.Date >= dateFrom && rr.Date <= dateTo && rr.PostedBy == null, cancellationToken);
                    break;
                case "POLiquidation":
                    receivingReport = (List<FilprideReceivingReport>)await receivingReportRepo
                        .GetAllAsync(rr => rr.Company == company && rr.DueDate >= dateFrom && rr.DueDate <= dateTo && rr.PostedBy != null, cancellationToken);
                    break;
            }

            return receivingReport;
        }

        public async Task<List<FilprideAuditTrail>> GetAuditTrails(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var auditTrailBooks = await _db
                .FilprideAuditTrails
                .Where(a => a.Company == company && DateOnly.FromDateTime(a.Date) >= dateFrom && DateOnly.FromDateTime(a.Date) <= dateTo)
                .OrderBy(a => a.Date)
                .ToListAsync();

            return auditTrailBooks;
        }

        public async Task<List<FilprideCustomerOrderSlip>> GetCosUnservedVolume(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            return await _db.FilprideCustomerOrderSlips
                .Include(a => a.Customer)
                .Include(a => a.Product)
                .Where(a => a.Company == company
                            && a.Date >= dateFrom
                            && a.Date <= dateTo
                            && a.Status != nameof(CosStatus.Closed)
                            && a.Status != nameof(CosStatus.Completed)
                            && a.Status != nameof(CosStatus.Disapproved))
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public async Task<List<SalesReportViewModel>> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, string company, List<int>? commissioneeIds = null, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var deliveryReceiptsQuery = _db.FilprideDeliveryReceipts
                .Where(dr => dr.Company == company &&
                             dr.DeliveredDate >= dateFrom &&
                             dr.DeliveredDate <= dateTo
                             && (commissioneeIds == null || commissioneeIds.Contains(dr.CommissioneeId!.Value)));

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                deliveryReceiptsQuery = deliveryReceiptsQuery.Where(dr => dr.Status == nameof(DRStatus.ForInvoicing) || dr.Status == nameof(DRStatus.Invoiced));
            }
            else if (statusFilter == "InvalidOnly")
            {
                deliveryReceiptsQuery = deliveryReceiptsQuery.Where(dr => dr.Status == nameof(DRStatus.Voided));
            }
            // "All" returns all status records

            var deliveryReceipts = await deliveryReceiptsQuery
                .Include(dr => dr.CustomerOrderSlip!.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.Customer)
                .Include(dr => dr.Details).ThenInclude(detail => detail.PurchaseOrder)
                .Include(dr => dr.PurchaseOrder)
                .OrderBy(dr => dr.DeliveredDate)
                .ThenBy(dr => dr.DeliveryReceiptNo)
                .ToListAsync(cancellationToken);

            // Fetch all sales invoices within the date range
            var salesInvoicesQuery = _db.FilprideSalesInvoices
                .Where(si => si.Company == company
                             && si.Status == nameof(Status.Posted)
                             && si.TransactionDate >= dateFrom
                             && si.TransactionDate <= dateTo);

            var salesInvoices = salesInvoicesQuery
                .Include(si => si.DeliveryReceipt)
                .ToList();

            // Create a result list to hold the combined data
            var result = new List<SalesReportViewModel>();

            // Iterate through each delivery receipt
            foreach (var dr in deliveryReceipts)
            {
                // Find the related sales invoice (if it exists)
                var relatedSalesInvoice = salesInvoices.FirstOrDefault(si => si.DeliveryReceiptId == dr.DeliveryReceiptId);

                // Add a new SalesReportViewModel to the result list
                result.Add(new SalesReportViewModel
                {
                    DeliveryReceipt = dr,
                    SalesInvoice = relatedSalesInvoice // This will be null if no related sales invoice exists
                });
            }

            return result;
        }

        public async Task<List<FilprideSalesInvoice>> GetSalesInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string company, string statusFilter = "ValidOnly",
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilprideSalesInvoices
                .Where(si => si.TransactionDate >= dateFrom && si.TransactionDate <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                query = query.Where(si => si.Status == nameof(Status.Posted));
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(si => si.Status == nameof(Status.Voided));
            }
            // "All" returns Posted, Voided, and Canceled records

            var salesInvoices = await query
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.PurchaseOrder)
                .Include(si => si.CustomerOrderSlip)
                .Include(si => si.DeliveryReceipt)
                    .ThenInclude(dr => dr!.CustomerOrderSlip)
                .Include(si => si.DeliveryReceipt)
                    .ThenInclude(dr => dr!.Details)
                    .ThenInclude(detail => detail.PurchaseOrder)
                .OrderBy(si => si.TransactionDate)
                .ToListAsync(cancellationToken);

            return salesInvoices;
        }

        public async Task<List<FilprideServiceInvoice>> GetServiceInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string company, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilprideServiceInvoices
                .Where(dr => dr.Company == company &&
                             dr.Period >= dateFrom &&
                             dr.Period <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                query = query.Where(dr => dr.Status == nameof(Status.Posted));
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(dr => dr.Status == nameof(Status.Voided));
            }
            // "All" returns Posted, Voided, and Canceled records

            var serviceInvoices = await query
                .Include(dr => dr.Customer)
                .Include(dr => dr.Service)
                .OrderBy(p => p.ServiceInvoiceNo)
                .ToListAsync(cancellationToken);

            return serviceInvoices;
        }

        public async Task<List<FilpridePurchaseOrder>> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string company, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilpridePurchaseOrders
                .Where(p => p.Company == company && p.Date >= dateFrom && p.Date <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                query = query.Where(p => p.Status == nameof(Status.Posted));
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(p => p.Status == nameof(Status.Voided));
            }
            // "All" returns Posted, Voided, and Canceled records

            var purchaseOrder = await query
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .Include(p => p.ActualPrices)
                .OrderBy(p => p.Date)
                .ToListAsync(cancellationToken);

            return purchaseOrder;
        }


        public async Task<List<FilprideCheckVoucherHeader>> GetClearedDisbursementReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var checkVoucherHeader = await _db.FilprideCheckVoucherHeaders
                .AsNoTracking()
                .Where(cd =>
                    cd.Company == company && cd.DcrDate >= dateFrom && cd.DcrDate <= dateTo &&
                    cd.Status == nameof(Status.Posted) &&
                    cd.CvType != nameof(CVType.Invoicing))
                .Include(cd => cd.BankAccount)
                .Include(cd => cd.Details)
                .OrderBy(cd => cd.Date)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            return checkVoucherHeader;
        }

        public async Task<List<FilprideReceivingReport>> GetPurchaseReport(DateOnly dateFrom,
            DateOnly dateTo,
            string company,
            List<int>? customerIds = null,
            List<int>? commissioneeIds = null,
            string dateSelectionType = "RRDate",
            string statusFilter = "ValidOnly",
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            // Base query without date filter yet
            var receivingReportsQuery = _db.FilprideReceivingReports
                .Where(rr => rr.Company == company
                            && (customerIds == null || customerIds.Contains(rr.DeliveryReceipt!.CustomerId))
                            && (commissioneeIds == null || commissioneeIds.Contains(rr.DeliveryReceipt!.CommissioneeId!.Value)));

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                receivingReportsQuery = receivingReportsQuery.Where(rr => rr.Status == nameof(Status.Posted));
            }
            else if (statusFilter == "InvalidOnly")
            {
                receivingReportsQuery = receivingReportsQuery.Where(rr => rr.Status == nameof(Status.Voided));
            }
            // "All" returns Posted, Voided, and Canceled records

            // Apply date filter based on dateSelectionType
            if (dateSelectionType == "RRDate")
            {
                // Filter by Receiving Report Date (original behavior)
                receivingReportsQuery = receivingReportsQuery
                    .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo);
            }
            else if (dateSelectionType == "SupplierSIDate")
            {
                // Filter by Supplier Invoice/SI Date instead
                receivingReportsQuery = receivingReportsQuery
                    .Where(rr => rr.SupplierInvoiceDate >= dateFrom
                              && rr.SupplierInvoiceDate <= dateTo);
            }

            // Include necessary related entities
            var receivingReports = await receivingReportsQuery
                .Include(rr => rr.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
                .Include(rr => rr.PurchaseOrder)
                    .ThenInclude(po => po!.Product)
                .Include(rr => rr.DeliveryReceipt)
                    .ThenInclude(dr => dr!.CustomerOrderSlip)
                        .ThenInclude(cos => cos!.PickUpPoint)
                .Include(rr => rr.DeliveryReceipt)
                    .ThenInclude(dr => dr!.CustomerOrderSlip)
                        .ThenInclude(cos => cos!.Commissionee)
                .Include(rr => rr.DeliveryReceipt)
                    .ThenInclude(dr => dr!.Customer)
                .Include(rr => rr.DeliveryReceipt)
                    .ThenInclude(dr => dr!.Hauler)
                .ToListAsync(cancellationToken);

            return receivingReports
                .OrderBy(rr => rr.Date)
                .ThenBy(rr => rr.ReceivingReportNo)
                .ThenBy(rr => rr.PONo)
                .ToList();
        }

        public async Task<List<FilprideDeliveryReceipt>> GetGrossMarginReport(
            DateOnly dateFrom,
            DateOnly dateTo,
            string company,
            List<int>? customers = null,
            List<int>? commissionee = null,
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            // Base query without date filter yet
            var deliveryReceiptsQuery = _db.FilprideDeliveryReceipts
                .Where(x => x.Company == company
                            && x.DeliveredDate >= dateFrom && x.DeliveredDate <= dateTo
                            && x.Status != nameof(Status.Canceled)
                            && x.Status != nameof(Status.Voided));

            // Apply customer filter if provided
            if (customers != null && customers.Count > 0)
            {
                deliveryReceiptsQuery = deliveryReceiptsQuery
                    .Where(x => customers.Contains(x.CustomerId));
            }

            if (commissionee != null && commissionee.Count > 0)
            {
                deliveryReceiptsQuery = deliveryReceiptsQuery
                    .Where(x => x.CustomerOrderSlip != null
                            && x.CustomerOrderSlip.CommissioneeId.HasValue
                            && commissionee.Contains(x.CustomerOrderSlip.CommissioneeId.Value));
            }
            var deliveryReceipts = await deliveryReceiptsQuery
                .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Product)
                .Include(x => x.Details)
                    .ThenInclude(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                .Include(x => x.Details)
                    .ThenInclude(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Product)
                .Include(x => x.CustomerOrderSlip)
                    .ThenInclude(x => x!.PickUpPoint)
                .Include(x => x.CustomerOrderSlip)
                    .ThenInclude(x => x!.Commissionee)
                .Include(x => x.Customer)
                .Include(x => x.Hauler)
                .ToListAsync(cancellationToken);

            return deliveryReceipts
                .OrderBy(x => x.DeliveredDate)
                .ThenBy(x => x.DeliveryReceiptNo)
                .ToList();
        }

        public async Task<List<FilprideCollectionReceipt>> GetCollectionReceiptReport(DateOnly dateFrom, DateOnly dateTo, string company, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilprideCollectionReceipts
                .AsNoTracking()
                .Where(cr => cr.Company == company && cr.TransactionDate >= dateFrom && cr.TransactionDate <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                query = query.Where(cr => cr.PostedBy != null);
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(cr => cr.VoidedBy != null);
            }
            // "All" returns all records

            var collectionReceipts = await query
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(si => si!.CustomerOrderSlip)
                .Include(cr => cr.Customer)
                .Include(cr => cr.ServiceInvoice)
                .Include(cr => cr.BankAccount)
                .AsSplitQuery()
                .OrderBy(cr => cr.Customer!.CustomerCode)
                .ThenBy(cr => cr.Customer!.CustomerName)
                .ThenBy(cr => cr.Customer!.CustomerType)
                .ToListAsync(cancellationToken);

            return collectionReceipts;
        }

        public async Task<List<FilprideReceivingReport>> GetTradePayableReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var receivingReports = await _db.FilprideReceivingReports
                .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Where(rr => rr.Company == company && rr.Date <= dateTo)
                .OrderBy(rr => rr.Date.Year)
                .ThenBy(rr => rr.Date.Month)
                .ThenBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName)
                .ToListAsync(cancellationToken);

            return receivingReports;
        }

        public async Task<List<FilprideDeliveryReceipt>> GetHaulerPayableReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var deliveryReceipts = await _db.FilprideDeliveryReceipts
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(dr => dr.Hauler)
                .Where(dr => dr.Company == company && dr.DeliveredDate <= dateTo && dr.DeliveredDate != null && dr.HaulerId != null && dr.FreightAmount > 0m)
                .OrderBy(dr => dr.DeliveredDate!.Value.Year)
                .ThenBy(dr => dr.DeliveredDate!.Value.Month)
                .ThenBy(dr => dr.Hauler!.SupplierName)
                .ToListAsync(cancellationToken);

            return deliveryReceipts;
        }

        public async Task<List<FilpridePurchaseOrder>> GetApReport(DateOnly monthYear, string company, CancellationToken cancellationToken = default)
        {
            var periodStart = new DateOnly(monthYear.Year, monthYear.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            var purchaseOrders = await _db.FilpridePurchaseOrders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(po => po.ReceivingReports!
                    .Where(rr => rr.Status == nameof(Status.Posted)))
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .Include(po => po.PickUpPoint)
                .Where(po => po.Company == company && !po.IsSubPo)
                .Where(po => po.Date <= periodEnd)
                .Where(po => (po.Status == nameof(Status.Posted) || po.Status == nameof(Status.Closed) && po.QuantityReceived > 0)
                             && (
                                 // POs created in the monthYear
                                 (po.Date.Year == monthYear.Year && po.Date.Month == monthYear.Month)
                                 // OR POs with at least one RR in monthYear
                                 || po.ReceivingReports!.Any(rr => rr.Status == nameof(Status.Posted)
                                                                   && rr.Date.Year == monthYear.Year
                                                                   && rr.Date.Month == monthYear.Month)
                                 // OR open POs with outstanding balance as of the selected month
                                 || (!po.IsClosed
                                     && po.Date < periodStart
                                     && po.Quantity > po.ReceivingReports!
                                         .Where(rr => rr.Status == nameof(Status.Posted) && rr.Date <= periodEnd)
                                         .Sum(rr => rr.QuantityReceived))
                             ))
                .OrderBy(po => po.Date)
                .ThenBy(po => po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            return purchaseOrders;
        }

        public async Task<List<FilprideSalesInvoice>> GetARPerCustomerReport(DateOnly dateFrom, DateOnly dateTo, string company, List<int>? customerIds = null, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var salesInvoiceQuery = _db.FilprideSalesInvoices
                .Where(x => x.Company == company
                            && (customerIds == null || customerIds.Contains(x.CustomerId))
                            && x.TransactionDate >= dateFrom && x.TransactionDate <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                salesInvoiceQuery = salesInvoiceQuery.Where(x => x.Status == nameof(Status.Posted));
            }
            else if (statusFilter == "InvalidOnly")
            {
                salesInvoiceQuery = salesInvoiceQuery.Where(x => x.Status == nameof(Status.Voided));
            }
            // "All" returns Posted, Voided, and Canceled records

            // Include necessary related entities
            var salesInvoices = await salesInvoiceQuery
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr!.Hauler)
                .Include(si => si.CustomerOrderSlip)
                .ToListAsync(cancellationToken);

            return salesInvoices.OrderBy(rr => rr.TransactionDate).ToList();
        }


        public async Task<List<FilprideJournalVoucherDetail>> GetJournalVoucherReport(DateOnly dateFrom,
            DateOnly dateTo, string company, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilprideJournalVoucherDetails
                .Include(jvd => jvd.JournalVoucherHeader)
                .ThenInclude(jvh => jvh!.CheckVoucherHeader)
                .Where(x => x.JournalVoucherHeader!.Company == company
                            && x.JournalVoucherHeader.Date >= dateFrom
                            && x.JournalVoucherHeader.Date <= dateTo);

            // Apply status filter
            if (statusFilter == "ValidOnly")
            {
                query = query.Where(x => x.JournalVoucherHeader!.PostedBy != null);
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(x => x.JournalVoucherHeader!.VoidedBy != null);
            }
            // "All" returns all records without filtering

            var journalVoucherDetails = await query
                .OrderBy(jvd => jvd.JournalVoucherHeader!.Date)
                .ThenBy(jvd => jvd.JournalVoucherHeader!.JournalVoucherHeaderNo)
                .ToListAsync(cancellationToken);


            return journalVoucherDetails;
        }

        public async Task<List<FilprideCustomerOrderSlip>> GetCustomerOrderSlipReport(DateOnly dateFrom, DateOnly dateTo, string company, string statusFilter = "ValidOnly", CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must not be greater than Date To!");
            }

            var query = _db.FilprideCustomerOrderSlips
                .Where(dr => dr.Company == company &&
                             dr.Date >= dateFrom &&
                             dr.Date <= dateTo);

            if (statusFilter == "ValidOnly")
            {
                query = query.Where(cos =>
                    cos.Status != nameof(CosStatus.Closed) &&
                    cos.Status != nameof(CosStatus.Disapproved) &&
                    cos.Status != nameof(CosStatus.Expired));
            }
            else if (statusFilter == "InvalidOnly")
            {
                query = query.Where(cos =>
                    cos.Status == nameof(CosStatus.Closed) ||
                    cos.Status == nameof(CosStatus.Disapproved) ||
                    cos.Status == nameof(CosStatus.Expired));
            }

            var customerOrderSlip = await query
                .Include(cos => cos.Customer)
                .Include(cos => cos.Hauler)
                .Include(cos => cos.Product)
                .Include(cos => cos.Supplier)
                .Include(cos => cos.PickUpPoint)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Product)
                .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                .Include(cos => cos.AppointedSuppliers)
                .OrderBy(p => p.CustomerOrderSlipNo)
                .ToListAsync(cancellationToken);

            return customerOrderSlip;
        }
    }
}
