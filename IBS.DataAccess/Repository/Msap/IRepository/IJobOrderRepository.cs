using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IJobOrderRepository : IRepository<JobOrder>
    {
        Task<IEnumerable<JobOrder>> GetAllJobOrdersWithDetailsAsync(CancellationToken cancellationToken);
        Task<IEnumerable<JobOrder>> GetJobOrdersWithDetailsAsync(DateTime start, DateTime end, CancellationToken cancellationToken);
        Task<JobOrder?> GetJobOrderWithDetailsAsync(int id, CancellationToken cancellationToken);
        Task<string> GenerateJobOrderNumber(CancellationToken cancellationToken);
        Task<List<JobOrder>> SearchBillableJobOrdersAsync(string term, int customerId, int limit, CancellationToken cancellationToken);
        Task<(IEnumerable<JobOrder> Data, int RecordsFiltered, int TotalRecords)> GetPagedJobOrdersAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}


