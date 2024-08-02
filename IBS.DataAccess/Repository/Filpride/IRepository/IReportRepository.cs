using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReportRepository : IRepository<FilprideGeneralLedgerBook>
    {
        List<FilprideSalesBook> GetSalesBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument);

        List<FilprideCashReceiptBook> GetCashReceiptBooks(DateOnly dateFrom, DateOnly dateTo);

        List<FilpridePurchaseBook> GetPurchaseBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering);

        Task<List<ReceivingReport>> GetReceivingReportAsync(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering);

        List<FilprideInventory> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo);

        List<FilprideGeneralLedgerBook> GetGeneralLedgerBooks(DateOnly dateFrom, DateOnly dateTo);

        List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo);

        List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo);
    }
}