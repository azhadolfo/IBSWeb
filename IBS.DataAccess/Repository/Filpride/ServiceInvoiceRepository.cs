using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ServiceInvoiceRepository : Repository<FilprideServiceInvoice>, IServiceInvoiceRepository
    {
        private readonly ApplicationDbContext _db;

        public ServiceInvoiceRepository(ApplicationDbContext db) : base(db)
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


        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .FilprideServiceInvoices
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_service_invoices
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY service_invoice_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Documented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSv == null)
            {
                return "SV0000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            var lastSv = await _db
                .FilprideServiceInvoices
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_service_invoices
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY service_invoice_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Undocumented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastSv == null)
            {
                return "SVU000000001";
            }

            var lastSeries = lastSv.ServiceInvoiceNo;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public override async Task<FilprideServiceInvoice?> GetAsync(Expression<Func<FilprideServiceInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .Include(s => s.DeliveryReceipt)
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
