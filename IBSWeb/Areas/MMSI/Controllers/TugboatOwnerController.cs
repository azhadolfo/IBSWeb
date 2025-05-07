using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatOwnerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TugboatOwnerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var companyOwners = _db.MMSITugboatOwners.ToList();

            return View(companyOwners);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugboatOwner model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSITugboatOwners.AddAsync(model, cancellationToken);
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
                var model = _db.MMSITugboatOwners.Where(i => i.TugboatOwnerId == id).FirstOrDefault();

                _db.MMSITugboatOwners.Remove(model);
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
            var model = await _db.MMSITugboatOwners.Where(a => a.TugboatOwnerId == id).
                FirstOrDefaultAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugboatOwner model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSITugboatOwners.FindAsync(model.TugboatOwnerId, cancellationToken);
            currentModel.TugboatOwnerNumber = model.TugboatOwnerNumber;
            currentModel.TugboatOwnerName = model.TugboatOwnerName;
            currentModel.FixedRate = model.FixedRate;

            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
