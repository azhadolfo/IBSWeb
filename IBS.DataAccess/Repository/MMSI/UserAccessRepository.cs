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
