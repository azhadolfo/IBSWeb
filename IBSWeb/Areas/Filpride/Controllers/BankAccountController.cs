using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class BankAccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        public BankAccountController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _userManager = userManager;
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

                var banks = await _unitOfWork.FilprideBankAccount
                .GetAllAsync(b => b.Company == companyClaims, cancellationToken);
                return View(banks);
            }
            catch (Exception ex)
            {
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
        public async Task<IActionResult> Create(FilprideBankAccount model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _unitOfWork.FilprideBankAccount.IsBankAccountNoExist(model.AccountNo, cancellationToken))
                {
                    ModelState.AddModelError("AccountNo", "Bank account no already exist!");
                    return View(model);
                }

                if (await _unitOfWork.FilprideBankAccount.IsBankAccountNameExist(model.AccountName, cancellationToken))
                {
                    ModelState.AddModelError("AccountName", "Bank account name already exist!");
                    return View(model);
                }

                model.Company = await GetCompanyClaimAsync();

                #region Generate Account No

                var lastCibAccount = await _dbContext.ChartOfAccounts
                    .Where(coa => coa.Level == 5 && coa.AccountNumber.StartsWith("1010101"))
                    .OrderByDescending(coa => coa.AccountNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastCibAccount != null)
                {
                    var lastAccountNumber = lastCibAccount.AccountNumber;
                    model.AccountNoCOA = (long.Parse(lastAccountNumber) + 1).ToString();
                }
                else
                {
                    model.AccountNoCOA = "101010101";
                }

                #endregion Generate Account No

                model.CreatedBy = _userManager.GetUserName(this.User);

                #region -- COA Entry --

                var coa = new ChartOfAccount
                {
                    IsMain = false,
                    AccountNumber = model.AccountNoCOA,
                    AccountName = "Cash in Bank" + " - " + model.AccountNo + " " + model.AccountName,
                    AccountType = "Asset",
                    NormalBalance = "Debit",
                    Parent = "1010101",
                    CreatedBy = _userManager.GetUserName(this.User),
                    CreatedDate = DateTime.Now,
                    Level = 5
                };

                await _dbContext.ChartOfAccounts.AddAsync(coa, cancellationToken);

                #endregion -- COA Entry --

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Bank created successfully.";
                return RedirectToAction("Index");
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
            var existingModel = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == id, cancellationToken);
            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideBankAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideBankAccount.GetAsync(b => b.BankAccountId == model.BankAccountId, cancellationToken);
            if (existingModel == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                existingModel.AccountNo = model.AccountNo;
                existingModel.AccountName = model.AccountName;
                existingModel.Bank = model.Bank;
                existingModel.Branch = model.Branch;

                TempData["success"] = "Bank edited successfully.";
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(existingModel);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToAction("Index");
        }
    }
}