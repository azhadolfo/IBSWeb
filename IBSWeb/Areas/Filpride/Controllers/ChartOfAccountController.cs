using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Services;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    public class ChartOfAccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChartOfAccountController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ChartOfAccountController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ILogger<ChartOfAccountController> logger,
            IUnitOfWork unitOfWork,
            ICacheService cacheService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
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

        public async Task<IActionResult> Index(string? view, bool showHidden = false, CancellationToken cancellationToken = default)
        {
            if (view == nameof(DynamicView.ChartOfAccount))
            {
                return View("ExportIndex");
            }

            ViewBag.ShowHidden = showHidden;

            var accounts = showHidden
                ? await _unitOfWork.FilprideChartOfAccount.GetAllAsyncIgnoreQueryFilters(cancellationToken: cancellationToken)
                : await _unitOfWork.FilprideChartOfAccount.GetAllAsync(cancellationToken: cancellationToken);

            return View(accounts.Where(c => c.Level == 1)
                .ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int parentId, string accountName, bool showHidden = false, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var normalizedAccountName = NormalizeAccountName(accountName);
                var parentAccount = await _unitOfWork.FilprideChartOfAccount
                    .GetAsyncIgnoreQueryFilters(c => c.AccountId == parentId, cancellationToken);

                if (parentAccount == null)
                {
                    throw new InvalidOperationException("Parent Account not found");
                }

                var levelToCreate = parentAccount.Level + 1;
                if (levelToCreate < 4 || levelToCreate > 5)
                {
                    throw new InvalidOperationException("Only Level 4 and Level 5 accounts can be created from this screen.");
                }

                var newAccount = new FilprideChartOfAccount
                {
                    IsMain = false,
                    AccountType = parentAccount.AccountType,
                    NormalBalance = parentAccount.NormalBalance,
                    AccountName = normalizedAccountName,
                    ParentAccountId = parentId,
                    CreatedBy = GetUserFullName(),
                    Level = levelToCreate,
                    FinancialStatementType = parentAccount.FinancialStatementType,
                    AccountNumber = await GenerateNextAccountNumberAsync(parentAccount, cancellationToken)
                };

                parentAccount.HasChildren = true;

                await _unitOfWork.FilprideChartOfAccount.AddAsync(newAccount, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                var companyClaim = await GetCompanyClaimAsync();
                await _cacheService.RemoveAsync($"coa:{companyClaim}", cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Created new Account #{newAccount.AccountNumber}", "Chart of Accounts");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Account #{newAccount.AccountNumber} Created Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride", showHidden }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chart of account. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { showHidden });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int accountId, string accountName, bool showHidden = false, CancellationToken cancellationToken = default)
        {
            var existingAccount = await _unitOfWork.FilprideChartOfAccount
                .GetAsyncIgnoreQueryFilters(x => x.AccountId == accountId, cancellationToken);

            if (existingAccount == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var normalizedAccountName = NormalizeAccountName(accountName);
                if (string.Equals(existingAccount.AccountName, normalizedAccountName, StringComparison.Ordinal))
                {
                    TempData["success"] = "No changes were made.";
                    return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride", showHidden }) });
                }

                existingAccount.AccountName = normalizedAccountName;
                existingAccount.EditedBy = GetUserFullName();
                existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _unitOfWork.SaveAsync(cancellationToken);
                var companyClaim = await GetCompanyClaimAsync();
                await _cacheService.RemoveAsync($"coa:{companyClaim}", cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Edited Account #{existingAccount.AccountNumber}", "Chart of Accounts");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Account Edited Successfully";
                return Json(new { redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride", showHidden }) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit chart of account. Edited by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { showHidden });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHidden(int accountId, bool showHidden = false, CancellationToken cancellationToken = default)
        {
            var existingAccount = await _unitOfWork.FilprideChartOfAccount
                .GetAsyncIgnoreQueryFilters(x => x.AccountId == accountId, cancellationToken);

            if (existingAccount == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingAccount.IsHidden = !existingAccount.IsHidden;
                existingAccount.EditedBy = GetUserFullName();
                existingAccount.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _unitOfWork.SaveAsync(cancellationToken);
                await _cacheService.RemoveAsync($"coa:{await GetCompanyClaimAsync()}", cancellationToken);

                var action = existingAccount.IsHidden ? "Hidden" : "Unhidden";

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(),
                    $"{action} Account #{existingAccount.AccountNumber}", "Chart of Accounts");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Account {existingAccount.AccountNumber} {action.ToLowerInvariant()} successfully";
                return Json(new
                {
                    redirectUrl = Url.Action("Index", "ChartOfAccount", new { area = "Filpride", showHidden })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle chart of account visibility. Updated by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { showHidden });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetChartOfAccountList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var chartOfAccounts = _unitOfWork.FilprideChartOfAccount
                    .GetAllQueryIgnoreQueryFilters();

                var totalRecords = await chartOfAccounts.CountAsync(cancellationToken);

                // Apply date range filter if provided (using CreatedDate)
                if (dateFrom.HasValue)
                {
                    chartOfAccounts = chartOfAccounts
                        .Where(s => s.CreatedDate >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    // Add one day to include the entire end date
                    var dateToInclusive = dateTo.Value.AddDays(1);
                    chartOfAccounts = chartOfAccounts
                        .Where(s => s.CreatedDate < dateToInclusive);
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    var hasCreatedDate = DateTime.TryParse(searchValue, out var createdDate);

                    chartOfAccounts = chartOfAccounts
                        .Where(s =>
                            (s.AccountNumber != null && s.AccountNumber.ToLower().Contains(searchValue)) ||
                            s.AccountName.ToLower().Contains(searchValue) ||
                            (s.AccountType != null && s.AccountType.ToLower().Contains(searchValue)) ||
                            (s.NormalBalance != null && s.NormalBalance.ToLower().Contains(searchValue)) ||
                            (s.IsHidden ? "hidden" : "visible").Contains(searchValue) ||
                            s.Level.ToString().Contains(searchValue) ||
                            (hasCreatedDate && DateOnly.FromDateTime(s.CreatedDate) == DateOnly.FromDateTime(createdDate))
                        );
                }

                //Apply sorting if provided
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    chartOfAccounts = chartOfAccounts
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalFilteredRecords = await chartOfAccounts.CountAsync(cancellationToken);

                // Apply pagination - HANDLE -1 FOR "ALL"
                IQueryable<FilprideChartOfAccount> pagedChartOfAccounts;

                if (parameters.Length == -1)
                {
                    // "All" selected - return all records
                    pagedChartOfAccounts = chartOfAccounts;
                }
                else
                {
                    // Normal pagination
                    pagedChartOfAccounts = chartOfAccounts
                        .Skip(parameters.Start)
                        .Take(parameters.Length);
                }

                var pagedData = await pagedChartOfAccounts
                    .Select(x => new
                    {
                        x.AccountId,
                        x.AccountNumber,
                        x.AccountName,
                        x.AccountType,
                        x.NormalBalance,
                        x.IsHidden,
                        x.Level,
                        x.CreatedDate
                    })
                    .ToListAsync(cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalFilteredRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get chart of accounts. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
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
                worksheet.Cells["L1"].Value = "IsHidden";
                worksheet.Cells["M1"].Value = "ParentAccountId";
                worksheet.Cells["N1"].Value = "OriginalChartOfAccount";

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
                    worksheet.Cells[row, 8].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet.Cells[row, 9].Value = item.EditedBy;
                    worksheet.Cells[row, 10].Value = item.EditedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet.Cells[row, 11].Value = item.HasChildren;
                    worksheet.Cells[row, 12].Value = item.IsHidden;
                    worksheet.Cells[row, 13].Value = item.ParentAccountId;
                    worksheet.Cells[row, 14].Value = item.AccountId;

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
                .IgnoreQueryFilters()
                .Select(coa => coa.AccountId) // Assuming Id is the primary key
                .ToListAsync(cancellationToken);
            return Json(coaIds);
        }

        private static string NormalizeAccountName(string accountName)
        {
            return string.IsNullOrWhiteSpace(accountName)
                ? throw new InvalidOperationException("Account name is required.")
                : accountName.Trim();
        }

        private async Task<string> GenerateNextAccountNumberAsync(FilprideChartOfAccount parentAccount, CancellationToken cancellationToken)
        {
            var siblingAccounts = await _unitOfWork.FilprideChartOfAccount
                .GetAllAsyncIgnoreQueryFilters(c => c.ParentAccountId == parentAccount.AccountId, cancellationToken);

            var lastAccountNumber = siblingAccounts
                .Where(c => !string.IsNullOrWhiteSpace(c.AccountNumber))
                .Select(c => c.AccountNumber!)
                .OrderByDescending(c => c.Length)
                .ThenByDescending(c => c)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(parentAccount.AccountNumber))
            {
                throw new InvalidOperationException("Parent account number is required.");
            }

            if (!string.IsNullOrWhiteSpace(lastAccountNumber))
            {
                if (!long.TryParse(lastAccountNumber, out var lastSeries))
                {
                    throw new InvalidOperationException($"Invalid account number format: {lastAccountNumber}");
                }

                return parentAccount.Level + 1 == 4
                    ? (lastSeries + 100).ToString()
                    : (lastSeries + 1).ToString();
            }

            if (!long.TryParse(parentAccount.AccountNumber, out var parentSeries))
            {
                throw new InvalidOperationException($"Invalid parent account number format: {parentAccount.AccountNumber}");
            }

            return parentAccount.Level + 1 == 4
                ? (parentSeries + 100).ToString()
                : (parentSeries + 1).ToString();
        }
    }
}
