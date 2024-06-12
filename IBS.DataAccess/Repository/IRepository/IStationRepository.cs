using IBS.Models.MasterFile;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<bool> IsStationCodeExistAsync(string stationCode, CancellationToken cancellationToken = default);

        Task<bool> IsStationNameExistAsync(string stationName, CancellationToken cancellationToken = default);

        Task<bool> IsPosCodeExistAsync(string posCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(Station model, CancellationToken cancellationToken = default);

        string GenerateFolderPath(string stationName);
    }
}