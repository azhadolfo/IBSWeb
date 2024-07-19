using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MasterFile
{
    public class StationRepository : Repository<MobilityStation>, IStationRepository
    {
        private ApplicationDbContext _db;

        public StationRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public string GenerateFolderPath(string stationName)
        {
            string formattedStationName = stationName.ToUpper().Replace(" ", "_");
            return $"D:\\FlowMeter\\Stations\\{formattedStationName}";
        }

        public async Task<bool> IsPosCodeExistAsync(string posCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityStations
                .AnyAsync(s => s.PosCode == posCode, cancellationToken);
        }

        public async Task<bool> IsStationCodeExistAsync(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityStations
                .AnyAsync(s => s.StationCode == stationCode, cancellationToken);
        }

        public async Task<bool> IsStationNameExistAsync(string stationName, CancellationToken cancellationToken = default)
        {
            return await _db.MobilityStations
                .AnyAsync(s => s.StationName == stationName, cancellationToken);
        }

        public async Task UpdateAsync(MobilityStation model, CancellationToken cancellationToken = default)
        {
            MobilityStation existingStation = await _db.MobilityStations
                .FindAsync(model.StationId, cancellationToken) ?? throw new InvalidOperationException($"Station with id '{model.StationId}' not found.");

            existingStation.PosCode = model.PosCode;
            existingStation.StationCode = model.StationCode;
            existingStation.StationName = model.StationName;
            existingStation.Initial = model.Initial;

            if (_db.ChangeTracker.HasChanges())
            {
                existingStation.EditedBy = model.EditedBy;
                existingStation.EditedDate = DateTime.Now;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}