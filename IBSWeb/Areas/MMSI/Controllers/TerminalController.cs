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
    public class TerminalController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TerminalController> _logger;

        public TerminalController(ApplicationDbContext db, IUnitOfWork unitOfWork, ILogger<TerminalController> logger)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(null, cancellationToken);
            return View(terminals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSITerminal model = new()
            {
                Ports = await _unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITerminal model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _unitOfWork.Terminal.AddAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Creation Succeed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create terminal.");
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _unitOfWork.Terminal.GetAsync(i => i.TerminalId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _unitOfWork.Terminal.RemoveAsync(model, cancellationToken);
                TempData["success"] = "Entry deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete terminal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Terminal.GetAsync(a => a.TerminalId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.Ports = await _unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITerminal model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Invalid entry, please try again.";
                return View(model);
            }

            var currentModel = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);

            if (currentModel == null)
            {
                TempData["error"] = "Entry not found, please try again.";
                return View(model);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                currentModel.TerminalNumber = model.TerminalNumber;
                currentModel.TerminalName = model.TerminalName;
                currentModel.PortId = model.PortId;
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete terminal.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }
    }
}
