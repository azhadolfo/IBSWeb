using IBS.DataAccess.Repository.IRepository;
using IBS.DTOs;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICollectionReceiptRepository : IRepository<FilprideCollectionReceipt>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        Task UpdateInvoice(int id, decimal paidAmount, CancellationToken cancellationToken = default);

        Task UndoSalesInvoiceChanges(FilprideCollectionReceiptDetail collectionReceiptDetail, CancellationToken cancellationToken);

        Task UndoServiceInvoiceChanges(FilprideCollectionReceiptDetail collectionReceiptDetail, CancellationToken cancellationToken);

        Task UpdateMultipleInvoice(string[] siNo, decimal[] paidAmount, CancellationToken cancellationToken = default);

        Task RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<List<FilprideOffsettings>> GetOffsettings(string source, string reference, string company, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideCollectionReceipt collectionReceipt, CancellationToken cancellationToken = default);

        Task ApplyClearingDateAsync(FilprideCollectionReceipt collectionReceipt, CancellationToken cancellationToken = default);

        Task ApplyCostOfMoney(FilprideDeliveryReceipt deliveryReceipt, decimal costOfMoney, string currentUser, DateOnly depositedDate, CancellationToken cancellationToken = default);

        Task BatchPostCollectionAsync(FilprideCollectionReceipt collectionReceipt, List<AccountTitleDto> accountTitlesDto, CancellationToken cancellationToken = default);

        Task BatchDepositAsync(FilprideCollectionReceipt collectionReceipt, Dictionary<string, FilprideChartOfAccount> accountTitlesDto,
            CancellationToken cancellationToken = default);
    }
}
