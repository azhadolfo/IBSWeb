using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class PurchaseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<PurchaseController> _logger;

        [BindProperty]
        public LubeDeliveryVM LubeDeliveryVM { get; set; }

        public PurchaseController(IUnitOfWork unitOfWork, ILogger<PurchaseController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ImportDelivery()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportDelivery(CancellationToken cancellationToken)
        {
            string importFolder = Path.Combine("D:", "AzhNewPC", "RealPos", "Feb 5");
            int yearToday = DateTime.Now.Year;

            try
            {
                var files = Directory.GetFiles(importFolder, "*.csv")
                                     .Where(f =>
                                     (f.Contains("FUEL", StringComparison.CurrentCulture) ||
                                     f.Contains("LUBE", StringComparison.CurrentCulture))
                                     && Path.GetFileNameWithoutExtension(f).Contains(yearToday.ToString()));

                if (files.Any())
                {
                    int fuelsCount = 0;
                    int lubesCount = 0;

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

                    }

                    if (fuelsCount != 0 || lubesCount != 0)
                   {
                        return Json(new
                        {
                            success = true,
                            message = $"Import successfully. Fuel Delivery: {fuelsCount} records, Lube Delivery: {lubesCount} records."
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"You're record is up to date."
                        });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "No xls file found." });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public async Task<IActionResult> Fuel()
        {
            IEnumerable<FuelPurchase> fuelPurchaseList = await _unitOfWork
                .FuelPurchase
                .GetAllAsync();

            return View(fuelPurchaseList);
        }

        public async Task<IActionResult> PreviewFuel(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FuelPurchase? fuelPurchase = await _unitOfWork.FuelPurchase.GetAsync(f => f.FuelPurchaseId == id, cancellationToken);

            Product product = await _unitOfWork.Product.GetAsync(p => p.ProductCode == fuelPurchase.ProductCode, cancellationToken);

            ViewData["ProductName"] = product.ProductName;

            return View(fuelPurchase);
        }

        public async Task<IActionResult> PostFuel(int id, CancellationToken cancellationToken)
        {
            if (id != 0)
            {
                try
                {
                    await _unitOfWork.FuelPurchase.PostAsync(id, cancellationToken);
                    TempData["success"] = "Fuel delivery approved successfully.";
                    return RedirectToAction(nameof(Fuel));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on posting fuel delivery.");
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Fuel));
                }
            }

            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> EditFuel(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            FuelPurchase fuelPurchase = await _unitOfWork
                .FuelPurchase
                .GetAsync(s => s.FuelPurchaseId == id, cancellationToken);

            if (fuelPurchase != null)
            {
                return View(fuelPurchase);
            }

            return NotFound();
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

        public async Task<IActionResult> Lube()
        {
            IEnumerable<LubePurchaseHeader> lubePurchaseHeaders = await _unitOfWork
                .LubePurchaseHeader
                .GetAllAsync();

            return View(lubePurchaseHeaders);
        }

        public async Task<IActionResult> PreviewLube(int? id, CancellationToken cancellationToken)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            LubeDeliveryVM = new LubeDeliveryVM
            {
                Header = await _unitOfWork.LubePurchaseHeader.GetAsync(lh => lh.LubeDeliveryHeaderId == id, cancellationToken),
                Details = await _unitOfWork.LubePurchaseDetail.GetAllAsync(sd => sd.LubeDeliveryHeaderId == id, cancellationToken)
            };

            Supplier? supplier = await _unitOfWork.Supplier.GetAsync(s => s.SupplierCode == LubeDeliveryVM.Header.SupplierCode);

            ViewData["SupplierName"] = supplier.SupplierName;

            return View(LubeDeliveryVM);
        }

        public async Task<IActionResult> PostLube(int id, CancellationToken cancellationToken)
        {
            if (id != 0)
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
