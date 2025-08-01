using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class StationRepository : Repository<MobilityStation>, IStationRepository
    {
        private readonly ApplicationDbContext _db;

        public StationRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public string GenerateFolderPath(string stationName)
        {
            var formattedStationName = stationName.ToUpper().Replace(" ", "_");
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
            var existingStation = await _db.MobilityStations
                .FirstOrDefaultAsync(x => x.StationId == model.StationId, cancellationToken)
                                  ?? throw new InvalidOperationException($"Station with id '{model.StationId}' not found.");

            existingStation.PosCode = model.PosCode;
            existingStation.StationCode = model.StationCode;
            existingStation.StationName = model.StationName;
            existingStation.Initial = model.Initial;

            if (_db.ChangeTracker.HasChanges())
            {
                existingStation.EditedBy = model.EditedBy;
                existingStation.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("No data changes!");
            }
        }
    }
}
