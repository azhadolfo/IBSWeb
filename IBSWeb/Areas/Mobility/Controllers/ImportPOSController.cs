using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Services;
using Microsoft.AspNetCore.Mvc;
using IBS.Services.Attributes;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.Drawing.Chart;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class ImportPOSController : Controller
    {
        private readonly GoogleDriveImportService _googleDriveImportService;

        private readonly ApplicationDbContext _dbContext;

        public ImportPOSController(GoogleDriveImportService googleDriveImportService, ApplicationDbContext dbContext)
        {
            _googleDriveImportService = googleDriveImportService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            try
            {
                var lastImport = await _dbContext.LogMessages
                    .Where(l => l.LoggerName == "Start")
                    .OrderByDescending(l => l.TimeStamp)
                    .FirstOrDefaultAsync(cancellationToken);

                if (lastImport != null)
                {
                    var lastImportEnd = await _dbContext.LogMessages
                        .Where(l => l.LoggerName == "End" && l.Message == lastImport.Message)
                        .OrderByDescending(l => l.TimeStamp)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (lastImportEnd != null)
                    {
                        var model = await _dbContext.LogMessages
                            .Where(l => l.TimeStamp <= lastImportEnd.TimeStamp &&
                                        l.TimeStamp >= lastImport.TimeStamp
                                        )
                            .OrderByDescending(l => l.TimeStamp)
                            .ToListAsync(cancellationToken);

                        ViewData["LastManualImport"] = lastImport.TimeStamp.ToString("MM/dd/yyyy");
                        return View(model);
                    }

                    ViewData["LastManualImport"] = lastImport.LoggerName;
                    TempData["info"] = "The import is not finished per logging";
                    return View();
                }

                TempData["info"] = "No manual import found";
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> StartImport()
        {
            try
            {
                await _googleDriveImportService.Execute();
                TempData["success"] = "Import was successful";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
