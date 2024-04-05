using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
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

        [HttpGet]
        public IActionResult ImportPurchase()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportPurchase(CancellationToken cancellationToken)
        {
            string importFolder = Path.Combine("D:", "AzhNewPC", "RealPos", "Feb 5");
            int yearToday = DateTime.Now.Year;

            try
            {
                var files = Directory.GetFiles(importFolder, "*.csv")
                                     .Where(f =>
                                     f.Contains("FUEL_DELIVERY", StringComparison.CurrentCulture) ||
                                     f.Contains("LUBE_DELIVERY", StringComparison.CurrentCulture) ||
                                     f.Contains("PO_SALES", StringComparison.CurrentCulture) &&
                                     Path.GetFileNameWithoutExtension(f).Contains(yearToday.ToString()));

                if (files.Any())
                {
                    int fuelsCount = 0;
                    int lubesCount = 0;
                    int poSalesCount = 0;

                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file).ToLower();

                        if (fileName.Contains("fuel"))
                        {
                            fuelsCount = await _unitOfWork.FuelPurchase.ProcessFuelDelivery(file, cancellationToken);
                        }
                        else if (fileName.Contains("lube"))
                        {
                            lubesCount = await _unitOfWork.LubePurchaseHeader.ProcessLubeDelivery(file, cancellationToken);
                        }
                        else if (fileName.Contains("po_sales"))
                        {
                            poSalesCount = await _unitOfWork.PurchaseOrder.ProcessPOSales(file, cancellationToken);
                        }

                    }

                    if (fuelsCount != 0 || lubesCount != 0 || poSalesCount != 0)
                   {
                        return Json(new
                        {
                            success = true,
                            message = $"Import successfully. Fuel Delivery: {fuelsCount} record(s), Lube Delivery: {lubesCount} record(s), PO Sales: {poSalesCount} record(s)."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = true,
                            message = "You're record is up to date."
                        });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "No CSV file found." });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Fuel(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            Expression<Func<FuelPurchase, bool>> filter = s => (stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim);

            IEnumerable<FuelPurchase> fuelPurchaseList = await _unitOfWork
                .FuelPurchase
                .GetAllAsync(filter, cancellationToken);

            return View(fuelPurchaseList);
        }

        public async Task<IActionResult> PreviewFuel(string? id, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            FuelPurchase? fuelPurchase = await _unitOfWork.FuelPurchase.GetAsync(f => f.FuelPurchaseNo == id, cancellationToken);

            if (fuelPurchase == null)
            {
                return BadRequest();
            }

            Product product = await _unitOfWork.Product.GetAsync(p => p.ProductCode == fuelPurchase.ProductCode, cancellationToken);

            ViewData["ProductName"] = product.ProductName;

            return View(fuelPurchase);
        }

        public async Task<IActionResult> PostFuel(string id, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(id))
            {
                try
                {
                    await _unitOfWork.FuelPurchase.PostAsync(id, cancellationToken);
                    TempData["success"] = "Fuel delivery approved successfully.";
                    return Redirect($"/User/Purchase/PreviewFuel/{id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting fuel delivery.");
                    TempData["error"] = ex.Message;
                    return Redirect($"/User/Purchase/PreviewFuel/{id}");
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> EditFuel(string id, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            FuelPurchase fuelPurchase = await _unitOfWork
                .FuelPurchase
                .GetAsync(s => s.FuelPurchaseNo == id, cancellationToken);

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
                await _unitOfWork.FuelPurchase.UpdateAsync(model, cancellationToken);
                TempData["success"] = "Fuel delivery updated successfully.";
                return RedirectToAction(nameof(Fuel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updating fuel delivery.");
                TempData["error"] = ex.Message;
                return View(model);
            }
        }

        public async Task<IActionResult> Lube(CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            Expression<Func<LubePurchaseHeader, bool>> filter = s => (stationCodeClaim == "ALL" || s.StationCode == stationCodeClaim);

            IEnumerable<LubePurchaseHeader> lubePurchaseHeaders = await _unitOfWork
                .LubePurchaseHeader
                .GetAllAsync(filter, cancellationToken);

            return View(lubePurchaseHeaders);
        }

        public async Task<IActionResult> PreviewLube(string? id, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            LubeDeliveryVM = new LubeDeliveryVM
            {
                Header = await _unitOfWork.LubePurchaseHeader.GetAsync(lh => lh.LubePurchaseHeaderNo == id, cancellationToken),
                Details = await _unitOfWork.LubePurchaseDetail.GetAllAsync(sd => sd.LubePurchaseHeaderNo == id, cancellationToken)
            };

            if (LubeDeliveryVM.Header == null || LubeDeliveryVM.Details == null)
            {
                return BadRequest();
            }

            Supplier? supplier = await _unitOfWork.Supplier.GetAsync(s => s.SupplierCode == LubeDeliveryVM.Header.SupplierCode);

            ViewData["SupplierName"] = supplier.SupplierName;

            return View(LubeDeliveryVM);
        }

        public async Task<IActionResult> PostLube(string id, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(id))
            {
                try
                {
                    await _unitOfWork.LubePurchaseHeader.PostAsync(id, cancellationToken);
                    TempData["success"] = "Lube delivery approved successfully.";
                    return Redirect($"/User/Purchase/PreviewLube/{id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting lube delivery.");
                    TempData["error"] = ex.Message;
                    return Redirect($"/User/Purchase/PreviewLube/{id}");
                }
            }

            return BadRequest();
        }
    }
}
