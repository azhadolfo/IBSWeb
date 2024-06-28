using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IDeliveryReceiptRepository : IRepository<FilprideDeliveryReceipt>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(DeliveryReceiptViewModel viewModel, string userName, CancellationToken cancellationToken = default);
    }
}