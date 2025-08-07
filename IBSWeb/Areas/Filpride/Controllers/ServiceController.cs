using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
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

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ServiceController> _logger;

        public ServiceController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<ServiceController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

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
            var viewModel = new FilprideService
            {
                CurrentAndPreviousTitles = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => coa.Level == 4 || coa.Level == 5)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountId.ToString(),
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                UnearnedTitles = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => coa.Level == 4 || coa.Level == 5)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountId.ToString(),
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken)
            };

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

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(services);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (await _unitOfWork.FilprideService.IsServicesExist(services.Name, companyClaims, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Services already exist!");
                    return View(services);
                }

                var currentAndPrevious = await _unitOfWork.FilprideChartOfAccount
                    .GetAsync(x => x.AccountId == services.CurrentAndPreviousId, cancellationToken);

                var unearned = await _unitOfWork.FilprideChartOfAccount
                    .GetAsync(x => x.AccountId == services.UnearnedId, cancellationToken);

                services.CurrentAndPreviousNo = currentAndPrevious!.AccountNumber;
                services.CurrentAndPreviousTitle = currentAndPrevious.AccountName;

                services.UnearnedNo = unearned!.AccountNumber;
                services.UnearnedTitle = unearned.AccountName;

                services.Company = companyClaims;

                services.CreatedBy = _userManager.GetUserName(User)!.ToUpper();

                services.ServiceNo = await _unitOfWork.FilprideService.GetLastNumber(cancellationToken);

                TempData["success"] = "Services created successfully";

                await _unitOfWork.FilprideService.AddAsync(services, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (
                    _userManager.GetUserName(User)!, $"Create Service #{services.ServiceNo}",
                    "Service", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

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

        [HttpPost]
        public async Task<IActionResult> GetServicesList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _unitOfWork.FilprideService
                    .GetAllAsync(null, cancellationToken);

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    query = query
                    .Where(s =>
                        s.ServiceNo!.ToLower().Contains(searchValue) ||
                        s.Name.ToLower().Contains(searchValue) ||
                        s.Percent.ToString().ToLower().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue) ||
                        s.CreatedDate.ToString("MM dd, yyyy").ToLower().Contains(searchValue)
                        ).ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    query = query
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();
                var pagedData = query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get services.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var services = await _unitOfWork.FilprideService
                .GetAsync(x => x.ServiceId == id, cancellationToken);

            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideService services, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(services);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingModel =  await _unitOfWork.FilprideService
                .GetAsync(x => x.ServiceId == services.ServiceId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            try
            {
                existingModel.Name = services.Name;
                existingModel.Percent = services.Percent;
                existingModel.IsFilpride = services.IsFilpride;
                existingModel.IsMobility = services.IsMobility;
                existingModel.IsBienes = services.IsBienes;
                TempData["success"] = "Services updated successfully";

                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new (
                    _userManager.GetUserName(User)!, $"Edited Service #{existingModel.ServiceNo}",
                    "Service", (await GetCompanyClaimAsync())! );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Failed to edit service master file. Edited by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ServiceList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllServiceIds()
        {
            var serviceIds = _dbContext.FilprideServices
                .Select(s => s.ServiceId)
                .ToList();

            return Json(serviceIds);
        }
    }
}
