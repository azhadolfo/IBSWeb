using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CompanyController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public CompanyController(IUnitOfWork unitOfWork, ILogger<CompanyController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
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

        public async Task<IActionResult> Index()
        {
            IEnumerable<Company> companies = await _unitOfWork
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
        public async Task<IActionResult> Create(Company model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            bool companyExist = await _unitOfWork
                .Company
                .IsCompanyExistAsync(model.CompanyName, cancellationToken);

            if (companyExist)
            {
                ModelState.AddModelError("CompanyName", "Company already exist.");
                return View(model);
            }

            bool tinNoExist = await _unitOfWork
                .Company
                .IsTinNoExistAsync(model.CompanyTin, cancellationToken);

            if (tinNoExist)
            {
                ModelState.AddModelError("CompanyTin", "Tin number already exist.");
                return View(model);
            }

            try
            {
                model.CompanyCode = await _unitOfWork
                    .Company
                    .GenerateCodeAsync(cancellationToken);

                model.CreatedBy = _userManager.GetUserName(User);
                await _unitOfWork.Company.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (
                    _userManager.GetUserName(User)!, $"Created Company {model.CompanyCode}",
                    "Company", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCompanyList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await _unitOfWork.Company
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(c =>
                        c.CompanyCode!.ToLower().Contains(searchValue) == true ||
                        c.CompanyName.ToLower().Contains(searchValue) == true ||
                        c.CompanyAddress.ToLower().Contains(searchValue) == true ||
                        c.CompanyTin.ToLower().Contains(searchValue) == true
                        ).ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
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
                _logger.LogError(ex, "Failed to get company.");
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Company model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.Company.UpdateAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (
                    _userManager.GetUserName(User)!, $"Edited Company {model.CompanyCode}",
                    "Company", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Company updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating company");
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            company.IsActive = true;
            await _unitOfWork.SaveAsync(cancellationToken);

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new (
                _userManager.GetUserName(User)!, $"Activated Company {company.CompanyCode}",
                "Company", (await GetCompanyClaimAsync())! );
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            TempData["success"] = "Company activated successfully";
            return RedirectToAction(nameof(Index));

        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            company.IsActive = false;
            await _unitOfWork.SaveAsync(cancellationToken);

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new (
                _userManager.GetUserName(User)!, $"Deactivated Company {company.CompanyCode}",
                "Company", (await GetCompanyClaimAsync())! );
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            TempData["success"] = "Company deactivated successfully";
            return RedirectToAction(nameof(Index));

        }
    }
}
