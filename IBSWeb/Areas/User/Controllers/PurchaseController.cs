using CsvHelper;
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
                            fuelsCount = await _unitOfWork.FuelDelivery.ProcessFuelDelivery(file, cancellationToken);
                        }
                        else if (fileName.Contains("lube"))
                        {
                            lubesCount = await _unitOfWork.LubeDelivery.ProcessLubeDelivery(file, cancellationToken);
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

                return View();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

    }
}
