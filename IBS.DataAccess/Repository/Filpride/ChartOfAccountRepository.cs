using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.DTOs;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ChartOfAccountRepository : Repository<FilprideChartOfAccount>, IChartOfAccountRepository
    {
        private readonly ApplicationDbContext _db;

        public ChartOfAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<FilprideChartOfAccount> GenerateAccount(FilprideChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default)
        {
            var existingCoa = await _db.FilprideChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == thirdLevel, cancellationToken) ?? throw new InvalidOperationException($"Chart of account with number '{thirdLevel}' not found.");

            model.AccountType = existingCoa.AccountType;
            model.NormalBalance = existingCoa.NormalBalance;
            model.Level = existingCoa.Level + 1;
            model.ParentAccountId = existingCoa.AccountId;
            model.AccountNumber = await GenerateNumberAsync(thirdLevel, cancellationToken);

            return model;
        }

        public async Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Level == 1)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMemberAccount(string parentAcc, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                //.Where(c => c.Parent == parentAcc)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default)
        {
            var query = from c in _db.FilprideChartOfAccounts
                        join gl in _db.FilprideGeneralLedgerBooks on c.AccountNumber equals gl.AccountNo into glGroup
                        from gl in glGroup.DefaultIfEmpty()
                        group new { c, gl } by new { c.Level, c.AccountNumber, c.AccountName, c.AccountType, } into g
                        select new ChartOfAccountDto
                        {
                            Level = g.Key.Level,
                            AccountNumber = g.Key.AccountNumber,
                            AccountName = g.Key.AccountName,
                            AccountType = g.Key.AccountType,
                            Debit = g.Sum(x => x.gl.Debit),
                            Credit = g.Sum(x => x.gl.Credit),
                            Balance = g.Sum(x => x.gl.Debit) - g.Sum(x => x.gl.Credit),
                            Children = new List<ChartOfAccountDto>()
                        };

            // Dictionary to store account information by level and account number (key)
            var accountDictionary = query.ToDictionary(x => new { x.Level, x.AccountNumber }, x => x);

            // Loop through all levels (ascending order to include level 1)
            foreach (var level in query.Select(x => x.Level).Distinct().OrderByDescending(x => x))
            {
                // Loop through accounts within the current level
                foreach (var account in accountDictionary.Where(x => x.Key.Level == level))
                {
                    // UpdateAsync parent account if it exists and handle potential null reference
                    if (account.Value.Parent != null && accountDictionary.TryGetValue(new { Level = level - 1, AccountNumber = account.Value.Parent }, out var parentAccount))
                    {
                        parentAccount.Debit += account.Value.Debit;
                        parentAccount.Credit += account.Value.Credit;
                        parentAccount.Balance += account.Value.Balance;
                        parentAccount.Children!.Add(account.Value);
                    }
                }
            }

            // Return the modified accounts
            return accountDictionary.Values.Where(x => x.Level == 1);
        }

        public async Task UpdateAsync(FilprideChartOfAccount model, CancellationToken cancellationToken = default)
        {
            var existingAccount = await _db.FilprideChartOfAccounts
                .FirstOrDefaultAsync(x => x.AccountId == model.AccountId, cancellationToken)
                                  ?? throw new InvalidOperationException($"Account with id '{model.AccountId}' not found.");

            existingAccount.AccountName = model.AccountName;

            if (_db.ChangeTracker.HasChanges())
            {
                existingAccount.EditedBy = model.EditedBy;
                existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        private async Task<string> GenerateNumberAsync(string parent, CancellationToken cancellationToken = default)
        {
            var lastAccount = await _db.FilprideChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                //.LastOrDefaultAsync(coa => coa.Parent == parent, cancellationToken);
                .LastOrDefaultAsync(cancellationToken);

            if (lastAccount != null)
            {
                var accountNo = int.Parse(lastAccount.AccountNumber!);
                var generatedNo = accountNo + 1;

                return generatedNo.ToString();
            }

            return parent + "01";
        }

        public override async Task<FilprideChartOfAccount?> GetAsync(Expression<Func<FilprideChartOfAccount, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<FilprideChartOfAccount>> GetAllAsync(Expression<Func<FilprideChartOfAccount, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<FilprideChartOfAccount> query = dbSet
                .Include(c => c.Children);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
