using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class PickUpPointRepository : Repository<FilpridePickUpPoint>, IPickUpPointRepository
    {
        private ApplicationDbContext _db;

        public PickUpPointRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<List<SelectListItem>> GetDistinctPickupPointList(string companyClaims, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePickUpPoints
                .Where(p => (companyClaims == nameof(Filpride) ? p.IsFilpride : p.IsMobility))
                .GroupBy(p => p.Depot)
                .OrderBy(g => g.Key)
                .Select(g => new SelectListItem
                {
                    Value = g.First().PickUpPointId.ToString(),
                    Text = g.Key // g.Key is the Depot name
                })
                .ToListAsync(cancellationToken);
        }


        public async Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(string companyClaims, int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePickUpPoints
                .OrderBy(p => p.Depot)
                .Where(p => p.SupplierId == supplierId && (companyClaims == nameof(Filpride) ? p.IsFilpride : p.IsMobility))
                .Select(po => new SelectListItem
                {
                    Value = po.PickUpPointId.ToString(),
                    Text = po.Depot
                })
                .ToListAsync(cancellationToken);
        }
    }
}
