using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class HaulerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public HaulerController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var haulers = await _unitOfWork.FilprideHauler
                .GetAllAsync(null, cancellationToken);

            return View(haulers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Hauler model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var companyClaims = await GetCompanyClaimAsync();

                var isHaulerExist = await _unitOfWork.FilprideHauler.IsHaulerNameExistAsync(model.HaulerName, companyClaims, cancellationToken);

                if (!isHaulerExist)
                {
                    model.HaulerCode = await _unitOfWork.FilprideHauler.GenerateCodeAsync(companyClaims, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Company = companyClaims;
                    await _unitOfWork.FilprideHauler.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Hauler created successfully";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("HaulerName", "Hauler already exist.");
                return View(model);
            }
            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var hauler = await _unitOfWork.FilprideHauler.GetAsync(h => h.HaulerId == id, cancellationToken);

            if (hauler == null)
            {
                return NotFound();
            }

            return View(hauler);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Hauler model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.FilprideHauler.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Hauler updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
                }
            }

            return View(model);
        }
    }
}