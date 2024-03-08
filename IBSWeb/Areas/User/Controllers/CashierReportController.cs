using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class CashierReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CashierReportController> _logger;

        [BindProperty]
        public SalesVM SalesVM { get; set; }

        public CashierReportController(IUnitOfWork unitOfWork, ILogger<CashierReportController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<SalesHeader> salesHeader = await _unitOfWork
                .SalesHeader
                .GetAllAsync();

            return View(salesHeader);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesHeaderId == id, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesHeaderId == id, cancellationToken)
            };

            return View(SalesVM);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            if (id != 0)
            {
                try
                {
                    await _unitOfWork.SalesHeader.PostAsync(id, cancellationToken);
                    TempData["success"] = "Cashier report approved successfully.";
                    return Redirect($"/User/CashierReport/Preview/{id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting cashier report.");
                    TempData["error"] = ex.Message;
                    return Redirect($"/User/CashierReport/Preview/{id}");
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesHeaderId == id, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesHeaderId == id, cancellationToken)
            };

            if (SalesVM != null)
            {
                return View(SalesVM);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalesVM model, double[] closing, double[] opening, CancellationToken cancellationToken)
        {
            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken)
            };

            if (model.Header.ActualCashOnHand < 0)
            {
                ModelState.AddModelError("ActualCashOnHand", "Please enter a value bigger than 0");
                return View(SalesVM);
            }

            if (String.IsNullOrEmpty(model.Header.Particular))
            {
                ModelState.AddModelError("Particular", "Indicate the reason of this changes.");
                return View(SalesVM);
            }

            try
            {
                await _unitOfWork.SalesHeader.UpdateAsync(model, closing, opening, cancellationToken);
                TempData["success"] = "Cashier Report updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating cashier report.");
                TempData["error"] = ex.Message;
                return View(SalesVM);
            }
        }
    }
}