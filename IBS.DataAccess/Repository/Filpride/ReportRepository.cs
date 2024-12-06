using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReportRepository : Repository<FilprideGeneralLedgerBook>, IReportRepository
    {
        private ApplicationDbContext _db;

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
             .OrderBy(d => d.DisbursementBookId)
             .ToList();

            return disbursementBooks;
        }

        public List<FilprideGeneralLedgerBook> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<FilprideGeneralLedgerBook, object> orderBy;

            if (dateFrom != null && dateTo != null)
            {
                orderBy = i => i.Date;
            }
            else
            {
                orderBy = i => i.Date; // Default ordering function
            }

            var generalLedgerBooks = _db
                .FilprideGeneralLedgerBooks
                .AsEnumerable()
                .Where(i => i.Company == company && i.Date >= dateFrom && i.Date <= dateTo && i.IsPosted)
                .OrderBy(orderBy)
                .ToList();

            return generalLedgerBooks;
        }

        public List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

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

        public async Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var receivingReportRepo = new ReceivingReportRepository(_db);
            List<FilprideReceivingReport> receivingReport = new List<FilprideReceivingReport>();

            if (selectedFiltering == "UnpostedRR")
            {
                receivingReport = (List<FilprideReceivingReport>)await receivingReportRepo
                    .GetAllAsync(rr => rr.Company == company && rr.Date >= dateFrom && rr.Date <= dateTo && rr.PostedBy == null);
            }
            else if (selectedFiltering == "POLiquidation")
            {
                receivingReport = (List<FilprideReceivingReport>)await receivingReportRepo
                    .GetAllAsync(rr => rr.Company == company && rr.DueDate >= dateFrom && rr.DueDate <= dateTo && rr.PostedBy != null);
            }

            return receivingReport;
        }

        public List<FilprideSalesBook> GetSalesBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument, string company)
        {
            Func<FilprideSalesBook, object> orderBy = null;
            Func<FilprideSalesBook, bool> query = null;

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

        public async Task<List<FilprideCustomerOrderSlip>> GetCosUnserveVolume(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            return await _db.FilprideCustomerOrderSlips
                .Include(a => a.Customer)
                .Include(a => a.Product)
                .Where(a => a.Company == company && a.Date >= dateFrom && a.Date <= dateTo && a.Status == nameof(CosStatus.Approved) && a.DeliveredQuantity < a.Quantity)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public List<FilprideSalesInvoice> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var salesInvoice = _db.FilprideSalesInvoices
            .Where(s => s.Company == company && s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo) // Filter by date and company
            .Include (s => s.Product)
            .Include (s => s.Customer)
            .Include (s => s.CustomerOrderSlip)
            .Include (s => s.DeliveryReceipt)
            .Include (s => s.PurchaseOrder)
            .OrderBy(s => s.TransactionDate) // Order by TransactionDate
            .ToList();

            return salesInvoice;
        }

        public List<FilpridePurchaseOrder> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var purchaseOrder = _db.FilpridePurchaseOrders
            .Where(p => p.Company == company && p.Date >= dateFrom && p.Date <= dateTo) // Filter by date and company
            .Include(p => p.Supplier)
            .Include(p => p.Product)
            .OrderBy(p => p.Date) // Order by TransactionDate
            .ToList();

            return purchaseOrder;
        }
        
        public List<FilprideReceivingReport> GetPurchaseReport (DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var receivingReports = _db.FilprideReceivingReports
                .Where(rr => rr.Company == company && rr.Date >= dateFrom && rr.Date <= dateTo) // Filter by date and company
                .Include (rr => rr.PurchaseOrder).ThenInclude(po => po.Supplier)
                .Include (rr => rr.PurchaseOrder).ThenInclude(po => po.Product)
                .Include (rr => rr.DeliveryReceipt).ThenInclude(dr => dr.CustomerOrderSlip)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Customer)
                .Include(rr => rr.DeliveryReceipt).ThenInclude(dr => dr.Hauler)
                .OrderBy(rr => rr.Date) // Order by TransactionDate
                .ToList();
            
            return receivingReports;
        }
        
        public List<FilprideSalesInvoice> GetOtcFuelSalesReport (DateOnly dateFrom, DateOnly dateTo, string company, string productName)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }
        
            var receivingReports = _db.FilprideSalesInvoices
                .Where(si => si.Company == company && si.TransactionDate >= dateFrom && si.TransactionDate <= dateTo && si.Product.ProductName == productName) // Filter by date and company
                .Include(si => si.Customer)
                .Include(si => si.CustomerOrderSlip)
                .Include (si => si.DeliveryReceipt)
                .Include(si => si.Product)
                .OrderBy(si => si.Customer.CustomerName).ThenBy(si => si.TransactionDate)
                .ThenBy(si => si.Customer.CustomerType) // Order by TransactionDate
                .ToList();
            
            return receivingReports;
        }
        
        public List<FilprideSalesInvoice> GetAllOtcFuelSalesReport (DateOnly dateFrom, DateOnly dateTo, string company)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }
        
            var receivingReports = _db.FilprideSalesInvoices
                .Where(si => si.Company == company && si.TransactionDate >= dateFrom && si.TransactionDate <= dateTo) // Filter by date and company
                .Include(si => si.Customer)
                .Include(si => si.CustomerOrderSlip)
                .Include (si => si.DeliveryReceipt)
                .Include(si => si.Product)
                .OrderBy(si => si.Product.ProductName).ThenBy(si => si.Customer.CustomerName).ThenBy((si => si.TransactionDate))
                .ThenBy(si => si.Customer.CustomerType) // Order by TransactionDate
                .ToList();
            
            return receivingReports;
        }
    }
}