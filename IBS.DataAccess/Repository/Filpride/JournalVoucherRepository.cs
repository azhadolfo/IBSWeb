using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;

namespace IBS.DataAccess.Repository.Filpride
{
    public class JournalVoucherRepository : Repository<FilprideJournalVoucherHeader>, IJournalVoucherRepository
    {
        private readonly ApplicationDbContext _db;

        public JournalVoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateCodeAsync(string company, string? type, CancellationToken cancellationToken = default)
        {
            if (type == nameof(DocumentType.Documented))
            {
                return await GenerateCodeForDocumented(company, cancellationToken);
            }

            return await GenerateCodeForUnDocumented(company, cancellationToken);
        }

        private async Task<string> GenerateCodeForDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastJv = await _db
                .FilprideJournalVoucherHeaders
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_journal_voucher_headers
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY journal_voucher_header_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Documented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastJv == null)
            {
                return "JV0000000001";
            }

            var lastSeries = lastJv.JournalVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");

        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastJv = await _db
                .FilprideJournalVoucherHeaders
                .FromSqlRaw(@"
                    SELECT *
                    FROM filpride_journal_voucher_headers
                    WHERE company = {0}
                        AND type = {1}
                    ORDER BY journal_voucher_header_no DESC
                    LIMIT 1
                    FOR UPDATE",
                    company,
                    nameof(DocumentType.Undocumented))
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (lastJv == null)
            {
                return "JVU000000001";
            }

            var lastSeries = lastJv.JournalVoucherHeaderNo!;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = int.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");

        }

        public override async Task<FilprideJournalVoucherHeader?> GetAsync(Expression<Func<FilprideJournalVoucherHeader, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier!.Supplier)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideJournalVoucherHeader>> GetAllAsync(Expression<Func<FilprideJournalVoucherHeader, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideJournalVoucherHeader> query = dbSet
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier!.Supplier);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task PostAsync(FilprideJournalVoucherHeader header,
            IEnumerable<FilprideJournalVoucherDetail> details,
            CancellationToken cancellationToken = default)
        {
            #region --General Ledger Book Recording(GL)--

            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var ledgers = new List<FilprideGeneralLedgerBook>();

            foreach (var detail in details)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == detail.AccountNo) ?? throw new ArgumentException($"Account title '{detail.AccountNo}' not found.");
                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = header.Date,
                        Reference = header.JournalVoucherHeaderNo!,
                        Description = header.Particulars,
                        AccountId = account.AccountId,
                        AccountNo = account.AccountNumber,
                        AccountTitle = account.AccountName,
                        Debit = detail.Debit,
                        Credit = detail.Credit,
                        Company = header.Company,
                        CreatedBy = header.CreatedBy!,
                        CreatedDate = header.CreatedDate,
                        SubAccountType = detail.SubAccountType,
                        SubAccountId = detail.SubAccountId,
                        SubAccountName = detail.SubAccountName,
                    }
                );
            }

            if (!IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

            #endregion --General Ledger Book Recording(GL)--

            #region --Journal Book Recording(JV)--

            var journalBook = new List<FilprideJournalBook>();
            foreach (var detail in details)
            {
                journalBook.Add(
                    new FilprideJournalBook
                    {
                        Date = header.Date,
                        Reference = header.JournalVoucherHeaderNo!,
                        Description = header.Particulars,
                        AccountTitle = detail.AccountNo + " " + detail.AccountName,
                        Debit = detail.Debit,
                        Credit = detail.Credit,
                        Company = header.Company,
                        CreatedBy = header.CreatedBy,
                        CreatedDate = header.CreatedDate
                    }
                );
            }

            await _db.FilprideJournalBooks.AddRangeAsync(journalBook, cancellationToken);

            #endregion --Journal Book Recording(JV)--

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
