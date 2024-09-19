using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SupplierController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<SupplierController> _logger;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public SupplierController(IUnitOfWork unitOfWork, ILogger<SupplierController> logger, IWebHostEnvironment webHostEnvironment, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            IEnumerable<FilprideSupplier> suppliers = await _unitOfWork.FilprideSupplier
                .GetAllAsync(c => c.Company == companyClaims, cancellationToken);

            return View(suppliers);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            FilprideSupplier model = new();
            model.DefaultExpenses = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            model.WithholdingTaxList = await _dbContext.ChartOfAccounts
                .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideSupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (await _unitOfWork.FilprideSupplier.IsSupplierExistAsync(model.SupplierName, model.Category, companyClaims, cancellationToken))
                {
                    ModelState.AddModelError("SupplierName", "Supplier already exist.");
                    return View(model);
                }

                if (await _unitOfWork.FilprideSupplier.IsTinNoExistAsync(model.SupplierTin, model.Branch, model.Category, companyClaims, cancellationToken))
                {
                    ModelState.AddModelError("SupplierTin", "Tin number already exist.");
                    return View(model);
                }

                if (model.WithholdingTaxtitle != null && model.WithholdingTaxPercent != 0)
                {
                    model.WithholdingTaxPercent = model.WithholdingTaxtitle.StartsWith("2010302") ? 1 : 2;
                }

                try
                {
                    if (registration != null && registration.Length > 0)
                    {
                        string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents", companyClaims, "Proof of Registration", model.SupplierName);

                        model.ProofOfRegistrationFilePath = await _unitOfWork.FilprideSupplier.SaveProofOfRegistration(registration, localPath, cancellationToken);
                    }

                    if (document != null && document.Length > 0)
                    {
                        string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents", companyClaims, "Proof of Exemption", model.SupplierName);

                        model.ProofOfExemptionFilePath = await _unitOfWork.FilprideSupplier.SaveProofOfRegistration(document, localPath, cancellationToken);
                    }

                    model.SupplierCode = await _unitOfWork.FilprideSupplier
                        .GenerateCodeAsync(companyClaims, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Company = companyClaims;
                    await _unitOfWork.FilprideSupplier.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Supplier created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on creating supplier.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Make sure to fill all the required details.");
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

            FilprideSupplier supplier = await _unitOfWork.FilprideSupplier.GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.DefaultExpenses = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

                supplier.WithholdingTaxList = await _dbContext.ChartOfAccounts
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
        public async Task<IActionResult> Edit(FilprideSupplier model, IFormFile? registration, IFormFile? document, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var companyClaims = await GetCompanyClaimAsync();

                    if (registration != null && registration.Length > 0)
                    {
                        string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents", companyClaims, "Proof of Registration", model.SupplierName);

                        model.ProofOfRegistrationFilePath = await _unitOfWork.FilprideSupplier.SaveProofOfRegistration(registration, localPath, cancellationToken);
                    }

                    if (document != null && document.Length > 0)
                    {
                        string localPath = Path.Combine(_webHostEnvironment.WebRootPath, "documents", companyClaims, "Proof of Exemption", model.SupplierName);

                        model.ProofOfExemptionFilePath = await _unitOfWork.FilprideSupplier.SaveProofOfRegistration(document, localPath, cancellationToken);
                    }

                    if (model.WithholdingTaxtitle != null && model.WithholdingTaxPercent != 0)
                    {
                        model.WithholdingTaxPercent = model.WithholdingTaxtitle.StartsWith("2010302") ? 1 : 2;
                    }


                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideSupplier.UpdateAsync(model, cancellationToken);

                    TempData["success"] = "Supplier updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating supplier.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideSupplier supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
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

            FilprideSupplier supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Supplier activated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FilprideSupplier supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
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

            FilprideSupplier supplier = await _unitOfWork
                .FilprideSupplier
                .GetAsync(c => c.SupplierId == id, cancellationToken);

            if (supplier != null)
            {
                supplier.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Supplier deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}