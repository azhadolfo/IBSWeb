using IBS.Models;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;
using IBS.Services.Msap;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class TariffRateController(
        ITariffRateService tariffRateService,
        UserManager<ApplicationUser> userManager)
        : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetTariffRateList(CancellationToken cancellationToken)
        {
            var list = await tariffRateService.GetAllAsync(cancellationToken);
            return Json(new { data = list });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var model = new TariffRateViewModel();
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TariffRateViewModel model, CancellationToken cancellationToken = default)
        {
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);

            if (model is { Dispatch: <= 0, BAF: <= 0 })
            {
                ModelState.AddModelError(string.Empty, "Dispatch and BAF value cannot be both zero.");
                await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
                return View(model);
            }

            var result = await tariffRateService.UpsertAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var entity = await tariffRateService.GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                return NotFound();
            }

            var model = new TariffRateViewModel(entity);
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TariffRateViewModel model, CancellationToken cancellationToken)
        {
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);

            var result = await tariffRateService.UpsertAsync(model, userManager.GetUserName(User)!, cancellationToken);

            if (result.IsSuccess)
            {
                TempData["success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["error"] = result.Message;
            await tariffRateService.PopulateSelectListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var list = await tariffRateService.GetTerminalsByPortAsync(portId, cancellationToken);
            return Json(list);
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await tariffRateService.DeleteAsync(id, userManager.GetUserName(User)!, cancellationToken);

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
