using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ICustomerRepository : IRepository<MobilityCustomer>
    {
        Task UpdateAsync(MobilityCustomer model, CancellationToken cancellationToken = default);

        Task<string> GenerateCodeAsync(string customerType, string stationCode, CancellationToken cancellationToken = default);
    }
}
