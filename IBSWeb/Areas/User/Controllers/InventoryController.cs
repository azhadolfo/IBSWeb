using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

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

        public async Task<IActionResult> GenerateInventoryCosting()
        {
            Inventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductsAsyncByCode(),
                Stations = await _unitOfWork.GetStationAsyncByCode()
            };

            return View(inventory);
        }

        public async Task<IActionResult> InventoryCosting(Inventory model, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {

            IEnumerable<Inventory> inventories;
            Product productDetails = await _unitOfWork.Product.GetAsync(p => p.ProductCode == model.ProductCode, cancellationToken);
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
                Station stationDetails = await _unitOfWork.Station.GetAsync(p => p.StationCode == model.StationCode, cancellationToken);
                ViewData["Station"] = $"{stationDetails.StationCode} {stationDetails.StationName.ToUpper()}";
            }

            ViewData["Product"] = $"{productDetails.ProductCode} {productDetails.ProductName.ToUpper()}";
            return View(inventories);
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory()
        {
            Inventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductsAsyncByCode(),
                Stations = await _unitOfWork.GetStationAsyncByCode()
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
                TempData["error"] = ex.Message;

                model.Products = await _unitOfWork.GetProductsAsyncByCode();
                model.Stations = await _unitOfWork.GetStationAsyncByCode();

                return View(model);
            }
        }

        public IActionResult ViewDetail(string transactionNo, string productCode, string typeOfTransaction)
        {
            if (productCode == null || transactionNo == null)
            {
                return NotFound();
            }

            if (productCode.Contains("PET") && typeOfTransaction == nameof(JournalType.Sales))
            {
                return Redirect($"/User/CashierReport/Preview/{transactionNo}");
            }
            else if (productCode.Contains("PET") && typeOfTransaction == nameof(JournalType.Purchase))
            {
                return Redirect($"/User/Purchase/PreviewFuel/{transactionNo}");
            }
            else
            {
                return Redirect($"/User/Purchase/PreviewLube/{transactionNo}");
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
                .GetAllAsync(l => l.Reference == inventory.TransactionNo, cancellationToken);

            if (inventory != null || ledgerEntries != null)
            {
                foreach (var entry in ledgerEntries)
                {
                    entry.IsValidated = true;
                }

                inventory.ValidatedBy = "Ako";
                inventory.ValidatedDate = DateTime.Now;
                await _unitOfWork.SaveAsync(cancellationToken);

                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }



    }
}
