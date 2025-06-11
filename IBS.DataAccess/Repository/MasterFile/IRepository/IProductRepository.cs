using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.MasterFile.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<bool> IsProductExist(string product, CancellationToken cancellationToken = default);

        Task UpdateAsync(Product model, CancellationToken cancellationToken = default);
    }
}
