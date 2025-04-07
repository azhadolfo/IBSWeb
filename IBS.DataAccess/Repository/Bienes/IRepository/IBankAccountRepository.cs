using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Bienes;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.DataAccess.Repository.Bienes.IRepository
{
    public interface IBankAccountRepository : IRepository<BienesBankAccount>
    {
        Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);

        Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default);

        Task<List<SelectListItem>> GetBankAccountListAsync(string company, CancellationToken cancellationToken = default);
    }
}
