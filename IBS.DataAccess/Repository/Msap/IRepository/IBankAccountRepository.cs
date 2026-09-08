using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IMsapBankAccountRepository : IRepository<MsapBankAccount>
    {
        Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);

        Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default);
    }
}