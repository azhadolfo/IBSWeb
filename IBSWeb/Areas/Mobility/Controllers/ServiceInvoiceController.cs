using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class ServiceInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ServiceInvoiceController> _logger;

        public ServiceInvoiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<ServiceInvoiceController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetServiceInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var serviceInvoices = await _unitOfWork.MobilityServiceInvoice
                    .GetAllAsync(sv => sv.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    serviceInvoices = serviceInvoices
                        .Where(s =>
                            s.ServiceInvoiceNo.ToLower().Contains(searchValue) ||
                            s.Customer?.CustomerName.ToLower().Contains(searchValue) == true ||
                            s.Customer?.CustomerTerms.ToLower().Contains(searchValue) == true ||
                            s.Service?.ServiceNo?.ToLower().Contains(searchValue) == true ||
                            s.Service?.Name.ToLower().Contains(searchValue) == true ||
                            s.Period.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.Instructions?.ToLower().Contains(searchValue) == true ||
                            s.CreatedBy?.ToLower().Contains(searchValue) == true
                            )
                        .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    serviceInvoices = serviceInvoices
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = serviceInvoices.Count();

                var pagedData = serviceInvoices
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
                _logger.LogError(ex, "Failed to get service invoice. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ServiceInvoiceViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.Customers = await _dbContext.MobilityCustomers
                .Where(c => c.StationCode == stationCodeClaims)
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);

            viewModel.Services = await _dbContext.MobilityServices
                .Where(s => s.StationCode == stationCodeClaims)
                .OrderBy(s => s.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Customers = await _dbContext.MobilityCustomers
                    .Where(c => c.StationCode == stationCodeClaims)
                    .OrderBy(c => c.CustomerId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CustomerId.ToString(),
                        Text = c.CustomerName
                    })
                    .ToListAsync(cancellationToken);
                viewModel.Services = await _dbContext.MobilityServices
                    .Where(s => s.StationCode == stationCodeClaims)
                    .OrderBy(s => s.ServiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceId.ToString(),
                        Text = s.Name
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(Create), viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Retrieval of Customer

                var customer = await _unitOfWork.MobilityCustomer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken);

                if (customer == null)
                {
                    return NotFound();
                }

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                MobilityServiceInvoice model = new()
                {
                    ServiceInvoiceNo = await _unitOfWork.MobilityServiceInvoice.GenerateCodeAsync(stationCodeClaims, viewModel.Type, cancellationToken),
                    CreatedBy = User.Identity!.Name,
                    Total = viewModel.Amount,
                    StationCode = stationCodeClaims,
                    CustomerAddress = customer.CustomerAddress,
                    CustomerTin = customer.CustomerTin,
                    Type = viewModel.Type,
                    CustomerId = viewModel.CustomerId,
                    ServiceId = viewModel.ServiceId,
                    DueDate = viewModel.DueDate,
                    Discount = viewModel.Discount,
                    Instructions = viewModel.Instructions,
                    Period = viewModel.Period,
                    Amount = viewModel.Amount,
                };

                if (DateOnly.FromDateTime(model.CreatedDate) < model.Period)
                {
                    model.UnearnedAmount += model.Amount;
                }
                else
                {
                    model.CurrentAndPreviousAmount += model.Amount;
                }

                _dbContext.Add(model);

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Created new service invoice# {model.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Service invoice created successfully. Series Number: {model.ServiceInvoiceNo}.";
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Customers = await _dbContext.MobilityCustomers
                    .Where(c => c.StationCode == stationCodeClaims)
                    .OrderBy(c => c.CustomerId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CustomerId.ToString(),
                        Text = c.CustomerName
                    })
                    .ToListAsync(cancellationToken);
                viewModel.Services = await _dbContext.MobilityServices
                    .Where(s => s.StationCode == stationCodeClaims)
                    .OrderBy(s => s.ServiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceId.ToString(),
                        Text = s.Name
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.MobilityServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(Status.Posted);

                    #region --SV Date Computation--

                        var postedDate = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                    #endregion --SV Date Computation--

                    ///TODO: waiting for ma'am LSA journal entries
                    #region --General Ledger Book Recording

                        // var ledgers = new List<FilprideGeneralLedgerBook>();
                        // var accountTitlesDto = await _unitOfWork.FilprideServiceInvoice.GetListOfAccountTitleDto(cancellationToken);
                        // var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
                        // var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                        // var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
                        // var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");
                        //
                        // //TODO waiting for Ma'am LSA journal entries
                        // ledgers.Add(
                        //         new FilprideGeneralLedgerBook
                        //         {
                        //             Date = postedDate,
                        //             Reference = model.ServiceInvoiceNo,
                        //             Description = model.Service.Name,
                        //             AccountId = arNonTradeTitle.AccountId,
                        //             AccountNo = arNonTradeTitle.AccountNumber,
                        //             AccountTitle = arNonTradeTitle.AccountName,
                        //             Debit = Math.Round(model.Total - (withHoldingTaxAmount + withHoldingVatAmount), 4),
                        //             Credit = 0,
                        //             Company = model.Company,
                        //             CreatedBy = model.CreatedBy,
                        //             CreatedDate = model.CreatedDate,
                        //             CustomerId = model.CustomerId
                        //         }
                        //     );
                        // if (withHoldingTaxAmount > 0)
                        // {
                        //     ledgers.Add(
                        //         new FilprideGeneralLedgerBook
                        //         {
                        //             Date = postedDate,
                        //             Reference = model.ServiceInvoiceNo,
                        //             Description = model.Service.Name,
                        //             AccountId = arTradeCwt.AccountId,
                        //             AccountNo = arTradeCwt.AccountNumber,
                        //             AccountTitle = arTradeCwt.AccountName,
                        //             Debit = withHoldingTaxAmount,
                        //             Credit = 0,
                        //             Company = model.Company,
                        //             CreatedBy = model.CreatedBy,
                        //             CreatedDate = model.CreatedDate
                        //         }
                        //     );
                        // }
                        // if (withHoldingVatAmount > 0)
                        // {
                        //     ledgers.Add(
                        //         new FilprideGeneralLedgerBook
                        //         {
                        //             Date = postedDate,
                        //             Reference = model.ServiceInvoiceNo,
                        //             Description = model.Service.Name,
                        //             AccountId = arTradeCwv.AccountId,
                        //             AccountNo = arTradeCwv.AccountNumber,
                        //             AccountTitle = arTradeCwv.AccountName,
                        //             Debit = withHoldingVatAmount,
                        //             Credit = 0,
                        //             Company = model.Company,
                        //             CreatedBy = model.CreatedBy,
                        //             CreatedDate = model.CreatedDate
                        //         }
                        //     );
                        // }
                        //
                        // ledgers.Add(
                        //        new FilprideGeneralLedgerBook
                        //        {
                        //            Date = postedDate,
                        //            Reference = model.ServiceInvoiceNo,
                        //            Description = model.Service.Name,
                        //            AccountNo = model.Service.CurrentAndPreviousNo,
                        //            AccountTitle = model.Service.CurrentAndPreviousTitle,
                        //            Debit = 0,
                        //            Credit = Math.Round((netOfVatAmount), 4),
                        //            Company = model.Company,
                        //            CreatedBy = model.CreatedBy,
                        //            CreatedDate = model.CreatedDate
                        //        }
                        //    );
                        //
                        // if (vatAmount > 0)
                        // {
                        //     ledgers.Add(
                        //         new FilprideGeneralLedgerBook
                        //         {
                        //             Date = postedDate,
                        //             Reference = model.ServiceInvoiceNo,
                        //             Description = model.Service.Name,
                        //             AccountId = vatOutputTitle.AccountId,
                        //             AccountNo = vatOutputTitle.AccountNumber,
                        //             AccountTitle = vatOutputTitle.AccountName,
                        //             Debit = 0,
                        //             Credit = Math.Round((vatAmount), 4),
                        //             Company = model.Company,
                        //             CreatedBy = model.CreatedBy,
                        //             CreatedDate = model.CreatedDate
                        //         }
                        //     );
                        // }
                        //
                        // if (!_unitOfWork.FilprideServiceInvoice.IsJournalEntriesBalanced(ledgers))
                        // {
                        //     throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        // }
                        //
                        // await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                    #endregion --General Ledger Book Recording

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted service invoice# {model.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Service invoice has been posted.";
                    return RedirectToAction(nameof(Print), new { id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {

            var model = await _unitOfWork.MobilityServiceInvoice
                .GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled service invoice# {model.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {

            var model = await _unitOfWork.MobilityServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            if (model != null)
            {
                ///TODO: uncomment this if the other modules are implemented
                //var hasAlreadyBeenUsed =
                    //await _dbContext.FilprideCollectionReceipts.AnyAsync(cr => cr.ServiceInvoiceId == model.ServiceInvoiceId && cr.Status != nameof(Status.Voided), cancellationToken) ||
                    //await _dbContext.FilprideDebitMemos.AnyAsync(dm => dm.ServiceInvoiceId == model.ServiceInvoiceId && dm.Status != nameof(Status.Voided), cancellationToken) ||
                    //await _dbContext.FilprideCreditMemos.AnyAsync(cm => cm.ServiceInvoiceId == model.ServiceInvoiceId && cm.Status != nameof(Status.Voided), cancellationToken);

                //if (hasAlreadyBeenUsed)
                //{
                    //TempData["error"] = "Please note that this record has already been utilized in a collection receipts, debit or credit memo. As a result, voiding it is not permitted.";
                    //return RedirectToAction(nameof(Index));
                //}

                if (model.VoidedBy == null)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        ///TODO: waiting for ma'am LSA journal entries
                        //await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideSalesBook>(gl => gl.SerialNo == model.ServiceInvoiceNo, cancellationToken);
                        //await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.ServiceInvoiceNo, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided service invoice# {model.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been voided.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to void service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                            ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingRecord = await _unitOfWork.MobilityServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            ServiceInvoiceViewModel viewModel = new()
            {
                Customers = await _dbContext.MobilityCustomers
                    .OrderBy(c => c.CustomerId)
                    .Where(c => c.StationCode == stationCodeClaims)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CustomerId.ToString(),
                        Text = c.CustomerName
                    })
                    .ToListAsync(cancellationToken),

                Services = await _dbContext.MobilityServices
                    .OrderBy(s => s.ServiceId)
                    .Where(s => s.StationCode == stationCodeClaims)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceId.ToString(),
                        Text = s.Name
                    })
                    .ToListAsync(cancellationToken),
                Total = existingRecord.Amount,
                CustomerAddress = existingRecord.CustomerAddress,
                CustomerTin = existingRecord.CustomerTin,
                Type = existingRecord.Type,
                CustomerId = existingRecord.CustomerId,
                ServiceId = existingRecord.ServiceId,
                DueDate = existingRecord.DueDate,
                Discount = existingRecord.Discount,
                Instructions = existingRecord.Instructions,
                Period = existingRecord.Period,
                Amount = existingRecord.Amount,
                ServiceInvoiceId = existingRecord.ServiceInvoiceId,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingRecord = await _unitOfWork.MobilityServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == viewModel.ServiceInvoiceId, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await _dbContext.MobilityCustomers
                        .FirstOrDefaultAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken);

                if (customer == null)
                {
                    return NotFound();
                }

                #region --Saving the default properties

                    existingRecord.Discount = viewModel.Discount;
                    existingRecord.Amount = viewModel.Amount;
                    existingRecord.Period = viewModel.Period;
                    existingRecord.DueDate = viewModel.DueDate;
                    existingRecord.Instructions = viewModel.Instructions;
                    existingRecord.EditedBy = _userManager.GetUserName(User);
                    existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.Total = viewModel.Amount;
                    existingRecord.CustomerId = viewModel.CustomerId;
                    existingRecord.ServiceId = viewModel.ServiceId;
                    existingRecord.CustomerAddress = customer.CustomerAddress;
                    existingRecord.CustomerTin = customer.CustomerTin;

                    if (DateOnly.FromDateTime(existingRecord.CreatedDate) < viewModel.Period)
                    {
                        existingRecord.UnearnedAmount += viewModel.Amount;
                    }
                    else
                    {
                        existingRecord.CurrentAndPreviousAmount += viewModel.Amount;
                    }

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edited service invoice# {existingRecord.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Service invoice updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var sv = await _unitOfWork.MobilityServiceInvoice
                .GetAsync(x => x.ServiceInvoiceId == id, cancellationToken);

            if (sv == null)
            {
                return NotFound();
            }

            if (!sv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of service invoice# {sv.ServiceInvoiceNo}", "Service Invoice", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                sv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}
