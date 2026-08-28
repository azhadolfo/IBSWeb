using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [Authorize(Roles = "Admin")]
    public class MonthlyPeriodController : Controller
    {
        private readonly ILogger<MonthlyPeriodController> _logger;

        private readonly IMonthlyClosureService _monthlyClosureService;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public MonthlyPeriodController(
            ILogger<MonthlyPeriodController> logger,
            IMonthlyClosureService monthlyClosureService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _monthlyClosureService = monthlyClosureService;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerMonthlyClosure(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync();

            if (companyClaim == null)
            {
                return BadRequest();
            }

            try
            {
                await _monthlyClosureService.CloseAsync(monthDate, companyClaim, User.Identity!.Name!, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(
                    GetUserFullName(),
                    $"Close the book for the month of {monthDate:MMM yyyy}",
                    "Monthly Period");

                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);


                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = $"Month of {monthDate:MMM yyyy} closed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to close period. Posted by: {Username}", GetUserFullName());
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerMonthlyOpening(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaim = await GetCompanyClaimAsync();

            if (companyClaim == null)
            {
                return BadRequest();
            }

            try
            {
                await _monthlyClosureService.OpenAsync(monthDate, companyClaim, User.Identity!.Name!, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(
                    GetUserFullName(),
                    $"Open the book for the month of {monthDate:MMM yyyy}",
                    "Monthly Period");

                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);


                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = $"Month of {monthDate:MMM yyyy} opened successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to open period. Open by: {Username}", GetUserFullName());
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
