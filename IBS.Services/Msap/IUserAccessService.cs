using IBS.Models.Msap.MasterFile;
using IBS.Models.Msap.Enums;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IUserAccessService
    {
        Task<bool> CheckAccess(string id, ProcedureEnum procedure, CancellationToken cancellationToken = default);
        Task<IEnumerable<UserAccess>> GetAllAsync(CancellationToken cancellationToken);
        Task<UserAccess?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<UserAccess> PopulateUsersAsync(UserAccess? model, CancellationToken cancellationToken);
        Task<ServiceResult> CreateAsync(UserAccess model, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(UserAccess model, string username, CancellationToken cancellationToken);
    }
}
