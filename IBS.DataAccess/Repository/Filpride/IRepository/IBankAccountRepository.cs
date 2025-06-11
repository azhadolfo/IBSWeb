using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IBankAccountRepository : IRepository<FilprideBankAccount>
    {
        Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);

        Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default);
    }
}
