using IBS.Models;
using IBS.Models.Msap;
using IBS.Services.Msap;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class AuditTrailController(IAuditTrailService auditTrailService, JobOrderService jobOrderService, ILogger<AuditTrailController> logger): Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewBag.JobOrders = await jobOrderService.GetJobOrderSelectListAsync(cancellationToken);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPagedAuditTrails([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var (data, filtered, total) = await auditTrailService.GetPagedAuditTrailsAsync(parameters, cancellationToken);
            return Json(new
            {
                draw = parameters.Draw,
                recordsTotal = total,
                recordsFiltered = filtered,
                data
            });
        }

        public async Task<IActionResult> GetTimeline(int id, CancellationToken cancellationToken)
        {
            try
            {
                var timeline = await auditTrailService.GetJobOrderTimelineAsync(id, cancellationToken);
                return PartialView("_Timeline", timeline);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching timeline for Job Order {JobOrderId}", id);
                return StatusCode(500, "Internal server error while fetching timeline.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchJobOrders(string term, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var parameters = new DataTablesParameters
            {
                Search = new DataTablesSearch { Value = term },
                Length = 10,
                Start = 0
            };

            (IEnumerable<JobOrder> data, _, _) = await jobOrderService.GetPagedJobOrdersAsync(parameters, cancellationToken);

            var result = data.Select(jo => new
            {
                id = jo.JobOrderId,
                text = $"{jo.JobOrderNumber} - {jo.Vessel.VesselName}"
            });

            return Json(result);
        }
    }
}
