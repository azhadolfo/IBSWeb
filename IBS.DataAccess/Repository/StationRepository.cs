using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository
{
    public class StationRepository : Repository<Station>, IStationRepository
    {
        private ApplicationDbContext _db;

        public StationRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public string GenerateFolderPath(string stationName)
        {
            string formattedStationName = stationName.ToUpper().Replace(" ", "-");
            return $"G:\\Other computers\\{formattedStationName}";
        }

        public async Task<bool> IsPosCodeExistAsync(string posCode, CancellationToken cancellationToken = default)
        {
            return await _db.Stations
                .AnyAsync(s => s.PosCode == posCode, cancellationToken);

        }

        public async Task<bool> IsStationCodeExistAsync(string stationCode, CancellationToken cancellationToken = default)
        {
            return await _db.Stations
                .AnyAsync(s => s.StationCode == stationCode, cancellationToken);
        }

        public async Task<bool> IsStationNameExistAsync(string stationName, CancellationToken cancellationToken = default)
        {
            return await _db.Stations
                .AnyAsync(s => s.StationName == stationName, cancellationToken);
        }

        public async Task UpdateAsync(Station model, CancellationToken cancellationToken = default)
        {
            Station existingStation = await _db.Stations
                .FindAsync(model.StationId, cancellationToken) ?? throw new InvalidOperationException($"Station with id '{model.StationId}' not found.");

            existingStation.PosCode = model.PosCode;
            existingStation.StationCode = model.StationCode;
            existingStation.StationName = model.StationName;
            existingStation.Initial = model.Initial;

            if (_db.ChangeTracker.HasChanges())
            {
                existingStation.EditedBy = "Ako";
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
