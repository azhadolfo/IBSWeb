using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI;
using IBS.Models.MMSI.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<DispatchTicketController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public DispatchTicketController(ApplicationDbContext db, IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager, ICloudStorageService clousStorageService,
            ILogger<DispatchTicketController> logger, IUserAccessService userAccessService)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = clousStorageService;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filterType)
        {
            var dispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.Status != "For Posting" && dt.Status != "Cancelled")
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .ToListAsync();

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task <IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.CreateDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var companyClaims = await GetCompanyClaimAsync();

            var viewModel = new ServiceRequestViewModel();
            viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketLists(viewModel, cancellationToken);
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            ViewData["PortId"] = 0;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

                TempData["error"] = "Can't create entry, please review your input.";
                ViewData["PortId"] = viewModel?.Terminal?.Port?.PortId;

                return View(viewModel);
            }

            var model = ServiceRequestToDispatchTicket(viewModel);

            try
            {
                model.Terminal = await _db.MMSITerminals.FindAsync(model.TerminalId, cancellationToken);
                model.Terminal.Port = await _db.MMSIPorts.FindAsync(model.Terminal.PortId, cancellationToken);
                DateTime timeStamp = DateTime.Now;

                model = await _unitOfWork.DispatchTicket.GetDispatchTicketLists(model, cancellationToken);
                model.Customer = await _db.FilprideCustomers.FindAsync(model.CustomerId, cancellationToken);

                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                {
                    model.CreatedBy = await GetUserNameAsync();
                    timeStamp = DateTime.Now;
                    model.CreatedDate = timeStamp;

                    // upload file if something is submitted
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        model.ImageName = GenerateFileNameToSave(imageFile.FileName, "img");
                        model.ImageSavedUrl = await _cloudStorageService.UploadFileAsync(imageFile, model.ImageName);

                        ViewBag.Message = "Image uploaded successfully!";
                    }
                    if (videoFile != null && videoFile.Length > 0)
                    {
                        model.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                        model.VideoSavedUrl = await _cloudStorageService.UploadFileAsync(videoFile, model.VideoName);

                        ViewBag.Message = "Video uploaded successfully!";
                    }

                    model.Status = "For Tariff";
                    DateTime dateTimeLeft = model.DateLeft.ToDateTime(model.TimeLeft);
                    DateTime dateTimeArrived = model.DateArrived.ToDateTime(model.TimeArrived);
                    TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                    var totalHours = Math.Round((decimal)timeDifference.TotalHours, 2);

                    // find the nearest half hour if the customer is phil-ceb
                    if (model.Customer.CustomerName == "PHIL-CEB MARINE SERVICES INC.")
                    {
                        var wholeHours = Math.Truncate(totalHours);
                        var fractionalPart = totalHours - wholeHours;

                        if (fractionalPart >= 0.75m)
                        {
                            totalHours = wholeHours + 1.0m; // round up to next hour
                        }
                        else if (fractionalPart >= 0.25m)
                        {
                            totalHours = wholeHours + 0.5m; // round to half hour
                        }
                        else
                        {
                            totalHours = wholeHours; // keep as is
                        }
                    }

                    model.TotalHours = totalHours;
                    await _db.MMSIDispatchTickets.AddAsync(model, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    var tempModel =
                        await _db.MMSIDispatchTickets.FirstOrDefaultAsync(dt => dt.CreatedDate == timeStamp);

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create dispatch ticket #{tempModel.DispatchNumber}",
                        DocumentType = "Dispatch Ticket",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion --Audit Trail

                    TempData["success"] = $"Dispatch Ticket #{tempModel.DispatchNumber} was successfully created.";

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketLists(viewModel, cancellationToken);
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    TempData["error"] = "Start Date/Time should be earlier than End Date/Time!";
                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return View(viewModel);
                }

            }
            catch (Exception ex)
            {
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                TempData["error"] = $"{ex.Message}";
                ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                return View(viewModel);
            }
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

            return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets.Where(dt => dt.DispatchTicketId == id)
                .Include(t => t.Service)
                .Include(t => t.Terminal).ThenInclude(t => t.Port)
                .Include(t => t.Tugboat)
                .ThenInclude(t => t.TugboatOwner)
                .Include(t => t.TugMaster)
                .Include(t => t.Vessel)
                .Include((t => t.Customer))
                .FirstOrDefaultAsync();

            await GenerateSignedUrl(model);
            ViewBag.FilterType = await GetCurrentFilterType();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SetTariff(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat).ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(cancellationToken);

            var viewModel = DispatchTicketModelToTariffVm(model);

            viewModel.Customers = await _unitOfWork.DispatchTicket.GetMMSICustomersById(cancellationToken);
            ViewBag.FilterType = await GetCurrentFilterType();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SetTariff(TariffViewModel vm, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId } );
            }

            var model = TariffVmToDispatchTicket(vm);

            try
            {
                var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);

                currentModel.Status = "For Approval";

                currentModel.TariffBy = user.UserName;
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
                currentModel.ApOtherTugs = model.ApOtherTugs;

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Set Tariff #{currentModel.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Tariff entered successfully!";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId } );
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTariff(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.Service)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .ThenInclude(t => t.TugboatOwner)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(cancellationToken);

            var viewModel = DispatchTicketModelToTariffVm(model);

            viewModel.Customers = await _unitOfWork.DispatchTicket.GetMMSICustomersById(cancellationToken);
            ViewBag.FilterType = await GetCurrentFilterType();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditTariff(TariffViewModel viewModel, string chargeType, string chargeType2, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId } );
            }

            var model = TariffVmToDispatchTicket(viewModel);

            try
            {
                var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);

                #region -- Changes

                var changes = new List<string>();

                if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                if (currentModel.DispatchChargeType != chargeType) { changes.Add($"DispatchChargeType: {currentModel.DispatchChargeType} -> {chargeType}"); }
                if (currentModel.BAFChargeType != chargeType2) { changes.Add($"BAFChargeType: {currentModel.BAFChargeType} -> {chargeType2}"); }
                if (currentModel.DispatchRate != model.DispatchRate) { changes.Add($"DispatchRate: {currentModel.DispatchRate} -> {model.DispatchRate}"); }
                if (currentModel.BAFRate != model.BAFRate) { changes.Add($"BAFRate: {currentModel.BAFRate} -> {model.BAFRate}"); }
                if (currentModel.DispatchDiscount != model.DispatchDiscount) { changes.Add($"DispatchDiscount: {currentModel.DispatchDiscount} -> {model.DispatchDiscount}"); }
                if (currentModel.BAFDiscount != model.BAFDiscount) { changes.Add($"BAFDiscount: {currentModel.BAFDiscount} -> {model.BAFDiscount}"); }
                if (currentModel.DispatchBillingAmount != model.DispatchBillingAmount) { changes.Add($"DispatchBillingAmount: {currentModel.DispatchBillingAmount} -> {model.DispatchBillingAmount}"); }
                if (currentModel.BAFBillingAmount != model.BAFBillingAmount) { changes.Add($"BAFBillingAmount: {currentModel.BAFBillingAmount} -> {model.BAFBillingAmount}"); }
                if (currentModel.DispatchNetRevenue != model.DispatchNetRevenue) { changes.Add($"DispatchNetRevenue: {currentModel.DispatchNetRevenue} -> {model.DispatchNetRevenue}"); }
                if (currentModel.BAFNetRevenue != model.BAFNetRevenue) { changes.Add($"BAFNetRevenue: {currentModel.BAFNetRevenue} -> {model.BAFNetRevenue}"); }
                if (currentModel.ApOtherTugs != model.ApOtherTugs) { changes.Add($"ApOtherTugs: {currentModel.ApOtherTugs} -> {model.ApOtherTugs}"); }
                if (currentModel.TotalBilling != model.TotalBilling) { changes.Add($"TotalBilling: {currentModel.TotalBilling} -> {model.TotalBilling}"); }
                if (currentModel.TotalNetRevenue != model.TotalNetRevenue) { changes.Add($"TotalNetRevenue: {currentModel.TotalNetRevenue} -> {model.TotalNetRevenue}"); }

                #endregion -- Changes

                currentModel.TariffEditedBy = user.UserName;
                currentModel.TariffEditedDate = DateTime.Now;
                currentModel.Status = "For Approval";
                currentModel.DispatchChargeType = chargeType;
                currentModel.BAFChargeType = chargeType2;
                currentModel.DispatchRate = model.DispatchRate;
                currentModel.BAFRate = model.BAFRate;
                currentModel.DispatchDiscount = model.DispatchDiscount;
                currentModel.BAFDiscount = model.BAFDiscount;
                currentModel.DispatchBillingAmount = model.DispatchBillingAmount;
                currentModel.BAFBillingAmount = model.BAFBillingAmount;
                currentModel.DispatchNetRevenue = model.DispatchNetRevenue;
                currentModel.BAFNetRevenue = model.BAFNetRevenue;
                currentModel.ApOtherTugs = model.ApOtherTugs;
                currentModel.TotalBilling = model.TotalBilling;
                currentModel.TotalNetRevenue = model.TotalNetRevenue;

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = changes.Any()
                        ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}"
                        : $"No changes detected for tariff details #{currentModel.DispatchNumber}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion -- Audit Trail

                TempData["success"] = "Tariff edited successfully!";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId } );
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTicket(int id, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.CreateDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(cancellationToken);

            var viewModel = DispatchTicketModelToServiceRequestVm(model);

            viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketLists(viewModel, cancellationToken);
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            if (!string.IsNullOrEmpty(model.ImageName))
            {
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            }
            if (!string.IsNullOrEmpty(model.VideoName))
            {
                viewModel.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);
            }

            ViewData["PortId"] = model?.Terminal?.Port?.PortId;
            ViewBag.FilterType = await GetCurrentFilterType();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditTicket(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Can't apply edit, please review your input.";
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }

            var model = ServiceRequestToDispatchTicket(viewModel);

            var user = await _userManager.GetUserAsync(User);

            try
            {
                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                {
                    var currentModel = await _db.MMSIDispatchTickets
                        .FindAsync(model.DispatchTicketId, cancellationToken);
                    model.Tugboat = await _db.MMSITugboats.FindAsync(model.TugBoatId, cancellationToken);
                    model.Customer = await _db.FilprideCustomers.FindAsync(model.CustomerId, cancellationToken);

                    DateTime dateTimeLeft = model.DateLeft.ToDateTime(model.TimeLeft);
                    DateTime dateTimeArrived = model.DateArrived.ToDateTime(model.TimeArrived);
                    TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                    var totalHours = Math.Round((decimal)timeDifference.TotalHours, 2);

                    // find the nearest half hour if the customer is phil-ceb
                    if (model.Customer?.CustomerName == "PHIL-CEB MARINE SERVICES INC.")
                    {
                        var wholeHours = Math.Truncate(totalHours);
                        var fractionalPart = totalHours - wholeHours;

                        if (fractionalPart >= 0.75m)
                        {
                            totalHours = wholeHours + 1.0m; // round up to next hour
                        }
                        else if (fractionalPart >= 0.25m)
                        {
                            totalHours = wholeHours + 0.5m; // round to half hour
                        }
                        else
                        {
                            totalHours = wholeHours; // keep as is
                        }
                    }

                    if (totalHours == 0)
                    {
                        totalHours = 0.5m;
                    }

                    model.TotalHours = totalHours;

                    if (imageFile != null)
                    {
                        // delete existing before replacing
                        if (!string.IsNullOrEmpty(currentModel.ImageName))
                        {
                            await _cloudStorageService.DeleteFileAsync(currentModel.ImageName);
                        }

                        model.ImageName = GenerateFileNameToSave(imageFile.FileName, "img");
                        model.ImageSavedUrl = await _cloudStorageService.UploadFileAsync(imageFile, model.ImageName);
                    }

                    if (videoFile != null)
                    {
                        if (!string.IsNullOrEmpty(currentModel.VideoName))
                        {
                            await _cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                        }

                        model.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                        model.VideoSavedUrl = await _cloudStorageService.UploadFileAsync(videoFile, model.VideoName);
                    }

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.DispatchNumber != model.DispatchNumber) { changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {model.DispatchNumber}"); }
                    if (currentModel.COSNumber  != model.COSNumber) { changes.Add($"COSNumber: {currentModel.COSNumber} -> {model.COSNumber}"); }
                    if (currentModel.VoyageNumber != model.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}"); }
                    if (currentModel.CustomerId  != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.DateLeft != model.DateLeft) { changes.Add($"DateLeft: {currentModel.DateLeft} -> {model.DateLeft}"); }
                    if (currentModel.TimeLeft != model.TimeLeft) { changes.Add($"TimeLeft: {currentModel.TimeLeft} -> {model.TimeLeft}"); }
                    if (currentModel.DateArrived != model.DateArrived) { changes.Add($"DateArrived: {currentModel.DateArrived} -> {model.DateArrived}"); }
                    if (currentModel.TimeArrived != model.TimeArrived) { changes.Add($"TimeArrived: {currentModel.TimeArrived} -> {model.TimeArrived}"); }
                    if (currentModel.TotalHours != model.TotalHours) { changes.Add($"TotalHours: {currentModel.TotalHours} -> {model.TotalHours}"); }
                    if (currentModel.TerminalId != model.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}"); }
                    if (currentModel.Service != model.Service) { changes.Add($"Service: {currentModel.Service} -> {model.Service}"); }
                    if (currentModel.TugBoatId != model.TugBoatId) { changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {model.TugBoatId}"); }
                    if (currentModel.TugMasterId != model.TugMasterId) { changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {model.TugMasterId}"); }
                    if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                    if (currentModel.Remarks != model.Remarks) { changes.Add($"Remarks: '{currentModel.Remarks}' -> '{model.Remarks}'"); }
                    if (imageFile != null && currentModel.ImageName != model.ImageName) { changes.Add($"ImageName: '{currentModel.ImageName}' -> '{model.ImageName}'"); }
                    if (videoFile != null && currentModel.VideoName != model.VideoName) { changes.Add($"VideoName: '{currentModel.VideoName}' -> '{model.VideoName}'"); }

                    if (currentModel.TugBoatId != model.TugBoatId && model.Tugboat.IsCompanyOwned && currentModel.ApOtherTugs != 0)
                    {
                        changes.Add($"ApOtherTugs: '{currentModel.ApOtherTugs}' -> '0'");
                        currentModel.ApOtherTugs = 0;
                    }

                    #endregion -- Changes

                    currentModel.EditedBy = user.UserName;
                    currentModel.EditedDate = DateTime.Now;
                    currentModel.TotalHours = totalHours;
                    currentModel.Date = model.Date;
                    currentModel.DispatchNumber = model.DispatchNumber;
                    currentModel.COSNumber = model.COSNumber;
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.DateLeft = model.DateLeft;
                    currentModel.TimeLeft = model.TimeLeft;
                    currentModel.DateArrived = model.DateArrived;
                    currentModel.TimeArrived = model.TimeArrived;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.ServiceId = model.ServiceId;
                    currentModel.TugBoatId = model.TugBoatId;
                    currentModel.TugMasterId = model.TugMasterId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.Remarks = model.Remarks;

                    // reset the state of tariff
                    currentModel.Status = "For Tariff";
                    currentModel.DispatchRate = default;
                    currentModel.DispatchBillingAmount = default;
                    currentModel.DispatchDiscount = default;
                    currentModel.DispatchNetRevenue = default;
                    currentModel.BAFRate = default;
                    currentModel.BAFBillingAmount = default;
                    currentModel.BAFDiscount = default;
                    currentModel.BAFNetRevenue = default;
                    currentModel.TotalBilling = default;
                    currentModel.TotalNetRevenue = default;
                    currentModel.ApOtherTugs = default;
                    currentModel.TariffBy = string.Empty;
                    currentModel.TariffDate = default;
                    currentModel.TariffEditedBy = string.Empty;
                    currentModel.TariffEditedDate = default;

                    if (imageFile != null)
                    {
                        currentModel.ImageName = model.ImageName;
                        currentModel.ImageSignedUrl = model.ImageSignedUrl;
                        currentModel.ImageSavedUrl = model.ImageSavedUrl;
                    }
                    if (videoFile != null)
                    {
                        currentModel.VideoName = model.VideoName;
                        currentModel.VideoSignedUrl = model.VideoSignedUrl;
                        currentModel.VideoSavedUrl = model.VideoSavedUrl;
                    }

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                            : $"No changes detected for #{currentModel.DispatchNumber}",
                        DocumentType = "Dispatch Ticket",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion --Audit Trail

                    TempData["success"] = "Entry edited successfully!";

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType()});
                }
                else
                {
                    TempData["error"] = "Date/Time Left cannot be later than Date/Time Arrived!";
                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }
        }

        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "For Billing";

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Approve tariff #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Entry Approved!";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType()});
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> RevokeApproval(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "For Approval";

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Revoke Approval #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Approval revoked successfully!";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User), ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "Disapproved";

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync(),
                    MachineName = Environment.MachineName,
                    Activity = $"Disapprove Tariff #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion --Audit Trail

                TempData["success"] = "Entry Disapproved!";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }

                else
                {
                    TempData["error"] = "Can't find entry, please try again.";

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _db
                .MMSITerminals
                .Where(t => t.PortId == portId)
                .OrderBy(t => t.TerminalName)
                .ToListAsync(cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
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
                    .Include(a => a.Service)
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
                    .Include(a => a.Service)
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
                        .Include(dt => dt.Service)
                        .Include(dt => dt.Terminal)
                        .ThenInclude(dt => dt.Port)
                        .Include(dt => dt.Tugboat)
                        .Include(dt => dt.TugMaster)
                        .Include(dt => dt.Vessel)
                        .Include(dt => dt.Customer)
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
                        case "ForApproval":
                            queried = queried.Where(dt =>
                                dt.Status == "For Approval");
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
                        dt.Service.ServiceName.ToString().Contains(searchValue) == true ||
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
                                if (searchValue == "for approval")
                                {
                                    queried = queried.Where(s => s.Status == "For Approval");
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

                foreach (var dispatchTicket in pagedData.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
                {
                    dispatchTicket.ImageSignedUrl = await GenerateSignedUrl(dispatchTicket.ImageName);
                }
                foreach (var dispatchTicket in pagedData.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                {
                    dispatchTicket.VideoSignedUrl = await GenerateSignedUrl(dispatchTicket.VideoName);
                }

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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> CheckForTariffRate (int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await _db.MMSIDispatchTickets
                .FindAsync(dispatchTicketId, cancellationToken);

            var tariffRate = await _db.MMSITariffRates
                .Where(t => t.CustomerId == customerId &&
                            t.TerminalId == dispatchModel.TerminalId &&
                            t.ServiceId == dispatchModel.ServiceId &&
                            t.AsOfDate <= dispatchModel.DateLeft)
                .OrderByDescending(t => t.AsOfDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (tariffRate != null)
            {
                var result = new
                {
                    Dispatch = tariffRate.Dispatch, // Assuming Rate is a decimal property in MMSITariffRates
                    BAF = tariffRate.BAF, // Example second decimal; replace with your logic
                    DispatchDiscount = tariffRate.DispatchDiscount,
                    BAFDiscount = tariffRate.BAFDiscount,
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

        private async Task<string> GenerateSignedUrl(string uploadName)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(uploadName))
            {
                return await _cloudStorageService.GetSignedUrlAsync(uploadName);
            }
            else
            {
                throw new Exception("Upload name invalid.");
            }
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

        private string? GenerateFileNameToSave(string incomingFileName, string type)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{type}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }

        public MMSIDispatchTicket ServiceRequestToDispatchTicket(ServiceRequestViewModel vm)
        {
            var model = new MMSIDispatchTicket
            {
                Date = vm.Date,
                COSNumber = vm.COSNumber,
                DispatchNumber = vm.DispatchNumber,
                VoyageNumber = vm.VoyageNumber,
                CustomerId = vm.CustomerId,
                DateLeft = vm.DateLeft,
                TimeLeft = vm.TimeLeft,
                DateArrived = vm.DateArrived,
                TimeArrived = vm.TimeArrived,
                TerminalId = vm.TerminalId,
                ServiceId = vm.ServiceId,
                TugBoatId = vm.TugBoatId,
                TugMasterId = vm.TugMasterId,
                VesselId = vm.VesselId,
                Remarks = vm.Remarks,
                DispatchChargeType = string.Empty,
                BAFChargeType = string.Empty,
                TariffBy = string.Empty,
                TariffEditedBy = string.Empty,
            };

            if (vm.DispatchTicketId != null)
            {
                model.DispatchTicketId = vm.DispatchTicketId ?? 0;
            }

            return model;
        }

        public MMSIDispatchTicket TariffVmToDispatchTicket(TariffViewModel vm)
        {
            var model = new MMSIDispatchTicket
            {
                DispatchTicketId = vm.DispatchTicketId,
                CustomerId = vm.CustomerId,
                DispatchRate = vm.DispatchRate,
                DispatchDiscount = vm.DispatchDiscount,
                DispatchBillingAmount = vm.DispatchBillingAmount,
                DispatchNetRevenue = vm.DispatchNetRevenue,
                BAFRate = vm.BAFRate,
                BAFDiscount = vm.BAFDiscount,
                BAFBillingAmount = vm.BAFBillingAmount,
                BAFNetRevenue = vm.BAFNetRevenue,
                TotalBilling = vm.TotalBilling,
                TotalNetRevenue = vm.TotalNetRevenue,
                ApOtherTugs = vm.ApOtherTugs ?? 0
            };

            return model;
        }

        public ServiceRequestViewModel DispatchTicketModelToServiceRequestVm(MMSIDispatchTicket model)
        {
            var viewModel = new ServiceRequestViewModel
            {
                Date = model.Date,
                COSNumber = model.COSNumber,
                DispatchNumber = model.DispatchNumber,
                VoyageNumber = model.VoyageNumber,
                CustomerId = model.CustomerId,
                DateLeft = model.DateLeft,
                TimeLeft = model.TimeLeft,
                DateArrived = model.DateArrived,
                TimeArrived = model.TimeArrived,
                TerminalId = model.TerminalId,
                ServiceId = model.ServiceId,
                TugBoatId = model.TugBoatId,
                TugMasterId = model.TugMasterId,
                VesselId = model.VesselId,
                Terminal = model.Terminal,
                Remarks = model.Remarks,
                ImageName = model.ImageName,
                ImageSignedUrl = model.ImageSignedUrl,
                VideoName = model.VideoName,
                VideoSignedUrl = model.VideoSignedUrl,
            };

            if (model.DispatchTicketId != null)
            {
                viewModel.DispatchTicketId = model.DispatchTicketId;
            }

            return viewModel;
        }

        public TariffViewModel DispatchTicketModelToTariffVm(MMSIDispatchTicket model)
        {
            var viewModel = new TariffViewModel
            {
                DispatchTicketId = model.DispatchTicketId,
                DispatchNumber = model.DispatchNumber,
                COSNumber = model.COSNumber,
                VoyageNumber = model.VoyageNumber,
                Date = model.Date,
                TugMasterName = model.TugMaster?.TugMasterName,
                DateLeft = model.DateLeft,
                TimeLeft = model.TimeLeft,
                DateArrived = model.DateArrived,
                TimeArrived = model.TimeArrived,
                TugboatName = model.Tugboat?.TugboatName,
                VesselName = model.Vessel?.VesselName,
                VesselType = model.Vessel?.VesselType,
                TerminalName = model.Terminal?.TerminalName,
                PortName = model.Terminal?.Port?.PortName,
                IsTugboatCompanyOwned = model.Tugboat?.IsCompanyOwned,
                TugboatOwnerName = model.Tugboat?.TugboatOwner?.TugboatOwnerName,
                FixedRate = model.Tugboat?.TugboatOwner?.FixedRate,
                Remarks = model.Remarks,
                CustomerName = model.Customer?.CustomerName,
                TotalHours = model.TotalHours,
                ImageName = model.ImageName,
                DispatchChargeType = model.DispatchChargeType,
                BAFChargeType = model.BAFChargeType,
                CustomerId = model.CustomerId,
                DispatchRate = model.DispatchRate,
                DispatchDiscount = model.DispatchDiscount,
                DispatchBillingAmount = model.DispatchBillingAmount,
                DispatchNetRevenue = model.DispatchNetRevenue,
                BAFRate = model.BAFRate,
                BAFDiscount = model.BAFDiscount,
                BAFBillingAmount = model.BAFBillingAmount,
                BAFNetRevenue = model.BAFNetRevenue,
                TotalBilling = model.TotalBilling,
                TotalNetRevenue = model.TotalNetRevenue,
                ApOtherTugs = model.ApOtherTugs
            };

            if (model?.DispatchTicketId != null)
            {
                viewModel.DispatchTicketId = model.DispatchTicketId;
            }

            return viewModel;
        }
    }
}
