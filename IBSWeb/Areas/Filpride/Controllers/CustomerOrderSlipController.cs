using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Text.Json;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Services;
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

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IHubContext<NotificationHub> _hubContext;

        private const string FilterTypeClaimType = "CustomerOrderSlip.FilterType";

        private readonly ILogger<CustomerOrderSlipController> _logger;

        private readonly ICloudStorageService _cloudStorageService;

        public CustomerOrderSlipController (IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IHubContext<NotificationHub> hubContext,
            ILogger<CustomerOrderSlipController> logger,
            ICloudStorageService cloudStorageService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _hubContext = hubContext;
            _logger = logger;
            _cloudStorageService = cloudStorageService;
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
            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
        }

        public async Task<IActionResult> Index(string filterType)
        {
            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View();
        }

        [HttpPost]
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
                        case "ForATLBooking":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForAtlBooking));
                            break;
                        case "ForCNCApproval":
                            query = query.Where(cos =>
                                cos.Status == nameof(CosStatus.ForApprovalOfCNC));
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
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    // Try to parse the searchValue into a DateOnly
                    bool isDateSearch = DateOnly.TryParse(searchValue, out var searchDate);

                    query = query.Where(s =>
                        s.CustomerOrderSlipNo.ToLower().Contains(searchValue) ||
                        s.OldCosNo.ToLower().Contains(searchValue) ||
                        (s.PurchaseOrder != null && s.PurchaseOrder.PurchaseOrderNo!.ToLower().Contains(searchValue)) ||
                        s.CustomerName.ToLower().Contains(searchValue) ||
                        (isDateSearch && s.Date == searchDate) ||
                        (s.Depot != null && s.Depot.ToLower().Contains(searchValue)) ||
                        s.ProductName.ToLower().Contains(searchValue) ||
                        s.Quantity.ToString().Contains(searchValue) ||
                        s.TotalAmount.ToString().Contains(searchValue) ||
                        s.Status.ToLower().Contains(searchValue));
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    query = query.OrderBy($"{columnName} {sortDirection}");
                }

                var totalRecords = query.Count();

                // Apply pagination and project to a lighter DTO
                var pagedData = await query
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .Select(cos => new
                    {
                        cos.CustomerOrderSlipId,
                        cos.CustomerOrderSlipNo,
                        cos.OldCosNo,
                        cos.PurchaseOrderId,
                        cos.PurchaseOrder!.PurchaseOrderNo,
                        cos.Depot,
                        cos.Date,
                        cos.CustomerName,
                        cos.ProductName,
                        cos.Quantity,
                        cos.DeliveredPrice,
                        cos.TotalAmount,
                        cos.Status,
                        cos.SupplierId,
                        cos.BalanceQuantity,
                        // Extract only PurchaseOrderNos from AppointedSuppliers
                        AppointedSupplierPOs = cos.AppointedSuppliers!
                            .Select(a => a.PurchaseOrder!.PurchaseOrderNo)
                            .ToList(),
                        cos.OldPrice
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
            ViewBag.FilterType = await GetCurrentFilterType();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            CustomerOrderSlipViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                // TODO uncomment this when implementing the feature to restrict the user to create for the previous posted period
                // MinDate = await _unitOfWork.GetThePreviousPostedPeriodAsync(cancellationToken) ?? DateTime.MinValue
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

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var customer = await _unitOfWork.FilprideCustomer
                    .GetAsync(x => x.CustomerId == viewModel.CustomerId, cancellationToken);

                var product = await _unitOfWork.Product.GetAsync(x => x.ProductId == viewModel.ProductId, cancellationToken);

                var commissionee = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierId == viewModel.CommissioneeId, cancellationToken);

                if (customer == null || product == null)
                {
                    return BadRequest();
                }

                // TODO uncomment this when implementing the feature to restrict the user to create for the previous posted period
                // if (await _unitOfWork.IsPeriodPostedAsync(viewModel.Date, cancellationToken))
                // {
                //     TempData["warning"] = $"Oops! {viewModel.Date:MMMM yyyy} has already been closed. New entries are not allowed.";
                //     return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                // }

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
                    OldPrice = !customer.RequiresPriceAdjustment ? viewModel.DeliveredPrice : 0,
                    Freight = viewModel.Freight,
                    CustomerName = customer.CustomerName,
                    ProductName = product.ProductName,
                    VatType = customer.VatType,
                    HasEWT = customer.WithHoldingTax,
                    HasWVAT = customer.WithHoldingVat,
                    CommissioneeName = commissionee?.SupplierName,
                    CommissioneeVatType = commissionee?.VatType,
                    CommissioneeTaxType = commissionee?.TaxType,
                    BusinessStyle = customer.BusinessStyle,
                    AvailableCreditLimit = await _unitOfWork.FilprideCustomerOrderSlip
                        .GetCustomerCreditBalance(customer.CustomerId, cancellationToken),
                };

                // Upload files if there is existing
                if (viewModel.UploadedFiles != null)
                {
                    var uploadUrls = new List<string>();

                    foreach (var file in viewModel.UploadedFiles)
                    {
                        var fileName = GenerateFileNameToSave(file.FileName);
                        await _cloudStorageService.UploadFileAsync(file, fileName);
                        uploadUrls.Add(fileName);
                    }

                    model.UploadedFiles = uploadUrls.ToArray();
                }

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
                TempData["success"] = $"Customer order slip created successfully. Series#: {model.CustomerOrderSlipNo}";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
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
                ViewBag.FilterType = await GetCurrentFilterType();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return BadRequest();
                }

                var getPurchaseOrder = await _unitOfWork.MobilityPurchaseOrder
                    .GetAsync(p => p.PurchaseOrderNo == existingRecord.CustomerPoNo, cancellationToken);

                CustomerOrderSlipViewModel viewModel = new()
                {
                    CustomerOrderSlipId = existingRecord.CustomerOrderSlipId,
                    Date = existingRecord.Date,
                    CustomerId = existingRecord.CustomerId,
                    CustomerAddress = existingRecord.CustomerAddress,
                    TinNo = existingRecord.CustomerTin,
                    Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                    HasCommission = existingRecord.HasCommission,
                    CommissioneeId = existingRecord.CommissioneeId,
                    Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                    CommissionRate = existingRecord.CommissionRate,
                    CustomerPoNo = existingRecord.CustomerPoNo,
                    Quantity = existingRecord.Quantity,
                    DeliveredPrice = existingRecord.DeliveredPrice,
                    Vat = _unitOfWork.FilprideCustomerOrderSlip.ComputeVatAmount((existingRecord.TotalAmount / 1.12m)),
                    TotalAmount = existingRecord.TotalAmount,
                    ProductId = existingRecord.ProductId,
                    Products = await _unitOfWork.GetProductListAsyncById(cancellationToken),
                    AccountSpecialist = existingRecord.AccountSpecialist,
                    Remarks = existingRecord.Remarks,
                    OtcCosNo = existingRecord.OldCosNo,
                    Status = existingRecord.Status,
                    Terms = existingRecord.Terms,
                    Branches = await _unitOfWork.FilprideCustomer
                        .GetCustomerBranchesSelectListAsync(existingRecord.CustomerId, cancellationToken),
                    SelectedBranch = existingRecord.Branch,
                    CustomerType = existingRecord.CustomerType,
                    StationCode = getPurchaseOrder?.StationCode,
                    Freight = existingRecord.Freight ?? 0,
                    // TODO uncomment this when implementing the feature to restrict the user to create for the previous posted period
                    // MinDate = await _unitOfWork.GetThePreviousPostedPeriodAsync(cancellationToken) ?? DateTime.MinValue
                };

                // If there is uploaded, get signed URL
                if (existingRecord.UploadedFiles == null)
                {
                    return View(viewModel);
                }

                var fileInfos = new List<COSFileInfo>();

                foreach (var file in existingRecord.UploadedFiles)
                {
                    var fileInfo = new COSFileInfo
                    {
                        FileName = file,
                        SignedUrl = await _cloudStorageService.GetSignedUrlAsync(file)
                    };
                    fileInfos.Add(fileInfo);
                }

                viewModel.FileInfos = fileInfos;

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

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.Commissionee = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                // TODO uncomment this when implementing the feature to restrict the user to create for the previous posted period
                // if (await _unitOfWork.IsPeriodPostedAsync(viewModel.Date, cancellationToken))
                // {
                //     TempData["warning"] = $"The book has already been closed for this period {viewModel.Date:MMMM yyyy}. " +
                //                           $"Editing this record is not permitted.";
                //     return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
                // }

                // If the new array has value
                if (viewModel.ArrayOfFileNames != "[]")
                {
                    var arrayOfFileNames = JsonSerializer.Deserialize<string[]>(viewModel.ArrayOfFileNames!);

                    if (existingRecord.UploadedFiles?.Length != 0) // If the old record has value
                    {
                        foreach (var existingFile in existingRecord.UploadedFiles!) // Filter out the files that does not exist in the new array
                        {
                            if (arrayOfFileNames!.Any(x =>
                                    x.Equals(existingFile, StringComparison.OrdinalIgnoreCase))) // If the old value is in the new array, keep it; else delete it and its file
                            {
                                continue;
                            }

                            await _cloudStorageService.DeleteFileAsync(existingFile);
                            existingRecord.UploadedFiles = existingRecord.UploadedFiles.Where(s => s != existingFile).ToArray();
                        }

                    }
                    // After this, the uploadedFiles only has the old fileNames that are retained

                    // If there is new file uploads detected
                    if (viewModel.UploadedFiles != null)
                    {
                        // Loop through all the names
                        foreach (var newFile in arrayOfFileNames!)
                        {
                            // If the name is not in the old array: it is a new file. So rename it, upload it, and add it to old array
                            if (existingRecord.UploadedFiles!.Any(x =>
                                    x.Equals(newFile, StringComparison.Ordinal)))
                            {
                                continue;
                            }

                            // Get the new file
                            var file = viewModel.UploadedFiles.FirstOrDefault(x => x.FileName == newFile);
                            // Generate new fileName
                            var fileName = GenerateFileNameToSave(file!.FileName);
                            // Upload the new file
                            await _cloudStorageService.UploadFileAsync(file, fileName);
                            // Add the new file into the old array
                            var tempList = existingRecord.UploadedFiles?.ToList(); // Convert array to list
                            tempList?.Add(fileName); // Add new file name
                            existingRecord.UploadedFiles = tempList?.ToArray();
                        }
                    }
                }
                else
                {
                    // If the new array is empty but the previous existing has value, it means the user deleted all
                    if (existingRecord.UploadedFiles?.Length != 0)
                    {
                        foreach (var existingFile in existingRecord.UploadedFiles!)
                        {
                            await _cloudStorageService.DeleteFileAsync(existingFile);
                            existingRecord.UploadedFiles = existingRecord.UploadedFiles.Where(s => s != existingFile).ToArray();
                        }
                    }
                }

                viewModel.CurrentUser = _userManager.GetUserName(User);

                if (string.IsNullOrEmpty(viewModel.Terms))
                {
                    var customer = await _unitOfWork.FilprideCustomer
                        .GetAsync(cos => cos.CustomerId == viewModel.CustomerId, cancellationToken);

                    if (customer == null)
                    {
                        return NotFound();
                    }

                    viewModel.Terms = customer.CustomerTerms;
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

                if (existingRecord.Quantity != viewModel.Quantity)
                {
                    changes.Add("Quantity was updated.");
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
                if (existingRecord.Freight != viewModel.Freight)
                {
                    changes.Add("Freight was updated.");
                }

                await _unitOfWork.FilprideCustomerOrderSlip.UpdateAsync(viewModel, cancellationToken);

                if (changes.Count > 0 && existingRecord.Status != nameof(CosStatus.Created))
                {
                    var users = await _dbContext.ApplicationUsers
                        .Where(a => a.Department == SD.Department_TradeAndSupply
                                    || a.Department == SD.Department_CreditAndCollection
                                    || a.Department == SD.Department_Finance)
                        .Select(u => u.Id)
                        .ToListAsync(cancellationToken);

                    var message = $"{viewModel.CurrentUser!.ToUpper()} has modified {existingRecord.CustomerOrderSlipNo}." +
                                  $" Updates include:\n{string.Join("\n", changes)}";

                    if (changes.Any(x => x.Contains("Product") || x.Contains("Quantity") ))
                    {
                        message += "\nFor TNS kindly reappoint the supplier.";
                        existingRecord.Status = nameof(CosStatus.Created);
                        existingRecord.PickUpPointId = null;
                        existingRecord.Depot = string.Empty;
                        existingRecord.OmApprovedBy = null;
                        existingRecord.OmApprovedDate = null;
                        existingRecord.FmApprovedBy = null;
                        existingRecord.FmApprovedDate = null;
                        existingRecord.CncApprovedBy = null;
                        existingRecord.CncApprovedDate = null;
                        existingRecord.FinanceInstruction = null;
                        existingRecord.OMReason = null;
                        existingRecord.ExpirationDate = null;

                        await _dbContext.FilprideCOSAppointedSuppliers
                            .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                            .ExecuteDeleteAsync(cancellationToken);
                    }

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

                FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy!, $"Edit customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Customer order slip updated successfully.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                ViewBag.FilterType = await GetCurrentFilterType();

                var customerOrderSlip = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (customerOrderSlip == null)
                {
                    return BadRequest();
                }

                // Create the view model with basic information
                var model = CreateBaseViewModel(customerOrderSlip);

                // Get signed url of uploads
                if (customerOrderSlip.UploadedFiles != null)
                {
                    var listOfSignedUrls = new List<COSFileInfo>();

                    foreach (var fileUrl in customerOrderSlip.UploadedFiles)
                    {
                        var fileInfoInstance = new COSFileInfo
                        {
                            FileName = Path.GetFileName(fileUrl),
                            SignedUrl = await _cloudStorageService.GetSignedUrlAsync(fileUrl)
                        };

                        listOfSignedUrls.Add(fileInfoInstance);
                    }

                    model.UploadedFiles = listOfSignedUrls;
                }

                // Calculate product costs based on appointed suppliers
                await CalculateProductCosts(customerOrderSlip.CustomerOrderSlipId, model, cancellationToken);

                // Calculate gross margin
                model.GrossMargin = model.NetOfVatCosPrice - model.NetOfVatProductCost -
                                    model.NetOfVatFreightCharge - model.NetOfVatCommission;


                // Return appropriate view based on approval status
                if (customerOrderSlip.Status == nameof(CosStatus.ForApprovalOfOM))
                {
                    return View("PreviewByOperationManager", model);
                }

                // Add credit information for finance view
                model.AvailableCreditLimit = customerOrderSlip.AvailableCreditLimit;
                model.Total = model.AvailableCreditLimit - customerOrderSlip.TotalAmount;

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

            var netOfVatCosPrice = customerOrderSlip.VatType == SD.VatType_Vatable
                ? vatCalculator.ComputeNetOfVat(customerOrderSlip.DeliveredPrice)
                : customerOrderSlip.DeliveredPrice;

            var netOfVatFreightCharge = customerOrderSlip.VatType == SD.VatType_Vatable && customerOrderSlip.Freight != 0
                ? vatCalculator.ComputeNetOfVat((decimal)customerOrderSlip.Freight!)
                : (decimal)customerOrderSlip.Freight!;

            var vatAmount = customerOrderSlip.VatType == SD.VatType_Vatable
                ? vatCalculator.ComputeVatAmount(
                    vatCalculator.ComputeNetOfVat(customerOrderSlip.TotalAmount))
                : 0m;

            var netOfVatCommission = customerOrderSlip.CommissioneeVatType == SD.VatType_Vatable  && customerOrderSlip.CommissionRate != 0
                ? vatCalculator.ComputeNetOfVat(customerOrderSlip.CommissionRate)
                : customerOrderSlip.CommissionRate;

            return new CustomerOrderSlipForApprovalViewModel
            {
                CustomerOrderSlip = customerOrderSlip,
                NetOfVatCosPrice = netOfVatCosPrice,
                NetOfVatFreightCharge = netOfVatFreightCharge,
                NetOfVatProductCost = netOfVatCommission,
                VatAmount = vatAmount,
                Status = customerOrderSlip.Status,
                PriceReference = customerOrderSlip.PriceReference
            };
        }

        private async Task CalculateProductCosts(int customerOrderSlipId,
            CustomerOrderSlipForApprovalViewModel model, CancellationToken cancellationToken)
        {
            var appointedSuppliers = await _dbContext.FilprideCOSAppointedSuppliers
                .Include(p => p.PurchaseOrder).ThenInclude(p => p!.ActualPrices)
                .Where(a => a.CustomerOrderSlipId == customerOrderSlipId)
                .ToListAsync(cancellationToken);

            var totalPoAmount = 0m;
            var totalQuantity = appointedSuppliers.Sum(a => a.Quantity);

            foreach (var supplier in appointedSuppliers)
            {
                var po = supplier.PurchaseOrder;
                var hasTriggeredPrices = po!.UnTriggeredQuantity != po.Quantity &&
                                         po.ActualPrices!.Any(p => p.IsApproved);

                if (hasTriggeredPrices)
                {
                    totalPoAmount += await CalculateWeightedCost(po, supplier.Quantity);
                }
                else
                {
                    var grossAmount = supplier.Quantity * await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(po.PurchaseOrderId, cancellationToken);
                    var netOfVat = po.VatType == SD.VatType_Vatable
                            ? _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(grossAmount)
                            : grossAmount;

                    totalPoAmount += netOfVat;
                }
            }

            model.NetOfVatProductCost = totalQuantity > 0 ? totalPoAmount / totalQuantity : 0;
        }

        private async Task<decimal> CalculateWeightedCost(FilpridePurchaseOrder po, decimal requiredQuantity)
        {
            var weightedCostTotal = 0m;
            var totalCosVolume = 0m;

            foreach (var price in po.ActualPrices!.Where(p => p.IsApproved).OrderBy(p => p.TriggeredDate))
            {
                var effectiveVolume = Math.Min(price.TriggeredVolume, requiredQuantity - totalCosVolume);

                weightedCostTotal += effectiveVolume * price.TriggeredPrice;
                totalCosVolume += effectiveVolume;

                if (totalCosVolume >= requiredQuantity)
                    break;
            }

            if (totalCosVolume > 0)
            {
                var weightedAvgPrice = weightedCostTotal / totalCosVolume;
                var finalWeightedAvgPrice = po.VatType == SD.VatType_Vatable
                        ?_unitOfWork.FilprideCustomerOrderSlip.ComputeNetOfVat(weightedAvgPrice)
                        : weightedAvgPrice;

                return requiredQuantity * finalWeightedAvgPrice;
            }

            var finalPrice = po.VatType == SD.VatType_Vatable
                    ? _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(po.PurchaseOrderId))
                    : await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(po.PurchaseOrderId);

            return requiredQuantity * finalPrice;
        }

        [Authorize(Roles = "OperationManager, Admin, HeadApprover")]
        public async Task<IActionResult> ApproveByOperationManager(int? id, string reason, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var message = string.Empty;

            try
            {
                var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

                if (existingRecord == null)
                {
                    return NotFound();
                }

                if (existingRecord.OmApprovedBy == null)
                {
                    existingRecord.OmApprovedBy = _userManager.GetUserName(User);
                    existingRecord.OmApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.OMReason = reason;
                    existingRecord.ExpirationDate = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime().AddDays(7));
                    existingRecord.Status = nameof(CosStatus.ForApprovalOfCNC);

                    if (existingRecord.DeliveryOption == SD.DeliveryOption_DirectDelivery && existingRecord.Freight != 0)
                    {
                        var multiplePo = await _dbContext.FilprideCOSAppointedSuppliers
                            .Include(a => a.PurchaseOrder)
                            .Where(a => a.CustomerOrderSlipId == existingRecord.CustomerOrderSlipId)
                            .ToListAsync(cancellationToken);

                        var poNumbers = new List<string>();

                        foreach (var item in multiplePo)
                        {
                            var existingPo = item.PurchaseOrder;

                            var subPoModel = new FilpridePurchaseOrder
                            {
                                PurchaseOrderNo = await _unitOfWork.FilpridePurchaseOrder.GenerateCodeAsync(existingRecord.Company, existingPo!.Type!, cancellationToken),
                                Date = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime()),
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
                                CreatedBy = existingRecord.OmApprovedBy,
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                PostedBy = existingRecord.OmApprovedBy,
                                PostedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                                Status = nameof(Status.Posted),
                                OldPoNo = existingPo.OldPoNo,
                                PickUpPointId = existingPo.PickUpPointId,
                                Type = existingPo.Type,
                                SupplierName = existingPo.SupplierName,
                                SupplierAddress = existingPo.SupplierAddress,
                                SupplierTin = existingPo.SupplierTin,
                                ProductName = existingPo.ProductName,
                                VatType = existingPo.VatType,
                                TaxType = existingPo.TaxType
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

                            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailCreate, cancellationToken);
                            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailPost, cancellationToken);

                            #endregion --Audit Trail Recording

                            await _unitOfWork.FilpridePurchaseOrder.AddAsync(subPoModel, cancellationToken);
                        }

                        message = $"Sub Purchase Order Numbers: {string.Join(", ", poNumbers)} have been successfully generated.";
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

        [Authorize(Roles = "FinanceManager, CncManager, Admin, HeadApprover")]
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

                if (existingRecord.Status == nameof(CosStatus.ForApprovalOfCNC))
                {
                    existingRecord.CncApprovedBy = _userManager.GetUserName(User);
                    existingRecord.CncApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.Status = nameof(CosStatus.ForApprovalOfFM);
                    TempData["success"] = "Customer order slip approved by cnc successfully.";
                }
                else
                {
                    existingRecord.FmApprovedBy = _userManager.GetUserName(User);
                    existingRecord.FmApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingRecord.Status = nameof(CosStatus.ForDR);
                    TempData["success"] = "Customer order slip approved by finance successfully.";
                }

                existingRecord.Terms = terms ?? existingRecord.Terms;
                existingRecord.FinanceInstruction = instructions;

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!, $"Approved customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
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

                if (existingRecord == null)
                {
                    return NotFound();
                }

                existingRecord.DisapprovedBy = _userManager.GetUserName(User);
                existingRecord.DisapprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingRecord.Status = nameof(CosStatus.Disapproved);

                FilprideAuditTrail auditTrailBook = new(existingRecord.DisapprovedBy!, $"Disapproved customer order slip# {existingRecord.CustomerOrderSlipNo}", "Customer Order Slip", existingRecord.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

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

        public async Task<IActionResult> GetCustomerDetails(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return Json(null);
            }

            var customer = await _unitOfWork.FilprideCustomer
                .GetAsync(c => c.CustomerId == id, cancellationToken);

            if (customer == null)
            {
                return Json(null);
            }

            return Json(new
            {
                customer.StationCode,
                Address = customer.CustomerAddress,
                TinNo = customer.CustomerTin,
                Terms = customer.CustomerTerms,
                customer.CustomerType,
                Branches = !customer.HasBranch ? null : await _unitOfWork.FilprideCustomer
                    .GetCustomerBranchesSelectListAsync(customer.CustomerId, cancellationToken),
                customer.HasMultipleTerms
            });
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> AppointSupplier(int? id, CancellationToken cancellationToken)
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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            if (existingRecord == null)
            {
                return NotFound();
            }

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
            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
            viewModel.PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                ViewBag.FilterType = await GetCurrentFilterType();
                var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                var depot = await _unitOfWork.FilpridePickUpPoint
                    .GetAsync(p => p.PickUpPointId == viewModel.PickUpPointId, cancellationToken);

                if (existingCos == null || depot == null)
                {
                    return BadRequest();
                }

                existingCos.PickUpPointId = viewModel.PickUpPointId;
                existingCos.Depot = depot.Depot;
                existingCos.Status = nameof(CosStatus.ForAtlBooking);

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
                        UnreservedQuantity = po.Quantity,
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

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to appoint supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> ReAppointSupplier(int? id, CancellationToken cancellationToken)
        {
            ViewBag.FilterType = await GetCurrentFilterType();
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

                if (existingRecord == null)
                {
                    return NotFound();
                }

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
            viewModel.Suppliers = await _unitOfWork.FilprideSupplier.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.PurchaseOrders = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncById(companyClaims, cancellationToken);
            viewModel.PickUpPoints = await _unitOfWork.GetDistinctFilpridePickupPointListById(companyClaims, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingCos = await _unitOfWork.FilprideCustomerOrderSlip
                    .GetAsync(cos => cos.CustomerOrderSlipId == viewModel.CustomerOrderSlipId, cancellationToken);

                if (existingCos == null)
                {
                    return BadRequest();
                }

                var depot = await _unitOfWork.FilpridePickUpPoint
                    .GetAsync(p => p.PickUpPointId == viewModel.PickUpPointId, cancellationToken);

                if (depot == null)
                {
                    return BadRequest();
                }

                existingCos.PickUpPointId = viewModel.PickUpPointId;
                existingCos.Depot = depot.Depot;
                existingCos.Status = nameof(CosStatus.ForAtlBooking);
                existingCos.OmApprovedBy = null;
                existingCos.OmApprovedDate = null;
                existingCos.FmApprovedBy = null;
                existingCos.FmApprovedDate = null;
                existingCos.CncApprovedBy = null;
                existingCos.CncApprovedDate = null;
                existingCos.FinanceInstruction = null;
                existingCos.OMReason = null;
                existingCos.ExpirationDate = null;

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
                        UnreservedQuantity = po.Quantity,
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

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to re-appoint supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Appointed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
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
                : [];


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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            if (existingRecord == null)
            {
                return NotFound();
            }

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
                        if (existingCos.DeliveryOption == SD.DeliveryOption_ForPickUpByHauler)
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
            TempData["warning"] = "The submitted information is invalid.";
            return View(viewModel);
        }

        [DepartmentAuthorize(SD.Department_Logistics, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> ReAppointHauler(int? id, CancellationToken cancellationToken)
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

            var existingRecord = await _unitOfWork.FilprideCustomerOrderSlip
                .GetAsync(cos => cos.CustomerOrderSlipId == id, cancellationToken);

            if (existingRecord == null)
            {
                return NotFound();
            }

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
                        if (existingCos.DeliveryOption == SD.DeliveryOption_ForPickUpByHauler)
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
            TempData["warning"] = "The submitted information is invalid.";
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

                if (existingRecord == null)
                {
                    return NotFound();
                }

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

            var filprideProduct = await _unitOfWork.Product.GetAsync(x => x.ProductId == productId, cancellationToken);
            var mobilityProduct = await _unitOfWork.MobilityProduct.GetAsync(p => p.ProductCode == filprideProduct!.ProductCode, cancellationToken);

            var purchaseOrder = (await _unitOfWork.MobilityPurchaseOrder
                .GetAllAsync(p => p.ProductId == mobilityProduct!.ProductId
                            && p.StationCode == stationCode
                            && p.PostedBy != null
                            && !p.IsReceived, cancellationToken))
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToList();

            return Json(purchaseOrder.Count == 0 ? null : purchaseOrder);
        }
        public async Task<IActionResult> GetPurchaseOrder(string? customerPoNo, CancellationToken cancellationToken)
        {
            if (customerPoNo == null)
            {
                return Json(null);
            }

            var purchaseOrder = await _unitOfWork.MobilityPurchaseOrder
                .GetAsync(p => p.PurchaseOrderNo == customerPoNo.Trim(), cancellationToken);

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

        [DepartmentAuthorize(SD.Department_Marketing, SD.Department_RCD)]
        public async Task<IActionResult> ChangePrice(int? id, decimal newPrice, string referenceNo, CancellationToken cancellationToken)
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
                    return NotFound();
                }

                existingRecord.OldPrice = existingRecord.DeliveredPrice;
                existingRecord.DeliveredPrice = newPrice;
                existingRecord.TotalAmount = existingRecord.Quantity * existingRecord.DeliveredPrice;
                existingRecord.PriceReference = referenceNo;

                await _unitOfWork.FilprideDeliveryReceipt.RecalculateDeliveryReceipts(existingRecord.CustomerOrderSlipId,
                    existingRecord.DeliveredPrice, cancellationToken);

                #region Notification

                var users = await _dbContext.ApplicationUsers
                    .Where(a => a.Position == SD.Position_OperationManager
                                || a.Department == SD.Department_CreditAndCollection)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                var message = $"The price for customer order slip# {existingRecord.CustomerOrderSlipNo} has been updated by {_userManager.GetUserName(User)!}, from {existingRecord.OldPrice:N4} to {existingRecord.DeliveredPrice:N4} (gross of VAT).";

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

                #endregion

                FilprideAuditTrail auditTrailBook = new(_userManager.GetUserName(User)!,
                    $"Update actual price for customer order slip# {existingRecord.CustomerOrderSlipNo}, from {existingRecord.OldPrice:N4} to {existingRecord.DeliveredPrice:N4} (gross of VAT).",
                    "Customer Order Slip",
                    existingRecord.Company);

                TempData["success"] = $"The price for {existingRecord.CustomerOrderSlipNo} has been updated, from {existingRecord.OldPrice:N4} to {existingRecord.DeliveredPrice:N4} (gross of VAT).";

                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to change the price the customer order slip. Error: {ErrorMessage}, Stack: {StackTrace}. Changed by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(Preview), new { id });
            }
        }

        private string GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }
    }
}
