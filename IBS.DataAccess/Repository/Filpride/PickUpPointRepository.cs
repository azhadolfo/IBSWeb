using System.Linq.Expressions;
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

        public override async Task<IEnumerable<FilpridePickUpPoint>> GetAllAsync(Expression<Func<FilpridePickUpPoint, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilpridePickUpPoint> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query
                .Include(p => p.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPickUpPointListBasedOnSupplier(string companyClaims, int supplierId, CancellationToken cancellationToken = default)
        {
            return await _db.FilpridePickUpPoints
                .OrderBy(p => p.Depot)
                .Where(p => p.SupplierId == supplierId)
                .Select(po => new SelectListItem
                {
                    Value = po.PickUpPointId.ToString(),
                    Text = po.Depot
                })
                .ToListAsync(cancellationToken);
        }
    }
}
