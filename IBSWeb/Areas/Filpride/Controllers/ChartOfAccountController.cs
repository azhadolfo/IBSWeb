using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ChartOfAccountController : Controller
    {
        public readonly ApplicationDbContext _dbContext;
        public readonly UserManager<IdentityUser> _userManager;
        public readonly IUnitOfWork _unitOfWork;

        public ChartOfAccountController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var Level1 = await _dbContext.FilprideChartOfAccounts
                .Include(c => c.Children.OrderBy(ch => ch.AccountNumber))
                .OrderBy(c => c.AccountNumber)
                .ToListAsync(cancellationToken);

            //
            return View(Level1.Where((c => c.Level == 1)).ToList());
        }

        public async Task<IActionResult> Create(int parentId, string accountName, CancellationToken cancellationToken)
        {

            try
            {
                var parentAccount = await _dbContext.FilprideChartOfAccounts
                    .Where(c => c.AccountId == parentId)
                    .FirstOrDefaultAsync(cancellationToken);
                int? lastSeries = int.Parse(_dbContext.FilprideChartOfAccounts
                    .Where(c => c.ParentAccountId == parentId)
                    .OrderByDescending(c => c.AccountNumber)
                    .FirstOrDefaultAsync(cancellationToken)?.Result?.AccountNumber ?? parentAccount.AccountNumber);

                var levelToCreate = parentAccount?.Level + 1;
                FilprideChartOfAccount newAccount = new FilprideChartOfAccount()
                {
                    IsMain = false,
                    AccountType = parentAccount?.AccountType,
                    NormalBalance = parentAccount.NormalBalance ?? "",
                    AccountName = accountName,
                    ParentAccountId = parentId,
                    CreatedBy = User.Identity?.Name,
                    EditedBy = null,
                    EditedDate = default,
                    Level = levelToCreate ?? 0,
                    FinancialStatementType = parentAccount?.FinancialStatementType ?? "",
                };
                switch (levelToCreate)
                {
                    case 4:
                        newAccount.AccountNumber = (lastSeries+100).ToString();
                        break;
                    case 5:
                        newAccount.AccountNumber = (lastSeries+1).ToString();
                        break;
                }

                await _dbContext.FilprideChartOfAccounts.AddAsync(newAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Account Created Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride" }) });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
