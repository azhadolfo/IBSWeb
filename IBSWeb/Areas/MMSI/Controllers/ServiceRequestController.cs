using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ServiceRequestController : Controller
    {
        public readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICloudStorageService _cloudStorageService;

        public ServiceRequestController(ApplicationDbContext db, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ICloudStorageService cloudStorageService)
        {
            _db = db;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _cloudStorageService = cloudStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var dispatchTickets = await _db.MMSIDispatchTickets
                .Where(sq => sq.Status == "For Posting")
                .Include(sq => sq.ActivityService)
                .Include(sq => sq.Terminal).ThenInclude(t => t.Port)
                .Include(sq => sq.Tugboat)
                .Include(sq => sq.TugMaster)
                .Include(sq => sq.Vessel)
                .ToListAsync();

            foreach (var dispatchTicket in dispatchTickets.Where(dt => !string.IsNullOrEmpty(dt.UploadName)))
            {
                await GenerateSignedUrl(dispatchTicket);
            }

            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task <IActionResult> Create(CancellationToken cancellationToken = default)
        {
            MMSIDispatchTicket model = new()
            {
                ActivitiesServices = await _unitOfWork.Msap.GetMMSIActivitiesServicesById(cancellationToken),
                Ports = await _unitOfWork.Msap.GetMMSIPortsById(cancellationToken),
                Tugboats = await _unitOfWork.Msap.GetMMSITugboatsById(cancellationToken),
                TugMasters = await _unitOfWork.Msap.GetMMSITugMastersById(cancellationToken),
                Vessels = await _unitOfWork.Msap.GetMMSIVesselsById(cancellationToken)
            };

            ViewData["PortId"] = 0;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIDispatchTicket model, IFormFile? file, CancellationToken cancellationToken = default)
        {
            model.Terminal = await _db.MMSITerminals.FindAsync(model.TerminalId, cancellationToken);
            model.Terminal.Port = await _db.MMSIPorts.FindAsync(model.Terminal.PortId, cancellationToken);

            try
            {
                model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                if (ModelState.IsValid)
                {

                    if (model.DateLeft < model.DateArrived || (model.TimeLeft != model.TimeArrived && model.TimeLeft < model.TimeArrived))
                    {
                        model.CreatedBy = await GetUserNameAsync();
                        model.CreatedDate = DateTime.Now;

                        // string uploadFolder = "wwwroot/Dispatch_Ticket_Uploads";
                        // string cloudUploadDirectory = "";

                        // upload file if something is submitted
                        if (file != null && file.Length > 0)
                        {

                            model.UploadName = GenerateFileNameToSave(file.FileName);
                            model.SavedUrl = await _cloudStorageService.UploadFileAsync(file, model.UploadName);

                            ViewBag.Message = "Image uploaded successfully!";

                            // if (!Directory.Exists(uploadFolder))
                            // {
                            //     Directory.CreateDirectory(uploadFolder);
                            // }
                            //
                            // var customFileName = DateTime.Now.ToString("yyyyMMddhhmmss") + Path.GetExtension(file.FileName);
                            // var filePath = Path.Combine("Dispatch_Ticket_Uploads", customFileName);
                            // var fullPath = Path.Combine("wwwroot", filePath);
                            // model.UploadName = customFileName;
                            //
                            // using (var stream = new FileStream(fullPath, FileMode.Create))
                            // {
                            //     await file.CopyToAsync(stream);
                            // }
                        }

                        model.Status = "For Posting";
                        DateTime dateTimeLeft = model.DateLeft.ToDateTime(model.TimeLeft);
                        DateTime dateTimeArrived = model.DateArrived.ToDateTime(model.TimeArrived);
                        TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                        model.TotalHours = (decimal)timeDifference.TotalHours;
                        await _db.MMSIDispatchTickets.AddAsync(model, cancellationToken);

                        #region -- Audit Trail

                        var audit = new MMSIAuditTrail
                        {
                            Date = DateTime.Now,
                            Username = await GetUserNameAsync(),
                            MachineName = Environment.MachineName,
                            Activity = $"Create service request: id#{model.DispatchTicketId}",
                            DocumentType = "ServiceRequest",
                            Company = await GetCompanyClaimAsync()
                        };

                        await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);

                        #endregion --Audit Trail

                        TempData["success"] = "Entry Created Successfully!";

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["error"] = "Time of departure should be earlier than the time of arrival!";
                        model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";
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
            var model = await _db.MMSIDispatchTickets.Where(dt => dt.DispatchTicketId == id)
                .Include(a => a.ActivityService)
                .Include(a => a.Terminal).ThenInclude(t => t.Port)
                .Include(a => a.Tugboat)
                .Include(a => a.TugMaster)
                .Include(a => a.Vessel)
                .FirstOrDefaultAsync();

            return View(model);

        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(cancellationToken);

            model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);
            await GenerateSignedUrl(model);

            ViewData["PortId"] = model?.Terminal?.Port?.PortId;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIDispatchTicket model, IFormFile? file, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.GetUserAsync(User);
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.DateLeft < model.DateArrived || model.TimeArrived != model.TimeLeft)
                    {
                        var currentModel = await _db.MMSIDispatchTickets.FindAsync(model.DispatchTicketId, cancellationToken);
                        TimeSpan timeDifference = model.DateArrived.ToDateTime(model.TimeArrived) - model.DateLeft.ToDateTime(model.TimeLeft);

                        if (file != null)
                        {
                            // delete existing before replacing
                            if (!string.IsNullOrEmpty(model.UploadName))
                            {
                                await _cloudStorageService.DeleteFileAsync(model.UploadName);
                                model.UploadName = null;
                            }

                            model.UploadName = GenerateFileNameToSave(file.FileName);
                            model.SavedUrl = await _cloudStorageService.UploadFileAsync(file, model.UploadName);

                            // if (currentModel.UploadName != null)
                            // {
                            //     string destinationPath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", currentModel.UploadName);
                            //
                            //     if (System.IO.File.Exists(destinationPath))
                            //     {
                            //         System.IO.File.Delete(destinationPath);
                            //     }
                            // }

                            // var customFileName = DateTime.Now.ToString("yyyyMMddhhmmss") + Path.GetExtension(file.FileName);
                            // var filePath = Path.Combine("Dispatch_Ticket_Uploads", customFileName);
                            // var itemPath = Path.Combine("wwwroot", filePath);
                            // model.UploadName = customFileName;
                            // await using (var stream = new FileStream(itemPath, FileMode.Create))
                            // {
                            //     await file.CopyToAsync(stream, cancellationToken);
                            // }
                        }

                        #region -- Changes

                        var changes = new List<string>();

                        if (currentModel.ActivityServiceId != model.ActivityServiceId) { changes.Add($"ActivityServiceId: {currentModel.ActivityServiceId} -> {model.ActivityServiceId}"); }
                        if (currentModel.DispatchNumber != model.DispatchNumber) { changes.Add($"DispatchNumber: {currentModel.DispatchNumber} -> {model.DispatchNumber}"); }
                        if (currentModel.Remarks != model.Remarks) { changes.Add($"Remarks: '{currentModel.Remarks}' -> '{model.Remarks}'"); }
                        if (currentModel.CreateDate != model.CreateDate) { changes.Add($"CreateDate: {currentModel.CreateDate} -> {model.CreateDate}"); }
                        if (currentModel.TerminalId != model.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}"); }
                        if (currentModel.TugBoatId != model.TugBoatId) { changes.Add($"TugBoatId: {currentModel.TugBoatId} -> {model.TugBoatId}"); }
                        if (currentModel.TugMasterId != model.TugMasterId) { changes.Add($"TugMasterId: {currentModel.TugMasterId} -> {model.TugMasterId}"); }
                        if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                        if (currentModel.VoyageNumber != model.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}"); }
                        if (currentModel.DateArrived != model.DateArrived) { changes.Add($"DateArrived: {currentModel.DateArrived} -> {model.DateArrived}"); }
                        if (currentModel.DateLeft != model.DateLeft) { changes.Add($"DateLeft: {currentModel.DateLeft} -> {model.DateLeft}"); }
                        if (currentModel.TimeArrived != model.TimeArrived) { changes.Add($"TimeArrived: {currentModel.TimeArrived} -> {model.TimeArrived}"); }
                        if (currentModel.TimeLeft != model.TimeLeft) { changes.Add($"TimeLeft: {currentModel.TimeLeft} -> {model.TimeLeft}"); }
                        if (currentModel.COSNumber  != model.COSNumber) { changes.Add($"COSNumber: {currentModel.COSNumber} -> {model.COSNumber}"); } if (currentModel.BaseOrStation != model.BaseOrStation) { changes.Add($"BaseOrStation: {currentModel.BaseOrStation} -> {model.BaseOrStation}"); }
                        if (file != null && currentModel.UploadName != model.UploadName) { changes.Add($"UploadName: '{currentModel.UploadName}' -> '{model.UploadName}'"); }

                        #endregion -- Changes

                        currentModel.EditedBy = user.UserName;
                        currentModel.EditedDate = DateTime.Now;
                        currentModel.TotalHours = (decimal)timeDifference.TotalHours;
                        currentModel.ActivityServiceId = model.ActivityServiceId;
                        currentModel.DispatchNumber = model.DispatchNumber;
                        currentModel.Remarks = model.Remarks;
                        currentModel.CreateDate = model.CreateDate;
                        currentModel.TerminalId = model.TerminalId;
                        currentModel.TugBoatId = model.TugBoatId;
                        currentModel.TugMasterId = model.TugMasterId;
                        currentModel.VesselId = model.VesselId;
                        currentModel.VoyageNumber = model.VoyageNumber;
                        currentModel.DateArrived = model.DateArrived;
                        currentModel.DateLeft = model.DateLeft;
                        currentModel.TimeArrived = model.TimeArrived;
                        currentModel.TimeLeft = model.TimeLeft;
                        currentModel.COSNumber = model.COSNumber;
                        currentModel.BaseOrStation = model.BaseOrStation;
                        if (file != null)
                        {
                            currentModel.UploadName = model.UploadName;
                        }

                        #region -- Audit Trail

                        var audit = new MMSIAuditTrail
                        {
                            Date = DateTime.Now,
                            Username = await GetUserNameAsync(),
                            MachineName = Environment.MachineName,
                            Activity = changes.Any()
                                ? $"Edit: id#{currentModel.DispatchTicketId}, {string.Join(", ", changes)}"
                                : $"No changes detected: id#{currentModel.DispatchTicketId}",
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

                        model = await _db.MMSIDispatchTickets
                        .Include(dt => dt.Terminal)
                        .ThenInclude(t => t.Port)
                        .FirstOrDefaultAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                        model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    model = await _db.MMSIDispatchTickets
                    .Include(dt => dt.Terminal)
                    .ThenInclude(t => t.Port)
                    .FirstOrDefaultAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                    model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == model.DispatchTicketId)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync(cancellationToken);

                model = await _unitOfWork.Msap.GetDispatchTicketLists(model, cancellationToken);

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

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketList(string status, CancellationToken cancellationToken = default)
        {
            var item = new List<MMSIDispatchTicket>();
            if (status == "All" || status == null)
            {
                item = await _db.MMSIDispatchTickets
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

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);

                await _cloudStorageService.DeleteFileAsync(model.UploadName);

                // string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);
                //
                // if (System.IO.File.Exists(filePath))
                // {
                //     System.IO.File.Delete(filePath);
                // }

                model.UploadName = null;
                await _db.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Image Deleted Successfully!";

                return RedirectToAction(nameof(Edit), new { id = model.DispatchTicketId });
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
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
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
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
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
                try
                {
                    var recordList = JsonConvert.DeserializeObject<List<string>>(records);
                    var posteds = new List<string>();

                    foreach (var recordId in recordList)
                    {
                        int idToFind = int.Parse(recordId);

                        var recordToUpdate = await _db.MMSIDispatchTickets
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "For Tariff";
                            posteds.Add($"{recordToUpdate.DispatchTicketId}");
                        }
                    }

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = posteds.Any()
                            ? $"Posted: #{string.Join(", #", posteds)}"
                            : $"No posting detected",
                        DocumentType = "ServiceRequest",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion --Audit Trail

                    TempData["success"] = "Records posted successfully";

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

                        var recordToUpdate = await _db.MMSIDispatchTickets
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "Cancelled";
                            posteds.Add(recordToUpdate.DispatchTicketId.ToString());
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

        private string? GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
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

        private async Task GenerateSignedUrl(MMSIDispatchTicket model)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(model.UploadName))
            {
                model.SignedUrl = await _cloudStorageService.GetSignedUrlAsync(model.UploadName);
            }
        }
    }
}
