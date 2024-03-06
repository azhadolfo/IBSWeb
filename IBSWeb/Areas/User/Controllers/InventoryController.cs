using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class InventoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IUnitOfWork unitOfWork, ILogger<InventoryController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GenerateInventoryCosting()
        {
            Inventory? inventory = new()
            {
                Products = await _unitOfWork.GetProductsAsyncByCode(),
                Stations = await _unitOfWork.GetStationAsyncByCode()
            };

            return View(inventory);
        }

        [HttpGet]
        public async Task<IActionResult> InventoryCosting(Inventory model, CancellationToken cancellationToken)
        {

            IEnumerable<Inventory> inventories;
            Product productDetails = await _unitOfWork.Product.GetAsync(p => p.ProductCode == model.ProductCode, cancellationToken);

            if (model.StationCode == "ALL")
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == model.ProductCode, cancellationToken);
                ViewData["Station"] = model.StationCode;
            }
            else
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == model.ProductCode && i.StationCode == model.StationCode, cancellationToken);
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
    }
}
