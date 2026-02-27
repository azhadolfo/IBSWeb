using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<JournalVoucherController> _logger;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<JournalVoucherController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.JournalVoucher))
            {
                return View("ExportIndex");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetJournalVouchers([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var journalVoucherHeader = await _unitOfWork.FilprideJournalVoucher
                    .GetAllAsync(jv => jv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    journalVoucherHeader = journalVoucherHeader
                    .Where(s =>
                        s.JournalVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.References?.Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) == true ||
                        s.Particulars.ToLower().Contains(searchValue) ||
                        s.CRNo?.ToLower().Contains(searchValue) == true ||
                        s.JVReason.ToLower().ToString().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    journalVoucherHeader = journalVoucherHeader
                        .Where(s =>
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    journalVoucherHeader = journalVoucherHeader
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = journalVoucherHeader.Count();

                var pagedData = journalVoucherHeader
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
                _logger.LogError(ex, "Failed to get journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateLiquidation(CancellationToken cancellationToken)
        {
            var viewModel = new JournalVoucherViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.IsAdvances &&
                    c.EmployeeId != null &&
                    c.Status == nameof(CheckVoucherPaymentStatus.Unliquidated))
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLiquidation(JournalVoucherViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
            viewModel.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.IsAdvances &&
                    c.EmployeeId != null &&
                    c.Status == nameof(CheckVoucherPaymentStatus.Unliquidated))
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);
            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                var generateJvNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken: cancellationToken);
                //JV Header Entry
                var model = new FilprideJournalVoucherHeader
                {
                    Type = viewModel.Type,
                    JournalVoucherHeaderNo = generateJvNo,
                    Date = viewModel.TransactionDate,
                    References = viewModel.References,
                    CVId = viewModel.CVId,
                    Particulars = viewModel.Particulars,
                    CRNo = viewModel.CRNo,
                    JVReason = viewModel.JVReason,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    JvType = nameof(JvType.Liquidation)
                };

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region Details

                var cv = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(x => x.CheckVoucherHeaderId == model.CVId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {model.CVId} not found");

                var jvDetails = new List<FilprideJournalVoucherDetail>();
                for (var i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    var currentAccountNumber = viewModel.AccountNumber[i];
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {currentAccountNumber} not found");

                    var isAdvances = accountTitle.AccountName.Contains("Advances to Officers and Employees");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = generateJvNo,
                            JournalVoucherHeaderId = model.JournalVoucherHeaderId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            SubAccountType = isAdvances ? SubAccountType.Employee : null,
                            SubAccountId = isAdvances ? cv.EmployeeId : null,
                            SubAccountName = isAdvances ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Created new journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Journal voucher # {model.JournalVoucherHeaderNo} created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetCV(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders
                .Include(s => s.Supplier)
                .Include(cvd => cvd.Details)
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (model != null)
            {
                return Json(new
                {
                    CVNo = model.CheckVoucherHeaderNo,
                    model.Date,
                    Name = model.SupplierName,
                    Address = model.Address,
                    TinNo = model.Tin,
                    model.PONo,
                    model.SINo,
                    model.Payee,
                    Amount = model.Total,
                    model.Particulars,
                    model.CheckNo,
                    AccountNo = model.Details!.Select(jvd => jvd.AccountNo),
                    AccountName = model.Details!.Select(jvd => jvd.AccountName),
                    Debit = model.Details!.Select(jvd => jvd.Debit),
                    Credit = model.Details!.Select(jvd => jvd.Credit),
                    TotalDebit = model.Details!.Sum(cvd => cvd.Debit),
                    TotalCredit = model.Details!.Sum(cvd => cvd.Credit),
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var header = await _dbContext.FilprideJournalVoucherHeaders
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier!.Supplier)
                .Include(jv => jv.Details)
                .FirstOrDefaultAsync(jvh => jvh.JournalVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var viewModel = new JournalVoucherVM
            {
                Header = header,
                Details = header.Details!.ToList(),
            };

            viewModel.IsAmortization = await _dbContext
                .JvAmortizationSettings
                .AnyAsync(jv => jv.JvId == id.Value && jv.IsActive, cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview journal voucher# {header.JournalVoucherHeaderNo}", "Journal Voucher", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var modelHeader = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

                if (modelHeader == null)
                {
                    return NotFound();
                }

                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, modelHeader.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot post this record because the period {modelHeader.Date:MMM yyyy} is already closed.");
                }

                var modelDetails = await _dbContext.FilprideJournalVoucherDetails
                    .Where(jvd => jvd.JournalVoucherHeaderId == modelHeader.JournalVoucherHeaderId)
                    .ToListAsync(cancellationToken: cancellationToken);

                modelHeader.PostedBy = GetUserFullName();
                modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                modelHeader.Status = nameof(Status.Posted);

                if (modelHeader.JvType == nameof(JvType.Accrual))
                {
                    await ReverseAccrual(modelHeader.JournalVoucherHeaderId, cancellationToken);
                }

                if (modelHeader.JvType == nameof(JvType.Amortization) && !await ProcessAmortizationAsync(modelHeader.JournalVoucherHeaderId, cancellationToken))
                {
                    throw new InvalidOperationException($"Cannot post this record because the amortization has not been completed yet.");
                }

                await _unitOfWork.FilprideJournalVoucher.PostAsync(modelHeader, modelDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy!, $"Posted journal voucher# {modelHeader.JournalVoucherHeaderNo}", "Journal Voucher", modelHeader.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher has been Posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideJournalVoucherHeaders
                .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

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
                model.Status = nameof(Status.Voided);

                await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideJournalBook>(crb => crb.Reference == model.JournalVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.JournalVoucherHeaderNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher has been Voided.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var model = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

                if (model == null)
                {
                    return NotFound();
                }

                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, model.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot cancel this record because the period {model.Date:MMM yyyy} is already closed.");
                }

                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLiquidation(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .Include(jv => jv.CheckVoucherHeader)
                    .FirstOrDefaultAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);
                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, existingHeaderModel.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails
                    .Where(cvd => cvd.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
                var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
                var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
                var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

                JournalVoucherViewModel model = new()
                {
                    JVId = existingHeaderModel.JournalVoucherHeaderId,
                    JVNo = existingHeaderModel.JournalVoucherHeaderNo,
                    TransactionDate = existingHeaderModel.Date,
                    References = existingHeaderModel.References,
                    CVId = existingHeaderModel.CVId,
                    Particulars = existingHeaderModel.Particulars,
                    CRNo = existingHeaderModel.CRNo,
                    JVReason = existingHeaderModel.JVReason,
                    AccountNumber = accountNumbers,
                    AccountTitle = accountTitles,
                    Debit = debit,
                    Credit = credit,
                    CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                        .OrderBy(c => c.CheckVoucherHeaderNo)
                        .Where(c =>
                            c.Company == companyClaims &&
                            c.IsAdvances &&
                            c.EmployeeId != null &&
                            c.Status == nameof(CheckVoucherPaymentStatus.Unliquidated))
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync(cancellationToken),
                    COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                    MinDate = minDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch jv. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLiquidation(JournalVoucherViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            viewModel.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
            viewModel.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.IsAdvances &&
                    c.EmployeeId != null &&
                    c.Status == nameof(CheckVoucherPaymentStatus.Unliquidated))
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);
            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == viewModel.JVId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                await _dbContext.FilprideJournalVoucherDetails
                    .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Saving the default entries

                existingHeaderModel.JournalVoucherHeaderNo = viewModel.JVNo;
                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.References = viewModel.References;
                existingHeaderModel.CVId = viewModel.CVId;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.CRNo = viewModel.CRNo;
                existingHeaderModel.JVReason = viewModel.JVReason;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion --Saving the default entries

                #region Details

                var cv = await _unitOfWork.FilprideCheckVoucher
                             .GetAsync(x => x.CheckVoucherHeaderId == existingHeaderModel.CVId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {existingHeaderModel.CVId} not found");

                var jvDetails = new List<FilprideJournalVoucherDetail>();
                for (var i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    var currentAccountNumber = viewModel.AccountNumber[i];
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {currentAccountNumber} not found");

                    var isAdvances = accountTitle.AccountName.Contains("Advances to Officers and Employees");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = existingHeaderModel.JournalVoucherHeaderNo!,
                            JournalVoucherHeaderId = existingHeaderModel.JournalVoucherHeaderId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            SubAccountType = isAdvances ? SubAccountType.Employee : null,
                            SubAccountId = isAdvances ? cv.EmployeeId : null,
                            SubAccountName = isAdvances ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", existingHeaderModel.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideJournalVoucher.GetAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (cv.IsPrinted == false)
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Printed original copy of journal voucher# {cv.JournalVoucherHeaderNo}", "Journal Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Printed re-printed copy of journal voucher# {cv.JournalVoucherHeaderNo}", "Journal Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetJournalVoucherList(
            [FromForm] DataTablesParameters parameters,
            DateTime? dateFrom,
            DateTime? dateTo,
            CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var journalVoucherHeaders = await _unitOfWork.FilprideJournalVoucher
                    .GetAllAsync(jv => jv.Company == companyClaims && jv.Type == nameof(DocumentType.Documented), cancellationToken);

                // Apply firstDayOfNextMonth range filter if provided
                if (dateFrom.HasValue)
                {
                    journalVoucherHeaders = journalVoucherHeaders
                        .Where(s => s.Date >= DateOnly.FromDateTime(dateFrom.Value))
                        .ToList();
                }

                if (dateTo.HasValue)
                {
                    journalVoucherHeaders = journalVoucherHeaders
                        .Where(s => s.Date <= DateOnly.FromDateTime(dateTo.Value))
                        .ToList();
                }

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    journalVoucherHeaders = journalVoucherHeaders
                        .Where(s =>
                            (s.JournalVoucherHeaderNo != null && s.JournalVoucherHeaderNo.ToLower().Contains(searchValue)) ||
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            (s.CheckVoucherHeader?.CheckVoucherHeaderNo != null && s.CheckVoucherHeader.CheckVoucherHeaderNo.ToLower().Contains(searchValue)) ||
                            (s.CRNo != null && s.CRNo.ToLower().Contains(searchValue)) ||
                            (s.JVReason != null && s.JVReason.ToLower().Contains(searchValue)) ||
                            (s.CreatedBy != null && s.CreatedBy.ToLower().Contains(searchValue)) ||
                            s.Status.ToLower().Contains(searchValue)
                        )
                        .ToList();
                }

                // Apply sorting if provided
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    // Map frontend column names to actual entity property names
                    var columnMapping = new Dictionary<string, string>
            {
                { "journalVoucherHeaderNo", "JournalVoucherHeaderNo" },
                { "date", "Date" },
                { "checkVoucherHeaderNo", "CheckVoucherHeader.CheckVoucherHeaderNo" },
                { "crNo", "CRNo" },
                { "jvReason", "JVReason" },
                { "createdBy", "CreatedBy" },
                { "status", "Status" }
            };

                    // Get the actual property name
                    var actualColumnName = columnMapping.ContainsKey(columnName)
                        ? columnMapping[columnName]
                        : columnName;

                    journalVoucherHeaders = journalVoucherHeaders
                        .AsQueryable()
                        .OrderBy($"{actualColumnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = journalVoucherHeaders.Count();

                // Apply pagination - HANDLE -1 FOR "ALL"
                IEnumerable<FilprideJournalVoucherHeader> pagedJournalVoucherHeaders;

                if (parameters.Length == -1)
                {
                    // "All" selected - return all records
                    pagedJournalVoucherHeaders = journalVoucherHeaders;
                }
                else
                {
                    // Normal pagination
                    pagedJournalVoucherHeaders = journalVoucherHeaders
                        .Skip(parameters.Start)
                        .Take(parameters.Length);
                }

                var pagedData = pagedJournalVoucherHeaders
                    .Select(x => new
                    {
                        x.JournalVoucherHeaderId,
                        x.JournalVoucherHeaderNo,
                        x.Date,
                        checkVoucherHeaderNo = x.CheckVoucherHeader?.CheckVoucherHeaderNo,
                        x.CRNo,
                        x.JVReason,
                        x.CreatedBy,
                        x.Status,
                        // Include status flags for badge rendering
                        isPosted = x.PostedBy != null,
                        isVoided = x.VoidedBy != null,
                        isCanceled = x.CanceledBy != null
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
                _logger.LogError(ex, "Failed to get journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.FilprideJournalVoucherHeaders
                .Where(jv => recordIds.Contains(jv.JournalVoucherHeaderId))
                .Include(jv => jv.CheckVoucherHeader)
                .OrderBy(jv => jv.JournalVoucherHeaderNo)
                .ToListAsync();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package

                #region -- Purchase Order Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("PurchaseOrder");

                worksheet3.Cells["A1"].Value = "Date";
                worksheet3.Cells["B1"].Value = "Terms";
                worksheet3.Cells["C1"].Value = "Quantity";
                worksheet3.Cells["D1"].Value = "Price";
                worksheet3.Cells["E1"].Value = "Amount";
                worksheet3.Cells["F1"].Value = "FinalPrice";
                worksheet3.Cells["G1"].Value = "QuantityReceived";
                worksheet3.Cells["H1"].Value = "IsReceived";
                worksheet3.Cells["I1"].Value = "ReceivedDate";
                worksheet3.Cells["J1"].Value = "Remarks";
                worksheet3.Cells["K1"].Value = "CreatedBy";
                worksheet3.Cells["L1"].Value = "CreatedDate";
                worksheet3.Cells["M1"].Value = "IsClosed";
                worksheet3.Cells["N1"].Value = "CancellationRemarks";
                worksheet3.Cells["O1"].Value = "OriginalProductId";
                worksheet3.Cells["P1"].Value = "OriginalSeriesNumber";
                worksheet3.Cells["Q1"].Value = "OriginalSupplierId";
                worksheet3.Cells["R1"].Value = "OriginalDocumentId";
                worksheet3.Cells["S1"].Value = "PostedBy";
                worksheet3.Cells["T1"].Value = "PostedDate";

                #endregion -- Purchase Order Table Header --

                #region -- Receiving Report Table Header --

                var worksheet4 = package.Workbook.Worksheets.Add("ReceivingReport");

                worksheet4.Cells["A1"].Value = "Date";
                worksheet4.Cells["B1"].Value = "DueDate";
                worksheet4.Cells["C1"].Value = "SupplierInvoiceNumber";
                worksheet4.Cells["D1"].Value = "SupplierInvoiceDate";
                worksheet4.Cells["E1"].Value = "TruckOrVessels";
                worksheet4.Cells["F1"].Value = "QuantityDelivered";
                worksheet4.Cells["G1"].Value = "QuantityReceived";
                worksheet4.Cells["H1"].Value = "GainOrLoss";
                worksheet4.Cells["I1"].Value = "Amount";
                worksheet4.Cells["J1"].Value = "OtherRef";
                worksheet4.Cells["K1"].Value = "Remarks";
                worksheet4.Cells["L1"].Value = "AmountPaid";
                worksheet4.Cells["M1"].Value = "IsPaid";
                worksheet4.Cells["N1"].Value = "PaidDate";
                worksheet4.Cells["O1"].Value = "CanceledQuantity";
                worksheet4.Cells["P1"].Value = "CreatedBy";
                worksheet4.Cells["Q1"].Value = "CreatedDate";
                worksheet4.Cells["R1"].Value = "CancellationRemarks";
                worksheet4.Cells["S1"].Value = "ReceivedDate";
                worksheet4.Cells["T1"].Value = "OriginalPOId";
                worksheet4.Cells["U1"].Value = "OriginalSeriesNumber";
                worksheet4.Cells["V1"].Value = "OriginalDocumentId";
                worksheet4.Cells["W1"].Value = "PostedBy";
                worksheet4.Cells["X1"].Value = "PostedDate";

                #endregion -- Receiving Report Table Header --

                #region -- Check Voucher Header Table Header --

                var worksheet5 = package.Workbook.Worksheets.Add("CheckVoucherHeader");

                worksheet5.Cells["A1"].Value = "TransactionDate";
                worksheet5.Cells["B1"].Value = "ReceivingReportNo";
                worksheet5.Cells["C1"].Value = "SalesInvoiceNo";
                worksheet5.Cells["D1"].Value = "PurchaseOrderNo";
                worksheet5.Cells["E1"].Value = "Particulars";
                worksheet5.Cells["F1"].Value = "CheckNo";
                worksheet5.Cells["G1"].Value = "Category";
                worksheet5.Cells["H1"].Value = "Payee";
                worksheet5.Cells["I1"].Value = "CheckDate";
                worksheet5.Cells["J1"].Value = "StartDate";
                worksheet5.Cells["K1"].Value = "EndDate";
                worksheet5.Cells["L1"].Value = "NumberOfMonths";
                worksheet5.Cells["M1"].Value = "NumberOfMonthsCreated";
                worksheet5.Cells["N1"].Value = "LastCreatedDate";
                worksheet5.Cells["O1"].Value = "AmountPerMonth";
                worksheet5.Cells["P1"].Value = "IsComplete";
                worksheet5.Cells["Q1"].Value = "AccruedType";
                worksheet5.Cells["R1"].Value = "Reference";
                worksheet5.Cells["S1"].Value = "CreatedBy";
                worksheet5.Cells["T1"].Value = "CreatedDate";
                worksheet5.Cells["U1"].Value = "Total";
                worksheet5.Cells["V1"].Value = "Amount";
                worksheet5.Cells["W1"].Value = "CheckAmount";
                worksheet5.Cells["X1"].Value = "CVType";
                worksheet5.Cells["Y1"].Value = "AmountPaid";
                worksheet5.Cells["Z1"].Value = "IsPaid";
                worksheet5.Cells["AA1"].Value = "CancellationRemarks";
                worksheet5.Cells["AB1"].Value = "OriginalBankId";
                worksheet5.Cells["AC1"].Value = "OriginalSeriesNumber";
                worksheet5.Cells["AD1"].Value = "OriginalSupplierId";
                worksheet5.Cells["AE1"].Value = "OriginalDocumentId";
                worksheet5.Cells["AF1"].Value = "PostedBy";
                worksheet5.Cells["AG1"].Value = "PostedDate";

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Details Table Header --

                var worksheet6 = package.Workbook.Worksheets.Add("CheckVoucherDetails");

                worksheet6.Cells["A1"].Value = "AccountNo";
                worksheet6.Cells["B1"].Value = "AccountName";
                worksheet6.Cells["C1"].Value = "TransactionNo";
                worksheet6.Cells["D1"].Value = "Debit";
                worksheet6.Cells["E1"].Value = "Credit";
                worksheet6.Cells["F1"].Value = "CVHeaderId";
                worksheet6.Cells["G1"].Value = "OriginalDocumentId";

                #endregion -- Check Voucher Details Table Header --

                #region -- Check Voucher Trade Payments Table Header --

                var worksheet7 = package.Workbook.Worksheets.Add("CheckVoucherTradePayments");

                worksheet7.Cells["A1"].Value = "Id";
                worksheet7.Cells["B1"].Value = "DocumentId";
                worksheet7.Cells["C1"].Value = "DocumentType";
                worksheet7.Cells["D1"].Value = "CheckVoucherId";
                worksheet7.Cells["E1"].Value = "AmountPaid";

                #endregion -- Check Voucher Trade Payments Table Header --

                #region -- Check Voucher Multiple Payment Table Header --

                var worksheet8 = package.Workbook.Worksheets.Add("MultipleCheckVoucherPayments");

                worksheet8.Cells["A1"].Value = "Id";
                worksheet8.Cells["B1"].Value = "CheckVoucherHeaderPaymentId";
                worksheet8.Cells["C1"].Value = "CheckVoucherHeaderInvoiceId";
                worksheet8.Cells["D1"].Value = "AmountPaid";

                #endregion -- Check Voucher Multiple Payment Table Header --

                #region -- Journal Voucher Header Table Header --

                var worksheet = package.Workbook.Worksheets.Add("JournalVoucherHeader");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "Reference";
                worksheet.Cells["C1"].Value = "Particulars";
                worksheet.Cells["D1"].Value = "CRNo";
                worksheet.Cells["E1"].Value = "JVReason";
                worksheet.Cells["F1"].Value = "CreatedBy";
                worksheet.Cells["G1"].Value = "CreatedDate";
                worksheet.Cells["H1"].Value = "CancellationRemarks";
                worksheet.Cells["I1"].Value = "OriginalCVId";
                worksheet.Cells["J1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["K1"].Value = "OriginalDocumentId";
                worksheet.Cells["L1"].Value = "PostedBy";
                worksheet.Cells["M1"].Value = "PostedDate";

                #endregion -- Journal Voucher Header Table Header --

                #region -- Journal Voucher Details Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("JournalVoucherDetails");

                worksheet2.Cells["A1"].Value = "AccountNo";
                worksheet2.Cells["B1"].Value = "AccountName";
                worksheet2.Cells["C1"].Value = "TransactionNo";
                worksheet2.Cells["D1"].Value = "Debit";
                worksheet2.Cells["E1"].Value = "Credit";
                worksheet2.Cells["F1"].Value = "JVHeaderId";
                worksheet2.Cells["G1"].Value = "OriginalDocumentId";

                #endregion -- Journal Voucher Details Table Header --

                #region -- Journal Voucher Header Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.References;
                    worksheet.Cells[row, 3].Value = item.Particulars;
                    worksheet.Cells[row, 4].Value = item.CRNo;
                    worksheet.Cells[row, 5].Value = item.JVReason;
                    worksheet.Cells[row, 6].Value = item.CreatedBy;
                    worksheet.Cells[row, 7].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet.Cells[row, 8].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 9].Value = item.CVId;
                    worksheet.Cells[row, 10].Value = item.JournalVoucherHeaderNo;
                    worksheet.Cells[row, 11].Value = item.JournalVoucherHeaderId;
                    worksheet.Cells[row, 12].Value = item.PostedBy;
                    worksheet.Cells[row, 13].Value = item.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Journal Voucher Header Export --

                #region -- Check Voucher Header Export (Non-Trade Payment or Trade Payment)--

                int cvhRow = 2;
                var currentCvTradeAndInvoicing = "";

                foreach (var item in selectedList)
                {
                    if (item.CheckVoucherHeader == null)
                    {
                        continue;
                    }
                    if (item.CheckVoucherHeader.CheckVoucherHeaderNo == currentCvTradeAndInvoicing)
                    {
                        continue;
                    }

                    currentCvTradeAndInvoicing = item.CheckVoucherHeader.CheckVoucherHeaderNo;
                    worksheet5.Cells[cvhRow, 1].Value = item.CheckVoucherHeader.Date.ToString("yyyy-MM-dd");
                    if (item.CheckVoucherHeader.RRNo != null && !item.CheckVoucherHeader.RRNo.Contains(null))
                    {
                        worksheet5.Cells[cvhRow, 2].Value = string.Join(", ", item.CheckVoucherHeader.RRNo.Select(rrNo => rrNo.ToString()));
                    }
                    if (item.CheckVoucherHeader.SINo != null && !item.CheckVoucherHeader.SINo.Contains(null))
                    {
                        worksheet5.Cells[cvhRow, 3].Value = string.Join(", ", item.CheckVoucherHeader.SINo.Select(siNo => siNo.ToString()));
                    }
                    if (item.CheckVoucherHeader.PONo != null && !item.CheckVoucherHeader.PONo.Contains(null))
                    {
                        worksheet5.Cells[cvhRow, 4].Value = string.Join(", ", item.CheckVoucherHeader.PONo.Select(poNo => poNo.ToString()));
                    }

                    worksheet5.Cells[cvhRow, 5].Value = item.CheckVoucherHeader.Particulars;
                    worksheet5.Cells[cvhRow, 6].Value = item.CheckVoucherHeader.CheckNo;
                    worksheet5.Cells[cvhRow, 7].Value = item.CheckVoucherHeader.Category;
                    worksheet5.Cells[cvhRow, 8].Value = item.CheckVoucherHeader.Payee;
                    worksheet5.Cells[cvhRow, 9].Value = item.CheckVoucherHeader.CheckDate?.ToString("yyyy-MM-dd");
                    worksheet5.Cells[cvhRow, 18].Value = item.CheckVoucherHeader.Reference;
                    worksheet5.Cells[cvhRow, 19].Value = item.CheckVoucherHeader.CreatedBy;
                    worksheet5.Cells[cvhRow, 20].Value = item.CheckVoucherHeader.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet5.Cells[cvhRow, 21].Value = item.CheckVoucherHeader.Total;
                    if (item.CheckVoucherHeader.Amount != null)
                    {
                        worksheet5.Cells[cvhRow, 22].Value = string.Join(" ", item.CheckVoucherHeader.Amount.Select(amount => amount.ToString("N4")));
                    }
                    worksheet5.Cells[cvhRow, 23].Value = item.CheckVoucherHeader.CheckAmount;
                    worksheet5.Cells[cvhRow, 24].Value = item.CheckVoucherHeader.CvType;
                    worksheet5.Cells[cvhRow, 25].Value = item.CheckVoucherHeader.AmountPaid;
                    worksheet5.Cells[cvhRow, 26].Value = item.CheckVoucherHeader.IsPaid;
                    worksheet5.Cells[cvhRow, 27].Value = item.CheckVoucherHeader.CancellationRemarks;
                    worksheet5.Cells[cvhRow, 28].Value = item.CheckVoucherHeader.BankId;
                    worksheet5.Cells[cvhRow, 29].Value = item.CheckVoucherHeader.CheckVoucherHeaderNo;
                    worksheet5.Cells[cvhRow, 30].Value = item.CheckVoucherHeader.SupplierId;
                    worksheet5.Cells[cvhRow, 31].Value = item.CheckVoucherHeader.CheckVoucherHeaderId;
                    worksheet5.Cells[cvhRow, 32].Value = item.CheckVoucherHeader.PostedBy;
                    worksheet5.Cells[cvhRow, 33].Value = item.CheckVoucherHeader.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    cvhRow++;
                }

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => recordIds.Contains(cv.CheckVoucherId) && cv.DocumentType == "RR")
                    .ToListAsync();

                int cvRow = 2;
                foreach (var payment in getCheckVoucherTradePayment)
                {
                    worksheet7.Cells[cvRow, 1].Value = payment.Id;
                    worksheet7.Cells[cvRow, 2].Value = payment.DocumentId;
                    worksheet7.Cells[cvRow, 3].Value = payment.DocumentType;
                    worksheet7.Cells[cvRow, 4].Value = payment.CheckVoucherId;
                    worksheet7.Cells[cvRow, 5].Value = payment.AmountPaid;

                    cvRow++;
                }

                #endregion -- Check Voucher Header Export (Non-Trade Payment or Trade Payment)--

                #region -- Get Check Voucher Multiple Payment --

                var cvNos = selectedList.Select(item => item.CheckVoucherHeader!.CheckVoucherHeaderNo).ToList();
                var checkVoucherPayment = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cvh => cvh.Reference != null && cvNos.Contains(cvh.CheckVoucherHeaderNo));
                var cvPaymentId = checkVoucherPayment.Select(cvn => cvn.CheckVoucherHeaderId).ToList();
                var getCheckVoucherMultiplePayment = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cv => cvPaymentId.Contains(cv.CheckVoucherHeaderPaymentId))
                    .ToListAsync();

                int cvn = 2;
                foreach (var payment in getCheckVoucherMultiplePayment)
                {
                    worksheet8.Cells[cvn, 1].Value = payment.Id;
                    worksheet8.Cells[cvn, 2].Value = payment.CheckVoucherHeaderPaymentId;
                    worksheet8.Cells[cvn, 3].Value = payment.CheckVoucherHeaderInvoiceId;
                    worksheet8.Cells[cvn, 4].Value = payment.AmountPaid;

                    cvn++;
                }

                #endregion -- Get Check Voucher Multiple Payment --

                #region -- Journal Voucher Details Export --

                var jvNos = selectedList.Select(item => item.JournalVoucherHeaderNo).ToList();

                var getJvDetails = await _dbContext.FilprideJournalVoucherDetails
                    .Where(jvd => jvNos.Contains(jvd.TransactionNo))
                    .OrderBy(jvd => jvd.JournalVoucherDetailId)
                    .ToListAsync();

                int jvdRow = 2;

                foreach (var item in getJvDetails)
                {
                    worksheet2.Cells[jvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[jvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[jvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[jvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[jvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[jvdRow, 6].Value = item.JournalVoucherHeaderId;
                    worksheet2.Cells[jvdRow, 7].Value = item.JournalVoucherDetailId;

                    jvdRow++;
                }

                #endregion -- Journal Voucher Details Export --

                #region -- Check Voucher Details Export (Non-Trade or Trade Payment) --

                var getCvDetails = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                var cvdRow = 2;

                foreach (var item in getCvDetails)
                {
                    worksheet6.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet6.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet6.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet6.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet6.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet6.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet6.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet6.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet6.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet6.Cells[cvdRow, 10].Value = item.SubAccountId;
                    worksheet6.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet6.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet6.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Non-Trade or Trade Payment) --

                #region -- Receving Report Export --

                var getReceivingReport = _dbContext.FilprideReceivingReports
                    .AsEnumerable()
                    .Where(rr => selectedList
                        .Select(item => item.CheckVoucherHeader?.RRNo)
                        .Any(rrs => rrs?.Contains(rr.ReceivingReportNo) == true))
                    .OrderBy(rr => rr.ReceivingReportNo)
                    .ToList();

                int rrRow = 2;
                var currentRr = "";

                foreach (var item in getReceivingReport)
                {
                    if (item.ReceivingReportNo == currentRr)
                    {
                        continue;
                    }

                    currentRr = item.ReceivingReportNo;
                    worksheet4.Cells[rrRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet4.Cells[rrRow, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                    worksheet4.Cells[rrRow, 3].Value = item.SupplierInvoiceNumber;
                    worksheet4.Cells[rrRow, 4].Value = item.SupplierInvoiceDate;
                    worksheet4.Cells[rrRow, 5].Value = item.TruckOrVessels;
                    worksheet4.Cells[rrRow, 6].Value = item.QuantityDelivered;
                    worksheet4.Cells[rrRow, 7].Value = item.QuantityReceived;
                    worksheet4.Cells[rrRow, 8].Value = item.GainOrLoss;
                    worksheet4.Cells[rrRow, 9].Value = item.Amount;
                    worksheet4.Cells[rrRow, 10].Value = item.AuthorityToLoadNo;
                    worksheet4.Cells[rrRow, 11].Value = item.Remarks;
                    worksheet4.Cells[rrRow, 12].Value = item.AmountPaid;
                    worksheet4.Cells[rrRow, 13].Value = item.IsPaid;
                    worksheet4.Cells[rrRow, 14].Value = item.PaidDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet4.Cells[rrRow, 15].Value = item.CanceledQuantity;
                    worksheet4.Cells[rrRow, 16].Value = item.CreatedBy;
                    worksheet4.Cells[rrRow, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet4.Cells[rrRow, 18].Value = item.CancellationRemarks;
                    worksheet4.Cells[rrRow, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                    worksheet4.Cells[rrRow, 20].Value = item.POId;
                    worksheet4.Cells[rrRow, 21].Value = item.ReceivingReportNo;
                    worksheet4.Cells[rrRow, 22].Value = item.ReceivingReportId;
                    worksheet4.Cells[rrRow, 23].Value = item.PostedBy;
                    worksheet4.Cells[rrRow, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    rrRow++;
                }

                #endregion -- Receving Report Export --

                #region -- Purchase Order Export --

                var getPurchaseOrder = await _dbContext.FilpridePurchaseOrders
                    .Where(po => getReceivingReport.Select(item => item.POId).Contains(po.PurchaseOrderId))
                    .OrderBy(po => po.PurchaseOrderNo)
                    .ToListAsync();

                int poRow = 2;
                var currentPo = "";

                foreach (var item in getPurchaseOrder)
                {
                    if (item.PurchaseOrderNo == currentPo)
                    {
                        continue;
                    }

                    currentPo = item.PurchaseOrderNo;
                    worksheet3.Cells[poRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet3.Cells[poRow, 2].Value = item.Terms;
                    worksheet3.Cells[poRow, 3].Value = item.Quantity;
                    worksheet3.Cells[poRow, 4].Value = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderCost(item.PurchaseOrderId);
                    worksheet3.Cells[poRow, 5].Value = item.Amount;
                    worksheet3.Cells[poRow, 6].Value = item.FinalPrice;
                    worksheet3.Cells[poRow, 7].Value = item.QuantityReceived;
                    worksheet3.Cells[poRow, 8].Value = item.IsReceived;
                    worksheet3.Cells[poRow, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                    worksheet3.Cells[poRow, 10].Value = item.Remarks;
                    worksheet3.Cells[poRow, 11].Value = item.CreatedBy;
                    worksheet3.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                    worksheet3.Cells[poRow, 13].Value = item.IsClosed;
                    worksheet3.Cells[poRow, 14].Value = item.CancellationRemarks;
                    worksheet3.Cells[poRow, 15].Value = item.ProductId;
                    worksheet3.Cells[poRow, 16].Value = item.PurchaseOrderNo;
                    worksheet3.Cells[poRow, 17].Value = item.SupplierId;
                    worksheet3.Cells[poRow, 18].Value = item.PurchaseOrderId;
                    worksheet3.Cells[poRow, 19].Value = item.PostedBy;
                    worksheet3.Cells[poRow, 20].Value = item.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss.ffffff") ?? null;

                    poRow++;
                }

                #endregion -- Purchase Order Export --

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JournalVoucherList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllJournalVoucherIds()
        {
            var jvIds = _dbContext.FilprideJournalVoucherHeaders
                .Where(jv => jv.Type == nameof(DocumentType.Documented))
                .Select(jv => jv.JournalVoucherHeaderId)
                .ToList();

            return Json(jvIds);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReJournalJv(int? month, int? year, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var jvs = await _dbContext.FilprideJournalVoucherHeaders
                    .Include(x => x.Details)
                    .Where(x =>
                        x.PostedBy != null &&
                        x.Date.Month == month &&
                        x.Date.Year == year)
                    .ToListAsync(cancellationToken);

                if (!jvs.Any())
                {
                    return Json(new { sucess = true, message = "No records were returned." });
                }

                foreach (var jv in jvs
                             .OrderBy(x => x.Date))
                {
                    await _unitOfWork.FilprideJournalVoucher.PostAsync(jv,
                        jv.Details!,
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return Json(new { month, year, count = jvs.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccrual(CancellationToken cancellationToken)
        {
            var viewModel = new JvCreateAccrualViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.CvType == nameof(CVType.Invoicing) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccrual(JvCreateAccrualViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.CvType == nameof(CVType.Invoicing) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var cv = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(x => x.CheckVoucherHeaderId == viewModel.CvId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {viewModel.CvId} not found");

                #region --Saving the default entries

                var generateJvNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken: cancellationToken);
                var particulars = $"Accrual of '{viewModel.Details.First(d => d.Debit > 0).AccountTitle}' for the month of {viewModel.TransactionDate:MMM yyyy}.";
                var model = new FilprideJournalVoucherHeader
                {
                    Type = viewModel.Type,
                    JournalVoucherHeaderNo = generateJvNo,
                    Date = viewModel.TransactionDate,
                    References = viewModel.References,
                    CVId = viewModel.CvId,
                    Particulars = particulars,
                    CRNo = viewModel.CrNo,
                    JVReason = viewModel.Reason,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    JvType = nameof(JvType.Accrual)
                };

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    var isAccrualAccount = accountTitle.AccountName.Contains("AP - Accrued Expenses");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = generateJvNo,
                            JournalVoucherHeaderId = model.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                            SubAccountType = isAccrualAccount ? SubAccountType.Supplier : null,
                            SubAccountId = isAccrualAccount ? cv.SupplierId : null,
                            SubAccountName = isAccrualAccount ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Created new journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Journal voucher # {model.JournalVoucherHeaderNo} created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditAccrual(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .Include(jv => jv.CheckVoucherHeader)
                    .FirstOrDefaultAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, existingHeaderModel.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails
                    .Where(cvd => cvd.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                JvEditAccrualViewModel model = new()
                {
                    JvId = existingHeaderModel.JournalVoucherHeaderId,
                    TransactionDate = existingHeaderModel.Date,
                    References = existingHeaderModel.References,
                    CvId = (int)existingHeaderModel.CVId!,
                    CrNo = existingHeaderModel.CRNo,
                    Reason = existingHeaderModel.JVReason,
                    CvList = await _dbContext.FilprideCheckVoucherHeaders
                        .OrderBy(c => c.CheckVoucherHeaderNo)
                        .Where(c =>
                            c.Company == companyClaims &&
                            c.CvType == nameof(CVType.Invoicing) &&
                            c.PostedBy != null)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync(cancellationToken),
                    MinDate = minDate
                };

                foreach (var detail in existingDetailsModel)
                {
                    model.Details.Add(new JvEditAccrualDetailViewModel
                    {
                        AccountNo = detail.AccountNo,
                        AccountTitle = detail.AccountName,
                        Debit = detail.Debit,
                        Credit = detail.Credit,
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch JV. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccrual(JvEditAccrualViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                        .OrderBy(c => c.CheckVoucherHeaderNo)
                        .Where(c =>
                            c.Company == companyClaims &&
                            c.CvType == nameof(CVType.Invoicing) &&
                            c.PostedBy != null)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == viewModel.JvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var cv = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(x => x.CheckVoucherHeaderId == viewModel.CvId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {viewModel.CvId} not found");

                await _dbContext.FilprideJournalVoucherDetails
                    .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Saving the default entries

                var particulars = $"Accrual of '{viewModel.Details.First(d => d.Debit > 0).AccountTitle}' for the month of {viewModel.TransactionDate:MMM yyyy}.";

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.References = viewModel.References;
                existingHeaderModel.CVId = viewModel.CvId;
                existingHeaderModel.Particulars = particulars;
                existingHeaderModel.CRNo = viewModel.CrNo;
                existingHeaderModel.JVReason = viewModel.Reason;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    var isAccrualAccount = accountTitle.AccountName.Contains("AP - Accrued Expenses");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = existingHeaderModel.JournalVoucherHeaderNo!,
                            JournalVoucherHeaderId = existingHeaderModel.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                            SubAccountType = isAccrualAccount ? SubAccountType.Supplier : null,
                            SubAccountId = isAccrualAccount ? cv.SupplierId : null,
                            SubAccountName = isAccrualAccount ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", existingHeaderModel.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        private async Task ReverseAccrual(int id, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken)
                ?? throw new InvalidOperationException($"Journal voucher header {id} not found.");

            var accountTitlesDto = await _unitOfWork.FilprideJournalVoucher.GetListOfAccountTitleDto(cancellationToken);
            var ledgers = new List<FilprideGeneralLedgerBook>();
            var nextMonth = existingHeaderModel.Date.AddMonths(1);
            var firstDayOfNextMonth = new DateOnly(nextMonth.Year, nextMonth.Month, 1);

            foreach (var detail in existingHeaderModel.Details!)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == detail.AccountNo)
                    ?? throw new ArgumentException($"Account title '{detail.AccountNo}' not found.");

                var particulars = $"Reversal of accrued '{existingHeaderModel.Details.First(d => d.Debit > 0).AccountName}' for the month of {firstDayOfNextMonth:MMM yyyy}.";

                ledgers.Add(
                    new FilprideGeneralLedgerBook
                    {
                        Date = firstDayOfNextMonth,
                        Reference = existingHeaderModel.JournalVoucherHeaderNo!,
                        Description = $"Reversal of {existingHeaderModel.Particulars}",
                        AccountId = account.AccountId,
                        AccountNo = account.AccountNumber,
                        AccountTitle = account.AccountName,
                        Debit = detail.Credit,
                        Credit = detail.Debit,
                        Company = existingHeaderModel.Company,
                        CreatedBy = existingHeaderModel.CreatedBy!,
                        CreatedDate = existingHeaderModel.CreatedDate,
                        SubAccountType = detail.SubAccountType,
                        SubAccountId = detail.SubAccountId,
                        SubAccountName = detail.SubAccountName,
                        ModuleType = nameof(ModuleType.Journal)
                    }
                );
            }

            if (!_unitOfWork.FilprideJournalVoucher.IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAmortization(CancellationToken cancellationToken)
        {
            var viewModel = new JvCreateAmortizationViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.CvType == nameof(CVType.Invoicing) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.PrepaidExpenseAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.AccountName.Contains("Prepaid Expenses") && !coa.HasChildren)
                .Select(coa => new SelectListItem
                {
                    Value = coa.AccountNumber,
                    Text = $"{coa.AccountNumber} - {coa.AccountName}"
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAmortization(JvCreateAmortizationViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.CvType == nameof(CVType.Invoicing) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.PrepaidExpenseAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.AccountName.Contains("Prepaid Expenses") && !coa.HasChildren)
                .Select(coa => new SelectListItem
                {
                    Value = coa.AccountNumber,
                    Text = $"{coa.AccountNumber} - {coa.AccountName}"
                })
                .ToListAsync(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var cv = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(x => x.CheckVoucherHeaderId == viewModel.CvId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {viewModel.CvId} not found");

                #region --Saving the default entries

                var generateJvNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken: cancellationToken);
                var startingMonth = new DateOnly(viewModel.TransactionDate.Year, viewModel.TransactionDate.Month, 1);
                var endingMonth = startingMonth.AddMonths(viewModel.NumberOfMonths - 1);
                var expenseAccount = viewModel.Details.First(d => d.Debit > 0).AccountTitle;
                var prepaidAccount = viewModel.Details.First(d => d.Credit > 0).AccountTitle;

                var particulars = $"Amortization of '{expenseAccount.Split(" ")[1]}' from {startingMonth:MMM yyyy} to {endingMonth:MMM yyyy}.";
                var model = new FilprideJournalVoucherHeader
                {
                    Type = viewModel.Type,
                    JournalVoucherHeaderNo = generateJvNo,
                    Date = viewModel.TransactionDate,
                    References = viewModel.References,
                    CVId = viewModel.CvId,
                    Particulars = particulars,
                    CRNo = viewModel.CrNo,
                    JVReason = viewModel.Reason,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    JvType = nameof(JvType.Amortization)
                };

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var amortizationSetting = new JvAmortizationSetting
                {
                    JvId = model.JournalVoucherHeaderId,
                    StartDate = startingMonth,
                    EndDate = endingMonth,
                    OccurrenceTotal = viewModel.NumberOfMonths,
                    OccurrenceRemaining = viewModel.NumberOfMonths - 1,
                    IsActive = true,
                    ExpenseAccount = expenseAccount,
                    PrepaidAccount = prepaidAccount,
                };

                await _dbContext.AddAsync(amortizationSetting, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    var isPrepaidAccount = accountTitle.AccountName.Contains("Prepaid Expenses");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = generateJvNo,
                            JournalVoucherHeaderId = model.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                            SubAccountType = isPrepaidAccount ? SubAccountType.Supplier : null,
                            SubAccountId = isPrepaidAccount ? cv.SupplierId : null,
                            SubAccountName = isPrepaidAccount ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Created new journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Journal voucher # {model.JournalVoucherHeaderNo} created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditAmortization(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingAmortizationSetting = await _dbContext.JvAmortizationSettings
                    .Include(jv => jv.JvHeader)
                    .ThenInclude(jv => jv.CheckVoucherHeader)
                    .FirstOrDefaultAsync(jv => jv.JvId == id, cancellationToken);

                if (existingAmortizationSetting == null)
                {
                    TempData["info"] = "This record cannot be edited because it is automatically generated based on the amortization settings.";
                    return RedirectToAction(nameof(Index));
                }

                var header = existingAmortizationSetting.JvHeader;

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, header.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {header.Date:MMM yyyy} is already closed.");
                }

                var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails
                    .Where(cvd => cvd.JournalVoucherHeaderId == header.JournalVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                JvEditAmortizationViewModel model = new()
                {
                    JvId = header.JournalVoucherHeaderId,
                    TransactionDate = header.Date,
                    References = header.References,
                    CvId = (int)header.CVId!,
                    CrNo = header.CRNo,
                    Reason = header.JVReason,
                    MinDate = minDate,
                    SelectedExpenseAccount = existingAmortizationSetting.ExpenseAccount.Split(" ")[0],
                    SelectedPrepaidAccount = existingAmortizationSetting.PrepaidAccount.Split(" ")[0],
                    NumberOfMonths = existingAmortizationSetting.OccurrenceTotal
                };

                model.CvList = await _dbContext.FilprideCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderNo)
                    .Where(c =>
                        c.Company == companyClaims &&
                        c.CvType == nameof(CVType.Invoicing) &&
                        c.PostedBy != null)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken);

                model.MinDate = await _unitOfWork
                    .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

                model.PrepaidExpenseAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => coa.AccountName.Contains("Prepaid Expenses") && !coa.HasChildren)
                    .Select(coa => new SelectListItem
                    {
                        Value = coa.AccountNumber,
                        Text = $"{coa.AccountNumber} - {coa.AccountName}"
                    })
                    .ToListAsync(cancellationToken);

                foreach (var detail in existingDetailsModel)
                {
                    model.Details.Add(new JvEditAmortizationDetailViewModel
                    {
                        AccountNo = detail.AccountNo,
                        AccountTitle = detail.AccountName,
                        Debit = detail.Debit,
                        Credit = detail.Credit
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch JV. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAmortization(JvEditAmortizationViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    c.CvType == nameof(CVType.Invoicing) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.PrepaidExpenseAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.AccountName.Contains("Prepaid Expenses") && !coa.HasChildren)
                .Select(coa => new SelectListItem
                {
                    Value = coa.AccountNumber,
                    Text = $"{coa.AccountNumber} - {coa.AccountName}"
                })
                .ToListAsync(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == viewModel.JvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var cv = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(x => x.CheckVoucherHeaderId == viewModel.CvId, cancellationToken)
                         ?? throw new NullReferenceException($"CV id {viewModel.CvId} not found");

                var amortizationSetting = await _dbContext.JvAmortizationSettings
                   .FirstOrDefaultAsync(jv => jv.JvId == existingHeaderModel.JournalVoucherHeaderId && jv.IsActive, cancellationToken)
                        ?? throw new NullReferenceException($"JV#{existingHeaderModel.JournalVoucherHeaderId} amortization settings not found.");

                await _dbContext.FilprideJournalVoucherDetails
                    .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Saving the default entries

                var startingMonth = new DateOnly(viewModel.TransactionDate.Year, viewModel.TransactionDate.Month, 1);
                var endingMonth = startingMonth.AddMonths(viewModel.NumberOfMonths - 1);
                var expenseAccount = viewModel.Details.First(d => d.Debit > 0).AccountTitle;
                var prepaidAccount = viewModel.Details.First(d => d.Credit > 0).AccountTitle;

                var particulars = $"Amortization of '{expenseAccount.Split(" ")[1]}' from {startingMonth:MMM yyyy} to {endingMonth:MMM yyyy}.";

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.References = viewModel.References;
                existingHeaderModel.CVId = viewModel.CvId;
                existingHeaderModel.Particulars = particulars;
                existingHeaderModel.CRNo = viewModel.CrNo;
                existingHeaderModel.JVReason = viewModel.Reason;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                amortizationSetting.StartDate = startingMonth;
                amortizationSetting.EndDate = endingMonth;
                amortizationSetting.OccurrenceTotal = viewModel.NumberOfMonths;
                amortizationSetting.OccurrenceRemaining = viewModel.NumberOfMonths - 1;
                amortizationSetting.IsActive = true;
                amortizationSetting.ExpenseAccount = expenseAccount;
                amortizationSetting.PrepaidAccount = prepaidAccount;

                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    var isPrepaidAccount = accountTitle.AccountName.Contains("Prepaid Expenses");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = existingHeaderModel.JournalVoucherHeaderNo!,
                            JournalVoucherHeaderId = existingHeaderModel.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                            SubAccountType = isPrepaidAccount ? SubAccountType.Supplier : null,
                            SubAccountId = isPrepaidAccount ? cv.SupplierId : null,
                            SubAccountName = isPrepaidAccount ? cv.Payee : null,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", existingHeaderModel.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        private async Task<bool> ProcessAmortizationAsync(int id, CancellationToken cancellationToken)
        {
            var existingAmortizationSetting = await _dbContext.JvAmortizationSettings
                .Include(x => x.JvHeader)
                    .ThenInclude(x => x.Details)
                .FirstOrDefaultAsync(x => x.JvId == id && x.IsActive, cancellationToken);

            if (existingAmortizationSetting == null)
                return true;

            var sourceJv = existingAmortizationSetting.JvHeader;

            if (sourceJv == null || sourceJv.Details == null || !sourceJv.Details.Any())
            {
                throw new InvalidOperationException($"The source journal voucher for amortization with ID {id} is missing or has no details.");
            }

            var newJournalVouchers = new List<FilprideJournalVoucherHeader>();

            for (var i = 1; i <= existingAmortizationSetting.OccurrenceRemaining; i++)
            {
                var generatedCode = await _unitOfWork
                    .FilprideJournalVoucher
                    .GenerateCodeAsync(sourceJv.Company, sourceJv.Type, i, cancellationToken);

                var newHeader = new FilprideJournalVoucherHeader
                {
                    Type = sourceJv.Type,
                    JournalVoucherHeaderNo = generatedCode,
                    Date = existingAmortizationSetting.StartDate.AddMonths(i),
                    References = sourceJv.References,
                    CVId = sourceJv.CVId,
                    Particulars = sourceJv.Particulars,
                    CRNo = sourceJv.CRNo,
                    JVReason = sourceJv.JVReason,
                    CreatedBy = GetUserFullName(),
                    Company = sourceJv.Company,
                    JvType = nameof(JvType.Amortization),
                    Details = new List<FilprideJournalVoucherDetail>()
                };

                foreach (var detail in sourceJv.Details)
                {
                    newHeader.Details.Add(new FilprideJournalVoucherDetail
                    {
                        AccountNo = detail.AccountNo,
                        AccountName = detail.AccountName,
                        TransactionNo = generatedCode,
                        Debit = detail.Debit,
                        Credit = detail.Credit,
                        SubAccountType = detail.SubAccountType,
                        SubAccountId = detail.SubAccountId,
                        SubAccountName = detail.SubAccountName
                    });
                }

                newJournalVouchers.Add(newHeader);
            }

            await _dbContext.FilprideJournalVoucherHeaders
                .AddRangeAsync(newJournalVouchers, cancellationToken);

            existingAmortizationSetting.OccurrenceRemaining = 0;
            existingAmortizationSetting.IsActive = false;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        [HttpGet]
        public async Task<IActionResult> CreateReclass(CancellationToken cancellationToken)
        {
            var viewModel = new JvCreateReclassViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    (c.CvType == nameof(CVType.Invoicing) || c.CvType == nameof(CVType.Payment)) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.CoaList = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReclass(JvCreateReclassViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    (c.CvType == nameof(CVType.Invoicing) || c.CvType == nameof(CVType.Payment)) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.CoaList = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                var generateJvNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken: cancellationToken);
                var model = new FilprideJournalVoucherHeader
                {
                    Type = viewModel.Type,
                    JournalVoucherHeaderNo = generateJvNo,
                    Date = viewModel.TransactionDate,
                    References = viewModel.References,
                    CVId = viewModel.CvId,
                    Particulars = viewModel.Particulars,
                    CRNo = viewModel.CrNo,
                    JVReason = viewModel.Reason,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    JvType = nameof(JvType.Reclass)
                };

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = generateJvNo,
                            JournalVoucherHeaderId = model.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Created new journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", model.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Journal voucher # {model.JournalVoucherHeaderNo} created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditReclass(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .Include(jv => jv.Details)
                    .FirstOrDefaultAsync(jv => jv.JournalVoucherHeaderId == id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);
                if (await _unitOfWork.IsPeriodPostedAsync(Module.JournalVoucher, existingHeaderModel.Date, cancellationToken))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                JvEditReclassViewModel model = new()
                {
                    JvId = existingHeaderModel.JournalVoucherHeaderId,
                    TransactionDate = existingHeaderModel.Date,
                    References = existingHeaderModel.References,
                    CvId = existingHeaderModel.CVId,
                    Particulars = existingHeaderModel.Particulars,
                    CrNo = existingHeaderModel.CRNo,
                    Reason = existingHeaderModel.JVReason,
                    CvList = await _dbContext.FilprideCheckVoucherHeaders
                        .OrderBy(c => c.CheckVoucherHeaderNo)
                        .Where(c =>
                            c.Company == companyClaims &&
                            (c.CvType == nameof(CVType.Invoicing) || c.CvType == nameof(CVType.Payment)) &&
                            c.PostedBy != null)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync(cancellationToken),
                    CoaList = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                    MinDate = minDate,
                    Details = existingHeaderModel.Details!
                        .Select(d => new JvEditReclassDetailViewModel
                        {
                            AccountNo = d.AccountNo,
                            AccountTitle = $"{d.AccountNo} {d.AccountName}",
                            Debit = d.Debit,
                            Credit = d.Credit
                        })
                        .ToList(),
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch jv. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReclass(JvEditReclassViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.CvList = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderNo)
                .Where(c =>
                    c.Company == companyClaims &&
                    (c.CvType == nameof(CVType.Invoicing) || c.CvType == nameof(CVType.Payment)) &&
                    c.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.MinDate = await _unitOfWork
                .GetMinimumPeriodBasedOnThePostedPeriods(Module.JournalVoucher, cancellationToken);

            viewModel.CoaList = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == viewModel.JvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                await _dbContext.FilprideJournalVoucherDetails
                    .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Saving the default entries

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.References = viewModel.References;
                existingHeaderModel.CVId = viewModel.CvId;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.CRNo = viewModel.CrNo;
                existingHeaderModel.JVReason = viewModel.Reason;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion --Saving the default entries

                #region Details

                var jvDetails = new List<FilprideJournalVoucherDetail>();

                foreach (var acctNo in viewModel.Details)
                {
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                                           .GetAsync(coa => coa.AccountNumber == acctNo.AccountNo, cancellationToken)
                                       ?? throw new NullReferenceException($"Account number {acctNo.AccountNo} not found");

                    jvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = acctNo.AccountNo,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = existingHeaderModel.JournalVoucherHeaderNo!,
                            JournalVoucherHeaderId = existingHeaderModel.JournalVoucherHeaderId,
                            Debit = acctNo.Debit,
                            Credit = acctNo.Credit,
                        }
                    );
                }

                #endregion Details

                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", existingHeaderModel.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }
    }
}
