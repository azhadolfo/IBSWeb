using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CreditMemoRepository : Repository<FilprideCreditMemo>, ICreditMemoRepository
    {
        private ApplicationDbContext _db;

        public CreditMemoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(CancellationToken cancellationToken = default)
        {
            FilprideCreditMemo? lastCm = await _db
                .FilprideCreditMemos
                .OrderBy(c => c.CreditMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCm != null)
            {
                string lastSeries = lastCm.CreditMemoNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "CM0000000001";
            }
        }

        public override async Task<FilprideCreditMemo> GetAsync(Expression<Func<FilprideCreditMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCreditMemo>> GetAllAsync(Expression<Func<FilprideCreditMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCreditMemo> query = dbSet
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}