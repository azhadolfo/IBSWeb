using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IVesselScheduleRepository : IRepository<VesselSchedule>
    {
        Task<IEnumerable<VesselSchedule>> GetSchedulesWithDetailsAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    }
}