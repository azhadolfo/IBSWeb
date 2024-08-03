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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var banks = await _unitOfWork.FilprideBankAccount
                .GetAllAsync(null, cancellationToken);
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

                var checkLastAccountNo = await _dbContext
                .FilprideBankAccounts
                .OrderBy(bank => bank.BankAccountId)
                .LastOrDefaultAsync(cancellationToken);

                #region -- Generate AccountNo --

                var generatedAccountNo = 0L;

                if (checkLastAccountNo != null)
                {
                    // Increment the last serial by one and return it
                    generatedAccountNo = checkLastAccountNo.SeriesNumber + 1L;
                }
                else
                {
                    // If there are no existing records, you can start with a default value like 1
                    generatedAccountNo = 1L;
                }

                model.AccountNoCOA = "1010101" + generatedAccountNo.ToString("D2");

                #endregion -- Generate AccountNo --

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