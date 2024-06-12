using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IFuelPurchaseRepository : IRepository<MobilityFuelPurchase>
    {
        Task RecordTheDeliveryToPurchase(IEnumerable<MobilityFuelDelivery> fuelDeliveries, CancellationToken cancellationToken = default);

        Task<int> ProcessFuelDelivery(string file, CancellationToken cancellationToken = default);

        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilityFuelPurchase model, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetFuelPurchaseJoin(IEnumerable<MobilityFuelPurchase> fuelPurchases, CancellationToken cancellationToken = default);
    }
}
