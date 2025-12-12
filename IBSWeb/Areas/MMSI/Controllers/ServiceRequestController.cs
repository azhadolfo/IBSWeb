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
using IBS.Utility.Helpers;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ServiceRequestController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<ServiceRequestController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public ServiceRequestController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ICloudStorageService cloudStorageService,
            ILogger<ServiceRequestController> logger, IUserAccessService userAccessService)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _userAccessService = userAccessService;
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

        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            var dispatchTickets = await _unitOfWork.DispatchTicket.GetAllAsync(dt => dt.Status == "For Posting", cancellationToken);
            var currentUser = await _userManager.GetUserAsync(User);

            if (User.IsInRole("PortCoordinator"))
            {
                dispatchTickets = dispatchTickets.Where(t => t.CreatedBy == currentUser!.UserName)
                    .ToList();
            }

            var mmsiDispatchTickets = dispatchTickets.ToList();
            foreach (var dispatchTicket in mmsiDispatchTickets.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
            {
                dispatchTicket.ImageSignedUrl = await GenerateSignedUrl(dispatchTicket.ImageName!);
            }
            foreach (var dispatchTicket in mmsiDispatchTickets.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
            {
                dispatchTicket.VideoSignedUrl = await GenerateSignedUrl(dispatchTicket.VideoName!);
            }

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateServiceRequest, cancellationToken))
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
            var model = ServiceRequestVmToDispatchTicketModel(viewModel);
            model.Terminal = await _unitOfWork.Terminal.GetAsync(t => t.TerminalId == model.TerminalId, cancellationToken);
            model.Terminal!.Port = await _unitOfWork.Port.GetAsync(p => p.PortId == model.Terminal.PortId, cancellationToken);

            try
            {
                model = await _unitOfWork.DispatchTicket.GetDispatchTicketLists(model, cancellationToken);

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

                    if (model.DateLeft != null && model.DateArrived != null && model.TimeLeft != null &&
                        model.TimeArrived != null)
                    {
                        model.Status = "For Posting";
                        model.Customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);
                        var dateTimeLeft = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
                        var dateTimeArrived = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);
                        var timeDifference = dateTimeArrived - dateTimeLeft;
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
                    }

                    await _unitOfWork.DispatchTicket.AddAsync(model, cancellationToken);

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create service request #{model.DispatchNumber}",
                        DocumentType = "Service Request",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                    };

                    await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                    #endregion --Audit Trail

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = $"Service Request #{model.DispatchNumber} was successfully created.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                    viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(await GetCompanyClaimAsync() ?? throw new InvalidOperationException(), cancellationToken);
                    TempData["warning"] = "Start Date/Time should be earlier than End Date/Time!";
                    ViewData["PortId"] = model.Terminal?.Port?.PortId;
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create service request.");
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(await GetCompanyClaimAsync() ?? throw new InvalidOperationException(), cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = $"{ex.Message}";
                ViewData["PortId"] = model.Terminal?.Port?.PortId;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var viewModel = DispatchTicketModelToServiceRequestVm(model);
            viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);

            if (!string.IsNullOrEmpty(viewModel.ImageName))
            {
                viewModel.ImageSignedUrl = await GenerateSignedUrl(viewModel.ImageName);
            }
            if (!string.IsNullOrEmpty(viewModel.VideoName))
            {
                viewModel.VideoSignedUrl = await GenerateSignedUrl(viewModel.VideoName);
            }

            ViewData["PortId"] = viewModel.Terminal?.Port?.PortId;
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ServiceRequestViewModel vm, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Can't apply edit, please review your input.";
                return RedirectToAction("Edit", new { id = vm.DispatchTicketId });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var user = await _userManager.GetUserAsync(User);
            var model = ServiceRequestVmToDispatchTicketModel(vm);
            var currentModel = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

            if (currentModel == null)
            {
                return NotFound();
            }

            try
            {
                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                {
                    TimeSpan timeDifference = default;

                    if (model.DateLeft != null && model.DateArrived != null && model.TimeLeft != null &&
                        model.TimeArrived != null)
                    {
                        // calculate for the hours of the new entry
                        DateTime dateTimeLeft = model.DateLeft.Value.ToDateTime(model.TimeLeft.Value);
                        DateTime dateTimeArrived = model.DateArrived.Value.ToDateTime(model.TimeArrived.Value);
                        timeDifference = dateTimeArrived - dateTimeLeft;
                        var totalHours = Math.Round((decimal)timeDifference.TotalHours, 2);

                        // find the nearest half hour if the new customer is phil-ceb
                        model.Customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

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

                            if (totalHours == 0)
                            {
                                totalHours = 0.5m;
                            }
                        }

                        model.TotalHours = totalHours;
                    }

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
                        // delete existing before replacing
                        if (!string.IsNullOrEmpty(currentModel.VideoName))
                        {
                            await _cloudStorageService.DeleteFileAsync(currentModel.VideoName);
                        }

                        model.VideoName = GenerateFileNameToSave(videoFile.FileName, "vid");
                        model.VideoSavedUrl = await _cloudStorageService.UploadFileAsync(videoFile, model.VideoName!);
                    }

                    #region -- Audit changes

                    var changes = new List<string>();
                    if (currentModel.Date != model.Date) { changes.Add($"CreateDate: {currentModel.Date} -> {model.Date}"); }
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
                    if (currentModel.ServiceId != model.ServiceId) { changes.Add($"ServiceId: {currentModel.ServiceId} -> {model.ServiceId}"); }
                    if (currentModel.TugBoatId != model.TugBoatId) { changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {model.TugBoatId}"); }
                    if (currentModel.TugMasterId != model.TugMasterId) { changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {model.TugMasterId}"); }
                    if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                    if (currentModel.Remarks != model.Remarks) { changes.Add($"Remarks: '{currentModel.Remarks}' -> '{model.Remarks}'"); }
                    if (imageFile != null && currentModel.ImageName != model.ImageName) { changes.Add($"ImageName: '{currentModel.ImageName}' -> '{model.ImageName}'"); }
                    if (videoFile != null && currentModel.VideoName != model.VideoName) { changes.Add($"VideoName: '{currentModel.VideoName}' -> '{model.VideoName}'"); }

                    #endregion -- Audit changes

                    #region -- Apply changes

                    currentModel.EditedBy = user!.UserName;
                    currentModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    currentModel.Date = model.Date;
                    currentModel.DispatchNumber = model.DispatchNumber;
                    currentModel.COSNumber = model.COSNumber;
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.DateLeft = model.DateLeft;
                    currentModel.TimeLeft = model.TimeLeft;
                    currentModel.DateArrived = model.DateArrived;
                    currentModel.TimeArrived = model.TimeArrived;
                    currentModel.TotalHours = (decimal)timeDifference.TotalHours;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.ServiceId = model.ServiceId;
                    currentModel.TugBoatId = model.TugBoatId;
                    currentModel.TugMasterId = model.TugMasterId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.Remarks = model.Remarks;
                    currentModel.TotalHours = model.TotalHours;
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
                            ? $"Edit service request #{currentModel.DispatchNumber}, {string.Join(", ", changes)}"
                            : $"No changes detected: id#{currentModel.DispatchNumber}",
                        DocumentType = "Service Request",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
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
                    model = await _dbContext.MMSIDispatchTickets
                        .Include(dt => dt.Terminal)
                        .ThenInclude(t => t!.Port)
                        .FirstOrDefaultAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                    var viewModel = DispatchTicketModelToServiceRequestVm(model!);
                    viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                    ViewData["PortId"] = viewModel.Terminal?.Port?.PortId;
                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                var companyClaims = await GetCompanyClaimAsync();
                _logger.LogError(ex, "Failed to edit service request.");
                TempData["error"] = ex.Message;

                // get the model values from db
                model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == model!.DispatchTicketId, cancellationToken);

                var viewModel = DispatchTicketModelToServiceRequestVm(model!);
                viewModel = await _unitOfWork.ServiceRequest.GetDispatchTicketSelectLists(viewModel, cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims!, cancellationToken);
                ViewData["PortId"] = model?.Terminal?.Port?.PortId;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken = default)
        {
            var terminals = await _unitOfWork.Terminal.GetAllAsync(t => t.PortId == portId, cancellationToken);

            var terminalsList = terminals.Select(t => new SelectListItem
            {
                Value = t.TerminalId.ToString(),
                Text = t.TerminalName
            }).ToList();

            return Json(terminalsList);
        }

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketLists([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var filterTypeClaim = await GetCurrentFilterType();

                var queried = await _dbContext.MMSIDispatchTickets
                    .Include(dt => dt.Service)
                    .Include(dt => dt.Terminal)
                    .ThenInclude(dt => dt!.Port)
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Vessel)
                    .Where(dt => dt.Status == "For Posting" || dt.Status == "Cancelled")
                    .ToListAsync(cancellationToken);

                // Apply status filter based on filterType
                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "ForPosting":
                            queried = queried.Where(dt =>
                                dt.Status == "For Posting").ToList();
                            break;
                        case "ForTariff":
                            queried = queried.Where(dt =>
                                dt.Status == "For Tariff").ToList();
                            break;
                        case "TariffPending":
                            queried = queried.Where(dt =>
                                dt.Status == "Tariff Pending").ToList();
                            break;
                        case "ForBilling":
                            queried = queried.Where(dt =>
                                dt.Status == "For Billing").ToList();
                            break;
                        case "ForCollection":
                            queried = queried.Where(dt =>
                                dt.Status == "For Collection").ToList();
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
                        dt.COSNumber!.ToLower().Contains(searchValue) == true ||
                        (dt.DispatchNumber != null && dt.DispatchNumber.ToLower().Contains(searchValue)) ||
                        dt.Service!.ServiceName.ToString().Contains(searchValue) == true ||
                        dt.Terminal!.TerminalName!.ToString().Contains(searchValue) == true ||
                        dt.Terminal.Port!.PortName!.ToString().Contains(searchValue) == true ||
                        dt.Tugboat!.TugboatName.ToString().Contains(searchValue) == true ||
                        dt.TugMaster!.TugMasterName.ToString().Contains(searchValue) == true ||
                        dt.Vessel!.VesselName.ToString().Contains(searchValue) == true ||
                        dt.Status.Contains(searchValue) == true
                        )
                        .ToList();
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
                                if (searchValue == "for posting")
                                {
                                    queried = queried.Where(s => s.Status == "For Posting").ToList();
                                }
                                if (searchValue == "cancelled")
                                {
                                    queried = queried.Where(s => s.Status == "Cancelled").ToList();
                                }
                                else
                                {
                                    queried = queried.Where(s => !string.IsNullOrEmpty(s.Status)).ToList();
                                }
                                break;
                        }
                    }
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    queried = queried
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = queried.Count();

                var pagedData = queried
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();

                if (User.IsInRole("PortCoordinator"))
                {
                    pagedData = pagedData.Where(t => t.CreatedBy == currentUser!.UserName)
                        .ToList();
                }

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
                _logger.LogError(ex, "Failed to dispatch tickets.");
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _cloudStorageService.DeleteFileAsync(model.ImageName!);
                model.ImageName = null;
                model.ImageSignedUrl = null;
                model.ImageSavedUrl = null;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";
                return RedirectToAction(nameof(Edit), new { id = model.DispatchTicketId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        public async Task<IActionResult> DeleteVideo(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                await _cloudStorageService.DeleteFileAsync(model.VideoName!);
                model.VideoName = null;
                model.VideoSignedUrl = null;
                model.VideoSavedUrl = null;
                await _unitOfWork.SaveAsync(cancellationToken);
                TempData["success"] = "Video Deleted Successfully!";
                return RedirectToAction(nameof(Edit), new { id = model.DispatchTicketId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete video.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PostSelected(string records, CancellationToken cancellationToken = default)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.PostServiceRequest, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(records))
            {
                TempData["info"] = "Passed record list is empty";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var recordList = JsonConvert.DeserializeObject<List<string>>(records);
                var postedTickets = new List<string>();

                foreach (var recordId in recordList!)
                {
                    int idToFind = int.Parse(recordId);
                    var recordToUpdate = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == idToFind, cancellationToken);

                    if (recordToUpdate != null)
                    {
                        recordToUpdate.Status = "Pending";
                        postedTickets.Add($"{recordToUpdate.DispatchNumber}");
                    }
                }

                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = postedTickets.Any()
                        ? $"Posted service requests #{string.Join(", #", postedTickets)}"
                        : $"No posting detected",
                    DocumentType = "Service Request",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Records posted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to post selected requests.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> CancelSelected(string records, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(records))
            {
                TempData["error"] = "Passed record list is empty";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var recordList = JsonConvert.DeserializeObject<List<string>>(records);
                var cancelledTickets = new List<string>();

                foreach (var recordId in recordList!)
                {
                    var idToFind = int.Parse(recordId);
                    var recordToUpdate = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == idToFind, cancellationToken);

                    if (recordToUpdate != null)
                    {
                        recordToUpdate.Status = "Cancelled";
                        cancelledTickets.Add(recordToUpdate.DispatchNumber);
                    }
                }

                await _unitOfWork.SaveAsync(cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = cancelledTickets.Any()
                        ? $"Cancel service requests #{string.Join(", #", cancelledTickets)}"
                        : $"No cancel detected",
                    DocumentType = "ServiceRequest",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                };

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion --Audit Trail

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Records cancelled successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel selected entries.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private string GenerateFileNameToSave(string incomingFileName, string type)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{type}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
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

        private async Task<string?> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.UserName;
        }

        private async Task<string> GenerateSignedUrl(string uploadName)
        {
            if (!string.IsNullOrWhiteSpace(uploadName))
            {
                return await _cloudStorageService.GetSignedUrlAsync(uploadName);
            }
            throw new Exception("Upload name invalid.");
        }

        public MMSIDispatchTicket ServiceRequestVmToDispatchTicketModel(ServiceRequestViewModel vm)
        {
            return new MMSIDispatchTicket
            {
                DispatchTicketId = vm.DispatchTicketId ?? 0,
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
                TariffEditedBy = string.Empty
            };
        }

        public ServiceRequestViewModel DispatchTicketModelToServiceRequestVm(MMSIDispatchTicket model)
        {
            return new ServiceRequestViewModel
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
        }
    }
}
