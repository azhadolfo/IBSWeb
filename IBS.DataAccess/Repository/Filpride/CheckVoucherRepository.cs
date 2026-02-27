using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                    x.Category == "Trade" &&
                    x.Type == nameof(DocumentType.Documented) &&
                    x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "CV0000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                        x.Category == "Trade" &&
                        x.Type == nameof(DocumentType.Undocumented) &&
                        x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "CVU000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

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
            var invoiceVoucher = await GetAsync(i => i.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken)
                ?? throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");

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
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeMultipleInvoiceForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeMultipleInvoiceForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeMultipleInvoiceForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                        x.CvType == nameof(CVType.Invoicing) &&
                        x.Type == nameof(DocumentType.Documented) &&
                        x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "INV000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        private async Task<string> GenerateCodeMultipleInvoiceForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                        x.CvType == nameof(CVType.Invoicing) &&
                        x.Type == nameof(DocumentType.Undocumented) &&
                        x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "INVU00000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(4);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
        }

        public async Task<string> GenerateCodeMultiplePaymentAsync(string company, string type, CancellationToken cancellationToken = default)
        {
            return type switch
            {
                nameof(DocumentType.Documented) => await GenerateCodeMultiplePaymentForDocumented(company, cancellationToken),
                nameof(DocumentType.Undocumented) => await GenerateCodeMultiplePaymentForUnDocumented(company, cancellationToken),
                _ => throw new ArgumentException("Invalid type")
            };
        }

        private async Task<string> GenerateCodeMultiplePaymentForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                        x.CvType == nameof(CVType.Payment) &&
                        x.Type == nameof(DocumentType.Documented) &&
                        x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "CVN000000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        private async Task<string> GenerateCodeMultiplePaymentForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCv = await _db
                .FilprideCheckVoucherHeaders
                .AsNoTracking()
                .OrderByDescending(x => x.CheckVoucherHeaderNo!.Length)
                .ThenByDescending(x => x.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(x =>
                        x.CvType == nameof(CVType.Payment) &&
                        x.Type == nameof(DocumentType.Undocumented) &&
                        x.Company == company,
                    cancellationToken);

            if (lastCv == null)
            {
                return "CVNU00000001";
            }

            var lastSeries = lastCv.CheckVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(4);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 4) + incrementedNumber.ToString("D8");
        }

        public async Task PostAsync(FilprideCheckVoucherHeader header,
            IEnumerable<FilprideCheckVoucherDetail> details,
            CancellationToken cancellationToken = default)
        {
            #region --General Ledger Book Recording(CV)--

            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var ledgers = new List<FilprideGeneralLedgerBook>();
            foreach (var detail in details)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == detail.AccountNo)
                              ?? throw new ArgumentException($"Account title '{detail.AccountNo}' not found.");
                ledgers.Add(
                        new FilprideGeneralLedgerBook
                        {
                            Date = header.Date,
                            Reference = header.CheckVoucherHeaderNo!,
                            Description = header.Particulars!,
                            AccountId = account.AccountId,
                            AccountNo = account.AccountNumber,
                            AccountTitle = account.AccountName,
                            Debit = detail.Debit,
                            Credit = detail.Credit,
                            Company = header.Company,
                            CreatedBy = header.PostedBy!,
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            SubAccountType = detail.SubAccountType,
                            SubAccountId = detail.SubAccountId,
                            SubAccountName = detail.SubAccountName,
                            ModuleType = nameof(ModuleType.Disbursement)
                        }
                    );
            }

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            #endregion --General Ledger Book Recording(CV)--
        }
    }
}
