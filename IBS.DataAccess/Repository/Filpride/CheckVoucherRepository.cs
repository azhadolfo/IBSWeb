using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

            var detailsVoucher = await _db.FilprideCheckVoucherDetails.Where(cvd => cvd.TransactionNo == invoiceVoucher.CheckVoucherHeaderNo && cvd.AccountNo == "2010102").Select(cvd => cvd.Credit).FirstOrDefaultAsync();

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.AmountPaid >= detailsVoucher)
            {
                invoiceVoucher.IsPaid = true;
            }
        }

        public override async Task<FilprideCheckVoucherHeader> GetAsync(Expression<Func<FilprideCheckVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideCheckVoucherHeader>> GetAllAsync(Expression<Func<FilprideCheckVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideCheckVoucherHeader> query = dbSet
               .Include(c => c.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}