using System.Linq.Dynamic.Core;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using IBS.Models;
using IBS.Services.Attributes;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class PurchaseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<PurchaseController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public PurchaseController(IUnitOfWork unitOfWork, ILogger<PurchaseController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        [HttpGet]
        public IActionResult Fuel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetFuelPurchase([FromForm] DataTablesParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaim = await GetStationCodeClaimAsync();

                var fuelPurchaseList = await _unitOfWork
                    .MobilityFuelPurchase
                    .GetAllAsync(x => x.StationCode == stationCodeClaim, cancellationToken);

                var query = _unitOfWork.MobilityFuelPurchase.GetFuelPurchaseJoin(fuelPurchaseList, cancellationToken);

                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    query = query
                        .Where(s =>
                            s.stationCode.ToLower().Contains(searchValue) ||
                            s.fuelPurchaseNo.ToLower().Contains(searchValue) ||
                            s.shiftDate.ToString().Contains(searchValue) ||
                            s.productName.ToLower().Contains(searchValue) ||
                            s.receivedBy.ToLower().Contains(searchValue))
                        .ToList();
                }

                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = query.Count();

                var pagedData = query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to get fuel purchases. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Fuel));
            }
        }

        public async Task<IActionResult> PreviewFuel(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var stationCodeClaim = await GetStationCodeClaimAsync();

            var fuelPurchase = await _unitOfWork.MobilityFuelPurchase
                .GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCodeClaim, cancellationToken);

            if (fuelPurchase == null)
            {
                return BadRequest();
            }

            var product = await _unitOfWork.Product.MapProductToDTO(fuelPurchase.ProductCode, cancellationToken);
            var station = await _unitOfWork.MobilityStation.MapStationToDTO(fuelPurchase.StationCode, cancellationToken);

            ViewData["ProductName"] = product!.ProductName;
            ViewData["Station"] = $"{station!.StationCode} - {station.StationName}";

            return View(fuelPurchase);
        }

        public async Task<IActionResult> PostFuel(string? id, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    var stationCodeClaims = await GetStationCodeClaimAsync();

                    if (stationCodeClaims == null)
                    {
                        return BadRequest();
                    }

                    var postedBy = _userManager.GetUserName(User)!;
                    await _unitOfWork.MobilityFuelPurchase.PostAsync(id, postedBy, stationCodeClaims, cancellationToken);
                    TempData["success"] = "Fuel delivery approved successfully.";
                    return RedirectToAction(nameof(PreviewFuel), new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting fuel delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(PreviewFuel), new { id });
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> EditFuel(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var stationCodeClaim = await GetStationCodeClaimAsync();

            var fuelPurchase = await _unitOfWork
                .MobilityFuelPurchase
                .GetAsync(f => f.FuelPurchaseNo == id && f.StationCode == stationCodeClaim, cancellationToken);

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

        [HttpGet]
        public IActionResult Lube(CancellationToken cancellationToken)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetLubePurchase([FromForm] DataTablesParameters parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaim = await GetStationCodeClaimAsync();

                var lubePurchaseHeaders = await _unitOfWork
                    .MobilityLubePurchaseHeader
                    .GetAllAsync(x => x.StationCode == stationCodeClaim, cancellationToken);

                var query = _unitOfWork.MobilityLubePurchaseHeader.GetLubePurchaseJoin(lubePurchaseHeaders, cancellationToken);

                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    query = query
                        .Where(s =>
                            s.stationCode.ToLower().Contains(searchValue) ||
                            s.lubePurchaseHeaderNo.ToLower().Contains(searchValue) ||
                            s.shiftDate.ToString().Contains(searchValue) ||
                            s.supplierName.ToLower().Contains(searchValue) ||
                            s.salesInvoice.ToLower().Contains(searchValue) ||
                            s.receivedBy.ToLower().Contains(searchValue))
                        .ToList();
                }

                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = query.Count();

                var pagedData = query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to get lube purchases. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Fuel));
            }
        }

        public async Task<IActionResult> PreviewLube(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var stationCodeClaim = await GetStationCodeClaimAsync();

            var lube = await _unitOfWork.MobilityLubePurchaseHeader
                .GetAsync(l => l.LubePurchaseHeaderNo == id && l.StationCode == stationCodeClaim, cancellationToken);

            if (lube == null)
            {
                return BadRequest();
            }

            var supplier = await _unitOfWork.FilprideSupplier.MapSupplierToDTO(lube.SupplierCode, cancellationToken);
            var station = await _unitOfWork.MobilityStation.MapStationToDTO(lube.StationCode, cancellationToken);

            ViewData["SupplierName"] = supplier!.SupplierName;
            ViewData["Station"] = $"{station!.StationCode} - {station.StationName}";

            return View(lube);
        }

        public async Task<IActionResult> PostLube(string? id, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    var stationCodeClaims = await GetStationCodeClaimAsync();

                    if (stationCodeClaims == null)
                    {
                        return BadRequest();
                    }

                    var postedBy = _userManager.GetUserName(User)!;
                    await _unitOfWork.MobilityLubePurchaseHeader.PostAsync(id, postedBy, stationCodeClaims, cancellationToken);
                    TempData["success"] = "Lube delivery approved successfully.";
                    return RedirectToAction(nameof(PreviewLube), new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting lube delivery.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(PreviewLube), new { id });
                }
            }

            return BadRequest();
        }
    }
}
