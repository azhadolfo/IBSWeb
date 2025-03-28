using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
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
        private readonly DispatchTicketRepository _dispatchRepo;
        private readonly UserManager<IdentityUser> _userManager;

        public ServiceRequestController(ApplicationDbContext db, DispatchTicketRepository dispatchRepo, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _dispatchRepo = dispatchRepo;
            _userManager = userManager;
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

            return View(dispatchTickets);
        }

        [HttpGet]
        public async Task <IActionResult> Create(CancellationToken cancellationToken = default)
        {
            MMSIDispatchTicket model = new()
            {
                ActivitiesServices = await _dispatchRepo.GetMMSIActivitiesServicesById(cancellationToken),
                Ports = await _dispatchRepo.GetMMSIPortsById(cancellationToken),
                Tugboats = await _dispatchRepo.GetMMSITugboatsById(cancellationToken),
                TugMasters = await _dispatchRepo.GetMMSITugMastersById(cancellationToken),
                Vessels = await _dispatchRepo.GetMMSIVesselsById(cancellationToken)
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
                model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);

                if (ModelState.IsValid)
                {
                    if (model.DateLeft < model.DateArrived || (model.TimeLeft != model.TimeArrived && model.TimeLeft < model.TimeArrived))
                    {
                        var user = await _userManager.GetUserAsync(User);
                        model.CreatedBy = user.UserName;
                        model.CreatedDate = DateTime.Now;

                        string uploadFolder = "wwwroot/Dispatch_Ticket_Uploads";

                        if (file != null && file.Length > 0)
                        {
                            if (!Directory.Exists(uploadFolder))
                            {
                                Directory.CreateDirectory(uploadFolder);
                            }

                            var customFileName = DateTime.Now.ToString("yyyyMMddhhmmss") + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine("Dispatch_Ticket_Uploads", customFileName);
                            var fullPath = Path.Combine("wwwroot", filePath);
                            model.UploadName = customFileName;

                            using (var stream = new FileStream(fullPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            ViewBag.Message = "Image uploaded successfully!";
                        }

                        model.Status = "For Posting";
                        DateTime dateTimeLeft = model.DateLeft.ToDateTime(model.TimeLeft);
                        DateTime dateTimeArrived = model.DateArrived.ToDateTime(model.TimeArrived);
                        TimeSpan timeDifference = dateTimeArrived - dateTimeLeft;
                        model.TotalHours = (decimal)timeDifference.TotalHours;

                        await _db.MMSIDispatchTickets.AddAsync(model, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Entry Created Successfully!";

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["error"] = "Time of departure should be earlier than the time of arrival!";
                        model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);
                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";
                    model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);
                    ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"{ex.Message}";
                model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);
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
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.DispatchTicketId == id)
                .Include(dt => dt.Terminal).ThenInclude(t => t.Port)
                .FirstOrDefaultAsync();
            model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);
            ViewData["PortId"] = model?.Terminal?.Port?.PortId;

            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIDispatchTicket model, IFormFile? file, CancellationToken cancellationToken)
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
                            if (currentModel.UploadName != null)
                            {
                                string destinationPath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", currentModel.UploadName);

                                if (System.IO.File.Exists(destinationPath))
                                {
                                    System.IO.File.Delete(destinationPath);
                                }
                            }

                            var customFileName = DateTime.Now.ToString("yyyyMMddhhmmss") + Path.GetExtension(file.FileName);
                            var filePath = Path.Combine("Dispatch_Ticket_Uploads", customFileName);
                            var itemPath = Path.Combine("wwwroot", filePath);
                            model.UploadName = customFileName;
                            using (var stream = new FileStream(itemPath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                        }

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

                        await _db.SaveChangesAsync();

                        TempData["success"] = "Entry edited successfully!";

                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["error"] = "Date/Time Left cannot be later than Date/Time Arrived!";

                        model = await _db.MMSIDispatchTickets
                        .Include(dt => dt.Terminal)            // Include the Terminal navigation property
                        .ThenInclude(t => t.Port)             // Include the Port navigation property of Terminal
                        .FirstOrDefaultAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                        model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);

                        ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                        return View(model);
                    }
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    model = await _db.MMSIDispatchTickets
                    .Include(dt => dt.Terminal)            // Include the Terminal navigation property
                    .ThenInclude(t => t.Port)             // Include the Port navigation property of Terminal
                    .FirstOrDefaultAsync(dt => dt.DispatchTicketId == model.DispatchTicketId, cancellationToken);

                    model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);

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
                .FirstOrDefaultAsync();

                model = await _dispatchRepo.GetDispatchTicketLists(model, cancellationToken);

                ViewData["PortId"] = model?.Terminal?.Port?.PortId;

                return View(model);
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
                    .Where(dt => dt.Status == "Cancelled" || dt.Status == "For Posting")
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

        public async Task<IActionResult> DeleteImage(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                string filePath = Path.Combine("wwwroot/Dispatch_Ticket_Uploads", model.UploadName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

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

        public async Task<IActionResult> PostServiceRequest(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "For Tariff";
                await _db.SaveChangesAsync();
                TempData["success"] = "Entry Posted!";

                return RedirectToAction("Index", "DispatchTicket", new { id = id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { id = id });
            }
        }

        public async Task<IActionResult> CancelServiceRequest(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                model.Status = "Cancelled";
                await _db.SaveChangesAsync();
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

                    foreach (var recordId in recordList)
                    {
                        int idToFind = int.Parse(recordId);

                        var recordToUpdate = await _db.MMSIDispatchTickets
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "For Tariff";
                        }
                    }

                    await _db.SaveChangesAsync();

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

                    foreach (var recordId in recordList)
                    {
                        int idToFind = int.Parse(recordId);

                        var recordToUpdate = await _db.MMSIDispatchTickets
                            .FindAsync(idToFind, cancellationToken);

                        if (recordToUpdate != null)
                        {
                            recordToUpdate.Status = "Cancelled";
                        }
                    }

                    await _db.SaveChangesAsync();

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
    }
}
