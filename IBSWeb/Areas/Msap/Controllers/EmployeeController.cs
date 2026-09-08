using IBS.Models.Msap.MasterFile;
using System.Security.Claims;
using IBS.Models;
using IBS.Services.Msap;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class EmployeeController(
        IEmployeeService employeeService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmployeeController> logger)
        : Controller
    {
        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name ?? "Unknown";
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var employees = await employeeService.GetAllAsync(cancellationToken);
            return View(employees);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new EmployeeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var result = await employeeService.CreateAsync(model, companyClaims, GetUserFullName(), cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetEmployeesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var (data, totalRecords) = await employeeService.GetPagedEmployeesAsync(parameters, cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get employee list.");
                return Json(new { error = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var employee = await employeeService.GetByIdAsync(id, cancellationToken);
            if (employee == null)
            {
                return NotFound();
            }

            return View(new EmployeeViewModel(employee));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel model, CancellationToken cancellationToken)
        {
            var result = await employeeService.UpdateAsync(model, GetUserFullName(), cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }
    }
}
