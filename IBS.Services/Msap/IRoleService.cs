using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;

namespace IBS.Services.Msap
{
    public interface IRoleService
    {
        Task<IEnumerable<IdentityRole>> GetAllRolesAsync(CancellationToken cancellationToken);
        Task<ServiceResult> CreateRoleAsync(string roleName, CancellationToken cancellationToken);
        Task<(IEnumerable<object> Data, int TotalRecords)> GetPagedRolesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}
