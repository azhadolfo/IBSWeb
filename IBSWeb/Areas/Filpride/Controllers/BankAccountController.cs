using System.Linq.Dynamic.Core;
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
    public class BankAccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<BankAccountController> _logger;

        public BankAccountController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<BankAccountController> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
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
            try
            {
                var banks = await _unitOfWork.FilprideBankAccount
                .GetAllAsync(null, cancellationToken);

                return view == nameof(DynamicView.BankAccount) ? View("ExportIndex", banks) : View(banks);
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
        public async Task<IActionResult> Create(FilprideBankAccount model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
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

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                model.Company = companyClaims;

                model.CreatedBy = _userManager.GetUserName(User);

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new bank {model.Bank} {model.AccountName} {model.AccountNo}", "Bank Account", model.Company);
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Bank #{model.AccountNo} created successfully.";
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

        [HttpPost]
        public async Task<IActionResult> GetBankAccountsList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _unitOfWork.FilprideBankAccount
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(b =>
                        b.AccountNo.ToLower().Contains(searchValue) ||
                        b.AccountName.ToLower().Contains(searchValue) ||
                        b.Bank.ToLower().Contains(searchValue) ||
                        b.Branch.ToLower().Contains(searchValue) ||
                        b.CreatedBy!.ToLower().Contains(searchValue) ||
                        b.CreatedDate.ToString().ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();
                var pagedData = query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get bank accounts.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideBankAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == model.BankAccountId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(existingModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Edited bank {existingModel.Bank} {existingModel.AccountName} {existingModel.AccountNo} => {model.Bank} {model.AccountName} {model.AccountNo}", "Bank Account", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                existingModel.AccountNo = model.AccountNo;
                existingModel.AccountName = model.AccountName;
                existingModel.Bank = model.Bank;
                existingModel.Branch = model.Branch;
                existingModel.IsFilpride = model.IsFilpride;
                existingModel.IsMobility = model.IsMobility;
                existingModel.IsBienes = model.IsBienes;

                TempData["success"] = "Bank edited successfully.";
                await _unitOfWork.SaveAsync(cancellationToken);
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

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.FilprideBankAccounts
                .Where(bank => recordIds.Contains(bank.BankAccountId))
                .OrderBy(bank => bank.BankAccountId)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("BankAccount");

            worksheet.Cells["A1"].Value = "Branch";
            worksheet.Cells["B1"].Value = "CreatedBy";
            worksheet.Cells["C1"].Value = "CreatedDate";
            worksheet.Cells["D1"].Value = "AccountName";
            worksheet.Cells["E1"].Value = "AccountNo";
            worksheet.Cells["F1"].Value = "Bank";
            worksheet.Cells["G1"].Value = "OriginalBankId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Branch;
                worksheet.Cells[row, 2].Value = item.CreatedBy;
                worksheet.Cells[row, 3].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 4].Value = item.AccountName;
                worksheet.Cells[row, 5].Value = item.AccountNo;
                worksheet.Cells[row, 6].Value = item.Bank;
                worksheet.Cells[row, 7].Value = item.BankAccountId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BankAccountList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllBankAccountIds()
        {
            var bankIds = _dbContext.FilprideBankAccounts
                .Select(b => b.BankAccountId)
                .ToList();

            return Json(bankIds);
        }
    }
}
