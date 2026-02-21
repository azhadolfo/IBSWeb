using System.Diagnostics;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Text.Json;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Filpride.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD, SD.Department_HRAndAdminOrLegal)]
    public class CheckVoucherNonTradeInvoiceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradeInvoiceController> _logger;

        private readonly ICacheService _cacheService;

        private readonly ISubAccountResolver _subAccountResolver;

        public CheckVoucherNonTradeInvoiceController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradeInvoiceController> logger,
            ICacheService cacheService,
            ISubAccountResolver subAccountResolver)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _cacheService = cacheService;
            _subAccountResolver = subAccountResolver;
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

        private string GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoiceCheckVouchers([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                var checkVoucher = await _dbContext.FilprideCheckVoucherHeaders
                    .Include(cvh => cvh.Supplier)
                    .Where(cvh => cvh.Company == companyClaims &&
                                  cvh.CvType == nameof(CVType.Invoicing) &&
                                  !cvh.IsPayroll)
                    .ToListAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucher = checkVoucher
                        .Where(s =>
                            s.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Payee?.ToLower().Contains(searchValue) == true ||
                            s.InvoiceAmount.ToString().Contains(searchValue) ||
                            s.AmountPaid.ToString().Contains(searchValue) ||
                            (s.InvoiceAmount - s.AmountPaid).ToString().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue) ||
                            s.Particulars?.ToLower().Contains(searchValue) == true
                        )
                        .ToList();
                }

                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    checkVoucher = checkVoucher
                        .Where(s =>
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                var projectedQuery = checkVoucher
                    .Select(x => new
                    {
                        x.CheckVoucherHeaderNo,
                        x.Date,
                        x.Payee,
                        x.SupplierId,
                        x.InvoiceAmount,
                        x.AmountPaid,
                        x.Status,
                        x.VoidedBy,
                        x.CanceledBy,
                        x.PostedBy,
                        x.IsPaid,
                        x.CheckVoucherHeaderId
                    })
                    .ToList();

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Name;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    projectedQuery = projectedQuery
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = projectedQuery.Count;

                var pagedData = projectedQuery
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
                _logger.LogError(ex, "Failed to get invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GetDefaultExpense(int? supplierId)
        {
            var supplier = (await _unitOfWork.FilprideSupplier
                    .GetAsync(supp => supp.SupplierId == supplierId))!.DefaultExpenseNumber;

            var defaultExpense = (await _unitOfWork.FilprideChartOfAccount
                .GetAllAsync(coa => (coa.Level == 4 || coa.Level == 5)))
                .OrderBy(coa => coa.AccountId)
                .ToList();

            if (defaultExpense.Count > 0)
            {
                var defaultExpenseList = defaultExpense.Select(coa => new
                {
                    coa.AccountNumber,
                    AccountTitle = coa.AccountName,
                    IsSelected = coa.AccountNumber == supplier?.Split(' ')[0]
                }).ToList();

                return Json(defaultExpenseList);
            }

            return Json(null);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var coaCacheKey = $"coa:{companyClaims}";
            var supplierCacheKey = $"supplier:{companyClaims}";
            var minDateCacheKey = $"minDate:{companyClaims}";

            // Chart of Accounts
            var coaSelectList = await _cacheService.GetAsync<List<SelectListItem>>(
                coaCacheKey,
                cancellationToken);

            if (coaSelectList == null)
            {
                coaSelectList = await _unitOfWork
                    .GetChartOfAccountListAsyncByAccountTitle(cancellationToken);

                await _cacheService.SetAsync(
                    coaCacheKey,
                    coaSelectList,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromHours(1),
                    cancellationToken);
            }

            var supplierSelectList = await _cacheService.GetAsync<List<SelectListItem>>(
                supplierCacheKey,
                cancellationToken);

            // Suppliers
            if (supplierSelectList == null)
            {
                supplierSelectList = await _unitOfWork
                    .GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await _cacheService.SetAsync(
                    supplierCacheKey,
                    supplierSelectList,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromHours(1),
                    cancellationToken);
            }

            // Min Date
            var minDate = await _cacheService.GetAsync<DateTime>(
                minDateCacheKey,
                cancellationToken);

            if (minDate == default)
            {
                minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

                await _cacheService.SetAsync(
                    minDateCacheKey,
                    minDate,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromHours(1),
                    cancellationToken);
            }

            viewModel.ChartOfAccounts = coaSelectList;
            viewModel.Suppliers = supplierSelectList;
            viewModel.MinDate = minDate;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["error"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                #region --Retrieve Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve Supplier

                FilprideCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultipleInvoiceAsync(companyClaims, viewModel.Type!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Payee = viewModel.SupplierName,
                    Address = viewModel.SupplierAddress!,
                    Tin = viewModel.SupplierTinNo!,
                    PONo = [viewModel.PoNo ?? string.Empty],
                    SINo = [viewModel.SiNo ?? string.Empty],
                    SupplierId = viewModel.SupplierId,
                    Particulars = viewModel.Particulars,
                    CreatedBy = GetUserFullName(),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Invoicing),
                    Company = companyClaims,
                    Type = viewModel.Type,
                    Total = viewModel.Total,
                    SupplierName = supplier.SupplierName,
                    TaxType = string.Empty,
                    VatType = string.Empty,
                    Status = nameof(CheckVoucherInvoiceStatus.ForApproval)
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(checkVoucherHeader, cancellationToken);

                #endregion -- Saving the default entries --

                #region -- cv invoiving details entry --

                List<FilprideCheckVoucherDetail> checkVoucherDetails = new();

                decimal apNontradeAmount = 0;
                decimal vatAmount = 0;
                decimal ewtOnePercentAmount = 0;
                decimal ewtTwoPercentAmount = 0;
                decimal ewtFivePercentAmount = 0;
                decimal ewtTenPercentAmount = 0;

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");
                var bir = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"), cancellationToken);

                foreach (var accountEntry in viewModel.AccountingEntries!)
                {
                    var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    var accountNo = parts[0];
                    var accountName = parts[1];

                    var (subAccountType, subAccountId) = SubAccountHelper.DetermineCvSubAccount(
                        accountEntry.CustomerMasterFileId,
                        accountEntry.SupplierMasterFileId,
                        accountEntry.EmployeeMasterFileId,
                        accountEntry.BankMasterFileId,
                        accountEntry.CompanyMasterFileId
                    );

                    string? subAccountName = null;

                    if (subAccountType.HasValue && subAccountId.HasValue)
                    {
                        var subAccountInfo = await _subAccountResolver.ResolveAsync(
                            subAccountType.Value,
                            subAccountId.Value,
                            cancellationToken
                        );

                        if (subAccountInfo != null)
                        {
                            subAccountName = subAccountInfo.Name;
                        }
                    }

                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = accountNo,
                        AccountName = accountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = accountEntry.NetOfVatAmount,
                        Credit = 0,
                        IsVatable = accountEntry.Vatable,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        SubAccountType = subAccountType,
                        SubAccountId = subAccountId,
                        SubAccountName = subAccountName,
                    });

                    if (accountEntry.Vatable)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    switch (accountEntry.TaxPercentage)
                    {
                        case 0.01m:
                            ewtOnePercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.02m:
                            ewtTwoPercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.05m:
                            ewtFivePercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.10m:
                            ewtTenPercentAmount += accountEntry.TaxAmount;
                            break;
                    }

                    apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;
                }

                checkVoucherHeader.InvoiceAmount = apNontradeAmount;

                if (vatAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = vatAmount,
                        Credit = 0,
                    });
                }

                if (apNontradeAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = apNonTradeTitle.AccountNumber,
                        AccountName = apNonTradeTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = apNontradeAmount,
                        Amount = apNontradeAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = checkVoucherHeader.SupplierId,
                        SubAccountName = checkVoucherHeader.SupplierName,
                    });
                }

                if (ewtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtOnePercentAmount,
                        Amount = ewtOnePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTwoPercentAmount,
                        Amount = ewtTwoPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtFivePercentAmount,
                        Amount = ewtFivePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTenPercentAmount,
                        Amount = ewtTenPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- cv invoiving details entry --

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    checkVoucherHeader.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    checkVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, checkVoucherHeader.SupportingFileSavedFileName!);
                }

                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", checkVoucherHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher invoicing #{checkVoucherHeader.CheckVoucherHeaderNo} created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);

                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,AccountingManager")]
        public async Task<IActionResult> Approve(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            if (model.Status != nameof(CheckVoucherInvoiceStatus.ForApproval))
            {
                TempData["error"] = "This invoice is not pending for approval.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.ApprovedBy = GetUserFullName();
                model.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.ForPosting);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Approved check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording
                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Approved.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve invoice check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Approved by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, existingModel.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingModel.Date:MMM yyyy} is already closed.");
                }

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.IsUserSelected && d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                existingModel.Suppliers =
                    await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                existingModel.COA = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);

                CheckVoucherNonTradeInvoicingViewModel viewModel = new()
                {
                    CVId = existingModel.CheckVoucherHeaderId,
                    Suppliers = existingModel.Suppliers,
                    SupplierName = existingModel.Supplier!.SupplierName,
                    ChartOfAccounts = existingModel.COA,
                    TransactionDate = existingModel.Date,
                    SupplierId = existingModel.SupplierId ?? 0,
                    SupplierAddress = existingModel.Address,
                    SupplierTinNo = existingModel.Tin,
                    PoNo = existingModel.PONo?.FirstOrDefault(),
                    SiNo = existingModel.SINo?.FirstOrDefault(),
                    Total = existingModel.Total,
                    Particulars = existingModel.Particulars!,
                    AccountingEntries = [],
                    MinDate = minDate
                };

                foreach (var details in existingDetailsModel)
                {
                    viewModel.AccountingEntries.Add(new AccountingEntryViewModel
                    {
                        AccountTitle = $"{details.AccountNo} {details.AccountName}",
                        Amount =
                            details.IsVatable ? Math.Round(details.Debit * 1.12m, 2) : Math.Round(details.Debit, 2),
                        Vatable = details.IsVatable,
                        TaxPercentage = details.EwtPercent,
                        BankMasterFileId = details.SubAccountType == SubAccountType.BankAccount
                            ? details.SubAccountId
                            : null,
                        CompanyMasterFileId = details.SubAccountType == SubAccountType.Company
                            ? details.SubAccountId
                            : null,
                        EmployeeMasterFileId = details.SubAccountType == SubAccountType.Employee
                            ? details.SubAccountId
                            : null,
                        CustomerMasterFileId = details.SubAccountType == SubAccountType.Customer
                            ? details.SubAccountId
                            : null,
                        SupplierMasterFileId = details.SubAccountType == SubAccountType.Supplier
                            ? details.SubAccountId
                            : null,
                    });
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv non trade invoice. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                var existingModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                #region -- Get supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion -- Get supplier

                existingModel.EditedBy = GetUserFullName();
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingModel.Date = viewModel.TransactionDate;
                existingModel.SupplierId = supplier.SupplierId;
                existingModel.Payee = supplier.SupplierName;
                existingModel.Address = supplier.SupplierAddress;
                existingModel.Tin = supplier.SupplierTin;
                existingModel.PONo = [viewModel.PoNo ?? string.Empty];
                existingModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingModel.Particulars = viewModel.Particulars;
                existingModel.Total = viewModel.Total;
                existingModel.SupplierName = supplier.SupplierName;

                #endregion --Saving the default entries

                #region --CV Details Entry

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId).
                    ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var checkVoucherDetails = new List<FilprideCheckVoucherDetail>();

                decimal apNontradeAmount = 0;
                decimal vatAmount = 0;
                decimal ewtOnePercentAmount = 0;
                decimal ewtTwoPercentAmount = 0;
                decimal ewtFivePercentAmount = 0;
                decimal ewtTenPercentAmount = 0;

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");
                var bir = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"), cancellationToken);

                foreach (var accountEntry in viewModel.AccountingEntries!)
                {
                    var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    var accountNo = parts[0];
                    var accountName = parts[1];

                    var (subAccountType, subAccountId) = SubAccountHelper.DetermineCvSubAccount(
                        accountEntry.CustomerMasterFileId,
                        accountEntry.SupplierMasterFileId,
                        accountEntry.EmployeeMasterFileId,
                        accountEntry.BankMasterFileId,
                        accountEntry.CompanyMasterFileId
                    );

                    string? subAccountName = null;

                    if (subAccountType.HasValue && subAccountId.HasValue)
                    {
                        var subAccountInfo = await _subAccountResolver.ResolveAsync(
                            subAccountType.Value,
                            subAccountId.Value,
                            cancellationToken
                        );

                        if (subAccountInfo != null)
                        {
                            subAccountName = subAccountInfo.Name;
                        }
                    }

                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = accountNo,
                        AccountName = accountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = accountEntry.NetOfVatAmount,
                        Credit = 0,
                        IsVatable = accountEntry.Vatable,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        SubAccountType = subAccountType,
                        SubAccountId = subAccountId,
                        SubAccountName = subAccountName,
                    });

                    if (accountEntry.Vatable)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    switch (accountEntry.TaxPercentage)
                    {
                        case 0.01m:
                            ewtOnePercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.02m:
                            ewtTwoPercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.05m:
                            ewtFivePercentAmount += accountEntry.TaxAmount;
                            break;

                        case 0.10m:
                            ewtTenPercentAmount += accountEntry.TaxAmount;
                            break;
                    }

                    apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;
                }

                existingModel.InvoiceAmount = apNontradeAmount;

                if (vatAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = vatAmount,
                        Credit = 0,
                    });
                }

                if (apNontradeAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = apNonTradeTitle.AccountNumber,
                        AccountName = apNonTradeTitle.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = apNontradeAmount,
                        Amount = apNontradeAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = existingModel.SupplierId,
                        SubAccountName = existingModel.SupplierName,
                    });
                }

                if (ewtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtOnePercentAmount,
                        Amount = ewtOnePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTwoPercentAmount,
                        Amount = ewtTwoPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtFivePercentAmount,
                        Amount = ewtFivePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (ewtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTenPercentAmount,
                        Amount = ewtTenPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId =  bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingModel.SupportingFileSavedFileName!);
                }

                #endregion -- Uploading file --

                // Capture BEFORE mutation
                var wasForPosting = existingModel.Status == nameof(CheckVoucherInvoiceStatus.ForPosting);

                if (existingModel.Status == nameof(CheckVoucherInvoiceStatus.ForPosting))
                {
                    existingModel.Status = nameof(CheckVoucherInvoiceStatus.ForApproval);
                    existingModel.ApprovedBy = null;
                    existingModel.ApprovedDate = null;
                }

                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                var auditMessage = wasForPosting
                    ? $"Edited check voucher# {existingModel.CheckVoucherHeaderNo} and reverted to For Approval"
                    : $"Edited check voucher# {existingModel.CheckVoucherHeaderNo}";

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), auditMessage, "Check Voucher", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Non-trade invoicing edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.Suppliers = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);
                viewModel.ChartOfAccounts = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, int? supplierId, int? employeeId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (id == null)
            {
                return NotFound();
            }

            var header = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var getSupplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId, cancellationToken);

            var getEmployee = await _unitOfWork.FilprideEmployee
                .GetAsync(s => s.EmployeeId == employeeId, cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier,
                Employee = getEmployee
            };

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview check voucher# {header.CheckVoucherHeaderNo}", "Check Voucher", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(viewModel);
        }

        public IActionResult GetAutomaticEntry(DateTime startDate, DateTime? endDate)
        {
            if (startDate != default && endDate != null)
            {
                return Json(true);
            }

            return Json(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            if (modelHeader.Status != nameof(CheckVoucherInvoiceStatus.ForPosting))
            {
                TempData["error"] = "This invoice must be approved before it can be posted.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }

            var modelDetails = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId && !cvd.IsDisplayEntry)
                .ToListAsync(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, modelHeader.Date, cancellationToken))
                {
                    throw new ArgumentException($"Cannot post this record because the period {modelHeader.Date:MMM yyyy} is already closed.");
                }

                modelHeader.PostedBy = GetUserFullName();
                modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                modelHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

                await _unitOfWork.FilprideCheckVoucher.PostAsync(modelHeader, modelDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher", modelHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Posted.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);

                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.Voided);

                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo, cancellationToken);

                //re-compute amount paid in trade and payment voucher

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Voided.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var cvHeader = await _dbContext.FilprideCheckVoucherHeaders
                    .Include(cv => cv.Details)
                    .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

                if (cvHeader == null)
                {
                    throw new NullReferenceException("CV Header not found.");
                }

                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, cvHeader.Date, cancellationToken))
                {
                    throw new ArgumentException($"Cannot unpost this record because the period {cvHeader.Date:MMM yyyy} is already closed.");
                }

                var userName = _userManager.GetUserName(this.User);
                if (userName == null)
                {
                    throw new NullReferenceException("User not found.");
                }

                if (cvHeader.Details!.Any(x => x.AmountPaid != 0) || cvHeader.AmountPaid != 0m)
                {
                    throw new ArgumentException("Payment for this invoice already exists, CV cannot be unposted.");
                }

                cvHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPosting);
                cvHeader.PostedBy = null;
                cvHeader.PostedDate = null;

                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == cvHeader.CheckVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == cvHeader.CheckVoucherHeaderNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Unposted check voucher# {cvHeader.CheckVoucherHeaderNo}", "Check Voucher", cvHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Unposted.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpost invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(GetUserFullName(), $"Printed re-printed copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id, supplierId });
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            if (supplierId == null)
            {
                return Json(null);
            }

            var companyClaims = await GetCompanyClaimAsync();

            var supplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId && (companyClaims == nameof(Filpride) ? s.IsFilpride : s.IsMobility));

            if (supplier == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Name = supplier.SupplierName,
                Address = supplier.SupplierAddress,
                TinNo = supplier.SupplierTin,
                supplier.TaxType,
                supplier.Category,
                TaxPercent = supplier.WithholdingTaxPercent,
                supplier.VatType,
                DefaultExpense = supplier.DefaultExpenseNumber,
                WithholdingTax = supplier.WithholdingTaxTitle,
                Vatable = supplier.VatType == SD.VatType_Vatable
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccounts()
        {
            var companyClaims = await GetCompanyClaimAsync();
            // Replace this with your actual repository/service call
            var bankAccounts = await _unitOfWork.FilprideBankAccount.GetAllAsync(b => (companyClaims == nameof(Filpride) ? b.IsFilpride : b.IsMobility));

            return Json(bankAccounts.Select(b => new
            {
                id = b.BankAccountId,
                accountName = b.AccountName,
                accountNumber = b.AccountNo
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccountById(int bankId)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var bankAccount = await _unitOfWork.FilprideBankAccount.GetAsync(b => b.BankAccountId == bankId && (companyClaims == nameof(Filpride) ? b.IsFilpride : b.IsMobility));

            if (bankAccount == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = bankAccount.BankAccountId,
                accountName = bankAccount.AccountName,
                accountNumber = bankAccount.AccountNo
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _unitOfWork.Company.GetAllAsync();

            return Json(companies.OrderBy(c => c.CompanyCode).Select(c => new
            {
                id = c.CompanyId,
                accountName = c.CompanyName,
                accountNumber = c.CompanyCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyById(int companyId)
        {
            var company = await _unitOfWork.Company.GetAsync(c => c.CompanyId == companyId);

            if (company == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = company.CompanyId,
                accountName = company.CompanyName,
                accountNumber = company.CompanyCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _unitOfWork.FilprideEmployee.GetAllAsync();

            return Json(employees.OrderBy(e => e.EmployeeNumber).Select(e => new
            {
                id = e.EmployeeId,
                accountName = $"{e.FirstName} {e.LastName}",
                accountNumber = e.EmployeeNumber
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var employee = await _unitOfWork.FilprideEmployee.GetAsync(e => e.EmployeeId == employeeId && e.Company == companyClaims);

            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = employee.EmployeeId,
                accountName = $"{employee.FirstName} {employee.LastName}",
                accountNumber = employee.EmployeeNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var companyClaims = await GetCompanyClaimAsync();
            var employees = await _unitOfWork.FilprideCustomer.GetAllAsync(c => (companyClaims == nameof(Filpride) ? c.IsFilpride : c.IsMobility));

            return Json(employees.OrderBy(c => c.CustomerCode).Select(c => new
            {
                id = c.CustomerId,
                accountName = c.CustomerName,
                accountNumber = c.CustomerCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerById(int customerId)
        {
            var customer = await _unitOfWork.FilprideCustomer
                .GetAsync(e => e.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = customer.CustomerId,
                accountName = customer.CustomerName,
                accountNumber = customer.CustomerCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var companyClaims = await GetCompanyClaimAsync();
            var suppliers = await _unitOfWork.FilprideSupplier.GetAllAsync(s => (companyClaims == nameof(Filpride) ? s.IsFilpride : s.IsMobility));

            return Json(suppliers.OrderBy(c => c.SupplierCode).Select(c => new
            {
                id = c.SupplierId,
                accountName = c.SupplierName,
                accountNumber = c.SupplierCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierById(int supplierId)
        {
            var supplier = await _unitOfWork.FilprideSupplier
                .GetAsync(e => e.SupplierId == supplierId);

            if (supplier == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = supplier.SupplierId,
                accountName = supplier.SupplierName,
                accountNumber = supplier.SupplierCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetNonTradeSupplierSelectList(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var selectList = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
            return Json(selectList);
        }
    }
}
