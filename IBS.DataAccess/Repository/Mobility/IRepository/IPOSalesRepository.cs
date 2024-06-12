using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IPOSalesRepository : IRepository<POSales>
    {
        Task RecordThePurchaseOrder(IEnumerable<PoSalesRaw> poSales, CancellationToken cancellationToken = default);

        Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default);
    }
}
