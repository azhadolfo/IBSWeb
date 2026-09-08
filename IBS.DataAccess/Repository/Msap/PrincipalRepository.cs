using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class PrincipalRepository(ApplicationDbContext dbContext)
        : Repository<Principal>(dbContext), IPrincipalRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public override async Task<IEnumerable<Principal>> GetAllAsync(Expression<Func<Principal, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<Principal> query = dbSet.Include(p => p.Customer);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapPortsSelectList(CancellationToken cancellationToken = default)
        {
            var ports = await _dbContext.MsapPorts
                .OrderBy(s => s.PortName)
                .Select(s => new SelectListItem
                {
                    Value = s.PortId.ToString(),
                    Text = s.PortName
                }).ToListAsync(cancellationToken);

            return ports;
        }
        public async Task<List<Principal>> SearchPrincipalsAsync(string term, int customerId, int limit, CancellationToken cancellationToken)
        {
            var query = dbSet.AsNoTracking().Where(p => p.CustomerId == customerId);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var s = term.ToLower();
                query = query.Where(p => p.PrincipalName.ToLower().Contains(s) || p.PrincipalNumber.ToLower().Contains(s));
            }

            return await query
                .OrderBy(p => p.PrincipalName)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}


