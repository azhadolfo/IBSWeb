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

        public async Task<string> GenerateControlNumberAsync(int companyId, CancellationToken cancellationToken = default)
        {
            var company = await _db.Companies.FindAsync(companyId, cancellationToken);

            var lastRecord = await _db.BienesPlacements
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.ControlNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord == null)
            {
                return $"{company.CompanyName.ToUpper()}-000001";
            }

            string lastSeries = lastRecord.ControlNumber;
            string numericPart = lastSeries.Substring(company.CompanyName.Length + 1);
            int incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, company.CompanyName.Length) + "-" + incrementedNumber.ToString("D6");
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
