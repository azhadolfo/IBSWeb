using IBS.Models.Msap.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    [Authorize]
    public class CompanyController(
        IUnitOfWork unitOfWork,
        ILogger<CompanyController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Company> companies = await unitOfWork
                .Company
                .GetAllAsync();
            return View(companies);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company model, CancellationToken cancellationToken)
        {
            bool companyExist = await unitOfWork
                .Company
                .IsCompanyExistAsync(model.CompanyName, cancellationToken);

            if (companyExist)
            {
                ModelState.AddModelError("CompanyName", "Company already exist.");
                return View(model);
            }

            bool tinNoExist = await unitOfWork
                .Company
                .IsTinNoExistAsync(model.CompanyTin, cancellationToken);

            if (tinNoExist)
            {
                ModelState.AddModelError("CompanyTin", "Tin number already exist.");
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CompanyCode = await unitOfWork.Company
                    .GenerateCodeAsync(cancellationToken);

                model.CreatedBy = GetUserFullName();
                await unitOfWork.Company.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (
                    GetUserFullName(), $"Created Company {model.CompanyCode}",
                    "Company" );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create company master file. Created by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCompanyList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await unitOfWork.Company
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(c =>
                        c.CompanyCode!.ToLower().Contains(searchValue) ||
                        c.CompanyName.ToLower().Contains(searchValue) ||
                        c.CompanyAddress.ToLower().Contains(searchValue) ||
                        c.CompanyTin.ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order is { Count: > 0 })
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
                logger.LogError(ex, "Failed to get company.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Company model, CancellationToken cancellationToken)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = GetUserFullName();
                await unitOfWork.Company.UpdateAsync(model, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (
                    GetUserFullName(), $"Edited Company {model.CompanyCode}",
                    "Company");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in updating company");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await unitOfWork.Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                company.IsActive = true;
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (
                    GetUserFullName(), $"Activated Company {company.CompanyCode}",
                    "Company" );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company activated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Activate), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                company.IsActive = false;
                await unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (
                    GetUserFullName(), $"Deactivated Company {company.CompanyCode}",
                    "Company");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company deactivated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Deactivate), new { id });
            }
        }
    }
}
