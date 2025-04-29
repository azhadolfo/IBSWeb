using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class PortController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PortController(ApplicationDbContext db)
        {
            _db = db;
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
                var model = _db.MMSIPorts.Where(i => i.PortId == id).FirstOrDefault();

                _db.MMSIPorts.Remove(model);
                _db.SaveChanges();

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    TempData["error"] = ex.InnerException.Message;
                }
                else
                {
                    TempData["error"] = ex.Message;
                }

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = _db.MMSIPorts.Where(a => a.PortId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPort model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSIPorts.FindAsync(model.PortId);

            currentModel.PortNumber = model.PortNumber;
            currentModel.PortName = model.PortName;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
