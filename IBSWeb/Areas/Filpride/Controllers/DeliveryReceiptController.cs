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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_Finance, SD.Department_Marketing, SD.Department_TradeAndSupply, SD.Department_Logistics, SD.Department_CreditAndCollection)]
    public class DeliveryReceiptController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IHubContext<NotificationHub> _hubContext;

        private const string FilterTypeClaimType = "DeliveryReceipt.FilterType";

        private readonly ILogger<DeliveryReceiptController> _logger;

        public DeliveryReceiptController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            IHubContext<NotificationHub> hubContext,
            ILogger<DeliveryReceiptController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
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
            if (user != null)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
            }
            return null;
        }

        public async Task<IActionResult> Index(string filterType)
        {
            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = filterType;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetDeliveryReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var drList = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(cos => cos.Company == companyClaims, cancellationToken);

                // Apply status filter based on filterType
                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "InTransit":
                            drList = drList.Where(dr =>
                                dr.Status == nameof(DRStatus.PendingDelivery));
                            break;
                        case "ForInvoice":
                            drList = drList.Where(dr =>
                                dr.Status == nameof(DRStatus.ForInvoicing));
                            break;
                        case "ForOMApproval":
                            drList = drList.Where(dr =>
                                dr.Status == nameof(CosStatus.ForApprovalOfOM));
                            break;
                        case "RecordLiftingDate":
                            drList = drList.Where(dr =>
                                !dr.HasReceivingReport && dr.CanceledBy == null && dr.VoidedBy == null);
                            break;
                            // Add other cases as needed
                    }
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    drList = drList
                    .Where(s =>
                        s.DeliveryReceiptNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip!.CustomerName.ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.TotalAmount.ToString().Contains(searchValue) ||
                        s.ManualDrNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip!.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip!.ProductName.ToLower().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder!.PurchaseOrderNo!.ToLower().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    drList = drList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = drList.Count();

                var pagedData = drList
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(dr => new
                    {
                        dr.DeliveryReceiptId,
                        dr.DeliveryReceiptNo,
                        dr.ManualDrNo,
                        dr.Date,
                        dr.DeliveredDate,
                        dr.CustomerOrderSlip!.CustomerOrderSlipNo,
                        dr.PurchaseOrder!.PurchaseOrderNo,
                        dr.CustomerOrderSlip!.Depot,
                        dr.CustomerOrderSlip!.CustomerName,
                        dr.CustomerOrderSlip!.ProductName,
                        dr.Quantity,
                        dr.CreatedBy,
                        dr.Status,
                        dr.VoidedBy,
                        dr.CanceledBy,
                        dr.HasReceivingReport,
                        dr.AuthorityToLoad!.UppiAtlNo,
                        dr.AuthorityToLoadNo
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
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to get delivery receipts. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            ViewBag.FilterType = await GetCurrentFilterType();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var isDrLock = await _dbContext.AppSettings
                .Where(s => s.SettingKey == AppSettingKey.LockTheCreationOfDr)
                .Select(s => s.Value == "true")
                .FirstOrDefaultAsync(cancellationToken);

            DeliveryReceiptViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken),
                IsTheCreationLockForTheMonth = isDrLock,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var customerOrderSlip = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    var supplierHauler = await _unitOfWork.FilprideSupplier.GetAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

                    if (customerOrderSlip == null)
                    {
                        return BadRequest();
                    }

                    FilprideDeliveryReceipt model = new()
                    {
                        DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(companyClaims, cancellationToken),
                        Date = viewModel.Date,
                        CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                        CustomerId = viewModel.CustomerId,
                        Remarks = viewModel.Remarks,
                        Quantity = viewModel.Volume,
                        TotalAmount = viewModel.TotalAmount,
                        Company = companyClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        ManualDrNo = viewModel.ManualDrNo,
                        Freight = viewModel.Freight,
                        FreightAmount = viewModel.Volume * (viewModel.Freight + viewModel.ECC),
                        ECC = viewModel.ECC,
                        Driver = viewModel.Driver,
                        PlateNo = viewModel.PlateNo,
                        HaulerId = viewModel.HaulerId ?? customerOrderSlip.HaulerId,
                        AuthorityToLoadId = viewModel.ATLId,
                        AuthorityToLoadNo = viewModel.ATLNo,
                        CommissioneeId = customerOrderSlip.CommissioneeId,
                        CommissionRate = customerOrderSlip.CommissionRate,
                        CommissionAmount = viewModel.Volume * customerOrderSlip.CommissionRate,
                        CustomerAddress = customerOrderSlip.CustomerAddress,
                        CustomerTin = customerOrderSlip.CustomerTin,
                        HaulerName = supplierHauler?.SupplierName ?? string.Empty,
                        PurchaseOrderId = viewModel.PurchaseOrderId,
                    };

                    customerOrderSlip.DeliveredQuantity += model.Quantity;
                    customerOrderSlip.BalanceQuantity -= model.Quantity;

                    if (customerOrderSlip.BalanceQuantity <= 0)
                    {
                        customerOrderSlip.Status = nameof(CosStatus.Completed);
                    }

                    await _unitOfWork.FilprideDeliveryReceipt.AssignNewPurchaseOrderAsync(model);

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    if (viewModel.Freight != customerOrderSlip.Freight || viewModel.IsECCEdited)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var po = await _dbContext.FilpridePurchaseOrders
                            .Include(x => x.ActualPrices)
                            .FirstOrDefaultAsync(x => x.PurchaseOrderId == model.PurchaseOrderId, cancellationToken)
                            ?? throw new NullReferenceException($"{model.PurchaseOrderId} not found");

                        var grossMargin = ComputeGrossMargin(customerOrderSlip, po);

                        var freightDifference = viewModel.Freight + viewModel.ECC - (decimal)customerOrderSlip.Freight!;

                        /// TODO Revise this later for dynamic checking
                        freightDifference = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(freightDifference);

                        var updatedGrossMargin = grossMargin + freightDifference;

                        var message = $"Delivery Receipt ({model.DeliveryReceiptNo}) has been successfully generated. " +
                                      $"The Freight and/or ECC values have been modified by {model.CreatedBy!.ToUpper()}. " +
                                      $"Please review and approve the adjustment in freight charges, which changed from {customerOrderSlip.Freight:N4} to {viewModel.Freight + viewModel.ECC:N4}. " +
                                      $"Note that this change will impact the approved gross margin, updating it from {grossMargin:N4} to {updatedGrossMargin:N4}.";

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

                        model.Status = nameof(DRStatus.ForApprovalOfOM);
                    }

                    var authorityToLoad = await _unitOfWork.FilprideAuthorityToLoad
                        .GetAsync(x => x.AuthorityToLoadId == model.AuthorityToLoadId, cancellationToken)
                        ?? throw new NullReferenceException("Authority to load not found");

                    authorityToLoad.HaulerName = supplierHauler?.SupplierName;
                    authorityToLoad.Freight = viewModel.Freight;
                    authorityToLoad.Driver = viewModel.Driver;
                    authorityToLoad.PlateNo = viewModel.PlateNo;

                    await _unitOfWork.SaveAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt created successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
                    viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to create delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = await GetCurrentFilterType();
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                var purchaseOrders = await _dbContext.FilprideBookAtlDetails
                    .Include(x => x.Header)
                    .Include(x => x.CustomerOrderSlip)
                    .Include(x => x.AppointedSupplier!)
                    .ThenInclude(x => x.PurchaseOrder!)
                    .ThenInclude(x => x.Supplier)
                    .Where(x => x.CustomerOrderSlipId == id)
                    .Select(a => new SelectListItem
                    {
                        Value = a.AppointedSupplier!.PurchaseOrderId.ToString(),
                        Text = $"PO: {a.AppointedSupplier.PurchaseOrder!.PurchaseOrderNo} | " +
                               $"Supplier: {a.AppointedSupplier.Supplier!.SupplierName} | " +
                               $"ATL#: {a.Header!.AuthorityToLoadNo} | " +
                               $"Unserved: {a.UnservedQuantity}"
                    })
                    .ToListAsync(cancellationToken);

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    CustomerId = existingRecord.Customer!.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                    CustomerAddress = existingRecord.CustomerAddress,
                    CustomerTin = existingRecord.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken),
                    PurchaseOrderId = (int)existingRecord.PurchaseOrderId!,
                    PurchaseOrders = purchaseOrders,
                    Product = existingRecord.CustomerOrderSlip!.Product!.ProductName,
                    CosVolume = existingRecord.CustomerOrderSlip.Quantity,
                    RemainingVolume = existingRecord.CustomerOrderSlip.BalanceQuantity + existingRecord.Quantity,
                    Price = existingRecord.CustomerOrderSlip.DeliveredPrice,
                    Volume = existingRecord.Quantity,
                    TotalAmount = existingRecord.TotalAmount,
                    Remarks = existingRecord.Remarks,
                    ManualDrNo = existingRecord.ManualDrNo,
                    Freight = existingRecord.Freight,
                    ECC = existingRecord.ECC,
                    DeliveryOption = existingRecord.CustomerOrderSlip.DeliveryOption,
                    HaulerId = existingRecord.HaulerId,
                    Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken),
                    Driver = existingRecord.Driver!,
                    PlateNo = existingRecord.PlateNo!,
                    ATLId = existingRecord.AuthorityToLoadId,
                    ATLNo = existingRecord.AuthorityToLoadNo,
                    HasReceivingReport = existingRecord.HasReceivingReport,
                };

                ViewBag.DeliveryOption = existingRecord.CustomerOrderSlip.DeliveryOption;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);

                    var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliveryReceiptId, cancellationToken);

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    if (viewModel.Freight != existingRecord.Freight || viewModel.IsECCEdited)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var po = await _dbContext.FilpridePurchaseOrders
                                     .Include(x => x.ActualPrices)
                                     .FirstOrDefaultAsync(x => x.PurchaseOrderId == existingRecord.PurchaseOrderId, cancellationToken)
                                 ?? throw new NullReferenceException($"{existingRecord.PurchaseOrderId} not found");

                        var grossMargin = ComputeGrossMargin(existingRecord.CustomerOrderSlip!, po, (viewModel.Freight + viewModel.ECC));

                        var freightDifference = viewModel.Freight + viewModel.ECC - existingRecord.Freight;

                        /// TODO Revise this later for dynamic checking
                        freightDifference = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(freightDifference);

                        var updatedGrossMargin = grossMargin + freightDifference;

                        var message = $"Delivery Receipt ({existingRecord.DeliveryReceiptNo}) has been updated by {existingRecord.CreatedBy!.ToUpper()}. " +
                                      $"The Freight and/or ECC values have been modified. Please review and approve the updated freight charges, " +
                                      $"which changed from {existingRecord.Freight + existingRecord.ECC:N4} to {viewModel.Freight + viewModel.ECC:N4}. " +
                                      $"This update will affect the approved gross margin, changing it from {grossMargin:N4} to {updatedGrossMargin:N4}.";

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
                    }

                    await _unitOfWork.FilprideDeliveryReceipt.UpdateAsync(viewModel, cancellationToken);

                    var authorityToLoad = await _unitOfWork.FilprideAuthorityToLoad
                                              .GetAsync(x => x.AuthorityToLoadId == existingRecord.AuthorityToLoadId, cancellationToken)
                                          ?? throw new NullReferenceException("Authority to load not found");

                    authorityToLoad.HaulerName = existingRecord.HaulerName;
                    authorityToLoad.Freight = existingRecord.Freight;
                    authorityToLoad.Driver = existingRecord.Driver;
                    authorityToLoad.PlateNo = existingRecord.PlateNo;

                    await _unitOfWork.SaveAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt updated successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
                    viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to edit delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = await GetCurrentFilterType();
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                return View(existingRecord);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to preview delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Previewed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (!existingRecord.IsPrinted)
                {
                    existingRecord.IsPrinted = true;
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to print delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Printed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [Authorize(Roles = "OperationManager, Admin, HeadApprover")]
        public async Task<IActionResult> Post(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                existingRecord.PostedBy = _userManager.GetUserName(User);
                existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingRecord.Status = nameof(DRStatus.PendingDelivery);

                FilprideAuditTrail auditTrailBook = new(existingRecord.PostedBy!, $"Approved delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "Delivery receipt approved successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to post delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        public async Task<IActionResult> GetCustomerDetails(int? id)
        {
            if (id == null)
            {
                return Json(null);
            }

            var customerDto = await _unitOfWork.FilprideDeliveryReceipt.MapCustomerToDTO(id, null);

            if (customerDto == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Address = customerDto.CustomerAddress,
                TinNo = customerDto.CustomerTin
            });
        }

        public async Task<IActionResult> GetCustomerOrderSlipList(int customerId, int? deliveryReceiptId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var orderSlips = _dbContext.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => (!cos.IsDelivered &&
                                cos.Status == nameof(CosStatus.Completed)
                               && cos.Company == companyClaims ||
                               cos.Status == nameof(CosStatus.ForDR)) &&
                              cos.BalanceQuantity > 0 &&
                              cos.CustomerId == customerId && cos.Company == companyClaims)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                });

            if (deliveryReceiptId != null)
            {
                var existingCos = _dbContext.FilprideDeliveryReceipts
                    .Include(dr => dr.CustomerOrderSlip)
                    .Where(dr => dr.DeliveryReceiptId == deliveryReceiptId && dr.Company == companyClaims)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.CustomerOrderSlipId.ToString(),
                        Text = dr.CustomerOrderSlip!.CustomerOrderSlipNo
                    });

                orderSlips = orderSlips.Union(existingCos);
            }

            var result = await orderSlips.ToListAsync(cancellationToken);


            return Json(result);
        }

        public async Task<IActionResult> GetCosDetails(int? id, int? initialPoId, int? initialAtlId, decimal? currentVolume)
        {
            if (id == null)
            {
                return Json(null);
            }

            var cosAtlDetails = await _dbContext.FilprideBookAtlDetails
                .Include(x => x.Header)
                .Include(x => x.CustomerOrderSlip)
                .Include(x => x.AppointedSupplier!)
                .ThenInclude(x => x.PurchaseOrder!)
                .ThenInclude(x => x.Supplier)
                .Where(x => x.CustomerOrderSlipId == id)
                .ToListAsync();

            if (!cosAtlDetails.Any())
            {
                return Json(null);
            }

            if (initialPoId != null && currentVolume != null)
            {
                var existingSelection = cosAtlDetails
                    .FirstOrDefault(x => x.AppointedSupplier!.PurchaseOrderId == initialPoId
                                && x.AuthorityToLoadId == initialAtlId);
                existingSelection!.UnservedQuantity += (decimal)currentVolume;
            }

            return Json(new
            {
                Product = cosAtlDetails.First().CustomerOrderSlip!.ProductName,
                cosAtlDetails.First().CustomerOrderSlip!.Quantity,
                RemainingVolume = cosAtlDetails.First().CustomerOrderSlip!.BalanceQuantity,
                Price = cosAtlDetails.First().CustomerOrderSlip!.DeliveredPrice,
                cosAtlDetails.First().CustomerOrderSlip!.DeliveryOption,
                cosAtlDetails.First().CustomerOrderSlip!.Freight,
                PurchaseOrders = cosAtlDetails
                    .Where(a => a.UnservedQuantity > 0 || (initialPoId.HasValue && a.AppointedSupplier!.PurchaseOrderId == initialPoId))
                    .Select(a => new
                    {
                        a.AppointedSupplier!.PurchaseOrderId,
                        a.AppointedSupplier!.PurchaseOrder!.PurchaseOrderNo,
                        a.AppointedSupplier.PurchaseOrder!.Supplier!.SupplierName,
                        a.UnservedQuantity,
                        atlId = a.AuthorityToLoadId,
                        atlNo = a.Header!.AuthorityToLoadNo,
                        IsCurrentlySelected = initialPoId.HasValue && a.AppointedSupplier!.PurchaseOrderId == initialPoId
                    })
            });
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        public async Task<IActionResult> Delivered(int? id, DateOnly deliveredDate, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(cos => cos.DeliveryReceiptId == id, cancellationToken);

            if (existingRecord == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                /// TODO Uncomment this feature once the mobility company is on live
                // if (existingRecord.Customer!.CustomerType == nameof(CustomerType.Retail))
                // {
                //     await _unitOfWork.MobilityReceivingReport
                //         .AutoGenerateReceivingReport(existingRecord, deliveredDate, cancellationToken);
                // }

                existingRecord.DeliveredDate = deliveredDate;
                existingRecord.Status = nameof(DRStatus.ForInvoicing);
                existingRecord.PostedBy = _userManager.GetUserName(User);
                existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region Mark the COS delivered

                var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId, cancellationToken);

                if (existingCos == null)
                {
                    return NotFound();
                }

                if (existingCos.Status == nameof(CosStatus.Completed))
                {
                    existingCos.IsDelivered = true;
                }

                #endregion

                #region--Inventory Recording

                await _unitOfWork.FilprideInventory.AddSalesToInventoryAsync(existingRecord, cancellationToken);

                #endregion

                await _unitOfWork.FilprideDeliveryReceipt.PostAsync(existingRecord, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Mark as delivered the delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Product has been delivered";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to mark delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Marked by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(DRStatus.Canceled);
                    model.CancellationRemarks = cancellationRemarks;
                    await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId, model.Quantity, cancellationToken);
                    await _unitOfWork.FilprideDeliveryReceipt.UpdatePreviousAppointedSupplierAsync(model);

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.SaveAsync(cancellationToken);
                    TempData["success"] = "Delivery Receipt has been canceled.";
                }
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.DeliveryReceiptNo && i.Company == model.Company);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(DRStatus.Voided);

                        if (existingInventory != null)
                        {
                            await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                        }

                        var connectedReceivingReport = await _dbContext.FilprideReceivingReports
                            .FirstOrDefaultAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId && rr.Status == nameof(Status.Posted), cancellationToken);

                        if (connectedReceivingReport != null)
                        {
                            await _unitOfWork.FilprideReceivingReport.VoidReceivingReportAsync(connectedReceivingReport.ReceivingReportId, model.VoidedBy!, cancellationToken);
                        }

                        await _unitOfWork.FilprideDeliveryReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DeliveryReceiptNo, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId, model.Quantity, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.UpdatePreviousAppointedSupplierAsync(model);

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                        await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _unitOfWork.SaveAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Delivery receipt has been Voided.";
                    }
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to void delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }

            return BadRequest();
        }

        public async Task<IActionResult> GenerateExcel(int id)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id);

            if (deliveryReceipt == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId);

            // Get the full path to the template in the wwwroot folder
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "DR Format.xlsx");
            byte[] fileBytes = System.IO.File.ReadAllBytes(templatePath);

            using (var package = new ExcelPackage(new MemoryStream(fileBytes)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // Assuming the template has one sheet

                worksheet.Cells["H2"].Value = deliveryReceipt.AuthorityToLoadNo;
                worksheet.Cells["H7"].Value = receivingReport?.OldRRNo ?? receivingReport?.ReceivingReportNo;
                worksheet.Cells["H9"].Value = deliveryReceipt.ManualDrNo;
                worksheet.Cells["H10"].Value = deliveryReceipt.Date.ToString("dd-MMM-yy");
                worksheet.Cells["H12"].Value = deliveryReceipt.CustomerOrderSlip!.OldCosNo;
                worksheet.Cells["B11"].Value = deliveryReceipt.CustomerOrderSlip.PickUpPoint!.Depot.ToUpper();
                worksheet.Cells["C12"].Value = deliveryReceipt.Customer!.CustomerName.ToUpper();
                worksheet.Cells["C13"].Value = deliveryReceipt.Customer.CustomerAddress.ToUpper();
                worksheet.Cells["B17"].Value = deliveryReceipt.CustomerOrderSlip.Product!.ProductName;
                worksheet.Cells["H17"].Value = deliveryReceipt.Quantity.ToString("N0");
                worksheet.Cells["H19"].Value = deliveryReceipt.Remarks;

                var stream = new MemoryStream();
                package.SaveAs(stream);

                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{deliveryReceipt.DeliveryReceiptNo}.xlsx");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckManualDrNoExists(string manualDrNo, int? drId)
        {
            if (drId.HasValue)
            {
                var existingManualDrNo = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.DeliveryReceiptId == drId)
                    .Select(dr => dr.ManualDrNo)
                    .FirstOrDefaultAsync();

                if (manualDrNo == existingManualDrNo)
                {
                    return Json(false);
                }
            }

            var exists = await _unitOfWork.FilprideDeliveryReceipt.CheckIfManualDrNoExists(manualDrNo);
            return Json(exists);
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> RecordLiftingDate(int id, DateOnly liftingDate, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var receivingReportNo = await _unitOfWork.FilprideReceivingReport
                    .AutoGenerateReceivingReport(model, liftingDate, User.Identity!.Name!, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!,
                    $"Record lifting date of delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                model.HasReceivingReport = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Delivery Receipt lifting date has been recorded successfully. " +
                                      $"RR#{receivingReportNo} has been generated.";

                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to record lifting date. Error: {ErrorMessage}, Stack: {StackTrace}. Recorded by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

        }

        private decimal ComputeGrossMargin(FilprideCustomerOrderSlip cos, FilpridePurchaseOrder po, decimal drFreight = 0)
        {
            var netSellingPrice = cos.VatType == SD.VatType_Vatable
                ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(cos.DeliveredPrice)
                : cos.DeliveredPrice;

            var commission = cos.CommissionRate;

            var freight = 0m;

            if (drFreight == 0)
            {
                freight = cos.VatType == SD.VatType_Vatable
                    ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat((decimal)cos.Freight!)
                    : (decimal)cos.Freight!;
            }
            else
            {
                ///TODO Revise this later for dynamic checking
                freight = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(drFreight);
            }

            bool hasActualPrice = po.ActualPrices != null && po.ActualPrices.Any(x => x.IsApproved);

            var productCost = hasActualPrice
                ? po.ActualPrices!.First().TriggeredPrice
                : po.Price;

            var netProductCost = po.VatType == SD.VatType_Vatable
                ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(productCost)
                : productCost;

            return netSellingPrice -  netProductCost - commission - freight;

        }

    }
}
