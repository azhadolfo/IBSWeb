using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class InventoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<InventoryController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public InventoryController(IUnitOfWork unitOfWork, ILogger<InventoryController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        private async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public async Task<IActionResult> GenerateInventoryCosting(CancellationToken cancellationToken)
        {
            MobilityInventory? inventory = new()
            {
                Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken),
                Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken)
            };

            return View(inventory);
        }

        public async Task<IActionResult> InventoryCosting(MobilityInventory model, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            IEnumerable<MobilityInventory> inventories;
            var productDetails = await _unitOfWork.Product.MapProductToDTO(model.ProductCode, cancellationToken);
            model.StationCode = stationCodeClaims;

            var endingBalance = await _dbContext.MobilityInventories
                                    .OrderBy(e => e.Date)
                                    .ThenBy(e => e.InventoryId)
                                    .Where(e => e.StationCode == stationCodeClaims && e.ProductCode == model.ProductCode)
                                    .LastOrDefaultAsync(e => e.Date.Month - 1 == dateFrom.Month, cancellationToken)
                                ?? await _dbContext.MobilityInventories
                                    .OrderBy(e => e.Date)
                                    .ThenBy(e => e.InventoryId)
                                    .Where(e => e.StationCode == stationCodeClaims && e.ProductCode == model.ProductCode)
                                    .LastOrDefaultAsync(cancellationToken);

            if (endingBalance != null)
            {
                inventories = await _dbContext.MobilityInventories
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.InventoryId)
                    .Where(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode && i.Date >= dateFrom && i.Date <= dateTo || i.InventoryId == endingBalance.InventoryId)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                inventories = await _dbContext.MobilityInventories
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.InventoryId)
                    .Where(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode && i.Date >= dateFrom && i.Date <= dateTo)
                    .ToListAsync(cancellationToken);
            }

            //inventories = await _unitOfWork.MobilityInventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode && i.Date >= dateFrom && i.Date <= dateTo, cancellationToken);
            var stationDetails = await _unitOfWork.MobilityStation.MapStationToDTO(model.StationCode, cancellationToken);

            ViewData["Station"] = $"{stationDetails!.StationCode} {stationDetails.StationName.ToUpper()}";

            ViewData["Product"] = $"{productDetails!.ProductCode} {productDetails.ProductName.ToUpper()}";
            return View(inventories);
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            MobilityInventory? inventory = new()
            {
                Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken),
                Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken)
            };

            return View(inventory);
        }

        [HttpPost]
        public async Task<IActionResult> BeginningInventory(MobilityInventory model, CancellationToken cancellationToken)
        {
            try
            {
                if (model.StationCode == null)
                {
                    model.StationCode = await GetStationCodeClaimAsync();
                }

                await _unitOfWork.MobilityInventory.CalculateTheBeginningInventory(model, cancellationToken);
                TempData["success"] = "Beginning inventory saving successfully.";
                return RedirectToAction(nameof(BeginningInventory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in saving the beginning inventory.");
                TempData["error"] = $"Error: '{ex.Message}'";

                model.Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken);
                model.Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);

                return View(model);
            }
        }

        public IActionResult ViewDetail(string transactionNo, string productCode, string typeOfTransaction, string stationCode)
        {
            if (productCode == null || transactionNo == null || stationCode == null)
            {
                return NotFound();
            }

            if (productCode.StartsWith("PET") && typeOfTransaction == nameof(JournalType.Sales))
            {
                return RedirectToAction(nameof(CashierReportController.Preview), "CashierReport", new { area = nameof(Mobility), id = transactionNo });
            }

            if (productCode.StartsWith("PET") && typeOfTransaction == nameof(JournalType.Purchase))
            {
                return RedirectToAction(nameof(PurchaseController.PreviewFuel), "Purchase", new { area = nameof(Mobility), id = transactionNo });
            }

            return RedirectToAction(nameof(PurchaseController.PreviewLube), "Purchase", new { area = nameof(Mobility), id = transactionNo });
        }

        [HttpPost]
        public async Task<IActionResult> ValidatePurchases(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var inventory = await _unitOfWork.MobilityInventory
                .GetAsync(i => i.InventoryId == id, cancellationToken);

            if (inventory == null)
            {
                return NotFound();
            }

            var ledgerEntries = await _unitOfWork.MobilityGeneralLedger
                .GetAllAsync(l => l.Reference == inventory.TransactionNo && l.StationCode == inventory.StationCode, cancellationToken);

            foreach (var entry in ledgerEntries)
            {
                entry.IsValidated = true;
            }

            inventory.ValidatedBy = _userManager.GetUserName(User);
            inventory.ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime();
            await _unitOfWork.SaveAsync(cancellationToken);

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ActualSounding(CancellationToken cancellationToken)
        {
            ActualSoundingViewModel viewModel = new()
            {
                Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ActualSounding(ActualSoundingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var inventory = await _unitOfWork.MobilityInventory
                        .GetAsync(i => i.InventoryId == viewModel.InventoryId, cancellationToken);

                    if (inventory == null)
                    {
                        return NotFound();
                    }

                    await _unitOfWork.MobilityInventory.CalculateTheActualSounding(inventory, viewModel, cancellationToken);

                    TempData["success"] = "Actual sounding/count inserted successfully.";
                    return RedirectToAction(nameof(ActualSounding));
                }
                catch (Exception ex)
                {
                    viewModel.Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetLastInventory(string productCode, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var lastInventory = await _unitOfWork.MobilityInventory.GetLastInventoryAsync(productCode, stationCodeClaims, cancellationToken);

            if (lastInventory == null)
            {
                return Json(null);
            }

            return Json(new
            {
                lastInventory.InventoryId,
                PerBook = lastInventory.InventoryBalance,
            });
        }
    }
}
