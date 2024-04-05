using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class StationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<StationController> _logger;

        public StationController(IUnitOfWork unitOfWork, ILogger<StationController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<Station> stations = await _unitOfWork
                .Station
                .GetAllAsync();
            return View(stations);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Station model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _unitOfWork.Station.IsPosCodeExistAsync(model.PosCode,cancellationToken))
                {
                    ModelState.AddModelError("PosCode", "Station POS Code already exist.");
                    return View(model);
                }

                if (await _unitOfWork.Station.IsStationCodeExistAsync(model.StationCode, cancellationToken))
                {
                    ModelState.AddModelError("StationCode", "Station Code already exist.");
                    return View(model);
                }

                if (await _unitOfWork.Station.IsStationNameExistAsync(model.StationName, cancellationToken))
                {
                    ModelState.AddModelError("StationName", "Station Name already exist.");
                    return View(model);
                }

                await _unitOfWork.Station.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Station created successfully";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Station station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                return View(station);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Station model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _unitOfWork.Station.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Station updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating station");
                    TempData["error"] = ex.Message;
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Station station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                return View(station);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Activate")]
        public async Task<IActionResult> ActivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Station station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                station.IsActive = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Station activated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Station station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                return View(station);
            }

            return NotFound();
        }

        [HttpPost, ActionName("Deactivate")]
        public async Task<IActionResult> DeactivatePost(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Station station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                station.IsActive = false;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Station deactivated successfully";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
    }
}
