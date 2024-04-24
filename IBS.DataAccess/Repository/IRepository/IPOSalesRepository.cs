using IBS.Models;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IPOSalesRepository : IRepository<POSales>
    {
        Task RecordThePurchaseOrder(IEnumerable<PoSalesRaw> poSales, CancellationToken cancellationToken = default);

        Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default);
    }
}
