using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IBankAccountRepository : IRepository<FilprideBankAccount>
    {
        Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);

        Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default);
    }
}