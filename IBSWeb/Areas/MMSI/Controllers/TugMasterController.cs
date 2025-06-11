using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TugMasterController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public TugMasterController(ApplicationDbContext dbContext, ILogger<TugMasterController> logger, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index(CancellationToken cancallationToken)
        {
            var tugMaster = await _unitOfWork.TugMaster.GetAllAsync(null, cancallationToken);
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
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.TugMaster.AddAsync(model, cancellationToken);
                await _unitOfWork.TugMaster.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tug master.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _unitOfWork.TugMaster.GetAsync(i => i.TugMasterId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _unitOfWork.TugMaster.RemoveAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
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
            var model = await _unitOfWork.TugMaster.GetAsync(a => a.TugMasterId == id, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugMaster model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.TugMaster.GetAsync(t => t.TugMasterId == model.TugMasterId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.TugMasterNumber = model.TugMasterNumber;
                currentModel.TugMasterName = model.TugMasterName;
                currentModel.IsActive = model.IsActive;
                await _unitOfWork.TugMaster.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit tug master.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
