using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IBillingRepository : IRepository<Billing>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<DispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMsapCustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMsapUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(int year, CancellationToken cancellationToken = default);

        Billing ProcessAddress(Billing model, CancellationToken cancellationToken = default);

        Task<(IEnumerable<Billing> Data, int RecordsFiltered, int TotalRecords)> GetPagedBillingsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);

        Task<List<Billing>> GetBillingsByCollectionIdAsync(int collectionId, CancellationToken cancellationToken);
    }
}