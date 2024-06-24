using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CustomerOrderSlipRepository : Repository<FilprideCustomerOrderSlip>, ICustomerOrderSlipRepository
    {
        private ApplicationDbContext _db;

        public CustomerOrderSlipRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideCustomerOrderSlip? lastCos = await _db
                .FilprideCustomerOrderSlips
                .OrderBy(c => c.CustomerOrderSlipNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCos != null)
            {
                string lastSeries = lastCos.CustomerOrderSlipNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "COS0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideCustomerOrderSlip>> GetAllAsync(Expression<Func<FilprideCustomerOrderSlip, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCustomerOrderSlip> query = dbSet
                .Include(po => po.Customer)
                .Include(po => po.Product);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideCustomerOrderSlip> GetAsync(Expression<Func<FilprideCustomerOrderSlip, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(po => po.Customer)
                .Include(po => po.Product)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}