using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class ServiceRepository : Repository<MMSIService>, IServiceRepository
    {
        private readonly ApplicationDbContext _db;

        public ServiceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            var activitiesServices = await _db.MMSIServices
                .OrderBy(s => s.ServiceNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.ServiceNumber + " " + s.ServiceName
                }).ToListAsync(cancellationToken);

            return activitiesServices;
        }
    }
}
