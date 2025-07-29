using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class UserAccessRepository : Repository<MMSIUserAccess>, IUserAccessRepository
    {
        private readonly ApplicationDbContext _db;

        public UserAccessRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MMSIUserAccess>> GetAllAsync(Expression<Func<MMSIUserAccess, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSIUserAccess> query = dbSet
                .OrderBy(ua => ua.UserName);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
