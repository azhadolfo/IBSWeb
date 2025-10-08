using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Enums;
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

        public List<FilpridePurchaseBook> GetPurchaseBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<FilpridePurchaseBook, object> orderBy;

            switch (selectedFiltering)
            {
                case "RRDate":
                    orderBy = p => p.Date;
                    break;

                case "DueDate":
                    orderBy = p => p.DueDate;
                    break;

                case "POLiquidation":
                case "UnpostedRR":
                    orderBy = p => p.PurchaseBookId;
                    break;

                default:
                    orderBy = p => p.Date;
                    break;
            }

            return _db
                .FilpridePurchaseBooks
                .AsEnumerable()
                .Where(p => p.Company == company && (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) >= dateFrom &&
                            (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) <= dateTo)
                .OrderBy(orderBy)
                .ToList();
        }

        public List<FilprideCashReceiptBook> GetCashReceiptBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var cashReceiptBooks = _db
             .FilprideCashReceiptBooks
             .AsEnumerable()
             .Where(cr => cr.Company == company && cr.Date >= dateFrom && cr.Date <= dateTo)
             .OrderBy(s => s.CashReceiptBookId)
             .ToList();

            return cashReceiptBooks;
        }

        public List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _db
             .FilprideDisbursementBooks
             .AsEnumerable()
             .Where(d => d.Company == company && d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.Date)
             .ToList();

            return disbursementBooks;
        }

        public async Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var generalLedgerBooks = await _db
                .FilprideGeneralLedgerBooks
                .Where(i => i.Company == company && i.Date >= dateFrom && i.Date <= dateTo && i.IsPosted)
                .Include(i => i.BankAccount)
                .Include(i => i.Customer)
                .Include(i => i.Supplier)
                .Include(i => i.Account)
                .Include(i => i.Employee)
                .Include(i => i.CompanyModel)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Reference)
                .ThenByDescending(i => i.Debit)
                .ToListAsync(cancellationToken);

            return generalLedgerBooks;
        }

        public List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
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

        public List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _db
             .FilprideJournalBooks
             .AsEnumerable()
             .Where(d => d.Company == company && d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.JournalBookId)
             .ToList();

            return disbursementBooks;
        }

        public async Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
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

        public List<FilprideSalesBook> GetSalesBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument, string company)
        {
            Func<FilprideSalesBook, object>? orderBy = null;
            Func<FilprideSalesBook, bool>? query;

            switch (selectedDocument)
            {
                case null:
                case "TransactionDate":
                    query = s => s.Company == company && s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo;
                    break;

                case "DueDate":
                    orderBy = s => s.DueDate;
                    query = s => s.Company == company && s.DueDate >= dateFrom && s.DueDate <= dateTo;
                    break;

                default:
                    orderBy = s => s.TransactionDate;
                    query = s => s.Company == company && s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo && s.SerialNo.Contains(selectedDocument);
                    break;
            }

            // Add a null check for orderBy
            var salesBooks = _db
                .FilprideSalesBooks
                .AsEnumerable()
                .Where(query)
                .OrderBy(orderBy ?? (Func<FilprideSalesBook, object>)(s => s.TransactionDate))
                .ToList();

            return salesBooks;
        }

        public async Task<List<FilprideAuditTrail>> GetAuditTrails(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
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
                throw new ArgumentException("Date From must be greater than Date To !");
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

        public async Task<List<SalesReportViewModel>> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, string company, List<int>? commissioneeIds = null, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            // Fetch all delivery receipts within the date range
            var deliveryReceipts = await _db.FilprideDeliveryReceipts
                .Where(dr => dr.Company == company &&
                             dr.DeliveredDate >= dateFrom &&
                             dr.DeliveredDate <= dateTo &&
                             (dr.Status == nameof(DRStatus.ForInvoicing) || dr.Status == nameof(DRStatus.Invoiced))
                             && (commissioneeIds == null || commissioneeIds.Contains(dr.CommissioneeId!.Value)))
                .Include(dr => dr.CustomerOrderSlip!.Product)
                .Include(dr => dr.CustomerOrderSlip).ThenInclude(cos => cos!.Commissionee)
                .Include(dr => dr.Customer)
                .Include(dr => dr.PurchaseOrder)
                .OrderBy(dr => dr.DeliveredDate)
                .ThenBy(dr => dr.DeliveryReceiptNo)
                .ToListAsync(cancellationToken);

            // Fetch all sales invoices within the date range
            var salesInvoices = _db.FilprideSalesInvoices
                .Where(si => si.Company == company &&
                             si.TransactionDate >= dateFrom &&
                             si.TransactionDate <= dateTo &&
                             si.Status == nameof(Status.Posted))
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

        public async Task<List<FilprideSalesInvoice>> GetSalesInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string company,
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var salesInvoices = await _db.FilprideSalesInvoices
                .Where(si => si.TransactionDate >= dateFrom && si.TransactionDate <= dateTo &&
                             si.Status == nameof(Status.Posted))
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.PurchaseOrder)
                .OrderBy(si => si.TransactionDate)
                .ToListAsync(cancellationToken);

            return salesInvoices;
        }

        public async Task<List<FilprideServiceInvoice>> GetServiceInvoiceReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            // Fetch all delivery receipts within the date range
            var seviceInvoices = await _db.FilprideServiceInvoices
                .Where(dr => dr.Company == company &&
                             DateOnly.FromDateTime(dr.CreatedDate) >= dateFrom &&
                             DateOnly.FromDateTime(dr.CreatedDate) <= dateTo &&
                             dr.Status == nameof(Status.Posted))
                .Include(dr => dr.Customer)
                .Include(dr => dr.Service)
                .OrderBy(p => p.CreatedDate)
                .ToListAsync(cancellationToken);

            return seviceInvoices;
        }

        public async Task<List<FilpridePurchaseOrder>> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var purchaseOrder = await _db.FilpridePurchaseOrders
            .Where(p => p.Company == company && p.Date >= dateFrom && p.Date <= dateTo && p.Status == nameof(Status.Posted)) // Filter by date and company
            .Include(p => p.Supplier)
            .Include(p => p.Product)
            .Include(p => p.ActualPrices)
            .OrderBy(p => p.Date) // Order by TransactionDate
            .ToListAsync(cancellationToken);

            return purchaseOrder;
        }


        public async Task<List<FilprideCheckVoucherHeader>> GetClearedDisbursementReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var checkVoucherHeader = await _db.FilprideCheckVoucherHeaders
                .Where(cd =>
                    cd.Company == company && cd.DcrDate >= dateFrom && cd.DcrDate <= dateTo &&
                    cd.Status == nameof(Status.Posted) &&
                    cd.CvType != nameof(CVType.Invoicing))
                .Include(cd => cd.BankAccount)
                .OrderBy(cd => cd.Date)
                .ToListAsync(cancellationToken);

            return checkVoucherHeader;
        }

        public async Task<List<FilprideReceivingReport>> GetPurchaseReport(DateOnly dateFrom,
            DateOnly dateTo,
            string company,
            List<int>? customerIds = null,
            List<int>? commissioneeIds = null,
            string dateSelectionType = "RRDate",
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            // Base query without date filter yet
            var receivingReportsQuery = _db.FilprideReceivingReports
                .Where(rr => rr.Company == company
                            && rr.Status == nameof(Status.Posted)
                            && (customerIds == null || customerIds.Contains(rr.DeliveryReceipt!.CustomerId))
                            && (commissioneeIds == null || commissioneeIds.Contains(rr.DeliveryReceipt!.CommissioneeId!.Value)));

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

            // For the additional delivery receipts part, apply similar date filtering logic
            var additionalDeliveryReceiptsQuery = _db.FilprideDeliveryReceipts
                .Where(dr => dr.Date >= dateFrom && dr.Date <= dateTo
                          && dr.Status == nameof(DRStatus.PendingDelivery)
                          && (customerIds == null || customerIds.Contains(dr.CustomerId))
                          && (commissioneeIds == null || commissioneeIds.Contains(dr.CommissioneeId!.Value)));

            var additionalDeliveryReceipts = await additionalDeliveryReceiptsQuery
                .Include(dr => dr.CustomerOrderSlip)
                .Include(dr => dr.Customer)
                .Include(dr => dr.Hauler)
                .Include(dr => dr.PurchaseOrder).ThenInclude(po => po!.Product)
                .ToListAsync(cancellationToken);

            /// TODO Call this if needs to implement the in-transit purchases
            var allReports = receivingReports
                .Concat(additionalDeliveryReceipts.Select(dr => new FilprideReceivingReport
                {
                    DeliveryReceipt = dr,
                    Date = dr.Date,
                    Company = company,
                    PurchaseOrder = dr.PurchaseOrder,
                    QuantityReceived = dr.Quantity,
                    QuantityDelivered = dr.Quantity
                }))
                .ToList();

            return receivingReports.OrderBy(rr => rr.Date).ToList();
        }

        public async Task<List<FilprideDeliveryReceipt>> GetGrossMarginReport(DateOnly dateFrom,
            DateOnly dateTo,
            string company,
            CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            // Base query without date filter yet
            var deliveryReceiptsQuery = _db.FilprideDeliveryReceipts
                .Where(x => x.Company == company
                            && x.DeliveredDate >= dateFrom && x.DeliveredDate <= dateTo
                            && x.Status != nameof(Status.Canceled)
                            && x.Status != nameof(Status.Voided));

            // Include necessary related entities
            var deliveryReceipts = await deliveryReceiptsQuery
                .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Product)
                .Include(x => x.CustomerOrderSlip)
                        .ThenInclude(x => x!.PickUpPoint)
                .Include(x => x.CustomerOrderSlip)
                        .ThenInclude(x => x!.Commissionee)
                .Include(x => x.Customer)
                .Include(x => x.Hauler)
                .ToListAsync(cancellationToken);

            return deliveryReceipts.OrderBy(x => x.DeliveredDate)
                .ThenBy(x => x.DeliveryReceiptNo).ToList();
        }

        public async Task<List<FilprideCollectionReceipt>> GetCollectionReceiptReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var collectionReceipts = await _db.FilprideCollectionReceipts
                .Where(cr => cr.Company == company && cr.TransactionDate >= dateFrom && cr.TransactionDate <= dateTo)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(si => si!.CustomerOrderSlip)
                .Include(cr => cr.Customer)
                .Include(cr => cr.ServiceInvoice)
                .Include(cr => cr.BankAccount)
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
                throw new ArgumentException("Date From must be greater than Date To !");
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

        public async Task<List<FilpridePurchaseOrder>> GetApReport(DateOnly monthYear, string company, CancellationToken cancellationToken = default)
        {
            var purchaseOrders = await _db.FilpridePurchaseOrders
                .Include(po => po.ReceivingReports!
                    .Where(rr => rr.Status == nameof(Status.Posted)))
                .Include(po => po.Product)
                .Include(po => po.Supplier)
                .Include(po => po.PickUpPoint)
                .Where(po => (po.Status == nameof(Status.Posted) || po.Status == nameof(Status.Closed))
                             && (
                                 // POs created in the monthYear
                                 (po.Date.Year == monthYear.Year && po.Date.Month == monthYear.Month)
                                 // OR POs with at least one RR in monthYear
                                 || po.ReceivingReports!.Any(rr => rr.Status == nameof(Status.Posted)
                                                                   && rr.Date.Year == monthYear.Year
                                                                   && rr.Date.Month == monthYear.Month)
                             ))
                .OrderBy(po => po.Date)
                .ThenBy(po => po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            return purchaseOrders;
        }

        public async Task<List<FilprideSalesInvoice>> GetARPerCustomerReport(DateOnly dateFrom, DateOnly dateTo, string company, List<int>? customerIds = null, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            // Base query without date filter yet
            var salesInvoiceQuery = _db.FilprideSalesInvoices
                .Where(x => x.Company == company
                            && x.Status == nameof(Status.Posted)
                            && (customerIds == null || customerIds.Contains(x.CustomerId))
                            && x.TransactionDate >= dateFrom && x.TransactionDate <= dateTo);

            // Include necessary related entities
            var salesInvoices = await salesInvoiceQuery
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr!.Hauler)
                .Include(si => si.CustomerOrderSlip)
                .ToListAsync(cancellationToken);

            return salesInvoices.OrderBy(rr => rr.TransactionDate).ToList();
        }
    }
}
