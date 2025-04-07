using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Bienes;

namespace IBS.DataAccess.Repository.Bienes.IRepository
{
    public interface IPlacementRepository : IRepository<BienesPlacement>
    {
        Task<string> GenerateControlNumberAsync(string company, CancellationToken cancellationToken = default);
    }
}
