using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IProductRepository : IRepository<MobilityProduct>
    {
        Task<bool> IsProductExist(string product, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilityProduct model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetProductListAsyncById(CancellationToken cancellationToken = default);
    }
}
