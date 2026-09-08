using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IVesselService
    {
        Task<IEnumerable<Vessel>> GetAllAsync(CancellationToken cancellationToken);
        Task<Vessel?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(Vessel model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(Vessel model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
