using System.Security.Claims;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Services.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class ChartOfAccountController(
        IChartOfAccountService chartOfAccountService,
        UserManager<ApplicationUser> userManager,
        ILogger<ChartOfAccountController> logger)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return string.Empty;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value ?? string.Empty;
        }

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.ChartOfAccount))
            {
                return View("ExportIndex");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int parentId, string accountName, CancellationToken cancellationToken)
        {
            var result = await chartOfAccountService.CreateAsync(parentId, accountName, GetUserFullName(), await GetCompanyClaimAsync(), cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return Json(new { redirectUrl = Url.Action(nameof(Index)) });
            }

            return BadRequest(new { message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int accountId, string accountName, CancellationToken cancellationToken)
        {
            var result = await chartOfAccountService.UpdateAsync(accountId, accountName, GetUserFullName(), await GetCompanyClaimAsync(), cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return Json(new { redirectUrl = Url.Action(nameof(Index)) });
            }

            if (result.Status == ServiceResultStatus.NotFound)
            {
                return NotFound();
            }

            return BadRequest(new { message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetChartOfAccountList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var (data, totalRecords) = await chartOfAccountService.GetPagedListAsync(parameters, dateFrom, dateTo, cancellationToken);

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
                logger.LogError(ex, "Failed to get chart of accounts.");
                return Json(new { error = "Internal server error" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var excelBytes = await chartOfAccountService.ExportToExcelAsync(selectedRecord, cancellationToken);
                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ChartOfAccountList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
        {
            var coas = await chartOfAccountService.GetAllAsync(cancellationToken);
            return Json(coas);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChartOfAccountIds(CancellationToken cancellationToken)
        {
            var coaIds = await chartOfAccountService.GetAllIdsAsync(cancellationToken);
            return Json(coaIds);
        }
    }
}
