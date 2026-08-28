using System.Globalization;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ProvisionalReceiptRepository : Repository<FilprideProvisionalReceipt>, IProvisionalReceiptRepository
    {
        private readonly ApplicationDbContext _db;

        public ProvisionalReceiptRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GenerateSeriesNumberAsync(string company, string type, CancellationToken cancellationToken = default)
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
            var lastCr = await _db
                .FilprideProvisionalReceipts
                .AsNoTracking()
                .OrderByDescending(x => x.SeriesNumber.Length)
                .ThenByDescending(x => x.SeriesNumber)
                .FirstOrDefaultAsync(x =>
                    
                    x.Type == nameof(DocumentType.Documented),
                    cancellationToken);

            if (lastCr == null)
            {
                return "PR0000000001";
            }

            var lastSeries = lastCr.SeriesNumber;
            var numericPart = lastSeries.Substring(2);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
        }

        private async Task<string> GenerateCodeForUnDocumented(string company, CancellationToken cancellationToken = default)
        {
            var lastCr = await _db
                .FilprideProvisionalReceipts
                .AsNoTracking()
                .OrderByDescending(x => x.SeriesNumber.Length)
                .ThenByDescending(x => x.SeriesNumber)
                .FirstOrDefaultAsync(x =>
                        
                        x.Type == nameof(DocumentType.Undocumented),
                    cancellationToken);

            if (lastCr == null)
            {
                return "PRU000000001";
            }

            var lastSeries = lastCr.SeriesNumber;
            var numericPart = lastSeries.Substring(3);
            var incrementedNumber = long.Parse(numericPart) + 1;

            return lastSeries.Substring(0, 3) + incrementedNumber.ToString("D9");
        }

        public async Task DepositAsync(FilprideProvisionalReceipt provisionalReceipt, CancellationToken cancellationToken = default)
        {
            var ledgers = new List<FilprideGeneralLedgerBook>();
            var accountTitlesDto = await GetListOfAccountTitleDto(cancellationToken);
            var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100")
                                  ?? throw new ArgumentException("Account title '101010100' not found.");

            var employeeName = provisionalReceipt.Supplier.SupplierName;

            var description = $"PR Ref collected from {employeeName} Check No. {provisionalReceipt.CheckNo} issued by {provisionalReceipt.BankAccountNo} {provisionalReceipt.BankAccountName}";

            ledgers.Add(
                new FilprideGeneralLedgerBook
                {
                    Date = provisionalReceipt.TransactionDate,
                    Reference = provisionalReceipt.SeriesNumber,
                    Description = description,
                    AccountId = cashInBankTitle.AccountId,
                    AccountNo = cashInBankTitle.AccountNumber,
                    AccountTitle = cashInBankTitle.AccountName,
                    Debit = provisionalReceipt.CashAmount + provisionalReceipt.CheckAmount + provisionalReceipt.ManagersCheckAmount,
                    Credit = 0,
                    CreatedBy = provisionalReceipt.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SubAccountType = SubAccountType.BankAccount,
                    SubAccountId = provisionalReceipt.BankId,
                    SubAccountName = provisionalReceipt.BankId.HasValue
                        ? $"{provisionalReceipt.BankAccountNo} {provisionalReceipt.BankAccountName}"
                        : null,
                    ModuleType = nameof(ModuleType.Collection)
                }
            );

            ledgers.Add(
                new FilprideGeneralLedgerBook
                {
                    Date = provisionalReceipt.TransactionDate,
                    Reference = provisionalReceipt.SeriesNumber,
                    Description = description,
                    AccountId = cashInBankTitle.AccountId,
                    AccountNo = cashInBankTitle.AccountNumber,
                    AccountTitle = cashInBankTitle.AccountName,
                    Debit = 0,
                    Credit = provisionalReceipt.CashAmount + provisionalReceipt.CheckAmount + provisionalReceipt.ManagersCheckAmount,
                    CreatedBy = provisionalReceipt.PostedBy!,
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    ModuleType = nameof(ModuleType.Collection)
                }
            );

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task ReturnedCheck(string prNo, string company, string userName, CancellationToken cancellationToken = default)
        {
            var originalEntries = await _db.FilprideGeneralLedgerBooks
                .Where(x => x.Reference == prNo
)
                .ToListAsync(cancellationToken);

            var reversalEntries = new List<FilprideGeneralLedgerBook>();
            var dateToday = DateTimeHelper.GetCurrentPhilippineTime();

            foreach (var originalEntry in originalEntries)
            {
                var reversalEntry = new FilprideGeneralLedgerBook
                {
                    Reference = originalEntry.Reference,
                    Date = DateOnly.FromDateTime(dateToday),
                    AccountNo = originalEntry.AccountNo,
                    AccountTitle = originalEntry.AccountTitle,
                    Description = "Reversal of entries due to returned checks.",
                    Debit = originalEntry.Credit,
                    Credit = originalEntry.Debit,
                    CreatedBy = userName,
                    CreatedDate = dateToday,
                    IsPosted = true,
                    AccountId = originalEntry.AccountId,
                    SubAccountType = originalEntry.SubAccountType,
                    SubAccountId = originalEntry.SubAccountId,
                    SubAccountName = originalEntry.SubAccountName,
                    ModuleType = originalEntry.ModuleType,
                };

                reversalEntries.Add(reversalEntry);
            }

            await _db.FilprideGeneralLedgerBooks.AddRangeAsync(reversalEntries, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public override async Task<FilprideProvisionalReceipt?> GetAsync(Expression<Func<FilprideProvisionalReceipt, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(x => x.Supplier)
                .Include(x => x.BankAccount)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override IQueryable<FilprideProvisionalReceipt> GetAllQuery(Expression<Func<FilprideProvisionalReceipt, bool>>? filter = null)
        {
            IQueryable<FilprideProvisionalReceipt> query = dbSet
                .Include(x => x.Supplier)
                .Include(x => x.BankAccount)
                .AsSplitQuery()
                .AsNoTracking();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query;
        }

        public override async Task<IEnumerable<FilprideProvisionalReceipt>> GetAllAsync(Expression<Func<FilprideProvisionalReceipt, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideProvisionalReceipt> query = dbSet
                .Include(x => x.Supplier)
                .Include(x => x.BankAccount);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
