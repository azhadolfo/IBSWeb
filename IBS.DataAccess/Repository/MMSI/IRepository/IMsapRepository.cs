using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IMsapRepository
    {
        Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIPortsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITerminalsById(MMSIDispatchTicket model, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIAllTerminalsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugboatsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITugMastersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIVesselsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSITerminalsById(int portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUncollectedBillingsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICollectedBillsById(int collectionId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICompanyOwnerSelectListById(CancellationToken cancellationToken = default);

        Task<MMSIDispatchTicket> GetDispatchTicketLists(MMSIDispatchTicket model, CancellationToken cancellationToken = default);

        Task<MMSIBilling> GetBillingLists(MMSIBilling model, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        Task<string> GenerateCollectionNumber(CancellationToken cancellationToken = default);

        Task<MMSIBilling> ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default);

    }
}
