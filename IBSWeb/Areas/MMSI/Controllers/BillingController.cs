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
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class BillingController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BillingController> _logger;
        private readonly IUserAccessService _userAccessService;
        private const string FilterTypeClaimType = "DispatchTicket.FilterType";

        public BillingController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager,
            ILogger<BillingController> logger, IUserAccessService userAccessService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userAccessService = userAccessService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filterType, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Billing.GetAllAsync(null, cancellationToken);
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

            var viewModel = await GetBillingSelectLists(new CreateBillingViewModel(), cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBillingViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
                TempData["warning"] = "Can't create entry, please review your input.";
                return View(viewModel);
            }

            try
            {
                var model = CreateBillingVmToBillingModel(viewModel);
                model.Status = "For Collection";
                model.CreatedBy = await GetUserNameAsync() ?? throw new InvalidOperationException();
                var datetimeNow = DateTimeHelper.GetCurrentPhilippineTime();
                model.CreatedDate = datetimeNow;

                if (model.IsUndocumented)
                {
                    model.MMSIBillingNumber = await _unitOfWork.Billing.GenerateBillingNumber(cancellationToken);
                }
                else
                {
                    model.MMSIBillingNumber = viewModel.MMSIBillingNumber!;
                }

                await _unitOfWork.Billing.AddAsync(model, cancellationToken);
                var newmodel = await _unitOfWork.Billing.GetAsync(b => b.CreatedDate == datetimeNow, cancellationToken);
                int id = newmodel!.MMSIBillingId;
                newmodel = await _unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == id, cancellationToken);

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

                await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                #endregion -- Audit Trail

                decimal totalAmount = 0;

                foreach (var billDispatchTicket in model.ToBillDispatchTickets!)
                {
                    var dtEntry = await _unitOfWork.DispatchTicket.GetAsync(dt => dt.DispatchTicketId == int.Parse(billDispatchTicket), cancellationToken);
                    totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                    dtEntry!.Status = "Billed";
                    dtEntry.BillingId = model.MMSIBillingId.ToString();
                }

                model.Amount = totalAmount;
                await _unitOfWork.SaveAsync(cancellationToken);

                if (model.IsUndocumented)
                {
                    TempData["success"] = $"Billing was successfully created. Control Number: {model.MMSIBillingNumber}";
                }
                else
                {
                    TempData["success"] = $"Billing #{model.MMSIBillingNumber} was successfully created.";
                }

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create billing.");
                TempData["error"] = ex.Message;
                viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
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
                var dispatchTickets = await _unitOfWork.DispatchTicket
                    .GetAllAsync(t => intDispatchTicketIds.Contains(t.DispatchTicketId));

                return Json(new
                {
                    success = true,
                    data = dispatchTickets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dispatch tickets.");

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

                var queried = _dbContext.MMSIBillings
                    .Include(b => b.Customer)
                    .Include(b => b.Terminal)
                    .ThenInclude(b => b!.Port)
                    .Include(b => b.Vessel)
                    .Where(b => b.Status != "For Posting" && b.Status != "Cancelled");

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
                    }
                }

                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    queried = queried
                        .Where(dt =>
                            dt.Date.Day.ToString().Contains(searchValue) == true ||
                            dt.Date.Month.ToString().Contains(searchValue) == true ||
                            dt.Date.Year.ToString().Contains(searchValue) == true ||
                            dt.MMSIBillingNumber.ToLower().Contains(searchValue) == true ||
                            dt.Amount.ToString().Contains(searchValue) == true ||
                            dt.Customer!.CustomerName.ToLower().Contains(searchValue) == true ||
                            dt.Terminal!.TerminalName!.ToLower().Contains(searchValue) == true ||
                            dt.Terminal.Port!.PortName!.ToLower().Contains(searchValue) == true ||
                            dt.Vessel!.VesselName.ToLower().Contains(searchValue) == true ||
                            dt.Status.ToLower().Contains(searchValue) == true
                        );
                }

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
                _logger.LogError(ex, "Failed to get billings.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (!await _userAccessService.CheckAccess(_userManager.GetUserId(User)!, ProcedureEnum.CreateBilling, cancellationToken))
            {
                TempData["error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            var model = await _unitOfWork.Billing
                .GetAsync(b => b.MMSIBillingId == id, cancellationToken) ?? throw new NullReferenceException();

            var viewModel = BillingModelToCreateBillingVm(model);
            viewModel = await GetBillingSelectLists(viewModel, cancellationToken);
            viewModel.UnbilledDispatchTickets = await GetEditTickets(viewModel.CustomerId, viewModel.MMSIBillingId ?? 0, cancellationToken);
            viewModel.CustomerPrincipal = await GetPrincipals(model.CustomerId.ToString(), cancellationToken);

            viewModel.Terminals = await _unitOfWork.Terminal
                .GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);

            viewModel.ToBillDispatchTickets = await _unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId.ToString(), cancellationToken);

            viewModel.Customers = await _unitOfWork.Billing
                .GetMMSICustomersWithBillablesSelectList(viewModel.CustomerId, model.Customer!.Type, cancellationToken);

            if (model.CustomerPrincipal?.Count == 0 || model.CustomerPrincipal == null)
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

                    var currentModel = await _unitOfWork.Billing
                        .GetAsync(b => b.MMSIBillingId == model.MMSIBillingId, cancellationToken) ?? throw new NullReferenceException();

                    var tempModel = await _unitOfWork.DispatchTicket
                        .GetAllAsync(d => d.BillingId == model.MMSIBillingId.ToString(), cancellationToken);

                    List<string>? idsOfBilledTickets = tempModel.Select(d => d.DispatchTicketId.ToString()).OrderBy(x => x).ToList();
                    currentModel.ToBillDispatchTickets = idsOfBilledTickets;

                    #region -- Changes

                    var changes = new List<string>();

                    if (currentModel.CustomerId != model.CustomerId)
                    {
                        changes.Add($"CustomerId: {currentModel.CustomerId} -> {model.CustomerId}");
                    }

                    if (currentModel.VoyageNumber != model.VoyageNumber)
                    {
                        changes.Add($"VoyageNumber: {currentModel.VoyageNumber} -> {model.VoyageNumber}");
                    }

                    if (currentModel.Date != model.Date)
                    {
                        changes.Add($"Date: {currentModel.Date} -> {model.Date}");
                    }

                    if (currentModel.TerminalId != model.TerminalId)
                    {
                        changes.Add($"TerminalId: {currentModel.TerminalId} -> {model.TerminalId}");
                    }

                    if (currentModel.VesselId != model.VesselId)
                    {
                        changes.Add($"VesselId: {currentModel.VesselId} -> {model.VesselId}");
                    }

                    if (currentModel.BilledTo != model.BilledTo)
                    {
                        changes.Add($"IsVatable: {currentModel.BilledTo} -> {model.BilledTo}");
                    }

                    if (!currentModel.ToBillDispatchTickets.OrderBy(x => x)
                            .SequenceEqual(model.ToBillDispatchTickets!.OrderBy(x => x)))
                    {
                        changes.Add($"ToBillDispatchTickets: #{string.Join(", #", currentModel.ToBillDispatchTickets)} -> #{string.Join(", #", model.ToBillDispatchTickets!)}");
                    }

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

                    await _unitOfWork.FilprideAuditTrail.AddAsync(audit, cancellationToken);

                    #endregion -- Audit Trail

                    currentModel.CustomerId = model.CustomerId;
                    currentModel.VoyageNumber = model.VoyageNumber;
                    currentModel.Date = model.Date;
                    currentModel.PortId = model.PortId;
                    currentModel.TerminalId = model.TerminalId;
                    currentModel.VesselId = model.VesselId;
                    currentModel.BilledTo = model.BilledTo;
                    currentModel.Status = "For Collection";

                    model.UnbilledDispatchTickets = await _unitOfWork.Billing
                        .GetMMSIBilledTicketsById(model.MMSIBillingId, cancellationToken);

                    foreach (var dispatchTicket in model.UnbilledDispatchTickets)
                    {
                        var id = int.Parse(dispatchTicket.Value);

                        var dtModel = await _unitOfWork.DispatchTicket
                            .GetAsync(dt => dt.DispatchTicketId == id, cancellationToken);

                        dtModel!.Status = "For Billing";
                        dtModel.BillingId = "0";
                    }

                    await _unitOfWork.DispatchTicket.SaveAsync(cancellationToken);
                    decimal totalAmount = 0;

                    foreach (var billDispatchTicket in model.ToBillDispatchTickets!)
                    {
                        var dtEntry = await _unitOfWork.DispatchTicket
                            .GetAsync(dt => dt.DispatchTicketId == int.Parse(billDispatchTicket), cancellationToken);

                        totalAmount = (totalAmount + dtEntry?.TotalNetRevenue) ?? 0m;
                        dtEntry!.Status = "Billed";
                        dtEntry.BillingId = model.MMSIBillingId.ToString();
                    }

                    currentModel.Amount = totalAmount;
                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Entry edited successfully!";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                else
                {
                    TempData["warning"] = "Can't create entry, please review your input.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit billing.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var model = await _unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (model != null)
                {
                    await _unitOfWork.Billing.RemoveAsync(model, cancellationToken);
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
                _logger.LogError(ex, "Failed to delete billing.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.Billing.GetAsync(b => b.MMSIBillingId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            model.ToBillDispatchTickets = await _unitOfWork.Billing
                .GetToBillDispatchTicketListAsync(model.MMSIBillingId.ToString(), cancellationToken);

            model.PaidDispatchTickets = await _unitOfWork.Billing
                .GetPaidDispatchTicketsAsync(model.MMSIBillingId.ToString(), cancellationToken);

            model.UniqueTugboats = await _unitOfWork.Billing
                .GetUniqueTugboatsListAsync(model.MMSIBillingId.ToString(), cancellationToken) ?? throw new NullReferenceException();

            model = _unitOfWork.Billing.ProcessAddress(model, cancellationToken);
            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            try
            {
                var billing = await _unitOfWork.Billing
                    .GetAsync(b => b.MMSIBillingId == id, cancellationToken);

                if (billing == null)
                {
                    TempData["error"] = "Billing not found";
                    return RedirectToAction(nameof(Index));
                }

                billing.ToBillDispatchTickets = await _unitOfWork.Billing
                    .GetToBillDispatchTicketListAsync(billing.MMSIBillingId.ToString(), cancellationToken);

                billing.PaidDispatchTickets = await _unitOfWork.Billing
                    .GetPaidDispatchTicketsAsync(billing.MMSIBillingId.ToString(), cancellationToken) ?? throw new NullReferenceException();

                billing.UniqueTugboats = await _unitOfWork.Billing
                    .GetUniqueTugboatsListAsync(billing.MMSIBillingId.ToString(), cancellationToken) ?? throw new NullReferenceException();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add($"Billing #{billing.MMSIBillingNumber}");
                worksheet.Cells.Style.Font.Name = "Calibri";
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
                    foreach (var ticket in billing.PaidDispatchTickets.Where(t => t.BAFNetRevenue != 0))
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

                worksheet.Cells[1, 1, row, 7].Style.Font.Name = "Calibri";
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 53;
                worksheet.Column(3).Width = 9;
                worksheet.Column(4).Width = 8.5;
                worksheet.Column(5).Width = 16;
                var excelBytes = package.GetAsByteArray();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DotMatrix_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print billing.");
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Error generating sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                ex.Message, ex.StackTrace, _userManager.GetUserAsync(User));
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetCustomerDetails(int customerId)
        {
            var customerDetails = await _unitOfWork.FilprideCustomer
                .GetAsync(c => c.CustomerId == customerId);

            if (customerDetails == null)
            {
                return new JsonResult("Customer not found");
            }

            var principal = await _unitOfWork.Principal
                .GetAsync(c => c.CustomerId == customerId);

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
                isUndoc = customerDetails.Type
            };

            return Json(customerDetailsJson);
        }

        [HttpGet]
        public async Task<JsonResult> GetPrincipalDetails(int principalId)
        {
            var customerDetails = await _unitOfWork.Principal
                .GetAsync(c => c.PrincipalId == principalId);

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
            var principals = await _unitOfWork.Principal
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId), cancellationToken);

            var principalsList = principals.Select(t => new SelectListItem
            {
                Value = t.PrincipalId.ToString(),
                Text = t.PrincipalName
            }).ToList();

            return principalsList;
        }

        [HttpGet]
        public async Task<IActionResult> GetDispatchTicketsByCustomer(string customerId, CancellationToken cancellationToken)
        {
            //order by dispatch number
            var dispatchTickets = await _unitOfWork.DispatchTicket
                .GetAllAsync(t => t.CustomerId == int.Parse(customerId) && t.Status == "For Billing", cancellationToken);

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
            var listToReturn = await _unitOfWork.Billing.GetMMSIUnbilledTicketsByCustomer(customerId, cancellationToken);
            IEnumerable<MMSIDispatchTicket>? billedTickets = null;

            if (billingId != 0)
            {
                billedTickets = await _unitOfWork.DispatchTicket
                    .GetAllAsync(dt => dt.BillingId == billingId.ToString(), cancellationToken);
            }

            if (billedTickets != null && billedTickets?.FirstOrDefault()?.CustomerId == customerId)
            {
                listToReturn?.AddRange(await _unitOfWork.Billing.GetMMSIBilledTicketsById(billingId, cancellationToken));
            }

            return listToReturn;
        }

        public async Task<CreateBillingViewModel> GetBillingSelectLists(CreateBillingViewModel viewModel, CancellationToken cancellationToken = default)
        {
            viewModel.Vessels = await _unitOfWork.Vessel.GetMMSIVesselsSelectList(cancellationToken);
            viewModel.Ports = await _unitOfWork.Port.GetMMSIPortsSelectList(cancellationToken);
            viewModel.Customers = await _unitOfWork.Billing.GetMMSICustomersWithBillablesSelectList(0, string.Empty, cancellationToken);

            if (viewModel.PortId != 0)
            {
                viewModel.Terminals = await _unitOfWork.Terminal.GetMMSITerminalsSelectList(viewModel.PortId, cancellationToken);
                return viewModel;
            }

            return viewModel;
        }
    }
}
