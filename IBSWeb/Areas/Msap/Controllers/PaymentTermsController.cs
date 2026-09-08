using IBS.Models.Msap.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class PaymentTermsController(
        IUnitOfWork unitOfWork,
        ILogger<SupplierController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaymentTerms([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await unitOfWork.Terms
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(s =>
                        s.TermsCode.ToLower().Contains(searchValue) ||
                        s.NumberOfDays.ToString("N0").Contains(searchValue) ||
                        s.NumberOfMonths.ToString("N0").Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();
                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get suppliers.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            PaymentTermsViewModel viewModel = new();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentTermsViewModel model, CancellationToken cancellationToken)
        {
            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.TermsCode = model.TermsCode.Trim();
                model.NumberOfDays = model.NumberOfDays;
                model.NumberOfMonths = model.NumberOfMonths;
                model.CreatedBy = getUserFullName;
                model.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.EditedBy = string.Empty;
                await unitOfWork.Terms.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(getUserFullName,
                    $"Create new Terms #{model.TermsCode}", "Terms");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create terms master file. Created by: {UserName}", getUserFullName);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                return NotFound();
            }

            var supplier = await unitOfWork.Terms.GetAsync(c => c.TermsCode == code, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(new PaymentTermsViewModel(supplier));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentTermsViewModel model, CancellationToken cancellationToken)
        {
            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = getUserFullName;
                await unitOfWork.Terms.UpdateAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new (getUserFullName,
                    $"Edited Terms #{model.TermsCode}", "Terms");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit supplier master file. Edited by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest();
            }

            var getUserFullName = GetUserFullName();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingTerms = await dbContext.Terms
                                        .FirstOrDefaultAsync(x => x.TermsCode == code, cancellationToken)
                                    ?? throw new InvalidOperationException("Terms with code not found.");

                dbContext.Remove(existingTerms);
                await dbContext.SaveChangesAsync(cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new (getUserFullName,
                    $"Deleted Terms #{code}", "Terms");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Terms deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,                    "Failed to delete terms master file. Deleted by: {UserName}",
                    getUserFullName);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(nameof(Index));
            }
        }
    }
}
