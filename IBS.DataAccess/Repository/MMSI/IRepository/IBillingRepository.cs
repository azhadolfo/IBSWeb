using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface IBillingRepository : IRepository<MMSIBilling>
    {
        /// <summary>
/// Persist pending billing changes to the data store.
/// </summary>
/// <param name="cancellationToken">Token to cancel the save operation.</param>
Task SaveAsync(CancellationToken cancellationToken);

        /// <summary>
/// Retrieve dispatch ticket identifiers that are eligible to be billed for a specific billing record.
/// </summary>
/// <param name="billingId">The identifier of the billing record to collect eligible dispatch tickets for.</param>
/// <returns>A list of dispatch ticket identifiers eligible for billing for the specified billing record, or <c>null</c> if none are found.</returns>
Task<List<string>?> GetToBillDispatchTicketListAsync(int billingId, CancellationToken cancellationToken = default);

        /// <summary>
/// Retrieves a distinct list of tugboat identifiers associated with the specified billing record.
/// </summary>
/// <param name="billingId">The identifier of the billing record to query.</param>
/// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
/// <returns>A list of unique tugboat identifiers for the billing record, or `null` if none are found.</returns>
Task<List<string>?> GetUniqueTugboatsListAsync(int billingId, CancellationToken cancellationToken = default);

        /// <summary>
/// Retrieves the dispatch tickets that have been marked as paid for the specified billing record.
/// </summary>
/// <param name="billingId">The identifier of the billing record to retrieve paid dispatch tickets for.</param>
/// <returns>A list of paid <see cref="MMSIDispatchTicket"/> instances associated with the billing record, or <c>null</c> if none are found.</returns>
Task<List<MMSIDispatchTicket>?> GetPaidDispatchTicketsAsync(int billingId, CancellationToken cancellationToken = default);

        /// <summary>
/// Retrieve terminal options associated with the specified port.
/// </summary>
/// <param name="portId">Identifier of the port whose terminals should be retrieved.</param>
/// <returns>A list of <see cref="SelectListItem"/> representing terminals for the given port; empty list if no terminals are found.</returns>
Task<List<SelectListItem>> GetMMSITerminalsByPortId(int portId, CancellationToken cancellationToken = default);

        /// <summary>
/// Retrieves customer options formatted as SelectListItem for use in UI selection controls.
/// </summary>
/// <returns>A list of SelectListItem representing available customers.</returns>
Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        /// <summary>
/// Gets customers that have billable items, filtered by an optional current customer and a billable type.
/// </summary>
/// <param name="currentCustomerId">Optional id of the current/selected customer; pass null when no current customer is specified.</param>
/// <param name="type">Billable category or ticket type used to filter the customers.</param>
/// <returns>A list of <see cref="SelectListItem"/> representing customers with billables matching the filter, or <c>null</c> if no matching customers are found.</returns>
Task<List<SelectListItem>?> GetMMSICustomersWithBillablesSelectList(int? currentCustomerId, string type, CancellationToken cancellationToken = default);

        /// <summary>
/// Retrieve unbilled MMSI tickets filtered by ticket type.
/// </summary>
/// <param name="type">The ticket type used to filter unbilled tickets.</param>
/// <returns>A list of SelectListItem representing unbilled tickets that match the specified type.</returns>
Task<List<SelectListItem>> GetMMSIUnbilledTicketsById(string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUnbilledTicketsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSIBilledTicketsById(int id, CancellationToken cancellationToken = default);

        Task<string> GenerateBillingNumber(CancellationToken cancellationToken = default);

        MMSIBilling ProcessAddress(MMSIBilling model, CancellationToken cancellationToken = default);
    }
}