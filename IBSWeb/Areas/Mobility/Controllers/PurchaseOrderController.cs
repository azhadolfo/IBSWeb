using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
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

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value ?? "ALL";
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var purchaseOrders = await _unitOfWork.MobilityPurchaseOrder
                .GetAllAsync(null, cancellationToken);

            if (GetStationCodeClaimAsync().Result != "ALL")
            {
                purchaseOrders = purchaseOrders.Where(po => po.StationCode == GetStationCodeClaimAsync().Result);
            }
            ViewData["StationCode"] = GetStationCodeClaimAsync().Result;

            return View(purchaseOrders);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var viewModel = new PurchaseOrderViewModel();

            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            viewModel.Stations = await _unitOfWork.GetMobilityStationListAsyncByCode(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var stationCodeClaim = await GetStationCodeClaimAsync();

            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var supplier = await _dbContext.FilprideSuppliers
                        .FirstOrDefaultAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                    MobilityPurchaseOrder model = new()
                    {
                        PurchaseOrderNo = await _unitOfWork.MobilityPurchaseOrder.GenerateCodeAsync(stationCodeClaim, viewModel.Type, cancellationToken),
                        Type = viewModel.Type,
                        Date = viewModel.Date,
                        SupplierId = 1,
                        PickUpPointId = viewModel.PickUpPointId,
                        ProductId = viewModel.ProductId,
                        Terms = viewModel.Terms,
                        Quantity = viewModel.Quantity,
                        UnitPrice = viewModel.UnitPrice,
                        Amount = viewModel.Quantity * viewModel.UnitPrice,
                        SupplierSalesOrderNo = viewModel.SupplierSalesOrderNo,
                        Remarks = viewModel.Remarks,
                        StationCode = stationCodeClaim,
                        CreatedBy = _userManager.GetUserName(User),
                        SupplierAddress = supplier.SupplierAddress,
                        SupplierTin = supplier.SupplierTin,
                        Company = companyClaims,
                    };

                    await _unitOfWork.MobilityPurchaseOrder.AddAsync(model, cancellationToken);

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
                    viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

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
                    Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                    PickUpPointId = existingRecord.PickUpPointId,
                    PickUpPoints = await _unitOfWork.FilpridePickUpPoint.GetDistinctPickupPointList(),
                    ProductId = existingRecord.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
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
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingModel = await _dbContext.MobilityPurchaseOrders.FindAsync(viewModel.PurchaseOrderId, cancellationToken);

                    var companyClaims = await GetCompanyClaimAsync();

                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    var suppliers = await _dbContext.FilprideSuppliers
                        .FirstOrDefaultAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                    viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

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
                    return View(viewModel);
                }
            }

            return View(viewModel);
        }

        // [HttpPost]
        // public async Task<IActionResult> Edit(PurchaseOrderViewModel viewModel, CancellationToken cancellationToken)
        // {
        //     if (ModelState.IsValid)
        //     {
        //         try
        //         {
        //             viewModel.CurrentUser = _userManager.GetUserName(User);
        //             await _unitOfWork.MobilityPurchaseOrder.UpdateAsync(viewModel, cancellationToken);
        //
        //             TempData["success"] = "Purchase order updated successfully.";
        //             return RedirectToAction(nameof(Index));
        //         }
        //         catch (Exception ex)
        //         {
        //             viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
        //             viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
        //             TempData["error"] = ex.Message;
        //             return View(viewModel);
        //         }
        //     }
        //
        //     viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(cancellationToken);
        //     viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
        //     TempData["error"] = "The submitted information is invalid.";
        //     return View(viewModel);
        // }

        public async Task<IActionResult> Preview(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

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

        public async Task<IActionResult> Post(string? id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(po => po.PurchaseOrderNo == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.PostedBy == null)
                {
                    existingRecord.PostedBy = _userManager.GetUserName(User);
                    existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    await _unitOfWork.MobilityPurchaseOrder.PostAsync(existingRecord, cancellationToken);
                }

                TempData["success"] = "Purchase order approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
    }
}
