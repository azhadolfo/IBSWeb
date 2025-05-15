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

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_RCD, SD.Department_Finance, SD.Department_Marketing, SD.Department_TradeAndSupply, SD.Department_Logistics, SD.Department_CreditAndCollection)]
    public class CustomerOrderSlipController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IHubContext<NotificationHub> _hubContext;

        private const string FilterTypeClaimType = "CustomerOrderSlip.FilterType";

        private readonly ILogger<CustomerOrderSlipController> _logger;

        public CustomerOrderSlipController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IHubContext<NotificationHub> hubContext,
            ILogger<CustomerOrderSlipController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
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
            ViewBag.FilterType = await GetCurrentFilterType();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCustomerOrderSlips([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var filterTypeClaim = await GetCurrentFilterType();

                var query = _dbContext.FilprideCustomerOrderSlips
                    .Include(cos => cos.Customer)
                    .Include(cos => cos.Hauler)
                    .Include(cos => cos.Product)
                    .Include(cos => cos.Supplier)
                    .Include(cos => cos.PickUpPoint)
                    .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Product)
                    .Include(cos => cos.PurchaseOrder).ThenInclude(po => po!.Supplier)
                    .Include(cos => cos.AppointedSuppliers)
                    .Where(cos => cos.Company == companyClaims);

                // Apply status filter based on filterType
                if (!string.IsNullOrEmpty(filterTypeClaim))
                {
                    switch (filterTypeClaim)
                    {
                        case "ForAppointSupplier":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.HaulerAppointed) ||
                                cos.Status == nameof(CosStatus.Created));
                            break;
                        case "ForAppointHauler":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.SupplierAppointed) ||
                                cos.Status == nameof(CosStatus.Created));
                            break;
                        case "ForATLBooking":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForAtlBooking));
                            break;
                        case "ForOMApproval":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForApprovalOfOM));
                            break;
                        case "ForFMApproval":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForApprovalOfFM));
                            break;
                        case "ForDR":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForDR));
                            break;
                        // Add other cases as needed
                    }
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    // Try to parse the searchValue into a DateOnly
                    bool isDateSearch = DateOnly.TryParse(searchValue, out var searchDate);

                    query = query.Where(s =>
                        s.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.OldCosNo.ToLower().Contains(searchValue) ||
                        (s.PurchaseOrder != null && s.PurchaseOrder.PurchaseOrderNo!.ToLower().Contains(searchValue)) ||
                        (s.Customer != null && s.Customer.CustomerName.ToLower().Contains(searchValue)) ||
                        (isDateSearch && s.Date == searchDate) ||
                        (s.PickUpPoint != null && s.PickUpPoint.Depot.ToLower().Contains(searchValue)) ||
                        s.Product!.ProductName.ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.TotalAmount.ToString().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue));
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    query = query.OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();

                // Apply pagination and project to a lighter DTO
                var pagedData = await query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(cos => new {
                        cos.CustomerOrderSlipId,
                        cos.CustomerOrderSlipNo,
                        cos.OldCosNo,
                        cos.PurchaseOrderId,
                        cos.PurchaseOrder!.PurchaseOrderNo,
                        cos.PickUpPoint!.Depot,
                        cos.PickUpPointId,
                        cos.Date,
                        cos.Customer!.CustomerName,
                        cos.Product!.ProductName,
                        cos.Quantity,
                        cos.TotalAmount,
                        cos.Status,
                        cos.SupplierId,
                        cos.Driver,
                        cos.PlateNo,
                        cos.BalanceQuantity,
                        // Extract only PurchaseOrderNos from AppointedSuppliers
                        AppointedSupplierPOs = cos.AppointedSuppliers!
                            .Select(a => a.PurchaseOrder!.PurchaseOrderNo)
                            .ToList(),
                    })
                    .ToListAsync(cancellationToken);

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
                _logger.LogError(ex, "Failed to get customer order slips. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [DepartmentAuthorize(SD.Department_Marketing, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            CustomerOrderSlipViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
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
                    FilprideCustomerOrderSlip model = new()
                    {
                        CustomerOrderSlipNo = await _unitOfWork.FilprideCustomerOrderSlip.GenerateCodeAsync(companyClaims, cancellationToken),
                        Date = viewModel.Date,
                        CustomerId = viewModel.CustomerId,
                        CustomerAddress = viewModel.CustomerAddress!,
                        CustomerTin = viewModel.TinNo!,
                        CustomerPoNo = viewModel.CustomerPoNo,
                        Quantity = viewModel.Quantity,
                        BalanceQuantity = viewModel.Quantity,
                        DeliveredPrice = viewModel.DeliveredPrice,
                        TotalAmount = viewModel.TotalAmount,
                        AccountSpecialist = viewModel.AccountSpecialist,
                        Remarks = viewModel.Remarks,
                        Company = companyClaims,
                        CreatedBy = _userManager.GetUserName(User),
                        ProductId = viewModel.ProductId,
                        Status = nameof(CosStatus.Created),
                        OldCosNo = viewModel.OtcCosNo,
                        Terms = viewModel.Terms,
                        Branch = viewModel.SelectedBranch,
                        CustomerType = viewModel.CustomerType!,
                    };

                    if (model.Branch != null)
                    {
                        var branch = await _dbContext.FilprideCustomerBranches
                            .Where(b => b.BranchName == model.Branch)
                            .FirstOrDefaultAsync(cancellationToken);

                        model.CustomerAddress = branch!.BranchAddress;
                        model.CustomerTin = branch.BranchTin;
                    }

                    if (viewModel.HasCommission)
                    {
                        model.HasCommission = viewModel.HasCommission;
                        model.CommissioneeId = viewModel.CommissioneeId;
                        model.CommissionRate = viewModel.CommissionRate;
                    }

                    await _unitOfWork.FilprideCustomerOrderSlip.AddAsync(model, cancellationToken);

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new customer order slip# {model.CustomerOrderSlipNo}", "Customer Order Slip", model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Customer order slip created successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to create customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_Marketing, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> EditCos(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var exisitingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (exisitingRecord == null)
                {
                    return BadRequest();
                }

                var getPurchaseOrder = await _unitOfWork.MobilityPurchaseOrder.GetAsync(p => p.PurchaseOrderNo == exisitingRecord.CustomerPoNo, cancellationToken);

                CustomerOrderSlipViewModel viewModel = new()
                {
                    CustomerOrderSlipId = exisitingRecord.CustomerOrderSlipId,
                    Date = exisitingRecord.Date,
                    CustomerId = exisitingRecord.CustomerId,
                    CustomerAddress = exisitingRecord.CustomerAddress,
                    TinNo = exisitingRecord.CustomerTin,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                    HasCommission = exisitingRecord.HasCommission,
                    CommissioneeId = exisitingRecord.CommissioneeId,
                    Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                    CommissionRate = exisitingRecord.CommissionRate,
                    CustomerPoNo = exisitingRecord.CustomerPoNo,
                    Quantity = exisitingRecord.Quantity,
                    DeliveredPrice = exisitingRecord.DeliveredPrice,
                    Vat = _unitOfWork.FilprideCustomerOrderSlip.ComputeVatAmount((exisitingRecord.TotalAmount / 1.12m)),
                    TotalAmount = exisitingRecord.TotalAmount,
                    ProductId = exisitingRecord.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    AccountSpecialist = exisitingRecord.AccountSpecialist,
                    Remarks = exisitingRecord.Remarks,
                    OtcCosNo = exisitingRecord.OldCosNo,
                    Status = exisitingRecord.Status,
                    Terms = exisitingRecord.Terms,
                    Branches = await _unitOfWork.FilprideCustomer
                        .GetCustomerBranchesSelectListAsync(exisitingRecord.CustomerId, cancellationToken),
                    SelectedBranch = exisitingRecord.Branch,
                    CustomerType = exisitingRecord.CustomerType,
                    StationCode = getPurchaseOrder.StationCode,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCos(CustomerOrderSlipViewModel viewModel, CancellationToken cancellationToken)
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
                    var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);
                    viewModel.CurrentUser = _userManager.GetUserName(User);

                    if (string.IsNullOrEmpty(viewModel.Terms))
                    {
                        var customer = await _unitOfWork.FilprideCustomer
                            .GetAsync(cos => cos.CustomerId == viewModel.CustomerId, cancellationToken);

                        viewModel.Terms = customer.CustomerTerms;
                    }

                    if (viewModel.ProductId == 0)
                    {
                        viewModel.ProductId = existingRecord.ProductId;
                    }

                    var changes = new List<string>();

                    if (existingRecord.Date != viewModel.Date)
                    {
                        changes.Add("Order Date was updated.");
                    }

                    if (existingRecord.ProductId != viewModel.ProductId)
                    {
                        changes.Add("Product was updated.");
                    }

                    if (existingRecord.OldCosNo != viewModel.OtcCosNo)
                    {
                        changes.Add("OTC COS# was updated.");
                    }
                    if (existingRecord.CustomerId != viewModel.CustomerId)
                    {
                        changes.Add("Customer was updated.");
                    }
                    if (existingRecord.DeliveredPrice != viewModel.DeliveredPrice)
                    {
                        changes.Add("Delivered Price was updated.");
                    }
                    if (existingRecord.CustomerPoNo != viewModel.CustomerPoNo)
                    {
                        changes.Add("Customer PO# was updated.");
                    }
                    if (existingRecord.HasCommission != viewModel.HasCommission)
                    {
                        changes.Add("Commission status was updated.");
                    }
                    if (existingRecord.CommissioneeId != viewModel.CommissioneeId)
                    {
                        changes.Add("Commissionee was updated.");
                    }
                    if (existingRecord.CommissionRate != viewModel.CommissionRate)
                    {
                        changes.Add("Commission Rate was updated.");
                    }
                    if (existingRecord.AccountSpecialist != viewModel.AccountSpecialist)
                    {
                        changes.Add("Account Specialist was updated.");
                    }
                    if (existingRecord.Remarks != viewModel.Remarks)
                    {
                        changes.Add("Remarks were updated.");
                    }

                    if (existingRecord.Branch != viewModel.SelectedBranch)
                    {
                        changes.Add("Branch was updated.");
                    }

                    if (existingRecord.Terms != viewModel.Terms)
                    {
                        changes.Add("Terms was updated.");
                    }

                    await _unitOfWork.FilprideCustomerOrderSlip.UpdateAsync(viewModel, cancellationToken);

                    if (existingRecord.AuthorityToLoadNo != null && changes.Count > 0)
                    {
                        var tnsAndLogisticUsers = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_TradeAndSupply || a.Department == SD.Department_Logistics)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"{viewModel.CurrentUser!.ToUpper()} has modified {existingRecord.CustomerOrderSlipNo}. Updates include:\n{string.Join("\n", changes)}";
                        message += $"\nKindly reappoint the supplier/hauler, if necessary.";

                        await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(tnsAndLogisticUsers, message);

                        var usernames = await _dbContext.ApplicationUsers
                            .Where(a => tnsAndLogisticUsers.Contains(a.Id))
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

                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Customer order slip updated successfully.";
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
                    viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
                    viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to edit customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
                return NotFound();

            try
            {
                var customerOrderSlip = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (customerOrderSlip == null)
                    return BadRequest();

                // Create the view model with basic information
                var model = CreateBaseViewModel(customerOrderSlip);

                // Calculate product costs based on appointed suppliers
                await CalculateProductCosts(customerOrderSlip.CustomerOrderSlipId, model, cancellationToken);

                // Calculate gross margin
                model.GrossMargin = model.NetOfVatCosPrice - model.NetOfVatProductCost -
                                    model.NetOfVatFreightCharge - customerOrderSlip.CommissionRate;

                // Return appropriate view based on approval status
                if (customerOrderSlip.FirstApprovedBy == null)
                    return View("PreviewByOperationManager", model);

                // Add credit information for finance view
                model.CreditBalance = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetCustomerCreditBalance(customerOrderSlip.CustomerId, cancellationToken);
                model.Total = model.CreditBalance - customerOrderSlip.TotalAmount;

                return View("PreviewByFinance", model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to preview customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Previewed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        private CustomerOrderSlipForApprovalViewModel CreateBaseViewModel(FilprideCustomerOrderSlip customerOrderSlip)
        {
            var vatCalculator = _unitOfWork.FilprideCustomerOrderSlip;

            return new CustomerOrderSlipForApprovalViewModel
            {
                CustomerOrderSlip = customerOrderSlip,
                NetOfVatCosPrice = vatCalculator.ComputeNetOfVat(customerOrderSlip.DeliveredPrice),
                NetOfVatFreightCharge = customerOrderSlip.Freight != 0
                    ? vatCalculator.ComputeNetOfVat((decimal)customerOrderSlip.Freight!)
                    : (decimal)customerOrderSlip.Freight,
                VatAmount = vatCalculator.ComputeVatAmount(
                    vatCalculator.ComputeNetOfVat(customerOrderSlip.TotalAmount)),
                Status = customerOrderSlip.Status
            };
        }

        private async Task CalculateProductCosts(int customerOrderSlipId,
            CustomerOrderSlipForApprovalViewModel model, CancellationToken cancellationToken)
        {
            var appointedSuppliers = await _dbContext.FilprideCOSAppointedSuppliers
                .Include(p => p.PurchaseOrder).ThenInclude(p => p!.ActualPrices)
                .Where(a => a.CustomerOrderSlipId == customerOrderSlipId)
                .ToListAsync(cancellationToken);

            decimal totalPoAmount = 0;
            decimal totalQuantity = appointedSuppliers.Sum(a => a.Quantity);

            foreach (var supplier in appointedSuppliers)
            {
                var po = supplier.PurchaseOrder;
                bool hasTriggeredPrices = po!.UnTriggeredQuantity != po.Quantity &&
                                         po.ActualPrices!.Any(p => p.IsApproved);

                if (hasTriggeredPrices)
                {
                    totalPoAmount += CalculateWeightedCost(po, supplier.Quantity);
                }
                else
                {
                    totalPoAmount += supplier.Quantity *
                                     _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(po.Price);
                }
            }

            model.NetOfVatProductCost = totalQuantity > 0 ? totalPoAmount / totalQuantity : 0;
        }

        private decimal CalculateWeightedCost(FilpridePurchaseOrder po, decimal requiredQuantity)
        {
            decimal weightedCostTotal = 0m;
            decimal totalCOSVolume = 0m;

            foreach (var price in po.ActualPrices!.Where(p => p.IsApproved).OrderBy(p => p.TriggeredDate))
            {
                var effectiveVolume = Math.Min(price.TriggeredVolume, requiredQuantity - totalCOSVolume);

                weightedCostTotal += effectiveVolume * price.TriggeredPrice;
                totalCOSVolume += effectiveVolume;

                if (totalCOSVolume >= requiredQuantity)
                    break;
            }

            if (totalCOSVolume > 0)
            {
                var weightedAvgPrice = weightedCostTotal / totalCOSVolume;
                return requiredQuantity * _unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat(weightedAvgPrice);
            }

            return requiredQuantity * _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(po.Price);
        }

        [Authorize(Roles = "OperationManager, Admin, HeadApprover")]
        public async Task<IActionResult> ApproveByOperationManager(int? id, decimal grossMargin, bool isGrossMarginChanged, string reason, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (grossMargin <= 0 && string.IsNullOrWhiteSpace(reason))
            {
                TempData["error"] = "Reason is required for negative or zero gross margin.";
                return RedirectToAction(nameof(Preview), new { id });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                var oldPrice = existingRecord.DeliveredPrice;

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                string message = string.Empty;

                if (existingRecord.FirstApprovedBy == null)
                {
                    existingRecord.FirstApprovedBy = _userManager.GetUserName(User);
                    existingRecord.FirstApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.OperationManagerReason = reason;

                    if (existingRecord.DeliveryOption == SD.DeliveryOption_DirectDelivery)
                    {
                        var multiplePO = await _dbContext.FilprideCOSAppointedSuppliers
                            .Include(a => a.PurchaseOrder)
                            .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                            .ToListAsync(cancellationToken);

                        var poNumbers = new List<string>();

                        foreach (var item in multiplePO)
                        {
                            var existingPo = item.PurchaseOrder;

                            var subPoModel = new FilpridePurchaseOrder
                            {
                                PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(existingRecord.Company, existingPo!.Type!, cancellationToken),
                                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                                SupplierId = existingPo.SupplierId,
                                ProductId = existingRecord.ProductId,
                                Terms = existingPo.Terms,
                                Quantity = item.Quantity,
                                Price = (decimal)existingRecord.Freight!,
                                Amount = item.Quantity * (decimal)existingRecord.Freight,
                                Remarks = $"{existingRecord.SubPORemarks}\nPlease note: The values in this purchase order are for the freight charge.",
                                Company = existingPo.Company,
                                IsSubPo = true,
                                CustomerId = existingRecord.CustomerId,
                                SubPoSeries = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeForSubPoAsync(existingPo.PurchaseOrderNo!, existingPo.Company, cancellationToken),
                                CreatedBy = existingRecord.FirstApprovedBy,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                PostedBy = existingRecord.FirstApprovedBy,
                                PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                Status = nameof(Status.Posted),
                                OldPoNo = existingPo.OldPoNo,
                                PickUpPointId = existingPo.PickUpPointId,
                                Type = existingPo.Type
                            };

                            poNumbers.Add(subPoModel.PurchaseOrderNo);

                            #region --Audit Trail Recording

                            FilprideAuditTrail auditTrailCreate = new(subPoModel.PostedBy!,
                                $"Created new purchase order# {subPoModel.PurchaseOrderNo}",
                                "Purchase Order",
                                subPoModel.Company);

                            FilprideAuditTrail auditTrailPost = new(subPoModel.PostedBy!,
                                $"Posted purchase order# {subPoModel.PurchaseOrderNo}",
                                "Purchase Order",
                                subPoModel.Company);

                            await _dbContext.AddAsync(auditTrailCreate, cancellationToken);
                            await _dbContext.AddAsync(auditTrailPost, cancellationToken);

                            #endregion --Audit Trail Recording

                            await _unitOfWork.FilpridePurchaseOrder.AddAsync(subPoModel, cancellationToken);
                            await _unitOfWork.SaveAsync(cancellationToken);
                        }

                        message = $"Sub Purchase Order Numbers: {string.Join(", ", poNumbers)} have been successfully generated.";
                    }

                    await _unitOfWork.FilprideCustomerOrderSlip.OperationManagerApproved(existingRecord, grossMargin, isGrossMarginChanged, cancellationToken);

                    if (isGrossMarginChanged)
                    {
                        var userCreated = await _dbContext.ApplicationUsers
                            .FirstOrDefaultAsync(a => a.UserName == existingRecord.CreatedBy, cancellationToken);

                        var notification = $"The gross margin was manually adjusted by {existingRecord.FirstApprovedBy!.ToUpper()} (OM). " +
                                           $"The price was adjusted from {oldPrice:N4} to {existingRecord.DeliveredPrice:N4}.";

                        await _unitOfWork.Notifications.AddNotificationAsync(userCreated!.Id, notification);

                        var hubConnections = await _dbContext.HubConnections
                            .Where(h => h.UserName == userCreated.UserName)
                            .ToListAsync(cancellationToken);

                        foreach (var hubConnection in hubConnections)
                        {
                            await _hubContext.Clients.Client(hubConnection.ConnectionId)
                                .SendAsync("ReceivedNotification", "You have a new message.", cancellationToken);
                        }
                    }
                }

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Approved customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Customer Order Slip has been successfully approved by the Operations Manager. \n\n {message}";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to approve customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Approved by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [Authorize(Roles = "FinanceManager, Admin, HeadApprover")]
        public async Task<IActionResult> ApproveByFinance(int? id, string? terms, string? instructions, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                if (existingRecord.SecondApprovedBy == null)
                {
                    existingRecord.SecondApprovedBy = _userManager.GetUserName(User);
                    existingRecord.SecondApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.Terms = terms ?? existingRecord.Terms;
                    existingRecord.FinanceInstruction = instructions;
                    await _unitOfWork.FilprideCustomerOrderSlip.FinanceApproved(existingRecord, cancellationToken);
                }

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Approved customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer order slip approved by finance successfully.";
                return RedirectToAction(nameof(Preview), new { id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to approve customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Approved by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        [Authorize(Roles = "OperationManager, FinanceManager, Admin")]
        public async Task<IActionResult> Disapprove(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord.DisapprovedBy == null)
                {
                    existingRecord.DisapprovedBy = _userManager.GetUserName(User);
                    existingRecord.DisapprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.Status = nameof(CosStatus.Disapproved);

                    FilprideAuditTrail auditTrailBook = new(existingRecord.DisapprovedBy!, $"Disapproved customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                TempData["success"] = "Customer order slip disapproved successfully.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to disapprove customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Disapproved by: {UserName}",
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

            var customer = await _dbContext.FilprideCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return Json(null);
            }

            return Json(new
            {
                StationCode = customer.StationCode,
                Address = customer.CustomerAddress,
                TinNo = customer.CustomerTin,
                Terms = customer.CustomerTerms,
                customer.CustomerType,
                Branches = !customer.HasBranch ? null : await _unitOfWork.FilprideCustomer
                    .GetCustomerBranchesSelectListAsync(customer.CustomerId),
                customer.HasMultipleTerms
            });
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> AppointSupplier(int? id, CancellationToken cancellationToken)
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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            var viewModel = new CustomerOrderSlipAppointingSupplierViewModel
            {
                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                ProductId = existingRecord.ProductId,
                COSVolume = existingRecord.Quantity,
                Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken),
                PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken),
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppointSupplier(CustomerOrderSlipAppointingSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CurrentUser = _userManager.GetUserName(User);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }

                    existingCos.PickUpPointId = viewModel.PickUpPointId;

                    if (existingCos.Status == nameof(CosStatus.HaulerAppointed))
                    {
                        existingCos.Status = nameof(CosStatus.ForAtlBooking);
                    }
                    else
                    {
                        existingCos.Status = nameof(CosStatus.SupplierAppointed);
                    }

                    switch (viewModel.DeliveryOption)
                    {
                        case SD.DeliveryOption_DirectDelivery:
                            existingCos.Freight = viewModel.Freight;
                            existingCos.SubPORemarks = viewModel.SubPoRemarks;
                            break;
                        case SD.DeliveryOption_ForPickUpByClient:
                            existingCos.Hauler = null;
                            existingCos.Freight = 0;
                            break;
                    }

                    existingCos.DeliveryOption = viewModel.DeliveryOption;

                    var appointedSuppliers = new List<FilprideCOSAppointedSupplier>();

                    foreach (var po in viewModel.PurchaseOrderQuantities)
                    {
                        appointedSuppliers.Add(new FilprideCOSAppointedSupplier
                        {
                            SupplierId = po.SupplierId,
                            CustomerOrderSlipId = existingCos.CustomerOrderSlipId,
                            PurchaseOrderId = po.PurchaseOrderId,
                            Quantity = po.Quantity,
                            UnservedQuantity = po.Quantity,
                        });
                    }

                    await _dbContext.FilprideCOSAppointedSuppliers.AddRangeAsync(appointedSuppliers, cancellationToken);

                    TempData["success"] = "Appointed supplier successfully.";

                    FilprideAuditTrail auditTrailBook = new(viewModel.CurrentUser!, $"Appoint supplier in customer order slip# {existingCos.CustomerOrderSlipNo}", "Customer Order Slip", existingCos.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                    viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
                    viewModel.PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to appoint supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> ReAppointSupplier(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                var viewModel = new CustomerOrderSlipAppointingSupplierViewModel
                {
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    ProductId = existingRecord.ProductId,
                    Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                    PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken),
                    COSVolume = existingRecord.Quantity,
                    DeliveryOption = existingRecord.DeliveryOption!,
                    Freight = existingRecord.Freight ?? 0,
                    PickUpPointId = (int)existingRecord.PickUpPointId!,
                    PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken),
                    SubPoRemarks = existingRecord.SubPORemarks,

                };

                var appointedSuppliers = await _dbContext.FilprideCOSAppointedSuppliers
                    .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                    .ToListAsync(cancellationToken);

                foreach (var appoint in appointedSuppliers)
                {
                    viewModel.SupplierIds.Add(appoint.SupplierId);
                    viewModel.PurchaseOrderIds.Add(appoint.PurchaseOrderId);

                    // Add PO quantity details
                    viewModel.PurchaseOrderQuantities.Add(new PurchaseOrderQuantityInfo
                    {
                        PurchaseOrderId = appoint.PurchaseOrderId,
                        SupplierId = appoint.SupplierId,
                        Quantity = appoint.Quantity
                    });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch appointed supplier. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReAppointSupplier(CustomerOrderSlipAppointingSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CurrentUser = _userManager.GetUserName(User);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }

                    if (existingCos.DeliveryOption != viewModel.DeliveryOption && existingCos.Status != nameof(CosStatus.SupplierAppointed))
                    {
                        var logisticUsers = await _dbContext.ApplicationUsers
                            .Where(a => a.Department == SD.Department_Logistics)
                            .Select(u => u.Id)
                            .ToListAsync(cancellationToken);

                        var message = $"{viewModel.CurrentUser!.ToUpper()} has updated the delivery option of {existingCos.CustomerOrderSlipNo} from {existingCos.DeliveryOption} to {viewModel.DeliveryOption}. " +
                                      $"Please reappoint your hauler if necessary.";

                        await _unitOfWork.Notifications.AddNotificationToMultipleUsersAsync(logisticUsers, message);

                        var usernames = await _dbContext.ApplicationUsers
                            .Where(a => logisticUsers.Contains(a.Id))
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

                    existingCos.PickUpPointId = viewModel.PickUpPointId;

                    switch (viewModel.DeliveryOption)
                    {
                        case SD.DeliveryOption_DirectDelivery:
                            existingCos.Freight = viewModel.Freight;
                            existingCos.SubPORemarks = viewModel.SubPoRemarks;
                            break;
                        case SD.DeliveryOption_ForPickUpByClient:
                            existingCos.Hauler = null;
                            existingCos.Freight = 0;
                            break;
                    }

                    existingCos.DeliveryOption = viewModel.DeliveryOption;

                    var existingAppointedSuppliers = await _dbContext.FilprideCOSAppointedSuppliers
                        .Where(a => a.CustomerOrderSlipId == existingCos.CustomerOrderSlipId)
                        .ToListAsync(cancellationToken);

                    _dbContext.RemoveRange(existingAppointedSuppliers);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var appointedSuppliers = new List<FilprideCOSAppointedSupplier>();

                    foreach (var po in viewModel.PurchaseOrderQuantities)
                    {
                        appointedSuppliers.Add(new FilprideCOSAppointedSupplier
                        {
                            SupplierId = po.SupplierId,
                            CustomerOrderSlipId = existingCos.CustomerOrderSlipId,
                            PurchaseOrderId = po.PurchaseOrderId,
                            Quantity = po.Quantity,
                            UnservedQuantity = po.Quantity,
                        });
                    }

                    await _dbContext.FilprideCOSAppointedSuppliers.AddRangeAsync(appointedSuppliers, cancellationToken);

                    TempData["success"] = "Reappointed supplier successfully.";

                    FilprideAuditTrail auditTrailBook = new(viewModel.CurrentUser!, $"Reappoint supplier in customer order slip# {existingCos.CustomerOrderSlipNo}", "Customer Order Slip", existingCos.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
                    viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
                    viewModel.PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to re-appoint supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> GetPurchaseOrders(string supplierIds, string depot, int? productId, int? cosId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (string.IsNullOrEmpty(supplierIds) || productId == null)
            {
                return NotFound();
            }

            var supplierIdList = supplierIds.Split(',')
                .Select(id => int.Parse(id.Trim()))
                .ToList();

            var previouslyAppointedPOs = cosId.HasValue
                ? await _dbContext.FilprideCOSAppointedSuppliers
                    .Where(a => a.CustomerOrderSlipId == cosId.Value)
                    .Select(a => new PurchaseOrderQuantityInfo()
                    {
                        PurchaseOrderId = a.PurchaseOrderId,
                        SupplierId = a.SupplierId,
                        Quantity = a.Quantity
                    })
                    .ToListAsync(cancellationToken)
                : new List<PurchaseOrderQuantityInfo>();


            var purchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Include(p => p.PickUpPoint)
                .Include(p => p.Supplier)
                .Where(p => supplierIdList.Contains(p.SupplierId) &&
                            p.PickUpPoint!.Depot == depot &&
                            p.ProductId == productId &&
                            !p.IsReceived && !p.IsSubPo &&
                            p.Status == nameof(Status.Posted) &&
                            p.Company == companyClaims)
                .ToListAsync(cancellationToken);


            var purchaseOrderList = purchaseOrders.OrderBy(p => p.PurchaseOrderNo).Select(p => new
            {
                Value = p.PurchaseOrderId,
                Text = p.PurchaseOrderNo,
                AvailableBalance = p.Quantity - p.QuantityReceived,
                p.SupplierId,
                p.Supplier!.SupplierName,
                PreviousQuantity = previouslyAppointedPOs
                    .FirstOrDefault(x => x.PurchaseOrderId == p.PurchaseOrderId)?.Quantity ?? 0, // Now safe
                IsPreSelected = previouslyAppointedPOs
                    .Any(x => x.PurchaseOrderId == p.PurchaseOrderId)
            }).ToList();

            return Json(purchaseOrderList);

        }

        public async Task<IActionResult> CheckCustomerBalance(int? customerId, CancellationToken cancellationToken)
        {
            if (customerId == null)
            {
                return NotFound();
            }

            var balance = await _unitOfWork.FilprideCustomerOrderSlip
                .GetCustomerCreditBalance((int)customerId, cancellationToken);

            return Json(balance);
        }


        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> AppointHauler(int? id, CancellationToken cancellationToken)
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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);
            var viewModel = new CustomerOrderSlipAppointingHauler
            {
                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                DeliveryOption = existingRecord.DeliveryOption!,
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AppointHauler(CustomerOrderSlipAppointingHauler viewModel, CancellationToken cancellationToken)
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
                    viewModel.CurrentUser = _userManager.GetUserName(User)!;

                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }


                    if (existingCos.PickUpPoint != null)
                    {
                        if (existingCos.DeliveryOption != SD.DeliveryOption_ForPickUpByClient)
                        {
                            existingCos.Freight = viewModel.Freight;
                            existingCos.HaulerId = viewModel.HaulerId;
                        }
                        else
                        {
                            existingCos.Freight = 0;
                            existingCos.HaulerId = null;
                        }

                        existingCos.Status = nameof(CosStatus.ForAtlBooking);
                    }
                    else
                    {
                        existingCos.Freight = viewModel.Freight;
                        existingCos.HaulerId = viewModel.HaulerId;
                        existingCos.Status = nameof(CosStatus.HaulerAppointed);
                    }

                    existingCos.Driver = viewModel.Driver;
                    existingCos.PlateNo = viewModel.PlateNo;

                    FilprideAuditTrail auditTrailBook = new(viewModel.CurrentUser, $"Appoint hauler in customer order slip# {existingCos.CustomerOrderSlipNo}", "Customer Order Slip", existingCos.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);
                    TempData["success"] = "Appointed hauler successfully.";
                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to appoint hauler. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    return View(viewModel);
                }
            }
            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> ReAppointHauler(int? id, CancellationToken cancellationToken)
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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            var viewModel = new CustomerOrderSlipAppointingHauler
            {
                CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                DeliveryOption = existingRecord.DeliveryOption!,
                Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken),
                HaulerId = (int)existingRecord.HaulerId!,
                Freight = (decimal)existingRecord.Freight!,
                Driver = existingRecord.Driver!,
                PlateNo = existingRecord.PlateNo!
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReAppointHauler(CustomerOrderSlipAppointingHauler viewModel, CancellationToken cancellationToken)
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
                    viewModel.CurrentUser = _userManager.GetUserName(User)!;

                    var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                    if (existingCos == null)
                    {
                        return BadRequest();
                    }

                    if (existingCos.PickUpPoint != null)
                    {
                        if (existingCos.DeliveryOption != SD.DeliveryOption_ForPickUpByClient)
                        {
                            existingCos.Freight = viewModel.Freight;
                            existingCos.HaulerId = viewModel.HaulerId;
                        }
                        else
                        {
                            existingCos.Freight = 0;
                            existingCos.HaulerId = null;
                        }
                    }
                    else
                    {
                        existingCos.Freight = viewModel.Freight;
                        existingCos.HaulerId = viewModel.HaulerId;
                    }

                    existingCos.Driver = viewModel.Driver;
                    existingCos.PlateNo = viewModel.PlateNo;

                    FilprideAuditTrail auditTrailBook = new(viewModel.CurrentUser, $"Reappoint hauler in customer order slip# {existingCos.CustomerOrderSlipNo}", "Customer Order Slip", existingCos.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);
                    TempData["success"] = "Reappointed hauler successfully.";
                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                }
                catch (Exception ex)
                {
                    viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
                    TempData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to re-appoint hauler. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    return View(viewModel);
                }
            }

            viewModel.Haulers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        public async Task<IActionResult> Close(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                existingRecord.Status = nameof(CosStatus.Closed);

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Closed customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                TempData["success"] = "Customer order slip closed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to close the customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Closed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }
        public async Task<IActionResult> GetPurchaseOrderList(int? productId, string stationCode, CancellationToken cancellationToken)
        {
            if (productId == null)
            {
                return Json(null);
            }

            var filprideProduct = await _dbContext.Products.FindAsync(productId);
            var mobilityProduct = await _dbContext.MobilityProducts.FirstOrDefaultAsync(p => p.ProductCode == filprideProduct!.ProductCode, cancellationToken);
            var purchaseOrder = await _dbContext.MobilityPurchaseOrders
                .Where(p => p.ProductId == mobilityProduct!.ProductId && p.StationCode == stationCode && p.PostedBy != null && !p.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            if (!purchaseOrder.Any())
            {
                return Json(null);
            }

            return Json(purchaseOrder);
        }
        public async Task<IActionResult> GetPurchaseOrder(string? customerPoNo, CancellationToken cancellationToken)
        {
            if (customerPoNo == null)
            {
                return Json(null);
            }

            var purchaseOrder = await _dbContext.MobilityPurchaseOrders
                .FirstOrDefaultAsync(p => p.PurchaseOrderNo == customerPoNo.TrimStart().TrimEnd(), cancellationToken);

            if (purchaseOrder == null)
            {
                return Json(null);
            }

            return Json(new
            {
                purchaseOrder.Quantity,
                purchaseOrder.UnitPrice,
            });
        }
    }
}
