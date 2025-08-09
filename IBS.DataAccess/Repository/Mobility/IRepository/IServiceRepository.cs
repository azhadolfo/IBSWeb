using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IServiceRepository : IRepository<MobilityService>
    {
        Task<string> GetLastNumber(string stationCode, CancellationToken cancellationToken = default);
        Task<bool> IsServicesExist(string serviceName, CancellationToken cancellationToken = default);
    }
}
