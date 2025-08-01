using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TugboatController> _logger;

        public TugboatController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, ILogger<TugboatController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var tugboat = await _unitOfWork.Tugboat.GetAllAsync(null, cancellationToken);
            return View(tugboat);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var tugboat = new MMSITugboat
            {
                CompanyList = await _unitOfWork.Tugboat.GetMMSICompanyOwnerSelectListById(cancellationToken)
            };

            return View(tugboat);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugboat model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.IsCompanyOwned)
                {
                    model.TugboatOwnerId = null;
                }

                await _unitOfWork.Tugboat.AddAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tugboat.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _unitOfWork.Tugboat.GetAsync(i => i.TugboatId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _unitOfWork.Tugboat.RemoveAsync(model, cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete tugboat.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Tugboat.GetAsync(a => a.TugboatId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.CompanyList = await _unitOfWork.Tugboat.GetMMSICompanyOwnerSelectListById(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugboat model, CancellationToken cancellationToken)
        {
            model.CompanyList = await _unitOfWork.Tugboat.GetMMSICompanyOwnerSelectListById(cancellationToken);
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugboatId, cancellationToken);

            if (currentModel == null)
            {
                TempData["info"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.TugboatOwnerId = model.IsCompanyOwned ? null : model.TugboatOwnerId;
                currentModel.IsCompanyOwned = model.IsCompanyOwned;
                currentModel.TugboatNumber = model.TugboatNumber;
                currentModel.TugboatName = model.TugboatName;
                await _unitOfWork.Tugboat.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit tugboat.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
