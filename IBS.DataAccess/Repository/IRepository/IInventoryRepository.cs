using IBS.Models;
using IBS.Models.ViewModels;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IInventoryRepository : IRepository<Inventory>
    {
        Task CalculateTheBeginningInventory(Inventory model, CancellationToken cancellationToken = default);

        Task<Inventory> GetLastInventoryAsync(string productCode, string stationCode, CancellationToken cancellationToken = default);

        Task CalculateTheActualSounding(Inventory model, ActualSoundingViewModel viewModel, CancellationToken cancellationToken = default);
    }
}