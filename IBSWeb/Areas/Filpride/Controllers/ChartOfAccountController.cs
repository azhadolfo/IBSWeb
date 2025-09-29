using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChartOfAccountController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ChartOfAccountController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger<ChartOfAccountController> logger,
            IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.ChartOfAccount))
            {
                var chartOfAccounts = await _unitOfWork.FilprideChartOfAccount
                    .GetAllAsync(cancellationToken : cancellationToken);

                return View("ExportIndex", chartOfAccounts);
            }

            var level1 = await _unitOfWork.FilprideChartOfAccount
                .GetAllAsync(cancellationToken : cancellationToken);

            return View(level1.Where(c => c.Level == 1)
                .ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int parentId, string accountName, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var parentAccount = await _unitOfWork.FilprideChartOfAccount
                    .GetAsync(c => c.AccountId == parentId, cancellationToken);

                if (parentAccount == null)
                {
                    throw new InvalidOperationException("Parent Account not found");
                }

                var lastAccount = (await _unitOfWork.FilprideChartOfAccount
                        .GetAllAsync(c => c.ParentAccountId == parentId, cancellationToken: cancellationToken))
                    .OrderByDescending(c => c.AccountNumber)
                    .FirstOrDefault();

                var lastSeries = int.Parse(lastAccount?.AccountNumber ?? parentAccount.AccountNumber!);

                var levelToCreate = parentAccount.Level + 1;

                var newAccount = new FilprideChartOfAccount
                {
                    IsMain = false,
                    AccountType = parentAccount?.AccountType,
                    NormalBalance = parentAccount?.NormalBalance ?? "",
                    AccountName = accountName,
                    ParentAccountId = parentId,
                    CreatedBy = GetUserFullName(),
                    Level = levelToCreate,
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

                await _unitOfWork.FilprideChartOfAccount.AddAsync(newAccount, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (_userManager.GetUserName(User)!,
                    $"Created new Account #{newAccount.AccountNumber}", "Chart of Accounts", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Account #{newAccount.AccountNumber} Created Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride" }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chart of account. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int accountId, string accountName, CancellationToken cancellationToken)
        {
            var existingAccount = await _unitOfWork.FilprideChartOfAccount
                .GetAsync(x => x.AccountId == accountId, cancellationToken);

            if (existingAccount == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingAccount.AccountName = accountName;
                existingAccount.EditedBy = GetUserFullName();
                existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (_userManager.GetUserName(User)!,
                    $"Edited Account #{existingAccount.AccountNumber}", "Chart of Accounts", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Account Edited Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride" }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit chart of account. Edited by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
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
                var selectedList = (await _unitOfWork.FilprideChartOfAccount
                    .GetAllAsync(coa => recordIds.Contains(coa.AccountId), cancellationToken))
                    .OrderBy(coa => coa.AccountId)
                    .ToList();

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

                var row = 2;

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
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ChartOfAccountList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
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
