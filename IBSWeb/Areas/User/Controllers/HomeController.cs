using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Globalization;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext _dbContext;

        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ImportSales()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportSales(CancellationToken cancellationToken)
        {
            var importFolder = Path.Combine("I:", "Other computers", "TARLAC", "SALESTEXT");
            //DateTime yesterday = DateTime.Now.AddDays(-1);

            DateTime yesterday = DateTime.Now.AddDays(-12);

            try
            {
                var files = Directory.GetFiles(importFolder, "*.csv")
                                    .Where(f =>
                                        (f.Contains("fuels", StringComparison.CurrentCultureIgnoreCase) ||
                                         f.Contains("lubes", StringComparison.CurrentCultureIgnoreCase) ||
                                         f.Contains("safedrops", StringComparison.CurrentCultureIgnoreCase))
                                        && Path.GetFileNameWithoutExtension(f).Contains(yesterday.ToString("yyyyMMdd"))
                                    );

                if (files.Any())
                {
                    int fuelsCount = 0;
                    int lubesCount = 0;
                    int safedropsCount = 0;
                    bool HasPoSales = false;

                    foreach (var file in files)
                    {
                        await using var stream = new FileStream(file, FileMode.Open);
                        using var reader = new StreamReader(stream);
                        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HeaderValidated = null,
                            MissingFieldFound = null,
                        });

                        string fileName = Path.GetFileName(file).ToLower();
                        var newRecords = new List<object>();

                        if (fileName.Contains("fuels"))
                        {
                            var records = csv.GetRecords<Fuel>();
                            var existingRecords = await _dbContext.Set<Fuel>().ToListAsync(cancellationToken);
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.nozdown == record.nozdown))
                                {
                                    if (!String.IsNullOrEmpty(record.cust) && !String.IsNullOrEmpty(record.plateno) && !String.IsNullOrEmpty(record.pono))
                                    {
                                        HasPoSales = true;
                                    }

                                    newRecords.Add(record);
                                    fuelsCount++;
                                }
                            }
                        }
                        else if (fileName.Contains("lubes"))
                        {
                            var records = csv.GetRecords<Lube>();
                            var existingRecords = await _dbContext.Set<Lube>().ToListAsync();
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.xStamp == record.xStamp))
                                {
                                    if (!String.IsNullOrEmpty(record.cust) && !String.IsNullOrEmpty(record.plateno) && !String.IsNullOrEmpty(record.pono))
                                    {
                                        HasPoSales = true;
                                    }

                                    newRecords.Add(record);
                                    lubesCount++;
                                }
                            }
                        }
                        else if (fileName.Contains("safedrops"))
                        {
                            var records = csv.GetRecords<SafeDrop>();
                            var existingRecords = await _dbContext.Set<SafeDrop>().ToListAsync();
                            foreach (var record in records)
                            {
                                if (!existingRecords.Exists(existingRecord => existingRecord.xSTAMP == record.xSTAMP))
                                {
                                    newRecords.Add(record);
                                    safedropsCount++;
                                }
                            }
                        }
                        else
                        {
                            // Handle invalid file types
                            continue;
                        }

                        await _dbContext.AddRangeAsync(newRecords, cancellationToken);
                        await _unitOfWork.SaveAsync(cancellationToken);
                    }


                    if (fuelsCount != 0 || lubesCount != 0 || safedropsCount != 0)
                    {
                        await _unitOfWork.SalesHeader.ComputeSalesPerCashier(DateOnly.FromDateTime(yesterday), HasPoSales, cancellationToken);
                    }
                    else
                    {
                        return Json(new
                        {
                            success = true,
                            message = $"You're record is up to date."
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        message = $"CSV files imported successfully.(Fuels: {fuelsCount} records, Lubes: {lubesCount} records, Safe drops: {safedropsCount} records.)"
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No csv file found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}