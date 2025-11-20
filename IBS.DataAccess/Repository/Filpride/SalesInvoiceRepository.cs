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
                .Include(si => si.DeliveryReceipt)
                    .ThenInclude(dr => dr!.Hauler)
                .Include(si => si.DeliveryReceipt)
                    .ThenInclude(dr => dr!.Commissionee)
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
