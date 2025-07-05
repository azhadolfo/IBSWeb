using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ChartOfAccountController : Controller
    {
        public readonly ApplicationDbContext _dbContext;
        public readonly UserManager<IdentityUser> _userManager;
        public readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChartOfAccountController> _logger;

        public ChartOfAccountController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork, ILogger<ChartOfAccountController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.ChartOfAccount))
            {
                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Include(c => c.Children.OrderBy(ch => ch.AccountNumber))
                    .OrderBy(c => c.AccountNumber)
                    .ToListAsync(cancellationToken);

                return View("ExportIndex", chartOfAccounts);
            }

            var Level1 = await _dbContext.FilprideChartOfAccounts
                .Include(c => c.Children.OrderBy(ch => ch.AccountNumber))
                .OrderBy(c => c.AccountNumber)
                .ToListAsync(cancellationToken);

            return View(Level1.Where((c => c.Level == 1)).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int parentId, string accountName, CancellationToken cancellationToken)
        {
            try
            {
                var parentAccount = await _dbContext.FilprideChartOfAccounts
                    .Where(c => c.AccountId == parentId)
                    .FirstOrDefaultAsync(cancellationToken);

                var lastSeries = int.Parse((_dbContext.FilprideChartOfAccounts
                    .Where(c => c.ParentAccountId == parentId)
                    .OrderByDescending(c => c.AccountNumber)
                    .FirstOrDefaultAsync(cancellationToken).Result?.AccountNumber ?? parentAccount?.AccountNumber) ?? string.Empty);

                var levelToCreate = parentAccount?.Level + 1;

                FilprideChartOfAccount newAccount = new FilprideChartOfAccount()
                {
                    IsMain = false,
                    AccountType = parentAccount?.AccountType,
                    NormalBalance = parentAccount?.NormalBalance ?? "",
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
                        newAccount.AccountNumber = (lastSeries + 100).ToString();
                        break;
                    case 5:
                        newAccount.AccountNumber = (lastSeries + 1).ToString();
                        break;
                }

                await _dbContext.FilprideChartOfAccounts.AddAsync(newAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Account Created Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride" }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chart of account. Created by: {UserName}", _userManager.GetUserName(User));
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int accountId, string accountName, CancellationToken cancellationToken)
        {
            try
            {
                var existingAccount = await _dbContext.FilprideChartOfAccounts
                    .FindAsync(accountId, cancellationToken);

                if (existingAccount == null)
                {
                    return NotFound();
                }

                existingAccount.AccountName = accountName;
                existingAccount.EditedBy = User.Identity!.Name;
                existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Account Edited Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride" }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit chart of account. Edited by: {UserName}", _userManager.GetUserName(User));
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => recordIds.Contains(coa.AccountId))
                    .OrderBy(coa => coa.AccountId)
                    .ToListAsync();

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ChartOfAccount");

                worksheet.Cells["A1"].Value = "IsMain";
                worksheet.Cells["B1"].Value = "AccountNumber";
                worksheet.Cells["C1"].Value = "AccountName";
                worksheet.Cells["D1"].Value = "AccountType";
                worksheet.Cells["E1"].Value = "NormalBalance";
                worksheet.Cells["F1"].Value = "Level";
                worksheet.Cells["G1"].Value = "CreatedBy";
                worksheet.Cells["H1"].Value = "CreatedDate";
                worksheet.Cells["I1"].Value = "EditedBy";
                worksheet.Cells["J1"].Value = "EditedDate";
                worksheet.Cells["K1"].Value = "HasChildren";
                worksheet.Cells["L1"].Value = "ParentAccountId";
                worksheet.Cells["M1"].Value = "OriginalChartOfAccount";

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.IsMain;
                    worksheet.Cells[row, 2].Value = item.AccountNumber;
                    worksheet.Cells[row, 3].Value = item.AccountName;
                    worksheet.Cells[row, 4].Value = item.AccountType;
                    worksheet.Cells[row, 5].Value = item.NormalBalance;
                    worksheet.Cells[row, 6].Value = item.Level;
                    worksheet.Cells[row, 7].Value = item.CreatedBy;
                    worksheet.Cells[row, 8].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 9].Value = item.EditedBy;
                    worksheet.Cells[row, 10].Value = item.EditedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 11].Value = item.HasChildren;
                    worksheet.Cells[row, 12].Value = item.ParentAccountId;
                    worksheet.Cells[row, 13].Value = item.AccountId;

                    row++;
                }

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ChartOfAccountList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
            }

        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllChartOfAccountIds(CancellationToken cancellationToken)
        {
            var coaIds = await _dbContext.FilprideChartOfAccounts
                .Select(coa => coa.AccountId) // Assuming Id is the primary key
                .ToListAsync(cancellationToken);
            return Json(coaIds);
        }
    }
}
