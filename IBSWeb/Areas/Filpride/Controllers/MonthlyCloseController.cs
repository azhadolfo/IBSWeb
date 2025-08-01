using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [Authorize(Roles = "Admin")]
    public class MonthlyCloseController : Controller
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public MonthlyCloseController(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TriggerMonthlyClosure(DateTime monthDate)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();

                var dataMap = new JobDataMap
                {
                    { "monthDate", monthDate },
                };

                var jobKey = JobKey.Create(nameof(MonthlyClosureService));

                await scheduler.TriggerJob(jobKey, dataMap);

                TempData["success"] = $"Month of {monthDate:MMM yyyy} closed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
