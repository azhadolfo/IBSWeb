using IBS.Models;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [Authorize(Roles = "Admin")]
    public class MonthlyCloseController : Controller
    {
        private readonly ILogger<MonthlyCloseController> _logger;

        private readonly IMonthlyClosureService _monthlyClosureService;

        private readonly UserManager<ApplicationUser> _userManager;

        public MonthlyCloseController(ILogger<MonthlyCloseController> logger,
            IMonthlyClosureService monthlyClosureService,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _monthlyClosureService = monthlyClosureService;
            _userManager = userManager;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TriggerMonthlyClosure(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync();

            if (companyClaim == null)
            {
                return BadRequest();
            }

            try
            {
                await _monthlyClosureService.Execute(monthDate, companyClaim, User.Identity!.Name!, cancellationToken);

                TempData["success"] = $"Month of {monthDate:MMM yyyy} closed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to close period. Posted by: {Username}", User.Identity!.Name);
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
