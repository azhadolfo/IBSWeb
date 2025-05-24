using IBS.DataAccess.Data;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatOwnerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TugboatOwnerController> _logger;

        public TugboatOwnerController(ApplicationDbContext db, ILogger<TugboatOwnerController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var companyOwners = _db.MMSITugboatOwners.ToList();
            return View(companyOwners);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITugboatOwner model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _db.MMSITugboatOwners.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tugboat owner.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSITugboatOwners
                    .FirstOrDefaultAsync(i => i.TugboatOwnerId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSITugboatOwners.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSITugboatOwners.Where(a => a.TugboatOwnerId == id).
                FirstOrDefaultAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugboatOwner model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSITugboatOwners.FindAsync(model.TugboatOwnerId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.TugboatOwnerNumber = model.TugboatOwnerNumber;
                currentModel.TugboatOwnerName = model.TugboatOwnerName;
                currentModel.FixedRate = model.FixedRate;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit tugboat owner.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
