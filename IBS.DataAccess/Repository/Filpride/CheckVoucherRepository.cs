using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class CheckVoucherRepository : Repository<FilprideCheckVoucherHeader>, ICheckVoucherRepository
    {
        private ApplicationDbContext _db;

        public CheckVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, CancellationToken cancellationToken = default)
        {
            FilprideCheckVoucherHeader? lastCv = await _db
                .FilprideCheckVoucherHeaders
                .Where(x => x.Company == company)
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

        public async Task UpdateInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        {
            var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken) ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.AmountPaid >= invoiceVoucher.Total)
            {
                invoiceVoucher.IsPaid = true;
            }
        }
    }
}