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
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_Finance, SD.Department_Marketing, SD.Department_TradeAndSupply, SD.Department_Logistics, SD.Department_CreditAndCollection, SD.Department_Accounting)]
    public class DeliveryReceiptController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IHubContext<NotificationHub> _hubContext;

        private const string FilterTypeClaimType = "DeliveryReceipt.FilterType";

        private readonly ILogger<DeliveryReceiptController> _logger;

        public DeliveryReceiptController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
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

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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
            ViewBag.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDeliveryReceipts([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
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
                if (!string.IsNullOrEmpty(parameters.Search.Value))
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
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    drList = drList
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
                MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken)
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

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            viewModel.MinDate =  await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customerOrderSlip = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                var hauler = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

                if (customerOrderSlip == null)
                {
                    return BadRequest();
                }

                var po = await _dbContext.FilpridePurchaseOrders
                             .Include(x => x.ActualPrices)
                             .FirstOrDefaultAsync(x => x.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken)
                         ?? throw new NullReferenceException($"{viewModel.PurchaseOrderId} not found");

                if (viewModel.Volume > po.Quantity - po.QuantityReceived)
                {
                    throw new ArgumentException($"The inputted quantity exceeds the remaining balance for Purchase Order: " +
                                                $"{po.PurchaseOrderNo}. Contact the TNS department.");
                }

                FilprideDeliveryReceipt model = new()
                {
                    DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken),
                    Type = viewModel.Type,
                    Date = viewModel.Date,
                    CustomerOrderSlipId = viewModel.CustomerOrderSlipId,
                    CustomerId = viewModel.CustomerId,
                    Remarks = viewModel.Remarks,
                    Quantity = viewModel.Volume,
                    TotalAmount = viewModel.Volume * customerOrderSlip.DeliveredPrice,
                    Company = companyClaims,
                    CreatedBy = GetUserFullName(),
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
                    HaulerName = hauler?.SupplierName,
                    HaulerVatType = hauler?.VatType,
                    HaulerTaxType = hauler?.TaxType,
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

                    var grossMargin = ComputeGrossMargin(customerOrderSlip, po);

                    var freightDifference = viewModel.Freight + viewModel.ECC - (decimal)customerOrderSlip.Freight!;

                    freightDifference = hauler?.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(freightDifference)
                        : freightDifference;

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

                authorityToLoad.HaulerName = hauler?.SupplierName;
                authorityToLoad.Freight = viewModel.Freight;
                authorityToLoad.Driver = viewModel.Driver;
                authorityToLoad.PlateNo = viewModel.PlateNo;

                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = $"Delivery receipt #{model.DeliveryReceiptNo} created successfully.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
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
                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken);
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException($"Cannot edit this record because the period {existingRecord.Date:MMM yyyy} is already closed.");
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
                    MinDate = minDate,
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

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                viewModel.CurrentUser = GetUserFullName();

                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == viewModel.DeliveryReceiptId, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                var po = await _dbContext.FilpridePurchaseOrders
                             .Include(x => x.ActualPrices)
                             .FirstOrDefaultAsync(x => x.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken)
                         ?? throw new NullReferenceException($"{viewModel.PurchaseOrderId} not found");

                if (!existingRecord.HasReceivingReport)
                {
                    if (viewModel.Volume > po.Quantity - po.QuantityReceived)
                    {
                        throw new ArgumentException($"The inputted quantity exceeds the remaining balance for Purchase Order: " +
                                                    $"{po.PurchaseOrderNo}. Contact the TNS department.");
                    }
                }

                if (viewModel.Freight != existingRecord.Freight || viewModel.IsECCEdited)
                {
                    var hauler = await _unitOfWork.FilprideSupplier
                        .GetAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

                    var operationManager = await _dbContext.ApplicationUsers
                        .Where(a => a.Position == SD.Position_OperationManager)
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);

                    var grossMargin = ComputeGrossMargin(existingRecord.CustomerOrderSlip!, po, (viewModel.Freight + viewModel.ECC));

                    var freightDifference = viewModel.Freight + viewModel.ECC - existingRecord.Freight;

                    freightDifference = hauler!.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(freightDifference)
                        : freightDifference;

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

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edit delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Delivery receipt updated successfully.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
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

                var companyClaims = await GetCompanyClaimAsync();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", companyClaims!);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var dr = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(x => x.DeliveryReceiptId == id, cancellationToken);

            if (dr == null)
            {
                return NotFound();
            }

            if (!dr.IsPrinted)
            {
                dr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed original copy of delivery receipt# {dr.DeliveryReceiptNo}", "Delivery Receipt", dr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed re-printed copy of delivery receipt# {dr.DeliveryReceiptNo}", "Delivery Receipt", dr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Preview), new { id });
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

                if (existingRecord.IsPrinted)
                {
                    return RedirectToAction(nameof(Preview), new { id });
                }

                existingRecord.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);

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

                existingRecord.Status = nameof(DRStatus.PendingDelivery);

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Approved delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
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
            var orderSlips = (await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAllAsync(cos => (!cos.IsDelivered &&
                                         cos.Status == nameof(CosStatus.Completed)
                                         && cos.Company == companyClaims ||
                                         cos.Status == nameof(CosStatus.ForDR)) &&
                                        cos.BalanceQuantity > 0 &&
                                        cos.CustomerId == customerId && cos.Company == companyClaims, cancellationToken))
                .OrderBy(cos => cos.CustomerOrderSlipId)
                .Select(cos => new SelectListItem
                {
                    Value = cos.CustomerOrderSlipId.ToString(),
                    Text = cos.CustomerOrderSlipNo
                });

            if (deliveryReceiptId != null)
            {
                var existingCos = (await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(dr => dr.DeliveryReceiptId == deliveryReceiptId
                                       && dr.Company == companyClaims, cancellationToken))
                    .Select(dr => new SelectListItem
                    {
                        Value = dr.CustomerOrderSlipId.ToString(),
                        Text = dr.CustomerOrderSlip!.CustomerOrderSlipNo
                    });

                orderSlips = orderSlips.Union(existingCos);
            }

            var result = orderSlips.ToList();


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

            if (cosAtlDetails.Count == 0)
            {
                return Json(null);
            }

            if (initialPoId != null && currentVolume != null)
            {
                var existingSelection = cosAtlDetails
                    .FirstOrDefault(x => x.AppointedSupplier!.PurchaseOrderId == initialPoId
                                && x.AuthorityToLoadId == initialAtlId);

                if (existingSelection != null)
                {
                    existingSelection.UnservedQuantity += (decimal)currentVolume;
                }
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
                existingRecord.PostedBy = GetUserFullName();
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

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var connectedReceivingReport = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId
                                    && rr.Status == nameof(Status.Posted), cancellationToken);

                if (connectedReceivingReport != null)
                {
                    await _unitOfWork.FilprideReceivingReport.VoidReceivingReportAsync(
                        connectedReceivingReport.ReceivingReportId, User.Identity!.Name!, cancellationToken);
                }

                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(DRStatus.Canceled);
                model.CancellationRemarks = cancellationRemarks;
                model.ManualDrNo += "x";
                await _unitOfWork.FilprideDeliveryReceipt.DeductTheVolumeToCos(model.CustomerOrderSlipId,
                    model.Quantity, cancellationToken);
                await _unitOfWork.FilprideDeliveryReceipt.UpdatePreviousAppointedSupplierAsync(model);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!,
                    $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Delivery Receipt has been canceled.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to cancel delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

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
                .FirstOrDefaultAsync(i => i.Reference == model.DeliveryReceiptNo
                                          && i.Company == model.Company, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {

                model.PostedBy = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(DRStatus.Voided);
                model.ManualDrNo += "x";

                if (existingInventory != null)
                {
                    await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                }

                var connectedReceivingReport = await _unitOfWork.FilprideReceivingReport
                    .GetAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId
                                    && rr.Status == nameof(Status.Posted), cancellationToken);

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

        public async Task<IActionResult> GenerateExcel(int id)
        {
            var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id);

            if (deliveryReceipt == null)
            {
                return NotFound();
            }

            var receivingReport = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId);

            // Get the full path to the template in the wwwroot folder
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "DR Format.xlsx");
            var fileBytes = await System.IO.File.ReadAllBytesAsync(templatePath);

            using var package = new ExcelPackage(new MemoryStream(fileBytes));
            var worksheet = package.Workbook.Worksheets[0];

            // Fill in the data
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
            worksheet.Cells["H19"].Value = $"{deliveryReceipt.PurchaseOrder?.PurchaseOrderNo} {deliveryReceipt.Remarks}";

            // === SIMPLE SECURITY PROTECTION ===

            // 1. Set a fixed password for your organization
            const string PROTECTION_PASSWORD = "mis123"; // Change this to your company password

            // 2. Protect the worksheet - prevents editing, deleting, formatting
            worksheet.Protection.SetPassword(PROTECTION_PASSWORD);
            worksheet.Protection.AllowSelectLockedCells = true;   // Users can select cells
            worksheet.Protection.AllowSelectUnlockedCells = true; // Users can select cells
            worksheet.Protection.AllowFormatCells = false;        // No formatting changes
            worksheet.Protection.AllowInsertRows = false;         // No adding rows
            worksheet.Protection.AllowDeleteRows = false;         // No deleting rows
            worksheet.Protection.AllowInsertColumns = false;      // No adding columns
            worksheet.Protection.AllowDeleteColumns = false;      // No deleting columns
            worksheet.Protection.AllowSort = false;               // No sorting
            worksheet.Protection.AllowAutoFilter = false;         // No filtering
            worksheet.View.ShowGridLines = false; // Makes it look more official and professional

            // 3. Add document properties for identification
            package.Workbook.Properties.Author = "Integrated Business System";
            package.Workbook.Properties.Company = "Filpride";
            package.Workbook.Properties.Comments = $"Official DR - Generated: {DateTimeHelper.GetCurrentPhilippineTime():yyyy-MM-dd HH:mm:ss}";

            // 4. Mark as final (shows read-only warning in Excel)
            package.Workbook.Properties.SetCustomPropertyValue("_MarkAsFinal", "true");

            var stream = new MemoryStream();
            await package.SaveAsAsync(stream);
            var content = stream.ToArray();
            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Generated excel file for delivery receipt# {deliveryReceipt.DeliveryReceiptNo}", "Delivery Receipt", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook);

            #endregion --Audit Trail Recording

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{deliveryReceipt.DeliveryReceiptNo}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> CheckManualDrNoExists(string manualDrNo, int? drId)
        {
            if (drId.HasValue)
            {
                var existingDr = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == drId);

                if (manualDrNo == existingDr?.ManualDrNo)
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
                    .AutoGenerateReceivingReport(model, liftingDate, GetUserFullName(), cancellationToken);

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

            var commission = cos.CommissioneeVatType == SD.VatType_Vatable && cos.CommissionRate != 0
                ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(cos.CommissionRate)
                : cos.CommissionRate;

            decimal freight;

            if (drFreight == 0)
            {
                freight = cos.VatType == SD.VatType_Vatable
                    ? cos.Freight != 0
                        ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat((decimal)cos.Freight!)
                        : (decimal)cos.Freight!
                    : (decimal)cos.Freight!;
            }
            else
            {
                freight = cos.VatType == SD.VatType_Vatable
                    ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(drFreight)
                    : drFreight;
            }

            var hasActualPrice = po.ActualPrices != null && po.ActualPrices.Any(x => x.IsApproved);

            var productCost = hasActualPrice
                ? po.ActualPrices!.First().TriggeredPrice
                : po.Price;

            var netProductCost = po.VatType == SD.VatType_Vatable
                ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(productCost)
                : productCost;

            return netSellingPrice -  netProductCost - commission - freight;

        }

        public async Task<IActionResult> GetHaulers(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            return Json(await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken));
        }

        public async Task<IActionResult> GetDeliveryReceiptDetails(int id, CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var dr = await _dbContext.FilprideDeliveryReceipts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                cos => cos.DeliveryReceiptId == id, cancellationToken);

            return Json(dr);
        }

        public async Task<IActionResult> ChangeHaulerFreight (int? id, decimal? freight, string? haulerId, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                var oldHaulerName = existingRecord.HaulerName;
                var oldFreight = existingRecord.Freight;

                var hauler = await _unitOfWork.FilprideSupplier.GetAsync(s => s.SupplierId == int.Parse(haulerId!), cancellationToken);
                if (hauler == null)
                {
                    return NotFound();
                }

                existingRecord.Freight = freight ?? 0m;
                existingRecord.FreightAmount = (freight ?? 0m) * existingRecord.Quantity;
                existingRecord.HaulerId = hauler.SupplierId;
                existingRecord.HaulerName = hauler.SupplierName;
                existingRecord.HaulerVatType = hauler.VatType;
                existingRecord.HaulerTaxType = hauler.TaxType;

                var userName = User.Identity!.Name!;

                FilprideAuditTrail auditTrailBook = new(userName,
                    $"Update hauler/freight for delivery receipt# {existingRecord.DeliveryReceiptNo}, hauler from ({oldHaulerName}) => ({existingRecord.HaulerName}), freight from ({oldFreight}) => ({existingRecord.Freight:N4})",
                    "Delivery Receipt",
                    existingRecord.Company);

                TempData["success"] =
                    $"Hauler/Freight for {existingRecord.DeliveryReceiptNo} has been updated, hauler from ({oldHaulerName}) => ({existingRecord.HaulerName}), freight from ({oldFreight}) => ({existingRecord.Freight:N4})";

                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex,
                    "Failed to change the commission details of the customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Changed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
    }
}
