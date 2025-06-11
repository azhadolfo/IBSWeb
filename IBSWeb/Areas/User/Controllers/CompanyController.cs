using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CompanyController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public CompanyController(IUnitOfWork unitOfWork, ILogger<CompanyController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Company> companies = await _unitOfWork
                .Company
                .GetAllAsync();
            return View(companies);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Company model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                bool companyExist = await _unitOfWork
                    .Company
                    .IsCompanyExistAsync(model.CompanyName, cancellationToken);

                if (companyExist)
                {
                    ModelState.AddModelError("CompanyName", "Company already exist.");
                    return View(model);
                }

                bool tinNoExist = await _unitOfWork
                    .Company
                    .IsTinNoExistAsync(model.CompanyTin, cancellationToken);

                if (tinNoExist)
                {
                    ModelState.AddModelError("CompanyTin", "Tin number already exist.");
                    return View(model);
                }

                model.CompanyCode = await _unitOfWork
                    .Company
                    .GenerateCodeAsync(cancellationToken);

                model.CreatedBy = _userManager.GetUserName(User);
                await _unitOfWork.Company.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Company created successfully";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Company model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.Company.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Company updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating company");
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                company.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Company activated successfully";
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                return View(company);
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

            var company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id, cancellationToken);

            if (company != null)
            {
                company.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Company deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}
