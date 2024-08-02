using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IReportRepository : IRepository<FilprideGeneralLedgerBook>
    {
        Task<List<FilprideSalesBook>> GetSalesBooksAsync(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument);

        List<FilprideCashReceiptBook> GetCashReceiptBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument);

        List<FilpridePurchaseBook> GetCashPurchaseBooks(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering);

        Task<List<ReceivingReport>> GetReceivingReportAsync(DateOnly dateFrom, DateOnly dateTo, string? selectedFiltering);

        Task<List<FilprideInventory>> GetInventoryBooksAsync(DateOnly dateFrom, DateOnly dateTo);

        Task<List<FilprideGeneralLedgerBook>> GetGeneralLedgerBooksAsync(DateOnly dateFrom, DateOnly dateTo);

        List<FilprideDisbursementBook> GetDisbursementBooks(DateOnly dateFrom, DateOnly dateTo);

        List<FilprideJournalBook> GetJournalBooks(DateOnly dateFrom, DateOnly dateTo);
    }
}