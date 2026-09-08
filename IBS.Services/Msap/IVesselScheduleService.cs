using IBS.Models.Msap;
using IBS.Utility.Helpers;

namespace IBS.Services.Msap
{
    public interface IVesselScheduleService
    {
        Task<ServiceResult<int>> CreateAsync(VesselSchedule model, string username, CancellationToken ct = default);
        Task<ServiceResult> UpdateAsync(VesselSchedule model, string username, CancellationToken ct = default);
        Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken ct = default);
        Task<VesselSchedule?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<VesselSchedule>> GetSchedulesAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
        Task<List<ScheduleConflict>> CheckConflictsAsync(VesselSchedule schedule, CancellationToken ct = default);
    }

    public class ScheduleConflict
    {
        public string Type { get; set; } = ""; // "Terminal" or "Tugboat"
        public string Message { get; set; } = "";
        public int ConflictingScheduleId { get; set; }
        public string? ConflictingVessel { get; set; }
        public DateTime ConflictStart { get; set; }
        public DateTime ConflictEnd { get; set; }
    }
}
