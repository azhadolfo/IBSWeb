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
    public class PortController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PortController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public PortController(ApplicationDbContext dbContext, ILogger<PortController> logger, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var ports = await _unitOfWork.Port.GetAllAsync(null, cancellationToken);
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

                await _unitOfWork.Port.AddAsync(model, cancellationToken);

                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Created new Port #{model.PortNumber}", "Port", nameof(MMSI));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

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
            var model = await _unitOfWork.Port.GetAsync(i => i.PortId == id, cancellationToken);
            if (model == null) return NotFound();
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.Port.RemoveAsync(model, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete port.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Port.GetAsync(a => a.PortId == id, cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIPort model, CancellationToken cancellationToken)
        {
            var currentModel = await _unitOfWork.Port.GetAsync(p => p.PortId == model.PortId, cancellationToken);

            if (currentModel == null) return NotFound();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit Trail Recording --

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Edited Port #{currentModel.PortNumber} => {model.PortNumber}", "Port", nameof(MMSI));
                await _dbContext.FilprideAuditTrails.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail Recording --

                currentModel.PortNumber = model.PortNumber;
                currentModel.PortName = model.PortName;
                currentModel.HasSBMA = model.HasSBMA;
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit port.");
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
            }
        }
    }
}
