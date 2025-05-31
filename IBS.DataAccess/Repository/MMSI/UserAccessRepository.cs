using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class UserAccessRepository : Repository<MMSIUserAccess>, IUserAccessRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public UserAccessRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MMSIUserAccess>> GetAllAsync(Expression<Func<MMSIUserAccess, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MMSIUserAccess> query = dbSet;
            if (filter != null)
            {
                query = query.Where(filter)
                    .OrderBy(ua => ua.UserName);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
