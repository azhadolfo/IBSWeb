using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Filpride.IRepository;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Filpride
{
    public class ServiceRepository : Repository<FilprideService>, IServiceRepository
    {
        private ApplicationDbContext _db;

        public ServiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<string> GetLastNumber(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _db
                .FilprideServices
                .OrderByDescending(s => s.ServiceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber != null && int.TryParse(lastNumber.ServiceNo, out int serviceNo))
            {
                return (serviceNo + 1).ToString();
            }
            else
            {
                return "2001";
            }
        }

        public Task<bool> IsServicesExist(string serviceName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}