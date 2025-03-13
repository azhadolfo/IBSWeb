using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IPickUpPointRepository : IRepository<FilpridePickUpPoint>
    {
        Task<List<SelectListItem>> GetDistinctPickupPointList(CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(int supplierId, CancellationToken cancellationToken = default);
    }
}
