using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Mobility.IRepository;
using IBS.Models.Mobility.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Mobility
{
    public class ServiceRepository : Repository<MobilityService>, IServiceRepository
    {
        private readonly ApplicationDbContext _db;

        public ServiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GetLastNumber(string stationCode, CancellationToken cancellationToken = default)
        {
            var lastNumber = await _db
                .MobilityServices
                .Where(s => s.StationCode == stationCode)
                .OrderByDescending(s => s.ServiceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber == null || !int.TryParse(lastNumber.ServiceNo, out int serviceNo))
            {
                return "2001";
            }

            return (serviceNo + 1).ToString();

        }

        public async Task<bool> IsServicesExist(string serviceName, string company, CancellationToken cancellationToken = default)
        {
            return await _db.FilprideServices
                .AnyAsync(c => c.Company == company && c.Name == serviceName, cancellationToken);
        }
    }
}
