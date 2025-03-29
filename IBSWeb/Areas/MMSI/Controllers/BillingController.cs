using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class BillingController : Controller
    {
        private readonly DispatchTicketRepository _dispatchRepo;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public BillingController(DispatchTicketRepository dispatchRepo, ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _dispatchRepo = dispatchRepo;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include (a => a.Customer)
                .Include (a => a.Port)
                .ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSIBilling model = new()
            {
                Customers = await _dispatchRepo.GetMMSICustomersById(cancellationToken),
                Vessels = await _dispatchRepo.GetMMSIVesselsById(cancellationToken),
                Ports = await _dispatchRepo.GetMMSIPortsById(cancellationToken),
                UnbilledDispatchTickets = await _dispatchRepo.GetMMSIUnbilledTicketsById(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MMSIBilling model, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Status = "For Collection";
                    model.CreatedBy = await GetUserNameAsync();
                    var datetimeNow = DateTime.Now;
                    model.CreatedDate = datetimeNow;

                    if (!model.IsDocumented)
                    {
                        model.MMSIBillingNumber = await _dispatchRepo.GenerateBillingNumber(cancellationToken);
                    }

                    // create it first so it generates id
                    await _db.MMSIBillings.AddAsync(model, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    model = await _db.MMSIBillings.Where(b => b.CreatedDate == datetimeNow).FirstOrDefaultAsync(cancellationToken);

                    int id = model.MMSIBillingId;

                    model = await _db.MMSIBillings.FindAsync(id, cancellationToken);

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create Billing: id#{model.MMSICollectionId}",
                        DocumentType = "Billing",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    decimal totalAmount = 0;

                    foreach(var billDispatchTicket in model.ToBillDispatchTickets)
                    {
                        var dtEntry = await _db.MMSIDispatchTickets.FindAsync(int.Parse(billDispatchTicket));
                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry.Status = "Billed";
                        dtEntry.BillingId = model.MMSIBillingId.ToString();
                    }

                    model.Amount = totalAmount;
                    await _db.SaveChangesAsync(cancellationToken);

                    TempData["success"] = "Entry Created Successfully!";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";
                    model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    TempData["error"] = ex.Message;
                }
                else
                {
                    TempData["error"] = $"{ex.InnerException.Message}";
                }

                model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

                return View(model);
            }
        }

        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await _db.MMSIDispatchTickets
                    .Include(t => t.Tugboat)
                    .Include(t => t.ActivityService)
                    .Where(t => intDispatchTicketIds.Contains(t.DispatchTicketId)) // Assuming Id is the primary key
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = dispatchTickets
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .FindAsync(id, cancellationToken);

            // get select lists
            model = await _dispatchRepo.GetBillingLists(model, cancellationToken);

            // get billed by current billing select list
            var unbilledDT = await _dispatchRepo.GetMMSIUnbilledTicketsById(cancellationToken);

            // get not yet billed tickets select list
            model.UnbilledDispatchTickets = await _dispatchRepo.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

            // add the both select list above
            model.UnbilledDispatchTickets.AddRange(unbilledDT);

            // get billed DTs
            var listOfDtBilled = await _db.MMSIDispatchTickets
                .Where(d => d.BillingId == id.ToString())
                .ToListAsync(cancellationToken);

            // put the already billed DT to string list so it appears on the select2 of view
            model.ToBillDispatchTickets = listOfDtBilled.Select(d => d.DispatchTicketId.ToString()).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MMSIBilling model, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // get previous version
                    var currentModel = await _db.MMSIBillings.FindAsync(model.MMSIBillingId, cancellationToken);

                    if(model.IsDocumented != null)
                    {
                        if (model.IsDocumented)
                        {
                            // overwrite if there's new
                            currentModel.MMSIBillingNumber = model.MMSIBillingNumber;
                        }
                        if (!model.IsDocumented && !currentModel.IsDocumented)
                        {
                            // as is, use old generated
                        }
                        if (!model.IsDocumented && currentModel.IsDocumented)
                        {
                            // if the old is doc but now is undoc, generate
                            currentModel.MMSIBillingNumber = await _dispatchRepo.GenerateBillingNumber(cancellationToken);
                        }
                    }

                    currentModel.Date = model.Date;
                    currentModel.Status = "For Collection";
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.IsDocumented = model.IsDocumented;

                    // get billed by current billing select list
                    var unbilledDT = await _dispatchRepo.GetMMSIUnbilledTicketsById(cancellationToken);

                    // get not yet billed tickets select list
                    model.UnbilledDispatchTickets = await _dispatchRepo.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

                    // add the both select list above
                    model.UnbilledDispatchTickets.AddRange(unbilledDT);

                    // reset status of all the dispatch ticket affected by editing select
                    foreach (var dispatchTicket in model.UnbilledDispatchTickets)
                    {
                        var id = int.Parse(dispatchTicket.Value);
                        var dtModel = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                        dtModel.Status = "For Billing";
                        dtModel.BillingId = "0";
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    decimal totalAmount = 0;

                    // re-bill the new selected tickets
                    foreach(var billDispatchTicket in model.ToBillDispatchTickets)
                    {
                        var dtEntry = await _db.MMSIDispatchTickets.FindAsync(int.Parse(billDispatchTicket));
                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry.Status = "Billed";
                        dtEntry.BillingId = model.MMSIBillingId.ToString();
                    }

                    currentModel.Amount = totalAmount;

                    await _db.SaveChangesAsync();
                    TempData["success"] = "Entry edited successfully!";

                    return RedirectToAction(nameof(Index));

                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _db.MMSIBillings.FindAsync(id, cancellationToken);

                if (model != null)
                {

                    _db.MMSIBillings.Remove(model);
                    _db.SaveChanges();

                    TempData["success"] = "Billing deleted successfully!";

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = "Can't find entry.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(ex.InnerException.Message))
                {
                    TempData["error"] = ex.Message;
                }
                else
                {
                    TempData["error"] = $"{ex.InnerException.Message}";
                }

                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Where(a => a.MMSIBillingId == id)
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
                .Include(a => a.Port)
                .FirstOrDefaultAsync(cancellationToken);

            // list of dispatch numbers
            model.ToBillDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Select(dt => dt.DispatchNumber.ToString())
                .ToListAsync(cancellationToken);

            // list of dispatch tickets
            model.PaidDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.ActivityService)
                .ToListAsync (cancellationToken);

            return View(model);
        }

        public JsonResult GetCustomerDetails(int customerId)
        {
            var customerDetails = _db.MMSICustomers
                .Find(customerId);

            var customerDetailsJson = new
            {
                terms = customerDetails.CustomerTerms,
                address = customerDetails.CustomerAddress,
                tinNo = customerDetails.CustomerTIN,
                businessStyle = customerDetails.CustomerBusinessStyle
            };

            return Json(customerDetailsJson);
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

    }
}
