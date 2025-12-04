using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class DebitMemoRepository : Repository<FilprideDebitMemo>, IDebitMemoRepository
    {
        private readonly ApplicationDbContext _db;

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

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .FilprideDebitMemos
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_debit_memos
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY credit_debit_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Documented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastDm == null)
            {
                return "DM0000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastDm = await _db
                .FilprideDebitMemos
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_debit_memos
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY credit_debit_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Undocumented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastDm == null)
            {
                return "DMU000000001";
            }

            var lastSeries = lastDm.DebitMemoNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

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
                .Include(si => si.SalesInvoice)
                .ThenInclude(cos => cos!.CustomerOrderSlip)
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
