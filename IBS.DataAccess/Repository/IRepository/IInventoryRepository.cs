using IBS.Models;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task CalculateTheBeginningInventory(Inventory model, CancellationToken cancellationToken = default);
    }
}
