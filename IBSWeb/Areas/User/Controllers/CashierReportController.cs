using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CashierReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CashierReportController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        [BindProperty]
        public SalesVM SalesVM { get; set; }

        public CashierReportController(IUnitOfWork unitOfWork, ILogger<CashierReportController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            Expression<Func<SalesHeader, bool>> filter = s => (stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim);

            IEnumerable<SalesHeader> salesHeader = await _unitOfWork
                .SalesHeader
                .GetAllAsync(filter, cancellationToken);

            var salesHeaderWithStationName = _unitOfWork.SalesHeader.GetSalesHeaderJoin(salesHeader, cancellationToken);

            return View(salesHeaderWithStationName);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string? id, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesNo == id, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesNo == id, cancellationToken)
            };

            if (SalesVM.Header == null || SalesVM.Details == null)
            {
                return BadRequest();
            }

            return View(SalesVM);
        }

        public async Task<IActionResult> Post(string id, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(id))
            {
                try
                {
                    var postedBy = _userManager.GetUserName(User);
                    await _unitOfWork.SalesHeader.PostAsync(id, postedBy, cancellationToken);
                    TempData["success"] = "Cashier report approved successfully.";
                    return Redirect($"/User/CashierReport/Preview/{id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting cashier report.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return Redirect($"/User/CashierReport/Preview/{id}");
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesNo == id, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesNo == id, cancellationToken)
            };

            if (SalesVM.Header == null)
            {
                return BadRequest();
            }

            return View(SalesVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalesVM model, double[] closing, double[] opening, CancellationToken cancellationToken)
        {
            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesHeaderId == model.Header.SalesHeaderId, cancellationToken)
            };

            if (String.IsNullOrEmpty(model.Header.Particular))
            {
                ModelState.AddModelError("Header.Particular", "Indicate the reason of this changes.");
                return View(SalesVM);
            }

            try
            {
                model.Header.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.SalesHeader.UpdateAsync(model, closing, opening, cancellationToken);
                TempData["success"] = "Cashier Report updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating cashier report.");
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(SalesVM);
            }
        }
    }
}