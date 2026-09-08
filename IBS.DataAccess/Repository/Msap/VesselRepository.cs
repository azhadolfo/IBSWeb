using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class VesselRepository(ApplicationDbContext db): Repository<Vessel>(db), IVesselRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetMsapVesselsSelectList(CancellationToken cancellationToken = default)
        {
            var vessels = await _db.MsapVessels.OrderBy(s => s.VesselName).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselName
            }).ToListAsync(cancellationToken);

            return vessels;
        }

        public async Task<List<SelectListItem>> GetMsapVesselsById(CancellationToken cancellationToken = default)
        {
            var vessels = await _db.MsapVessels.OrderBy(s => s.VesselNumber).Select(s => new SelectListItem
            {
                Value = s.VesselId.ToString(),
                Text = s.VesselNumber + " " + s.VesselName + " " + s.VesselType
            }).ToListAsync(cancellationToken);

            return vessels;
        }
    }
}


