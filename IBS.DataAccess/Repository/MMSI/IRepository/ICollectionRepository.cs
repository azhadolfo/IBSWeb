using System.Linq.Expressions;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.MMSI.IRepository
{
    public interface ICollectionRepository : IRepository<MMSICollection>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<List<SelectListItem>> GetMMSICustomersById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICustomersWithCollectiblesSelectList(int collectionId, string type, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSIUncollectedBillingsById(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetMMSICollectedBillsById(int collectionId, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>?> GetMMSIUncollectedBillingsByCustomer(int? customerId, CancellationToken cancellationToken);

        Task<string> GenerateCollectionNumber(CancellationToken cancellationToken = default);
    }
}
