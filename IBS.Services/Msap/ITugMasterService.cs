using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface ITugMasterService
    {
        Task<IEnumerable<TugMaster>> GetAllAsync(CancellationToken cancellationToken);
        Task<TugMaster?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(TugMaster model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(TugMaster model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken cancellationToken);
    }
}
