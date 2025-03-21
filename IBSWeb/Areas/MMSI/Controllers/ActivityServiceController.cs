using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ActivityServiceController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ActivityServiceController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var activitiesServices = _db.MMSIActivitiesServices.ToList();

            return View(activitiesServices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIActivityService model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSIActivitiesServices.AddAsync(model, cancellationToken);
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
                var model = _db.MMSIActivitiesServices.Where(i => i.ActivityServiceId == id).FirstOrDefault();

                _db.MMSIActivitiesServices.Remove(model);
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
            var model = _db.MMSIActivitiesServices.Where(a => a.ActivityServiceId == id).FirstOrDefault();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIActivityService model, CancellationToken cancellationToken)
        {
            try
            {
                var currentModel = await _db.MMSIActivitiesServices.FindAsync(model.ActivityServiceId);

                currentModel.ActivityServiceNumber = model.ActivityServiceNumber;
                currentModel.ActivityServiceName = model.ActivityServiceName;

                await _db.SaveChangesAsync();

                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["error"] = "An error occurred while editing the entry. Please try again.";

                return View(model);
            }
        }
    }
}
