using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IDispatchTicketRepository : IRepository<MMSIDispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        Task<MMSIDispatchTicket> GetDispatchTicketLists(MMSIDispatchTicket model, CancellationToken cancellationToken = default);
    }
}
