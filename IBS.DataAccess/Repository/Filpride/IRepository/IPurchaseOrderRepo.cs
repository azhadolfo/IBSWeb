using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepo : IRepository<PurchaseOrder>
    {
        Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsync(CancellationToken cancellationToken = default);
    }
}