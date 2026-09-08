using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace IBS.Services.Msap
{
    public class RoleService(
        RoleManager<IdentityRole> roleManager)
        : IRoleService
    {
        public async Task<IEnumerable<IdentityRole>> GetAllRolesAsync(CancellationToken cancellationToken)
        {
            return await roleManager.Roles.ToListAsync(cancellationToken);
        }

        public async Task<ServiceResult> CreateRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return ServiceResult.Failure("Role name is required.");
            }

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    return ServiceResult.Success("Role created successfully.");
                }
                return ServiceResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return ServiceResult.Failure("Role already exists.");
        }

        public async Task<(IEnumerable<object> Data, int TotalRecords)> GetPagedRolesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var queried = roleManager.Roles;

            if (!string.IsNullOrEmpty(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.ToLower();
                queried = queried.Where(r => r.Name!.ToLower().Contains(searchValue));
            }

            if (parameters.Order?.Count > 0)
            {
                var orderColumn = parameters.Order[0];
                var columnName = parameters.Columns[orderColumn.Column].Name;
                if (string.IsNullOrEmpty(columnName))
                {
                    columnName = parameters.Columns[orderColumn.Column].Data;
                }

                var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                queried = queried.AsQueryable().OrderBy($"{columnName} {sortDirection}");
            }

            var totalRecords = await queried.CountAsync(cancellationToken);
            var pagedData = await queried
                .Select(r => new { r.Name })
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync(cancellationToken);

            return (pagedData, totalRecords);
        }
    }
}
