using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.Msap.IRepository;
using IBS.Models.Msap;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository.Msap
{
    public class VesselScheduleRepository(ApplicationDbContext db) : Repository<VesselSchedule>(db), IVesselScheduleRepository
    {
        private readonly ApplicationDbContext _db = db;

        public async Task<IEnumerable<VesselSchedule>> GetSchedulesWithDetailsAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            var query = _db.MsapVesselSchedules
                .Include(s => s.Vessel)
                .Include(s => s.Port)
                .Include(s => s.Terminal)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(s => s.PlannedEnd >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.PlannedStart <= to.Value);

            return await query
                .OrderBy(s => s.PlannedStart)
                .ToListAsync(ct);
        }
    }
}