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
using OfficeOpenXml;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class DispatchTicketController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<DispatchTicketController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public DispatchTicketController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager, ICloudStorageService clousStorageService,
            ILogger<DispatchTicketController> logger, IUserAccessService userAccessService)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = clousStorageService;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filterType)
        {
            var dispatchTickets = await _unitOfWork.DispatchTicket
                .GetAllAsync(dt => dt.Status != "For Posting" && dt.Status != "Cancelled");
            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var companyClaims = await GetCompanyClaimAsync();
            var viewModel = new ServiceRequestViewModel();
            viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
            ViewData["PortId"] = 0;
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                TempData["warning"] = "Can't create entry, please review your input.";
                ViewData["PortId"] = viewModel.Terminal?.Port?.PortId;
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);

            try
            {
                model.Terminal = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
                model.Terminal!.Port = await _unitOfWork.Port.GetAsync(p => p.PortId == model.Terminal.PortId, cancellationToken);
                model = await _unitOfWork.DispatchTicket.GetDispatchTicketLists(model, cancellationToken);
                model.Customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                {
                    model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                    var timeStamp = DateTimeHelper.GetCurrentPhilippineTime();
                    model.CreatedDate = timeStamp;

                    // upload file if something is submitted
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        model.ImageName = GenerateFileNameToSave(imageFile.FileName, "img");
                        model.ImageSavedUrl = await _cloudStorageService.UploadFileAsync(imageFile, model.ImageName!);
                        ViewBag.Message = "Image uploaded successfully!";
                    }

                    if (videoFile != null && videoFile.Length > 0)
                    {
                        model.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                        model.VideoSavedUrl = await _cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                        ViewBag.Message = "Video uploaded successfully!";
                    }

                    model.Status = "For Tariff";
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

                    model.TotalHours = totalHours;
                    await _unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create dispatch ticket #{model.DispatchNumber}",
                        DocumentType = "Dispatch Ticket",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                    };

                    await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                    #endregion --Audit Trail

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = $"Dispatch Ticket #{model.DispatchNumber} was successfully created.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }

                await transaction.RollbackAsync(cancellationToken);
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                TempData["warning"] = "Start Date/Time should be earlier than End Date/Time!";
                ViewData["PortId"] = model.Terminal?.Port?.PortId;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create dispatch ticket.");
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                TempData["error"] = $"{ex.Message}";
                ViewData["PortId"] = model.Terminal?.Port?.PortId;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await GenerateSignedUrl(model);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SetTariff(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

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
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = TariffVmToDispatchTicket(vm);

            try
            {
                var currentModel = await _unitOfWork.DispatchTicket
                    .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                if (currentModel == null)
                {
                    return NotFound();
                }

                #region -- Apply values

                currentModel.Status = "For Approval";
                currentModel.TariffBy = user!.UserName!;
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

                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Apply tariff values

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Set Tariff #{currentModel.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Tariff entered successfully!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to set tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(SetTariff), new { id = vm.DispatchTicketId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTariff(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.SetTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

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
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId });
            }

            var model = TariffVmToDispatchTicket(viewModel);
            var currentModel = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

            if (currentModel == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Audit changes

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

                #endregion -- Audit changes

                #region -- Apply changes

                currentModel.TariffEditedBy = user!.UserName!;
                currentModel.TariffEditedDate = DateTimeHelper.GetCurrentPhilippineTime();
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

                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Apply changes to tariff

                #region -- Audit trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = changes.Any()
                        ? $"Edit tariff #{currentModel.DispatchNumber} {string.Join(", ", changes)}"
                        : $"No changes detected for tariff details #{currentModel.DispatchNumber}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion -- Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Tariff edited successfully!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to edit tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(EditTariff), new { id = viewModel.DispatchTicketId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTicket(int id, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateDispatchTicket, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = DispatchTicketModelToServiceRequestVm(model);
            viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);

            if (!string.IsNullOrEmpty(model.ImageName))
            {
                viewModel.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            }
            if (!string.IsNullOrEmpty(model.VideoName))
            {
                viewModel.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);
            }

            ViewData["PortId"] = model.Terminal?.Port?.PortId;
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditTicket(ServiceRequestViewModel viewModel, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Can't apply edit, please review your input.";
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var model = ServiceRequestVmToDispatchTicket(viewModel);
            var user = await _userManager.GetUserAsync(User);

            try
            {
                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                {
                    var currentModel = await _unitOfWork.DispatchTicket
                        .GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                    if (currentModel == null)
                    {
                        return NotFound();
                    }

                    model.Tugboat = await _unitOfWork.Tugboat.GetAsync(t => t.TugboatId == model.TugBoatId, cancellationToken);
                    model.Customer = await _unitOfWork.FilprideCustomer.GetAsync(t => t.CustomerId == model.CustomerId, cancellationToken);
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
                        model.ImageSavedUrl = await _cloudStorageService.UploadFileAsync(imageFile, model.ImageName!);
                    }

                    if (videoFile != null)
                    {
                        if (!string.IsNullOrEmpty(currentModel.VideoName))
                        {
                            await _cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                        }

                        model.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                        model.VideoSavedUrl = await _cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                    }

                    #region -- Audit changes

                    var changes = new List<string>();
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.DispatchNumber != model.DispatchNumber) { changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {model.DispatchNumber}"); }
                    if (currentModel.COSNumber != model.COSNumber) { changes.Add($"COSNumber: {currentModel.COSNumber} -> {model.COSNumber}"); }
                    if (currentModel.VoyageNumber != model.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
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

                    if (currentModel.TugBoatId != model.TugBoatId && model.Tugboat!.IsCompanyOwned && currentModel.ApOtherTugs != 0)
                    {
                        changes.Add($"ApOtherTugs: '{currentModel.ApOtherTugs}' -> '0'");
                        currentModel.ApOtherTugs = 0;
                    }

                    #endregion -- Audit changes

                    #region -- Apply changes

                    currentModel.EditedBy = user!.UserName;
                    currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
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
                    currentModel.DispatchRate = 0;
                    currentModel.DispatchBillingAmount = 0;
                    currentModel.DispatchDiscount = 0;
                    currentModel.DispatchNetRevenue = 0;
                    currentModel.BAFRate = 0;
                    currentModel.BAFBillingAmount = 0;
                    currentModel.BAFDiscount = 0;
                    currentModel.BAFNetRevenue = 0;
                    currentModel.TotalBilling = 0;
                    currentModel.TotalNetRevenue = 0;
                    currentModel.ApOtherTugs = 0;
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

                    await _unitOfWork.SaveAsync(cancellationToken);

                    #endregion -- Apply changes

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit dispatch ticket #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                            : $"No changes detected for #{currentModel.DispatchNumber}",
                        DocumentType = "Dispatch Ticket",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException(),
                    };

                    await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                    #endregion --Audit Trail

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Entry edited successfully!";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = "Date/Time Left cannot be later than Date/Time Arrived!";
                    ViewData["PortId"] = model.Terminal?.Port?.PortId;
                    return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to edit ticket.");
                TempData["error"] = ex.Message;
                ViewData["PortId"] = model.Terminal?.Port?.PortId;
                return RedirectToAction("EditTicket", new { id = viewModel.DispatchTicketId });
            }
        }

        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Status = "For Billing";
                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Approve tariff #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException(),
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry Approved!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to approve tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> RevokeApproval(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Status = "For Approval";
                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Revoke Approval #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException(),
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Approval revoked successfully!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to revoke tariff approval.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Disapprove(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.ApproveTariff, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Status = "Disapproved";
                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Disapprove Tariff #{model.DispatchTicketId}",
                    DocumentType = "Tariff",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException(),
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Entry Disapproved!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to disapprove tariff.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                TempData["error"] = "Can't find entry, please try again.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Status = "Cancelled";
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Service Request cancelled.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel dispatch ticket.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);
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
            List<MMSIDispatchTicket> item;

            if (status == "All")
            {
                item = (await _unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.Status != "Cancelled" && dt.Status != "For Posting", cancellationToken)).ToList();
            }
            else
            {
                item = (await _unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.Status == status, cancellationToken)).ToList();
            }

            return Json(item);
        }

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketLists([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var filterTypeClaim = await GetCurrentFilterType();
                var queried = _dbContext.MMSIDispatchTickets
                        .Include(dt => dt.Service)
                        .Include(dt => dt.Terminal)
                        .ThenInclude(dt => dt!.Port)
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
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                    .Where(dt =>
                        dt.Date.Day.ToString().Contains(searchValue) == true ||
                        dt.Date.Month.ToString().Contains(searchValue) == true ||
                        dt.Date.Year.ToString().Contains(searchValue) == true ||

                        dt.COSNumber!.ToLower().Contains(searchValue) == true ||
                        dt.DispatchNumber.ToLower().Contains(searchValue) == true ||

                        dt.DateLeft.Day.ToString().Contains(searchValue) == true ||
                        dt.DateLeft.Month.ToString().Contains(searchValue) == true ||
                        dt.DateLeft.Year.ToString().Contains(searchValue) == true ||

                        dt.TimeLeft.Hour.ToString().Contains(searchValue) == true ||
                        dt.TimeLeft.Minute.ToString().Contains(searchValue) == true ||

                        dt.DateArrived.Day.ToString().Contains(searchValue) == true ||
                        dt.DateArrived.Month.ToString().Contains(searchValue) == true ||
                        dt.DateArrived.Year.ToString().Contains(searchValue) == true ||

                        dt.TimeArrived.Hour.ToString().Contains(searchValue) == true ||
                        dt.TimeArrived.Minute.ToString().Contains(searchValue) == true ||

                        dt.Service!.ServiceName.ToLower().Contains(searchValue) == true ||
                        dt.Terminal!.Port!.PortName!.ToLower().Contains(searchValue) == true ||
                        dt.Terminal!.TerminalName!.ToLower().Contains(searchValue) == true ||
                        dt.Tugboat!.TugboatName.ToLower().Contains(searchValue) == true ||
                        dt.Customer!.CustomerName.ToLower().Contains(searchValue) == true ||
                        dt.Vessel!.VesselName.ToLower().Contains(searchValue) == true ||
                        dt.Status.ToLower().Contains(searchValue) == true
                        );
                }

                // Column-specific search
                foreach (var column in parameters.Columns)
                {
                    if (!string.IsNullOrEmpty(column.Search.Value))
                    {
                        var searchValue = column.Search.Value.ToLower();
                        switch (column.Data)
                        {
                            case "status":
                                switch (searchValue)
                                {
                                    case "for tariff":
                                        queried = queried.Where(s => s.Status == "For Tariff");
                                        break;
                                    case "for approval":
                                        queried = queried.Where(s => s.Status == "For Approval");
                                        break;
                                    case "disapproved":
                                        queried = queried.Where(s => s.Status == "Disapproved");
                                        break;
                                    case "for billing":
                                        queried = queried.Where(s => s.Status == "For Billing");
                                        break;
                                    case "billed":
                                        queried = queried.Where(s => s.Status == "Billed");
                                        break;
                                }
                                break;
                        }
                    }
                }

                // Sorting
                if (parameters.Order.Count > 0)
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
                    dispatchTicket.ImageSignedUrl = await GenerateSignedUrl(dispatchTicket.ImageName!);
                }
                foreach (var dispatchTicket in pagedData.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
                {
                    dispatchTicket.VideoSignedUrl = await GenerateSignedUrl(dispatchTicket.VideoName!);
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
                _logger.LogError(ex, "Failed to get dispatch tickets.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.ImageName!);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                model.ImageName = null;
                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete image.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> CheckForTariffRate(int customerId, int dispatchTicketId, CancellationToken cancellationToken)
        {
            var dispatchModel = await _unitOfWork.DispatchTicket
                .GetAsync(dt => dt.DispatchTicketId == dispatchTicketId, cancellationToken);

            if (dispatchModel == null)
            {
                return NotFound();
            }

            var tariffRate = await _unitOfWork.TariffTable
                .GetAsync(t => t.CustomerId == customerId &&
                               t.TerminalId == dispatchModel.TerminalId &&
                               t.ServiceId == dispatchModel.ServiceId &&
                               t.AsOfDate <= dispatchModel.DateLeft, cancellationToken);

            if (tariffRate != null)
            {
                var result = new
                {
                    tariffRate.Dispatch, // Assuming Rate is a decimal property in MMSITariffRates
                    tariffRate.BAF, // Example second decimal; replace with your logic
                    tariffRate.DispatchDiscount,
                    tariffRate.BAFDiscount,
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
            throw new Exception("Upload name invalid.");
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

        private async Task<string?> GetCurrentFilterType()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;

        }

        private async Task<string?> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.UserName;
        }

        private static string GenerateFileNameToSave(string incomingFileName, string type)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{type}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        public MMSIDispatchTicket ServiceRequestVmToDispatchTicket(ServiceRequestViewModel vm)
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
                DispatchRate = vm.DispatchRate ?? 0,
                DispatchDiscount = vm.DispatchDiscount ?? 0,
                DispatchBillingAmount = vm.DispatchBillingAmount,
                DispatchNetRevenue = vm.DispatchNetRevenue,
                BAFRate = vm.BAFRate ?? 0,
                BAFDiscount = vm.BAFDiscount ?? 0,
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
                DispatchTicketId = model.DispatchTicketId,
            };

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

            return viewModel;
        }

        public void ReadXLS(string filePath)
        {
            var existingFile = new FileInfo(filePath);

            using var package = new ExcelPackage(existingFile);
            var worksheet = package.Workbook.Worksheets[1];
            var colCount = worksheet.Dimension.End.Column;
            var rowCount = worksheet.Dimension.End.Row;

            for (var row = 1; row <= rowCount; row++)
            {
                for (var col = 1; col <= colCount; col++)
                {
                    Console.WriteLine(" Row:" + row + " column:" + col + " Value:" + worksheet.Cells[row, col].Value?.ToString()?.Trim());
                }
            }
        }
    }
}
