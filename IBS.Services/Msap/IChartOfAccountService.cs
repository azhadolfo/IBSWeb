using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IChartOfAccountService
    {
        Task<IEnumerable<ChartOfAccount>> GetAllAsync(CancellationToken cancellationToken = default);
        
        Task<(IEnumerable<object> Data, int TotalRecords)> GetPagedListAsync(DataTablesParameters parameters, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
        
        Task<ServiceResult> CreateAsync(int parentId, string accountName, string createdBy, string company, CancellationToken cancellationToken = default);
        
        Task<ServiceResult> UpdateAsync(int accountId, string accountName, string editedBy, string company, CancellationToken cancellationToken = default);
        
        Task<byte[]> ExportToExcelAsync(string selectedRecordIds, CancellationToken cancellationToken = default);

        Task<IEnumerable<int>> GetAllIdsAsync(CancellationToken cancellationToken = default);
    }
}
