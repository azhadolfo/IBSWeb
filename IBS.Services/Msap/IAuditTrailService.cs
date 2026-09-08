using IBS.Models;

namespace IBS.Services.Msap
{
    public interface IAuditTrailService
    {
        Task<IEnumerable<AuditTrail>> GetAuditTrailsByEntityAsync(string documentType, int recordId, CancellationToken cancellationToken);
        
        Task<IEnumerable<AuditTrail>> GetJobOrderTimelineAsync(int jobOrderId, CancellationToken cancellationToken);
        
        Task<(IEnumerable<AuditTrail> Data, int RecordsFiltered, int TotalRecords)> GetPagedAuditTrailsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}
