using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
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

        private static List<DeliveryReceiptDetailInput> NormalizeDetails(DeliveryReceiptViewModel viewModel)
        {
            return viewModel.Details
                .Where(d => d.CustomerOrderSlipId > 0
                            && d.PurchaseOrderId > 0
                            && d.AuthorityToLoadId > 0
                            && d.Quantity > 0)
                .ToList();
        }

        private async Task PopulateDeliveryReceiptViewModelAsync(
            DeliveryReceiptViewModel viewModel,
            string companyClaims,
            CancellationToken cancellationToken)
        {
            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListNotDeliveredAsync(companyClaims, cancellationToken);
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken);
        }

        private async Task<List<int>> ApplyDeliveryReceiptDetailsAsync(
            FilprideDeliveryReceipt model,
            IEnumerable<DeliveryReceiptDetailInput> requestedDetails,
            bool reserveQuantities,
            CancellationToken cancellationToken)
        {
            var detailInputs = requestedDetails.ToList();
            if (!detailInputs.Any())
            {
                throw new ArgumentException("Please add at least one delivery receipt line.");
            }

            var cosIds = detailInputs.Select(d => d.CustomerOrderSlipId).Distinct().ToList();
            var poIds = detailInputs.Select(d => d.PurchaseOrderId).Distinct().ToList();
            var atlIds = detailInputs.Select(d => d.AuthorityToLoadId).Distinct().ToList();

            var cosRecords = await _dbContext.FilprideCustomerOrderSlips
                .Include(c => c.Product)
                .Include(c => c.Customer)
                .Include(c => c.Commissionee)
                .Include(c => c.PickUpPoint)
                .Where(c => cosIds.Contains(c.CustomerOrderSlipId))
                .ToDictionaryAsync(c => c.CustomerOrderSlipId, cancellationToken);

            var poRecords = await _dbContext.FilpridePurchaseOrders
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Where(p => poIds.Contains(p.PurchaseOrderId))
                .ToDictionaryAsync(p => p.PurchaseOrderId, cancellationToken);

            var atlRecords = await _dbContext.FilprideAuthorityToLoads
                .Where(a => atlIds.Contains(a.AuthorityToLoadId))
                .ToDictionaryAsync(a => a.AuthorityToLoadId, cancellationToken);

            var atlDetails = await _dbContext.FilprideBookAtlDetails
                .Include(d => d.AppointedSupplier)
                .Where(d => atlIds.Contains(d.AuthorityToLoadId)
                            && cosIds.Contains(d.CustomerOrderSlipId)
                            && poIds.Contains(d.AppointedSupplier!.PurchaseOrderId))
                .ToListAsync(cancellationToken);

            model.Details.Clear();

            decimal totalQuantity = 0m;
            decimal totalAmount = 0m;
            decimal totalCommission = 0m;

            foreach (var input in detailInputs)
            {
                if (!cosRecords.TryGetValue(input.CustomerOrderSlipId, out var cos))
                {
                    throw new ArgumentException($"COS {input.CustomerOrderSlipId} not found.");
                }

                if (!poRecords.TryGetValue(input.PurchaseOrderId, out var po))
                {
                    throw new ArgumentException($"PO {input.PurchaseOrderId} not found.");
                }

                if (!atlRecords.TryGetValue(input.AuthorityToLoadId, out var atl))
                {
                    throw new ArgumentException($"ATL {input.AuthorityToLoadId} not found.");
                }

                var atlDetail = atlDetails.FirstOrDefault(d =>
                    d.AuthorityToLoadId == input.AuthorityToLoadId &&
                    d.CustomerOrderSlipId == input.CustomerOrderSlipId &&
                    d.AppointedSupplier!.PurchaseOrderId == input.PurchaseOrderId);

                if (atlDetail == null)
                {
                    throw new ArgumentException("No ATL detail found for the selected COS/PO/ATL combination.");
                }

                if (reserveQuantities && input.Quantity > atlDetail.UnservedQuantity)
                {
                    throw new ArgumentException(
                        $"The inputted quantity exceeds the ATL unserved quantity for PO {po.PurchaseOrderNo}.");
                }

                if (input.Quantity > cos.BalanceQuantity)
                {
                    throw new ArgumentException($"The inputted quantity exceeds the remaining balance of COS {cos.CustomerOrderSlipNo}.");
                }

                if (reserveQuantities)
                {
                    atlDetail.UnservedQuantity -= input.Quantity;
                }

                cos.DeliveredQuantity += input.Quantity;
                cos.BalanceQuantity -= input.Quantity;
                if (cos.BalanceQuantity == 0)
                {
                    cos.Status = nameof(CosStatus.Completed);
                }

                var unitPrice = cos.DeliveredPrice;
                var amount = input.Quantity * unitPrice;
                var commissionAmount = input.Quantity * cos.CommissionRate;

                model.Details.Add(new FilprideDeliveryReceiptDetail
                {
                    CustomerOrderSlipId = cos.CustomerOrderSlipId,
                    PurchaseOrderId = po.PurchaseOrderId,
                    AuthorityToLoadId = atl.AuthorityToLoadId,
                    AuthorityToLoadNo = atl.AuthorityToLoadNo,
                    ProductId = cos.ProductId,
                    ProductName = cos.ProductName,
                    Quantity = input.Quantity,
                    UnitPrice = unitPrice,
                    TotalAmount = amount
                });

                totalQuantity += input.Quantity;
                totalAmount += amount;
                totalCommission += commissionAmount;
            }

            var firstDetail = model.Details.First();
            var firstCos = cosRecords[firstDetail.CustomerOrderSlipId];
            var distinctAtlNos = model.Details
                .Select(d => d.AuthorityToLoadNo)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Distinct()
                .ToList();
            var distinctCommissionees = cosRecords.Values
                .Where(c => model.Details.Any(d => d.CustomerOrderSlipId == c.CustomerOrderSlipId))
                .Select(c => c.CommissioneeId)
                .Distinct()
                .ToList();

            model.CustomerOrderSlipId = firstDetail.CustomerOrderSlipId;
            model.PurchaseOrderId = firstDetail.PurchaseOrderId;
            model.AuthorityToLoadId = firstDetail.AuthorityToLoadId;
            model.AuthorityToLoadNo = distinctAtlNos.Count == 1 ? distinctAtlNos[0] : "MULTIPLE";
            model.Quantity = totalQuantity;
            model.TotalAmount = totalAmount;
            model.FreightAmount = totalQuantity * (model.Freight + model.ECC);
            model.CommissionAmount = totalCommission;
            model.CustomerAddress = firstCos.CustomerAddress;
            model.CustomerTin = firstCos.CustomerTin;
            model.CommissioneeId = distinctCommissionees.Count == 1 ? distinctCommissionees[0] : null;
            model.CommissionRate = distinctCommissionees.Count == 1 ? firstCos.CommissionRate : 0m;

            return atlIds;
        }

        private async Task<string> ResolveDeliveryReceiptStatusAsync(
            IEnumerable<DeliveryReceiptDetailInput> requestedDetails,
            decimal drFreight,
            CancellationToken cancellationToken)
        {
            var detailInputs = requestedDetails.ToList();
            if (!detailInputs.Any())
            {
                return nameof(DRStatus.PendingDelivery);
            }

            var cosId = detailInputs.Select(d => d.CustomerOrderSlipId).First();

            var cos = await _dbContext.FilprideCustomerOrderSlips
                .FirstOrDefaultAsync(c => c.CustomerOrderSlipId == cosId, cancellationToken)
                ?? throw new ArgumentException("Customer order slip not found.");

            var cosFreight = cos.Freight ?? 0m;
            var needsOmApproval = cosFreight != drFreight;

            return needsOmApproval
                ? nameof(CosStatus.ForApprovalOfOM)
                : nameof(DRStatus.PendingDelivery);
        }

        private async Task RestoreDeliveryReceiptDetailsAsync(FilprideDeliveryReceipt model, CancellationToken cancellationToken)
        {
            var existingDetails = await _dbContext.FilprideDeliveryReceiptDetails
                .Where(d => d.DeliveryReceiptId == model.DeliveryReceiptId)
                .ToListAsync(cancellationToken);

            var cosIds = existingDetails.Select(d => d.CustomerOrderSlipId).Distinct().ToList();
            var atlIds = existingDetails.Select(d => d.AuthorityToLoadId).Distinct().ToList();
            var poIds = existingDetails.Select(d => d.PurchaseOrderId).Distinct().ToList();

            var cosRecords = await _dbContext.FilprideCustomerOrderSlips
                .Where(c => cosIds.Contains(c.CustomerOrderSlipId))
                .ToDictionaryAsync(c => c.CustomerOrderSlipId, cancellationToken);

            var atlDetails = await _dbContext.FilprideBookAtlDetails
                .Include(d => d.AppointedSupplier)
                .Where(d => atlIds.Contains(d.AuthorityToLoadId)
                            && cosIds.Contains(d.CustomerOrderSlipId)
                            && poIds.Contains(d.AppointedSupplier!.PurchaseOrderId))
                .ToListAsync(cancellationToken);

            foreach (var detail in existingDetails)
            {
                if (cosRecords.TryGetValue(detail.CustomerOrderSlipId, out var cos))
                {
                    if (cos.Status == nameof(CosStatus.Completed))
                    {
                        cos.Status = nameof(CosStatus.ForDR);
                    }

                    cos.DeliveredQuantity -= detail.Quantity;
                    cos.BalanceQuantity += detail.Quantity;
                    cos.IsDelivered = false;
                }

                var atlDetail = atlDetails.FirstOrDefault(d =>
                    d.AuthorityToLoadId == detail.AuthorityToLoadId &&
                    d.CustomerOrderSlipId == detail.CustomerOrderSlipId &&
                    d.AppointedSupplier!.PurchaseOrderId == detail.PurchaseOrderId);

                atlDetail?.UnservedQuantity += detail.Quantity;
            }

            _dbContext.FilprideDeliveryReceiptDetails.RemoveRange(existingDetails);
        }

        private IReadOnlyList<FilprideDeliveryReceiptDetail> GetEffectiveDeliveryReceiptDetails(FilprideDeliveryReceipt model)
        {
            if (model.Details.Any())
            {
                return model.Details.ToList();
            }

            if (model.CustomerOrderSlipId == 0 || model.PurchaseOrderId == null || model.AuthorityToLoadId == 0)
            {
                return Array.Empty<FilprideDeliveryReceiptDetail>();
            }

            return
            [
                new FilprideDeliveryReceiptDetail
                {
                    DeliveryReceiptId = model.DeliveryReceiptId,
                    CustomerOrderSlipId = model.CustomerOrderSlipId,
                    PurchaseOrderId = model.PurchaseOrderId.Value,
                    AuthorityToLoadId = model.AuthorityToLoadId,
                    AuthorityToLoadNo = model.AuthorityToLoadNo,
                    ProductId = model.CustomerOrderSlip?.ProductId ?? model.PurchaseOrder?.ProductId ?? 0,
                    ProductName = model.CustomerOrderSlip?.ProductName ?? model.PurchaseOrder?.ProductName ?? string.Empty,
                    Quantity = model.Quantity,
                    UnitPrice = model.CustomerOrderSlip?.DeliveredPrice ?? 0m,
                    TotalAmount = model.TotalAmount,
                    CustomerOrderSlip = model.CustomerOrderSlip,
                    PurchaseOrder = model.PurchaseOrder,
                    AuthorityToLoad = model.AuthorityToLoad
                }
            ];
        }

        private async Task UpdateAuthorityToLoadsAsync(
            IEnumerable<int> authorityToLoadIds,
            string? haulerName,
            decimal freight,
            string? driver,
            string? plateNo,
            CancellationToken cancellationToken)
        {
            var atlIds = authorityToLoadIds.Distinct().ToList();
            var atls = await _dbContext.FilprideAuthorityToLoads
                .Where(a => atlIds.Contains(a.AuthorityToLoadId))
                .ToListAsync(cancellationToken);

            foreach (var atl in atls)
            {
                atl.HaulerName = haulerName;
                atl.Freight = freight;
                atl.Driver = driver;
                atl.PlateNo = plateNo;
            }
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

                var drList = _unitOfWork.FilprideDeliveryReceipt
                    .GetAllQuery(x => x.Company == companyClaims);

                var totalRecords = await drList.CountAsync(cancellationToken);

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
                    var hasDate = DateOnly.TryParse(searchValue, out var date);

                    drList = drList
                    .Where(s =>
                        s.DeliveryReceiptNo.ToLower().Contains(searchValue) ||
                        (hasDate && s.Date == date) ||
                        s.CustomerOrderSlip!.CustomerName.ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.TotalAmount.ToString().Contains(searchValue) ||
                        s.ManualDrNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip!.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.CustomerOrderSlip!.ProductName.ToLower().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue) ||
                        s.PurchaseOrder!.PurchaseOrderNo!.ToLower().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue) ||
                        s.Freight.ToString().Contains(searchValue) ||
                        s.HaulerName!.ToLower().Contains(searchValue) == true
                        );
                }
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    drList = drList.Where(s => s.Date == filterDate);
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    drList = drList
                        .OrderBy($"{columnName} {sortDirection}");
                }

                var totalFilteredRecords = await drList.CountAsync(cancellationToken);

                var pagedData = await drList
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
                        SupplierAtlNo = _dbContext.FilprideBookAtlDetails
                            .Where(d => d.AuthorityToLoadId == dr.AuthorityToLoadId
                                        && d.CustomerOrderSlipId == dr.CustomerOrderSlipId
                                        && (!dr.PurchaseOrderId.HasValue
                                            || (d.AppointedSupplier != null
                                                && d.AppointedSupplier.PurchaseOrderId == dr.PurchaseOrderId)))
                            .Select(d => d.SupplierAtlNo)
                            .FirstOrDefault(),
                        dr.AuthorityToLoadNo,
                        dr.Freight,
                        dr.HaulerName
                    })
                    .ToListAsync(cancellationToken);

                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalFilteredRecords,
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsCreate))]
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsCreate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await PopulateDeliveryReceiptViewModelAsync(viewModel, companyClaims, cancellationToken);

            var normalizedDetails = NormalizeDetails(viewModel);
            if (!normalizedDetails.Any())
            {
                TempData["warning"] = "Please add at least one delivery receipt line.";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await _dbContext.FilprideCustomers
                    .FirstOrDefaultAsync(c => c.CustomerId == viewModel.CustomerId, cancellationToken);

                var hauler = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

                if (customer == null)
                {
                    return BadRequest();
                }

                FilprideDeliveryReceipt model = new()
                {
                    DeliveryReceiptNo = await _unitOfWork.FilprideDeliveryReceipt.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken),
                    Type = viewModel.Type,
                    Date = viewModel.Date,
                    CustomerId = viewModel.CustomerId,
                    Remarks = viewModel.Remarks,
                    Company = companyClaims,
                    CreatedBy = GetUserFullName(),
                    ManualDrNo = viewModel.ManualDrNo,
                    Freight = viewModel.Freight,
                    ECC = viewModel.ECC,
                    Driver = viewModel.Driver,
                    PlateNo = viewModel.PlateNo,
                    HaulerId = viewModel.HaulerId,
                    HaulerName = hauler?.SupplierName,
                    HaulerVatType = hauler?.VatType,
                    HaulerTaxType = hauler?.TaxType,
                    Status = await ResolveDeliveryReceiptStatusAsync(normalizedDetails, viewModel.Freight, cancellationToken)
                };

                _dbContext.FilprideDeliveryReceipts.Add(model);
                var atlIds = await ApplyDeliveryReceiptDetailsAsync(model, normalizedDetails, reserveQuantities: true, cancellationToken);
                await UpdateAuthorityToLoadsAsync(atlIds, hauler?.SupplierName, viewModel.Freight, viewModel.Driver, viewModel.PlateNo, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsEdit))]
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

                if (await _unitOfWork.IsPeriodPostedAsync(Module.DeliveryReceipt, existingRecord.Date, cancellationToken))
                {
                    throw new ArgumentException($"Cannot edit this record because the period {existingRecord.Date:MMM yyyy} is already closed.");
                }

                var purchaseOrders = await _dbContext.FilprideBookAtlDetails
                    .Include(x => x.Header)
                    .Include(x => x.CustomerOrderSlip)
                    .Include(x => x.AppointedSupplier!)
                    .ThenInclude(x => x.PurchaseOrder!)
                    .ThenInclude(x => x.Supplier)
                    .Where(x => x.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
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
                    PurchaseOrderId = existingRecord.PurchaseOrderId ?? 0,
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
                    Details = existingRecord.Details.Any()
                        ? existingRecord.Details
                            .OrderBy(d => d.ProductName)
                            .Select(d => new DeliveryReceiptDetailInput
                            {
                                CustomerOrderSlipId = d.CustomerOrderSlipId,
                                CustomerOrderSlipNo = d.CustomerOrderSlip?.CustomerOrderSlipNo,
                                PurchaseOrderId = d.PurchaseOrderId,
                                AuthorityToLoadId = d.AuthorityToLoadId,
                                AuthorityToLoadNo = d.AuthorityToLoadNo,
                                ProductName = d.ProductName,
                                Price = d.UnitPrice,
                                Quantity = d.Quantity,
                                Amount = d.TotalAmount
                            })
                            .ToList()
                        : new List<DeliveryReceiptDetailInput>
                        {
                            new()
                            {
                                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                                CustomerOrderSlipNo = existingRecord.CustomerOrderSlip.CustomerOrderSlipNo,
                                PurchaseOrderId = existingRecord.PurchaseOrderId ?? 0,
                                AuthorityToLoadId = existingRecord.AuthorityToLoadId,
                                AuthorityToLoadNo = existingRecord.AuthorityToLoadNo,
                                ProductName = existingRecord.CustomerOrderSlip.Product?.ProductName,
                                Price = existingRecord.CustomerOrderSlip.DeliveredPrice,
                                Quantity = existingRecord.Quantity,
                                Amount = existingRecord.TotalAmount
                            }
                        }
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsEdit))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DeliveryReceiptViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await PopulateDeliveryReceiptViewModelAsync(viewModel, companyClaims, cancellationToken);

            var normalizedDetails = NormalizeDetails(viewModel);
            if (!normalizedDetails.Any())
            {
                TempData["warning"] = "Please add at least one delivery receipt line.";
                return View(viewModel);
            }

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
                if (existingRecord.HasReceivingReport)
                {
                    TempData["error"] = "Cannot edit delivery receipt lines because a receiving report already exists.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }

                await RestoreDeliveryReceiptDetailsAsync(existingRecord, cancellationToken);

                var hauler = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierId == viewModel.HaulerId, cancellationToken);

                existingRecord.Date = viewModel.Date;
                existingRecord.CustomerId = viewModel.CustomerId;
                existingRecord.Remarks = viewModel.Remarks;
                existingRecord.ManualDrNo = viewModel.ManualDrNo;
                existingRecord.Driver = viewModel.Driver;
                existingRecord.PlateNo = viewModel.PlateNo;
                existingRecord.HaulerId = viewModel.HaulerId;
                existingRecord.ECC = viewModel.ECC;
                existingRecord.Freight = viewModel.Freight;
                existingRecord.HaulerName = hauler?.SupplierName;
                existingRecord.HaulerVatType = hauler?.VatType;
                existingRecord.HaulerTaxType = hauler?.TaxType;
                existingRecord.Status = await ResolveDeliveryReceiptStatusAsync(normalizedDetails, viewModel.Freight, cancellationToken);
                existingRecord.EditedBy = viewModel.CurrentUser;
                existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                var atlIds = await ApplyDeliveryReceiptDetailsAsync(existingRecord, normalizedDetails, reserveQuantities: true, cancellationToken);
                await UpdateAuthorityToLoadsAsync(atlIds, hauler?.SupplierName, existingRecord.Freight, existingRecord.Driver, existingRecord.PlateNo, cancellationToken);

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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsPreview))]
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", companyClaims!);
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsPreview))]
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

                FilprideAuditTrail auditTrail = new(GetUserFullName(), $"Printed original copy of delivery receipt# {dr.DeliveryReceiptNo}", "Delivery Receipt", dr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(GetUserFullName(), $"Printed re-printed copy of delivery receipt# {dr.DeliveryReceiptNo}", "Delivery Receipt", dr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Preview), new { id });
        }

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsPreview))]
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

                if (existingRecord.Status == nameof(DRStatus.PendingDelivery))
                {
                    TempData["info"] = "Delivery receipt has already been approved.";
                    return RedirectToAction(nameof(Preview), new { id });
                }

                existingRecord.Status = nameof(DRStatus.PendingDelivery);

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Approved delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
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

        public async Task<IActionResult> GetCosDetails(int? id, int? initialPoId, int? initialAtlId, decimal? currentVolume, int? deliveryReceiptId)
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

            decimal restoredCosQuantity = 0m;

            if (deliveryReceiptId.HasValue)
            {
                var existingDetails = await _dbContext.FilprideDeliveryReceiptDetails
                    .Where(d => d.DeliveryReceiptId == deliveryReceiptId.Value
                                && d.CustomerOrderSlipId == id.Value)
                    .Select(d => new
                    {
                        d.PurchaseOrderId,
                        d.AuthorityToLoadId,
                        d.Quantity
                    })
                    .ToListAsync();

                restoredCosQuantity = existingDetails.Sum(d => d.Quantity);

                foreach (var detail in existingDetails)
                {
                    var existingSelection = cosAtlDetails.FirstOrDefault(x =>
                        x.AppointedSupplier!.PurchaseOrderId == detail.PurchaseOrderId &&
                        x.AuthorityToLoadId == detail.AuthorityToLoadId);

                    if (existingSelection != null)
                    {
                        existingSelection.UnservedQuantity += detail.Quantity;
                    }
                }
            }
            else if (initialPoId != null && currentVolume != null)
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
                RemainingVolume = cosAtlDetails.First().CustomerOrderSlip!.BalanceQuantity + restoredCosQuantity,
                Price = cosAtlDetails.First().CustomerOrderSlip!.DeliveredPrice,
                cosAtlDetails.First().CustomerOrderSlip!.DeliveryOption,
                cosAtlDetails.First().CustomerOrderSlip!.Freight,
                PurchaseOrders = cosAtlDetails
                    .Where(a => a.UnservedQuantity > 0 || (deliveryReceiptId.HasValue &&
                                                           _dbContext.FilprideDeliveryReceiptDetails.Any(d =>
                                                               d.DeliveryReceiptId == deliveryReceiptId.Value &&
                                                               d.CustomerOrderSlipId == id.Value &&
                                                               d.PurchaseOrderId == a.AppointedSupplier!.PurchaseOrderId &&
                                                               d.AuthorityToLoadId == a.AuthorityToLoadId)) ||
                                (initialPoId.HasValue && a.AppointedSupplier!.PurchaseOrderId == initialPoId))
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsMarkAsDelivered))]
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

            if (existingRecord.PostedBy != null || existingRecord.DeliveredDate != null)
            {
                TempData["info"] = "Delivery Receipt has already been marked as delivered.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            var minDate = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()).AddDays(-2);

            if (deliveredDate < minDate && !User.IsInRole("Admin"))
            {
                TempData["error"] = "The selected date cannot be more than 2 days in the past.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingRecord.DeliveredDate = deliveredDate;
                existingRecord.Status = nameof(DRStatus.ForInvoicing);
                existingRecord.PostedBy = GetUserFullName();
                existingRecord.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region Mark the COS delivered

                foreach (var cos in GetEffectiveDeliveryReceiptDetails(existingRecord)
                             .Select(d => d.CustomerOrderSlip)
                             .Where(c => c != null)
                             .DistinctBy(c => c!.CustomerOrderSlipId))
                {
                    if (cos!.Status == nameof(CosStatus.Completed))
                    {
                        cos.IsDelivered = true;
                    }
                }

                #endregion Mark the COS delivered

                #region--Inventory Recording

                await _unitOfWork.FilprideInventory.AddSalesToInventoryAsync(existingRecord, cancellationToken);

                #endregion

                await _unitOfWork.FilprideDeliveryReceipt.PostAsync(existingRecord, cancellationToken);

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Mark as delivered the delivery receipt# {existingRecord.DeliveryReceiptNo}", "Delivery Receipt", existingRecord.Company);
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsCancel))]
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                var connectedReceivingReports = (await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId
                                       && rr.Status == nameof(Status.Posted), cancellationToken))
                    .ToList();

                foreach (var connectedReceivingReport in connectedReceivingReports)
                {
                    await _unitOfWork.FilprideReceivingReport.VoidReceivingReportAsync(
                        connectedReceivingReport.ReceivingReportId, GetUserFullName(), cancellationToken);
                }

                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(DRStatus.Canceled);
                model.CancellationRemarks = cancellationRemarks;
                model.ManualDrNo += "x";
                await RestoreDeliveryReceiptDetailsAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!,
                    $"Canceled delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Delivery Receipt #{model.DeliveryReceiptNo} has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var existingInventories = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .Where(i => i.Reference == model.DeliveryReceiptNo
                            && i.Company == model.Company)
                .ToListAsync(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(DRStatus.Voided);
                model.ManualDrNo += "x";

                foreach (var existingInventory in existingInventories)
                {
                    await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
                }

                var connectedReceivingReports = (await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.DeliveryReceiptId == model.DeliveryReceiptId
                                       && rr.Status == nameof(Status.Posted), cancellationToken))
                    .ToList();

                foreach (var connectedReceivingReport in connectedReceivingReports)
                {
                    await _unitOfWork.FilprideReceivingReport.VoidReceivingReportAsync(connectedReceivingReport.ReceivingReportId, model.VoidedBy!, cancellationToken);
                }

                await _unitOfWork.GeneralLedger.ReverseEntries(model.DeliveryReceiptNo, cancellationToken);
                await RestoreDeliveryReceiptDetailsAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Delivery Receipt #{model.DeliveryReceiptNo} has been voided successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to void delivery receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return Json(new { success = false, message = ex.Message });
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

            var receivingReports = (await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.DeliveryReceiptId == deliveryReceipt.DeliveryReceiptId))
                .OrderBy(rr => rr.Date)
                .ThenBy(rr => rr.ReceivingReportNo)
                .ToList();

            var rrReference = string.Join(", ", receivingReports
                .Select(rr => rr.OldRRNo ?? rr.ReceivingReportNo)
                .Where(rr => !string.IsNullOrWhiteSpace(rr)));

            // Get the full path to the template in the wwwroot folder
            var templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "templates", "DR Format.xlsx");
            var fileBytes = await System.IO.File.ReadAllBytesAsync(templatePath);

            using var package = new ExcelPackage(new MemoryStream(fileBytes));
            var worksheet = package.Workbook.Worksheets[0];

            // Fill in the data
            worksheet.Cells["H2"].Value = deliveryReceipt.AuthorityToLoadNo;
            worksheet.Cells["H7"].Value = rrReference;
            worksheet.Cells["H9"].Value = deliveryReceipt.ManualDrNo;
            worksheet.Cells["H10"].Value = deliveryReceipt.Date.ToString("dd-MMM-yy");
            worksheet.Cells["H12"].Value = deliveryReceipt.CustomerOrderSlip!.OldCosNo;
            worksheet.Cells["B11"].Value = deliveryReceipt.CustomerOrderSlip.PickUpPoint!.Depot.ToUpper();
            worksheet.Cells["C12"].Value = deliveryReceipt.Customer!.CustomerName.ToUpper();
            worksheet.Cells["C13"].Value = deliveryReceipt.Customer.CustomerAddress.ToUpper();
            worksheet.Cells["B17"].Value = deliveryReceipt.CustomerOrderSlip.ProductName;
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

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Generated excel file for delivery receipt# {deliveryReceipt.DeliveryReceiptNo}", "Delivery Receipt", companyClaims!);
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsRecordLiftingDate))]
        [HttpGet]
        public async Task<IActionResult> RecordLiftingDate(int id, DateOnly liftingDate, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDeliveryReceipt
                .GetAsync(dr => dr.DeliveryReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DeliveryReceipt, cancellationToken);

            if (liftingDate < DateOnly.FromDateTime(minDate) && !User.IsInRole("Admin"))
            {
                TempData["error"] = $"The selected date cannot be before {minDate:MM/dd/yyyy}.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            if (model.HasReceivingReport)
            {
                TempData["info"] = "Delivery Receipt lifting date has already been recorded.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var receivingReportNo = await _unitOfWork.FilprideReceivingReport
                    .AutoGenerateReceivingReport(model, liftingDate, GetUserFullName(), cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(),
                    $"Record lifting date of delivery receipt# {model.DeliveryReceiptNo}", "Delivery Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                model.HasReceivingReport = true;
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                TempData["success"] = "Delivery Receipt lifting date has been recorded successfully. " +
                                      $"Generated RR reference(s): {receivingReportNo}.";

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

            return netSellingPrice - netProductCost - commission - freight;
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

        [Authorize(Policy = nameof(DeliveryReceipts.DeliveryReceiptsChangeHaulerFreight))]
        public async Task<IActionResult> ChangeHaulerFreight(int? id, decimal? freight, string? haulerId, CancellationToken cancellationToken)
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
                var userName = GetUserFullName();

                var hauler = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == int.Parse(haulerId!), cancellationToken);

                if (hauler == null)
                {
                    return NotFound();
                }

                var newFreightAmount = (freight ?? 0m) * existingRecord.Quantity;
                var difference = newFreightAmount - existingRecord.FreightAmount;

                existingRecord.Freight = freight ?? 0m;
                existingRecord.FreightAmount = existingRecord.Quantity * (existingRecord.Freight + existingRecord.ECC);
                existingRecord.HaulerId = hauler.SupplierId;
                existingRecord.HaulerName = hauler.SupplierName;
                existingRecord.HaulerVatType = hauler.VatType;
                existingRecord.HaulerTaxType = hauler.TaxType;

                if (existingRecord.DeliveredDate != null)
                {
                    await _unitOfWork.FilprideDeliveryReceipt.CreateEntriesForUpdatingFreight(existingRecord,
                        difference, userName, cancellationToken);
                }

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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReJournalSales(int? month, int? year, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!month.HasValue || !year.HasValue)
                {
                    return BadRequest("Month and year are required.");
                }

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var drs = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(x =>
                            x.Company == companyClaims &&
                            x.VoidedBy == null &&
                            x.CanceledDate == null &&
                            x.DeliveredDate.HasValue &&
                            x.DeliveredDate.Value.Month == month &&
                            x.DeliveredDate.Value.Year == year,
                        cancellationToken);

                if (!drs.Any())
                {
                    return Json(new { sucess = true, message = "No records were returned." });
                }

                var drReferences = drs
                    .Select(x => x.DeliveryReceiptNo)
                    .Distinct()
                    .ToList();

                var existingGlEntries = await _dbContext.FilprideGeneralLedgerBooks
                    .Where(x => x.Company == companyClaims && drReferences.Contains(x.Reference))
                    .ToListAsync(cancellationToken);

                if (existingGlEntries.Count != 0)
                {
                    _dbContext.FilprideGeneralLedgerBooks.RemoveRange(existingGlEntries);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                foreach (var dr in drs
                             .OrderBy(x => x.DeliveredDate))
                {
                    await _unitOfWork.FilprideDeliveryReceipt.PostAsync(dr, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return Json(new { month, year, count = drs.Count() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
