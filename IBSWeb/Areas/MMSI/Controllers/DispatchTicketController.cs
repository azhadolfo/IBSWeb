using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class DispatchTicketController : Controller
    {
        public readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<DispatchTicketController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public DispatchTicketController(ApplicationDbContext db, IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager, ICloudStorageService clousStorageService, ILogger<DispatchTicketController> logger)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = clousStorageService;
            _logger = logger;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var claims = await _userManager.GetClaimsAsync(await _userManager.GetUserAsync(User));
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task UpdateFilterTypeClaim(string filterType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var existingClaim = (await _userManager.GetClaimsAsync(user))
                    .FirstOrDefault(c => c.Type == FilterTypeClaimType);

                if (existingClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, existingClaim);
                }

                if (!string.IsNullOrEmpty(filterType))
                {
                    await _userManager.AddClaimAsync(user, new Claim(FilterTypeClaimType, filterType));
                }
            }
        }

        private async Task<string> GetCurrentFilterType()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
            }
            return null;
        }

        private async Task<string> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.UserName;
        }

        public async Task<IActionResult> Index(string filterType)
        {
            var dispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.Status != "For Posting" && dt.Status != "Cancelled")
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .ToListAsync();

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        public void ReadXLS(string FilePath)
        {
            FileInfo existingFile = new FileInfo(FilePath);
            using (ExcelPackage package = new ExcelPackage(existingFile))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                int colCount = worksheet.Dimension.End.Column;
                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 1; row <= rowCount; row++)
                {
                    for (int col = 1; col <= colCount; col++)
                    {
                        Console.WriteLine(" Row:" + row + " column:" + col + " Value:" + worksheet.Cells[row, col].Value?.ToString().Trim());
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public async Task <IActionResult> UploadExcel(IFormFile excelFile, CancellationToken cancellationToken)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempFiles");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fullPath = Path.Combine(uploadsFolder, excelFile.FileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await excelFile.CopyToAsync(stream);
            }

            string name = excelFile.FileName;
            FileInfo existingFile = new FileInfo(fullPath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(existingFile);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
            {
                return BadRequest("The uploaded Excel file is null.");
            }

            int colCount = worksheet.Dimension.End.Column;
            int rowCount = worksheet.Dimension.End.Row;

            #region == Excel Reading ==

            string dateFormat = "MM/dd/yyyy";
            string timeFormat = "HH:mm";

            for (int row = 9; row <= rowCount; row++)
            {
                string dateFromExcel = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                DateTime datePart = DateTime.ParseExact(dateFromExcel, dateFormat, CultureInfo.InvariantCulture);
                var dispatchNumber = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var cosNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                var dateTimeLeft = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                var dataTimeTimeArrived = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                var tugboat = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                var tugboatName = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                var tugMaster = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                var tugMasterName = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                var vessel = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
                var vesselName = worksheet.Cells[row, 11].Value?.ToString()?.Trim();
                var port = worksheet.Cells[row, 12].Value?.ToString()?.Trim();
                var portName = worksheet.Cells[row, 13].Value?.ToString()?.Trim();
                var berminal = worksheet.Cells[row, 14].Value?.ToString()?.Trim();
                var berminalName = worksheet.Cells[row, 15].Value?.ToString()?.Trim();
                var baseOrStation = worksheet.Cells[row, 16].Value?.ToString()?.Trim();
                var bctivityService = worksheet.Cells[row, 17].Value?.ToString()?.Trim();
                var remarks = worksheet.Cells[row, 18].Value?.ToString()?.Trim();
                var voyageNumber = worksheet.Cells[row, 19].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(dispatchNumber) && string.IsNullOrEmpty(dateTimeLeft) && string.IsNullOrEmpty(dataTimeTimeArrived))
                {
                    continue; // Skip empty rows
                }

                // var model = new ExcelTestModel
                // {
                //     // equate the values gathered to the model instance here
                // };
                //
                // await _db.ExcelTestModels.AddAsync(model, cancellationToken);
            }

            #endregion

            await _db.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets.Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync();

            await GenerateSignedUrl(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SetTariff(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetTariff(MMSIDispatchTicket model, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);

                currentModel.Status = "Tariff Pending";

                currentModel.CustomerId = model.CustomerId;
                currentModel.DispatchChargeType = chargeType;
                currentModel.DispatchRate = model.DispatchRate;
                currentModel.DispatchDiscount = model.DispatchDiscount;
                currentModel.BAFChargeType = chargeType2;
                currentModel.BAFRate = model.BAFRate;
                currentModel.BAFDiscount = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
                currentModel.BAFBillingAmount = model.BAFBillingAmount;
                currentModel.BAFNetRevenue = model.BAFNetRevenue;
                currentModel.TotalBilling = model.TotalBilling;
                currentModel.TotalNetRevenue = model.TotalNetRevenue;

                #region -- Audit Trail

                var audit = new MMSIAuditTrail
                {
                    Date = DateTime.Now,
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Set Tariff:#{currentModel.DispatchTicketId}",
                    DocumentType = "DispatchTicket",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Tariff entered successfully!";

                return RedirectToAction(nameof(Index), new { id = currentModel.DispatchTicketId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == model.DispatchTicketId)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTariff(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

            model.Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditTariff(MMSIDispatchTicket model, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);

                #region -- Changes

                var changes = new List<string>();

                if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                if (currentModel.DispatchChargeType != chargeType) { changes.Add($"DispatchChargeType: {currentModel.DispatchChargeType} -> {chargeType}"); }
                if (currentModel.DispatchRate != model.DispatchRate) { changes.Add($"DispatchRate: {currentModel.DispatchRate} -> {model.DispatchRate}"); }
                if (currentModel.DispatchDiscount != model.DispatchDiscount) { changes.Add($"DispatchDiscount: {currentModel.DispatchDiscount} -> {model.DispatchDiscount}"); }
                if (currentModel.BAFChargeType != chargeType2) { changes.Add($"BAFChargeType: {currentModel.BAFChargeType} -> {chargeType2}"); }
                if (currentModel.BAFRate != model.BAFRate) { changes.Add($"BAFRate: {currentModel.BAFRate} -> {model.BAFRate}"); }
                if (currentModel.BAFDiscount != model.BAFDiscount) { changes.Add($"BAFDiscount: {currentModel.BAFDiscount} -> {model.BAFDiscount}"); }
                if (currentModel.DispatchBillingAmount != model.DispatchBillingAmount) { changes.Add($"DispatchBillingAmount: {currentModel.DispatchBillingAmount} -> {model.DispatchBillingAmount}"); }
                if (currentModel.DispatchNetRevenue != model.DispatchNetRevenue) { changes.Add($"DispatchNetRevenue: {currentModel.DispatchNetRevenue} -> {model.DispatchNetRevenue}"); }
                if (currentModel.BAFBillingAmount != model.BAFBillingAmount) { changes.Add($"BAFBillingAmount: {currentModel.BAFBillingAmount} -> {model.BAFBillingAmount}"); }
                if (currentModel.BAFNetRevenue != model.BAFNetRevenue) { changes.Add($"BAFNetRevenue: {currentModel.BAFNetRevenue} -> {model.BAFNetRevenue}"); }
                if (currentModel.TotalBilling != model.TotalBilling) { changes.Add($"TotalBilling: {currentModel.TotalBilling} -> {model.TotalBilling}"); }
                if (currentModel.TotalNetRevenue != model.TotalNetRevenue) { changes.Add($"TotalNetRevenue: {currentModel.TotalNetRevenue} -> {model.TotalNetRevenue}"); }

                #endregion -- Changes

                currentModel.Status = "Tariff Pending";
                currentModel.CustomerId = model.CustomerId;
                currentModel.DispatchChargeType = chargeType;
                currentModel.DispatchRate = model.DispatchRate;
                currentModel.DispatchDiscount = model.DispatchDiscount;
                currentModel.BAFChargeType = chargeType2;
                currentModel.BAFRate = model.BAFRate;
                currentModel.BAFDiscount = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
                currentModel.BAFBillingAmount = model.BAFBillingAmount;
                currentModel.BAFNetRevenue = model.BAFNetRevenue;
                currentModel.TotalBilling = model.TotalBilling;
                currentModel.TotalNetRevenue = model.TotalNetRevenue;

                #region -- Audit Trail

                var audit = new MMSIAuditTrail
                {
                    Date = DateTime.Now,
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = changes.Any()
                        ? $"Edit Tariff:#{currentModel.DispatchTicketId} {string.Join(", ", changes)}"
                        : $"No changes detected for tariff details #{currentModel.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion -- Audit Trail

                TempData["success"] = "Tariff edited successfully!";

                return RedirectToAction(nameof(Index), new { id = currentModel.DispatchTicketId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == model.DispatchTicketId)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync(cancellationToken);

                return View(model);
            }
        }

        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "For Billing";

                #region -- Audit Trail

                var audit = new MMSIAuditTrail
                {
                    Date = DateTime.Now,
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Approve Tariff:#{model.DispatchTicketId}",
                    DocumentType = "DispatchTicket",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Entry Approved!";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> RevokeApproval(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "Tariff Pending";

                #region -- Audit Trail

                var audit = new MMSIAuditTrail
                {
                    Date = DateTime.Now,
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Approval Revoked:#{model.DispatchTicketId}",
                    DocumentType = "DispatchTicket",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Approval revoked successfully!";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "Disapproved";

                #region -- Audit Trail

                var audit = new MMSIAuditTrail
                {
                    Date = DateTime.Now,
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Disapprove Tariff:#{model.DispatchTicketId}",
                    DocumentType = "DispatchTicket",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Entry Disapproved!";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);

                if (model != null)
                {
                    //if (model.UploadName != null)
                    //{
                    //    string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);

                    //    if (System.IO.File.Exists(filePath))
                    //    {
                    //        System.IO.File.Delete(filePath);
                    //    }
                    //}

                    model.Status = "Cancelled";
                    _db.SaveChanges();
                    TempData["success"] = "Service Request cancelled.";

                    return RedirectToAction(nameof(Index));
                }

                else
                {
                    TempData["error"] = "Can't find entry, please try again.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _db
                .MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalId)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalNumber + " " + t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketList(string status, CancellationToken cancellationToken)
        {
            var item = new List<MMSIDispatchTicket>();
            if (status == "All" || status == null)
            {
                item = await _db.MMSIDispatchTickets
                    .Where(dt => dt.Status != "Cancelled" && dt.Status != "For Posting")
                    .Include(a => a.ActivityService)
                    .Include(a => a.Terminal).ThenInclude(t => t.Port)
                    .Include(a => a.Tugboat)
                    .Include(a => a.TugMaster)
                    .Include(a => a.Vessel)
                    .ToListAsync();
            }
            else
            {
                item = await _db.MMSIDispatchTickets
                    .Where(dt => dt.Status == status)
                    .Include(a => a.ActivityService)
                    .Include(a => a.Terminal).ThenInclude(t => t.Port)
                    .Include(a => a.Tugboat)
                    .Include(a => a.TugMaster)
                    .Include(a => a.Vessel)
                    .ToListAsync(cancellationToken);
            }

            return Json(item);
        }

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketLists([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var queried = _db.MMSIDispatchTickets
                        .Include(dt => dt.ActivityService)
                        .Include(dt => dt.Terminal)
                        .ThenInclude(dt => dt.Port)
                        .Include(dt => dt.Tugboat)
                        .Include(dt => dt.TugMaster)
                        .Include(dt => dt.Vessel)
                        .Where(dt => dt.Status != "For Posting" && dt.Status != "Cancelled");

                // Apply status filter based on filterType
                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "ForPosting":
                            queried = queried.Where(dt =>
                                dt.Status == "For Posting");
                            break;
                        case "ForTariff":
                            queried = queried.Where(dt =>
                                dt.Status == "For Tariff");
                            break;
                        case "TariffPending":
                            queried = queried.Where(dt =>
                                dt.Status == "Tariff Pending");
                            break;
                        case "ForBilling":
                            queried = queried.Where(dt =>
                                dt.Status == "For Billing");
                            break;
                        case "ForCollection":
                            queried = queried.Where(dt =>
                                dt.Status == "For Collection");
                            break;
                        // Add other cases as needed
                    }
                }

                // Global search
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(dt =>
                        dt.COSNumber.ToLower().Contains(searchValue) == true ||
                        dt.DispatchNumber.ToString().Contains(searchValue) == true ||
                        dt.ActivityService.ActivityServiceName.ToString().Contains(searchValue) == true ||
                        dt.Terminal.TerminalName.ToString().Contains(searchValue) == true ||
                        dt.Terminal.Port.PortName.ToString().Contains(searchValue) == true ||
                        dt.Tugboat.TugboatName.ToString().Contains(searchValue) == true ||
                        dt.TugMaster.TugMasterName.ToString().Contains(searchValue) == true ||
                        dt.Vessel.VesselName.ToString().Contains(searchValue) == true ||
                        dt.Status.Contains(searchValue) == true
                        );
                }

                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search?.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();
                        switch (column.Data)
                        {
                            case "status":
                                if (searchValue == "for tariff")
                                {
                                    queried = queried.Where(s => s.Status == "For Tariff");
                                }
                                if (searchValue == "tariff pending")
                                {
                                    queried = queried.Where(s => s.Status == "Tariff Pending");
                                }
                                if (searchValue == "disapproved")
                                {
                                    queried = queried.Where(s => s.Status == "Disapproved");
                                }
                                if (searchValue == "for billing")
                                {
                                    queried = queried.Where(s => s.Status == "For Billing");
                                }
                                if (searchValue == "billed")
                                {
                                    queried = queried.Where(s => s.Status == "Billed");
                                }
                                else
                                {
                                    queried = queried.Where(s => s.Status != null);
                                }
                                break;
                        }
                    }
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = queried.Count();

                var pagedData = queried
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
                _logger.LogError(ex, "Failed to get disbursements.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.ImageName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                model.ImageName = null;
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";

                return RedirectToAction(nameof(Index), new { id = model.DispatchTicketId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> CheckForTariffRate (int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await _db.MMSIDispatchTickets
                .FindAsync(dispatchTicketId, cancellationToken);

            var tariffRate = await _db.MMSITariffRates
                .Where(t => t.CustomerId == customerId &&
                            t.TerminalId == dispatchModel.TerminalId &&
                            t.ActivityServiceId == dispatchModel.ActivityServiceId &&
                            t.AsOfDate <= dispatchModel.DateLeft)
                .OrderByDescending(t => t.AsOfDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (tariffRate != null)
            {
                var result = new
                {
                    Dispatch = tariffRate?.Dispatch ?? 0.00m, // Assuming Rate is a decimal property in MMSITariffRates
                    BAF = tariffRate?.BAF ?? 0.00m, // Example second decimal; replace with your logic
                    Exists = true
                };

                return Json(result);
            }
            else
            {
                var result = new
                {
                    Exists = false
                };

                return Json(result);
            }
        }

        private async Task GenerateSignedUrl(MMSIDispatchTicket model)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(model.ImageName))
            {
                model.ImageSignedUrl = await _cloudStorageService.GetSignedUrlAsync(model.ImageName);
            }
            if (!string.IsNullOrWhiteSpace(model.VideoName))
            {
                model.VideoSignedUrl = await _cloudStorageService.GetSignedUrlAsync(model.VideoName);
            }
        }
    }
}
