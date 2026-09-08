using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IDispatchTicketRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<DispatchTicket?> GetDispatchTicketWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        Task<IEnumerable<DispatchTicket>> GetAllDispatchTicketsWithDetailsAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<DispatchTicket>> GetDispatchTicketsWithDetailsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);

        Task<bool> IsJobOrderEditableAsync(int? jobOrderId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<DispatchTicket> Data, int RecordsFiltered, int TotalRecords)> GetPagedDispatchTicketsAsync(DataTablesParameters parameters, string filterType, CancellationToken cancellationToken = default);       }
}


