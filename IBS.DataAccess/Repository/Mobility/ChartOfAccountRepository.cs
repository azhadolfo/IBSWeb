using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.DTOs;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ChartOfAccountRepository : Repository<MobilityChartOfAccount>, IChartOfAccountRepository
    {
        private readonly ApplicationDbContext _db;

        public ChartOfAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<MobilityChartOfAccount> GenerateAccount(MobilityChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default)
        {
            var existingCoa = await _db.MobilityChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == thirdLevel, cancellationToken)
                              ?? throw new InvalidOperationException($"Chart of account with number '{thirdLevel}' not found.");

            model.AccountType = existingCoa.AccountType;
            model.NormalBalance = existingCoa.NormalBalance;
            model.Level = existingCoa.Level + 1;
            model.Parent = existingCoa.AccountNumber;
            model.AccountNumber = await GenerateNumberAsync(thirdLevel, cancellationToken);

            return model;
        }

        public async Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default)
        {
            return await _db.MobilityChartOfAccounts
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
            return await _db.MobilityChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Parent == parentAcc)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public IEnumerable<ChartOfAccountDto> GetSummaryReportView(CancellationToken cancellationToken = default)
        {
            var query = from c in _db.MobilityChartOfAccounts
                        join gl in _db.MobilityGeneralLedgers on c.AccountNumber equals gl.AccountNumber into glGroup
                        from gl in glGroup.DefaultIfEmpty()
                        group new { c, gl } by new { c.Level, c.AccountNumber, c.AccountName, c.AccountType, c.Parent } into g
                        select new ChartOfAccountDto
                        {
                            Level = g.Key.Level,
                            AccountNumber = g.Key.AccountNumber,
                            AccountName = g.Key.AccountName,
                            AccountType = g.Key.AccountType,
                            Parent = g.Key.Parent,
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

        public async Task UpdateAsync(MobilityChartOfAccount model, CancellationToken cancellationToken = default)
        {
            var existingAccount = await _db.MobilityChartOfAccounts
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
            var lastAccount = await _db.MobilityChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .LastOrDefaultAsync(coa => coa.Parent == parent, cancellationToken);

            if (lastAccount == null)
            {
                return parent + "01";
            }

            var accountNo = int.Parse(lastAccount.AccountNumber!);
            var generatedNo = accountNo + 1;

            return generatedNo.ToString();

        }
    }
}
