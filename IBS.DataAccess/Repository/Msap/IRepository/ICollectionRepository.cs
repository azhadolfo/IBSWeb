using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface ICollectionRepository : IRepository<Collection>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMsapCustomersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapCustomersWithCollectiblesSelectList(int collectionId, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapUncollectedBillingsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMsapCollectedBillsById(int collectionId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMsapUncollectedBillingsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<List<Billing>> GetMsapUncollectedBillingsByCustomerList(int? customerId, CancellationToken cancellationToken);

        Task<string> GenerateCollectionNumber(CancellationToken cancellationToken = default);

        // Accounting Methods
        Task PostAsync(Collection collection, List<Offsettings> offsettings, CancellationToken cancellationToken = default);

        Task UpdateBillingPayment(int billingId, decimal paidAmount, CancellationToken cancellationToken = default);

        Task RemoveBillingPayment(int billingId, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default);

        Task<(IEnumerable<Collection> Data, int RecordsFiltered, int TotalRecords)> GetPagedCollectionsAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }
}


