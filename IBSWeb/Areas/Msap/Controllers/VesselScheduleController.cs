using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using IBS.Services.Msap;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    [Authorize]
    public class     VesselScheduleController(
        IUnitOfWork unitOfWork,
        IVesselScheduleService scheduleService) : Controller
    {
        public async Task<IActionResult> Index(DateTime? from, DateTime? to, CancellationToken ct)
        {
            from ??= DateTimeHelper.GetCurrentPhilippineTime().Date;
            to ??= from.Value.AddDays(14);

            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To = to.Value.ToString("yyyy-MM-dd");

            var schedules = await scheduleService.GetSchedulesAsync(from, to, ct);
            return View(schedules);
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleData(DateTime? from, DateTime? to, CancellationToken ct)
        {
            from ??= DateTimeHelper.GetCurrentPhilippineTime().Date;
            to ??= from.Value.AddDays(14);

            var schedules = await scheduleService.GetSchedulesAsync(from, to, ct);

            var tasks = schedules.Select(s =>
            {
                var tugIds = string.IsNullOrEmpty(s.AssignedTugboatIds)
                    ? new List<int>()
                    : JsonSerializer.Deserialize<List<int>>(s.AssignedTugboatIds) ?? new List<int>();

                return new
                {
                    id = s.VesselScheduleId.ToString(),
                    name = $"{s.Vessel.VesselName}",
                    start = s.PlannedStart.ToString("yyyy-MM-dd HH:mm"),
                    end = s.PlannedEnd.ToString("yyyy-MM-dd HH:mm"),
                    progress = s.Status == SD.VesselScheduleStatus.Completed ? 100
                        : s.Status == SD.VesselScheduleStatus.InProgress ? 50 : 0,
                    status = s.Status,
                    vessel = s.Vessel.VesselName,
                    port = s.Port.PortName,
                    terminal = s.Terminal.TerminalName,
                    tugCount = s.RequiredTugCount,
                    tugIds,
                    notes = s.Notes
                };
            }).ToList();

            return Json(new { tasks, from = from.Value.ToString("yyyy-MM-dd"), to = to.Value.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetScheduleList([FromForm] DataTablesParameters parameters, DateTime? dateFilter, CancellationToken ct)
        {
            var from = (dateFilter ?? DateTimeHelper.GetCurrentPhilippineTime()).Date;
            var to = from.AddDays(14);

            var schedules = await scheduleService.GetSchedulesAsync(from, to, ct);
            var total = schedules.Count();

            var data = schedules.Select(s => new
            {
                vesselScheduleId = s.VesselScheduleId,
                vessel = s.Vessel.VesselName,
                port = s.Port.PortName,
                terminal = s.Terminal.TerminalName,
                start = s.PlannedStart.ToString("yyyy-MM-dd HH:mm"),
                end = s.PlannedEnd.ToString("yyyy-MM-dd HH:mm"),
                tugCount = s.RequiredTugCount,
                status = s.Status
            }).ToList();

            return Json(new { draw = parameters.Draw, recordsTotal = total, recordsFiltered = total, data });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new VesselScheduleViewModel
            {
                PlannedStart = DateTimeHelper.GetCurrentPhilippineTime(),
                PlannedEnd = DateTimeHelper.GetCurrentPhilippineTime().AddHours(2)
            };

            await PopulateDropdownsAsync(vm, ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VesselScheduleViewModel vm, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var entity = MapToEntity(vm);
                var result = await scheduleService.CreateAsync(entity, User.Identity?.Name ?? "system", ct);

                if (result.IsSuccess)
                {
                    TempData["success"] = "Schedule created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result.Message ?? "Failed to create schedule.");
            }

            await PopulateDropdownsAsync(vm, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var entity = await scheduleService.GetByIdAsync(id, ct);
            if (entity == null)
            {
                return NotFound();
            }

            var vm = MapToViewModel(entity);
            await PopulateDropdownsAsync(vm, ct, entity.PortId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VesselScheduleViewModel vm, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var entity = MapToEntity(vm);
                entity.VesselScheduleId = vm.VesselScheduleId;

                var result = await scheduleService.UpdateAsync(entity, User.Identity?.Name ?? "system", ct);

                if (result.IsSuccess)
                {
                    TempData["success"] = "Schedule updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", result.Message ?? "Failed to update schedule.");
            }

            await PopulateDropdownsAsync(vm, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var entity = await unitOfWork.VesselSchedule.GetAsync(
                s => s.VesselScheduleId == id, ct);
            if (entity == null)
            {
                return NotFound();
            }

            // Load navigation properties manually
            var schedule = (await unitOfWork.VesselSchedule.GetSchedulesWithDetailsAsync(null, null, ct))
                .FirstOrDefault(s => s.VesselScheduleId == id);

            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await scheduleService.DeleteAsync(id, User.Identity?.Name ?? "system", ct);

            if (result.IsSuccess)
            {
                TempData["success"] = "Schedule deleted successfully.";
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetVesselVoyageType(int vesselId, CancellationToken ct)
        {
            var vessel = await unitOfWork.Vessel.GetAsync(v => v.VesselId == vesselId, ct);
            var voyageType = vessel?.VesselType == "FOREIGN" ? "Foreign" : "Local";
            return Json(voyageType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckConflicts([FromBody] ConflictCheckRequest request, CancellationToken ct)
        {
            var entity = new VesselSchedule
            {
                VesselScheduleId = request.VesselScheduleId,
                TerminalId = request.TerminalId,
                PlannedStart = request.PlannedStart,
                PlannedEnd = request.PlannedEnd,
                AssignedTugboatIds = request.SelectedTugboatIds?.Any() == true
                    ? JsonSerializer.Serialize(request.SelectedTugboatIds)
                    : null
            };

            var conflicts = await scheduleService.CheckConflictsAsync(entity, ct);
            return Json(new { hasConflicts = conflicts.Count > 0, conflicts });
        }

        [HttpGet]
        public async Task<IActionResult> GetTugboatOccupancy(DateTime date, CancellationToken ct)
        {
            var from = date.Date;
            var to = from.AddDays(1).AddTicks(-1);
            var schedules = await scheduleService.GetSchedulesAsync(from, to, ct);

            // Build lookup: tugboatId → assignments for this day
            var busyTugIds = new HashSet<int>();
            var assignmentsByTug = new Dictionary<int, List<object>>();

            foreach (var s in schedules)
            {
                var ids = string.IsNullOrEmpty(s.AssignedTugboatIds)
                    ? new List<int>()
                    : JsonSerializer.Deserialize<List<int>>(s.AssignedTugboatIds) ?? new();

                foreach (var tid in ids)
                {
                    busyTugIds.Add(tid);
                    if (!assignmentsByTug.ContainsKey(tid))
                    {
                        assignmentsByTug[tid] = new List<object>();
                    }

                    assignmentsByTug[tid].Add(new
                    {
                        vesselName = s.Vessel?.VesselName ?? "N/A",
                        start = s.PlannedStart.ToString("HH:mm"),
                        end = s.PlannedEnd.ToString("HH:mm"),
                        fullStart = s.PlannedStart.ToString("yyyy-MM-dd HH:mm"),
                        fullEnd = s.PlannedEnd.ToString("yyyy-MM-dd HH:mm"),
                        status = s.Status,
                        vesselScheduleId = s.VesselScheduleId
                    });
                }
            }

            // Load ALL tugboats
            var allTugboats = await unitOfWork.Tugboat.GetAllAsync(null, ct);

            var tugboats = allTugboats.Select(t =>
            {
                var hasAssignments = assignmentsByTug.TryGetValue(t.TugboatId, out var list);
                return new
                {
                    tugboatId = t.TugboatId,
                    tugboatName = t.TugboatName,
                    tugboatNumber = t.TugboatNumber,
                    available = !hasAssignments,
                    assignments = hasAssignments ? list : new List<object>()
                };
            }).OrderBy(t => t.tugboatName).ToList();

            return Json(new { date = date.ToString("yyyy-MM-dd"), tugboats });
        }

        [HttpGet]
        public async Task<IActionResult> GetOccupancyMatrix(DateTime date, CancellationToken ct)
        {
            var from = date.Date;
            var to = from.AddDays(1).AddTicks(-1);

            var schedules = await scheduleService.GetSchedulesAsync(from, to, ct);

            var terminals = schedules
                .Select(s => s.Terminal)
                .DistinctBy(t => t.TerminalId)
                .OrderBy(t => t.TerminalName)
                .ToList();

            var matrix = terminals.Select(t =>
            {
                var terminalSchedules = schedules.Where(s => s.TerminalId == t.TerminalId).ToList();
                var hours = Enumerable.Range(0, 24).Select(h =>
                {
                    var slotStart = date.Date.AddHours(h);
                    var slotEnd = slotStart.AddHours(1);
                    var sch = terminalSchedules.FirstOrDefault(s =>
                        s.PlannedStart < slotEnd && s.PlannedEnd > slotStart);
                    return new
                    {
                        hour = h,
                        vesselScheduleId = sch?.VesselScheduleId,
                        vessel = sch?.Vessel.VesselName,
                        status = sch?.Status
                    };
                }).ToList();
                return new
                {
                    terminalId = t.TerminalId,
                    terminalName = t.TerminalName,
                    hours
                };
            }).ToList();

            return Json(new { date = date.ToString("yyyy-MM-dd"), terminals = matrix });
        }

        [HttpGet]
        public async Task<IActionResult> GetTerminalsByPort(int portId, CancellationToken ct)
        {
            var terminals = await unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, ct);
            return Json(terminals.Select(t => new { value = t.TerminalId.ToString(), text = t.TerminalName }));
        }

        private async Task PopulateDropdownsAsync(VesselScheduleViewModel vm, CancellationToken ct, int? selectedPortId = null)
        {
            vm.Vessels = (await unitOfWork.Vessel.GetAllAsync(null, ct))
                .Select(v => new SelectListItem { Value = v.VesselId.ToString(), Text = $"{v.VesselName} ({v.VesselNumber})" })
                .ToList();
            vm.Ports = (await unitOfWork.Port.GetAllAsync(null, ct))
                .Select(p => new SelectListItem { Value = p.PortId.ToString(), Text = p.PortName })
                .ToList();
            vm.Tugboats = (await unitOfWork.Tugboat.GetAllAsync(null, ct))
                .Select(t => new SelectListItem { Value = t.TugboatId.ToString(), Text = $"{t.TugboatName} (#{t.TugboatNumber})" })
                .ToList();

            if (selectedPortId.HasValue)
            {
                vm.Terminals = (await unitOfWork.Terminal.GetAllAsync(t => t.PortId == selectedPortId.Value, ct))
                    .Select(t => new SelectListItem { Value = t.TerminalId.ToString(), Text = t.TerminalName })
                    .ToList();
            }
        }

        private static VesselSchedule MapToEntity(VesselScheduleViewModel vm)
        {
            return new VesselSchedule
            {
                VesselId = vm.VesselId,
                PortId = vm.PortId,
                TerminalId = vm.TerminalId,
                PlannedStart = vm.PlannedStart,
                PlannedEnd = vm.PlannedEnd,
                RequiredTugCount = vm.SelectedTugboatIds?.Count > 0 ? vm.SelectedTugboatIds.Count : vm.RequiredTugCount,
                AssignedTugboatIds = vm.SelectedTugboatIds?.Any() == true
                    ? JsonSerializer.Serialize(vm.SelectedTugboatIds)
                    : null,
                VoyageNumber = vm.VoyageNumber,
                VesselType = vm.VesselType,
                Status = vm.Status,
                Notes = vm.Notes,
                JobOrderId = vm.JobOrderId
            };
        }

        private static VesselScheduleViewModel MapToViewModel(VesselSchedule entity)
        {
            var tugIds = string.IsNullOrEmpty(entity.AssignedTugboatIds)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(entity.AssignedTugboatIds) ?? new List<int>();

            return new VesselScheduleViewModel
            {
                VesselScheduleId = entity.VesselScheduleId,
                VesselId = entity.VesselId,
                PortId = entity.PortId,
                TerminalId = entity.TerminalId,
                PlannedStart = entity.PlannedStart,
                PlannedEnd = entity.PlannedEnd,
                RequiredTugCount = entity.RequiredTugCount,
                SelectedTugboatIds = tugIds,
                VoyageNumber = entity.VoyageNumber,
                VesselType = entity.VesselType,
                Status = entity.Status,
                Notes = entity.Notes,
                JobOrderId = entity.JobOrderId
            };
        }
    }

    public class ConflictCheckRequest(
        List<int>? selectedTugboatIds,
        DateTime plannedEnd,
        DateTime plannedStart,
        int terminalId,
        int vesselScheduleId)
    {
        public int VesselScheduleId { get; } = vesselScheduleId;
        public int TerminalId { get; } = terminalId;
        public DateTime PlannedStart { get; } = plannedStart;
        public DateTime PlannedEnd { get; } = plannedEnd;
        public List<int>? SelectedTugboatIds { get; } = selectedTugboatIds;
    }
}
