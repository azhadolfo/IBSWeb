using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TugboatController> _logger;

        public TugboatController(ApplicationDbContext db, IUnitOfWork unitOfWork, ILogger<TugboatController> logger)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var tugboat = _db.MMSITugboats.ToList();

            return View(tugboat);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSITugboat tugboat = new MMSITugboat();
            tugboat.CompanyList = await _unitOfWork.Tugboat.GetMMSICompanyOwnerSelectListById(cancellationToken);

            return View(tugboat);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugboat model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";

                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.IsCompanyOwned)
                {
                    model.TugboatOwnerId = null;
                }

                await _db.MMSITugboats.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

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
                var model = await _db.MMSITugboats.FirstOrDefaultAsync(i => i.TugboatId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSITugboats.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

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
            var model = await _db.MMSITugboats.FirstOrDefaultAsync(a => a.TugboatId == id, cancellationToken);

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
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _db.MMSITugboats.FindAsync(model.TugboatId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.TugboatOwnerId = model.IsCompanyOwned ? null : model.TugboatOwnerId;
                currentModel.IsCompanyOwned = model.IsCompanyOwned;
                currentModel.TugboatNumber = model.TugboatNumber;
                currentModel.TugboatName = model.TugboatName;
                await _db.SaveChangesAsync(cancellationToken);
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
