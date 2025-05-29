using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IServiceRequestRepository : IRepository<MMSIDispatchTicket>
    {
        Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default);

        Task<ServiceRequestViewModel> GetDispatchTicketSelectLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default);

    }
}
