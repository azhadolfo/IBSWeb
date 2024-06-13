using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.ViewModels;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepository : IRepository<FilpridePurchaseOrder>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task Update(PurchaseOrderViewModel model, CancellationToken cancellationToken);
    }
}