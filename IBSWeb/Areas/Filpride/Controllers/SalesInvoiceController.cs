using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class SalesInvoiceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<SalesInvoiceController> _logger;

        public SalesInvoiceController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILogger<SalesInvoiceController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
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
            if (view == nameof(DynamicView.SalesInvoice))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.Company == companyClaims && si.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", salesInvoices);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalesInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    salesInvoices = salesInvoices
                        .Where(s =>
                            s.SalesInvoiceNo!.ToLower().Contains(searchValue) ||
                            s.Customer!.CustomerName.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerTerms.ToLower().Contains(searchValue) ||
                            s.Product!.ProductName.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.CreatedBy!.ToLower().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue) ||
                            s.Remarks.ToLower().Contains(searchValue) ||
                            s.DeliveryReceipt?.DeliveryReceiptNo.ToLower().Contains(searchValue) == true
                            )
                        .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    salesInvoices = salesInvoices
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = salesInvoices.Count();

                var pagedData = salesInvoices
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(si => new
                    {
                        si.Amount,
                        si.SalesInvoiceNo,
                        DeliveryReceiptNo = si.DeliveryReceipt?.DeliveryReceiptNo ?? "",
                        si.TransactionDate,
                        si.Customer!.CustomerName,
                        si.Terms,
                        si.Product!.ProductName,
                        si.CreatedBy,
                        si.Status,
                        si.SalesInvoiceId,
                        si.PostedBy,
                        si.AmountPaid,
                        si.VoidedBy,
                        si.CanceledBy,
                        si.PaymentStatus,

                    })
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
                _logger.LogError(ex, "Failed to get sales invoices. Error: {ErrorMessage}, Stack: {StackTrace}.",
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

            SalesInvoiceViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Customers =
                    await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region Saving Default Entries

                var model = new FilprideSalesInvoice
                {
                    SalesInvoiceNo = await _unitOfWork.FilprideSalesInvoice.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken),
                    CustomerId = viewModel.CustomerId,
                    ProductId = viewModel.ProductId,
                    OtherRefNo = viewModel.OtherRefNo,
                    Quantity = viewModel.Quantity,
                    UnitPrice = viewModel.UnitPrice,
                    Amount = viewModel.Quantity * viewModel.UnitPrice,
                    Remarks = viewModel.Remarks,
                    TransactionDate = viewModel.TransactionDate,
                    Discount = viewModel.Discount,
                    DueDate = _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(viewModel.Terms, viewModel.TransactionDate),
                    PurchaseOrderId = viewModel.PurchaseOrderId,
                    CreatedBy = _userManager.GetUserName(User),
                    Company = companyClaims,
                    Type = viewModel.Type,
                    ReceivingReportId = viewModel.ReceivingReportId,
                    CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                    DeliveryReceiptId = viewModel.DeliveryReceiptId,
                    Terms = viewModel.Terms,
                    CustomerAddress = viewModel.CustomerAddress,
                    CustomerTin = viewModel.CustomerTin,
                };

                if (model.Amount >= model.Discount)
                {
                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.FilprideSalesInvoice.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = $"Sales invoice #{model.SalesInvoiceNo} created successfully";
                    return RedirectToAction(nameof(Index));
                }

                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                TempData["warning"] = "Please input below or exact amount based on the Sales Invoice";
                return View(viewModel);

                #endregion Saving Default Entries
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            if (customer == null)
            {
                return Json(null); // Return null if no matching customer is found
            }

            return Json(new
            {
                SoldTo = customer.CustomerName,
                Address = customer.CustomerAddress,
                TinNo = customer.CustomerTin,
                customer.BusinessStyle,
                customer.CustomerType,
                customer.WithHoldingTax,
                CosList = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListPerCustomerAsync(customerId, cancellationToken)
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetProductAndDRDetails(int cosId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return Json(null);
            }
            var cos = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(c => c.CustomerOrderSlipId == cosId, cancellationToken);
            if (cos == null)
            {
                return Json(null);
            }

            return Json(new
            {
                cos.Product!.ProductId,
                ProductName = $"{cos.Product.ProductCode} {cos.Product.ProductName}",
                cos.Product.ProductUnit,
                cos.DeliveredPrice,
                cos.Terms,
                cos.CustomerAddress,
                cos.CustomerTin,
                DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListForSalesInvoice(companyClaims, cos.CustomerOrderSlipId, cancellationToken)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var existingModel = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                var viewModel = new SalesInvoiceViewModel
                {
                    SalesInvoiceId = existingModel.SalesInvoiceId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    SalesInvoiceNo = existingModel.SalesInvoiceNo,
                    BusinessStyle = existingModel.CustomerOrderSlip?.BusinessStyle,
                    CustomerId = existingModel.CustomerId,
                    ProductId = existingModel.ProductId,
                    OtherRefNo = existingModel.OtherRefNo,
                    Quantity = existingModel.Quantity,
                    UnitPrice = existingModel.UnitPrice,
                    Remarks = existingModel.Remarks,
                    TransactionDate = existingModel.TransactionDate,
                    Discount = existingModel.Discount,
                    PurchaseOrderId = existingModel.PurchaseOrderId,
                    Type = existingModel.Type,
                    ReceivingReportId = existingModel.ReceivingReportId,
                    CustomerOrderSlipId = existingModel.CustomerOrderSlipId,
                    DeliveryReceiptId = existingModel.DeliveryReceiptId,
                    Terms = existingModel.Terms,
                    CustomerAddress = existingModel.CustomerAddress,
                    CustomerTin = existingModel.CustomerTin,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalesInvoiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Customers =
                    await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == viewModel.SalesInvoiceId, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                existingRecord.CustomerId = viewModel.CustomerId;
                existingRecord.TransactionDate = viewModel.TransactionDate;
                existingRecord.OtherRefNo = viewModel.OtherRefNo;
                existingRecord.PurchaseOrderId = viewModel.PurchaseOrderId;
                existingRecord.Quantity = viewModel.Quantity;
                existingRecord.UnitPrice = viewModel.UnitPrice;
                existingRecord.Remarks = viewModel.Remarks;
                existingRecord.Discount = viewModel.Discount;
                existingRecord.Amount = viewModel.Quantity * viewModel.UnitPrice;
                existingRecord.ProductId = viewModel.ProductId;
                existingRecord.ReceivingReportId = viewModel.ReceivingReportId;
                existingRecord.CustomerOrderSlipId = viewModel.CustomerOrderSlipId;
                existingRecord.DeliveryReceiptId = viewModel.DeliveryReceiptId;
                existingRecord.Terms = viewModel.Terms;
                existingRecord.DueDate = _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingRecord.Terms, viewModel.TransactionDate);
                existingRecord.CustomerAddress = viewModel.CustomerAddress;
                existingRecord.CustomerTin = viewModel.CustomerTin;

                existingRecord.EditedBy = _userManager.GetUserName(User);
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edited sales invoice# {existingRecord.SalesInvoiceNo}", "Sales Invoice", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sales invoice updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var sales = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (sales == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview sales invoice print layout sales invoice#{sales.SalesInvoiceNo}", "Sales Invoice", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(sales);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(s => s.SalesInvoiceId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = _userManager.GetUserName(this.User);
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);

                #region--DR process

                if (model.DeliveryReceiptId != null)
                {
                    var existingDr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == model.DeliveryReceiptId, cancellationToken) ?? throw new ArgumentNullException($"The DR#{model.DeliveryReceiptId} not found! Contact MIS Enterprise.");

                    existingDr.HasAlreadyInvoiced = true;
                    existingDr.Status = nameof(DRStatus.Invoiced);
                }

                #endregion

                await _unitOfWork.FilprideSalesInvoice.PostAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sales Invoice has been Posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var existingInventory = await _unitOfWork.FilprideInventory
                .GetAsync(i => i.Reference == model.SalesInvoiceNo && i.Company == model.Company, cancellationToken);

            var hasAlreadyBeenUsed =
                await _dbContext.FilprideCollectionReceipts.AnyAsync(cr => cr.SalesInvoiceId == model.SalesInvoiceId && cr.Status != nameof(Status.Voided), cancellationToken) ||
                await _dbContext.FilprideDebitMemos.AnyAsync(dm => dm.SalesInvoiceId == model.SalesInvoiceId && dm.Status != nameof(Status.Voided), cancellationToken) ||
                await _dbContext.FilprideCreditMemos.AnyAsync(cm => cm.SalesInvoiceId == model.SalesInvoiceId && cm.Status != nameof(Status.Voided), cancellationToken);

            if (hasAlreadyBeenUsed)
            {
                TempData["info"] = "Please note that this record has already been utilized in collection receipts, debit or credit memo. As a result, voiding it is not permitted.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.VoidedBy = _userManager.GetUserName(this.User);
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Voided);

                await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideSalesBook>(sb => sb.SerialNo == model.SalesInvoiceNo, cancellationToken);
                await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.SalesInvoiceNo, cancellationToken);

                if (existingInventory != null)
                {
                    await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                }

                var dr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(d => d.HasAlreadyInvoiced && d.DeliveryReceiptId == model.DeliveryReceiptId);

                if (dr != null)
                {
                    dr.HasAlreadyInvoiced = false;
                    dr.Status = nameof(DRStatus.ForInvoicing);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sales Invoice has been Voided.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = _userManager.GetUserName(this.User);
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.PaymentStatus = nameof(Status.Canceled);
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Sales Invoice has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel sales invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GetPOs(int productId)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetAllAsync(po =>
                    po.Company == companyClaims && po.ProductId == productId && po.QuantityReceived != 0 &&
                    po.PostedBy != null);

            if (purchaseOrders.Any())
            {
                var poList = purchaseOrders.Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var si = await _unitOfWork.FilprideSalesInvoice.GetAsync(x => x.SalesInvoiceId == id, cancellationToken);

            if (si == null)
            {
                return NotFound();
            }

            if (!si.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of sales invoice# {si.SalesInvoiceNo}", "Sales Invoice", si.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                si.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed re-printed copy of sales invoice# {si.SalesInvoiceNo}", "Sales Invoice", si.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> GetDrDetails(int? drId, CancellationToken cancellationToken)
        {
            var dr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(d => d.DeliveryReceiptId == drId, cancellationToken);

            if (dr == null)
            {
                return Json(null);
            }

            var automatedRr = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.DeliveryReceiptId == dr.DeliveryReceiptId && rr.Status == nameof(Status.Posted), cancellationToken);

            int receivingReportId = 0;

            if (automatedRr != null)

            {
                receivingReportId = automatedRr.ReceivingReportId;
            }

            return Json(new
            {
                TransactionDate = dr.DeliveredDate,
                dr.Quantity,
                receivingReportId,
                dr.PurchaseOrderId,
                OtherRefNo = dr.ManualDrNo,
                Remarks = $"Customer PO# {dr.CustomerOrderSlip!.CustomerPoNo}" +
                          (!dr.Customer!.HasBranch ? "" : $"\nBranch: {dr.CustomerOrderSlip.Branch}")
            });

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

            var selectedList = await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(invoice => recordIds.Contains(invoice.SalesInvoiceId));

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("SalesInvoice");

            worksheet.Cells["A1"].Value = "OtherRefNo";
            worksheet.Cells["B1"].Value = "Quantity";
            worksheet.Cells["C1"].Value = "UnitPrice";
            worksheet.Cells["D1"].Value = "Amount";
            worksheet.Cells["E1"].Value = "Remarks";
            worksheet.Cells["F1"].Value = "Status";
            worksheet.Cells["G1"].Value = "TransactionDate";
            worksheet.Cells["H1"].Value = "Discount";
            worksheet.Cells["I1"].Value = "AmountPaid";
            worksheet.Cells["J1"].Value = "Balance";
            worksheet.Cells["K1"].Value = "IsPaid";
            worksheet.Cells["L1"].Value = "IsTaxAndVatPaid";
            worksheet.Cells["M1"].Value = "DueDate";
            worksheet.Cells["N1"].Value = "CreatedBy";
            worksheet.Cells["O1"].Value = "CreatedDate";
            worksheet.Cells["P1"].Value = "CancellationRemarks";
            worksheet.Cells["Q1"].Value = "OriginalReceivingReportId";
            worksheet.Cells["R1"].Value = "OriginalCustomerId";
            worksheet.Cells["S1"].Value = "OriginalPOId";
            worksheet.Cells["T1"].Value = "OriginalProductId";
            worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["V1"].Value = "OriginalDocumentId";
            worksheet.Cells["W1"].Value = "PostedBy";
            worksheet.Cells["X1"].Value = "PostedDate";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.OtherRefNo;
                worksheet.Cells[row, 2].Value = item.Quantity;
                worksheet.Cells[row, 3].Value = item.UnitPrice;
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 5].Value = item.Remarks;
                worksheet.Cells[row, 6].Value = item.Status;
                worksheet.Cells[row, 7].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = item.Discount;
                worksheet.Cells[row, 9].Value = item.AmountPaid;
                worksheet.Cells[row, 10].Value = item.Balance;
                worksheet.Cells[row, 11].Value = item.IsPaid;
                worksheet.Cells[row, 12].Value = item.IsTaxAndVatPaid;
                worksheet.Cells[row, 13].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 14].Value = item.CreatedBy;
                worksheet.Cells[row, 15].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 16].Value = item.CancellationRemarks;
                worksheet.Cells[row, 17].Value = item.ReceivingReportId;
                worksheet.Cells[row, 18].Value = item.CustomerId;
                worksheet.Cells[row, 19].Value = item.PurchaseOrderId;
                worksheet.Cells[row, 20].Value = item.ProductId;
                worksheet.Cells[row, 21].Value = item.SalesInvoiceNo;
                worksheet.Cells[row, 22].Value = item.SalesInvoiceId;
                worksheet.Cells[row, 23].Value = item.PostedBy;
                worksheet.Cells[row, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SalesInvoiceList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllSalesInvoiceIds()
        {
            var invoiceIds = _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(invoice => invoice.Type == nameof(DocumentType.Documented))
                .Result
                .Select(invoice => invoice.SalesInvoiceId);

            return Json(invoiceIds);
        }
    }
}
