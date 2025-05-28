using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IPurchaseOrderRepository : IRepository<MobilityPurchaseOrder>
    {
        Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default);

        Task PostAsync(MobilityPurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);

        Task UpdateAsync(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken);
    }
}
