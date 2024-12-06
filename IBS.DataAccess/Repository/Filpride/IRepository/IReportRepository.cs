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

        Task<List<FilprideReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, string company);

        List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        List<FilprideGeneralLedgerBook> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo, string company);

        Task<List<FilprideAuditTrail>> GetAuditTrails(DateOnly dateFrom, DateOnly dateTo, string company);

        Task<List<FilprideCustomerOrderSlip>> GetCosUnserveVolume(DateOnly dateFrom, DateOnly dateTo, string company);

        public List<FilprideSalesInvoice> GetSalesReport(DateOnly dateFrom, DateOnly dateTo, string company);

        public List<FilpridePurchaseOrder> GetPurchaseOrderReport(DateOnly dateFrom, DateOnly dateTo, string company);

        public List<FilprideReceivingReport> GetPurchaseReport(DateOnly dateFrom, DateOnly dateTo, string company);
        public List<FilprideSalesInvoice> GetOtcFuelSalesReport(DateOnly dateFrom, DateOnly dateTo, string company, string productName);
        public List<FilprideSalesInvoice> GetAllOtcFuelSalesReport (DateOnly dateFrom, DateOnly dateTo, string company);
    }
}