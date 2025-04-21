using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class BankAccountRepository : Repository<MobilityBankAccount>, IBankAccountRepository
    {
        private ApplicationDbContext _db;

        public BankAccountRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => b.AccountName == accountName);
        }

        public async Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideBankAccounts
                .AnyAsync(b => b.AccountNo == accountNo);
        }
    }
}
