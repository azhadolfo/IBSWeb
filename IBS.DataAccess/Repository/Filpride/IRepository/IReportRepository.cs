using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReportRepository : IRepository<FilprideGeneralLedgerBook>
    {
        List<FilprideSalesBook> GetSalesBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument, string company);

        List<FilprideCashReceiptBook> GetCashReceiptBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        List<FilpridePurchaseBook> GetPurchaseBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering, string company);

        Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, string company, CancellationToken cancellationToken = default);

        List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default);

        List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        Task<List<FilprideAuditTrail>> GetAuditTrails(DateOnly dateFrom, DateOnly dateTo, string company);

        Task<List<FilprideCustomerOrderSlip>> GetCosUnservedVolume(DateOnly dateFrom, DateOnly dateTo, string company);

        public Task<List<SalesReportViewModel>> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default);

        public Task <List<FilpridePurchaseOrder>> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cacnellationToken = default);

        public Task<List<FilprideCheckVoucherHeader>> GetClearedDisbursementReport(DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default);

        public Task<List<FilprideReceivingReport>> GetPurchaseReport(DateOnly dateFrom, DateOnly dateTo, string company, List<int>? customerIds = null, string dateSelectionType = "RRDate", CancellationToken cancellationToken = default);

        public Task<List<FilprideCollectionReceipt>> GetCollectionReceiptReport (DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default);

        public Task<List<FilprideReceivingReport>> GetTradePayableReport (DateOnly dateFrom, DateOnly dateTo, string company, CancellationToken cancellationToken = default);
    }
}
