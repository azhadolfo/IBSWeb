using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IFuelPurchaseRepository : IRepository<FuelPurchase>
    {
        Task RecordTheDeliveryToPurchase(IEnumerable<FuelDelivery> fuelDeliveries, CancellationToken cancellationToken = default);

        Task<int> ProcessFuelDelivery(string file, CancellationToken cancellationToken = default);

        Task PostAsync(string id, string postedBy, string stationCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(FuelPurchase model, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetFuelPurchaseJoin(IEnumerable<FuelPurchase> fuelPurchases, CancellationToken cancellationToken = default);
    }
}
