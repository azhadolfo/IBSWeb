using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface ISupplierRepository : IRepository<MobilitySupplier>
    {
        Task<string> GenerateCodeAsync(string stationCodeClaims, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilitySupplier model, CancellationToken cancellationToken = default);
    }
}
