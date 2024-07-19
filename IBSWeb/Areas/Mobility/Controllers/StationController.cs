using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class StationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<StationController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public StationController(IUnitOfWork unitOfWork, ILogger<StationController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<MobilityStation> stations = await _unitOfWork
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
        public async Task<IActionResult> Create(MobilityStation model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _unitOfWork.Station.IsPosCodeExistAsync(model.PosCode, cancellationToken))
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

                model.FolderPath = _unitOfWork.Station.GenerateFolderPath(model.StationName);
                model.CreatedBy = _userManager.GetUserName(User);
                await _unitOfWork.Station.AddAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Station created successfully";
                return RedirectToAction(nameof(Index));
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

            MobilityStation station = await _unitOfWork
                .Station
                .GetAsync(c => c.StationId == id, cancellationToken);

            if (station != null)
            {
                return View(station);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MobilityStation model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.EditedBy = _userManager.GetUserName(User);
                    await _unitOfWork.Station.UpdateAsync(model, cancellationToken);
                    TempData["success"] = "Station updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating station");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(model);
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

            MobilityStation station = await _unitOfWork
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

            MobilityStation station = await _unitOfWork
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

            MobilityStation station = await _unitOfWork
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

            MobilityStation station = await _unitOfWork
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