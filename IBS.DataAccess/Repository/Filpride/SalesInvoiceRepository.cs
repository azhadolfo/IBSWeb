using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class SalesInvoiceRepository : Repository<FilprideSalesInvoice>, ISalesInvoiceRepository
    {
        private ApplicationDbContext _db;

        public SalesInvoiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<DateOnly> ComputeDueDateAsync(string customerTerms, DateOnly transactionDate)
        {
            if (customerTerms != null)
            {
                DateOnly dueDate;

                switch (customerTerms)
                {
                    case "7D":
                        return transactionDate.AddDays(7);

                    case "10D":
                        return transactionDate.AddDays(10);

                    case "15D":
                    case "15PDC":
                        return transactionDate.AddDays(15);

                    case "30D":
                    case "30PDC":
                        return transactionDate.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return transactionDate.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return transactionDate.AddDays(60);

                    case "90D":
                        return transactionDate.AddDays(90);

                    case "M15":
                        return transactionDate.AddMonths(1).AddDays(15 - transactionDate.Day);

                    case "M30":
                        if (transactionDate.Month == 1)
                        {
                            dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    case "M29":
                        if (transactionDate.Month == 1)
                        {
                            dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(transactionDate.Year, transactionDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-2);
                            }
                            else if (dueDate.Day == 30)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return transactionDate;
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
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

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken)
        {
            FilprideSalesInvoice? lastSi = await _db
                .FilprideSalesInvoices
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Documented))
                //.OrderBy(c => c.SalesInvoiceNo) -- Uncomment this later this is for skipping of invoice only
                .OrderBy(c => c.SalesInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSi != null)
            {
                string lastSeries = lastSi.SalesInvoiceNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "SI0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            FilprideSalesInvoice? lastSi = await _db
                .FilprideSalesInvoices
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.SalesInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSi != null)
            {
                string lastSeries = lastSi.SalesInvoiceNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "SIU000000001";
            }
        }

        public override async Task<FilprideSalesInvoice> GetAsync(Expression<Func<FilprideSalesInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr.Hauler)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideSalesInvoice>> GetAllAsync(Expression<Func<FilprideSalesInvoice, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideSalesInvoice> query = dbSet
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr.Hauler);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
