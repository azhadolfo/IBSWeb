using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Models.Filpride.Books;
using IBS.Utility.Constants;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class SalesInvoiceRepository : Repository<FilprideSalesInvoice>, ISalesInvoiceRepository
    {
        private readonly ApplicationDbContext _db;

        public SalesInvoiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public DateOnly ComputeDueDateAsync(string customerTerms, DateOnly transactionDate)
        {
            if (string.IsNullOrEmpty(customerTerms))
            {
                throw new ArgumentException("No record found.");
            }

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

                        switch (dueDate.Day)
                        {
                            case 31:
                                dueDate = dueDate.AddDays(-2);
                                break;
                            case 30:
                                dueDate = dueDate.AddDays(-1);
                                break;
                        }
                    }
                    return dueDate;

                default:
                    return transactionDate;
            }
        }

        public async Task PostAsync(FilprideSalesInvoice salesInvoice, CancellationToken cancellationToken = default)
        {
            #region --Sales Book Recording

            var salesBook = new FilprideSalesBook
            {
                TransactionDate = salesInvoice.TransactionDate,
                SerialNo = salesInvoice.SalesInvoiceNo!,
                SoldTo = salesInvoice.CustomerOrderSlip!.CustomerName,
                TinNo = salesInvoice.CustomerOrderSlip.CustomerTin,
                Address = salesInvoice.CustomerOrderSlip.CustomerAddress,
                Description = salesInvoice.CustomerOrderSlip!.ProductName,
                Amount = salesInvoice.Amount - salesInvoice.Discount
            };

            switch (salesInvoice.CustomerOrderSlip.VatType)
            {
                case SD.VatType_Vatable:
                    salesBook.VatableSales = ComputeNetOfVat(salesBook.Amount);
                    salesBook.VatAmount = ComputeVatAmount(salesBook.VatableSales);
                    salesBook.NetSales = salesBook.VatableSales - salesBook.Discount;
                    break;
                case SD.VatType_Exempt:
                    salesBook.VatExemptSales = salesBook.Amount;
                    salesBook.NetSales = salesBook.VatExemptSales - salesBook.Discount;
                    break;
                default:
                    salesBook.ZeroRated = salesBook.Amount;
                    salesBook.NetSales = salesBook.ZeroRated - salesBook.Discount;
                    break;
            }

            salesBook.Discount = salesInvoice.Discount;
            salesBook.CreatedBy = salesInvoice.CreatedBy;
            salesBook.CreatedDate = salesInvoice.CreatedDate;
            salesBook.DueDate = salesInvoice.DueDate;
            salesBook.DocumentId = salesInvoice.SalesInvoiceId;
            salesBook.Company = salesInvoice.Company;

            await _db.FilprideSalesBooks.AddAsync(salesBook, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion --Sales Book Recording
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
            var lastSi = await _db
                .FilprideSalesInvoices
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Documented))
                .OrderBy(c => c.SalesInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSi == null)
            {
                return "SI0000000001";
            }

            var lastSeries = lastSi.SalesInvoiceNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken)
        {
            var lastSi = await _db
                .FilprideSalesInvoices
                .Where(c => c.Company == company && c.Type == nameof(DocumentType.Undocumented))
                .OrderBy(c => c.SalesInvoiceNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastSi == null)
            {
                return "SIU000000001";
            }

            var lastSeries = lastSi.SalesInvoiceNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<FilprideSalesInvoice?> GetAsync(Expression<Func<FilprideSalesInvoice, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr!.Hauler)
                .Include(si => si.CustomerOrderSlip)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideSalesInvoice>> GetAllAsync(Expression<Func<FilprideSalesInvoice, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideSalesInvoice> query = dbSet
                .Include(si => si.Product)
                .Include(si => si.Customer)
                .Include(si => si.DeliveryReceipt).ThenInclude(dr => dr!.Hauler)
                .Include(si => si.CustomerOrderSlip);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
