using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IBS.Services.Msap
{
    public class VesselScheduleService(
        IUnitOfWork unitOfWork,
        ILogger<VesselScheduleService> logger) : IVesselScheduleService
    {
        public async Task<ServiceResult<int>> CreateAsync(VesselSchedule model, string username, CancellationToken ct = default)
        {
            try
            {
                if (model.PlannedEnd <= model.PlannedStart)
                {
                    return ServiceResult<int>.Failure("Planned end must be after planned start.", ServiceResultStatus.ValidationError);
                }

                model.CreatedBy = username;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.VesselSchedule.AddAsync(model, ct);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Created vessel schedule (Vessel #{model.VesselId}, {model.PlannedStart:MM/dd HH:mm} – {model.PlannedEnd:MM/dd HH:mm})", "Vessel Schedule", model.VesselScheduleId), ct);
                await unitOfWork.SaveAsync(ct);

                return ServiceResult<int>.Success(model.VesselScheduleId, "Schedule created successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create vessel schedule");
                return ServiceResult<int>.Failure("Failed to create schedule. Please try again.");
            }
        }

        public async Task<ServiceResult> UpdateAsync(VesselSchedule model, string username, CancellationToken ct = default)
        {
            try
            {
                var existing = await unitOfWork.VesselSchedule.GetAsync(s => s.VesselScheduleId == model.VesselScheduleId, ct);
                if (existing == null)
                {
                    return ServiceResult.Failure("Schedule not found.", ServiceResultStatus.NotFound);
                }

                if (existing.Status == SD.VesselScheduleStatus.Completed || existing.Status == SD.VesselScheduleStatus.Cancelled)
                {
                    return ServiceResult.Failure("Cannot edit a completed or cancelled schedule.", ServiceResultStatus.ValidationError);
                }

                if (model.PlannedEnd <= model.PlannedStart)
                {
                    return ServiceResult.Failure("Planned end must be after planned start.", ServiceResultStatus.ValidationError);
                }

                existing.VesselId = model.VesselId;
                existing.PortId = model.PortId;
                existing.TerminalId = model.TerminalId;
                existing.PlannedStart = model.PlannedStart;
                existing.PlannedEnd = model.PlannedEnd;
                existing.RequiredTugCount = model.RequiredTugCount;
                existing.AssignedTugboatIds = model.AssignedTugboatIds;
                existing.VoyageNumber = model.VoyageNumber;
                existing.VesselType = model.VesselType;
                existing.Status = model.Status;
                existing.Notes = model.Notes;
                existing.EditedBy = username;
                existing.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Updated vessel schedule #{model.VesselScheduleId}", "Vessel Schedule", model.VesselScheduleId), ct);
                await unitOfWork.SaveAsync(ct);

                return ServiceResult.Success("Schedule updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update vessel schedule {Id}", model.VesselScheduleId);
                return ServiceResult.Failure("Failed to update schedule. Please try again.");
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id, string username, CancellationToken ct = default)
        {
            try
            {
                var existing = await unitOfWork.VesselSchedule.GetAsync(s => s.VesselScheduleId == id, ct);
                if (existing == null)
                {
                    return ServiceResult.Failure("Schedule not found.", ServiceResultStatus.NotFound);
                }

                await unitOfWork.VesselSchedule.RemoveAsync(existing, ct);
                await unitOfWork.AuditTrail.AddAsync(new AuditTrail(username, $"Deleted vessel schedule #{id}", "Vessel Schedule", id), ct);
                await unitOfWork.SaveAsync(ct);

                return ServiceResult.Success("Schedule deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete vessel schedule {Id}", id);
                return ServiceResult.Failure("Failed to delete schedule. Please try again.");
            }
        }

        public async Task<VesselSchedule?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await unitOfWork.VesselSchedule.GetAsync(s => s.VesselScheduleId == id, ct);
        }

        public async Task<IEnumerable<VesselSchedule>> GetSchedulesAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            return await unitOfWork.VesselSchedule.GetSchedulesWithDetailsAsync(from, to, ct);
        }

        public async Task<List<ScheduleConflict>> CheckConflictsAsync(VesselSchedule schedule, CancellationToken ct = default)
        {
            var conflicts = new List<ScheduleConflict>();
            var from = schedule.PlannedStart.AddHours(-1);
            var to = schedule.PlannedEnd.AddHours(1);
            var allSchedules = await GetSchedulesAsync(from, to, ct);
            var others = allSchedules.Where(s => s.VesselScheduleId != schedule.VesselScheduleId).ToList();

            foreach (var s in others.Where(s =>
                s.TerminalId == schedule.TerminalId &&
                s.PlannedStart < schedule.PlannedEnd &&
                s.PlannedEnd > schedule.PlannedStart))
            {
                conflicts.Add(new ScheduleConflict
                {
                    Type = "Terminal",
                    Message = $"Terminal '{s.Terminal.TerminalName}' occupied by '{s.Vessel.VesselName}'.",
                    ConflictingScheduleId = s.VesselScheduleId,
                    ConflictingVessel = s.Vessel.VesselName,
                    ConflictStart = s.PlannedStart,
                    ConflictEnd = s.PlannedEnd
                });
            }

            var tugboatIds = string.IsNullOrEmpty(schedule.AssignedTugboatIds)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(schedule.AssignedTugboatIds) ?? new();

            if (tugboatIds.Count > 0)
            {
                var allTugboats = await unitOfWork.Tugboat.GetAllAsync(t => tugboatIds.Contains(t.TugboatId), ct);
                var tugboatNames = allTugboats.ToDictionary(t => t.TugboatId, t => t.TugboatName);

                foreach (var s in others)
                {
                    var otherTugIds = string.IsNullOrEmpty(s.AssignedTugboatIds)
                        ? new List<int>()
                        : JsonSerializer.Deserialize<List<int>>(s.AssignedTugboatIds) ?? new();
                    if (otherTugIds.Count == 0)
                    {
                        continue;
                    }

                    var shared = tugboatIds.Intersect(otherTugIds).ToList();
                    if (shared.Count > 0 &&
                        s.PlannedStart < schedule.PlannedEnd &&
                        s.PlannedEnd > schedule.PlannedStart)
                    {
                        conflicts.Add(new ScheduleConflict
                        {
                            Type = "Tugboat",
                            Message = $"Tugboat(s) '{string.Join(", ", shared.Select(id => tugboatNames.GetValueOrDefault(id, $"#{id}")))}' assigned to '{s.Vessel.VesselName}'.",
                            ConflictingScheduleId = s.VesselScheduleId,
                            ConflictingVessel = s.Vessel.VesselName,
                            ConflictStart = s.PlannedStart,
                            ConflictEnd = s.PlannedEnd
                        });
                    }
                }
            }

            return conflicts;
        }
    }
}
