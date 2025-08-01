using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SupplierController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SupplierController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorageService;

        public SupplierController(IUnitOfWork unitOfWork,
            ILogger<SupplierController> logger,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
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

        private string GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            IEnumerable<FilprideSupplier> suppliers = await _unitOfWork.FilprideSupplier
                .GetAllAsync(null, cancellationToken);

            if (view == nameof(DynamicView.Supplier))
            {
                return View("ExportIndex", suppliers);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            FilprideSupplier model = new()
            {
                DefaultExpenses = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                WithholdingTaxList = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => coa.AccountNumber!.Contains("2010302"))
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideSupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (await _unitOfWork.FilprideSupplier.IsSupplierExistAsync(model.SupplierName, model.Category,
                    companyClaims, cancellationToken))
            {
                ModelState.AddModelError("SupplierName", "Supplier already exist.");
                return View(model);
            }

            if (await _unitOfWork.FilprideSupplier.IsTinNoExistAsync(model.SupplierTin, model.Branch!,
                    model.Category, companyClaims, cancellationToken))
            {
                ModelState.AddModelError("SupplierTin", "Tin number already exist.");
                return View(model);
            }

            try
            {
                if (registration != null && registration.Length > 0)
                {
                    model.ProofOfRegistrationFileName = GenerateFileNameToSave(registration.FileName);
                    model.ProofOfRegistrationFilePath =
                        await _cloudStorageService.UploadFileAsync(registration,
                            model.ProofOfRegistrationFileName!);
                }

                if (document != null && document.Length > 0)
                {
                    model.ProofOfExemptionFileName = GenerateFileNameToSave(document.FileName);
                    model.ProofOfExemptionFilePath =
                        await _cloudStorageService.UploadFileAsync(document, model.ProofOfExemptionFileName!);
                }

                model.SupplierCode = await _unitOfWork.FilprideSupplier
                    .GenerateCodeAsync(cancellationToken);
                model.CreatedBy = _userManager.GetUserName(User);
                model.Company = companyClaims;
                await _unitOfWork.FilprideSupplier.AddAsync(model, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new supplier {model.SupplierCode}", "Supplier", model.Company);
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create supplier master file. Created by: {UserName}",
                    _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSuppliersList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await _unitOfWork.FilprideSupplier
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(s =>
                        s.SupplierCode!.ToLower().Contains(searchValue) ||
                        s.SupplierName.ToLower().Contains(searchValue) ||
                        s.SupplierAddress.ToLower().Contains(searchValue) ||
                        s.SupplierTin.ToLower().Contains(searchValue) ||
                        s.SupplierTerms.ToLower().Contains(searchValue) ||
                        s.Category.ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();
                var pagedData = queried
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
                _logger.LogError(ex, "Failed to get suppliers.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork.FilprideSupplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.DefaultExpenses = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            supplier.WithholdingTaxList = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideSupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (registration != null && registration.Length > 0)
                {
                    model.ProofOfRegistrationFileName = GenerateFileNameToSave(registration.FileName);
                    model.ProofOfRegistrationFilePath = await _cloudStorageService.UploadFileAsync(registration, model.ProofOfRegistrationFileName!);
                }

                if (document != null && document.Length > 0)
                {
                    model.ProofOfExemptionFileName = GenerateFileNameToSave(document.FileName);
                    model.ProofOfExemptionFilePath = await _cloudStorageService.UploadFileAsync(document, model.ProofOfExemptionFileName!);
                }

                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.FilprideSupplier.UpdateAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to edit supplier master file. Edited by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);

        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.IsActive = true;
            await _unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Supplier activated successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);

        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.IsActive = false;
            await _unitOfWork.SaveAsync(cancellationToken);
            TempData["success"] = "Supplier deactivated successfully";
            return RedirectToAction(nameof(Index));

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
            var selectedList = await _dbContext.FilprideSuppliers
                .Where(supp => recordIds.Contains(supp.SupplierId))
                .OrderBy(supp => supp.SupplierCode)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Supplier");

            worksheet.Cells["A1"].Value = "Name";
            worksheet.Cells["B1"].Value = "Address";
            worksheet.Cells["C1"].Value = "ZipCode";
            worksheet.Cells["D1"].Value = "TinNo";
            worksheet.Cells["E1"].Value = "Terms";
            worksheet.Cells["F1"].Value = "VatType";
            worksheet.Cells["G1"].Value = "TaxType";
            worksheet.Cells["H1"].Value = "ProofOfRegistrationFilePath";
            worksheet.Cells["I1"].Value = "ReasonOfExemption";
            worksheet.Cells["J1"].Value = "Validity";
            worksheet.Cells["K1"].Value = "ValidityDate";
            worksheet.Cells["L1"].Value = "ProofOfExemptionFilePath";
            worksheet.Cells["M1"].Value = "CreatedBy";
            worksheet.Cells["N1"].Value = "CreatedDate";
            worksheet.Cells["O1"].Value = "Branch";
            worksheet.Cells["P1"].Value = "Category";
            worksheet.Cells["Q1"].Value = "TradeName";
            worksheet.Cells["R1"].Value = "DefaultExpenseNumber";
            worksheet.Cells["S1"].Value = "WithholdingTaxPercent";
            worksheet.Cells["T1"].Value = "WithholdingTaxTitle";
            worksheet.Cells["U1"].Value = "OriginalSupplierId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.SupplierName;
                worksheet.Cells[row, 2].Value = item.SupplierAddress;
                worksheet.Cells[row, 3].Value = item.ZipCode;
                worksheet.Cells[row, 4].Value = item.SupplierTin;
                worksheet.Cells[row, 5].Value = item.SupplierTerms;
                worksheet.Cells[row, 6].Value = item.VatType;
                worksheet.Cells[row, 7].Value = item.TaxType;
                worksheet.Cells[row, 8].Value = item.ProofOfRegistrationFilePath;
                worksheet.Cells[row, 9].Value = item.ReasonOfExemption;
                worksheet.Cells[row, 10].Value = item.Validity;
                worksheet.Cells[row, 11].Value = item.ValidityDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 12].Value = item.ProofOfExemptionFilePath;
                worksheet.Cells[row, 13].Value = item.CreatedBy;
                worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 15].Value = item.Branch;
                worksheet.Cells[row, 16].Value = item.Category;
                worksheet.Cells[row, 17].Value = item.TradeName;
                worksheet.Cells[row, 18].Value = item.DefaultExpenseNumber;
                worksheet.Cells[row, 19].Value = item.WithholdingTaxPercent;
                worksheet.Cells[row, 20].Value = item.WithholdingTaxtitle;
                worksheet.Cells[row, 21].Value = item.SupplierId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SupplierList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllSupplierIds()
        {
            var supplierIds = _dbContext.FilprideSuppliers
                 .Select(s => s.SupplierId)
                 .ToList();

            return Json(supplierIds);
        }
    }
}
