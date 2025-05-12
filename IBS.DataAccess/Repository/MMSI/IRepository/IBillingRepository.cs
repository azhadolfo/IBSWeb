using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IBillingRepository : IRepository<MMSIBilling>
    {
        Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer (int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<MMSIBilling> GetBillingLists(MMSIBilling model, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        Task<MMSIBilling> ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default);
    }
}
