using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Bienes;
using IBS.Models.Bienes.ViewModels;

namespace IBS.DataAccess.Repository.Bienes.IRepository
{
    public interface IPlacementRepository : IRepository<BienesPlacement>
    {
        Task<string> GenerateControlNumberAsync(int companyId, CancellationToken cancellationToken = default);

        Task UpdateAsync(PlacementViewModel viewModel, CancellationToken cancellationToken = default);

    }
}
