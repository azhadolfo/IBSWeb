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
    public class VesselRepository : Repository<MMSIVessel>, IVesselRepository
    {
        public readonly ApplicationDbContext _dbContext;

        public VesselRepository (ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> GetMMSIVesselsSelectList(CancellationToken cancellationToken = default)
        {
            List<SelectListItem> vessels = await _dbContext.MMSIVessels.OrderBy(s => s.VesselName).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselName
            }).ToListAsync(cancellationToken);

            return vessels;
        }
    }
}
