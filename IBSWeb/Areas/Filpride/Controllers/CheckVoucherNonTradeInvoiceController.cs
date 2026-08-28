using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
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
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DTOs;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore.Storage;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CheckVoucherNonTradeInvoiceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradeInvoiceController> _logger;

        private readonly ISubAccountResolver _subAccountResolver;

        private const string FilterTypeClaimType = "CheckVoucherNonTradeInvoice.FilterType";

        public CheckVoucherNonTradeInvoiceController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradeInvoiceController> logger,
            ISubAccountResolver subAccountResolver)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _subAccountResolver = subAccountResolver;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        private async Task<string?> GetCompanyClaimAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            IList<Claim> claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private async Task UpdateFilterTypeClaim(string filterType)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Claim? existingClaim = (await _userManager.GetClaimsAsync(user))
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
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            IList<Claim> claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == FilterTypeClaimType)?.Value;
        }

        private string GenerateFileNameToSave(string incomingFileName)
        {
            string fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            string extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        public async Task<IActionResult> Index(string filterType)
        {
            await UpdateFilterTypeClaim(filterType);
            ViewBag.FilterType = await GetCurrentFilterType();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoiceCheckVouchers([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                string? companyClaims = await GetCompanyClaimAsync();
                string? filterTypeClaim = await GetCurrentFilterType();

                IQueryable<FilprideCheckVoucherHeader> checkVoucher = _unitOfWork.FilprideCheckVoucher
                    .GetAllQuery()
                    .Where(cvh => 
                                  cvh.CvType == nameof(CVType.Invoicing) &&
                                  !cvh.IsPayroll);

                int totalRecords = await checkVoucher.CountAsync(cancellationToken);

                // Apply status filter based on filterType
                if (!string.IsNullOrEmpty(filterTypeClaim) && filterTypeClaim == "ForApproval")
                {
                    checkVoucher = checkVoucher.Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval));
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    string searchValue = parameters.Search.Value.ToLower();
                    bool hasDate = DateOnly.TryParse(searchValue, out DateOnly date);

                    checkVoucher = checkVoucher
                        .Where(s =>
                            s.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                            (hasDate && s.Date == date) ||
                            (s.Payee != null &&
                            s.Payee.ToLower().Contains(searchValue) == true) ||
                            s.InvoiceAmount.ToString().Contains(searchValue) ||
                            s.AmountPaid.ToString().Contains(searchValue) ||
                            (s.InvoiceAmount - s.AmountPaid).ToString().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue) ||
                            (s.Particulars != null &&
                            s.Particulars.ToLower().Contains(searchValue) == true)
                        );
                }

                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    checkVoucher = checkVoucher.Where(s => s.Date == filterDate);
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    DataTablesOrder orderColumn = parameters.Order[0];
                    string columnName = parameters.Columns[orderColumn.Column].Name;
                    string sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    checkVoucher = checkVoucher
                        .OrderBy($"{columnName} {sortDirection}");
                }

                int totalFilteredRecords = await checkVoucher.CountAsync(cancellationToken);

                var pagedData = await checkVoucher
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
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
                _logger.LogError(ex, "Failed to get invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        public async Task<IActionResult> GetDefaultExpense(int? supplierId)
        {
            string? supplier = (await _unitOfWork.FilprideSupplier
                    .GetAsync(supp => supp.SupplierId == supplierId))!.DefaultExpenseNumber;

            var defaultExpense = (await _unitOfWork.FilprideChartOfAccount
                .GetAllAsync(coa => (coa.Level == 4 || coa.Level == 5)))
                .OrderBy(coa => coa.AccountId)
                .ToList();

            if (defaultExpense.Count <= 0)
            {
                return Json(null);
            }

            var defaultExpenseList = defaultExpense.Select(coa => new
            {
                coa.AccountNumber,
                AccountTitle = coa.AccountName,
                IsSelected = coa.AccountNumber == supplier?.Split(' ')[0]
            }).ToList();

            return Json(defaultExpenseList);

        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceCreate))]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting CheckVoucherNonTradeInvoice/Create GET for user {UserName}.",
                _userManager.GetUserName(User));

            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            string? companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                _logger.LogWarning("CheckVoucherNonTradeInvoice/Create GET aborted because company claim is missing for user {UserName}.",
                    _userManager.GetUserName(User));
                return BadRequest();
            }

            List<SelectListItem> coaSelectList = await _unitOfWork
                .GetChartOfAccountListAsyncByAccountTitle(cancellationToken);

            _logger.LogInformation("CheckVoucherNonTradeInvoice/Create GET loaded {CoaCount} chart of account items for company {Company}.",
                coaSelectList.Count, companyClaims);

            List<SelectListItem> supplierSelectList = await _unitOfWork
                .GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

            _logger.LogInformation("CheckVoucherNonTradeInvoice/Create GET loaded {SupplierCount} supplier items for company {Company}.",
                supplierSelectList.Count, companyClaims);

            DateTime minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            viewModel.ChartOfAccounts = coaSelectList;
            viewModel.Suppliers = supplierSelectList;
            viewModel.MinDate = minDate;

            _logger.LogInformation("CheckVoucherNonTradeInvoice/Create GET completed for company {Company}. MinDate: {MinDate}.",
                companyClaims, minDate);

            return View(viewModel);
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceCreate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            string? companyClaims = await GetCompanyClaimAsync();

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

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                #region --Retrieve Supplier

                FilprideSupplier? supplier = await _unitOfWork.FilprideSupplier
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

                List<FilprideCheckVoucherDetail> checkVoucherDetails = [];

                decimal apNontradeAmount = 0;
                decimal vatAmount = 0;
                decimal ewtOnePercentAmount = 0;
                decimal ewtTwoPercentAmount = 0;
                decimal ewtFivePercentAmount = 0;
                decimal ewtTenPercentAmount = 0;
                decimal reverseVatAmount = 0;
                decimal reverseEwtOnePercentAmount = 0;
                decimal reverseEwtTwoPercentAmount = 0;
                decimal reverseEwtFivePercentAmount = 0;
                decimal reverseEwtTenPercentAmount = 0;

                List<AccountTitleDto> accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                AccountTitleDto apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "201020200") ?? throw new ArgumentException("Account title '201020200' not found.");
                AccountTitleDto vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                AccountTitleDto ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                AccountTitleDto ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                AccountTitleDto ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                AccountTitleDto ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");
                FilprideSupplier? bir = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"), cancellationToken);

                foreach (AccountingEntryViewModel accountEntry in viewModel.AccountingEntries!)
                {
                    string[] parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    string accountNo = parts[0];
                    string accountName = parts[1];

                    (SubAccountType? subAccountType, int? subAccountId) = SubAccountHelper.DetermineCvSubAccount(
                        accountEntry.CustomerMasterFileId,
                        accountEntry.SupplierMasterFileId,
                        accountEntry.BankMasterFileId,
                        accountEntry.CompanyMasterFileId,
                        accountEntry.EmployeeMasterFileId
                    );

                    string? subAccountName = null;

                    if (subAccountType.HasValue && subAccountId.HasValue)
                    {
                        SubAccountInfoDto? subAccountInfo = await _subAccountResolver.ResolveAsync(
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
                        Debit = accountEntry.NetOfVatAmount < 0 ? 0 : accountEntry.NetOfVatAmount,
                        Credit = accountEntry.NetOfVatAmount < 0 ? Math.Abs(accountEntry.NetOfVatAmount) : 0,
                        IsVatable = accountEntry.Vatable,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        SubAccountType = subAccountType,
                        SubAccountId = subAccountId,
                        SubAccountName = subAccountName,
                    });

                    if (accountEntry.Vatable && accountEntry.VatAmount > 0)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    if (accountEntry.TaxAmount > 0)
                    {
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
                    }

                    if (accountEntry.VatAmount < 0)
                    {
                        reverseVatAmount += Math.Abs(accountEntry.VatAmount);
                    }

                    // Check EWT percentage
                    if (accountEntry.TaxAmount < 0)
                    {
                        switch (accountEntry.TaxPercentage)
                        {
                            case 0.01m:
                                reverseEwtOnePercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.02m:
                                reverseEwtTwoPercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.05m:
                                reverseEwtFivePercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.10m:
                                reverseEwtTenPercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;
                        }
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

                if (reverseVatAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = reverseVatAmount,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = reverseEwtOnePercentAmount,
                        Credit = 0,
                        Amount = reverseEwtOnePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = reverseEwtTwoPercentAmount,
                        Credit = 0,
                        Amount = reverseEwtTwoPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = reverseEwtFivePercentAmount,
                        Credit = 0,
                        Amount = reverseEwtFivePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = reverseEwtTenPercentAmount,
                        Credit = 0,
                        Amount = reverseEwtTenPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher invoicing #{checkVoucherHeader.CheckVoucherHeaderNo} created successfully.";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

        [Authorize(Roles = "Admin,AccountingManager")]
        public async Task<IActionResult> Approve(int id, int? supplierId, CancellationToken cancellationToken)
        {
            FilprideCheckVoucherHeader? model = await _unitOfWork.FilprideCheckVoucher
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

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.ApprovedBy = GetUserFullName();
                model.ApprovedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.ForPosting);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Approved check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher");
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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceEdit))]
        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                string? companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                FilprideCheckVoucherHeader? existingModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                DateTime minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, existingModel.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingModel.Date:MMM yyyy} is already closed.");
                }

                List<FilprideCheckVoucherDetail> existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
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

                foreach (FilprideCheckVoucherDetail details in existingDetailsModel)
                {
                    bool isCredit = details.IsUserSelected && details.Credit != 0;

                    decimal baseAmount = isCredit
                        ? -details.Credit
                        : details.Debit;

                    decimal computedAmount = details.IsVatable
                        ? DecimalRoundingHelper.MultiplyAndRound(baseAmount, 1.12m)
                        : DecimalRoundingHelper.RoundToFour(baseAmount);

                    viewModel.AccountingEntries.Add(new AccountingEntryViewModel
                    {
                        AccountTitle = $"{details.AccountNo} {details.AccountName}",
                        Amount = computedAmount,
                        Vatable = details.IsVatable,
                        TaxPercentage = details.EwtPercent,
                        BankMasterFileId = details.SubAccountType == SubAccountType.BankAccount
                            ? details.SubAccountId
                            : null,
                        CompanyMasterFileId = details.SubAccountType == SubAccountType.Company
                            ? details.SubAccountId
                            : null,
                        CustomerMasterFileId = details.SubAccountType == SubAccountType.Customer
                            ? details.SubAccountId
                            : null,
                        SupplierMasterFileId = details.SubAccountType == SubAccountType.Supplier
                            ? details.SubAccountId
                            : null,
                        EmployeeMasterFileId = details.SubAccountType == SubAccountType.Employee
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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceEdit))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            string? companyClaims = await GetCompanyClaimAsync();

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

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                FilprideCheckVoucherHeader? existingModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                #region -- Get supplier

                FilprideSupplier? supplier = await _unitOfWork.FilprideSupplier
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

                List<FilprideCheckVoucherDetail> existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
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
                decimal reverseVatAmount = 0;
                decimal reverseEwtOnePercentAmount = 0;
                decimal reverseEwtTwoPercentAmount = 0;
                decimal reverseEwtFivePercentAmount = 0;
                decimal reverseEwtTenPercentAmount = 0;

                List<AccountTitleDto> accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                AccountTitleDto apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "201020200") ?? throw new ArgumentException("Account title '201020200' not found.");
                AccountTitleDto vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                AccountTitleDto ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                AccountTitleDto ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                AccountTitleDto ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                AccountTitleDto ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");
                FilprideSupplier? bir = await _unitOfWork.FilprideSupplier
                    .GetAsync(x => x.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"), cancellationToken);

                foreach (AccountingEntryViewModel accountEntry in viewModel.AccountingEntries!)
                {
                    string[] parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    string accountNo = parts[0];
                    string accountName = parts[1];

                    (SubAccountType? subAccountType, int? subAccountId) = SubAccountHelper.DetermineCvSubAccount(
                        accountEntry.CustomerMasterFileId,
                        accountEntry.SupplierMasterFileId,
                        accountEntry.BankMasterFileId,
                        accountEntry.CompanyMasterFileId,
                        accountEntry.EmployeeMasterFileId
                    );

                    string? subAccountName = null;

                    if (subAccountType.HasValue && subAccountId.HasValue)
                    {
                        SubAccountInfoDto? subAccountInfo = await _subAccountResolver.ResolveAsync(
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
                        Debit = accountEntry.NetOfVatAmount < 0 ? 0 : accountEntry.NetOfVatAmount,
                        Credit = accountEntry.NetOfVatAmount < 0 ? Math.Abs(accountEntry.NetOfVatAmount) : 0,
                        IsVatable = accountEntry.Vatable,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        SubAccountType = subAccountType,
                        SubAccountId = subAccountId,
                        SubAccountName = subAccountName,
                    });

                    if (accountEntry.Vatable && accountEntry.VatAmount > 0)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    if (accountEntry.TaxAmount > 0)
                    {
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
                    }

                    if (accountEntry.VatAmount < 0)
                    {
                        reverseVatAmount += Math.Abs(accountEntry.VatAmount);
                    }

                    // Check EWT percentage
                    if (accountEntry.TaxAmount < 0)
                    {
                        switch (accountEntry.TaxPercentage)
                        {
                            case 0.01m:
                                reverseEwtOnePercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.02m:
                                reverseEwtTwoPercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.05m:
                                reverseEwtFivePercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;

                            case 0.10m:
                                reverseEwtTenPercentAmount += Math.Abs(accountEntry.TaxAmount);
                                break;
                        }
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

                if (reverseVatAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = reverseVatAmount,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
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
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = reverseEwtOnePercentAmount,
                        Credit = 0,
                        Amount = reverseEwtOnePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = reverseEwtTwoPercentAmount,
                        Credit = 0,
                        Amount = reverseEwtTwoPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = reverseEwtFivePercentAmount,
                        Credit = 0,
                        Amount = reverseEwtFivePercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
                        SubAccountName = bir.SupplierName,
                    });
                }

                if (reverseEwtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = reverseEwtTenPercentAmount,
                        Credit = 0,
                        Amount = reverseEwtTenPercentAmount,
                        SubAccountType = SubAccountType.Supplier,
                        SubAccountId = bir!.SupplierId,
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
                bool wasForPosting = existingModel.Status == nameof(CheckVoucherInvoiceStatus.ForPosting);

                if (existingModel.Status == nameof(CheckVoucherInvoiceStatus.ForPosting))
                {
                    existingModel.Status = nameof(CheckVoucherInvoiceStatus.ForApproval);
                    existingModel.ApprovedBy = null;
                    existingModel.ApprovedDate = null;
                }

                #region --Audit Trail Recording

                string auditMessage = wasForPosting
                    ? $"Edited check voucher# {existingModel.CheckVoucherHeaderNo} and reverted to For Approval"
                    : $"Edited check voucher# {existingModel.CheckVoucherHeaderNo}";

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), auditMessage, "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Non-trade invoicing edited successfully";
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
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

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoicePreview))]
        [HttpGet]
        public async Task<IActionResult> Print(int? id, int? supplierId, int? employeeId, CancellationToken cancellationToken)
        {
            string? companyClaims = await GetCompanyClaimAsync();

            if (id == null)
            {
                return NotFound();
            }

            FilprideCheckVoucherHeader? header = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            List<FilprideCheckVoucherDetail> details = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            FilprideSupplier? getSupplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId, cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview check voucher# {header.CheckVoucherHeaderNo}", "Check Voucher");
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            ViewBag.FilterType = await GetCurrentFilterType();
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

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoicePost))]
        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            FilprideCheckVoucherHeader? modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            if (modelHeader.PostedBy != null)
            {
                TempData["info"] = "Check Voucher has already been posted.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }

            if (modelHeader.Status != nameof(CheckVoucherInvoiceStatus.ForPosting))
            {
                TempData["error"] = "This invoice must be approved before it can be posted.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
            }

            List<FilprideCheckVoucherDetail> modelDetails = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId && !cvd.IsDisplayEntry)
                .ToListAsync(cancellationToken);

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, modelHeader.Date, cancellationToken))
                {
                    TempData["error"] = $"Cannot post this record because the period {modelHeader.Date:MMM yyyy} is already closed.";
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }

                modelHeader.PostedBy = GetUserFullName();
                modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                modelHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

                await _unitOfWork.FilprideCheckVoucher.PostAsync(modelHeader, modelDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher");
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
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceCancel))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            FilprideCheckVoucherHeader? model = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Check Voucher #{model.CheckVoucherHeaderNo} has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            FilprideCheckVoucherHeader? model = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = null;
                model.VoidedBy = GetUserFullName();
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(CheckVoucherInvoiceStatus.Voided);

                await _unitOfWork.GeneralLedger.ReverseEntries(model.CheckVoucherHeaderNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);

                return Json(new { success = true, message = $"Check Voucher #{model.CheckVoucherHeaderNo} has been voided successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoiceUnpost))]
        public async Task<IActionResult> Unpost(int id, int? supplierId, CancellationToken cancellationToken)
        {
            await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                FilprideCheckVoucherHeader cvHeader = await _dbContext.FilprideCheckVoucherHeaders
                                                          .Include(cv => cv.Details)
                                                          .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken)
                                                      ?? throw new NullReferenceException("CV Header not found.");

                if (await _unitOfWork.IsPeriodPostedAsync(Module.CheckVoucher, cvHeader.Date, cancellationToken))
                {
                    TempData["error"] = $"Cannot unpost this record because the period {cvHeader.Date:MMM yyyy} is already closed.";
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }

                if (cvHeader.Details!.Any(x => x.AmountPaid != 0) || cvHeader.AmountPaid != 0m)
                {
                    TempData["error"] = "Payment for this invoice already exists, CV cannot be unposted.";
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }

                cvHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPosting);
                cvHeader.PostedBy = null;
                cvHeader.PostedDate = null;

                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == cvHeader.CheckVoucherHeaderNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Unposted check voucher# {cvHeader.CheckVoucherHeaderNo}", "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Unposted.";

                return RedirectToAction(nameof(Print), new { id, supplierId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpost invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { filterType = await GetCurrentFilterType() });
            }
        }

        [Authorize(Policy = nameof(CheckVoucherNonTradeInvoice.CheckVoucherNonTradeInvoicePreview))]
        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
        {
            FilprideCheckVoucherHeader? cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(GetUserFullName(), $"Printed re-printed copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher");
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

            string? companyClaims = await GetCompanyClaimAsync();

            FilprideSupplier? supplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId && companyClaims == nameof(Filpride));

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
            string? companyClaims = await GetCompanyClaimAsync();
            // Replace this with your actual repository/service call
            IEnumerable<FilprideBankAccount> bankAccounts = await _unitOfWork.FilprideBankAccount
                .GetAllAsync(b => companyClaims == nameof(Filpride));

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
            string? companyClaims = await GetCompanyClaimAsync();
            FilprideBankAccount? bankAccount = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == bankId && companyClaims == nameof(Filpride));

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
            IEnumerable<Company> companies = await _unitOfWork.Company.GetAllAsync();

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
            Company? company = await _unitOfWork.Company.GetAsync(c => c.CompanyId == companyId);

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
            string? companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            IEnumerable<FilprideSupplier> employees = await _unitOfWork.FilprideSupplier
                .GetAllAsync(s => s.IsActive && s.Category == "Employee");

            return Json(employees.OrderBy(e => e.EmployeeNumber).ThenBy(e => e.SupplierName).Select(e => new
            {
                id = e.SupplierId,
                accountName = e.SupplierName,
                accountNumber = e.EmployeeNumber
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            string? companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            FilprideSupplier? employee = await _unitOfWork.FilprideSupplier
                .GetAsync(e => e.SupplierId == employeeId && e.Category == "Employee");

            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = employee.SupplierId,
                accountName = employee.SupplierName,
                accountNumber = employee.EmployeeNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            string? companyClaims = await GetCompanyClaimAsync();
            IEnumerable<FilprideCustomer> employees = await _unitOfWork.FilprideCustomer
                .GetAllAsync(c => companyClaims == nameof(Filpride));

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
            FilprideCustomer? customer = await _unitOfWork.FilprideCustomer
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
            string? companyClaims = await GetCompanyClaimAsync();
            IEnumerable<FilprideSupplier> suppliers = await _unitOfWork.FilprideSupplier
                .GetAllAsync(s => companyClaims == nameof(Filpride) && s.Category != "Employee");

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
            FilprideSupplier? supplier = await _unitOfWork.FilprideSupplier
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
            string? companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            List<SelectListItem> selectList = await _unitOfWork
                .GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

            return Json(selectList);
        }
    }
}
