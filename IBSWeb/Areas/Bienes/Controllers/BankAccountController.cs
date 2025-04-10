using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Bienes;
using IBS.Models.Bienes.MasterFile;
using IBS.Models.Filpride.Books;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Bienes.Controllers
{
    [Area(nameof(Bienes))]
    [CompanyAuthorize(nameof(Bienes))]
    public class BankAccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<BankAccountController> _logger;

        public BankAccountController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ILogger<BankAccountController> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var banks = await _unitOfWork.BienesBankAccount
                .GetAllAsync(b => b.Company == companyClaims, cancellationToken);

                return View(banks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in Index.");
                TempData["error"] = ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BienesBankAccount model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (await _unitOfWork.BienesBankAccount.IsBankAccountNoExist(model.AccountNo, cancellationToken))
                    {
                        ModelState.AddModelError("AccountNo", "Bank account no already exist!");
                        return View(model);
                    }

                    if (await _unitOfWork.BienesBankAccount.IsBankAccountNameExist(model.AccountName, cancellationToken))
                    {
                        ModelState.AddModelError("AccountName", "Bank account name already exist!");
                        return View(model);
                    }

                    model.Company = await GetCompanyClaimAsync();

                    model.CreatedBy = _userManager.GetUserName(this.User);

                    await _dbContext.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new bank {model.Bank} {model.AccountName} {model.AccountNo}", "Bank Account", "", model.Company);
                    await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Bank created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create bank account. Created by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.BienesBankAccount
                .GetAsync(b => b.BankAccountId == id, cancellationToken);

            return View(existingModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BienesBankAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.BienesBankAccount.GetAsync(b => b.BankAccountId == model.BankAccountId, cancellationToken);
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    existingModel.AccountNo = model.AccountNo;
                    existingModel.AccountName = model.AccountName;
                    existingModel.Bank = model.Bank;
                    existingModel.Branch = model.Branch;

                    TempData["success"] = "Bank edited successfully.";
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Edited bank {existingModel.Bank} {existingModel.AccountName} {existingModel.AccountNo}", "Bank Account", "", existingModel.Company);
                    await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit bank account. Edited by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(existingModel);
                }
            }
            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(existingModel);
        }
    }
}
