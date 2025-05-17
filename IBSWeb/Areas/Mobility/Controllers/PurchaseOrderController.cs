using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
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
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(ApplicationDbContext dbContext, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ILogger<PurchaseOrderController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var purchaseOrders = await _unitOfWork.MobilityPurchaseOrder
                .GetAllAsync(null, cancellationToken);

                purchaseOrders = purchaseOrders.Where(po => po.StationCode == GetStationCodeClaimAsync().Result);

            ViewData["StationCode"] = GetStationCodeClaimAsync().Result;

            return View(purchaseOrders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var purchaseOrders = await _unitOfWork.MobilityPurchaseOrder
                    .GetAllAsync(po => po.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    purchaseOrders = purchaseOrders
                    .Where(s =>
                        s.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.PickUpPoint?.Depot.ToLower().Contains(searchValue) == true ||
                        s.Product?.ProductName.ToLower().Contains(searchValue) == true ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.Remarks.ToString().Contains(searchValue) ||
                        s.CreatedBy?.ToLower().Contains(searchValue) == true ||
                        s.Status.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    purchaseOrders = purchaseOrders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = purchaseOrders.Count();

                var pagedData = purchaseOrders
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
                _logger.LogError(ex, "Failed to get purchase order. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var viewModel = new PurchaseOrderViewModel();

            viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var supplier = await _unitOfWork.MobilitySupplier
                        .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                    if (supplier == null)
                    {
                        return NotFound();
                    }

                    MobilityPurchaseOrder model = new()
                    {
                        PurchaseOrderNo = await _unitOfWork.MobilityPurchaseOrder.GenerateCodeAsync(stationCodeClaims, viewModel.Type, cancellationToken),
                        Type = viewModel.Type,
                        Date = viewModel.Date,
                        SupplierId = supplier.SupplierId,
                        PickUpPointId = viewModel.PickUpPointId,
                        ProductId = viewModel.ProductId,
                        Terms = viewModel.Terms,
                        Quantity = viewModel.Quantity,
                        UnitPrice = viewModel.UnitPrice,
                        Amount = viewModel.Quantity * viewModel.UnitPrice,
                        SupplierSalesOrderNo = viewModel.SupplierSalesOrderNo,
                        Remarks = viewModel.Remarks,
                        StationCode = stationCodeClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        SupplierAddress = supplier.SupplierAddress,
                        SupplierTin = supplier.SupplierTin,
                    };

                    await _unitOfWork.MobilityPurchaseOrder.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new purchase order# {model.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = $"Purchase Order created successfully. Series Number: {model.PurchaseOrderNo}.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);
            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                PurchaseOrderViewModel viewModel = new()
                {
                    PurchaseOrderId = existingRecord.PurchaseOrderId,
                    Type = existingRecord.Type,
                    Date = existingRecord.Date,
                    SupplierId = existingRecord.SupplierId,
                    Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken),
                    PickUpPointId = existingRecord.PickUpPointId,
                    PickUpPoints = await _unitOfWork.MobilityPickUpPoint.GetDistinctPickupPointList(cancellationToken),
                    ProductId = existingRecord.ProductId,
                    Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken),
                    Terms = existingRecord.Terms,
                    Quantity = existingRecord.Quantity,
                    UnitPrice = existingRecord.UnitPrice,
                    SupplierSalesOrderNo = existingRecord.SupplierSalesOrderNo,
                    Remarks = existingRecord.Remarks,
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var existingModel = await _dbContext.MobilityPurchaseOrders.FindAsync(viewModel.PurchaseOrderId, cancellationToken);

                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    var suppliers = await _unitOfWork.MobilitySupplier
                        .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                    if (suppliers == null)
                    {
                        return NotFound();
                    }

                    viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);

                    existingModel.Date = viewModel.Date;
                    existingModel.SupplierId = viewModel.SupplierId;
                    existingModel.ProductId = viewModel.ProductId;
                    existingModel.Quantity = viewModel.Quantity;
                    //existingModel.UnTriggeredQuantity = existingModel.Quantity;
                    existingModel.UnitPrice = viewModel.UnitPrice;
                    existingModel.Amount = viewModel.Quantity * viewModel.UnitPrice;
                    existingModel.SupplierSalesOrderNo = viewModel.SupplierSalesOrderNo;
                    existingModel.Remarks = viewModel.Remarks;
                    existingModel.Terms = viewModel.Terms;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    //existingModel.OldPoNo = viewModel.OldPoNo;
                    //existingModel.TriggerDate = viewModel.TriggerDate;
                    existingModel.PickUpPointId = viewModel.PickUpPointId;
                    existingModel.SupplierAddress = suppliers.SupplierAddress;
                    existingModel.SupplierTin = suppliers.SupplierTin;

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited purchase order# {existingModel.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Purchase Order updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.MobilityProduct.GetProductListAsyncById(cancellationToken);
            return View(viewModel);
        }

        public async Task<IActionResult> GetPickUpPoints(int supplierId, CancellationToken cancellationToken)
        {
            var pickUpPoints = await _unitOfWork.MobilityPickUpPoint.GetPickUpPointListBasedOnSupplier(supplierId, cancellationToken);

            return Json(pickUpPoints);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessProductTransfer(int purchaseOrderId, int pickupPointId, string notes, CancellationToken cancellationToken)
        {

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var purchaseOrder = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(p => p.PurchaseOrderId == purchaseOrderId, cancellationToken);

                if (purchaseOrder == null)
                {
                    return NotFound();
                }

                var pickupPoint = await _dbContext.MobilityPickUpPoints
                    .FindAsync(pickupPointId, cancellationToken);

                if (pickupPoint == null)
                {
                    return NotFound();
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Product transfer for Purchase Order {purchaseOrder.PurchaseOrderNo} from {purchaseOrder.PickUpPoint?.Depot} to {pickupPoint.Depot}. \nNote: {notes}", "Purchase Order", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                purchaseOrder.PickUpPointId = pickupPointId;

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to product transfer the purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Transfer by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return Json(new { success = false, message = TempData["error"] });
            }

        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _dbContext.MobilityPurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Product)
                .Include(po => po.PickUpPoint)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityPurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.PostedBy == null)
                {
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(Status.Posted);

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted purchase order# {model.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Posted.";
                }
                return RedirectToAction(nameof(Print), new { id });
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityPurchaseOrders.FindAsync(id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var hasAlreadyBeenUsed =
                await _dbContext.FilprideReceivingReports.AnyAsync(
                    rr => rr.POId == model.PurchaseOrderId && rr.Status != nameof(Status.Voided),
                    cancellationToken) ||
                await _dbContext.FilprideCheckVoucherHeaders.AnyAsync(cv =>
                    cv.CvType == "Trade" && cv.PONo!.Contains(model.PurchaseOrderNo) && cv.Status != nameof(Status.Voided), cancellationToken);

            if (hasAlreadyBeenUsed)
            {
                TempData["error"] = "Please note that this record has already been utilized in a receiving report or check voucher. As a result, voiding it is not permitted.";
                return RedirectToAction(nameof(Index));
            }

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(Status.Voided);

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided purchase order# {model.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Voided.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityPurchaseOrders.FindAsync(id, cancellationToken);

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

                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled purchase order# {model.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.MobilityPurchaseOrder
                .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

            if (po == null)
            {
                return NotFound();
            }

            if (!po.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of purchase order# {po.PurchaseOrderNo}", "Purchase Order", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> UnservedPurchaseOrder(CancellationToken cancellationToken)
        {
            var groupedPurchaseOrders = await _dbContext.MobilityPurchaseOrders
                .Where(po => po.Status == "Posted" && !po.IsReceived)
                .Include(po => po.Product)
                .Include(po => po.PickUpPoint)
                .Include(po => po.MobilityStation)
                .GroupBy(po => po.StationCode)
                .Select(g => new UnservedPurchaseOrderViewModel
                {
                    StationName = $"{g.Key} {g.First().MobilityStation!.StationName} {g.First().MobilityStation!.Initial}",
                    PurchaseOrders = g.ToList()
                })
                .ToListAsync(cancellationToken);

            return View(groupedPurchaseOrders);
        }
    }
}
