using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;

namespace IBS.DataAccess.Repository.Filpride.IRepository
{
    public interface IServiceRepository : IRepository<FilprideService>
    {
        Task<string> GetLastNumber(CancellationToken cancellationToken = default);

        Task<bool> IsServicesExist(string serviceName, string company, CancellationToken cancellationToken = default);
    }
}
