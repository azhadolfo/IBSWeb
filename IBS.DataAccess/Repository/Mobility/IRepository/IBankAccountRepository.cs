using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IBankAccountRepository : IRepository<MobilityBankAccount>
    {
        Task<bool> IsBankAccountNameExist(string accountName, CancellationToken cancellationToken = default);
        Task<bool> IsBankAccountNoExist(string accountNo, CancellationToken cancellationToken = default);
    }
}
