using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
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

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
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

        private async Task<string> GetCurrentFilterType()
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
                                !dr.HasReceivingReport);
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
                        s.Customer.CustomerName?.ToLower().Contains(searchValue) == true ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.TotalAmount.ToString().Contains(searchValue) ||
                        s.ManualDrNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip.Product.ProductName.ToLower().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(searchValue) ||
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

                    drList = drList
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = drList.Count();

                var pagedData = drList
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
            var companyClaims = await GetCompanyClaimAsync();
            var isDrLock = await _dbContext.AppSettings
                .Where(s => s.SettingKey == AppSettingKey.LockTheCreationOfDr)
                .Select(s => s.Value == "true")
                .FirstOrDefaultAsync(cancellationToken);

            if (isDrLock)
            {
                TempData["denied"] = "Creation of the DR is locked due to incomplete in-transit deliveries.";
                return RedirectToAction(nameof(Index));
            }

            DeliveryReceiptViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var customerOrderSlip = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

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
                        AuthorityToLoadNo = viewModel.ATLNo,
                        CommissioneeId = customerOrderSlip.CommissioneeId,
                        CommissionRate = customerOrderSlip.CommissionRate,
                        CommissionAmount = viewModel.Volume * customerOrderSlip.CommissionRate,
                        CustomerAddress = customerOrderSlip.CustomerAddress,
                        CustomerTin = customerOrderSlip.CustomerTin,
                    };

                    customerOrderSlip.DeliveredQuantity += model.Quantity;
                    customerOrderSlip.BalanceQuantity -= model.Quantity;

                    if (customerOrderSlip.BalanceQuantity <= 0)
                    {
                        customerOrderSlip.Status = nameof(CosStatus.Completed);
                    }

                    await _unitOfWork.FilprideDeliveryReceipt.AssignNewPurchaseOrderAsync(viewModel, model);

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    if (viewModel.IsECCEdited)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"{model.DeliveryReceiptNo} has been generated and includes an ECC entry created by {model.CreatedBy.ToUpper()}. " +
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

                        model.Status = nameof(DRStatus.ForApprovalOfOM);
                    }

                    if ((viewModel.Driver != customerOrderSlip.Driver || viewModel.PlateNo != customerOrderSlip.PlateNo) && !string.IsNullOrEmpty(viewModel.ATLNo))
                    {
                        var tnsUsers = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_TradeAndSupply)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"Please be informed that {model.CreatedBy.ToUpper()} has updated the 'Driver' and 'Plate#' for {model.DeliveryReceiptNo}.";

                        await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(tnsUsers, message);

                        var usernames = await _dbContext.ApplicationUsers
                            .Where(a => tnsUsers.Contains(a.Id))
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

                    if ((viewModel.HaulerId != customerOrderSlip.HaulerId || viewModel.Freight != customerOrderSlip.Freight) && !string.IsNullOrEmpty(viewModel.ATLNo))
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var hauler = await _unitOfWork.FilprideSupplier
                            .GetAsync(h => h.SupplierId == viewModel.HaulerId, cancellationToken);

                        var message = $"{model.DeliveryReceiptNo} has been generated and the Hauler/Freight has been modified by {model.CreatedBy.ToUpper()}." +
                                      $" Please review and approve the changes from '{customerOrderSlip.Hauler.SupplierName}' with a freight cost of '{customerOrderSlip.Freight:N4}'" +
                                      $" to '{hauler.SupplierName}' with a freight cost of '{model.Freight:N4}'.";

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

                    await _unitOfWork.SaveAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt created successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to create delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                var purchaseOrders = await _dbContext.FilprideCOSAppointedSuppliers
                    .Include(a => a.PurchaseOrder)
                    .Include(a => a.Supplier)
                    .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                    .Select(a => new SelectListItem
                    {
                        Value = a.PurchaseOrderId.ToString(),
                        Text = $"{a.PurchaseOrder.PurchaseOrderNo} - {a.Supplier.SupplierName} (Unserved: {a.UnservedQuantity})"
                    })
                    .ToListAsync();

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    CustomerId = existingRecord.Customer.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                    CustomerAddress = existingRecord.CustomerAddress,
                    CustomerTin = existingRecord.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
                    PurchaseOrderId = (int)existingRecord.PurchaseOrderId,
                    PurchaseOrders = purchaseOrders,
                    Product = existingRecord.CustomerOrderSlip.Product.ProductName,
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
                    Driver = existingRecord.Driver,
                    PlateNo = existingRecord.PlateNo,
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
        public async Task<IActionResult> Edit(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    viewModel.CurrentUser = _userManager.GetUserName(User);

                    var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliveryReceiptId, cancellationToken);

                    if (viewModel.IsECCEdited)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"{existingRecord.DeliveryReceiptNo} has been modified and includes an ECC entry created by {viewModel.CurrentUser.ToUpper()}. " +
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
                    }

                    if (viewModel.Driver != existingRecord.Driver || viewModel.PlateNo != existingRecord.PlateNo)
                    {
                        var tnsUsers = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_TradeAndSupply)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"Please be informed that {viewModel.CurrentUser.ToUpper()} has updated the 'Driver' and 'Plate#' for {existingRecord.DeliveryReceiptNo}.";

                        await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(tnsUsers, message);

                        var usernames = await _dbContext.ApplicationUsers
                            .Where(a => tnsUsers.Contains(a.Id))
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

                    if (viewModel.HaulerId != existingRecord.HaulerId || viewModel.Freight != existingRecord.Freight)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var hauler = await _unitOfWork.FilprideSupplier
                            .GetAsync(h => h.SupplierId == viewModel.HaulerId, cancellationToken);

                        var message = $"{existingRecord.DeliveryReceiptNo} has been modified by {viewModel.CurrentUser.ToUpper()} to update the Hauler/Freight." +
                                      $" Please review and approve the changes from '{existingRecord.Hauler.SupplierName}' with a freight cost of '{existingRecord.Freight:N4}'" +
                                      $" to '{hauler.SupplierName}' with a freight cost of '{viewModel.Freight:N4}'.";

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

                    await transaction.CommitAsync(cancellationToken);

                    TempData["success"] = "Delivery receipt updated successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to edit delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
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

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(existingRecord.PostedBy, $"Approved delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
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
            var orderSlips = _dbContext.FilprideCustomerOrderSlips
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Where(cos => ((!cos.IsDelivered &&
                                cos.Status == nameof(CosStatus.Completed)) ||
                               cos.Status == nameof(CosStatus.ForDR)) &&
                              cos.BalanceQuantity > 0 &&
                              cos.CustomerId == customerId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                });

            if (deliveryReceiptId != null)
            {
                var existingCos = _dbContext.FilprideDeliveryReceipts
                    .Include(dr => dr.CustomerOrderSlip)
                    .Where(dr => dr.DeliveryReceiptId == deliveryReceiptId)
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.CustomerOrderSlipId.ToString(),
                        Text = dr.CustomerOrderSlip.CustomerOrderSlipNo
                    });

                orderSlips = orderSlips.Union(existingCos);
            }

            var result = await orderSlips.ToListAsync(cancellationToken);


            return Json(result);
        }

        public async Task<IActionResult> GetCosDetails(int? id, int? initialPoId, decimal? currentVolume)
        {
            if (id == null)
            {
                return Json(null);
            }

            var cos = await _dbContext.FilprideCustomerOrderSlips
                .Include(cos => cos.Product)
                .Include(cos => cos.AppointedSuppliers)
                .ThenInclude(a => a.Supplier)
                .Include(cos => cos.AppointedSuppliers)
                .ThenInclude(a => a.PurchaseOrder)
                .FirstOrDefaultAsync(cos => cos.CustomerOrderSlipId == id);

            if (cos == null)
            {
                return Json(null);
            }

            if (initialPoId != null && currentVolume != null)
            {
                var existingSelection = cos.AppointedSuppliers.FirstOrDefault(sp => sp.PurchaseOrderId == initialPoId);
                existingSelection.UnservedQuantity += (decimal)currentVolume;
            }

            return Json(new
            {
                Product = cos.Product.ProductName,
                cos.Quantity,
                RemainingVolume = cos.BalanceQuantity,
                Price = cos.DeliveredPrice,
                cos.DeliveryOption,
                ATLNo = cos.AuthorityToLoadNo,
                cos.HaulerId,
                cos.Driver,
                cos.PlateNo,
                cos.Freight,
                PurchaseOrders = cos.AppointedSuppliers
                    .Where(a => a.UnservedQuantity > 0 || (initialPoId.HasValue && a.PurchaseOrderId == initialPoId))
                    .Select(a => new
                    {
                        a.PurchaseOrderId,
                        a.PurchaseOrder.PurchaseOrderNo,
                        a.Supplier.SupplierName,
                        a.UnservedQuantity,
                        a.AtlNo,
                        IsCurrentlySelected = initialPoId.HasValue && a.PurchaseOrderId == initialPoId
                    })
            });
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        public async Task<IActionResult> Delivered(int? id, string deliveredDate, CancellationToken cancellationToken)
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
                existingRecord.DeliveredDate = DateOnly.Parse(deliveredDate);
                existingRecord.Status = nameof(DRStatus.ForInvoicing);
                existingRecord.PostedBy = _userManager.GetUserName(User);
                existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region Mark the COS delivered

                var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId, cancellationToken);

                if (existingCos.Status == nameof(CosStatus.Completed))
                {
                    existingCos.IsDelivered = true;
                }

                #endregion

                #region--Inventory Recording

                await _unitOfWork.FilprideInventory.AddSalesToInventoryAsync(existingRecord, cancellationToken);

                #endregion

                await _unitOfWork.FilprideDeliveryReceipt.PostAsync(existingRecord, cancellationToken);


                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Mark as delivered the delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
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

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
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
            var model = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

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
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                        if (existingInventory != null)
                        {
                            await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                        }

                        var connectedReceivingReport = await _dbContext.FilprideReceivingReports
                            .FirstOrDefaultAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId && rr.Status == nameof(Status.Posted), cancellationToken);

                        if (connectedReceivingReport != null)
                        {
                            await _unitOfWork.FilprideReceivingReport.VoidReceivingReportAsync(connectedReceivingReport.ReceivingReportId, model.VoidedBy, ipAddress, cancellationToken);
                        }

                        await _unitOfWork.FilprideDeliveryReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DeliveryReceiptNo, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId, model.Quantity, cancellationToken);
                        await _unitOfWork.FilprideDeliveryReceipt.UpdatePreviousAppointedSupplierAsync(model);

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
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
                worksheet.Cells["H12"].Value = deliveryReceipt.CustomerOrderSlip.OldCosNo;
                worksheet.Cells["B11"].Value = deliveryReceipt.CustomerOrderSlip.PickUpPoint.Depot.ToUpper();
                worksheet.Cells["C12"].Value = deliveryReceipt.Customer.CustomerName.ToUpper();
                worksheet.Cells["C13"].Value = deliveryReceipt.Customer.CustomerAddress.ToUpper();
                worksheet.Cells["B17"].Value = deliveryReceipt.CustomerOrderSlip.Product.ProductName;
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
                    .AutoGenerateReceivingReport(model, liftingDate, cancellationToken);

                #region --Audit Trail Recording

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(User.Identity.Name,
                    $"Record lifting date of delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt",
                    ipAddress, model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                model.HasReceivingReport = true;
                await _unitOfWork.SaveAsync(cancellationToken);

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

    }
}
