using IBS.Models.Msap.MasterFile;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Services.Msap;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class SupplierController(
        IUnitOfWork unitOfWork,
        ILogger<SupplierController> logger,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ICloudStorageService cloudStorageService,
        ICacheService cacheService)
        : Controller
    {
        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private string GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.Supplier))
            {
                return View("ExportIndex");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            SupplierViewModel model = new()
            {
                DefaultExpenses = await dbContext.ChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                WithholdingTaxList = await dbContext.ChartOfAccounts
                    .Where(coa => coa.AccountNumber!.Contains("2010302") && !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierViewModel model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            model.DefaultExpenses = await dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.WithholdingTaxList = await dbContext.ChartOfAccounts
                .Where(coa => coa.AccountNumber!.Contains("2010302") && !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            if (await unitOfWork.Supplier.IsSupplierExistAsync(model.SupplierName, model.Category,
                    companyClaims, cancellationToken))
            {
                ModelState.AddModelError("SupplierName", "Supplier already exist.");
                return View(model);
            }

            if (await unitOfWork.Supplier.IsTinNoExistAsync(model.SupplierTin, model.Branch!,
                    model.Category, companyClaims, cancellationToken))
            {
                ModelState.AddModelError("SupplierTin", "Tin number already exist.");
                return View(model);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (registration is { Length: > 0 })
                {
                    model.ProofOfRegistrationFileName = GenerateFileNameToSave(registration.FileName);
                    model.ProofOfRegistrationFilePath = await cloudStorageService.UploadFileAsync(registration, model.ProofOfRegistrationFileName!);
                }

                if (document is { Length: > 0 })
                {
                    model.ProofOfExemptionFileName = GenerateFileNameToSave(document.FileName);
                    model.ProofOfExemptionFilePath = await cloudStorageService.UploadFileAsync(document, model.ProofOfExemptionFileName!);
                }

                model.SupplierCode = await unitOfWork.Supplier.GenerateCodeAsync(cancellationToken);
                model.CreatedBy = GetUserFullName();
                model.Company = companyClaims;
                await unitOfWork.Supplier.AddAsync(model, cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                await cacheService.RemoveAsync($"coa:{model.Company}", cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new Supplier #{model.SupplierCode}", "Supplier");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create supplier master file. Created by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSuppliersList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var queried = await unitOfWork.Supplier
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
                if (parameters.Order?.Count > 0)
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
                logger.LogError(ex, "Failed to get suppliers.");
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

            var supplier = await unitOfWork.Supplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            var model = new SupplierViewModel(supplier);

            model.DefaultExpenses = await dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.WithholdingTaxList = await dbContext.ChartOfAccounts
                .Where(coa => coa.AccountNumber!.Contains("2010302") && !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SupplierViewModel model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            model.DefaultExpenses = await dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.WithholdingTaxList = await dbContext.ChartOfAccounts
                .Where(coa => coa.AccountNumber!.Contains("2010302") && !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (registration is { Length: > 0 })
                {
                    model.ProofOfRegistrationFileName = GenerateFileNameToSave(registration.FileName);
                    model.ProofOfRegistrationFilePath = await cloudStorageService.UploadFileAsync(registration, model.ProofOfRegistrationFileName!);
                }

                if (document is { Length: > 0 })
                {
                    model.ProofOfExemptionFileName = GenerateFileNameToSave(document.FileName);
                    model.ProofOfExemptionFilePath = await cloudStorageService.UploadFileAsync(document, model.ProofOfExemptionFileName!);
                }

                model.EditedBy = GetUserFullName();
                await unitOfWork.Supplier.UpdateAsync(model, cancellationToken);
                await cacheService.RemoveAsync($"coa:{model.Company}", cancellationToken);

                #region -- Audit Trail Recording --

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Edited Supplier #{model.SupplierCode}", "Supplier" );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to edit supplier master file. Edited by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
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

            var supplier = await unitOfWork.Supplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            return View(supplier);
        }

        [HttpPost, ActionName("Activate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await unitOfWork.Supplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                supplier.IsActive = true;
                await unitOfWork.SaveAsync(cancellationToken);
                await cacheService.RemoveAsync($"coa:{supplier.Company}", cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new(GetUserFullName(),
                    $"Activated Supplier #{supplier.SupplierCode}", "Supplier");
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier activated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to activate supplier master file. Activated by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Activate), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await unitOfWork.Supplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            return View(supplier);
        }

        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await unitOfWork.Supplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await unitOfWork.Terms.GetTermsListAsyncByCode(cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                supplier.IsActive = false;
                await unitOfWork.SaveAsync(cancellationToken);
                await cacheService.RemoveAsync($"coa:{supplier.Company}", cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrailBook = new (GetUserFullName(),
                    $"Deactivated Supplier #{supplier.SupplierCode}", "Supplier" );
                await unitOfWork.AuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier deactivated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deactivate supplier master file. Deactivated by: {UserName}", userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Deactivate), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSupplierList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var suppliers = await unitOfWork.Supplier
                    .GetAllAsync(null, cancellationToken);

                // Apply date range filter if provided (using CreatedDate)
                if (dateFrom.HasValue)
                {
                    suppliers = suppliers
                        .Where(s => s.CreatedDate >= dateFrom.Value)
                        .ToList();
                }

                if (dateTo.HasValue)
                {
                    // Add one day to include the entire end date
                    var dateToInclusive = dateTo.Value.AddDays(1);
                    suppliers = suppliers
                        .Where(s => s.CreatedDate < dateToInclusive)
                        .ToList();
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    suppliers = suppliers
                        .Where(s =>
                            (s.SupplierCode != null && s.SupplierCode.ToLower().Contains(searchValue)) ||
                            (s.SupplierName.ToLower().Contains(searchValue)) ||
                            (s.SupplierAddress.ToLower().Contains(searchValue)) ||
                            (s.SupplierTin.ToLower().Contains(searchValue)) ||
                            (s.SupplierTerms.ToLower().Contains(searchValue)) ||
                            (s.VatType.ToLower().Contains(searchValue)) ||
                            (s.Category.ToLower().Contains(searchValue)) ||
                            s.CreatedDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Apply sorting if provided
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    // Map frontend column names to actual entity property names
                    var columnMapping = new Dictionary<string, string>
                    {
                        { "supplierCode", "SupplierCode" },
                        { "supplierName", "SupplierName" },
                        { "supplierAddress", "SupplierAddress" },
                        { "supplierTin", "SupplierTin" },
                        { "supplierTerms", "SupplierTerms" },
                        { "vatType", "VatType" },
                        { "category", "Category" },
                        { "createdDate", "CreatedDate" }
                    };

                    // Get the actual property name
                    var actualColumnName = columnMapping.TryGetValue(columnName, out string? value)
                        ? value
                        : columnName;

                    suppliers = suppliers
                        .AsQueryable()
                        .OrderBy($"{actualColumnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = suppliers.Count();

                // Apply pagination - HANDLE -1 FOR "ALL"
                IEnumerable<Supplier> pagedSuppliers;

                if (parameters.Length == -1)
                {
                    // "All" selected - return all records
                    pagedSuppliers = suppliers;
                }
                else
                {
                    // Normal pagination
                    pagedSuppliers = suppliers
                        .Skip(parameters.Start)
                        .Take(parameters.Length);
                }

                var pagedData = pagedSuppliers
                    .Select(x => new
                    {
                        x.SupplierId,
                        x.SupplierCode,
                        x.SupplierName,
                        x.SupplierAddress,
                        x.SupplierTin,
                        x.SupplierTerms,
                        x.VatType,
                        x.Category,
                        x.CreatedDate
                    })
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
                logger.LogError(ex, "Failed to get suppliers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await dbContext.Suppliers
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
                worksheet.Cells[row, 11].Value = item.ValidityDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                worksheet.Cells[row, 12].Value = item.ProofOfExemptionFilePath;
                worksheet.Cells[row, 13].Value = item.CreatedBy;
                worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                worksheet.Cells[row, 15].Value = item.Branch;
                worksheet.Cells[row, 16].Value = item.Category;
                worksheet.Cells[row, 17].Value = item.TradeName;
                worksheet.Cells[row, 18].Value = item.DefaultExpenseNumber;
                worksheet.Cells[row, 19].Value = item.WithholdingTaxPercent;
                worksheet.Cells[row, 20].Value = item.WithholdingTaxTitle;
                worksheet.Cells[row, 21].Value = item.SupplierId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SupplierList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllSupplierIds()
        {
            var supplierIds = dbContext.Suppliers
                .Select(s => s.SupplierId)
                .ToList();

            return Json(supplierIds);
        }
    }
}
