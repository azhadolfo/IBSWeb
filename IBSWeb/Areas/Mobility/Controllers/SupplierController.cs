using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class SupplierController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<Filpride.Controllers.SupplierController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorageService;

        public SupplierController(IUnitOfWork unitOfWork,
            ILogger<Filpride.Controllers.SupplierController> logger,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
        }

        private async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        private string? GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        private async Task GenerateSignedUrl(FilprideSupplier model)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(model.ProofOfExemptionFileName))
            {
                model.ProofOfExemptionFilePath = await _cloudStorageService.GetSignedUrlAsync(model.ProofOfExemptionFileName);
            }

            if (!string.IsNullOrWhiteSpace(model.ProofOfRegistrationFileName))
            {
                model.ProofOfRegistrationFilePath = await _cloudStorageService.GetSignedUrlAsync(model.ProofOfRegistrationFileName);
            }
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            IEnumerable<MobilitySupplier> suppliers = await _dbContext.MobilitySuppliers
                .Where(c => c.StationCode == stationCodeClaims)
                .ToListAsync(cancellationToken);

            return View(suppliers);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MobilitySupplier model = new();
            model.DefaultExpenses = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.WithholdingTaxList = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.AccountNumber!.Contains("2010302"))
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MobilitySupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
                return View(model);
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (await _dbContext.MobilitySuppliers
                    .AnyAsync(
                        s => s.StationCode == stationCodeClaims && s.SupplierName == model.SupplierName &&
                             s.Category == model.Category, cancellationToken))
            {
                ModelState.AddModelError("SupplierName", "Supplier already exist.");
                return View(model);
            }

            if (await _dbContext.MobilitySuppliers
                    .AnyAsync(
                        s => s.StationCode == stationCodeClaims && s.SupplierTin == model.SupplierTin &&
                             s.Branch == model.Branch && s.Category == model.Category, cancellationToken))
            {
                ModelState.AddModelError("SupplierTin", "Tin number already exist.");
                return View(model);
            }

            model.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

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

                model.SupplierCode = await _unitOfWork.MobilitySupplier
                    .GenerateCodeAsync(stationCodeClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(User);
                model.StationCode = stationCodeClaims;
                await _unitOfWork.MobilitySupplier.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new supplier {model.SupplierCode}", "Supplier", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create supplier master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork.MobilitySupplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);
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

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MobilitySupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            model.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.StationCode = stationCodeClaims;

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
                await _unitOfWork.MobilitySupplier.UpdateAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited supplier #{model.SupplierCode}", "Supplier", nameof(Mobility));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit supplier master file. Edited by: {UserName}", _userManager.GetUserName(User));
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

            var supplier = await _unitOfWork
                .MobilitySupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

                return View(supplier);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork.MobilitySupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                supplier.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Activated supplier {supplier.SupplierCode}", "Supplier", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier activated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate supplier master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Activate), new { id = id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork
                .MobilitySupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

                return View(supplier);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var supplier = await _unitOfWork.MobilitySupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.PaymentTerms = await _unitOfWork.FilprideTerms.GetFilprideTermsListAsyncByCode(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                supplier.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Deactivated supplier {supplier.SupplierCode}", "Supplier", nameof(Mobility));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Supplier deactivated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create customer master file. Created by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Deactivate), new { id = id });
            }
        }
    }
}
