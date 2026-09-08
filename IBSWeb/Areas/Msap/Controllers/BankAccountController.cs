using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap.MasterFile;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    [Authorize(Roles = "Admin")]
    public class BankAccountController(
        IUnitOfWork unitOfWork)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var bankAccounts = await unitOfWork.BankAccount.GetAllAsync(cancellationToken: cancellationToken);
            return View(bankAccounts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new BankAccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BankAccountViewModel bankAccount, CancellationToken cancellationToken)
        {
            if (await unitOfWork.BankAccount.IsBankAccountNoExist(bankAccount.AccountNo, cancellationToken))
            {
                ModelState.AddModelError("AccountNo", "Account Number already exists.");
                return View(bankAccount);
            }

            bankAccount.CreatedBy = GetUserFullName();
            bankAccount.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

            await unitOfWork.BankAccount.AddAsync(bankAccount, cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Bank Account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var bankAccount = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == id, cancellationToken);
            if (bankAccount == null)
            {
                return NotFound();
            }
            return View(new BankAccountViewModel(bankAccount));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BankAccountViewModel bankAccount, CancellationToken cancellationToken)
        {
            var existingBankAccount = await unitOfWork.BankAccount.GetAsync(b => b.BankAccountId == bankAccount.BankAccountId, cancellationToken);
            if (existingBankAccount == null)
            {
                return NotFound();
            }

            if (existingBankAccount.AccountNo != bankAccount.AccountNo)
            {
                if (await unitOfWork.BankAccount.IsBankAccountNoExist(bankAccount.AccountNo, cancellationToken))
                {
                    ModelState.AddModelError("AccountNo", "Account Number already exists.");
                    return View(bankAccount);
                }
            }

            existingBankAccount.BankAccountCode = bankAccount.BankAccountCode;
            existingBankAccount.Bank = bankAccount.Bank;
            existingBankAccount.Branch = bankAccount.Branch;
            existingBankAccount.AccountNo = bankAccount.AccountNo;
            existingBankAccount.AccountName = bankAccount.AccountName;
            existingBankAccount.Company = bankAccount.Company;

            await unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Bank Account updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        #region API Calls
        
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var bankAccounts = await unitOfWork.BankAccount.GetAllAsync(cancellationToken: cancellationToken);
            return Json(new { data = bankAccounts });
        }
        
        #endregion
    }
}
