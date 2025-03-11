using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Utility.Enums;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CheckVoucherRepository : Repository<FilprideCheckVoucherHeader>, ICheckVoucherRepository
    {
        private ApplicationDbContext _db;

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
            else
            {
                return await GenerateCodeForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.Category == "Trade" && x.CheckVoucherHeaderNo.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "CV0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.Category == "Trade" && x.CheckVoucherHeaderNo.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "CVU000000001";
            }
        }

        public async Task UpdateInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        {
            var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken) ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");

            var detailsVoucher = await _db.FilprideCheckVoucherDetails.Where(cvd => cvd.TransactionNo == invoiceVoucher.CheckVoucherHeaderNo && cvd.AccountNo == "202010200").Select(cvd => cvd.Credit).FirstOrDefaultAsync();

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

            var detailsVoucher = _db.FilprideCheckVoucherDetails.Where(cvd => invoiceVoucher.CheckVoucherHeaderNo.Contains(cvd.TransactionNo)).Select(cvd => cvd.AmountPaid).Sum();

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.Total <= detailsVoucher)
            {
                invoiceVoucher.IsPaid = true;
            }
        }

        public override async Task<FilprideCheckVoucherHeader> GetAsync(Expression<Func<FilprideCheckVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(x => x.Employee)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCheckVoucherHeader>> GetAllAsync(Expression<Func<FilprideCheckVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCheckVoucherHeader> query = dbSet
                .Include(x => x.Employee)
               .Include(c => c.Supplier);

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
            else
            {
                return await GenerateCodeMultipleInvoiceForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeMultipleInvoiceForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "INV000000001";
            }
        }

        private async Task<string> GenerateCodeMultipleInvoiceForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(4);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
            }
            else
            {
                return "INVU00000001";
            }
        }

        public async Task<string> GenerateCodeMultiplePaymentAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeMultiplePaymentForDocumented(company, cancellationToken);
            }
            else
            {
                return await GenerateCodeMultiplePaymentForUnDocumented(company, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeMultiplePaymentForDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "CVN000000001";
            }
        }

        private async Task<string> GenerateCodeMultiplePaymentForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo;
                string numericPart = lastSeries.Substring(4);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
            }
            else
            {
                return "PYTU00000001";
            }
        }
    }
}
