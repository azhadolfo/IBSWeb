using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class PostedPeriodController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<PostedPeriodController> _logger;

        public PostedPeriodController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager, ILogger<PostedPeriodController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new PostedPeriodViewModel
            {
                AvailableModules = await GetAvailableModulesAsync(),
                PostedPeriods = await GetPostedPeriodsAsync()
            };

            return View(viewModel);
        }

        private async Task<List<ModuleSelectItem>> GetAvailableModulesAsync()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Get all modules
            var allModules = Enum.GetNames(typeof(Module)).ToList();

            // Get already posted modules for current period
            var postedModules = await _dbContext.PostedPeriods
                .Where(p => p.Month == currentMonth && p.Year == currentYear)
                .Select(p => p.Module)
                .ToListAsync();

            return allModules
                .Where(module => !postedModules.Contains(module))
                .Select(module => new ModuleSelectItem { Value = module, Text = module })
                .ToList();
        }

        private async Task<List<PostedPeriod>> GetPostedPeriodsAsync()
        {
            return await _dbContext.PostedPeriods
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ThenBy(p => p.Module)
                .ToListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostPeriod(PostPeriodRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the validation errors.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = User.Identity!.Name!;
                var postedPeriods = new List<PostedPeriod>();

                foreach (var module in request.SelectedModules)
                {
                    // Check if period already exists
                    var existingPeriod = await _dbContext.PostedPeriods
                        .FirstOrDefaultAsync(p => p.Module == module &&
                                           p.Month == request.Month &&
                                           p.Year == request.Year);

                    if (existingPeriod != null)
                    {
                        TempData["ErrorMessage"] = $"Period for {module} in {request.Month}/{request.Year} is already posted.";
                        return RedirectToAction(nameof(Index));
                    }

                    var postedPeriod = new PostedPeriod
                    {
                        Company = request.Company ?? "Filpride", // Default company
                        Module = module,
                        Month = request.Month,
                        Year = request.Year,
                        IsPosted = true,
                        PostedOn = DateTimeHelper.GetCurrentPhilippineTime(),
                        PostedBy = userId
                    };

                    postedPeriods.Add(postedPeriod);
                }

                _dbContext.PostedPeriods.AddRange(postedPeriods);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully posted {postedPeriods.Count} module(s) for period {request.Month}/{request.Year}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error posting periods: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Unpost a period (if needed)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnpostPeriod(int id)
        {
            try
            {
                var postedPeriod = await _dbContext.PostedPeriods.FindAsync(id);
                if (postedPeriod == null)
                {
                    TempData["ErrorMessage"] = "Posted period not found.";
                    return RedirectToAction(nameof(Index));
                }

                _dbContext.PostedPeriods.Remove(postedPeriod);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully unposted {postedPeriod.Module} for period {postedPeriod.Month}/{postedPeriod.Year}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error unposting period: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAvailableModules(int month, int year)
        {
            try
            {
                // Get all modules
                var allModules = Enum.GetNames(typeof(Module)).ToList();

                // Get already posted modules for specified period
                var postedModules = await _dbContext.PostedPeriods
                    .Where(p => p.Month == month && p.Year == year)
                    .Select(p => p.Module)
                    .ToListAsync();

                var availableModules = allModules
                    .Where(module => !postedModules.Contains(module))
                    .Select(module => new { value = module, text = module })
                    .ToList();

                return Json(availableModules);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
