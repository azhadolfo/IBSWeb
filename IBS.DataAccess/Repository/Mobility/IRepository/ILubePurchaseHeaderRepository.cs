using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ILubePurchaseHeaderRepository : IRepository<MobilityLubePurchaseHeader>
    {
        Task<int> ProcessLubeDelivery(string file, CancellationToken cancellationToken = default);

        Task RecordTheDeliveryToPurchase(IEnumerable<LubeDelivery> lubeDeliveries, CancellationToken cancellationToken = default);

        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetLubePurchaseJoin(IEnumerable<MobilityLubePurchaseHeader> lubePurchases, CancellationToken cancellationToken = default);
    }
}