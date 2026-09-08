using IBS.DTOs;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IUserService
    {
        Task<IEnumerable<object>> GetAllUsersAsync(CancellationToken cancellationToken);
        Task<ServiceResult<object>> GetUserByIdAsync(string id, CancellationToken cancellationToken);
        Task<ServiceResult> UpsertUserAsync(UserUpsertDto model, string currentUsername, CancellationToken cancellationToken);
        Task<ServiceResult> ToggleUserStatusAsync(string id, string currentUsername, CancellationToken cancellationToken);
        Task<ServiceResult> ResetPasswordAsync(PasswordResetDto model, string currentUsername, CancellationToken cancellationToken);
        Task<IEnumerable<object>> GetRolesAsync(CancellationToken cancellationToken);
    }
}
