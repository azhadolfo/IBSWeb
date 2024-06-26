using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DeliveryReportRepository : Repository<FilprideDeliveryReport>, IDeliveryReportRepository
    {
        private ApplicationDbContext _db;

        public DeliveryReportRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideDeliveryReport? lastDr = await _db
                .FilprideDeliveryReports
                .OrderBy(c => c.DeliveryReportNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDr != null)
            {
                string lastSeries = lastDr.DeliveryReportNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "DR0000000001";
            }
        }

        public override async Task<IEnumerable<FilprideDeliveryReport>> GetAllAsync(Expression<Func<FilprideDeliveryReport, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDeliveryReport> query = dbSet
                .Include(dr => dr.CustomerOrderSlip)
                .Include(dr => dr.Hauler);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<FilprideDeliveryReport> GetAsync(Expression<Func<FilprideDeliveryReport, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(dr => dr.CustomerOrderSlip)
                .Include(dr => dr.Hauler)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}