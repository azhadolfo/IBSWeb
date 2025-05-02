using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Services.Attributes;
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
        public readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<ServiceRequestController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public ServiceRequestController(ApplicationDbContext db, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ICloudStorageService cloudStorageService,
            ILogger<ServiceRequestController> logger)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
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

        public async Task<IActionResult> Index(string filterType)
        {
            var dispatchTickets = await _db.MMSIServiceRequests
                .Where(sq => sq.Status == "For Posting")
                .Include(sq => sq.ActivityService)
                .Include(sq => sq.Terminal).ThenInclude(t => t.Port)
                .Include(sq => sq.Tugboat)
                .Include(sq => sq.TugMaster)
                .Include(sq => sq.Vessel)
                .ToListAsync();

            foreach (var dispatchTicket in dispatchTickets.Where(dt => !string.IsNullOrEmpty(dt.ImageName)))
            {
                dispatchTicket.ImageSignedUrl = await GenerateSignedUrl(dispatchTicket.ImageName);
            }
            foreach (var dispatchTicket in dispatchTickets.Where(dt => !string.IsNullOrEmpty(dt.VideoName)))
            {
                dispatchTicket.VideoSignedUrl = await GenerateSignedUrl(dispatchTicket.VideoName);
            }

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task <IActionResult> Create(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            MMSIServiceRequest model = new()
            {
                ActivitiesServices = await _unitOfWork.Msap.GetMMSIActivitiesServicesById(cancellationToken),
                Ports = await _unitOfWork.Msap.GetMMSIPortsById(cancellationToken),
                Tugboats = await _unitOfWork.Msap.GetMMSITugboatsById(cancellationToken),
                TugMasters = await _unitOfWork.Msap.GetMMSITugMastersById(cancellationToken),
                Vessels = await _unitOfWork.Msap.GetMMSIVesselsById(cancellationToken),
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
            };

            ViewData["PortId"] = 0;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIServiceRequest model, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            model.Terminal = await _db.MMSITerminals.FindAsync(model.TerminalId, cancellationToken);
            model.Terminal.Port = await _db.MMSIPorts.FindAsync(model.Terminal.PortId, cancellationToken);
            DateTime timeStamp = DateTime.Now;

            try
            {
                model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                if (!ModelState.IsValid)
                {
                    var companyClaims = await GetCompanyClaimAsync();

                    TempData["error"] = "Can't create entry, please review your input.";
                    model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return View(model);
                }

                if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                    {
                        if (model.Date > model.DateLeft)
                        {
                            throw new ArgumentException("Date start should not be earlier than date today.");
                        }

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

                        model.Status = "For Posting";
                        DateTime dateTimeLeft = model.DateLeft.ToDateTime(model.TimeLeft);
                        DateTime dateTimeArrived = model.DateArrived.ToDateTime(model.TimeArrived);
                        TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                        model.TotalHours = (decimal)timeDifference.TotalHours;
                        await _db.MMSIServiceRequests.AddAsync(model, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);

                        var tempModel =
                            await _db.MMSIServiceRequests.FirstOrDefaultAsync(dt => dt.CreatedDate == timeStamp);

                        #region -- Audit Trail

                        var audit = new MMSIAuditTrail
                        {
                            Date = DateTime.Now,
                            Username = await GetUserNameAsync(),
                            MachineName = Environment.MachineName,
                            Activity = $"Create service request: id#{tempModel.ServiceRequestId}",
                            DocumentType = "ServiceRequest",
                            Company = await GetCompanyClaimAsync()
                        };

                        await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);

                        #endregion --Audit Trail

                        TempData["success"] = $"Service Request #{tempModel.DispatchNumber} was successfully created.";

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["error"] = "Start Date/Time should be earlier than End Date/Time!";
                        model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"{ex.Message}";
                model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
                ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                return View(model);
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIServiceRequests.Where(dt => dt.ServiceRequestId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync();


            if (!string.IsNullOrEmpty(model.ImageName))
            {
                model.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            }
            if (!string.IsNullOrEmpty(model.VideoName))
            {
                model.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);
            }

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _db.MMSIServiceRequests
                .Where(dt => dt.ServiceRequestId == id)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
            model.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            if (!string.IsNullOrEmpty(model.ImageName))
            {
                model.ImageSignedUrl = await GenerateSignedUrl(model.ImageName);
            }
            if (!string.IsNullOrEmpty(model.VideoName))
            {
                model.VideoSignedUrl = await GenerateSignedUrl(model.VideoName);
            }

            ViewData["PortId"] = model?.Terminal?.Port?.PortId;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIServiceRequest model, IFormFile? imageFile, IFormFile? videoFile, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.GetUserAsync(User);
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.DateLeft < model.DateArrived || (model.DateLeft == model.DateArrived && model.TimeLeft < model.TimeArrived))
                    {
                        if (model.Date > model.DateLeft)
                        {
                            throw new ArgumentException("Date start should not be earlier than date today.");
                        }
                        var currentModel = await _db.MMSIServiceRequests.FindAsync(model.ServiceRequestId, cancellationToken);
                        TimeSpan timeDifference = model.DateArrived.ToDateTime(model.TimeArrived) - model.DateLeft.ToDateTime(model.TimeLeft);

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

                        if (currentModel.Date != model.Date) { changes.Add($"CreateDate: {currentModel.Date} -> {model.Date}"); }
                        if (currentModel.COSNumber  != model.COSNumber) { changes.Add($"COSNumber: {currentModel.COSNumber} -> {model.COSNumber}"); }
                        if (currentModel.CustomerId  != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                        if (currentModel.DispatchNumber != model.DispatchNumber) { changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {model.DispatchNumber}"); }
                        if (currentModel.DateLeft != model.DateLeft) { changes.Add($"DateLeft: {currentModel.DateLeft} -> {model.DateLeft}"); }
                        if (currentModel.TimeLeft != model.TimeLeft) { changes.Add($"TimeLeft: {currentModel.TimeLeft} -> {model.TimeLeft}"); }
                        if (currentModel.DateArrived != model.DateArrived) { changes.Add($"DateArrived: {currentModel.DateArrived} -> {model.DateArrived}"); }
                        if (currentModel.TimeArrived != model.TimeArrived) { changes.Add($"TimeArrived: {currentModel.TimeArrived} -> {model.TimeArrived}"); }
                        if (currentModel.TerminalId != model.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}"); }
                        if (currentModel.ActivityServiceId != model.ActivityServiceId) { changes.Add($"ActivityServiceId: {currentModel.ActivityServiceId} -> {model.ActivityServiceId}"); }
                        if (currentModel.TugBoatId != model.TugBoatId) { changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {model.TugBoatId}"); }
                        if (currentModel.TugMasterId != model.TugMasterId) { changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {model.TugMasterId}"); }
                        if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                        if (currentModel.Remarks != model.Remarks) { changes.Add($"Remarks: '{currentModel.Remarks}' -> '{model.Remarks}'"); }
                        if (imageFile != null && currentModel.ImageName != model.ImageName) { changes.Add($"ImageName: '{currentModel.ImageName}' -> '{model.ImageName}'"); }
                        if (videoFile != null && currentModel.VideoName != model.VideoName) { changes.Add($"VideoName: '{currentModel.VideoName}' -> '{model.VideoName}'"); }

                        #endregion -- Changes

                        currentModel.EditedBy = user.UserName;
                        currentModel.EditedDate = DateTime.Now;
                        currentModel.TotalHours = (decimal)timeDifference.TotalHours;
                        currentModel.Date = model.Date;
                        currentModel.COSNumber = model.COSNumber;
                        currentModel.CustomerId = model.CustomerId;
                        currentModel.DispatchNumber = model.DispatchNumber;
                        currentModel.DateLeft = model.DateLeft;
                        currentModel.TimeLeft = model.TimeLeft;
                        currentModel.DateArrived = model.DateArrived;
                        currentModel.TimeArrived = model.TimeArrived;
                        currentModel.TerminalId = model.TerminalId;
                        currentModel.ActivityServiceId = model.ActivityServiceId;
                        currentModel.TugBoatId = model.TugBoatId;
                        currentModel.TugMasterId = model.TugMasterId;
                        currentModel.VesselId = model.VesselId;
                        currentModel.Remarks = model.Remarks;
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

                        var audit = new MMSIAuditTrail
                        {
                            Date = DateTime.Now,
                            Username = await GetUserNameAsync(),
                            MachineName = Environment.MachineName,
                            Activity = changes.Any()
                                ? $"Edit: id#{currentModel.ServiceRequestId}, {string.Join(", ", changes)}"
                                : $"No changes detected: id#{currentModel.ServiceRequestId}",
                            DocumentType = "ServiceRequest",
                            Company = await GetCompanyClaimAsync()
                        };

                        await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);

                        #endregion --Audit Trail

                        TempData["success"] = "Entry edited successfully!";

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["error"] = "Date/Time Left cannot be later than Date/Time Arrived!";

                        model = await _db.MMSIServiceRequests
                        .Include(dt => dt.Terminal)
                        .ThenInclude(t => t.Port)
                        .FirstOrDefaultAsync(dt => dt.ServiceRequestId == model.ServiceRequestId, cancellationToken);

                        model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    model = await _db.MMSIServiceRequests
                    .Include(dt => dt.Terminal)
                    .ThenInclude(t => t.Port)
                    .FirstOrDefaultAsync(dt => dt.ServiceRequestId == model.ServiceRequestId, cancellationToken);

                    model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                var companyClaims = await GetCompanyClaimAsync();

                TempData["error"] = ex.Message;

                model = await _db.MMSIServiceRequests
                .Where(dt => dt.ServiceRequestId == model.ServiceRequestId)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(cancellationToken);

                model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
                model.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

                ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangeTerminal(int portId, CancellationToken cancellationToken = default)
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

        [HttpPost]
        public async Task<IActionResult> GetDispatchTicketList(string status, CancellationToken cancellationToken = default)
        {
            var item = new List<MMSIServiceRequest>();
            if (status == "All" || status == null)
            {
                item = await _db.MMSIServiceRequests
                    .Where(dt => dt.Status == "Cancelled" || dt.Status == "For Posting")
                    .Include(a => a.ActivityService)
                    .Include(a => a.Terminal).ThenInclude(t => t.Port)
                    .Include(a => a.Tugboat)
                    .Include(a => a.TugMaster)
                    .Include(a => a.Vessel)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                item = await _db.MMSIServiceRequests
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

                var queried = _db.MMSIServiceRequests
                    .Include(dt => dt.ActivityService)
                    .Include(dt => dt.Terminal)
                    .ThenInclude(dt => dt.Port)
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.TugMaster)
                    .Include(dt => dt.Vessel)
                    .Where(dt => dt.Status == "For Posting" || dt.Status == "Cancelled");

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
                                if (searchValue == "for posting")
                                {
                                    queried = queried.Where(s => s.Status == "For Posting");
                                }
                                if (searchValue == "cancelled")
                                {
                                    queried = queried.Where(s => s.Status == "Cancelled");
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

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _db.MMSIServiceRequests.FindAsync(id, cancellationToken);

                await _cloudStorageService.DeleteFileAsync(model.ImageName);

                // string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);
                //
                // if (System.IO.File.Exists(filePath))
                // {
                //     System.IO.File.Delete(filePath);
                // }

                model.ImageName = null;
                model.ImageSignedUrl = null;
                model.ImageSavedUrl = null;
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";

                return RedirectToAction(nameof(Edit), new { id = model.ServiceRequestId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Edit), new { id = id });
            }
        }

        public async Task<IActionResult> DeleteVideo(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _db.MMSIServiceRequests.FindAsync(id, cancellationToken);

                await _cloudStorageService.DeleteFileAsync(model.VideoName);

                model.VideoName = null;
                model.VideoSignedUrl = null;
                model.VideoSavedUrl = null;
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Video Deleted Successfully!";

                return RedirectToAction(nameof(Edit), new { id = model.ServiceRequestId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Edit), new { id = id });
            }
        }

        public async Task<IActionResult> PostServiceRequest(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _db.MMSIServiceRequests.FindAsync(id, cancellationToken);
                model.Status = "For Tariff";
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Entry Posted!";

                return RedirectToAction("Index", "DispatchTicket", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> CancelServiceRequest(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _db.MMSIServiceRequests.FindAsync(id, cancellationToken);
                model.Status = "Cancelled";
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Entry Cancelled";

                return RedirectToAction(nameof(Index), new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> PostSelected(string records, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(records))
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var recordList = JsonConvert.DeserializeObject<List<string>>(records);
                    var postedTickets = new List<string>();

                    foreach (var recordId in recordList)
                    {
                        int idToFind = int.Parse(recordId);

                        var recordToUpdate = await _db.MMSIServiceRequests
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "For Tariff";

                            var timeStamp = DateTime.Now;
                            var creator = await GetUserNameAsync();

                            MMSIDispatchTicket newTicket = new MMSIDispatchTicket()
                            {
                                Date = recordToUpdate.Date,
                                COSNumber = recordToUpdate.COSNumber,
                                CustomerId = recordToUpdate.CustomerId,
                                DispatchNumber = recordToUpdate.DispatchNumber,
                                DateLeft = recordToUpdate.DateLeft,
                                TimeLeft = recordToUpdate.TimeLeft,
                                DateArrived = recordToUpdate.DateArrived,
                                TimeArrived = recordToUpdate.TimeArrived,
                                TerminalId = recordToUpdate.TerminalId,
                                ActivityServiceId = recordToUpdate.ActivityServiceId,
                                TugBoatId = recordToUpdate.TugBoatId,
                                TugMasterId = recordToUpdate.TugMasterId,
                                VesselId = recordToUpdate.VesselId,
                                Remarks = recordToUpdate.Remarks,
                                CreatedBy = creator,
                                CreatedDate = timeStamp,
                                ImageName = recordToUpdate.ImageName,
                                ImageSavedUrl = recordToUpdate.ImageSavedUrl,
                                VideoName = recordToUpdate.VideoName,
                                VideoSavedUrl = recordToUpdate.VideoSavedUrl,
                                Status = "For Tariff",
                                TotalHours = recordToUpdate.TotalHours
                            };

                            await _db.MMSIDispatchTickets.AddAsync(newTicket, cancellationToken);
                            await _db.SaveChangesAsync(cancellationToken);

                            postedTickets.Add($"{recordToUpdate.ServiceRequestId} => #{newTicket.DispatchTicketId}");
                        }
                        else
                        {
                            throw new Exception("Service request not found.");
                        }
                    }

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = postedTickets.Any()
                            ? $"Posted: #{string.Join(", #", postedTickets)}"
                            : $"No posting detected",
                        DocumentType = "DispatchTicket",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    #endregion --Audit Trail

                    TempData["success"] = "Records posted successfully";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }
            TempData["error"] = "Passed record list is empty";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CancelSelected(string records, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(records))
            {
                try
                {
                    var recordList = JsonConvert.DeserializeObject<List<string>>(records);
                    var posteds = new List<string>();

                    foreach (var recordId in recordList)
                    {
                        int idToFind = int.Parse(recordId);

                        var recordToUpdate = await _db.MMSIServiceRequests
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "Cancelled";
                            posteds.Add(recordToUpdate.ServiceRequestId.ToString());
                        }
                    }

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = posteds.Any()
                            ? $"Cancel: id#{string.Join(", #", posteds)}"
                            : $"No cancel detected",
                        DocumentType = "ServiceRequest",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion --Audit Trail

                    await _db.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Records cancelled successfully";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }
            TempData["error"] = "Passed record list is empty";
            return RedirectToAction(nameof(Index));
        }

        private string? GenerateFileNameToSave(string incomingFileName, string type)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{type}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var claims = await _userManager.GetClaimsAsync(await _userManager.GetUserAsync(User));
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task<string> GetUserNameAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user.UserName;
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
    }
}
