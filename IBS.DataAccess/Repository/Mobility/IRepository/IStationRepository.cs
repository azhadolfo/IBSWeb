using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;

namespace IBS.DataAccess.Repository.Mobility.IRepository
{
    public interface IStationRepository : IRepository<MobilityStation>
    {
        Task<bool> IsStationCodeExistAsync(string stationCode, CancellationToken cancellationToken = default);

        Task<bool> IsStationNameExistAsync(string stationName, CancellationToken cancellationToken = default);

        Task<bool> IsPosCodeExistAsync(string posCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(MobilityStation model, CancellationToken cancellationToken = default);

        string GenerateFolderPath(string stationName);
    }
}