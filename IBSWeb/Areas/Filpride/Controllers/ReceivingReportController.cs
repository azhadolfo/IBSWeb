using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
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
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD, SD.Department_CreditAndCollection)]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ReceivingReportController> _logger;

        private const string FilterTypeClaimType = "ReceivingReport.FilterType";

        public ReceivingReportController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<ReceivingReportController> logger)
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
            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
        }

        public async Task<IActionResult> Index(string? view, string filterType, CancellationToken cancellationToken)
        {
            await UpdateFilterTypeClaim(filterType);
            if (view != nameof(DynamicView.ReceivingReport))
            {
                return View();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var receivingReports = await _unitOfWork.FilprideReceivingReport
                .GetAllAsync(rr => rr.Company == companyClaims && rr.Type == nameof(DocumentType.Documented), cancellationToken);

            return View("ExportIndex", receivingReports);

            //For the function of correcting the journal entries
            // var receivingReportss = await _unitOfWork.FilprideReceivingReport
            //     .GetAllAsync();
            //
            // foreach (var receivingReports in receivingReportss)
            // {
            //     await Void(receivingReports.ReceivingReportId, cancellationToken);
            //     await Post(receivingReports.ReceivingReportId, cancellationToken);
            // }


        }

        [HttpPost]
        public async Task<IActionResult> GetReceivingReports([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var receivingReports = await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.Company == companyClaims, cancellationToken);

                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "RecordLiftingDate":
                            receivingReports = receivingReports
                                .Where(rr => rr.SupplierInvoiceDate == null);
                            break;
                            // Add other cases as needed
                    }
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    receivingReports = receivingReports
                    .Where(s =>
                        s.ReceivingReportNo!.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder!.PurchaseOrderNo!.ToLower().Contains(searchValue) ||
                        s.DeliveryReceipt?.DeliveryReceiptNo.ToLower().Contains(searchValue) == true ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.QuantityReceived.ToString().Contains(searchValue) ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue) ||
                        s.Remarks.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    receivingReports = receivingReports
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = receivingReports.Count();

                var pagedData = receivingReports
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(rr => new
                    {
                        rr.ReceivingReportId,
                        rr.ReceivingReportNo,
                        rr.Date,
                        rr.PurchaseOrder!.PurchaseOrderNo,
                        rr.PurchaseOrder.OldPoNo,
                        rr.OldRRNo,
                        rr.DeliveryReceiptId,
                        rr.DeliveryReceipt?.DeliveryReceiptNo,
                        rr.DeliveryReceipt?.Customer?.CustomerName,
                        rr.PurchaseOrder!.Product!.ProductName,
                        rr.QuantityReceived,
                        rr.CreatedBy,
                        rr.Status,
                        rr.VoidedBy,
                        rr.PostedBy,
                        rr.CanceledBy,
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
                _logger.LogError(ex, "Failed to get receiving reports. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ReceivingReportViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Retrieve PO

                var existingPo = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                if (existingPo == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve PO

                var totalAmountRr = existingPo.Quantity - existingPo.QuantityReceived;

                if (viewModel.QuantityDelivered > totalAmountRr)
                {
                    TempData["info"] = "Input is exceed to remaining quantity delivered";
                    return View(viewModel);
                }

                var model = new FilprideReceivingReport
                {
                    ReceivingReportNo = await _unitOfWork.FilprideReceivingReport.GenerateCodeAsync(companyClaims, existingPo.Type!, cancellationToken),
                    Date = viewModel.Date,
                    DueDate = await _unitOfWork.FilprideReceivingReport.ComputeDueDateAsync(viewModel.PurchaseOrderId, viewModel.Date, cancellationToken),
                    POId = existingPo.PurchaseOrderId,
                    PONo = existingPo.PurchaseOrderNo,
                    SupplierInvoiceNumber = viewModel.SupplierSiNo,
                    SupplierInvoiceDate = viewModel.SupplierSiDate,
                    TruckOrVessels = viewModel.TruckOrVessels,
                    QuantityReceived = viewModel.QuantityReceived,
                    QuantityDelivered = viewModel.QuantityDelivered,
                    GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered,
                    Amount = viewModel.QuantityReceived * await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(existingPo.PurchaseOrderId, cancellationToken),
                    AuthorityToLoadNo = viewModel.AuthorityToLoadNo,
                    Remarks = viewModel.Remarks,
                    CreatedBy = User.Identity!.Name,
                    Company = companyClaims,
                    ReceivedDate = viewModel.ReceivedDate,
                    SupplierDrNo = viewModel.SupplierDrNo,
                    WithdrawalCertificate = viewModel.WithdrawalCertificate,
                    Type = existingPo.Type,
                    OldRRNo = viewModel.OldRRNo,
                };

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.FilprideReceivingReport.AddAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Receiving Report #{model.ReceivingReportNo} created successfully";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReport
                .GetAsync(x => x.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            var viewModel = new ReceivingReportViewModel
            {
                ReceivingReportId = receivingReport.ReceivingReportId,
                Date = receivingReport.Date,
                PurchaseOrderId = receivingReport.POId,
                PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                    .GetPurchaseOrderListAsyncById(companyClaims, cancellationToken),
                ReceivedDate = receivingReport.ReceivedDate,
                SupplierSiNo = receivingReport.SupplierInvoiceNumber,
                SupplierSiDate = receivingReport.SupplierInvoiceDate,
                SupplierDrNo = receivingReport.SupplierDrNo,
                WithdrawalCertificate = receivingReport.WithdrawalCertificate,
                TruckOrVessels = receivingReport.TruckOrVessels,
                QuantityDelivered = receivingReport.QuantityDelivered,
                QuantityReceived = receivingReport.QuantityReceived,
                AuthorityToLoadNo = receivingReport.AuthorityToLoadNo,
                Remarks = receivingReport.Remarks,
                PostedBy = receivingReport.PostedBy,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideReceivingReport
                .GetAsync(x => x.ReceivingReportId == viewModel.ReceivingReportId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {

                #region --Retrieve PO

                var po = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(x => x.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                if (po == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve PO

                var totalAmountRr = po.Quantity - po.QuantityReceived;

                if (viewModel.QuantityDelivered > totalAmountRr && existingModel.PostedBy == null)
                {
                    TempData["info"] = "Input is exceed to remaining quantity delivered";
                    return View(viewModel);
                }

                existingModel.Date = viewModel.Date;
                existingModel.POId = po.PurchaseOrderId;
                existingModel.PONo = po.PurchaseOrderNo;
                existingModel.DueDate = await _unitOfWork.FilprideReceivingReport.ComputeDueDateAsync(po.PurchaseOrderId, viewModel.Date, cancellationToken);
                existingModel.SupplierInvoiceNumber = viewModel.SupplierSiNo;
                existingModel.SupplierInvoiceDate = viewModel.SupplierSiDate;
                existingModel.SupplierDrNo = viewModel.SupplierDrNo;
                existingModel.WithdrawalCertificate = viewModel.WithdrawalCertificate;
                existingModel.TruckOrVessels = viewModel.TruckOrVessels;
                existingModel.QuantityDelivered = viewModel.QuantityDelivered;
                existingModel.QuantityReceived = viewModel.QuantityReceived;
                existingModel.GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered;
                existingModel.AuthorityToLoadNo = viewModel.AuthorityToLoadNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.ReceivedDate = viewModel.ReceivedDate;
                existingModel.Amount = viewModel.QuantityReceived * await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(po.PurchaseOrderId, cancellationToken);
                existingModel.OldRRNo = viewModel.OldRRNo;

                if (!_dbContext.ChangeTracker.HasChanges())
                {
                    TempData["warning"] = "No data changes!";
                    return View(viewModel);
                }

                existingModel.EditedBy = User.Identity!.Name;
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited receiving report# {existingModel.ReceivingReportNo}", "Receiving Report", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Receiving Report updated successfully";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var receivingReport = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview receiving report# {receivingReport.ReceivingReportNo}", "Purchase Order", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(receivingReport);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model.ReceivedDate == null)
                {
                    TempData["info"] = "Please indicate the received date.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }

                model.PostedBy = User.Identity!.Name;
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.FilprideReceivingReport.PostAsync(model, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Receiving Report has been posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to post receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.ReceivingReportNo
                                          && i.Company == model.Company, cancellationToken: cancellationToken);

            if (existingInventory == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var connectedSi = await _unitOfWork.FilprideSalesInvoice
                .GetAsync(x => x.ReceivingReportId == id, cancellationToken);

            if (connectedSi != null)
            {
                connectedSi.ReceivingReportId = 0;
            }

            try
            {
                model.VoidedBy = _userManager.GetUserName(this.User);
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Voided);
                model.PostedBy = null;
                model.DeliveryReceipt!.HasReceivingReport = false;

                await _unitOfWork.FilprideReceivingReport.RemoveRecords<FilpridePurchaseBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
                await _unitOfWork.FilprideReceivingReport.RemoveRecords<FilprideGeneralLedgerBook>(pb => pb.Reference == model.ReceivingReportNo, cancellationToken);
                await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                await _unitOfWork.FilprideReceivingReport.RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Receiving Report has been voided.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = _userManager.GetUserName(this.User);
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled receiving report# {model.ReceivingReportNo}", "Receiving Report", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Receiving Report has been canceled.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po == null)
            {
                return NotFound();
            }

            var receivingReports = await _unitOfWork
                .FilprideReceivingReport
                .GetAllAsync(x => x.Company == po.Company
                                   && x.PONo == po.PurchaseOrderNo, cancellationToken);

            var rrList = receivingReports
                .Select(rr => new
                {
                    rr.ReceivingReportNo,
                    rr.QuantityReceived,
                    rr.QuantityDelivered,
                    rr.Company,
                    rr.PONo,
                    rr.Status
                })
                .ToList();

            var rrPostedOnly = rrList
                .Where(rr => rr.Company == po.Company
                                   && rr.PONo == po.PurchaseOrderNo
                                   && rr.Status == nameof(Status.Posted))
                .ToList();



            var rrNotPosted = rrList
                .Where(rr => rr.Company == po.Company
                                                    && rr.PONo == po.PurchaseOrderNo
                                                    && rr.Status == nameof(Status.Pending))
                .ToList();

            var rrCanceled = rrList
                .Where(rr => rr.Company == po.Company
                             && rr.PONo == po.PurchaseOrderNo
                             && (rr.Status == nameof(Status.Canceled)
                                 || rr.Status == nameof(Status.Voided)))
                .ToList();

            return Json(new
            {
                poNo = po.PurchaseOrderNo,
                poQuantity = po.Quantity.ToString(SD.Two_Decimal_Format),
                rrList,
                rrListPostedOnly = rrPostedOnly,
                rrListNotPosted = rrNotPosted,
                rrListCanceled = rrCanceled
            });
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var rr = await _unitOfWork.FilprideReceivingReport
                .GetAsync(x => x.ReceivingReportId == id, cancellationToken);

            if (rr == null)
            {
                return NotFound();
            }

            if (!rr.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of receiving report# {rr.ReceivingReportNo}", "Receiving Report", rr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                rr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed re-printed copy of receiving report# {rr.ReceivingReportNo}", "Receiving Report", rr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording
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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected records from the database
            var selectedList = await _dbContext.FilprideReceivingReports
                .Where(rr => recordIds.Contains(rr.ReceivingReportId))
                .Include(rr => rr.PurchaseOrder)
                .OrderBy(rr => rr.ReceivingReportNo)
                .ToListAsync();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                #region -- Purchase Order Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("PurchaseOrder");

                worksheet2.Cells["A1"].Value = "Date";
                worksheet2.Cells["B1"].Value = "Terms";
                worksheet2.Cells["C1"].Value = "Quantity";
                worksheet2.Cells["D1"].Value = "Price";
                worksheet2.Cells["E1"].Value = "Amount";
                worksheet2.Cells["F1"].Value = "FinalPrice";
                worksheet2.Cells["G1"].Value = "QuantityReceived";
                worksheet2.Cells["H1"].Value = "IsReceived";
                worksheet2.Cells["I1"].Value = "ReceivedDate";
                worksheet2.Cells["J1"].Value = "Remarks";
                worksheet2.Cells["K1"].Value = "CreatedBy";
                worksheet2.Cells["L1"].Value = "CreatedDate";
                worksheet2.Cells["M1"].Value = "IsClosed";
                worksheet2.Cells["N1"].Value = "CancellationRemarks";
                worksheet2.Cells["O1"].Value = "OriginalProductId";
                worksheet2.Cells["P1"].Value = "OriginalSeriesNumber";
                worksheet2.Cells["Q1"].Value = "OriginalSupplierId";
                worksheet2.Cells["R1"].Value = "OriginalDocumentId";
                worksheet2.Cells["S1"].Value = "PostedBy";
                worksheet2.Cells["T1"].Value = "PostedDate";

                #endregion -- Purchase Order Table Header --

                #region -- Receving Report Table Header --

                var worksheet = package.Workbook.Worksheets.Add("ReceivingReport");

                worksheet.Cells["A1"].Value = "Date";
                worksheet.Cells["B1"].Value = "DueDate";
                worksheet.Cells["C1"].Value = "SupplierInvoiceNumber";
                worksheet.Cells["D1"].Value = "SupplierInvoiceDate";
                worksheet.Cells["E1"].Value = "TruckOrVessels";
                worksheet.Cells["F1"].Value = "QuantityDelivered";
                worksheet.Cells["G1"].Value = "QuantityReceived";
                worksheet.Cells["H1"].Value = "GainOrLoss";
                worksheet.Cells["I1"].Value = "Amount";
                worksheet.Cells["J1"].Value = "OtherRef";
                worksheet.Cells["K1"].Value = "Remarks";
                worksheet.Cells["L1"].Value = "AmountPaid";
                worksheet.Cells["M1"].Value = "IsPaid";
                worksheet.Cells["N1"].Value = "PaidDate";
                worksheet.Cells["O1"].Value = "CanceledQuantity";
                worksheet.Cells["P1"].Value = "CreatedBy";
                worksheet.Cells["Q1"].Value = "CreatedDate";
                worksheet.Cells["R1"].Value = "CancellationRemarks";
                worksheet.Cells["S1"].Value = "ReceivedDate";
                worksheet.Cells["T1"].Value = "OriginalPOId";
                worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["V1"].Value = "OriginalDocumentId";
                worksheet.Cells["W1"].Value = "PostedBy";
                worksheet.Cells["X1"].Value = "PostedDate";

                #endregion -- Receving Report Table Header --

                #region -- Receving Report Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 3].Value = item.SupplierInvoiceNumber;
                    worksheet.Cells[row, 4].Value = item.SupplierInvoiceDate;
                    worksheet.Cells[row, 5].Value = item.TruckOrVessels;
                    worksheet.Cells[row, 6].Value = item.QuantityDelivered;
                    worksheet.Cells[row, 7].Value = item.QuantityReceived;
                    worksheet.Cells[row, 8].Value = item.GainOrLoss;
                    worksheet.Cells[row, 9].Value = item.Amount;
                    worksheet.Cells[row, 10].Value = item.AuthorityToLoadNo;
                    worksheet.Cells[row, 11].Value = item.Remarks;
                    worksheet.Cells[row, 12].Value = item.AmountPaid;
                    worksheet.Cells[row, 13].Value = item.IsPaid;
                    worksheet.Cells[row, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.CanceledQuantity;
                    worksheet.Cells[row, 16].Value = item.CreatedBy;
                    worksheet.Cells[row, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 18].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 20].Value = item.POId;
                    worksheet.Cells[row, 21].Value = item.ReceivingReportNo;
                    worksheet.Cells[row, 22].Value = item.ReceivingReportId;
                    worksheet.Cells[row, 23].Value = item.PostedBy;
                    worksheet.Cells[row, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Receving Report Export --

                #region -- Purchase Order Export --

                int poRow = 2;
                var currentPo = "";

                foreach (var item in selectedList.DistinctBy(rr => rr.PurchaseOrder!.PurchaseOrderNo))
                {
                    if (item.PurchaseOrder == null)
                    {
                        continue;
                    }

                    if (item.PurchaseOrder.PurchaseOrderNo == currentPo)
                    {
                        continue;
                    }

                    currentPo = item.PurchaseOrder.PurchaseOrderNo;
                    worksheet2.Cells[poRow, 1].Value = item.PurchaseOrder.Date.ToString("yyyy-MM-dd");
                    worksheet2.Cells[poRow, 2].Value = item.PurchaseOrder.Terms;
                    worksheet2.Cells[poRow, 3].Value = item.PurchaseOrder.Quantity;
                    worksheet2.Cells[poRow, 4].Value = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(item.PurchaseOrder.PurchaseOrderId);
                    worksheet2.Cells[poRow, 5].Value = item.PurchaseOrder.Amount;
                    worksheet2.Cells[poRow, 6].Value = item.PurchaseOrder.FinalPrice;
                    worksheet2.Cells[poRow, 7].Value = item.PurchaseOrder.QuantityReceived;
                    worksheet2.Cells[poRow, 8].Value = item.PurchaseOrder.IsReceived;
                    worksheet2.Cells[poRow, 9].Value = item.PurchaseOrder.ReceivedDate != default ? item.PurchaseOrder.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                    worksheet2.Cells[poRow, 10].Value = item.PurchaseOrder.Remarks;
                    worksheet2.Cells[poRow, 11].Value = item.PurchaseOrder.CreatedBy;
                    worksheet2.Cells[poRow, 12].Value = item.PurchaseOrder.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[poRow, 13].Value = item.PurchaseOrder.IsClosed;
                    worksheet2.Cells[poRow, 14].Value = item.PurchaseOrder.CancellationRemarks;
                    worksheet2.Cells[poRow, 15].Value = item.PurchaseOrder.ProductId;
                    worksheet2.Cells[poRow, 16].Value = item.PurchaseOrder.PurchaseOrderNo;
                    worksheet2.Cells[poRow, 17].Value = item.PurchaseOrder.SupplierId;
                    worksheet2.Cells[poRow, 18].Value = item.PurchaseOrder.PurchaseOrderId;
                    worksheet2.Cells[poRow, 19].Value = item.PurchaseOrder.PostedBy;
                    worksheet2.Cells[poRow, 20].Value = item.PurchaseOrder.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    poRow++;
                }

                #endregion -- Purchase Order Export --

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReceivingReportList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllReceivingReportIds()
        {
            var rrIds = _dbContext.FilprideReceivingReports
                                     .Where(rr => rr.Type == nameof(DocumentType.Documented))
                                     .Select(rr => rr.ReceivingReportId)
                                     .ToList();

            return Json(rrIds);
        }
    }
}
