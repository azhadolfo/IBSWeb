using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CreditMemoRepository : Repository<FilprideCreditMemo>, ICreditMemoRepository
    {
        private readonly ApplicationDbContext _db;

        public CreditMemoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCreditMemo? lastCm = await _db
                .FilprideCreditMemos
                .Where(cm => cm.Company == company && cm.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.CreditMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCm == null)
            {
                return "CM0000000001";
            }

            var lastSeries = lastCm.CreditMemoNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCm = await _db
                .FilprideCreditMemos
                .Where(cm => cm.Company == company && cm.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.CreditMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCm == null)
            {
                return "CMU000000001";
            }

            var lastSeries = lastCm.CreditMemoNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<FilprideCreditMemo?> GetAsync(Expression<Func<FilprideCreditMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.CustomerOrderSlip)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCreditMemo>> GetAllAsync(Expression<Func<FilprideCreditMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCreditMemo> query = dbSet
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
