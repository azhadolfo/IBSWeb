using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using IBS.Models;

namespace IBS.Services.Msap
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken);
        Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ServiceResult<int>> CreateAsync(Employee model, string? companyClaims, string username, CancellationToken cancellationToken);
        Task<ServiceResult> UpdateAsync(Employee model, string username, CancellationToken cancellationToken);
        Task<(IEnumerable<Employee> Data, int TotalRecords)> GetPagedEmployeesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}
