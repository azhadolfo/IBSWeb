using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI.MasterFile;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class TugboatOwnerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<TugboatOwnerController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TugboatOwnerController(ApplicationDbContext dbContext, ILogger<TugboatOwnerController> logger, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyOwners = await _unitOfWork.TugboatOwner.GetAllAsync(null, cancellationToken);
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
                TempData["warning"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.TugboatOwner.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Created new Tugboat Owner #{model.TugboatOwnerNumber}", "Tugboat Owner", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

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
                var model = await _unitOfWork.TugboatOwner.GetAsync(i => i.TugboatOwnerId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _unitOfWork.TugboatOwner.RemoveAsync(model, cancellationToken);
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
            var model = await _unitOfWork.TugboatOwner.GetAsync(a => a.TugboatOwnerId == id, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITugboatOwner model, CancellationToken cancellationToken)
        {
            var currentModel = await _unitOfWork.TugboatOwner.GetAsync(t => t.TugboatOwnerId == model.TugboatOwnerId, cancellationToken);

            if (currentModel == null)
            {
                TempData["info"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited Tugboat Owner #{currentModel.TugboatOwnerNumber} => {model.TugboatOwnerNumber}", "Tugboat Owner", nameof(MMSI));
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.TugboatOwnerNumber = model.TugboatOwnerNumber;
                currentModel.TugboatOwnerName = model.TugboatOwnerName;
                currentModel.FixedRate = model.FixedRate;
                await _unitOfWork.SaveAsync(cancellationToken);
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
