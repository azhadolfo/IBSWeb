using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
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

            Expression<Func<MobilitySalesHeader, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

            IEnumerable<MobilitySalesHeader> salesHeader = await _unitOfWork
                .MobilitySalesHeader
                .GetAllAsync(filter, cancellationToken);

            var salesHeaderWithStationName = _unitOfWork.MobilitySalesHeader.GetSalesHeaderJoin(salesHeader, cancellationToken);

            return View(salesHeaderWithStationName);
        }

        public async Task<IActionResult> Preview(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }
            var station = await _unitOfWork.Station.MapStationToDTO(stationCode, cancellationToken);

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.MobilitySalesHeader.GetAsync(sh => sh.SalesNo == id && sh.StationCode == station.StationCode, cancellationToken),
                Details = await _unitOfWork.MobilitySalesDetail.GetAllAsync(sd => sd.SalesNo == id && sd.StationCode == station.StationCode, cancellationToken)
            };

            if (SalesVM.Header == null || SalesVM.Details == null)
            {
                return BadRequest();
            }

            ViewData["Station"] = $"{station.StationCode} - {station.StationName}";
            return View(SalesVM);
        }

        public async Task<IActionResult> Post(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                try
                {
                    var postedBy = _userManager.GetUserName(User);
                    await _unitOfWork.MobilitySalesHeader.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Cashier report approved successfully.";
                    return RedirectToAction(nameof(Preview), new { id, stationCode });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting cashier report.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(Preview), new { id, stationCode });
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.MobilitySalesHeader.GetAsync(sh => sh.SalesNo == id && sh.StationCode == stationCode, cancellationToken),
                Details = await _unitOfWork.MobilitySalesDetail.GetAllAsync(sd => sd.SalesNo == id && sd.StationCode == stationCode, cancellationToken)
            };

            if (SalesVM.Header == null)
            {
                return BadRequest();
            }

            return View(SalesVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalesVM model, decimal[] closing, decimal[] opening, CancellationToken cancellationToken)
        {
            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.MobilitySalesHeader.GetAsync(sh => sh.SalesHeaderId == model.Header.SalesHeaderId && sh.StationCode == model.Header.StationCode, cancellationToken),
                Details = await _unitOfWork.MobilitySalesDetail.GetAllAsync(sd => sd.SalesHeaderId == model.Header.SalesHeaderId && sd.StationCode == model.Header.StationCode, cancellationToken)
            };

            if (string.IsNullOrEmpty(model.Header.Particular))
            {
                ModelState.AddModelError("Header.Particular", "Indicate the reason of this changes.");
                return View(SalesVM);
            }

            try
            {
                model.Header.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.MobilitySalesHeader.UpdateAsync(model, closing, opening, cancellationToken);
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

        [HttpGet]
        public async Task<IActionResult> AdjustReport(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            var model = new AdjustReportViewModel
            {
                OfflineList = await _unitOfWork.MobilityOffline.GetOfflineListAsync(stationCodeClaim, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustReport(AdjustReportViewModel model, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.MobilityOffline.InsertEntry(model, cancellationToken);

                TempData["success"] = "Adjusted report successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in inserting manual entry.");
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOfflineDetails(int offlineId, CancellationToken cancellationToken = default)
        {
            var offline = await _unitOfWork.MobilityOffline.GetOffline(offlineId, cancellationToken);

            if (offline == null)
            {
                return NotFound();
            }

            var formattedData = new
            {
                StartDate = offline.StartDate.ToString("MMM/dd/yyyy"),
                EndDate = offline.EndDate.ToString("MMM/dd/yyyy"),
                offline.Product,
                offline.Pump,
                FirstDsrOpeningBefore = offline.FirstDsrOpening,
                FirstDsrClosingBefore = offline.FirstDsrClosing,
                SecondDsrOpeningBefore = offline.SecondDsrOpening,
                SecondDsrClosingBefore = offline.SecondDsrClosing,
                Liters = offline.Liters.ToString("N2"),
                Balance = offline.Balance.ToString("N2"),
                offline.FirstDsrNo,
                offline.SecondDsrNo
            };

            return Json(formattedData);
        }
    }
}