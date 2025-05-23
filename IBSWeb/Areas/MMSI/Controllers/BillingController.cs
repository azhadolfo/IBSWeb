using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.MMSI;
using IBS.Services;
using IBS.Models.MMSI.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
        private readonly IUserAccessService _userAccessService;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public BillingController(IUnitOfWork unitOfWork, ApplicationDbContext db, UserManager<IdentityUser> userManager,
            ILogger<BillingController> logger, IUserAccessService userAccessService)
        {
            _unitOfWork = unitOfWork;
            _db = db;
            _userManager = userManager;
            _userAccessService = userAccessService;
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

        private async Task<string?> GetCurrentFilterType()
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

            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            CreateBillingViewModel viewModel = new()
            {
                Customers = await _unitOfWork.Billing.GetMMSICustomersWithBillablesById(cancellationToken),
                Vessels = await _unitOfWork.Billing.GetMMSIVesselsById(cancellationToken),
                Ports = await _unitOfWork.Billing.GetMMSIPortsById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBillingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                viewModel = await _unitOfWork.Billing.GetBillingLists(viewModel, cancellationToken);

                TempData["error"] = "Can't create entry, please review your input.";
                return View(viewModel);
            }

            try
            {
                var model = CreateBillingVmToBillingModel(viewModel);

                model.Status = "For Collection";
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                var datetimeNow = DateTime.Now;
                model.CreatedDate = datetimeNow;

                if (model.IsUndocumented)
                {
                    model.MMSIBillingNumber = await _unitOfWork.Billing.GenerateBillingNumber(cancellationToken);
                }
                else
                {
                    model.MMSIBillingNumber = viewModel.MMSIBillingNumber!;
                }

                // create it first so it generates id
                await _db.MMSIBillings.AddAsync(model, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                // find it and reference it
                var newmodel = await _db.MMSIBillings.Where(b => b.CreatedDate == datetimeNow).FirstOrDefaultAsync(cancellationToken);

                int id = newmodel!.MMSIBillingId;

                newmodel = await _db.MMSIBillings.FindAsync(id, cancellationToken);

                #region -- Audit Trail

                var audit = new FilprideAuditTrail
                {
                    Date = DateTimeHelper.GetCurrentPhilippineTime(),
                    Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                    MachineName = Environment.MachineName,
                    Activity = $"Create billing #{newmodel!.MMSIBillingNumber} for tickets #{string.Join(", #", model.ToBillDispatchTickets!)}",
                    DocumentType = "Billing",
                    Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                };

                await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                #endregion -- Audit Trail

                decimal totalAmount = 0;

                foreach(var billDispatchTicket in model.ToBillDispatchTickets!)
                {
                    var dtEntry = await _db.MMSIDispatchTickets
                        .FindAsync(int.Parse(billDispatchTicket));

                    totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                    dtEntry!.Status = "Billed";
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
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                viewModel = await _unitOfWork.Billing.GetBillingLists(viewModel, cancellationToken);

                return View(viewModel);
            }
        }

        public MMSIBilling CreateBillingVmToBillingModel(CreateBillingViewModel viewModel)
        {
            var model = new MMSIBilling
            {
                Date = viewModel.Date,
                IsUndocumented = viewModel.IsUndocumented,
                BilledTo = viewModel.BilledTo,
                VoyageNumber = viewModel.VoyageNumber,
                Amount = viewModel.Amount,
                IsPrincipal = viewModel.IsPrincipal,
                CustomerId = viewModel.CustomerId,
                PrincipalId = viewModel.PrincipalId,
                VesselId = viewModel.VesselId,
                PortId = viewModel.PortId,
                TerminalId = viewModel.TerminalId,
                ToBillDispatchTickets = viewModel.ToBillDispatchTickets,
            };

            if (viewModel.MMSIBillingId != null)
            {
                model.MMSIBillingId = viewModel.MMSIBillingId ?? 0;

                if (model.MMSIBillingId == 0)
                {
                    throw new NullReferenceException("MMSIBillingId cannot be zero.");
                }
            }

            return model;
        }

        public CreateBillingViewModel BillingModelToCreateBillingVm(MMSIBilling model)
        {
            var viewModel = new CreateBillingViewModel
            {
                MMSIBillingId = model.MMSIBillingId,
                MMSIBillingNumber = model.MMSIBillingNumber,
                Date = model.Date,
                IsUndocumented = model.IsUndocumented,
                BilledTo = model.BilledTo,
                VoyageNumber = model.VoyageNumber,
                Amount = model.Amount,
                IsPrincipal = model.IsPrincipal,
                CustomerId = model.CustomerId,
                PrincipalId = model.PrincipalId,
                VesselId = model.VesselId,
                PortId = model.PortId,
                TerminalId = model.TerminalId,
                ToBillDispatchTickets = model.ToBillDispatchTickets
            };

            return viewModel;
        }

        [HttpPost]
        public async Task<IActionResult> GetDispatchTickets(List<string> dispatchTicketIds)
        {
            try
            {
                var intDispatchTicketIds = dispatchTicketIds.Select(int.Parse).ToList();
                var dispatchTickets = await _db.MMSIDispatchTickets
                    .Include(t => t.Tugboat)
                    .Include(t => t.Service)
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
                    .ThenInclude(b => b!.Port)
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
                            dt.Customer!.CustomerName.ToString().Contains(searchValue) == true ||
                            dt.Terminal!.TerminalName!.ToString().Contains(searchValue) == true ||
                            dt.Terminal.Port!.PortName!.ToString().Contains(searchValue) == true ||
                            dt.Vessel!.VesselName.ToString().Contains(searchValue) == true ||
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
            if (!await _userAccessService.CheckAccess( _userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _db.MMSIBillings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.MMSIBillingId == id, cancellationToken) ?? throw new NullReferenceException();

            var viewModel = BillingModelToCreateBillingVm(model);

            // get select lists
            viewModel = await _unitOfWork.Billing.GetBillingLists(viewModel, cancellationToken);

            viewModel.Terminals = await _unitOfWork.Billing.GetMMSITerminalsByPortId(viewModel.PortId, cancellationToken);

            // get billed by current billing select list
            var unbilledDT = await _unitOfWork.Billing.GetMMSIUnbilledTicketsById(cancellationToken);

            // get not yet billed tickets select list
            viewModel.UnbilledDispatchTickets = await _unitOfWork.Billing.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

            // add the both select list above
            viewModel.UnbilledDispatchTickets.AddRange(unbilledDT);

            // get billed DTs
            var listOfDtBilled = await _db.MMSIDispatchTickets
                .Where(d => d.BillingId == id.ToString())
                .ToListAsync(cancellationToken);

            // put the already billed DT to string list so it appears on the select2 of view
            viewModel.ToBillDispatchTickets = listOfDtBilled.Select(d => d.DispatchTicketId.ToString()).ToList();

            viewModel.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);

            viewModel.Customers = await _unitOfWork.Billing.GetMMSICustomersById(cancellationToken);

            if (model.CustomerPrincipal == null)
            {
                ViewData["HasPrincipal"] = false;
            }
            else
            {
                ViewData["HasPrincipal"] = true;
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreateBillingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var model = CreateBillingVmToBillingModel(viewModel);

                    // get previous version
                    var currentModel = await _db.MMSIBillings
                        .FindAsync(model.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                    // empty list of string
                    List<string>? idsOfBilledTickets;

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
                    if (currentModel.BilledTo != model.BilledTo) { changes.Add($"IsVatable: {currentModel.BilledTo} -> {model.BilledTo}"); }
                    if (!currentModel.ToBillDispatchTickets.OrderBy(x => x).SequenceEqual(model.ToBillDispatchTickets!.OrderBy(x => x)))
                    { changes.Add($"ToBillDispatchTickets: #{string.Join(", #", currentModel.ToBillDispatchTickets)} -> #{string.Join(", #", model.ToBillDispatchTickets!)}"); }

                    #endregion -- Changes

                    #region -- Audit Trail

                    var audit = new FilprideAuditTrail
                    {
                        Date = DateTimeHelper.GetCurrentPhilippineTime(),
                        Username = await GetUserNameAsync() ?? throw new InvalidOperationException(),
                        MachineName = Environment.MachineName,
                        Activity = changes.Any()
                            ? $"Edit billing #{currentModel.MMSIBillingNumber} {string.Join(", ", changes)}"
                            : $"No changes detected for Billing #{currentModel.MMSIBillingNumber}",
                        DocumentType = "Billing",
                        Company = await GetCompanyClaimAsync() ?? throw new InvalidOperationException()
                    };

                    await _db.FilprideAuditTrails.AddAsync(audit, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.Date = model.Date;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.BilledTo = model.BilledTo;
                    currentModel.Status = "For Collection";

                    // get billed by current billing select list
                    var unbilledDT = await _unitOfWork.Billing.GetMMSIUnbilledTicketsById(cancellationToken);

                    // get not yet billed tickets select list
                    model.UnbilledDispatchTickets = await _unitOfWork.Billing.GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

                    // add the both select list above
                    model.UnbilledDispatchTickets.AddRange(unbilledDT);

                    // reset status of all the dispatch ticket affected by editing select
                    foreach (var dispatchTicket in model.UnbilledDispatchTickets)
                    {
                        var id = int.Parse(dispatchTicket.Value);
                        var dtModel = await _db.MMSIDispatchTickets.FindAsync(id, cancellationToken);
                        dtModel!.Status = "For Billing";
                        dtModel.BillingId = "0";
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    decimal totalAmount = 0;

                    // re-bill the new selected tickets
                    foreach(var billDispatchTicket in model.ToBillDispatchTickets!)
                    {
                        var dtEntry = await _db.MMSIDispatchTickets.FindAsync(int.Parse(billDispatchTicket));
                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry!.Status = "Billed";
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
                if (string.IsNullOrEmpty(ex.InnerException?.Message))
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
                .ThenInclude(t => t!.Port)
                .Include(b => b.Vessel)
                .Include(b => b.Customer)
                .Include(b => b.Principal)
                .FirstOrDefaultAsync(cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            // list of dispatch numbers(to be used in selectlist2, multiselect)
            model.ToBillDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Select(dt => dt.DispatchNumber.ToString())
                .ToListAsync(cancellationToken);

            // list of dispatch tickets
            model.PaidDispatchTickets = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Include(dt => dt.Tugboat)
                .Include(dt => dt.Service)
                .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                .ToListAsync (cancellationToken);

            model.UniqueTugboats = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == model.MMSIBillingId.ToString())
                .Select(dt => dt.Tugboat!.TugboatName.ToString())
                .Distinct() // Ensures unique values
                .ToListAsync(cancellationToken);

            model = _unitOfWork.Billing.ProcessAddress(model, cancellationToken);

            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await _db.MMSIBillings
                    .Where(b => b.MMSIBillingId == id)
                    .Include(b => b.Terminal)
                    .ThenInclude(t => t!.Port)
                    .Include(b => b.Vessel)
                    .Include(b => b.Customer)
                    .Include(b => b.Principal)
                    .FirstOrDefaultAsync(cancellationToken);

                // check if there is no record
                if (billing == null)
                {
                    TempData["error"] = "Billing not found";
                    return RedirectToAction(nameof(Index));
                }

                // list of dispatch numbers(to be used in selectlist2, multiselect)
                billing.ToBillDispatchTickets = await _db.MMSIDispatchTickets
                    .Where(dt => dt.BillingId == billing.MMSIBillingId.ToString())
                    .Select(dt => dt.DispatchNumber.ToString())
                    .ToListAsync(cancellationToken);

                // list of dispatch tickets
                billing.PaidDispatchTickets = await _db.MMSIDispatchTickets
                    .Where(dt => dt.BillingId == billing.MMSIBillingId.ToString())
                    .Include(dt => dt.Tugboat)
                    .Include(dt => dt.Service)
                    .OrderBy(dt => dt.DateLeft).ThenBy(dt => dt.TimeLeft)
                    .ToListAsync (cancellationToken);

                billing.UniqueTugboats = await _db.MMSIDispatchTickets
                    .Where(dt => dt.BillingId == billing.MMSIBillingId.ToString())
                    .Select(dt => dt.Tugboat!.TugboatName.ToString())
                    .Distinct() // Ensures unique values
                    .ToListAsync(cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add($"Billing# {billing.MMSIBillingNumber}");

                worksheet.Cells["B2"].Value = $"{billing.Customer?.CustomerName}";
                worksheet.Cells["E2"].Value = $"{billing.Date}";
                worksheet.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells["B3"].Value = $"{billing.Customer?.CustomerAddress}                              TERMS: {billing.Customer?.CustomerTerms}";
                worksheet.Cells["B4"].Value = $"{billing.Customer?.CustomerTin}";
                worksheet.Cells["E4"].Value = $"VOYAGE NO. {billing.VoyageNumber}";
                worksheet.Cells["B6"].Value = $"FOR THE SERVICE RE: {billing.Vessel?.VesselName}";
                worksheet.Cells["B7"].Value = $"LOCATION PORT: {billing.Port!.PortName}";

                var rowStart = 9;
                var row = rowStart;

                foreach (var tugboat in billing.UniqueTugboats)
                {
                    worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: {tugboat}";
                    row++;
                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.Tugboat?.TugboatName == tugboat))
                    {
                        worksheet.Cells[row, 1].Value = "1";
                        worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                        worksheet.Cells[row, 4].Value = $"{ticket.DispatchRate}";
                        worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 5].Value = $"{ticket.DispatchBillingAmount}";
                        worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        row++;
                    }
                    row++;
                }

                var ticketsWithBAF = billing.PaidDispatchTickets;

                if (ticketsWithBAF != null)
                {
                    foreach(var ticket in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                    {
                        worksheet.Cells[row, 2].Value = $"NAME OF TUGBOAT: BUNKER ADJUSTMENT FACTOR";
                        row++;
                        foreach (var record in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
                        {
                            worksheet.Cells[row, 1].Value = "1";
                            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 2].Value = $"{ticket.Service?.ServiceName}          {ticket.DateLeft} {ticket.TimeLeft}          {ticket.DateArrived} {ticket.TimeArrived}";
                            worksheet.Cells[row, 4].Value = $"{ticket.BAFRate}";
                            worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, 5].Value = $"{ticket.BAFNetRevenue}";
                            worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            row++;
                        }
                        row++;
                    }
                }

                worksheet.Column(1).Width = 8.5;
                worksheet.Column(2).Width = 60;
                worksheet.Column(3).Width = 8.7;
                worksheet.Column(4).Width = 9.2;
                worksheet.Column(5).Width = 18;

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Error generating sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                ex.Message, ex.StackTrace, _userManager.GetUserAsync(User));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetCustomerDetails(int customerId)
        {
            var customerDetails = await _db.FilprideCustomers
                .FindAsync(customerId);

            if (customerDetails == null)
            {
                return new JsonResult("Customer not found");
            }

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

            if (customerDetails == null)
            {
                return new JsonResult("Principal not found");
            }

            var customerDetailsJson = new
            {
                terms = customerDetails.Terms,
                address = customerDetails.Address,
                tinNo = customerDetails.TIN,
                businessStyle = customerDetails.BusinessType
            };

            return Json(customerDetailsJson);
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
                Text = t.PrincipalName
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
            var list = await _unitOfWork.Billing.GetMMSIUnbilledTicketsByCustomer(customerId, cancellationToken);

            var model = await _db.MMSIDispatchTickets
                .Where(dt => dt.BillingId == billingId.ToString())
                .ToListAsync(cancellationToken);

            if (model?.FirstOrDefault()?.CustomerId == customerId)
            {
                list?.AddRange(await _unitOfWork.Billing.GetMMSIBilledTicketsById(billingId, cancellationToken));
            }

            return list;
        }
    }
}
