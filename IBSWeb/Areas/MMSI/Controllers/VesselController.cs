using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class VesselController : Controller
    {
        private readonly ApplicationDbContext _db;

        public VesselController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var vessels = _db.MMSIVessels.ToList();

            return View(vessels);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIVessel model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSIVessels.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                _db.MMSIVessels.Remove(model);

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

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIVessels.FirstOrDefaultAsync(i => i.VesselId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSIVessels.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIVessels.FirstOrDefaultAsync(a => a.VesselId == id, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIVessel model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSIVessels.FindAsync(model.VesselId);

            if (currentModel == null)
            {
                return NotFound();
            }

            currentModel.VesselNumber = model.VesselNumber;
            currentModel.VesselName = model.VesselName;
            currentModel.VesselType = model.VesselType;
            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
