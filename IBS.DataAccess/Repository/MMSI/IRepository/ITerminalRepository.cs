using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ITerminalRepository : IRepository<MMSITerminal>
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

        Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer (int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUncollectedBillingsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICollectedBillsById(int collectionId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICompanyOwnerSelectListById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUncollectedBillingsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIUsersSelectListById(CancellationToken cancellationToken = default);

        Task<MMSIDispatchTicket> GetDispatchTicketLists(MMSIDispatchTicket model, CancellationToken cancellationToken = default);

        Task<MMSIBilling> GetBillingLists(MMSIBilling model, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        Task<string> GenerateCollectionNumber(CancellationToken cancellationToken = default);

        MMSIBilling ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default);

        Task<List<MMSIDispatchTicket>> GetSalesReport (DateOnly DateFrom, DateOnly DateTo, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>>
            GetMMSITerminalsSelectList(int portId, CancellationToken cancellationToken = default);
    }
}
