using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPurchaseOrderRepo : IRepository<FilpridePurchaseOrder>
    {
        Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPurchaseOrderListAsync(string company, CancellationToken cancellationToken = default);
    }
}