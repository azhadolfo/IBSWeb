using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI;
using IBS.Models.MMSI.MasterFile;
using IBS.Models.MMSI.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class ServiceRepository : Repository<MMSIService>, IServiceRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public ServiceRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIActivitiesServicesById(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> activitiesServices = await _dbContext.MMSIServices
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
