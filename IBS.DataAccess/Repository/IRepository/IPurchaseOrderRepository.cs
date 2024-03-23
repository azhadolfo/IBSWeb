using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        Task RecordThePurchaseOrder(IEnumerable<POSales> poSales, CancellationToken cancellationToken = default);

        Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default);
    }
}
