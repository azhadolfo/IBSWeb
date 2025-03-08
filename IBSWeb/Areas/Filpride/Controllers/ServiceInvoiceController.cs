using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
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

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.ServiceInvoice))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var serviceInvoices = await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(sv => sv.Company == companyClaims && sv.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", serviceInvoices);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetServiceInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var serviceInvoices = await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(sv => sv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    serviceInvoices = serviceInvoices
                        .Where(s =>
                            s.ServiceInvoiceNo.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerTerms.ToLower().Contains(searchValue) ||
                            s.Service.ServiceNo.ToLower().Contains(searchValue) ||
                            s.Service.Name.ToLower().Contains(searchValue) ||
                            s.Period.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.Instructions?.ToLower().Contains(searchValue) == true ||
                            s.CreatedBy.ToLower().Contains(searchValue)
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
                _logger.LogError(ex, "Failed to get service invoices.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideServiceInvoice();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Customers = await _dbContext.FilprideCustomers
                .Where(c => c.Company == companyClaims)
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            viewModel.Services = await _dbContext.FilprideServices
                .Where(s => s.Company == companyClaims)
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
        public async Task<IActionResult> Create(FilprideServiceInvoice model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Customers = await _dbContext.FilprideCustomers
                .Where(c => c.Company == companyClaims)
                .OrderBy(c => c.CustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            model.Services = await _dbContext.FilprideServices
                .Where(s => s.Company == companyClaims)
                .OrderBy(s => s.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {

                    #region --Retrieval of Customer

                    var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    #endregion --Retrieval of Customer

                    #region --Saving the default properties

                    model.ServiceInvoiceNo = await _unitOfWork.FilprideServiceInvoice.GenerateCodeAsync(companyClaims, model.Type, cancellationToken);

                    model.CreatedBy = _userManager.GetUserName(this.User);

                    model.Total = model.Amount;

                    model.Company = companyClaims;

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

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Created new service invoice# {model.ServiceInvoiceNo}", "Service Invoice", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Service invoice created successfully.";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create service invoice. Created by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return PartialView("_PreviewPartialView", soa);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Posted);

                        #region --SV Date Computation--

                        var postedDate = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                        #endregion --SV Date Computation--

                        #region --Sales Book Recording

                        decimal withHoldingTaxAmount = 0;
                        decimal withHoldingVatAmount = 0;
                        decimal netOfVatAmount = 0;
                        decimal vatAmount = 0;

                        if (model.Customer.VatType == SD.VatType_Vatable)
                        {
                            netOfVatAmount = _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(model.Total);
                            vatAmount = _unitOfWork.FilprideCreditMemo.ComputeVatAmount(netOfVatAmount);
                        }
                        else
                        {
                            netOfVatAmount = model.Total;
                        }

                        if (model.Customer.WithHoldingTax)
                        {
                            withHoldingTaxAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                        }

                        if (model.Customer.WithHoldingVat)
                        {
                            withHoldingVatAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.05m);
                        }

                        var sales = new FilprideSalesBook();

                        if (model.Customer.VatType == SD.VatType_Vatable)
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatAmount = vatAmount;
                            sales.VatableSales = netOfVatAmount;
                            sales.Discount = model.Discount;
                            sales.NetSales = netOfVatAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }
                        else if (model.Customer.VatType == SD.VatType_Exempt)
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatExemptSales = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = netOfVatAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }
                        else
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.ServiceInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.ZeroRated = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = netOfVatAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;
                        }

                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();
                        var accountTitlesDto = await _unitOfWork.FilprideServiceInvoice.GetListOfAccountTitleDto(cancellationToken);
                        var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
                        var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                        var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
                        var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");

                        //TODO waiting for Ma'am LSA journal entries
                        ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountId = arNonTradeTitle.AccountId,
                                    AccountNo = arNonTradeTitle.AccountNumber,
                                    AccountTitle = arNonTradeTitle.AccountName,
                                    Debit = Math.Round(model.Total - (withHoldingTaxAmount + withHoldingVatAmount), 4),
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate,
                                    CustomerId = model.CustomerId
                                }
                            );
                        if (withHoldingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountId = arTradeCwt.AccountId,
                                    AccountNo = arTradeCwt.AccountNumber,
                                    AccountTitle = arTradeCwt.AccountName,
                                    Debit = withHoldingTaxAmount,
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (withHoldingVatAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountId = arTradeCwv.AccountId,
                                    AccountNo = arTradeCwv.AccountNumber,
                                    AccountTitle = arTradeCwv.AccountName,
                                    Debit = withHoldingVatAmount,
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        ledgers.Add(
                               new FilprideGeneralLedgerBook
                               {
                                   Date = postedDate,
                                   Reference = model.ServiceInvoiceNo,
                                   Description = model.Service.Name,
                                   AccountNo = model.Service.CurrentAndPreviousNo,
                                   AccountTitle = model.Service.CurrentAndPreviousTitle,
                                   Debit = 0,
                                   Credit = Math.Round((netOfVatAmount), 4),
                                   Company = model.Company,
                                   CreatedBy = model.CreatedBy,
                                   CreatedDate = model.CreatedDate
                               }
                           );

                        if (vatAmount > 0)
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.ServiceInvoiceNo,
                                    Description = model.Service.Name,
                                    AccountId = vatOutputTitle.AccountId,
                                    AccountNo = vatOutputTitle.AccountNumber,
                                    AccountTitle = vatOutputTitle.AccountName,
                                    Debit = 0,
                                    Credit = Math.Round((vatAmount), 4),
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_unitOfWork.FilprideServiceInvoice.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted service invoice# {model.ServiceInvoiceNo}", "Service Invoice", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been posted.";
                        return RedirectToAction(nameof(Print), new { id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post service invoice. Posted by: {UserName}", _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideServiceInvoices.FindAsync(id, cancellationToken);

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
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled service invoice# {model.ServiceInvoiceNo}", "Service Invoice", ipAddress, model.Company);
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
                _logger.LogError(ex, "Failed to cancel service invoice. Canceled by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                var hasAlreadyBeenUsed =
                    await _dbContext.FilprideCollectionReceipts.AnyAsync(cr => cr.ServiceInvoiceId == model.ServiceInvoiceId && cr.Status != nameof(Status.Voided), cancellationToken) ||
                    await _dbContext.FilprideDebitMemos.AnyAsync(dm => dm.ServiceInvoiceId == model.ServiceInvoiceId && dm.Status != nameof(Status.Voided), cancellationToken) ||
                    await _dbContext.FilprideCreditMemos.AnyAsync(cm => cm.ServiceInvoiceId == model.ServiceInvoiceId && cm.Status != nameof(Status.Voided), cancellationToken);

                if (hasAlreadyBeenUsed)
                {
                    TempData["error"] = "Please note that this record has already been utilized in a collection receipts, debit or credit memo. As a result, voiding it is not permitted.";
                    return RedirectToAction(nameof(Index));
                }

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

                        await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideSalesBook>(gl => gl.SerialNo == model.ServiceInvoiceNo, cancellationToken);
                        await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.ServiceInvoiceNo, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided service invoice# {model.ServiceInvoiceNo}", "Service Invoice", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been voided.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to void service invoice. Voided by: {UserName}", _userManager.GetUserName(User));
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
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _unitOfWork.FilprideServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.FilprideCustomers
                .OrderBy(c => c.CustomerId)
                .Where(c => c.Company == existingModel.Company)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync(cancellationToken);
            existingModel.Services = await _dbContext.FilprideServices
                .OrderBy(s => s.ServiceId)
                .Where(s => s.Company == existingModel.Company)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideServiceInvoice model, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {

                    #region --Saving the default properties

                    existingModel.Discount = model.Discount;
                    existingModel.Amount = model.Amount;
                    existingModel.Period = model.Period;
                    existingModel.DueDate = model.DueDate;
                    existingModel.Instructions = model.Instructions;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingModel.Total = model.Amount;
                    existingModel.CustomerId = model.CustomerId;
                    existingModel.ServiceId = model.ServiceId;

                    if (DateOnly.FromDateTime(model.CreatedDate) < model.Period)
                    {
                        existingModel.UnearnedAmount += model.Amount;
                    }
                    else
                    {
                        existingModel.CurrentAndPreviousAmount += model.Amount;
                    }

                    #endregion --Saving the default properties

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited service invoice# {existingModel.ServiceInvoiceNo}", "Service Invoice", ipAddress, existingModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Service invoice updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit service invoice. Edited by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(existingModel);
                }
            }

            return View(existingModel);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var sv = await _unitOfWork.FilprideServiceInvoice.GetAsync(x => x.ServiceInvoiceId == id, cancellationToken);
            if (!sv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of service invoice# {sv.ServiceInvoiceNo}", "Service Invoice", ipAddress, sv.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                sv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.FilprideServiceInvoices
                .Where(sv => recordIds.Contains(sv.ServiceInvoiceId))
                .OrderBy(sv => sv.ServiceInvoiceNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("ServiceInvoice");

            worksheet.Cells["A1"].Value = "DueDate";
            worksheet.Cells["B1"].Value = "Period";
            worksheet.Cells["C1"].Value = "Amount";
            worksheet.Cells["D1"].Value = "Total";
            worksheet.Cells["E1"].Value = "Discount";
            worksheet.Cells["F1"].Value = "CurrentAndPreviousMonth";
            worksheet.Cells["G1"].Value = "UnearnedAmount";
            worksheet.Cells["H1"].Value = "Status";
            worksheet.Cells["I1"].Value = "AmountPaid";
            worksheet.Cells["J1"].Value = "Balance";
            worksheet.Cells["K1"].Value = "Instructions";
            worksheet.Cells["L1"].Value = "IsPaid";
            worksheet.Cells["M1"].Value = "CreatedBy";
            worksheet.Cells["N1"].Value = "CreatedDate";
            worksheet.Cells["O1"].Value = "CancellationRemarks";
            worksheet.Cells["P1"].Value = "OriginalCustomerId";
            worksheet.Cells["Q1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["R1"].Value = "OriginalServicesId";
            worksheet.Cells["S1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Period.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = item.Amount;
                worksheet.Cells[row, 4].Value = item.Total;
                worksheet.Cells[row, 5].Value = item.Discount;
                worksheet.Cells[row, 6].Value = item.CurrentAndPreviousAmount;
                worksheet.Cells[row, 7].Value = item.UnearnedAmount;
                worksheet.Cells[row, 8].Value = item.Status;
                worksheet.Cells[row, 9].Value = item.AmountPaid;
                worksheet.Cells[row, 10].Value = item.Balance;
                worksheet.Cells[row, 11].Value = item.Instructions;
                worksheet.Cells[row, 12].Value = item.IsPaid;
                worksheet.Cells[row, 13].Value = item.CreatedBy;
                worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 15].Value = item.CancellationRemarks;
                worksheet.Cells[row, 16].Value = item.CustomerId;
                worksheet.Cells[row, 17].Value = item.ServiceInvoiceNo;
                worksheet.Cells[row, 18].Value = item.ServiceId;
                worksheet.Cells[row, 19].Value = item.ServiceInvoiceId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceInvoiceList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllServiceInvoiceIds()
        {
            var svIds = _dbContext.FilprideServiceInvoices
                                     .Where(sv => sv.Type == nameof(DocumentType.Documented))
                                     .Select(sv => sv.ServiceInvoiceId) // Assuming Id is the primary key
                                     .ToList();

            return Json(svIds);
        }
    }
}
