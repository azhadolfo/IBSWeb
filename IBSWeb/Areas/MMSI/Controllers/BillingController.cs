using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.MMSI;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

                    // find it and reference it
                    var newmodel = await _db.MMSIBillings.Where(b => b.CreatedDate == datetimeNow).FirstOrDefaultAsync(cancellationToken);
                    int id = newmodel.MMSIBillingId;
                    newmodel = await _db.MMSIBillings.FindAsync(id, cancellationToken);

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = $"Create Billing: id#{id} for dt #{string.Join(", #", model.ToBillDispatchTickets)}",
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
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.MMSIBillingId == id, cancellationToken);

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

            model.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);

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
                            model.MMSIBillingNumber = currentModel.MMSIBillingNumber;
                        }
                        if (!model.IsDocumented && currentModel.IsDocumented)
                        {
                            // if the old is doc but now is undoc, generate
                            currentModel.MMSIBillingNumber = await _dispatchRepo.GenerateBillingNumber(cancellationToken);
                        }
                    }

                    //empty string list
                    List<string> idsOfBilledTickets = null;
                    //get dt billed by current billing
                    var tempModel = await _db.MMSIDispatchTickets
                        .Where(d => d.BillingId == model.MMSIBillingId.ToString())
                        .ToListAsync(cancellationToken);
                    //from tempmodel, select the id=>string and list it
                    idsOfBilledTickets = tempModel.Select(d => d.DispatchTicketId.ToString()).OrderBy(x => x).ToList();
                    //put the dt billed to its model container
                    currentModel.ToBillDispatchTickets = idsOfBilledTickets;

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.IsDocumented != model.IsDocumented) { changes.Add($"IsDocumented: {currentModel.IsDocumented} -> {model.IsDocumented}"); }
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.MMSIBillingNumber != model.MMSIBillingNumber) { changes.Add($"MMSIBillingNumber: {currentModel.MMSIBillingNumber} -> {model.MMSIBillingNumber}"); }
                    if (currentModel.VoyageNumber != model.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.PrincipalId != model.PrincipalId) { changes.Add($"PrincipalId: {currentModel.PrincipalId} -> {model.PrincipalId}"); }
                    if (currentModel.PortId != model.PortId) { changes.Add($"PortId: {currentModel.PortId} -> {model.PortId}"); }
                    if (currentModel.TerminalId != model.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}"); }
                    if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                    if (!currentModel.ToBillDispatchTickets.OrderBy(x => x).SequenceEqual(model.ToBillDispatchTickets.OrderBy(x => x)))
                    { changes.Add($"ToBillDispatchTickets: #{string.Join(", #", currentModel.ToBillDispatchTickets)} -> #{string.Join(", #", model.ToBillDispatchTickets)}"); }

                    #endregion -- Changes

                    #region -- Audit Trail

                    var audit = new MMSIAuditTrail
                    {
                        Date = DateTime.Now,
                        Username = await GetUserNameAsync(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit Billing: id #{currentModel.MMSIBillingId} {string.Join(", ", changes)}"
                            : $"No changes detected for Billing #{currentModel.MMSIBillingId}",
                        DocumentType = "Billing",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.Date = model.Date;
                    currentModel.Status = "For Collection";
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.PrincipalId = model.PrincipalId;
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
                .ThenInclude(t => t.Port)
                .Include(a => a.Vessel)
                .Include(a => a.Customer)
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
                businessStyle = customerDetails.CustomerBusinessStyle,
                hasPrincipal = customerDetails.HasPrincipal
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        public async Task<JsonResult> GetPrincipalDetails(int principalId)
        {
            var customerDetails = await _db.MMSIPrincipals
                .FindAsync(principalId);

            var customerDetailsJson = new
            {
                terms = customerDetails.Terms,
                address = customerDetails.Address,
                tinNo = customerDetails.TIN,
                businessStyle = customerDetails.BusinessType
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

        [HttpGet]
        public async Task<IActionResult> GetPrincipalsJSON(string customerId, CancellationToken cancellationToken)
        {
            var principalsList = await GetPrincipals(customerId, cancellationToken);

            return Json(principalsList);
        }

        [HttpGet]
        public async Task<List<SelectListItem>> GetPrincipals(string customerId, CancellationToken cancellationToken)
        {
            var principals = await _db
                .MMSIPrincipals
                .Where(t => t.CustomerId == int.Parse(customerId))
                .OrderBy(t => t.PrincipalName)
                .ToListAsync(cancellationToken);

            var principalsList = principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalNumber + " " + t.PrincipalName
            }).ToList();

            return principalsList;
        }
    }
}
