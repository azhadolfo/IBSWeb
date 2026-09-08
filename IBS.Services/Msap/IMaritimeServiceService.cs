using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IMaritimeServiceService
    {
        Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken);
        Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(Service model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(Service model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
