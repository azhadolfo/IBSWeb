using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class BankAccountRepository : Repository<FilprideBankAccount>, IBankAccountRepository
    {
        private ApplicationDbContext _db;

        public BankAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => string.Equals(b.AccountName, accountName, StringComparison.CurrentCultureIgnoreCase));
        }

        public async Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => string.Equals(b.AccountNo, accountNo, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}