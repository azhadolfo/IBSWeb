using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var banks = await _unitOfWork.FilprideBankAccount
                .GetAllAsync(b => b.Company == companyClaims, cancellationToken);

                if (view == nameof(DynamicView.BankAccount))
                {
                    return View("ExportIndex", banks);
                }

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
            var existingModel = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == id, cancellationToken);
            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideBankAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideBankAccount.GetAsync(b => b.BankAccountId == model.BankAccountId, cancellationToken);
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
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(existingModel);
                }
            }
            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(existingModel);
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BankAccountList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllBankAccountIds()
        {
            var bankIds = _dbContext.FilprideBankAccounts
                                     .Select(b => b.BankAccountId) // Assuming Id is the primary key
                                     .ToList();

            return Json(bankIds);
        }
    }
}
