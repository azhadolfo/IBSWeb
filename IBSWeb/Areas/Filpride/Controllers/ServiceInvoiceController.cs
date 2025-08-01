using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using IBS.Models.Filpride.ViewModels;
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

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ServiceInvoiceController> _logger;

        public ServiceInvoiceController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<ServiceInvoiceController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetServiceInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var serviceInvoices = await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(sv => sv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    serviceInvoices = serviceInvoices
                        .Where(s =>
                            s.ServiceInvoiceNo.ToLower().Contains(searchValue) ||
                            s.CustomerName.ToLower().Contains(searchValue) ||
                            s.ServiceName.ToLower().Contains(searchValue) ||
                            s.Period.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Total.ToString().Contains(searchValue) ||
                            s.Instructions.ToLower().Contains(searchValue) ||
                            s.CreatedBy?.ToLower().Contains(searchValue) == true
                            )
                        .ToList();
                }

                // Sorting
                if (parameters.Order.Count > 0)
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
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var viewModel = new ServiceInvoiceViewModel
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                Services = await _unitOfWork.GetFilprideServiceListById(companyClaims, cancellationToken)

            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.Services = await _unitOfWork.GetFilprideServiceListById(companyClaims, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Retrieval of Customer/Service

                var customer = await _unitOfWork.FilprideCustomer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken);

                var service = await _unitOfWork.FilprideService
                    .GetAsync(c => c.ServiceId == viewModel.ServiceId, cancellationToken);

                if (customer == null || service == null)
                {
                    return NotFound();
                }

                #endregion --Retrieval of Customer/Service

                var model = new FilprideServiceInvoice
                {
                    ServiceInvoiceNo = await _unitOfWork.FilprideServiceInvoice.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken),
                    ServiceId = service.ServiceId,
                    ServiceName = service.Name,
                    ServicePercent = service.Percent,
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    CustomerAddress = customer.CustomerAddress,
                    CustomerBusinessStyle = customer.BusinessStyle,
                    CustomerTin = customer.CustomerTin,
                    VatType = customer.VatType,
                    HasEwt = customer.WithHoldingTax,
                    HasWvat = customer.WithHoldingVat,
                    CreatedBy = User.Identity!.Name,
                    Total = viewModel.Total,
                    Balance = viewModel.Total,
                    Company = companyClaims,
                    Period = viewModel.Period,
                    Instructions = viewModel.Instructions,
                    DueDate = viewModel.DueDate,
                    Discount = viewModel.Discount,
                    Type = viewModel.Type,
                };

                #region --Additional procedure for Transaction Fee

                if (viewModel.DeliveryReceiptId != null)
                {
                    model.DeliveryReceiptId = await ProcessTheDrForTransactionFee(viewModel.DeliveryReceiptId, cancellationToken);
                }

                #endregion

                await _unitOfWork.FilprideServiceInvoice.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Created new service invoice# {model.ServiceInvoiceNo}", "Service Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Service invoice #{model.ServiceInvoiceNo} created successfully.";
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var soa = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model == null)
                {
                    return NotFound();
                }

                model.PostedBy = _userManager.GetUserName(this.User);
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);

                #region --SV Date Computation--

                var postedDate = DateOnly.FromDateTime(model.CreatedDate) >= model.Period
                    ? DateOnly.FromDateTime(model.CreatedDate)
                    : model.Period.AddMonths(1).AddDays(-1);

                #endregion --SV Date Computation--

                #region --Sales Book Recording

                decimal withHoldingTaxAmount = 0;
                decimal withHoldingVatAmount = 0;
                decimal netOfVatAmount;
                decimal vatAmount = 0;

                if (model.VatType == SD.VatType_Vatable)
                {
                    netOfVatAmount = _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(model.Total);
                    vatAmount = _unitOfWork.FilprideCreditMemo.ComputeVatAmount(netOfVatAmount);
                }
                else
                {
                    netOfVatAmount = model.Total;
                }

                if (model.HasEwt)
                {
                    withHoldingTaxAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                }

                if (model.HasWvat)
                {
                    withHoldingVatAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.05m);
                }

                var sales = new FilprideSalesBook
                {
                    TransactionDate = postedDate,
                    SerialNo = model.ServiceInvoiceNo,
                    SoldTo = model.CustomerName,
                    TinNo = model.CustomerTin,
                    Address = model.CustomerAddress,
                    Description = model.ServiceName,
                    Amount = model.Total,
                    VatAmount = vatAmount,
                    VatableSales = netOfVatAmount,
                    Discount = model.Discount,
                    NetSales = netOfVatAmount,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate,
                    DueDate = model.DueDate,
                    DocumentId = model.ServiceInvoiceId,
                    Company = model.Company,
                };

                switch (model.VatType)
                {
                    case SD.VatType_Vatable:
                        sales.VatAmount = vatAmount;
                        sales.VatableSales = netOfVatAmount;
                        break;
                    case SD.VatType_Exempt:
                        sales.VatExemptSales = model.Total;
                        break;
                    default:
                        sales.ZeroRated = model.Total;
                        break;
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

                ///TODO waiting for Ma'am LSA journal entries
                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = postedDate,
                        Reference = model.ServiceInvoiceNo,
                        Description = model.ServiceName,
                        AccountId = arNonTradeTitle.AccountId,
                        AccountNo = arNonTradeTitle.AccountNumber,
                        AccountTitle = arNonTradeTitle.AccountName,
                        Debit = model.Total - (withHoldingTaxAmount + withHoldingVatAmount),
                        Credit = 0,
                        Company = model.Company,
                        CreatedBy = model.PostedBy,
                        CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                        CustomerId = model.CustomerId,
                        CustomerName = model.CustomerName
                    }
                );
                if (withHoldingTaxAmount > 0)
                {
                    ledgers.Add(
                        new FilprideGeneralLedgerBook
                        {
                            Date = postedDate,
                            Reference = model.ServiceInvoiceNo,
                            Description = model.ServiceName,
                            AccountId = arTradeCwt.AccountId,
                            AccountNo = arTradeCwt.AccountNumber,
                            AccountTitle = arTradeCwt.AccountName,
                            Debit = withHoldingTaxAmount,
                            Credit = 0,
                            Company = model.Company,
                            CreatedBy = model.PostedBy,
                            CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
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
                            Description = model.ServiceName,
                            AccountId = arTradeCwv.AccountId,
                            AccountNo = arTradeCwv.AccountNumber,
                            AccountTitle = arTradeCwv.AccountName,
                            Debit = withHoldingVatAmount,
                            Credit = 0,
                            Company = model.Company,
                            CreatedBy = model.PostedBy,
                            CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                        }
                    );
                }

                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = postedDate,
                        Reference = model.ServiceInvoiceNo,
                        Description = model.ServiceName,
                        AccountNo = model.Service!.CurrentAndPreviousNo!,
                        AccountTitle = model.Service!.CurrentAndPreviousTitle!,
                        Debit = 0,
                        Credit = netOfVatAmount,
                        Company = model.Company,
                        CreatedBy = model.PostedBy,
                        CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                    }
                );

                if (vatAmount > 0)
                {
                    ledgers.Add(
                        new FilprideGeneralLedgerBook
                        {
                            Date = postedDate,
                            Reference = model.ServiceInvoiceNo,
                            Description = model.ServiceName,
                            AccountId = vatOutputTitle.AccountId,
                            AccountNo = vatOutputTitle.AccountNumber,
                            AccountTitle = vatOutputTitle.AccountName,
                            Debit = 0,
                            Credit = vatAmount,
                            Company = model.Company,
                            CreatedBy = model.PostedBy,
                            CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
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

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted service invoice# {model.ServiceInvoiceNo}", "Service Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Service invoice has been posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice.GetAsync(x => x.ServiceInvoiceId == id, cancellationToken);


            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = _userManager.GetUserName(this.User);
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                if (model.DeliveryReceiptId != null)
                {
                    await ReverseTheProcessedDr(model.DeliveryReceiptId, cancellationToken);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled service invoice# {model.ServiceInvoiceNo}", "Service Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Service invoice has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice.GetAsync(x => x.ServiceInvoiceId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var hasAlreadyBeenUsed =
                await _dbContext.FilprideCollectionReceipts.AnyAsync(cr => cr.ServiceInvoiceId == model.ServiceInvoiceId && cr.Status != nameof(Status.Voided), cancellationToken) ||
                await _dbContext.FilprideDebitMemos.AnyAsync(dm => dm.ServiceInvoiceId == model.ServiceInvoiceId && dm.Status != nameof(Status.Voided), cancellationToken) ||
                await _dbContext.FilprideCreditMemos.AnyAsync(cm => cm.ServiceInvoiceId == model.ServiceInvoiceId && cm.Status != nameof(Status.Voided), cancellationToken);

            if (hasAlreadyBeenUsed)
            {
                TempData["info"] = "Please note that this record has already been utilized in a collection receipts, debit or credit memo. As a result, voiding it is not permitted.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.VoidedBy = _userManager.GetUserName(this.User);
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Voided);

                if (model.DeliveryReceiptId != null)
                {
                    await ReverseTheProcessedDr(model.DeliveryReceiptId, cancellationToken);
                }

                await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideSalesBook>(gl => gl.SerialNo == model.ServiceInvoiceNo, cancellationToken);
                await _unitOfWork.FilprideServiceInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.ServiceInvoiceNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided service invoice# {model.ServiceInvoiceNo}", "Service Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var existingModel = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(sv => sv.ServiceInvoiceId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var viewModel = new ServiceInvoiceViewModel
            {
                ServiceInvoiceId = existingModel.ServiceInvoiceId,
                CustomerId = existingModel.CustomerId,
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                ServiceId = existingModel.ServiceId,
                Services = await _unitOfWork.GetFilprideServiceListById(companyClaims, cancellationToken),
                DueDate = existingModel.DueDate,
                Instructions = existingModel.Instructions,
                Period = existingModel.Period,
                Total = existingModel.Total,
                DeliveryReceiptId = existingModel.DeliveryReceiptId,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(s => s.ServiceInvoiceId == viewModel.ServiceInvoiceId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(existingModel.Company, cancellationToken);
            viewModel.Services = await _unitOfWork.GetFilprideServiceListById(existingModel.Company, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await _unitOfWork.FilprideCustomer
                    .GetAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken);

                var service = await _unitOfWork.FilprideService
                    .GetAsync(c => c.ServiceId == viewModel.ServiceId, cancellationToken);

                if (customer == null || service == null)
                {
                    return NotFound();
                }

                #region --Saving the default properties

                existingModel.Discount = viewModel.Discount;
                existingModel.Total = viewModel.Total;
                existingModel.Balance = viewModel.Total;
                existingModel.Period = viewModel.Period;
                existingModel.DueDate = viewModel.DueDate;
                existingModel.Instructions = viewModel.Instructions;
                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingModel.Total = viewModel.Total;
                existingModel.CustomerId = viewModel.CustomerId;
                existingModel.ServiceId = viewModel.ServiceId;
                existingModel.ServiceName = service.Name;
                existingModel.ServicePercent = service.Percent;
                existingModel.CustomerName = customer.CustomerName;
                existingModel.CustomerBusinessStyle = customer.BusinessStyle;
                existingModel.CustomerAddress = customer.CustomerAddress;
                existingModel.CustomerTin = customer.CustomerTin;
                existingModel.VatType = customer.VatType;
                existingModel.HasEwt = customer.WithHoldingTax;
                existingModel.HasWvat = customer.WithHoldingVat;

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited service invoice# {existingModel.ServiceInvoiceNo}", "Service Invoice", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                #region --Additional procedure for Transaction Fee

                if (existingModel.DeliveryReceiptId != viewModel.DeliveryReceiptId && existingModel.DeliveryReceipt != null)
                {
                    await ReverseTheProcessedDr(existingModel.DeliveryReceiptId, cancellationToken);
                }

                if (viewModel.DeliveryReceiptId != null)
                {
                    existingModel.DeliveryReceiptId = await ProcessTheDrForTransactionFee(viewModel.DeliveryReceiptId, cancellationToken);
                }

                #endregion

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Service invoice updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit service invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var sv = await _unitOfWork.FilprideServiceInvoice
                .GetAsync(x => x.ServiceInvoiceId == id, cancellationToken);

            if (sv == null)
            {
                return NotFound();
            }

            if (!sv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of service invoice# {sv.ServiceInvoiceNo}", "Service Invoice", sv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

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
            var selectedList = _unitOfWork.FilprideServiceInvoice
                .GetAllAsync(sv => recordIds.Contains(sv.ServiceInvoiceId))
                .Result
                .OrderBy(sv => sv.ServiceInvoiceNo);

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
            worksheet.Cells["T1"].Value = "PostedBy";
            worksheet.Cells["U1"].Value = "PostedDate";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Period.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = item.Total;
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
                worksheet.Cells[row, 20].Value = item.PostedBy;
                worksheet.Cells[row, 21].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ServiceInvoiceList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllServiceInvoiceIds()
        {
            var svIds = _unitOfWork.FilprideServiceInvoice
                                     .GetAllAsync(sv => sv.Type == nameof(DocumentType.Documented))
                                     .Result
                                     .Select(sv => sv.ServiceInvoiceId);

            return Json(svIds);
        }

        [HttpGet]
        public async Task<IActionResult> GetDRsByCustomer(int customerId, int previousSelectedDr)
        {
            var drs = await _unitOfWork.FilprideDeliveryReceipt
                .GetAllAsync(x => x.CustomerId == customerId
                                  && !x.HasAlreadyInvoiced
                                  || x.DeliveryReceiptId == previousSelectedDr);

            var result = new List<object>();

            foreach (var dr in drs)
            {
                decimal cosPrice = dr.CustomerOrderSlip!.DeliveredPrice;
                decimal cost = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost((int)dr.PurchaseOrderId!);
                decimal freight = dr.Freight;
                decimal commission = dr.CommissionRate;
                decimal grossMargin = cosPrice - (cost + freight + commission);
                decimal total = dr.Quantity * grossMargin;

                result.Add(new
                {
                    value = dr.DeliveryReceiptId.ToString(),
                    text = $"{dr.DeliveryReceiptNo} ({total:N2})",
                    grossTotal = total
                });
            }

            return Json(result);
        }

        private async Task<int> ProcessTheDrForTransactionFee(int? deliveryReceiptId,
            CancellationToken cancellationToken)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(x => x.DeliveryReceiptId == deliveryReceiptId, cancellationToken);

            if (deliveryReceipt == null)
            {
                throw new NullReferenceException("DR not found!");
            }

            deliveryReceipt.HasAlreadyInvoiced = true;
            deliveryReceipt.Status = nameof(DRStatus.Invoiced);

            #region Reverse the DR related

            var drGl = await _dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Reference == deliveryReceipt.DeliveryReceiptNo
                            && x.Company == deliveryReceipt.Company)
                .ToListAsync(cancellationToken);

            foreach (var dr in drGl)
            {
                (dr.Debit, dr.Credit) = (dr.Credit, dr.Debit);
            }

            #endregion

            return deliveryReceipt.DeliveryReceiptId;
        }

        private async Task ReverseTheProcessedDr(int? previousDeliveryReceiptId,
            CancellationToken cancellationToken)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(x => x.DeliveryReceiptId == previousDeliveryReceiptId, cancellationToken);

            if (deliveryReceipt == null)
            {
                throw new NullReferenceException("DR not found!");
            }

            deliveryReceipt.HasAlreadyInvoiced = false;
            deliveryReceipt.Status = nameof(DRStatus.ForInvoicing);

            #region Reverse the DR related

            var drGl = await _dbContext.FilprideGeneralLedgerBooks
                .Where(x => x.Reference == deliveryReceipt.DeliveryReceiptNo
                            && x.Company == deliveryReceipt.Company)
                .ToListAsync(cancellationToken);

            foreach (var dr in drGl)
            {
                (dr.Credit, dr.Debit) = (dr.Debit, dr.Credit);
            }

            #endregion
        }

    }
}
