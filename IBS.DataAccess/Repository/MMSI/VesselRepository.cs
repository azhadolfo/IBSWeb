using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI.IRepository;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.MMSI
{
    public class VesselRepository : Repository<MMSIVessel>, IVesselRepository
    {
        private readonly ApplicationDbContext _db;

        public VesselRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMMSIVesselsSelectList(CancellationToken cancellationToken = default)
        {
            var vessels = await _db.MMSIVessels.OrderBy(s => s.VesselName).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselName
            }).ToListAsync(cancellationToken);

            return vessels;
        }
    }
}
