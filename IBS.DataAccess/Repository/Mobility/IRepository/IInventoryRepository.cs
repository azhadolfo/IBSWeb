using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IInventoryRepository : IRepository<MobilityInventory>
    {
        Task CalculateTheBeginningInventory(MobilityInventory model, CancellationToken cancellationToken = default);

        Task<MobilityInventory> GetLastInventoryAsync(string productCode, string stationCode, CancellationToken cancellationToken = default);

        Task CalculateTheActualSounding(MobilityInventory model, ActualSoundingViewModel viewModel, CancellationToken cancellationToken = default);
    }
}