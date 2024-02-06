using CsvHelper;
using CsvHelper.Configuration;
using IBS.DataAccess.Data;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Globalization;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ImportCsv()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile[] files)
        {
            if (ModelState.IsValid)
            {
                foreach (IFormFile file in files)
                {
                    if (file == null || file.Length == 0)
                    {
                        return BadRequest("Please select a CSV file to upload.");
                    }

                    using (var reader = new StreamReader(file.OpenReadStream()))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HeaderValidated = null,
                        MissingFieldFound = null,
                    }))
                    {
                        IEnumerable<object> records = null;
                        string fileName = file.FileName.ToLowerInvariant();

                        if (fileName.Contains("fuels"))
                        {
                            records = csv.GetRecords<Fuel>();
                        }
                        else if (fileName.Contains("lubes"))
                        {
                            records = csv.GetRecords<Lube>();
                        }
                        else if (fileName.Contains("safedrops"))
                        {
                            records = csv.GetRecords<SafeDrop>();
                        }
                        else
                        {
                            TempData["error"] = "CSV file is not valid.";
                            return View(records);
                        }

                        await _dbContext.AddRangeAsync(records);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                TempData["success"] = "CSV imported successfully.";
                return RedirectToAction(nameof(ImportCsv));
            }

            return View(files);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}