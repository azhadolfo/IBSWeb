using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Mobility;
using IBS.Utility.Enums;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class CheckVoucherRepository : Repository<MobilityCheckVoucherHeader>, ICheckVoucherRepository
    {
        private ApplicationDbContext _db;

        public CheckVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string stationCode, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(stationCode, cancellationToken);
            }
            else
            {
                return await GenerateCodeForUnDocumented(stationCode, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeForDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCode && x.Type == nameof(DocumentType.Documented) && x.Category == "Trade"
                            && x.CheckVoucherHeaderNo!.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return "CV0000000001";
            }
        }

        private async Task<string> GenerateCodeForUnDocumented(string stationCode, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCode && x.Type == nameof(DocumentType.Undocumented) && x.Category == "Trade"
                            && x.CheckVoucherHeaderNo!.Contains("CV"))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
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

            var detailsVoucher = await _db.MobilityCheckVoucherDetails.Where(cvd => cvd.TransactionNo == invoiceVoucher.CheckVoucherHeaderNo && cvd.AccountNo == "202010200").Select(cvd => cvd.Credit).FirstOrDefaultAsync();

            invoiceVoucher.AmountPaid += paymentAmount;

            if (invoiceVoucher.AmountPaid >= detailsVoucher)
            {
                invoiceVoucher.IsPaid = true;
                invoiceVoucher.Status = nameof(CheckVoucherInvoiceStatus.Paid);
            }
        }

        // public async Task UpdateMultipleInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        // {
        //     var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken) ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");
        //
        //     var detailsVoucher = _db.FilprideCheckVoucherDetails.Where(cvd => invoiceVoucher.CheckVoucherHeaderNo.Contains(cvd.TransactionNo)).Select(cvd => cvd.AmountPaid).Sum();
        //
        //     invoiceVoucher.AmountPaid += paymentAmount;
        //
        //     if (invoiceVoucher.Total <= detailsVoucher)
        //     {
        //         invoiceVoucher.IsPaid = true;
        //     }
        // }

        public override async Task<MobilityCheckVoucherHeader?> GetAsync(Expression<Func<MobilityCheckVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(x => x.Employee)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MobilityCheckVoucherHeader>> GetAllAsync(Expression<Func<MobilityCheckVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MobilityCheckVoucherHeader> query = dbSet
                .Include(x => x.Employee)
               .Include(c => c.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateCodeMultipleInvoiceAsync(string stationCodeClaims, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeMultipleInvoiceForDocumented(stationCodeClaims, cancellationToken);
            }
            else
            {
                return await GenerateCodeMultipleInvoiceForUnDocumented(stationCodeClaims, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeMultipleInvoiceForDocumented(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCodeClaims && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "INV000000001";
            }
        }

        private async Task<string> GenerateCodeMultipleInvoiceForUnDocumented(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCodeClaims && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Invoicing))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
                string numericPart = lastSeries.Substring(4);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
            }
            else
            {
                return "INVU00000001";
            }
        }

        public async Task<string> GenerateCodeMultiplePaymentAsync(string stationCodeClaims, string type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeMultiplePaymentForDocumented(stationCodeClaims, cancellationToken);
            }
            else
            {
                return await GenerateCodeMultiplePaymentForUnDocumented(stationCodeClaims, cancellationToken);
            }
        }

        private async Task<string> GenerateCodeMultiplePaymentForDocumented(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCodeClaims && x.Type == nameof(DocumentType.Documented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
                string numericPart = lastSeries.Substring(3);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
            }
            else
            {
                return "CVN000000001";
            }
        }

        private async Task<string> GenerateCodeMultiplePaymentForUnDocumented(string stationCodeClaims, CancellationToken cancellationToken = default)
        {
            MobilityCheckVoucherHeader? lastCv = await _db
                .MobilityCheckVoucherHeaders
                .Where(x => x.StationCode == stationCodeClaims && x.Type == nameof(DocumentType.Undocumented) && x.CvType == nameof(CVType.Payment))
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .LastOrDefaultAsync(cancellationToken);

            if (lastCv != null)
            {
                string lastSeries = lastCv.CheckVoucherHeaderNo!;
                string numericPart = lastSeries.Substring(4);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
            }
            else
            {
                return "CVNU00000001";
            }
        }
    }
}
