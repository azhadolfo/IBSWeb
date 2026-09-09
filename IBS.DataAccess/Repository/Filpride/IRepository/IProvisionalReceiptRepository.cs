using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IProvisionalReceiptRepository : IRepository<FilprideProvisionalReceipt>
    {
        Task<string> GenerateSeriesNumberAsync(string company, string type, CancellationToken cancellationToken = default);
        Task ApplyClearingDateAsync(FilprideProvisionalReceipt provisionalReceipt, CancellationToken cancellationToken = default);
    }
}
