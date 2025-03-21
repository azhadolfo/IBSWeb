using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TugboatController(ApplicationDbContext db)
        {
            _db = db;//sample test
        }

        public IActionResult Index()
        {
            var tugboat = _db.MMSITugboats.ToList();

            return View(tugboat);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugboat model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSITugboats.AddAsync(model, cancellationToken);
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
                var model = _db.MMSITugboats.Where(i => i.TugboatId == id).FirstOrDefault();

                _db.MMSITugboats.Remove(model);
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
            var model = _db.MMSITugboats.Where(a => a.TugboatId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugboat model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSITugboats.FindAsync(model.TugboatId);

            currentModel.TugboatNumber = model.TugboatNumber;
            currentModel.TugboatName = model.TugboatName;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
