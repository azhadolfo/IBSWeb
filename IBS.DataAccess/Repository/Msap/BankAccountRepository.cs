using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class MsapBankAccountRepository(ApplicationDbContext db): Repository<MsapBankAccount>(db), IMsapBankAccountRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default)
        {
            return await _db.MsapBankAccounts
                 .Select(ba => new SelectListItem
                 {
                     Value = ba.BankAccountId.ToString(),
                     Text = ba.AccountName
                 })
                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default)
        {
            return await _db.MsapBankAccounts
                .AnyAsync(b => b.AccountName == accountName, cancellationToken);
        }

        public async Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default)
        {
            return await _db.MsapBankAccounts
                .AnyAsync(b => b.AccountNo == accountNo, cancellationToken);
        }
    }
}