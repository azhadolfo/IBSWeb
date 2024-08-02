using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ReportRepository : Repository<FilprideGeneralLedgerBook>, IReportRepository
    {
        private ApplicationDbContext _db;

        public ReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public List<FilpridePurchaseBook> GetCashPurchaseBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering)
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
                .Where(p => (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) >= dateFrom &&
                            (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) <= dateTo)
                .OrderBy(orderBy)
                .ToList();
        }

        public List<FilprideCashReceiptBook> GetCashReceiptBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var cashReceiptBooks = _db
             .FilprideCashReceiptBooks
             .AsEnumerable()
             .Where(cr => cr.Date >= dateFrom && cr.Date <= dateTo)
             .OrderBy(s => s.CashReceiptBookId)
             .ToList();

            return cashReceiptBooks;
        }

        public List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _db
             .FilprideDisbursementBooks
             .AsEnumerable()
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.DisbursementBookId)
             .ToList();

            return disbursementBooks;
        }

        public async Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooksAsync(DateOnly dateFrom, DateOnly dateTo)
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

            var generalLedgerBooks = await GetAllAsync(i => i.Date >= dateFrom && i.Date <= dateTo && i.IsPosted);

            return (List<FilprideGeneralLedgerBook>)generalLedgerBooks;
        }

        public async Task<List<FilprideInventory>> GetInventoryBooksAsync(DateOnly dateFrom, DateOnly dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var inventoryRepo = new InventoryRepository(_db);

            var inventoryBooks = await inventoryRepo
                .GetAllAsync(i => i.Date >= dateFrom && i.Date <= dateTo);

            return (List<FilprideInventory>)inventoryBooks;
        }

        public List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _db
             .FilprideJournalBooks
             .AsEnumerable()
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.JournalBookId)
             .ToList();

            return disbursementBooks;
        }

        public async Task<List<ReceivingReport>> GetReceivingReportAsync(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var receivingReportRepo = new ReceivingReportRepo(_db);
            List<ReceivingReport> receivingReport = new List<ReceivingReport>();

            if (selectedFiltering == "UnpostedRR")
            {
                receivingReport = (List<ReceivingReport>)await receivingReportRepo
                    .GetAllAsync(rr => rr.Date >= dateFrom && rr.Date <= dateTo && rr.PostedBy == null);
            }
            else if (selectedFiltering == "POLiquidation")
            {
                receivingReport = (List<ReceivingReport>)await receivingReportRepo
                    .GetAllAsync(rr => rr.DueDate >= dateFrom && rr.DueDate <= dateTo && rr.PostedBy != null);
            }

            return receivingReport;
        }

        public Task<List<FilprideSalesBook>> GetSalesBooksAsync(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument)
        {
            throw new NotImplementedException();
        }
    }
}