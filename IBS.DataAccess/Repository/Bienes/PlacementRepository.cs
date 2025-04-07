using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Bienes.IRepository;
using IBS.Models.Bienes;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Bienes
{
    public class PlacementRepository : Repository<BienesPlacement>, IPlacementRepository
    {
        private ApplicationDbContext _db;

        public PlacementRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public Task<string> GenerateControlNumberAsync(string company, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override async Task<BienesPlacement> GetAsync(Expression<Func<BienesPlacement, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(p => p.Company)
                .Include(p => p.BankAccount)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<BienesPlacement>> GetAllAsync(Expression<Func<BienesPlacement, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<BienesPlacement> query = dbSet
                .Include(p => p.Company)
                .Include(p => p.BankAccount);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
