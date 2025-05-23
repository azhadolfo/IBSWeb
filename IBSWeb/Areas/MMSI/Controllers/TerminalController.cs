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

        public TerminalController(ApplicationDbContext db, IUnitOfWork unitOfWork)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var terminals = _db.MMSITerminals
                .Include(t => t.Port)
                .ToList();

            return View(terminals);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSITerminal model = new()
            {
                Ports = await _unitOfWork.Terminal.GetMMSIPortsById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSITerminal model, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["error"] = "Invalid entry, please try again.";

                    return View(model);
                }

                await _db.MMSITerminals.AddAsync(model, cancellationToken);
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

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSITerminals.FirstOrDefaultAsync(i => i.TerminalId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                _db.MMSITerminals.Remove(model);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Entry deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = _db.MMSITerminals
                .Where(a => a.TerminalId == id)
                .Include(t => t.Port)
                .FirstOrDefault();

            if (model == null)
            {
                return NotFound();
            }

            model.Ports = await _unitOfWork.Terminal.GetMMSIPortsById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSITerminal model, CancellationToken cancellationToken)
        {
            var currentModel = await _db.MMSITerminals.FindAsync(model.TerminalId);

            if (currentModel == null)
            {
                return NotFound();
            }

            currentModel.TerminalNumber = model.TerminalNumber;
            currentModel.TerminalName = model.TerminalName;
            currentModel.PortId = model.PortId;

            await _db.SaveChangesAsync();

            TempData["success"] = "Edited successfully";

            return RedirectToAction(nameof(Index));
        }
    }
}
