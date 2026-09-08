using IBS.Models;
using IBS.Models.Msap.MasterFile;
using IBS.Models.Msap.ViewModels;
using IBS.Services.Msap;
using IBS.Models.Enums;
using IBS.Services.Msap.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    [RequireAnyAccess("Access denied. You don't have permission to manage maritime master files.", ProcedureEnum.ManageMaritimeMasterFile)]
    public class TugboatOwnerController(
        ITugboatOwnerService tugboatOwnerService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTugboatOwnerList(CancellationToken cancellationToken)
        {
            var list = await tugboatOwnerService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TugboatOwnerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TugboatOwnerViewModel model, CancellationToken cancellationToken = default)
        {
            var result = await tugboatOwnerService.CreateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await tugboatOwnerService.GetByIdAsync(id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }
            return View(new TugboatOwnerViewModel(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TugboatOwnerViewModel model, CancellationToken cancellationToken)
        {
            var result = await tugboatOwnerService.UpdateAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            return View(model);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await tugboatOwnerService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
