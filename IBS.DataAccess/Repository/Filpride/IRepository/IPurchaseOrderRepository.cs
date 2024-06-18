using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepository : IRepository<FilpridePurchaseOrder>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(PurchaseOrderViewModel viewModel, string userName, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetPurchaseOrderListAsync(CancellationToken cancellationToken = default);
    }
}