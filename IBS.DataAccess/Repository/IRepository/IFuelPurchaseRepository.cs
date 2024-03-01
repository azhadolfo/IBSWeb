using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IFuelPurchaseRepository : IRepository<FuelPurchase>
    {
        Task RecordTheDeliveryToPurchase(IEnumerable<FuelDelivery> fuelDeliveries, CancellationToken cancellationToken = default);

        Task<int> ProcessFuelDelivery(string file, CancellationToken cancellationToken = default);

        Task PostAsync(int id, CancellationToken cancellationToken = default);

        Task UpdateAsync(FuelPurchase model, CancellationToken cancellationToken = default);
    }
}
