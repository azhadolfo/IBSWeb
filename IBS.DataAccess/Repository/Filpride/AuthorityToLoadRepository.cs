using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.Integrated;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class AuthorityToLoadRepository : Repository<FilprideAuthorityToLoad>, IAuthorityToLoadRepository
    {
        private ApplicationDbContext _db;

        public AuthorityToLoadRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateAtlNo(CancellationToken cancellationToken)
        {
            FilprideAuthorityToLoad? lastAtl = await _db
                .FilprideAuthorityToLoads
                .OrderBy(c => c.AuthorityToLoadId)
                .ThenBy(c => c.AuthorityToLoadNo)
                .LastOrDefaultAsync(cancellationToken);

            var yearToday = DateTime.UtcNow.Year;

            if (lastAtl != null)
            {
                var lastAtlNo = lastAtl.AuthorityToLoadNo;
                var lastAtlParts = lastAtlNo.Split('-');
                if (int.TryParse(lastAtlParts.Last(), out var lastIncrement))
                {
                    var newIncrement = lastIncrement + 1;
                    return $"FRI-{yearToday}-{newIncrement}";
                }
            }

            return $"FRI-{yearToday}-1";
        }

        public override async Task<IEnumerable<FilprideAuthorityToLoad>> GetAllAsync(Expression<Func<FilprideAuthorityToLoad, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideAuthorityToLoad> query = dbSet
                .Include(atl => atl.Supplier)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(po => po.Product)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr.Hauler)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr.Customer)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideAuthorityToLoad> GetAsync(Expression<Func<FilprideAuthorityToLoad, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cos => cos.Supplier)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(po => po.Product)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr.Hauler)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(dr => dr.Customer)
                .Include(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos.PickUpPoint)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos.AppointedSuppliers).ThenInclude(a => a.PurchaseOrder)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos.Customer)
                .Include(atl => atl.Details).ThenInclude(atl => atl.CustomerOrderSlip).ThenInclude(cos => cos.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
