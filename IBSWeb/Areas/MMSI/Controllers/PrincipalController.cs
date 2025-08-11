using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class PrincipalController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PrincipalController> _logger;

        public PrincipalController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager, ILogger<PrincipalController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var principals = await _unitOfWork.Principal.GetAllAsync(null, cancellationToken);
            return View(principals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = new MMSIPrincipal
            {
                CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await _unitOfWork.FilprideCustomer
                    .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken) ?? throw new NullReferenceException("Customer not found");
                model.CustomerId = customer.CustomerId;
                await _unitOfWork.Principal.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Created new Principal #{model.PrincipalNumber}", "Principal", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create principal.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _unitOfWork.Principal
                    .GetAsync(p => p.PrincipalId == id, cancellationToken);

                if (model == null) return NotFound();
                await _unitOfWork.Principal.RemoveAsync(model, cancellationToken);
                await _unitOfWork.Principal.SaveAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete principal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await _unitOfWork.Principal.GetAsync(p => p.PrincipalId == id, cancellationToken);
            if (model == null) return NotFound();
            model.CustomerSelectList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPrincipal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.Principal.GetAsync(p => p.PrincipalId == model.PrincipalId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Principal not found.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited Principal #{currentModel.PrincipalNumber} => {model.PrincipalNumber}", "Principal", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.Address = model.Address;
                currentModel.PrincipalName = model.PrincipalName;
                currentModel.PrincipalNumber = model.PrincipalNumber;
                currentModel.TIN = model.TIN;
                currentModel.BusinessType = model.BusinessType;
                currentModel.PrincipalName = model.PrincipalName;
                currentModel.Terms = model.Terms;
                currentModel.Mobile1 = model.Mobile1;
                currentModel.Mobile2 = model.Mobile2;
                currentModel.Landline1 = model.Landline1;
                currentModel.Landline2 = model.Landline2;
                currentModel.IsVatable = model.IsVatable;
                currentModel.IsActive = model.IsActive;
                currentModel.CustomerId = model.CustomerId;
                await _unitOfWork.Principal.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit principal.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
