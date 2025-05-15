using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IPickUpPointRepository : IRepository<MobilityPickUpPoint>
    {
        Task<List<SelectListItem>> GetMobilityTradeSupplierListAsyncById(string stationCode, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetDistinctPickupPointList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(int supplierId, CancellationToken cancellationToken = default);
    }
}
