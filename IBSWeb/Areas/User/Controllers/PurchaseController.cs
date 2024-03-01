﻿using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class PurchaseController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<PurchaseController> _logger;

        private readonly ApplicationDbContext _dbContext;

        public PurchaseController(IUnitOfWork unitOfWork, ILogger<PurchaseController> logger, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dbContext = dbContext;
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
                            lubesCount = await _unitOfWork.LubePurchase.ProcessLubeDelivery(file, cancellationToken);
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

        [HttpGet]
        public async Task<IActionResult> Fuel()
        {
            IEnumerable<FuelPurchase> fuelPurchaseList = await _unitOfWork
                .FuelPurchase
                .GetAllAsync();

            return View(fuelPurchaseList);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductName(string productCode, CancellationToken cancellationToken)
        {
            try
            {
                Product product = await _unitOfWork.Product.GetAsync(p => p.ProductCode == productCode, cancellationToken);

                if (product != null)
                {
                    return Json(new
                    {
                        ProductName = product.ProductName
                    });
                }

                return NotFound("Product not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get product name for code {ProductCode}", productCode);
                return StatusCode(500, "An error occurred while processing your request.");
            }
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

    }
}
