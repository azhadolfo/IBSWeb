using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.DTOs;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapChartOfAccountRepository(ApplicationDbContext db)
        : Repository<MsapChartOfAccount>(db), IMsapChartOfAccountRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<MsapChartOfAccount> GenerateAccount(MsapChartOfAccount model, string thirdLevel, CancellationToken cancellationToken = default)
        {
            var existingCoa = await _db.MsapChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == thirdLevel, cancellationToken)
                              ?? throw new InvalidOperationException($"Chart of account with number '{thirdLevel}' not found.");

            model.AccountType = existingCoa.AccountType;
            model.NormalBalance = existingCoa.NormalBalance;
            model.Level = existingCoa.Level + 1;
            model.ParentAccountId = existingCoa.AccountId;
            model.AccountNumber = await GenerateNumberAsync(model.ParentAccountId, thirdLevel, cancellationToken);

            return model;
        }

        public async Task<List<SelectListItem>> GetMainAccount(CancellationToken cancellationToken = default)
        {
            return await _db.MsapChartOfAccounts
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
            return await _db.MsapChartOfAccounts
                .OrderBy(c => c.AccountNumber)
                //.Where(c => c.Parent == parentAcc)
                .Select(c => new SelectListItem
                {
                    Value = c.AccountNumber,
                    Text = c.AccountNumber + " " + c.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(MsapChartOfAccount model, CancellationToken cancellationToken = default)
        {
            var existingAccount = await _db.MsapChartOfAccounts
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

        private async Task<string> GenerateNumberAsync(int? parentId, string thirdLevel, CancellationToken cancellationToken = default)
        {
            var lastAccount = await _db.MsapChartOfAccounts
                .OrderByDescending(c => c.AccountNumber!.Length)
                .ThenByDescending(c => c.AccountNumber)
                .FirstOrDefaultAsync(coa => coa.ParentAccountId == parentId, cancellationToken);

            if (lastAccount == null)
            {
                return thirdLevel + "01";
            }

            var accountNo = long.Parse(lastAccount.AccountNumber!);
            var generatedNo = accountNo + 1;

            return generatedNo.ToString();
        }

        public override async Task<MsapChartOfAccount?> GetAsync(Expression<Func<MsapChartOfAccount, bool>> filter, CancellationToken cancellationToken = default)
        {
            return await dbSet.Where(filter)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public override async Task<IEnumerable<MsapChartOfAccount>> GetAllAsync(Expression<Func<MsapChartOfAccount, bool>>? filter, CancellationToken cancellationToken = default)
        {
            IQueryable<MsapChartOfAccount> query = dbSet
                .Include(c => c.Children);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}