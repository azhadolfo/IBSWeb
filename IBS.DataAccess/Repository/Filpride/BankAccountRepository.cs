using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class BankAccountRepository : Repository<FilprideBankAccount>, IBankAccountRepository
    {
        private readonly ApplicationDbContext _db;

        public BankAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                 .Where(a => (company == nameof(Filpride) ? a.IsFilpride : a.IsMobility))
                 .Select(ba => new SelectListItem
                 {
                     Value = ba.BankAccountId.ToString(),
                     Text = ba.AccountName
                 })
                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => b.AccountName == accountName, cancellationToken);
        }

        public async Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => b.AccountNo == accountNo, cancellationToken);
        }
    }
}
