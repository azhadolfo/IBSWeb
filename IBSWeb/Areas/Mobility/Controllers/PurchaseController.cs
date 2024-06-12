using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area("Mobility")]
    [Authorize]
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

            Expression<Func<FuelPurchase, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

            IEnumerable<FuelPurchase> fuelPurchaseList = await _unitOfWork
                .FuelPurchase
                .GetAllAsync(filter, cancellationToken);

            var result = _unitOfWork.FuelPurchase.GetFuelPurchaseJoin(fuelPurchaseList, cancellationToken);

            return View(result);
        }

        public async Task<IActionResult> PreviewFuel(string? id, string? stationCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stationCode))
            {
                return NotFound();
            }

            FuelPurchase? fuelPurchase = await _unitOfWork.FuelPurchase.GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCode, cancellationToken);

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
                    await _unitOfWork.FuelPurchase.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Fuel delivery approved successfully.";
                    return Redirect($"/Mobility/Purchase/PreviewFuel/{id}?stationCode={stationCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting fuel delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return Redirect($"/Mobility/Purchase/PreviewFuel/{id}?stationCode={stationCode}");
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

            FuelPurchase fuelPurchase = await _unitOfWork
                .FuelPurchase
                .GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCode, cancellationToken);

            if (fuelPurchase != null)
            {
                return View(fuelPurchase);
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> EditFuel(FuelPurchase model, CancellationToken cancellationToken)
        {
            if (model.PurchasePrice < 0)
            {
                ModelState.AddModelError("PurchasePrice", "Please enter a value bigger than 0");
                return View(model);
            }

            try
            {
                model.EditedBy = _userManager.GetUserName(User);
                await _unitOfWork.FuelPurchase.UpdateAsync(model, cancellationToken);
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

            Expression<Func<LubePurchaseHeader, bool>> filter = s => stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim;

            IEnumerable<LubePurchaseHeader> lubePurchaseHeaders = await _unitOfWork
                .LubePurchaseHeader
                .GetAllAsync(filter, cancellationToken);

            var result = _unitOfWork.LubePurchaseHeader.GetLubePurchaseJoin(lubePurchaseHeaders, cancellationToken);

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
                Header = await _unitOfWork.LubePurchaseHeader.GetAsync(lh => lh.LubePurchaseHeaderNo == id && lh.StationCode == stationCode, cancellationToken),
                Details = await _unitOfWork.LubePurchaseDetail.GetAllAsync(sd => sd.LubePurchaseHeaderNo == id && sd.StationCode == stationCode, cancellationToken)
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
                    await _unitOfWork.LubePurchaseHeader.PostAsync(id, postedBy, stationCode, cancellationToken);
                    TempData["success"] = "Lube delivery approved successfully.";
                    return Redirect($"/Mobility/Purchase/PreviewLube/{id}?stationCode={stationCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting lube delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return Redirect($"/Mobility/Purchase/PreviewLube/{id}?stationCode={stationCode}");
                }
            }

            return BadRequest();
        }
    }
}