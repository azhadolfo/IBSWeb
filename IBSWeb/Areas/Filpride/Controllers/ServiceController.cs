using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ServiceController> _logger;

        public ServiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<ServiceController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var services = await _dbContext.FilprideServices.ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.Service))
            {
                return View("ExportIndex", services);
            }

            return View(services);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideService();

            viewModel.CurrentAndPreviousTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.UnearnedTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideService services, CancellationToken cancellationToken)
        {
            services.CurrentAndPreviousTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (await _unitOfWork.FilprideService.IsServicesExist(services.Name, companyClaims, cancellationToken))
                    {
                        ModelState.AddModelError("Name", "Services already exist!");
                        return View(services);
                    }

                    var currentAndPrevious = await _dbContext.FilprideChartOfAccounts
                        .FindAsync(services.CurrentAndPreviousId, cancellationToken);

                    var unearned = await _dbContext.FilprideChartOfAccounts
                        .FindAsync(services.UnearnedId, cancellationToken);

                    services.CurrentAndPreviousNo = currentAndPrevious.AccountNumber;
                    services.CurrentAndPreviousTitle = currentAndPrevious.AccountName;

                    services.UnearnedNo = unearned.AccountNumber;
                    services.UnearnedTitle = unearned.AccountName;

                    services.Company = companyClaims;

                    services.CreatedBy = _userManager.GetUserName(this.User).ToUpper();

                    services.ServiceNo = await _unitOfWork.FilprideService.GetLastNumber(cancellationToken);

                    TempData["success"] = "Services created successfully";

                    await _dbContext.AddAsync(services, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to create service master file. Created by: {UserName}", _userManager.GetUserName(User));
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return View(services);
                }
            }
            return View(services);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideServices == null)
            {
                return NotFound();
            }

            var services = await _dbContext.FilprideServices.FindAsync(id, cancellationToken);
            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FilprideService services, CancellationToken cancellationToken)
        {
            if (id != services.ServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingModel = await _dbContext.FilprideServices.FindAsync(id, cancellationToken);
                    if (existingModel != null)
                    {
                        existingModel.Name = services.Name;
                        existingModel.Percent = services.Percent;
                        existingModel.IsFilpride = services.IsFilpride;
                        existingModel.IsMobility = services.IsMobility;
                        TempData["success"] = "Services updated successfully";

                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Failed to edit service master file. Edited by: {UserName}", _userManager.GetUserName(User));
                    if (!ServicesExists(services.ServiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(services);
        }

        private bool ServicesExists(int id)
        {
            return (_dbContext.FilprideServices?.Any(e => e.ServiceId == id)).GetValueOrDefault();
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.FilprideServices
                .Where(service => recordIds.Contains(service.ServiceId))
                .OrderBy(service => service.ServiceId)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Services");

            worksheet.Cells["A1"].Value = "CurrentAndPreviousTitle";
            worksheet.Cells["B1"].Value = "UneranedTitle";
            worksheet.Cells["C1"].Value = "Name";
            worksheet.Cells["D1"].Value = "Percent";
            worksheet.Cells["E1"].Value = "CreatedBy";
            worksheet.Cells["F1"].Value = "CreatedDate";
            worksheet.Cells["G1"].Value = "CurrentAndPreviousNo";
            worksheet.Cells["H1"].Value = "UnearnedNo";
            worksheet.Cells["I1"].Value = "OriginalServiceId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.CurrentAndPreviousTitle;
                worksheet.Cells[row, 2].Value = item.UnearnedTitle;
                worksheet.Cells[row, 3].Value = item.Name;
                worksheet.Cells[row, 4].Value = item.Percent;
                worksheet.Cells[row, 5].Value = item.CreatedBy;
                worksheet.Cells[row, 6].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 7].Value = item.CurrentAndPreviousNo;
                worksheet.Cells[row, 8].Value = item.UnearnedNo;
                worksheet.Cells[row, 9].Value = item.ServiceId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ServiceList_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllServiceIds()
        {
            var serviceIds = _dbContext.FilprideServices
                                     .Select(s => s.ServiceId) // Assuming Id is the primary key
                                     .ToList();

            return Json(serviceIds);
        }
    }
}
