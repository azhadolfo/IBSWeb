using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ChartOfAccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ChartOfAccountController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public ChartOfAccountController(IUnitOfWork unitOfWork, ILogger<ChartOfAccountController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<ChartOfAccount> coa = await _unitOfWork
                .ChartOfAccount
                .GetAllAsync();

            return View(coa);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ChartOfAccount chartOfAccount = new()
            {
                Accounts = await _unitOfWork.ChartOfAccount.GetMainAccount()
            };

            return View(chartOfAccount);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ChartOfAccount model, string thirdLevel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model = await _unitOfWork.ChartOfAccount.GenerateAccount(model, thirdLevel, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    await _unitOfWork.ChartOfAccount.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Account created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in creating account.");
                    TempData["error"] = $"Error: '{ex.Message}', contact MIS for assistance!";
                    return View(model);
                }
            }

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        public async Task<IActionResult> GetChartOfAccount(string parentNo, CancellationToken cancellationToken)
        {
            return Json(await _unitOfWork.ChartOfAccount.GetMemberAccount(parentNo, cancellationToken));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0 )
            {
                return NotFound();
            }

            ChartOfAccount account = await _unitOfWork.ChartOfAccount
                .GetAsync(c => c.AccountId == id, cancellationToken);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ChartOfAccount model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.ChartOfAccount.UpdateAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Account updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating chart of account.");
                    TempData["error"] = $"Error: '{ex.Message}', contact MIS for assistance!";
                    return View(model);
                }
            }

            return View(model);
        }
    }
}
