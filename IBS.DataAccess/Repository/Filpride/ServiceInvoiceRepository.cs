using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ServiceInvoiceRepository : Repository<FilprideServiceInvoice>, IServiceInvoiceRepository
    {
        private ApplicationDbContext _db;

        public ServiceInvoiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            FilprideServiceInvoice? lastSv = await _db
                .FilprideServiceInvoices
                .Where(c => c.Company == company)
                .OrderBy(c => c.ServiceInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSv != null)
            {
                string lastSeries = lastSv.ServiceInvoiceNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "SV0000000001";
            }
        }

        public override async Task<FilprideServiceInvoice> GetAsync(Expression<Func<FilprideServiceInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideServiceInvoice>> GetAllAsync(Expression<Func<FilprideServiceInvoice, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideServiceInvoice> query = dbSet
                .Include(s => s.Customer)
                .Include(s => s.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}