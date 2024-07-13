using IBS.DataAccess.Repository.IRepository;
using IBS.Dtos;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using IBSWeb.Areas.Mobility.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
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
            Inventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken),
                Stations = await _unitOfWork.GetStationListAsyncByCode(cancellationToken)
            };

            return View(inventory);
        }

        public async Task<IActionResult> InventoryCosting(Inventory model, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            IEnumerable<Inventory> inventories;
            ProductDto productDetails = await _unitOfWork.Product.MapProductToDTO(model.ProductCode, cancellationToken);
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            model.StationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            if (model.StationCode == "ALL")
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.Date >= dateFrom && i.Date <= dateTo, cancellationToken);
                ViewData["Station"] = model.StationCode;
            }
            else
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode && i.Date >= dateFrom && i.Date <= dateTo, cancellationToken);
                StationDto stationDetails = await _unitOfWork.Station.MapStationToDTO(model.StationCode, cancellationToken);
                ViewData["Station"] = $"{stationDetails.StationCode} {stationDetails.StationName.ToUpper()}";
            }

            ViewData["Product"] = $"{productDetails.ProductCode} {productDetails.ProductName.ToUpper()}";
            return View(inventories);
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            Inventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken),
                Stations = await _unitOfWork.GetStationListAsyncByCode(cancellationToken)
            };

            return View(inventory);
        }

        [HttpPost]
        public async Task<IActionResult> BeginningInventory(Inventory model, CancellationToken cancellationToken)
        {
            try
            {
                if (model.StationCode == null)
                {
                    var user = await _userManager.GetUserAsync(User);
                    var claims = await _userManager.GetClaimsAsync(user);
                    model.StationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;
                }

                await _unitOfWork.Inventory.CalculateTheBeginningInventory(model, cancellationToken);
                TempData["success"] = "Beginning inventory saving successfully.";
                return RedirectToAction(nameof(BeginningInventory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in saving the beginning inventory.");
                TempData["error"] = $"Error: '{ex.Message}'";

                model.Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken);
                model.Stations = await _unitOfWork.GetStationListAsyncByCode(cancellationToken);

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

            Inventory inventory = await _unitOfWork.Inventory
                .GetAsync(i => i.InventoryId == id, cancellationToken);

            IEnumerable<GeneralLedger> ledgerEntries = await _unitOfWork.GeneralLedger
                .GetAllAsync(l => l.Reference == inventory.TransactionNo && l.StationCode == inventory.StationCode, cancellationToken);

            if (inventory != null || ledgerEntries != null)
            {
                foreach (var entry in ledgerEntries)
                {
                    entry.IsValidated = true;
                }

                inventory.ValidatedBy = _userManager.GetUserName(User);
                inventory.ValidatedDate = DateTime.Now;
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
                    var inventory = await _unitOfWork.Inventory
                        .GetAsync(i => i.InventoryId == viewModel.InventoryId, cancellationToken);

                    await _unitOfWork.Inventory.CalculateTheActualSounding(inventory, viewModel, cancellationToken);

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

        public async Task<IActionResult> GetLastInventory(string productCode, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCode = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            var lastInventory = await _unitOfWork.Inventory.GetLastInventoryAsync(productCode, stationCode, cancellationToken);

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