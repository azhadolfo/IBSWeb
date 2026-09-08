using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface ITugboatService
    {
        Task<IEnumerable<Tugboat>> GetAllAsync(CancellationToken cancellationToken);
        Task<Tugboat?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<Tugboat> PopulateSelectListsAsync(Tugboat? model, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(Tugboat model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(Tugboat model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
