using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class InventoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<InventoryController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public InventoryController(IUnitOfWork unitOfWork, ILogger<InventoryController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> GenerateInventoryCosting(CancellationToken cancellationToken)
        {
            MobilityInventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken),
                Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken)
            };

            return View(inventory);
        }

        public async Task<IActionResult> InventoryCosting(MobilityInventory model, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            IEnumerable<MobilityInventory> inventories;
            ProductDto productDetails = await _unitOfWork.Product.MapProductToDTO(model.ProductCode, cancellationToken);
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            model.StationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            if (model.StationCode == "ALL")
            {
                inventories = await _unitOfWork.MobilityInventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.Date >= dateFrom && i.Date <= dateTo, cancellationToken);
                ViewData["Station"] = model.StationCode;
            }
            else
            {
                inventories = await _unitOfWork.MobilityInventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode && i.Date >= dateFrom && i.Date <= dateTo, cancellationToken);
                StationDto stationDetails = await _unitOfWork.MobilityStation.MapStationToDTO(model.StationCode, cancellationToken);
                ViewData["Station"] = $"{stationDetails.StationCode} {stationDetails.StationName.ToUpper()}";
            }

            ViewData["Product"] = $"{productDetails.ProductCode} {productDetails.ProductName.ToUpper()}";
            return View(inventories);
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            MobilityInventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken),
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
                    var user = await _userManager.GetUserAsync(User);
                    var claims = await _userManager.GetClaimsAsync(user);
                    model.StationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;
                }

                await _unitOfWork.MobilityInventory.CalculateTheBeginningInventory(model, cancellationToken);
                TempData["success"] = "Beginning inventory saving successfully.";
                return RedirectToAction(nameof(BeginningInventory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in saving the beginning inventory.");
                TempData["error"] = $"Error: '{ex.Message}'";

                model.Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken);
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
                return RedirectToAction(nameof(CashierReportController.Preview), "CashierReport", new { area = nameof(Mobility), id = transactionNo, stationCode });
            }
            else if (productCode.StartsWith("PET") && typeOfTransaction == nameof(JournalType.Purchase))
            {
                return RedirectToAction(nameof(PurchaseController.PreviewFuel), "Purchase", new { area = nameof(Mobility), id = transactionNo, stationCode });
            }
            else
            {
                return RedirectToAction(nameof(PurchaseController.PreviewLube), "Purchase", new { area = nameof(Mobility), id = transactionNo, stationCode });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidatePurchases(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            MobilityInventory inventory = await _unitOfWork.MobilityInventory
                .GetAsync(i => i.InventoryId == id, cancellationToken);

            IEnumerable<MobilityGeneralLedger> ledgerEntries = await _unitOfWork.MobilityGeneralLedger
                .GetAllAsync(l => l.Reference == inventory.TransactionNo && l.StationCode == inventory.StationCode, cancellationToken);

            if (inventory != null || ledgerEntries != null)
            {
                foreach (var entry in ledgerEntries)
                {
                    entry.IsValidated = true;
                }

                inventory.ValidatedBy = _userManager.GetUserName(User);
                inventory.ValidatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                await _unitOfWork.SaveAsync(cancellationToken);

                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ActualSounding(CancellationToken cancellationToken)
        {
            ActualSoundingViewModel viewModel = new()
            {
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken)
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

                    await _unitOfWork.MobilityInventory.CalculateTheActualSounding(inventory, viewModel, cancellationToken);

                    TempData["success"] = "Actual sounding inserted successfully.";
                    return RedirectToAction(nameof(ActualSounding));
                }
                catch (Exception ex)
                {
                    viewModel.Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetLastInventory(string productCode, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            var lastInventory = await _unitOfWork.MobilityInventory.GetLastInventoryAsync(productCode, stationCode, cancellationToken);

            if (lastInventory == null)
            {
                return NotFound();
            }

            return Json(new
            {
                lastInventory.InventoryId,
                PerBook = lastInventory.InventoryBalance,
            });
        }
    }
}
