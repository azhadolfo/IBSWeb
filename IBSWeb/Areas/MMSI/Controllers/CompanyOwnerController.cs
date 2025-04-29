using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class CompanyOwnerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CompanyOwnerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var companyOwners = _db.MMSICompanyOwners.ToList();

            return View(companyOwners);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSICompanyOwner model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSICompanyOwners.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.InnerException?.Message))
                {
                    TempData["error"] = ex.InnerException.Message;
                }
                else
                {
                    TempData["error"] = ex.Message;
                }

                return View(model);
            }
        }
        public IActionResult Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = _db.MMSICompanyOwners.Where(i => i.MMSICompanyOwnerId == id).FirstOrDefault();

                _db.MMSICompanyOwners.Remove(model);
                _db.SaveChanges();

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = _db.MMSICompanyOwners.Where(a => a.MMSICompanyOwnerId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSICompanyOwner model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSICompanyOwners.FindAsync(model.MMSICompanyOwnerId);
            currentModel.CompanyOwnerNumber = model.CompanyOwnerNumber;
            currentModel.CompanyOwnerName = model.CompanyOwnerName;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
