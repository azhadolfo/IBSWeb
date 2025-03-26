using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
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
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD)]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IHubContext<NotificationHub> _hubContext;

        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext, ILogger<PurchaseOrderController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
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
            if (view == nameof(DynamicView.PurchaseOrder))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(po => po.Company == companyClaims && po.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", purchaseOrders);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(po => po.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    purchaseOrders = purchaseOrders
                    .Where(s =>
                        s.PurchaseOrderNo.ToLower().Contains(searchValue) ||
                        s.OldPoNo.ToLower().Contains(searchValue) ||
                        s.Supplier.SupplierName.ToLower().Contains(searchValue) ||
                        s.PickUpPoint.Depot.ToLower().Contains(searchValue) ||
                        s.Product.ProductName.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.Remarks.ToString().Contains(searchValue) ||
                        s.CreatedBy.ToLower().Contains(searchValue) ||
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

            var isPoLock = await _dbContext.AppSettings
                .Where(s => s.SettingKey == AppSettingKey.LockTheCreationOfPo)
                .Select(s => s.Value == "true")
                .FirstOrDefaultAsync(cancellationToken);

            if (isPoLock)
            {
                TempData["denied"] = "Creation is locked due to untriggered purchase orders.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new FilpridePurchaseOrder();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilpridePurchaseOrder model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            model.Company = companyClaims;

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var supplier = await _dbContext.FilprideSuppliers
                        .FirstOrDefaultAsync(s => s.SupplierId == model.SupplierId, cancellationToken);

                    model.PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(companyClaims, model.Type, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Amount = model.Quantity * model.Price;
                    model.UnTriggeredQuantity = model.Quantity;
                    model.SupplierAddress = supplier.SupplierAddress;
                    model.SupplierTin = supplier.SupplierTin;
                    await _dbContext.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Purchase Order created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder.GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            purchaseOrder.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            purchaseOrder.PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(purchaseOrder.SupplierId, cancellationToken);

            purchaseOrder.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            ViewBag.PurchaseOrders = purchaseOrder.Quantity;

            return View(purchaseOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilpridePurchaseOrder model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingModel = await _dbContext.FilpridePurchaseOrders.FindAsync(model.PurchaseOrderId, cancellationToken);

                    var companyClaims = await GetCompanyClaimAsync();

                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    var suppliers = await _dbContext.FilprideSuppliers
                        .FirstOrDefaultAsync(s => s.SupplierId == model.SupplierId, cancellationToken);

                    model.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

                    existingModel.Date = model.Date;
                    existingModel.SupplierId = model.SupplierId;
                    existingModel.ProductId = model.ProductId;
                    existingModel.Quantity = model.Quantity;
                    existingModel.UnTriggeredQuantity = existingModel.Quantity;
                    existingModel.Price = model.Price;
                    existingModel.Amount = model.Quantity * model.Price;
                    existingModel.SupplierSalesOrderNo = model.SupplierSalesOrderNo;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Terms = model.Terms;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingModel.OldPoNo = model.OldPoNo;
                    existingModel.TriggerDate = model.TriggerDate;
                    existingModel.PickUpPointId = model.PickUpPointId;
                    existingModel.SupplierAddress = suppliers.SupplierAddress;
                    existingModel.SupplierTin = suppliers.SupplierTin;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited purchase order# {existingModel.PurchaseOrderNo}", "Purchase Order", ipAddress, existingModel.Company);
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
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideReceivingReports == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _dbContext.FilpridePurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Product)
                .Include(po => po.ActualPrices)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.PostedBy == null)
                {
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(Status.Posted);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
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
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

            var hasAlreadyBeenUsed =
                await _dbContext.FilprideReceivingReports.AnyAsync(
                    rr => rr.POId == model.PurchaseOrderId && rr.Status != nameof(Status.Voided),
                    cancellationToken) ||
                await _dbContext.FilprideCheckVoucherHeaders.AnyAsync(cv =>
                    cv.CvType == "Trade" && cv.PONo.Contains(model.PurchaseOrderNo) && cv.Status != nameof(Status.Voided), cancellationToken);

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

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
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
            var model = await _dbContext.FilpridePurchaseOrders.FindAsync(id, cancellationToken);

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
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress, model.Company);
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
            var po = await _unitOfWork.FilpridePurchaseOrder.GetAsync(x => x.PurchaseOrderId == id, cancellationToken);
            if (!po.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of purchase order# {po.PurchaseOrderNo}", "Purchase Order", ipAddress, po.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
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
            var selectedList = await _dbContext.FilpridePurchaseOrders
                .Where(po => recordIds.Contains(po.PurchaseOrderId))
                .OrderBy(po => po.PurchaseOrderNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("PurchaseOrder");

            worksheet.Cells["A1"].Value = "Date";
            worksheet.Cells["B1"].Value = "Terms";
            worksheet.Cells["C1"].Value = "Quantity";
            worksheet.Cells["D1"].Value = "Price";
            worksheet.Cells["E1"].Value = "Amount";
            worksheet.Cells["F1"].Value = "FinalPrice";
            worksheet.Cells["G1"].Value = "QuantityReceived";
            worksheet.Cells["H1"].Value = "IsReceived";
            worksheet.Cells["I1"].Value = "ReceivedDate";
            worksheet.Cells["J1"].Value = "Remarks";
            worksheet.Cells["K1"].Value = "CreatedBy";
            worksheet.Cells["L1"].Value = "CreatedDate";
            worksheet.Cells["M1"].Value = "IsClosed";
            worksheet.Cells["N1"].Value = "CancellationRemarks";
            worksheet.Cells["O1"].Value = "OriginalProductId";
            worksheet.Cells["P1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["Q1"].Value = "OriginalSupplierId";
            worksheet.Cells["R1"].Value = "OriginalDocumentId";
            worksheet.Cells["S1"].Value = "PostedDate";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Terms;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = item.Price;
                worksheet.Cells[row, 5].Value = item.Amount;
                worksheet.Cells[row, 6].Value = item.FinalPrice;
                worksheet.Cells[row, 7].Value = item.QuantityReceived;
                worksheet.Cells[row, 8].Value = item.IsReceived;
                worksheet.Cells[row, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : default;
                worksheet.Cells[row, 10].Value = item.Remarks;
                worksheet.Cells[row, 11].Value = item.CreatedBy;
                worksheet.Cells[row, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 13].Value = item.IsClosed;
                worksheet.Cells[row, 14].Value = item.CancellationRemarks;
                worksheet.Cells[row, 15].Value = item.ProductId;
                worksheet.Cells[row, 16].Value = item.PurchaseOrderNo;
                worksheet.Cells[row, 17].Value = item.SupplierId;
                worksheet.Cells[row, 18].Value = item.PurchaseOrderId;
                worksheet.Cells[row, 19].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PurchaseOrderList_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllPurchaseOrderIds()
        {
            var poIds = _dbContext.FilpridePurchaseOrders
                                     .Where(po => po.Type == nameof(DocumentType.Documented))
                                     .Select(po => po.PurchaseOrderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(poIds);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(int purchaseOrderId, decimal volume, decimal price, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingRecord = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(p => p.PurchaseOrderId == purchaseOrderId, cancellationToken);

                var currentUser = _userManager.GetUserName(User);

                if (existingRecord == null)
                {
                    return Json(new { success = false, message = "Record not found." });
                }

                existingRecord.UnTriggeredQuantity -= volume;

                var actualPrice = new FilpridePOActualPrice
                {
                    PurchaseOrderId = existingRecord.PurchaseOrderId,
                    TriggeredVolume = volume,
                    TriggeredPrice = price
                };

                #region Notification

                var operationManager = await _dbContext.ApplicationUsers
                    .Where(a => a.Position == SD.Position_OperationManager)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                var message = $"The cost for Purchase Order {existingRecord.PurchaseOrderNo} has been updated by {currentUser}, affecting a volume of {volume:N4}L from {existingRecord.Price:N4} to {price:N4} (gross of VAT). " +
                              $"Please review and approve.";

                await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(operationManager, message);

                var usernames = await _dbContext.ApplicationUsers
                    .Where(a => operationManager.Contains(a.Id))
                    .Select(u => u.UserName)
                    .ToListAsync(cancellationToken);

                foreach (var username in usernames)
                {
                    var hubConnections = await _dbContext.HubConnections
                        .Where(h => h.UserName == username)
                        .ToListAsync(cancellationToken);

                    foreach (var hubConnection in hubConnections)
                    {
                        await _hubContext.Clients.Client(hubConnection.ConnectionId)
                            .SendAsync("ReceivedNotification", "You have a new message.", cancellationToken);
                    }
                }

                existingRecord.Status = nameof(DRStatus.ForApprovalOfOM);

                await _dbContext.FilpridePOActualPrices.AddAsync(actualPrice, cancellationToken);
                #endregion

                #region --Audit Trail Recording

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(currentUser, $"Update actual price for {volume}L purchase order# {existingRecord.PurchaseOrderNo}", "Purchase Order", ipAddress, existingRecord.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = $"The price for {existingRecord.PurchaseOrderNo} has been updated, affecting a volume of {volume}L.";

                return Json(new { success = true, message = TempData["success"] });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update price of purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Updated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return Json(new { success = false, message = TempData["error"] });
            }
        }

        [HttpPost]
        [Authorize(Roles = "OperationManager, Admin, HeadApprover")]
        public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        {
            var existingRecord = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(p => p.PurchaseOrderId == id, cancellationToken);

            if (existingRecord != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var actualPrices = await _dbContext.FilpridePOActualPrices
                        .FirstOrDefaultAsync(a => a.PurchaseOrderId == existingRecord.PurchaseOrderId && !a.IsApproved, cancellationToken);

                    if (actualPrices != null)
                    {
                        actualPrices.ApprovedBy = _userManager.GetUserName(this.User);
                        actualPrices.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        actualPrices.IsApproved = true;

                        await _unitOfWork.FilpridePurchaseOrder.UpdateActualCostOnSalesAndReceiptsAsync(actualPrices, cancellationToken);
                    }

                    existingRecord.Status = nameof(Status.Posted);

                    var isCreationOfPoLocked = await _dbContext.AppSettings
                        .Where(s => s.SettingKey == AppSettingKey.LockTheCreationOfPo)
                        .Select(s => s.Value == "true")
                        .FirstOrDefaultAsync(cancellationToken);

                    var untriggeredPoNumbers = await _unitOfWork.FilpridePurchaseOrder
                        .GetUntriggeredPurchaseOrderNumbersAsync(cancellationToken);

                    if (isCreationOfPoLocked && untriggeredPoNumbers.Count == 0)
                    {
                        await _unitOfWork.FilpridePurchaseOrder
                            .UnlockTheCreationOfPurchaseOrderAsync(cancellationToken);

                        var users = await _dbContext.ApplicationUsers
                            .Where(u => u.Department == SD.Department_TradeAndSupply ||
                                        u.Department == SD.Department_ManagementAccounting)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = "Please be informed that all un-triggered POs have now been triggered completely. The creation of POs is now available. \n" +
                                      "CC: Management Accounting";

                        await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(users, message);

                        var usernames = await _dbContext.ApplicationUsers
                            .Where(a => users.Contains(a.Id))
                            .Select(u => u.UserName)
                            .ToListAsync(cancellationToken);

                        foreach (var username in usernames)
                        {
                            var hubConnections = await _dbContext.HubConnections
                                .Where(h => h.UserName == username)
                                .ToListAsync(cancellationToken);

                            foreach (var hubConnection in hubConnections)
                            {
                                await _hubContext.Clients.Client(hubConnection.ConnectionId)
                                    .SendAsync("ReceivedNotification", "You have a new message.", cancellationToken);
                            }
                        }

                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User),
                        $"Approved the actual price of purchase order# {existingRecord.PurchaseOrderNo}",
                        "Purchase Order",
                        ipAddress,
                        existingRecord.Company);

                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new { message = "The Purchase Order has been approved. All associated Receiving Reports (RR) have been updated with the new cost." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to approve purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Approved by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    return BadRequest(new { error = ex.Message });
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> GetPickUpPoints(int supplierId, CancellationToken cancellationToken)
        {
            var pickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(supplierId, cancellationToken);

            return Json(pickUpPoints);
        }
    }
}
