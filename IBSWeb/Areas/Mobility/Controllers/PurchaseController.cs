using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
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
    public class PurchaseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<PurchaseController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        [BindProperty]
        public LubeDeliveryVM LubeDeliveryVM { get; set; }

        public PurchaseController(IUnitOfWork unitOfWork, ILogger<PurchaseController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Fuel(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            Expression<Func<MobilityFuelPurchase, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

            IEnumerable<MobilityFuelPurchase> fuelPurchaseList = await _unitOfWork
                .MobilityFuelPurchase
                .GetAllAsync(filter, cancellationToken);

            var result = _unitOfWork.MobilityFuelPurchase.GetFuelPurchaseJoin(fuelPurchaseList, cancellationToken);

            return View(result);
        }

        public async Task<IActionResult> PreviewFuel(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            MobilityFuelPurchase? fuelPurchase = await _unitOfWork.MobilityFuelPurchase.GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCode, cancellationToken);

            if (fuelPurchase == null)
            {
                return BadRequest();
            }

            var product = await _unitOfWork.Product.MapProductToDTO(fuelPurchase.ProductCode, cancellationToken);
            var station = await _unitOfWork.Station.MapStationToDTO(fuelPurchase.StationCode, cancellationToken);

            ViewData["ProductName"] = product.ProductName;
            ViewData["Station"] = $"{station.StationCode} - {station.StationName}";

            return View(fuelPurchase);
        }

        public async Task<IActionResult> PostFuel(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(stationCode))
            {
                try
                {
                    var postedBy = _userManager.GetUserName(User);
                    await _unitOfWork.MobilityFuelPurchase.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Fuel delivery approved successfully.";
                    return RedirectToAction(nameof(PreviewFuel), new { id, stationCode });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting fuel delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(PreviewFuel), new { id, stationCode });
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> EditFuel(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            MobilityFuelPurchase fuelPurchase = await _unitOfWork
                .MobilityFuelPurchase
                .GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCode, cancellationToken);

            if (fuelPurchase != null)
            {
                return View(fuelPurchase);
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> EditFuel(MobilityFuelPurchase model, CancellationToken cancellationToken)
        {
            if (model.PurchasePrice < 0)
            {
                ModelState.AddModelError("PurchasePrice", "Please enter a value bigger than 0");
                return View(model);
            }

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.MobilityFuelPurchase.UpdateAsync(model, cancellationToken);
                TempData["success"] = "Fuel delivery updated successfully.";
                return RedirectToAction(nameof(Fuel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating fuel delivery.");
                TempData["error"] = $"Error: '{ex.Message}'";
                return View(model);
            }
        }

        public async Task<IActionResult> Lube(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            Expression<Func<MobilityLubePurchaseHeader, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

            IEnumerable<MobilityLubePurchaseHeader> lubePurchaseHeaders = await _unitOfWork
                .MobilityLubePurchaseHeader
                .GetAllAsync(filter, cancellationToken);

            var result = _unitOfWork.MobilityLubePurchaseHeader.GetLubePurchaseJoin(lubePurchaseHeaders, cancellationToken);

            return View(result);
        }

        public async Task<IActionResult> PreviewLube(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            LubeDeliveryVM = new LubeDeliveryVM
            {
                Header = await _unitOfWork.MobilityLubePurchaseHeader.GetAsync(lh => lh.LubePurchaseHeaderNo == id && lh.StationCode == stationCode, cancellationToken),
                Details = await _unitOfWork.MobilityLubePurchaseDetail.GetAllAsync(sd => sd.LubePurchaseHeaderNo == id && sd.StationCode == stationCode, cancellationToken)
            };

            if (LubeDeliveryVM.Header == null || LubeDeliveryVM.Details == null)
            {
                return BadRequest();
            }

            SupplierDto supplier = await _unitOfWork.Supplier.MapSupplierToDTO(LubeDeliveryVM.Header.SupplierCode, cancellationToken);
            StationDto station = await _unitOfWork.Station.MapStationToDTO(LubeDeliveryVM.Header.StationCode, cancellationToken);

            ViewData["SupplierName"] = supplier.SupplierName;
            ViewData["Station"] = $"{station.StationCode} - {station.StationName}";

            return View(LubeDeliveryVM);
        }

        public async Task<IActionResult> PostLube(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(stationCode))
            {
                try
                {
                    var postedBy = _userManager.GetUserName(User);
                    await _unitOfWork.MobilityLubePurchaseHeader.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Lube delivery approved successfully.";
                    return RedirectToAction(nameof(PreviewLube), new { id, stationCode });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting lube delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(PreviewLube), new { id, stationCode });
                }
            }

            return BadRequest();
        }
    }
}