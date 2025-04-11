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

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<ReceivingReportController> _logger;

        public ReceivingReportController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ILogger<ReceivingReportController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var receivingReports = await _unitOfWork.MobilityReceivingReport
                .GetAllAsync(null, cancellationToken);

                receivingReports = receivingReports.Where(po => po.StationCode == GetStationCodeClaimAsync().Result);

            ViewData["StationCode"] = GetStationCodeClaimAsync().Result;

            return View(receivingReports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetReceivingReports([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaim = await GetStationCodeClaimAsync();

                var receivingReports = await _unitOfWork.MobilityReceivingReport
                    .GetAllAsync(rr => rr.StationCode == stationCodeClaim, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    receivingReports = receivingReports
                    .Where(s =>
                        s.ReceivingReportNo.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.QuantityReceived.ToString().Contains(searchValue) ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.CreatedBy.ToLower().Contains(searchValue) ||
                        s.Remarks.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
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
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaim = await GetStationCodeClaimAsync();
            ReceivingReportViewModel viewModel = new()
            {
                DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken),
                PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                    .Where(po => po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                    .Select(po => new SelectListItem
                    {
                        Value = po.PurchaseOrderId.ToString(),
                        Text = po.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaim = await GetStationCodeClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Retrieve PO

                    var existingPo = await _unitOfWork.MobilityPurchaseOrder.GetAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                    #endregion --Retrieve PO

                    var totalAmountRR = existingPo.Quantity - existingPo.QuantityReceived;

                    if (viewModel.QuantityDelivered > totalAmountRR)
                    {
                        viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                        viewModel.PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                            .Where(po =>
                                po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                            .Select(po => new SelectListItem
                            {
                                Value = po.PurchaseOrderId.ToString(),
                                Text = po.PurchaseOrderNo
                            })
                            .ToListAsync(cancellationToken);
                        TempData["error"] = "Input is exceed to remaining quantity delivered";
                        return View(viewModel);
                    }

                    MobilityReceivingReport model = new()
                    {
                        ReceivingReportNo = await _unitOfWork.MobilityReceivingReport.GenerateCodeAsync(stationCodeClaim, existingPo.Type, cancellationToken),
                        Date = viewModel.Date,
                        Remarks = viewModel.Remarks,
                        StationCode = stationCodeClaim,
                        CreatedBy = _userManager.GetUserName(User),
                        GainOrLoss = viewModel.QuantityReceived - viewModel.QuantityDelivered,
                        PurchaseOrderNo = existingPo.PurchaseOrderNo,
                        Type = existingPo.Type,
                        PurchaseOrderId = viewModel.PurchaseOrderId,
                        ReceivedDate = viewModel.ReceivedDate,
                        SupplierInvoiceNumber = viewModel.SupplierInvoiceNumber,
                        SupplierInvoiceDate = viewModel.SupplierInvoiceDate,
                        SupplierDrNo = viewModel.SupplierDrNo,
                        WithdrawalCertificate = viewModel.WithdrawalCertificate,
                        TruckOrVessels = viewModel.TruckOrVessels,
                        QuantityDelivered = viewModel.QuantityDelivered,
                        QuantityReceived = viewModel.QuantityReceived,
                        AuthorityToLoadNo = viewModel.AuthorityToLoadNo,
                        DueDate = await _unitOfWork.MobilityReceivingReport.ComputeDueDateAsync(viewModel.PurchaseOrderId, viewModel.Date, cancellationToken),
                        Amount = viewModel.QuantityReceived * existingPo.UnitPrice,
                    };

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.StationCode);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.MobilityReceivingReport.AddAsync(model, cancellationToken);
                    await _unitOfWork.SaveAsync(cancellationToken);

                    TempData["success"] = "Receiving report created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    viewModel.PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                        .Where(po =>
                            po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                        .Select(po => new SelectListItem
                        {
                            Value = po.PurchaseOrderId.ToString(),
                            Text = po.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
            viewModel.PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                .Where(po =>
                    po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaim = await GetStationCodeClaimAsync();
            if (id == 0)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityReceivingReport
                    .GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                ReceivingReportViewModel viewModel = new()
                {
                    ReceivingReportId = existingRecord.ReceivingReportId,
                    Date = existingRecord.Date,
                    Remarks = existingRecord.Remarks,
                    DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken),
                    PurchaseOrderId = existingRecord.PurchaseOrderId,
                    ReceivedDate = existingRecord.ReceivedDate,
                    SupplierInvoiceNumber = existingRecord.SupplierInvoiceNumber,
                    SupplierInvoiceDate = existingRecord.SupplierInvoiceDate,
                    SupplierDrNo = existingRecord.SupplierDrNo,
                    WithdrawalCertificate = existingRecord.WithdrawalCertificate,
                    TruckOrVessels = existingRecord.TruckOrVessels,
                    QuantityDelivered = existingRecord.QuantityDelivered,
                    QuantityReceived = existingRecord.QuantityReceived,
                    AuthorityToLoadNo = existingRecord.AuthorityToLoadNo,
                    PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                        .Where(po =>
                            po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                        .Select(po => new SelectListItem
                        {
                            Value = po.PurchaseOrderId.ToString(),
                            Text = po.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken),
                    PostedBy = existingRecord.PostedBy,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceivingReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaim = await GetStationCodeClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);
                    await _unitOfWork.MobilityReceivingReport.UpdateAsync(viewModel, stationCodeClaim, cancellationToken);

                    TempData["success"] = "Receiving report updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
                    viewModel.PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                        .Where(po =>
                            po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                        .Select(po => new SelectListItem
                        {
                            Value = po.PurchaseOrderId.ToString(),
                            Text = po.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListAsync(cancellationToken);
            viewModel.PurchaseOrders = await _dbContext.MobilityPurchaseOrders
                .Where(po =>
                    po.StationCode == stationCodeClaim && !po.IsReceived && po.PostedBy != null && !po.IsClosed)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityReceivingReport
                    .GetAsync(rr => rr.ReceivingReportNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.MobilityReceivingReport.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var rr = await _unitOfWork.MobilityReceivingReport.GetAsync(x => x.ReceivingReportId == id, cancellationToken);
            if (!rr.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of receiving report# {rr.ReceivingReportNo}", "Receiving Report", ipAddress, rr.StationCode);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                rr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityReceivingReport.GetAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.ReceivedDate == null)
                    {
                        TempData["error"] = "Please indicate the received date.";
                        return RedirectToAction(nameof(Index));
                    }

                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Posted);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.StationCode);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _unitOfWork.MobilityReceivingReport.PostAsync(model, cancellationToken);

                        TempData["success"] = "Receiving Report has been Posted.";
                        return RedirectToAction(nameof(Print), new { id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityReceivingReports
                .FindAsync(id, cancellationToken);

            // var existingInventory = await _dbContext.FilprideInventories
            //     .Include(i => i.Product)
            //     .FirstOrDefaultAsync(i => i.Reference == model.ReceivingReportNo && i.Company == model.Company);

            if (model != null /* && existingInventory != null */)
            {
                var hasAlreadyBeenUsed =
                    await _dbContext.FilprideSalesInvoices.AnyAsync(
                        si => si.ReceivingReportId == model.ReceivingReportId && si.Status != nameof(Status.Voided),
                        cancellationToken) ||
                    await _dbContext.FilprideCheckVoucherHeaders.AnyAsync(cv =>
                        cv.CvType == "Trade" && cv.RRNo.Contains(model.ReceivingReportNo) && cv.Status != nameof(Status.Voided), cancellationToken);

                if (hasAlreadyBeenUsed)
                {
                    TempData["error"] = "Please note that this record has already been utilized in a sales invoice or check voucher. As a result, voiding it is not permitted.";
                    return RedirectToAction(nameof(Index));
                }

                if (model.VoidedBy == null)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    try
                    {
                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.MobilityReceivingReport.RemoveRecords<FilpridePurchaseBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
                        await _unitOfWork.MobilityReceivingReport.RemoveRecords<FilprideGeneralLedgerBook>(pb => pb.Reference == model.ReceivingReportNo, cancellationToken);
                        //await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                        await _unitOfWork.MobilityReceivingReport.RemoveQuantityReceived(model.PurchaseOrderId, model.QuantityReceived, cancellationToken);
                        model.QuantityReceived = 0;

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.StationCode);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to void receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                            ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(Index));
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityReceivingReports.FindAsync(id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                        model.QuantityDelivered = 0;
                        model.QuantityReceived = 0;
                        model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress, model.StationCode);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel receiving report. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetDrDetails(int drId, CancellationToken cancellationToken)
        {
            var drDetails = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == drId, cancellationToken);
            if (drDetails == null)
            {
                return NotFound();
            }
            return Json(new
            {
                Product = drDetails.CustomerOrderSlip.PurchaseOrder.Product.ProductName,
                drDetails.Quantity
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.MobilityPurchaseOrder.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .MobilityReceivingReports
                .Where(rr => rr.StationCode == po.StationCode && rr.PurchaseOrderNo == po.PurchaseOrderNo && rr.PostedBy != null)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .MobilityReceivingReports
                .Where(rr => rr.StationCode == po.StationCode && rr.PurchaseOrderNo == po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .MobilityReceivingReports
                .Where(rr => rr.StationCode == po.StationCode && rr.PurchaseOrderNo == po.PurchaseOrderNo && rr.PostedBy == null && rr.CanceledBy == null)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .MobilityReceivingReports
                .Where(rr => rr.StationCode == po.StationCode && rr.PurchaseOrderNo == po.PurchaseOrderNo && rr.CanceledBy != null)
                .ToListAsync(cancellationToken);

            if (po != null)
            {
                return Json(new
                {
                    poNo = po.PurchaseOrderNo,
                    poQuantity = po.Quantity.ToString(SD.Two_Decimal_Format),
                    rrList = rr,
                    rrListPostedOnly = rrPostedOnly,
                    rrListNotPosted = rrNotPosted,
                    rrListCanceled = rrCanceled
                });
            }
            else
            {
                return Json(null);
            }
        }
    }
}
