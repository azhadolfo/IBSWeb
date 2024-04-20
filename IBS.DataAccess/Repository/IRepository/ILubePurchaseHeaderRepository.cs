using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface ILubePurchaseHeaderRepository : IRepository<LubePurchaseHeader>
    {
        Task<int> ProcessLubeDelivery(string file, CancellationToken cancellationToken = default);

        Task RecordTheDeliveryToPurchase(IEnumerable<LubeDelivery> lubeDeliveries, CancellationToken cancellationToken = default);

        Task PostAsync(string id, string postedBy, CancellationToken cancellationToken = default);

        IEnumerable<dynamic> GetLubePurchaseJoin(IEnumerable<LubePurchaseHeader> lubePurchases, CancellationToken cancellationToken = default);
    }
}
