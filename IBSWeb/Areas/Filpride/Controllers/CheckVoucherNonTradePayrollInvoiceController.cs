using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD, SD.Department_HRAndAdminOrLegal)]
    public class CheckVoucherNonTradePayrollInvoiceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradeInvoiceController> _logger;

        private readonly ICacheService _cacheService;

        public CheckVoucherNonTradePayrollInvoiceController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradeInvoiceController> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
            _cacheService = cacheService;
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

                var checkVoucherDetails = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvd.CheckVoucherHeader!.Company == companyClaims &&
                                  cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                                  cvd.CheckVoucherHeader.IsPayroll &&
                                  cvd.SupplierId.HasValue &&
                                  cvd.Credit > 0)
                    .Include(cvd => cvd.Supplier)
                    .Include(cvd => cvd.CheckVoucherHeader)
                    .ThenInclude(cvh => cvh!.Supplier)
                    .ToListAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherDetails = checkVoucherDetails
                        .Where(s =>
                            s.TransactionNo.ToLower().Contains(searchValue) ||
                            s.CheckVoucherHeader!.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.AmountPaid.ToString().Contains(searchValue) ||
                            (s.Amount - s.AmountPaid).ToString().Contains(searchValue) ||
                            s.CheckVoucherHeader?.Status.ToLower().Contains(searchValue) == true ||
                            s.CheckVoucherHeader?.Particulars?.ToLower().Contains(searchValue) == true
                        )
                        .ToList();
                }

                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    checkVoucherDetails = checkVoucherDetails
                        .Where(s =>
                            s.CheckVoucherHeader!.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                var projectedQuery = checkVoucherDetails
                    .Select(x => new
                    {
                        x.TransactionNo,
                        x.CheckVoucherHeader!.Date,
                        Payee = x.Supplier!.SupplierName,
                        x.SupplierId,
                        x.Amount,
                        x.AmountPaid,
                        x.CheckVoucherHeader!.Status,
                        x.CheckVoucherHeader!.VoidedBy,
                        x.CheckVoucherHeader!.CanceledBy,
                        x.CheckVoucherHeader!.PostedBy,
                        x.CheckVoucherHeader!.IsPaid,
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

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
            viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

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
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                decimal apNonTradeTotal = 0m;

                for (int i = 0; i < viewModel.MultipleSupplierId!.Length; i++)
                {
                    if (viewModel.AccountTitle[i] == "AP-Non Trade Payable" && viewModel.Credit[i] != 0)
                    {
                        apNonTradeTotal += viewModel.Credit[i];
                    }
                }

                FilprideCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultipleInvoiceAsync(companyClaims, viewModel.Type!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Payee = null,
                    Address = "",
                    Tin = "",
                    PONo = [viewModel.PoNo ?? string.Empty],
                    SINo = [viewModel.SiNo ?? string.Empty],
                    SupplierId = null,
                    Particulars = viewModel.Particulars,
                    Total = viewModel.Total,
                    CreatedBy = GetUserFullName(),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Invoicing),
                    Company = companyClaims,
                    Type = viewModel.Type,
                    InvoiceAmount = apNonTradeTotal,
                    TaxType = string.Empty,
                    VatType = string.Empty,
                    IsPayroll = true
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(checkVoucherHeader, cancellationToken);

                #endregion -- Saving the default entries --

                #region -- cv invoiving details entry --

                List<FilprideCheckVoucherDetail> checkVoucherDetails = new();

                for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = viewModel.AccountNumber[i],
                            AccountName = viewModel.AccountTitle[i],
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            Amount = viewModel.AccountTitle[i] == "AP-Non Trade Payable" ? viewModel.Credit[i] : 0m,
                            SupplierId = viewModel.MultipleSupplierId![i] != 0 ? viewModel.MultipleSupplierId[i] : null,
                            IsUserSelected = true
                        });
                    }
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
                _logger.LogError(ex, "Failed to create payroll invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            try
            {
                var existingHeaderModel =
                    await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id,
                        cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate =
                    await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (existingHeaderModel.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .OrderBy(s => s.CheckVoucherDetailId)
                    .ToListAsync(cancellationToken);

                var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
                var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
                var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
                var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var coa = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

                var suppliers =
                    await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                var details = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(s => s.Supplier)
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(cancellationToken);

                var payees = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .OrderBy(s => s.CheckVoucherDetailId)
                    .Select(s => new
                    {
                        PayeeId = (int?)(s.SupplierId ?? s.EmployeeId ?? 0),
                        PayeeName = s.SupplierId != null
                            ? $"{s.Supplier!.SupplierCode} {s.Supplier.SupplierName}"
                            : s.EmployeeId != null
                            ? $"{s.Employee!.EmployeeNumber} {s.Employee.FirstName} {s.Employee.LastName}"
                            : ""
                    })
                    .ToListAsync(cancellationToken);

                var getSupplierId = payees.Select(p => p.PayeeId).ToArray();
                var getPayeeName = payees.Select(p => p.PayeeName).ToArray();

                CheckVoucherNonTradeInvoicingViewModel model = new()
                {
                    MultipleSupplierId = getSupplierId,
                    SupplierAddress = details?.Supplier?.SupplierAddress,
                    SupplierTinNo = details?.Supplier?.SupplierTin,
                    Suppliers = suppliers,
                    TransactionDate = existingHeaderModel.Date,
                    Particulars = existingHeaderModel.Particulars!,
                    Total = existingHeaderModel.Total,
                    AccountNumber = accountNumbers,
                    AccountTitle = accountTitles,
                    Debit = debit,
                    Credit = credit,
                    ChartOfAccounts = coa,
                    CVId = existingHeaderModel.CheckVoucherHeaderId,
                    PoNo = existingHeaderModel.PONo?.First(),
                    SiNo = existingHeaderModel.SINo?.First(),
                    MinDate = minDate,
                    SupplierNames = getPayeeName
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv non trade payroll invoice. Error: {ErrorMessage}, Stack: {StackTrace}.",
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
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                decimal apNonTradeTotal = 0m;

                for (int i = 0; i < viewModel.MultipleSupplierId!.Length; i++)
                {
                    if (viewModel.AccountTitle[i] == "AP-Non Trade Payable" && viewModel.Credit[i] != 0)
                    {
                        apNonTradeTotal += viewModel.Credit[i];
                    }
                }

                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.PONo = [viewModel.PoNo ?? string.Empty];
                existingHeaderModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.InvoiceAmount = apNonTradeTotal;

                #endregion -- Saving the default entries --

                #region -- cv invoiving details entry --

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken: cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                List<FilprideCheckVoucherDetail> checkVoucherDetails = new();

                for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = viewModel.AccountNumber[i],
                            AccountName = viewModel.AccountTitle[i],
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                            CheckVoucherHeaderId = viewModel.CVId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            Amount = viewModel.AccountTitle[i] == "AP-Non Trade Payable" ? viewModel.Credit[i] : 0m,
                            SupplierId = viewModel.MultipleSupplierId![i] != 0 ? viewModel.MultipleSupplierId[i] : null
                        });
                    }
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- cv invoiving details entry --

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher invoicing edited successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit payroll invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

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

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (cvHeader.Date < DateOnly.FromDateTime(minDate))
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
                .Include(cvd => cvd.Supplier)
                .Include(cvd => cvd.BankAccount)
                .Include(cvd => cvd.Company)
                .Include(cvd => cvd.Customer)
                .Include(cvd => cvd.Employee)
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

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            var modelDetails = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId)
                .Include(cvd => cvd.Customer)
                .Include(cvd => cvd.Employee)
                .Include(cvd => cvd.Company)
                .ToListAsync(cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (modelHeader.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException($"Cannot post this record because the period {modelHeader.Date:MMM yyyy} is already closed.");
                }

                modelHeader.PostedBy = GetUserFullName();
                modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                modelHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

                #region --General Ledger Book Recording(CV)--

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var ledgers = new List<FilprideGeneralLedgerBook>();
                foreach (var details in modelDetails)
                {
                    var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account title '{details.AccountNo}' not found.");
                    ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = modelHeader.Date,
                                Reference = modelHeader.CheckVoucherHeaderNo!,
                                Description = modelHeader.Particulars!,
                                AccountId = account.AccountId,
                                AccountNo = account.AccountNumber,
                                AccountTitle = account.AccountName,
                                Debit = details.Debit,
                                Credit = details.Credit,
                                Company = modelHeader.Company,
                                CreatedBy = modelHeader.PostedBy,
                                CreatedDate = modelHeader.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                BankAccountId = details.BankId,
                                BankAccountName = modelHeader.BankId.HasValue ? $"{modelHeader.BankAccountNumber} {modelHeader.BankAccountName}" : null,
                                SupplierId = details.SupplierId,
                                SupplierName = modelHeader.SupplierName,
                                CustomerId = details.CustomerId,
                                CustomerName = details.Customer?.CustomerName,
                                CompanyId = details.CompanyId,
                                CompanyName = details.Company?.CompanyName,
                                EmployeeId = details.EmployeeId,
                                EmployeeName = details.EmployeeId.HasValue ? $"{details.Employee?.FirstName} {details.Employee?.MiddleName} {details.Employee?.LastName}" : null,
                                ModuleType = nameof(ModuleType.Disbursement)
                            }
                        );
                }

                if (!_unitOfWork.FilprideCheckVoucher.IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                #endregion --General Ledger Book Recording(CV)--

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

    }
}
