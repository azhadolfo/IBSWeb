using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

            IEnumerable<Product> products = await _unitOfWork.Product.GetAllAsync();

            var productList = new SelectList(products, "ProductCode", "ProductName");

            IEnumerable<Station> stations = await _unitOfWork.Station.GetAllAsync();

            var stationList = new SelectList(stations, "StationCode", "StationName");

            ViewData["Products"] = productList;
            ViewData["Stations"] = stationList;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> InventoryCosting(string productCode, string stationCode, CancellationToken cancellationToken)
        {
            if (productCode == null)
            {
                TempData["error"] = "Please select product.";
                return RedirectToAction(nameof(GenerateInventoryCosting));
            }

            IEnumerable<Inventory> inventories;
            Product productDetails = await _unitOfWork.Product.GetAsync(p => p.ProductCode == productCode, cancellationToken);

            if (stationCode == null)
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == productCode, cancellationToken);
                ViewData["Station"] = "ALL";
            }
            else
            {
                inventories = await _unitOfWork.Inventory.GetAllAsync(i => i.ProductCode == productCode && i.StationCode == stationCode, cancellationToken);
                Station stationDetails = await _unitOfWork.Station.GetAsync(p => p.StationCode == stationCode, cancellationToken);
                ViewData["Station"] = $"{stationDetails.StationCode} {stationDetails.StationName.ToUpper()}";
            }

            ViewData["Product"] = $"{productDetails.ProductCode} {productDetails.ProductName.ToUpper()}";
            return View(inventories);
        }
    }
}
