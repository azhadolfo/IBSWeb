using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Logistics, SD.Department_TradeAndSupply, SD.Department_Marketing, SD.Department_RCD)]
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<PurchaseOrderController> _logger;
        private const string FilterTypeClaimType = "PurchaseOrder.FilterType";

        public PurchaseOrderController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IHubContext<NotificationHub> hubContext, ILogger<PurchaseOrderController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name
                   ?? "Unknown User";
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
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
            }
            return null;
        }

        public async Task<IActionResult> Index(string? view, string filterType, CancellationToken cancellationToken)
        {
            await UpdateFilterTypeClaim(filterType);
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
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var purchaseOrders = await _dbContext.FilpridePurchaseOrders
                    .Include(po => po.PickUpPoint)
                    .Include(po => po.ActualPrices)
                    .Where(po => po.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    purchaseOrders = purchaseOrders
                        .Where(po => po.Status == nameof(Status.ForApprovalOfOM))
                        .ToList();
                }

                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case nameof(Status.ForApprovalOfOM):
                            purchaseOrders = purchaseOrders
                                .Where(rr => rr.Status == nameof(Status.ForApprovalOfOM))
                                .ToList();
                            break;
                            // Add other cases as needed
                    }
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    purchaseOrders = purchaseOrders
                    .Where(s =>
                        s.PurchaseOrderNo!.ToLower().Contains(searchValue) ||
                        s.OldPoNo.ToLower().Contains(searchValue) ||
                        s.SupplierName.ToLower().Contains(searchValue) ||
                        s.PickUpPoint!.Depot.ToLower().Contains(searchValue) ||
                        s.ProductName.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.Remarks.ToString().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    purchaseOrders = purchaseOrders
                        .Where(s =>
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    purchaseOrders = purchaseOrders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = purchaseOrders.Count;

                var pagedData = purchaseOrders
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(po => new
                    {
                        po.PurchaseOrderId,
                        po.PurchaseOrderNo,
                        po.Date,
                        po.Terms,
                        po.SupplierId,
                        po.SupplierName,
                        po.ProductName,
                        po.PickUpPoint?.Depot,
                        po.Quantity,
                        po.QuantityReceived,
                        po.FinalPrice,
                        po.Amount,
                        po.CreatedBy,
                        po.Status,
                        po.IsReceived,
                        po.VoidedBy,
                        po.CanceledBy,
                        po.PostedBy,
                        po.UnTriggeredQuantity,
                        po.IsSubPo,
                        po.IsClosed
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
                _logger.LogError(ex, "Failed to get purchase order. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new PurchaseOrderViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.PaymentTerms = await _unitOfWork.FilprideTerms
                .GetFilprideTermsListAsyncByCode(cancellationToken);

            viewModel.Suppliers = await _unitOfWork.FilprideSupplier
                .GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork
                .GetProductListAsyncById(cancellationToken);

            ViewBag.FilterType = await GetCurrentFilterType();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            viewModel.PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(companyClaims,
                viewModel.SupplierId, cancellationToken);
            viewModel.PaymentTerms = await _unitOfWork.FilprideTerms
                .GetFilprideTermsListAsyncByCode(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                var product = await _unitOfWork.Product
                    .GetAsync(p => p.ProductId == viewModel.ProductId, cancellationToken);

                if (supplier == null || product == null)
                {
                    return NotFound();
                }

                var model = new FilpridePurchaseOrder
                {
                    PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(companyClaims, viewModel.Type!, cancellationToken),
                    Date = viewModel.Date,
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    SupplierAddress = supplier.SupplierAddress,
                    SupplierTin = supplier.SupplierTin,
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Terms = viewModel.Terms,
                    Quantity = viewModel.Quantity,
                    Price = viewModel.Price,
                    Amount = viewModel.Quantity * viewModel.Price,
                    Remarks = viewModel.Remarks,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    Type = viewModel.Type,
                    TriggerDate = viewModel.TriggerDate,
                    UnTriggeredQuantity = !supplier.RequiresPriceAdjustment ? 0 : viewModel.Quantity,
                    PickUpPointId = viewModel.PickUpPointId,
                    VatType = supplier.VatType,
                    TaxType = supplier.TaxType,
                    OldPoNo = viewModel.OldPoNo,
                    FinalPrice = viewModel.Price,
                };

                await _unitOfWork.FilpridePurchaseOrder.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new purchase order# {model.PurchaseOrderNo}", "Purchase Order", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Purchase Order #{model.PurchaseOrderNo} created successfully";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
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

            var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            var viewModel = new PurchaseOrderViewModel
            {
                Date = purchaseOrder.Date,
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                SupplierId = purchaseOrder.SupplierId,
                Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                ProductId = purchaseOrder.ProductId,
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                PickUpPointId = purchaseOrder.PickUpPointId,
                PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(companyClaims,
                        purchaseOrder.SupplierId, cancellationToken),
                Terms = purchaseOrder.Terms,
                Quantity = purchaseOrder.Quantity,
                Price = purchaseOrder.Price,
                Remarks = purchaseOrder.Remarks,
                TriggerDate = purchaseOrder.TriggerDate,
                OldPoNo = purchaseOrder.OldPoNo,
                SupplierSalesOrderNo = purchaseOrder.SupplierSalesOrderNo,
                PaymentTerms = await _unitOfWork.FilprideTerms
                    .GetFilprideTermsListAsyncByCode(cancellationToken)
            };

            ViewBag.FilterType = await GetCurrentFilterType();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            viewModel.PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(companyClaims,
                viewModel.SupplierId, cancellationToken);
            viewModel.PaymentTerms = await _unitOfWork.FilprideTerms
                .GetFilprideTermsListAsyncByCode(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingModel = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(x => x.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                var product = await _unitOfWork.Product
                    .GetAsync(p => p.ProductId == viewModel.ProductId, cancellationToken);

                if (supplier == null || product == null)
                {
                    return NotFound();
                }

                existingModel.Date = viewModel.Date;
                existingModel.SupplierId = viewModel.SupplierId;
                existingModel.ProductId = viewModel.ProductId;
                existingModel.Quantity = viewModel.Quantity;
                existingModel.UnTriggeredQuantity = !supplier.RequiresPriceAdjustment ? 0 : existingModel.Quantity;
                existingModel.Price = viewModel.Price;
                existingModel.FinalPrice = existingModel.Price;
                existingModel.Amount = viewModel.Quantity * viewModel.Price;
                existingModel.SupplierSalesOrderNo = viewModel.SupplierSalesOrderNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.Terms = viewModel.Terms;
                existingModel.OldPoNo = viewModel.OldPoNo;
                existingModel.TriggerDate = viewModel.TriggerDate;
                existingModel.PickUpPointId = viewModel.PickUpPointId;
                existingModel.SupplierName = supplier.SupplierName;
                existingModel.SupplierAddress = supplier.SupplierAddress;
                existingModel.SupplierTin = supplier.SupplierTin;
                existingModel.ProductName = product.ProductName;
                existingModel.VatType = supplier.VatType;
                existingModel.TaxType = supplier.TaxType;

                if (!_dbContext.ChangeTracker.HasChanges())
                {
                    TempData["warning"] = "No data changes!";
                    return View(viewModel);
                }

                existingModel.EditedBy = GetUserFullName();
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited purchase order# {existingModel.PurchaseOrderNo}", "Purchase Order", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Purchase Order updated successfully";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.FilterType = await GetCurrentFilterType();

            var purchaseOrder = await _dbContext.FilpridePurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Product)
                .Include(po => po.ActualPrices)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var model = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                model.PostedBy = GetUserFullName();
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);
                model.Remarks = StringHelper.FormatRemarksWithSignatories(model.Remarks, GetUserFullName());

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!,
                    $"Posted purchase order# {model.PurchaseOrderNo}", "Purchase Order", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Purchase Order has been Posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to post purchase order. Error: {ErrorMessage}", ex.Message);
                return RedirectToAction(nameof(Print), new { id });
            }

        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var model = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                var hasAlreadyBeenUsed =
                    await _dbContext.FilprideReceivingReports.AnyAsync(
                        rr => rr.POId == model.PurchaseOrderId && rr.Status != nameof(Status.Voided),
                        cancellationToken) ||
                    await _dbContext.FilprideCheckVoucherHeaders.AnyAsync(cv =>
                        cv.CvType == "Trade" && cv.PONo!.Contains(model.PurchaseOrderNo) &&
                        cv.Status != nameof(Status.Voided), cancellationToken);

                if (hasAlreadyBeenUsed)
                {
                    TempData["info"] = "Please note that this record has already been utilized in a receiving report or check voucher. " +
                                       "As a result, voiding it is not permitted.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }

                model.PostedBy = null;
                model.VoidedBy = _userManager.GetUserName(this.User);
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Voided);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!,
                    $"Voided purchase order# {model.PurchaseOrderNo}", "Purchase Order", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Purchase Order has been Voided.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to void purchase order. Error: {ErrorMessage}", ex.Message);
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled purchase order# {model.PurchaseOrderNo}", "Purchase Order", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Purchase Order has been Cancelled.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var po = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

            if (po == null)
            {
                return NotFound();
            }

            if (!po.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of purchase order# {po.PurchaseOrderNo}", "Purchase Order", po.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                FilprideAuditTrail auditTrailBook = new(printedBy!, $"Printed re-printed copy of purchase order# {po.PurchaseOrderNo}", "Purchase Order", po.Company);
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
            worksheet.Cells["S1"].Value = "PostedBy";
            worksheet.Cells["T1"].Value = "PostedDate";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Terms;
                worksheet.Cells[row, 3].Value = item.Quantity;
                worksheet.Cells[row, 4].Value = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(item.PurchaseOrderId);
                worksheet.Cells[row, 5].Value = item.Amount;
                worksheet.Cells[row, 6].Value = item.FinalPrice;
                worksheet.Cells[row, 7].Value = item.QuantityReceived;
                worksheet.Cells[row, 8].Value = item.IsReceived;
                worksheet.Cells[row, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                worksheet.Cells[row, 10].Value = item.Remarks;
                worksheet.Cells[row, 11].Value = item.CreatedBy;
                worksheet.Cells[row, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 13].Value = item.IsClosed;
                worksheet.Cells[row, 14].Value = item.CancellationRemarks;
                worksheet.Cells[row, 15].Value = item.ProductId;
                worksheet.Cells[row, 16].Value = item.PurchaseOrderNo;
                worksheet.Cells[row, 17].Value = item.SupplierId;
                worksheet.Cells[row, 18].Value = item.PurchaseOrderId;
                worksheet.Cells[row, 19].Value = item.PostedBy;
                worksheet.Cells[row, 20].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PurchaseOrderList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllPurchaseOrderIds()
        {
            var companyClaims = await GetCompanyClaimAsync();
            var poIds = await _dbContext.FilpridePurchaseOrders
                                     .Where(po => po.Type == nameof(DocumentType.Documented) && po.Company == companyClaims)
                                     .Select(po => po.PurchaseOrderId)
                                     .ToListAsync();

            return Json(poIds);
        }

        [HttpPost]
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

                var message = $"The cost for Purchase Order {existingRecord.PurchaseOrderNo} has been updated by {currentUser}, from {existingRecord.Price:N4} to {price:N4} (gross of VAT). " +
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

                existingRecord.Status = nameof(Status.ForApprovalOfOM);

                await _dbContext.FilpridePOActualPrices.AddAsync(actualPrice, cancellationToken);
                #endregion

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(currentUser!, $"Update actual price for purchase order# {existingRecord.PurchaseOrderNo}, from {existingRecord.Price:N4} to {price:N4} (gross of VAT).", "Purchase Order", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = $"The price for {existingRecord.PurchaseOrderNo} has been updated, from {existingRecord.Price:N4} to {price:N4} (gross of VAT).";

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

            if (existingRecord == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var actualPrices = await _dbContext.FilpridePOActualPrices
                    .FirstOrDefaultAsync(a => a.PurchaseOrderId == existingRecord.PurchaseOrderId
                                              && !a.IsApproved, cancellationToken);

                if (actualPrices == null)
                {
                    TempData["error"] = "Actual price not found!";
                    return Json(new { success = false, message = TempData["error"] });
                }

                actualPrices.ApprovedBy = GetUserFullName();
                actualPrices.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                actualPrices.IsApproved = true;

                await _unitOfWork.FilpridePurchaseOrder.UpdateActualCostOnSalesAndReceiptsAsync(actualPrices, cancellationToken);

                existingRecord.FinalPrice =  actualPrices.TriggeredPrice;
                existingRecord.Status = nameof(Status.Posted);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Approved the actual price of purchase order# {existingRecord.PurchaseOrderNo}",
                    "Purchase Order",
                    existingRecord.Company);

                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

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

        public async Task<IActionResult> GetPickUpPoints(int supplierId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var pickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetPickUpPointListBasedOnSupplier(companyClaims, supplierId, cancellationToken);

            return Json(pickUpPoints);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessProductTransfer(int purchaseOrderId, int pickupPointId, string notes, CancellationToken cancellationToken)
        {

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(p => p.PurchaseOrderId == purchaseOrderId, cancellationToken);

                if (purchaseOrder == null)
                {
                    return NotFound();
                }

                var pickupPoint = await _unitOfWork.FilpridePickUpPoint
                    .GetAsync(x => x.PickUpPointId == pickupPointId, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Product transfer for Purchase Order {purchaseOrder.PurchaseOrderNo} from {purchaseOrder.PickUpPoint!.Depot} to {pickupPoint!.Depot}. \nNote: {notes}", "Purchase Order", purchaseOrder.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                purchaseOrder.PickUpPointId = pickupPointId;

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true });
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

        [HttpPost]
        public async Task<IActionResult> UpdateSupplierSalesOrderNo(int purchaseOrderId, string supplierSalesOrderNo, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(p => p.PurchaseOrderId == purchaseOrderId, cancellationToken);

                if (purchaseOrder == null)
                {
                    return Json(new { success = false, message = "Purchase Order not found." });
                }

                purchaseOrder.SupplierSalesOrderNo = supplierSalesOrderNo;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Update sales order number of purchase order# {purchaseOrder.PurchaseOrderNo}.", "Purchase Order", purchaseOrder.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = "Supplier Order # updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update sales order number of purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Transfer by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return Json(new { success = false, message = TempData["error"] });
            }
        }

        public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilpridePurchaseOrder
                .GetAsync(x => x.PurchaseOrderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.Status = nameof(Status.Closed);
                model.IsClosed = true;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Closed purchase order# {model.PurchaseOrderNo}", "Purchase Order", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Purchase Order has been closed.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to close purchase order. Error: {ErrorMessage}, Stack: {StackTrace}. Closed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }
    }
}
