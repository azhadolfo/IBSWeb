using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.MMSI;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class BillingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<BillingController> _logger;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public BillingController(IUnitOfWork unitOfWork, ApplicationDbContext db, UserManager<IdentityUser> userManager,
            ILogger<BillingController> logger)
        {
            _unitOfWork = unitOfWork;
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Include(a => a.Terminal)
                .Include(a => a.Vessel)
                .Include (a => a.Customer)
                .Include (a => a.Port)
                .ToListAsync();

            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View(model);
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

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            MMSIBilling model = new()
            {
                Customers = await _unitOfWork.Msap.GetMMSICustomersById(cancellationToken),
                Vessels = await _unitOfWork.Msap.GetMMSIVesselsById(cancellationToken),
                Ports = await _unitOfWork.Msap.GetMMSIPortsById(cancellationToken)
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

                    if (model.IsUndocumented)
                    {
                        model.MMSIBillingNumber = await _unitOfWork.Msap.GenerateBillingNumber(cancellationToken);
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
                        Activity = $"Create billing #{newmodel.MMSIBillingNumber} for tickets #{string.Join(", #", model.ToBillDispatchTickets)}",
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

                    if (model.IsUndocumented)
                    {
                        TempData["success"] = $"Billing was successfully created. Control Number: {model.MMSIBillingNumber}";
                    }
                    else
                    {
                        TempData["success"] = $"Billing was successfully created.";
                    }

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";
                    model = await _unitOfWork.Msap.GetBillingLists(model, cancellationToken);

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

                model = await _unitOfWork.Msap.GetBillingLists(model, cancellationToken);

                return View(model);
            }
        }

        [HttpPost]
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

        [HttpPost]
        public async Task<IActionResult> GetBillingList([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var queried = _db.MMSIBillings
                    .Include(b => b.Customer)
                    .Include(b => b.Terminal)
                    .ThenInclude(b => b.Port)
                    .Include(b => b.Vessel)
                    .Where(b => b.Status != "For Posting" && b.Status != "Cancelled");

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
                            dt.Customer.CustomerName.ToString().Contains(searchValue) == true ||
                            dt.Terminal.TerminalName.ToString().Contains(searchValue) == true ||
                            dt.Terminal.Port.PortName.ToString().Contains(searchValue) == true ||
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
                                if (searchValue == "for collection")
                                {
                                    queried = queried.Where(s => s.Status == "For Collection");
                                }
                                if (searchValue == "collected")
                                {
                                    queried = queried.Where(s => s.Status == "Collected");
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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.MMSIBillingId == id, cancellationToken);

            // get select lists
            model = await _unitOfWork.Msap.GetBillingLists(model, cancellationToken);

            // get billed by current billing select list
            var unbilledDT = await _unitOfWork.Msap.GetMMSIUnbilledTicketsById(cancellationToken);

            // get not yet billed tickets select list
            model.UnbilledDispatchTickets = await _unitOfWork.Msap.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

            // add the both select list above
            model.UnbilledDispatchTickets.AddRange(unbilledDT);

            // get billed DTs
            var listOfDtBilled = await _db.MMSIDispatchTickets
                .Where(d => d.BillingId == id.ToString())
                .ToListAsync(cancellationToken);

            // put the already billed DT to string list so it appears on the select2 of view
            model.ToBillDispatchTickets = listOfDtBilled.Select(d => d.DispatchTicketId.ToString()).ToList();

            model.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);

            if (model.CustomerPrincipal == null)
            {
                ViewData["HasPrincipal"] = false;
            }
            else
            {
                ViewData["HasPrincipal"] = true;
            }

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

                    // handle what to do if isUndoc value exists
                    if(model.IsUndocumented != null)
                    {
                        // if new value is undoc:
                        if (model.IsUndocumented)
                        {
                            // and previous value is doc
                            if (!currentModel.IsUndocumented)
                            {
                                // replace with new generated
                                currentModel.MMSIBillingNumber = await _unitOfWork.Msap.GenerateBillingNumber(cancellationToken);
                            }
                            else
                            {
                                // if was already undoc, maintain the old generated
                                model.MMSIBillingNumber = currentModel.MMSIBillingNumber;
                            }
                        }
                        // if new is documented
                        if (!model.IsUndocumented)
                        {
                            // replace it with new regardless of past type
                            currentModel.MMSIBillingNumber = model.MMSIBillingNumber;
                        }
                    }

                    // empty list of string
                    List<string> idsOfBilledTickets = null;

                    // get dt billed by this billing
                    var tempModel = await _db.MMSIDispatchTickets
                        .Where(d => d.BillingId == model.MMSIBillingId.ToString())
                        .ToListAsync(cancellationToken);

                    // from dts billed, select the id=>string and list it
                    idsOfBilledTickets = tempModel.Select(d => d.DispatchTicketId.ToString()).OrderBy(x => x).ToList();

                    //put the dt billed to its model container
                    currentModel.ToBillDispatchTickets = idsOfBilledTickets;

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.VoyageNumber != model.VoyageNumber) { changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}"); }
                    if (currentModel.Date != model.Date) { changes.Add($"Date: {currentModel.Date} -> {model.Date}"); }
                    if (currentModel.TerminalId != model.TerminalId) { changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}"); }
                    if (currentModel.VesselId != model.VesselId) { changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}"); }
                    if (currentModel.CustomerId != model.CustomerId) { changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}"); }
                    if (currentModel.PrincipalId != model.PrincipalId) { changes.Add($"PrincipalId: {currentModel.PrincipalId} -> {model.PrincipalId}"); }
                    if (currentModel.BilledTo != model.BilledTo) { changes.Add($"IsVatable: {currentModel.BilledTo} -> {model.BilledTo}"); }
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
                            ? $"Edit billing #{currentModel.MMSIBillingNumber} {string.Join(", ", changes)}"
                            : $"No changes detected for Billing #{currentModel.MMSIBillingNumber}",
                        DocumentType = "Billing",
                        Company = await GetCompanyClaimAsync()
                    };

                    await _db.MMSIAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.Date = model.Date;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.CustomerId = model.CustomerId;
                    currentModel.PrincipalId = model.PrincipalId;
                    currentModel.BilledTo = model.BilledTo;
                    currentModel.Status = "For Collection";

                    // get billed by current billing select list
                    var unbilledDT = await _unitOfWork.Msap.GetMMSIUnbilledTicketsById(cancellationToken);

                    // get not yet billed tickets select list
                    model.UnbilledDispatchTickets = await _unitOfWork.Msap.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

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

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });

                }
                else
                {
                    TempData["error"] = "Can't create entry, please review your input.";

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    TempData["error"] = "Can't find entry.";

                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Preview (int id, CancellationToken cancellationToken)
        {
            var model = await _db.MMSIBillings
                .Where(b => b.MMSIBillingId == id)
                .Include(b => b.Terminal)
                .ThenInclude(t => t.Port)
                .Include(b => b.Vessel)
                .Include(b => b.Customer)
                .Include(b => b.Principal)
                .FirstOrDefaultAsync(cancellationToken);

            // list of dispatch numbers(to be used in selectlist2, multiselect)
            model.ToBillDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Select(dt => dt.DispatchNumber.ToString())
                .ToListAsync(cancellationToken);

            // list of dispatch tickets
            model.PaidDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.ActivityService)
                .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                .ToListAsync (cancellationToken);

            model.UniqueTugboats = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Select(dt => dt.Tugboat.TugboatName.ToString())
                .Distinct() // Ensures unique values
                .ToListAsync(cancellationToken);

            model = await _unitOfWork.Msap.ProcessAddress(model, cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<JsonResult> GetCustomerDetails(int customerId)
        {
            var customerDetails = await _db.FilprideCustomers
                .FindAsync(customerId);

            var principal = await _db.MMSIPrincipals
                .Where(p => p.CustomerId == customerId)
                .FirstOrDefaultAsync();

            bool hasPrincipal = default;

            if (principal != null)
            {
                hasPrincipal = true;
            }

            var customerDetailsJson = new
            {
                terms = customerDetails.CustomerTerms,
                address = customerDetails.CustomerAddress,
                tinNo = customerDetails.CustomerTin,
                businessStyle = customerDetails.BusinessStyle,
                hasPrincipal = hasPrincipal,
                vatType = customerDetails.VatType,
                isUndoc = customerDetails.Type //
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

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketsByCustomer (string customerId, CancellationToken cancellationToken)
        {
            var dispatchTickets = await _db
                .MMSIDispatchTickets
                .Where(t => t.CustomerId == int.Parse(customerId) && t.Status == "For Billing")
                .OrderBy(t => t.DispatchNumber)
                .ToListAsync(cancellationToken);

            var principalsList = dispatchTickets.Select(t => new SelectListItem
            {
                Value = t.DispatchTicketId.ToString(),
                Text = t.DispatchNumber
            }).ToList();

            return Json(principalsList);
        }


        [HttpPost]
        public async Task<List<SelectListItem>?> GetEditTickets(int? customerId, int billingId, CancellationToken cancellationToken = default)
        {
            // bills uncollected but with the same customers
            var list = await _unitOfWork.Msap.GetMMSIUnbilledTicketsByCustomer(customerId, cancellationToken);

            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId.ToString())
                .ToListAsync(cancellationToken);

            if (model?.FirstOrDefault()?.CustomerId == customerId)
            {
                list?.AddRange(await _unitOfWork.Msap.GetMMSIBilledTicketsById(billingId, cancellationToken));
            }

            return list;
        }
    }
}
