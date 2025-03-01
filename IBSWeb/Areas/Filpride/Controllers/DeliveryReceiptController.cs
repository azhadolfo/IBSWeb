using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public DeliveryReceiptController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
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
                            drList = drList.Where(cos =>
                                cos.Status == nameof(CosStatus.ForApprovalOfOM));
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
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
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
                CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
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
                        AuthorityToLoadNo = customerOrderSlip.AuthorityToLoadNo,
                        CommissioneeId = customerOrderSlip.CommissioneeId,
                        CommissionRate = customerOrderSlip.CommissionRate,
                        CommissionAmount = viewModel.Volume * customerOrderSlip.CommissionRate,
                    };

                    customerOrderSlip.DeliveredQuantity += model.Quantity;
                    customerOrderSlip.BalanceQuantity -= model.Quantity;

                    if (customerOrderSlip.BalanceQuantity <= 0)
                    {
                        customerOrderSlip.Status = nameof(CosStatus.Completed);
                    }

                    if (!customerOrderSlip.HasMultiplePO)
                    {
                        model.PurchaseOrderId = customerOrderSlip.PurchaseOrderId;
                    }
                    else
                    {
                        await _unitOfWork.FilprideDeliveryReceipt.AssignNewPurchaseOrderAsync(viewModel, model);
                    }

                    //TODO Remove this in the future
                    #region Remove this in the future

                    if (string.IsNullOrEmpty(viewModel.ATLNo))
                    {
                        model.Status = "Draft";
                    }

                    #endregion

                    await _unitOfWork.FilprideDeliveryReceipt.AddAsync(model, cancellationToken);

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    if (viewModel.IsECCEdited)
                    {
                        var operationManager = await _dbContext.ApplicationUsers
                            .Where(a => a.Position == SD.Position_OperationManager)
                            .ToListAsync(cancellationToken);

                        foreach (var user in operationManager)
                        {
                            var message = $"{model.DeliveryReceiptNo} has been generated and includes an ECC entry created by {model.CreatedBy.ToUpper()}. Please review and approve.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                                .Where(h => h.UserName == user.UserName)
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
                        var tnsUser = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_TradeAndSupply)
                            .ToListAsync(cancellationToken);

                        foreach (var user in tnsUser)
                        {
                            var message = $"Please be informed that {model.CreatedBy.ToUpper()} has updated the 'Driver' and 'Plate#' for {model.DeliveryReceiptNo}.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                           .Where(h => h.UserName == user.UserName)
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
                            .ToListAsync(cancellationToken);

                        var hauler = await _unitOfWork.FilprideSupplier
                            .GetAsync(h => h.SupplierId == customerOrderSlip.HaulerId, cancellationToken);

                        foreach (var user in operationManager)
                        {
                            var message = $"{model.DeliveryReceiptNo} has been generated and the Hauler/Freight has been modified by {model.CreatedBy.ToUpper()}." +
                                          $" Please review and approve the changes from '{customerOrderSlip.Hauler.SupplierName}' with a freight cost of '{customerOrderSlip.Freight:N4}'" +
                                          $" to '{hauler.SupplierName}' with a freight cost of '{model.Freight:N4}'.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                                .Where(h => h.UserName == user.UserName)
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

                DeliveryReceiptViewModel viewModel = new()
                {
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    Date = existingRecord.Date,
                    CustomerId = existingRecord.Customer.CustomerId,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                    CustomerAddress = existingRecord.Customer.CustomerAddress,
                    CustomerTin = existingRecord.Customer.CustomerTin,
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(cancellationToken),
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
                    ATLNo = existingRecord.AuthorityToLoadNo
                };

                ViewBag.DeliveryOption = existingRecord.CustomerOrderSlip.DeliveryOption;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
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
                            .ToListAsync(cancellationToken);

                        foreach (var user in operationManager)
                        {
                            var message = $"{existingRecord.DeliveryReceiptNo} has been modified and includes an ECC entry created by {viewModel.CurrentUser.ToUpper()}. Please review and approve.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                                .Where(h => h.UserName == user.UserName)
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
                        var tnsUser = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_TradeAndSupply)
                            .ToListAsync(cancellationToken);

                        foreach (var user in tnsUser)
                        {
                            var message = $"Please be informed that {viewModel.CurrentUser.ToUpper()} has updated the 'Driver' and 'Plate#' for {existingRecord.DeliveryReceiptNo}.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                           .Where(h => h.UserName == user.UserName)
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
                            .ToListAsync(cancellationToken);

                        var hauler = await _unitOfWork.FilprideSupplier
                            .GetAsync(h => h.SupplierId == viewModel.HaulerId, cancellationToken);

                        foreach (var user in operationManager)
                        {
                            var message = $"{existingRecord.DeliveryReceiptNo} has been modified by {viewModel.CurrentUser.ToUpper()} to update the Hauler/Freight." +
                                          $" Please review and approve the changes from '{existingRecord.Hauler.SupplierName}' with a freight cost of '{existingRecord.Freight:N4}'" +
                                          $" to '{hauler.SupplierName}' with a freight cost of '{viewModel.Freight:N4}'.";

                            await _unitOfWork.Notifications.AddNotificationAsync(user.Id, message);

                            var hubConnections = await _dbContext.HubConnections
                                .Where(h => h.UserName == user.UserName)
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

        public async Task<IActionResult> GetCosDetails(int? id)
        {
            if (id == null)
            {
                return Json(null);
            }

            var cos = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(cos => cos.CustomerOrderSlipId == id);

            if (cos == null)
            {
                return Json(null);
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
                cos.Freight
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

                await _unitOfWork.FilprideReceivingReport.AutoGenerateReceivingReport(existingRecord, cancellationToken);

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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

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

                        if (model.CustomerOrderSlip.HasMultiplePO)
                        {
                            await _unitOfWork.FilprideDeliveryReceipt.UpdatePreviousAppointedSupplierAsync(model);
                        }

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
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
            }

            return BadRequest();
        }

        public async Task<IActionResult> BookAuthorityToLoad(int id, string? supplierAtlNo, DateOnly bookedDate, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (existingRecord == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                FilprideAuthorityToLoad model = new()
                {
                    AuthorityToLoadNo = await _unitOfWork.FilprideAuthorityToLoad.GenerateAtlNo(cancellationToken),
                    DeliveryReceiptId = existingRecord.DeliveryReceiptId,
                    DateBooked = bookedDate,
                    ValidUntil = bookedDate.AddDays(5),
                    UppiAtlNo = supplierAtlNo,
                    Remarks = "Please secure delivery documents. FILPRIDE DR / SUPPLIER DR / WITHDRAWAL CERTIFICATE",
                    CreatedBy = _userManager.GetUserName(User),
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                };

                await _unitOfWork.FilprideAuthorityToLoad.AddAsync(model, cancellationToken);

                existingRecord.AuthorityToLoadNo = model.AuthorityToLoadNo;
                existingRecord.Status = nameof(DRStatus.PendingDelivery);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User), $"Book ATL for delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", ipAddress, existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "ATL booked successfully";
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(AuthorityToLoadController.Print), "AuthorityToLoad", new { area = nameof(Filpride), id = model.AuthorityToLoadId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
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
    }
}
