using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
        public async Task<IActionResult> Create(Company model)
        {
            if (ModelState.IsValid)
            {
                bool companyExist = await _unitOfWork
                    .Company
                    .IsCompanyExistAsync(model.CompanyName);

                if (companyExist)
                {
                    ModelState.AddModelError("CompanyName", "Company already exist.");
                    return View(model);
                }

                bool tinNoExist = await _unitOfWork
                    .Company
                    .IsTinNoExistAsync(model.CompanyTin);

                if (tinNoExist)
                {
                    ModelState.AddModelError("CompanyTin", "Tin number already exist.");
                    return View(model);
                }

                model.CompanyCode = await _unitOfWork
                    .Company
                    .GenerateCodeAsync();

                await _unitOfWork.Company.AddAsync(model);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Company created successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Company company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Company model)
        {
            if (ModelState.IsValid)
            {
                await _unitOfWork.Company.UpdateAsync(model);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Company updated successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Company company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Company company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id);

            if (company != null)
            {
                company.IsActive = true;
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Company activated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Company company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id);

            if (company != null)
            {
                return View(company);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Company company = await _unitOfWork
                .Company
                .GetAsync(c => c.CompanyId == id);

            if (company != null)
            {
                company.IsActive = false;
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Company deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}