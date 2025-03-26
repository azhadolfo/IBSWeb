using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI
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

        public IActionResult Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = _db.MMSIVessels.Where(i => i.VesselId == id).FirstOrDefault();

                _db.MMSIVessels.Remove(model);
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
            var model = _db.MMSIVessels.Where(a => a.VesselId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIVessel model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSIVessels.FindAsync(model.VesselId);

            currentModel.VesselNumber = model.VesselNumber;
            currentModel.VesselName = model.VesselName;
            currentModel.VesselType = model.VesselType;
            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
