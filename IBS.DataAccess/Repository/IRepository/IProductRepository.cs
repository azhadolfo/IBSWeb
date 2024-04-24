using IBS.Models;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<bool> IsProductExist(string product, CancellationToken cancellationToken = default);

        Task UpdateAsync(Product model, CancellationToken cancellationToken = default);
    }
}