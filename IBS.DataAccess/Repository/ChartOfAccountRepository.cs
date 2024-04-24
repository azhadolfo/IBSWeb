using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class ChartOfAccountRepository : Repository<ChartOfAccount>, IChartOfAccountRepository
    {
        private ApplicationDbContext _db;

        public ChartOfAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<ChartOfAccount> GenerateAccount(ChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default)
        {
            ChartOfAccount existingCoa = await _db.ChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == thirdLevel, cancellationToken) ?? throw new InvalidOperationException($"Chart of account with number '{thirdLevel}' not found.");

            model.AccountType = existingCoa.AccountType;
            model.AccountCategory = existingCoa.AccountCategory;
            model.Level = existingCoa.Level + 1;
            model.Parent = existingCoa.AccountNumber;
            model.AccountNumber = await GenerateNumberAsync(thirdLevel, cancellationToken);

            return model;
        }

        public async Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Level == 1)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMemberAccount(string parentNo, CancellationToken cancellationToken = default)
        {
            return await _db.ChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .Where(c => c.Parent == parentNo)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(ChartOfAccount model, CancellationToken cancellationToken = default)
        {
            ChartOfAccount existingAccount = await _db.ChartOfAccounts
                .FindAsync(model.AccountId, cancellationToken) ?? throw new InvalidOperationException($"Account with id '{model.AccountId}' not found.");

            existingAccount.AccountName = model.AccountName;

            if (_db.ChangeTracker.HasChanges())
            {
                existingAccount.EditedBy = model.EditedBy;
                existingAccount.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }

        private async Task<string> GenerateNumberAsync(string parent, CancellationToken cancellationToken = default)
        {
            ChartOfAccount? lastAccount = await _db.ChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                .LastOrDefaultAsync(coa => coa.Parent == parent, cancellationToken);

            if (lastAccount != null)
            {
                var accountNo = Int32.Parse(lastAccount.AccountNumber);
                var generatedNo = accountNo + 1;

                return generatedNo.ToString();
            }
            else
            {
                return parent + "01";
            }
        }
    }
}
