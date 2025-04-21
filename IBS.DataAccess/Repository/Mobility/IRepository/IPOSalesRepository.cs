using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IPOSalesRepository : IRepository<MobilityPOSales>
    {
        Task RecordThePurchaseOrder(IEnumerable<MobilityPoSalesRaw> poSales, CancellationToken cancellationToken = default);

        Task<int> ProcessPOSales(string file, CancellationToken cancellationToken = default);

        Task<int> ProcessPOSalesGoogleDrive(GoogleDriveFileViewModel file, CancellationToken cancellationToken = default);
    }
}
