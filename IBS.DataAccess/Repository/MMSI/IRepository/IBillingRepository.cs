using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IBillingRepository : IRepository<MMSIBilling>
    {
        Task<List<SelectListItem>> GetMMSITerminalsByPortId(int portId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersWithBillablesSelectList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer (int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        MMSIBilling ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default);
    }
}
