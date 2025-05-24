using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class PortController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PortController> _logger;

        public PortController(ApplicationDbContext db, ILogger<PortController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var ports = _db.MMSIPorts.ToList();

            return View(ports);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIPort model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSIPorts.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Creation Succeed!";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create port.");
                TempData["error"] = ex.Message;

                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIPorts.FirstOrDefaultAsync(i => i.PortId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSIPorts.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete port.");
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIPorts.FirstOrDefaultAsync(a => a.PortId == id, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPort model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSIPorts.FindAsync(model.PortId);

            if (currentModel == null)
            {
                return NotFound();
            }

            currentModel.PortNumber = model.PortNumber;
            currentModel.PortName = model.PortName;
            currentModel.HasSBMA = model.HasSBMA;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
