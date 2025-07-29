using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CheckVoucherRepository : Repository<FilprideCheckVoucherHeader>, ICheckVoucherRepository
    {
        private readonly ApplicationDbContext _db;

        public CheckVoucherRepository(ApplicationDbContext db) : base(db)
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
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.Category == "Trade" &&
                            x.CheckVoucherHeaderNo!.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CV0000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.Category == "Trade" &&
                            x.CheckVoucherHeaderNo!.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CVU000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public async Task UpdateInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        {
            var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken)
                                 ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");

            var detailsVoucher = await _db.FilprideCheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == invoiceVoucher.CheckVoucherHeaderNo
                              && cvd.AccountNo == "202010200")
                .Select(cvd => cvd.Credit)
                .FirstOrDefaultAsync(cancellationToken);

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.AmountPaid >= detailsVoucher)
            {
                invoiceVoucher.IsPaid = true;
                invoiceVoucher.Status = nameof(CheckVoucherInvoiceStatus.Paid);
            }
        }

        public async Task UpdateMultipleInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        {
            var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken) ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");

            var detailsVoucher = await _db.FilprideCheckVoucherDetails
                .Where(cvd => invoiceVoucher.CheckVoucherHeaderNo!.Contains(cvd.TransactionNo))
                .Select(cvd => cvd.AmountPaid)
                .SumAsync(cancellationToken);

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.Total <= detailsVoucher)
            {
                invoiceVoucher.IsPaid = true;
            }
        }

        public override async Task<FilprideCheckVoucherHeader?> GetAsync(Expression<Func<FilprideCheckVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cv => cv.BankAccount)
                .Include(cv => cv.Employee)
                .Include(cv => cv.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCheckVoucherHeader>> GetAllAsync(Expression<Func<FilprideCheckVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCheckVoucherHeader> query = dbSet
                .Include(cv => cv.BankAccount)
                .Include(cv => cv.Employee)
                .Include(cv => cv.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateCodeMultipleInvoiceAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeMultipleInvoiceForDocumented(company, cancellationToken);
            }

            return await GenerateCodeMultipleInvoiceForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeMultipleInvoiceForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "INV000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        private async Task<string> GenerateCodeMultipleInvoiceForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "INVU00000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(4);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
        }

        public async Task<string> GenerateCodeMultiplePaymentAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeMultiplePaymentForDocumented(company, cancellationToken);
            }

            return await GenerateCodeMultiplePaymentForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeMultiplePaymentForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CVN000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        private async Task<string> GenerateCodeMultiplePaymentForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv == null)
            {
                return "CVNU00000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(4);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
        }
    }
}
