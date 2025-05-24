using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugMasterController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TugMasterController> _logger;

        public TugMasterController(ApplicationDbContext db, ILogger<TugMasterController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var tugMaster = _db.MMSITugMasters.ToList();

            return View(tugMaster);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugMaster model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSITugMasters.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tug master.");
                TempData["error"] = ex.Message;

                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSITugMasters
                    .FirstOrDefaultAsync(i => i.TugMasterId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSITugMasters.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete tug master.");
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSITugMasters
                .FirstOrDefaultAsync(a => a.TugMasterId == id, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugMaster model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSITugMasters.FindAsync(model.TugMasterId);

            if (currentModel == null)
            {
                return NotFound();
            }

            currentModel.TugMasterNumber = model.TugMasterNumber;
            currentModel.TugMasterName = model.TugMasterName;
            currentModel.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
