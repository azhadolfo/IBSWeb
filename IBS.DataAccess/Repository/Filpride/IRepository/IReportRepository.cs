using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;

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
    }
}