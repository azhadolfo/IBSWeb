using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DebitMemoRepository : Repository<FilprideDebitMemo>, IDebitMemoRepository
    {
        private ApplicationDbContext _db;

        public DebitMemoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideDebitMemo? lastDm = await _db
                .FilprideDebitMemos
                .Where(dm => dm.Company == company && dm.Type == nameof(DocumentType.Documented))
                .OrderBy(dm => dm.DebitMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDm != null)
            {
                string lastSeries = lastDm.DebitMemoNo!;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "DM0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideDebitMemo? lastDm = await _db
                .FilprideDebitMemos
                .Where(dm => dm.Company == company && dm.Type == nameof(DocumentType.Undocumented))
                .OrderBy(dm => dm.DebitMemoNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastDm != null)
            {
                string lastSeries = lastDm.DebitMemoNo!;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "DMU000000001";
            }
        }

        public override async Task<FilprideDebitMemo?> GetAsync(Expression<Func<FilprideDebitMemo, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(c => c.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideDebitMemo>> GetAllAsync(Expression<Func<FilprideDebitMemo, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideDebitMemo> query = dbSet
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
