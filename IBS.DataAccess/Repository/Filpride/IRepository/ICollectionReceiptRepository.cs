using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Integrated;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface ICollectionReceiptRepository : IRepository<FilprideCollectionReceipt>
    {
        Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default);

        Task UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task UndoSalesInvoiceChanges(FilprideCollectionReceiptDetail collectionReceiptDetail, CancellationToken cancellationToken);

        Task UndoServiceInvoiceChanges(FilprideCollectionReceiptDetail collectionReceiptDetail, CancellationToken cancellationToken);

        Task UpdateMultipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveSVPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task UpdateSV(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<List<FilprideOffsettings>> GetOffsettings(string source, string reference, string company, CancellationToken cancellationToken = default);

        Task PostAsync(FilprideCollectionReceipt collectionReceipt, List<FilprideOffsettings> offsettings, CancellationToken cancellationToken = default);

        Task DepositAsync(FilprideCollectionReceipt collectionReceipt, CancellationToken cancellationToken = default);

        Task ReturnedCheck(string crNo, string company, string userName, CancellationToken cancellationToken = default);

        Task RedepositAsync(FilprideCollectionReceipt collectionReceipt, CancellationToken cancellationToken = default);

        Task ApplyCostOfMoney(FilprideDeliveryReceipt deliveryReceipt, decimal costOfMoney, string currentUser, CancellationToken cancellationToken = default);
    }
}
