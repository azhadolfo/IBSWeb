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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD, SD.Department_HRAndAdminOrLegal)]
    public class CheckVoucherNonTradePaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradePaymentController> _logger;

        public CheckVoucherNonTradePaymentController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradePaymentController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
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
        public async Task<IActionResult> GetPaymentCheckVouchers([FromForm] DataTablesParameters parameters, DateOnly filterDate, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(x => x.Company == companyClaims
                                      && x.CvType == nameof(CVType.Payment), cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                    .Where(s =>
                        s.CheckVoucherHeaderNo?.ToLower().Contains(searchValue) == true ||
                        s.Total.ToString().Contains(searchValue) ||
                        s.Payee?.ToLower().Contains(searchValue) == true ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.Reference?.ToLower().Contains(searchValue) == true ||
                        s.Status.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }
                if (filterDate != DateOnly.MinValue && filterDate != default)
                {
                    var searchValue = filterDate.ToString(SD.Date_Format).ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
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

                    checkVoucherHeaders = checkVoucherHeaders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = checkVoucherHeaders.Count();

                var pagedData = checkVoucherHeaders
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
                _logger.LogError(ex, "Failed to get check voucher payment. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
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

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview check voucher# {header.CheckVoucherHeaderNo}", "Check Voucher", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (!cv.IsPrinted)
            {
                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed re-printed copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
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
                modelHeader.Status = nameof(CheckVoucherPaymentStatus.Posted);

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
                                EmployeeId = details.EmployeeId,
                                EmployeeName = details.EmployeeId.HasValue ? $"{details.Employee?.FirstName} {details.Employee?.MiddleName} {details.Employee?.LastName}" : null,
                                CompanyId = details.CompanyId,
                                CompanyName = details.Company?.CompanyName,
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

                #region --Disbursement Book Recording(CV)--

                var disbursement = new List<FilprideDisbursementBook>();
                foreach (var details in modelDetails)
                {
                    var bank = await _unitOfWork.FilprideBankAccount.GetAsync(model => model.BankAccountId == modelHeader.BankId, cancellationToken);
                    disbursement.Add(
                            new FilprideDisbursementBook
                            {
                                Date = modelHeader.Date,
                                CVNo = modelHeader.CheckVoucherHeaderNo!,
                                Payee = modelHeader.Payee!,
                                Amount = modelHeader.Total,
                                Particulars = modelHeader.Particulars!,
                                Bank = bank != null ? bank.Branch : "N/A",
                                CheckNo = modelHeader.CheckNo!,
                                CheckDate = modelHeader.CheckDate?.ToString("MM/dd/yyyy") ?? "N/A",
                                ChartOfAccount = details.AccountNo + " " + details.AccountName,
                                Debit = details.Debit,
                                Credit = details.Credit,
                                Company = modelHeader.Company,
                                CreatedBy = modelHeader.CreatedBy,
                                CreatedDate = modelHeader.CreatedDate
                            }
                        );
                }

                await _dbContext.FilprideDisbursementBooks.AddRangeAsync(disbursement, cancellationToken);

                #endregion --Disbursement Book Recording(CV)--

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy!, $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher", modelHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(p => p.CheckVoucherHeaderPaymentId == id)
                    .Include(p => p.CheckVoucherHeaderInvoice)
                    .ToListAsync(cancellationToken);

                foreach (var invoice in updateMultipleInvoicingVoucher)
                {
                    if (invoice.CheckVoucherHeaderInvoice!.IsPaid)
                    {
                        invoice.CheckVoucherHeaderInvoice!.Status = nameof(CheckVoucherInvoiceStatus.Paid);
                    }
                }

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Posted.";

                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);

                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                existingHeaderModel.CanceledBy = GetUserFullName();
                existingHeaderModel.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Status = nameof(CheckVoucherPaymentStatus.Canceled);
                existingHeaderModel.CancellationRemarks = cancellationRemarks;

                var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                    .Include(cvp => cvp.CheckVoucherHeaderPayment)
                    .ToListAsync(cancellationToken);

                foreach (var cv in getCVs)
                {
                    var existingDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                    d.SupplierId == existingHeaderModel.SupplierId)
                        .ToListAsync(cancellationToken);

                    foreach (var existingDetail in existingDetails)
                    {
                        existingDetail.AmountPaid = 0;
                    }

                    cv.CheckVoucherHeaderInvoice!.AmountPaid -= cv.AmountPaid;
                    cv.CheckVoucherHeaderInvoice.IsPaid = false;
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.CanceledBy!, $"Canceled check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Cancelled.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                    .Include(cvp => cvp.CheckVoucherHeaderPayment)
                    .ToListAsync(cancellationToken);

                foreach (var cv in getCVs)
                {
                    var existingDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                    d.SupplierId == existingHeaderModel.SupplierId)
                        .ToListAsync(cancellationToken);

                    foreach (var existingDetail in existingDetails)
                    {
                        existingDetail.AmountPaid = 0;
                    }

                    cv.CheckVoucherHeaderInvoice!.AmountPaid -= cv.AmountPaid;
                    cv.CheckVoucherHeaderInvoice.IsPaid = false;
                    cv.CheckVoucherHeaderInvoice.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

                }

                existingHeaderModel.PostedBy = null;
                existingHeaderModel.VoidedBy = GetUserFullName();
                existingHeaderModel.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Status = nameof(CheckVoucherPaymentStatus.Voided);


                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == existingHeaderModel.CheckVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == existingHeaderModel.CheckVoucherHeaderNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.VoidedBy!, $"Voided check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Voided.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Unpost(int id, CancellationToken cancellationToken)
        {
            var cvHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (cvHeader == null)
            {
                throw new NullReferenceException("CV Header not found.");
            }

            var userName = _userManager.GetUserName(this.User);

            if (userName == null)
            {
                throw new NullReferenceException("User not found.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (cvHeader.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException($"Cannot unpost this record because the period {cvHeader.Date:MMM yyyy} is already closed.");
                }

                cvHeader.PostedBy = null;
                cvHeader.Status = nameof(CheckVoucherPaymentStatus.ForPosting);

                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == cvHeader.CheckVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(d => d.CVNo == cvHeader.CheckVoucherHeaderNo, cancellationToken);

                var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(mcvp => mcvp.CheckVoucherHeaderPaymentId == id)
                    .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                    .ToListAsync(cancellationToken);

                foreach (var invoice in updateMultipleInvoicingVoucher)
                {
                    if (invoice.CheckVoucherHeaderInvoice!.IsPaid)
                    {
                        invoice.CheckVoucherHeaderInvoice!.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);
                    }
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(userName, $"Unposted check voucher# {cvHeader.CheckVoucherHeaderNo}", "Check Voucher", cvHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Unposted.";

                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unpost check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Print), new { id });
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
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

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
                    .Include(cvd => cvd.Supplier)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingDetailsModel == null)
                {
                    return NotFound();
                }

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var checkVoucher = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd =>
                        cvd.CheckVoucherHeader!.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null &&
                        cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                        cvd.CheckVoucherHeader.Company == companyClaims ||
                        cvd.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null &&
                        cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                        cvd.CheckVoucherHeaderId == cvd.CheckVoucherHeader.CheckVoucherHeaderId &&
                        cvd.CheckVoucherHeader.Company == companyClaims)
                    .Include(cvd => cvd.CheckVoucherHeader)
                    .OrderBy(cvd => cvd.CheckVoucherDetailId)
                    .Select(cvd => new SelectListItem
                    {
                        Value = cvd.CheckVoucherHeaderId.ToString(),
                        Text = cvd.CheckVoucherHeader!.CheckVoucherHeaderNo
                    })
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var suppliers =
                    await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                var bankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Select(cvp => cvp.CheckVoucherHeaderInvoiceId)
                    .ToListAsync(cancellationToken);

                //for trim the system generated invoice reference to payment
                var particulars = existingHeaderModel.Particulars ?? "";
                var index = particulars.IndexOf("Payment for", StringComparison.Ordinal);

                CheckVoucherNonTradePaymentViewModel model = new()
                {
                    TransactionDate = existingHeaderModel.Date,
                    MultipleCvId = getCVs.ToArray(),
                    CheckVouchers = checkVoucher,
                    Total = existingHeaderModel.AmountPaid,
                    BankId = existingHeaderModel.BankId ?? 0,
                    Banks = bankAccounts,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? default,
                    Particulars = index >= 0 ? particulars.Substring(0, index).Trim() : particulars,
                    Payee = existingHeaderModel.SupplierId != null
                        ? existingHeaderModel.Supplier!.SupplierName
                        : existingDetailsModel.Supplier!.SupplierName,
                    PayeeAddress = existingHeaderModel.Address,
                    PayeeTin = existingHeaderModel.Tin,
                    MultipleSupplierId = existingHeaderModel.SupplierId ?? existingDetailsModel.SupplierId,
                    Suppliers = suppliers,
                    CvId = existingHeaderModel.CheckVoucherHeaderId,
                    OldCVNo = existingHeaderModel.OldCvNo,
                    MinDate = minDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv non trade payment. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region--Get Check Voucher Invoicing

                var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                    .Include(cv => cv.Supplier)
                    .Include(x => x.Details)
                    .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                    .Include(cvp => cvp.CheckVoucherHeaderPayment)
                    .ToListAsync(cancellationToken);

                foreach (var cv in getCVs)
                {
                    var existingDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                    d.SupplierId == existingHeaderModel.SupplierId)
                        .ToListAsync(cancellationToken);

                    foreach (var existingDetail in existingDetails)
                    {
                        existingDetail.AmountPaid = 0;
                    }

                }

                var invoicingVoucher = (await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cv => viewModel.MultipleCvId!.Contains(cv.CheckVoucherHeaderId), cancellationToken))
                    .OrderBy(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                var isForTheBir = false;

                foreach (var invoice in invoicingVoucher)
                {
                    var cv = viewModel.PaymentDetails.FirstOrDefault(c => c.CVId == invoice.CheckVoucherHeaderId);

                    if (cv == null)
                    {
                        return NotFound();
                    }

                    var getCvDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(i => cv.CVId == i.CheckVoucherHeaderId &&
                                    i.SupplierId != null &&
                                    i.SupplierId == viewModel.MultipleSupplierId &&
                                    i.CheckVoucherHeader!.CvType == nameof(CVType.Invoicing))
                        .OrderBy(i => i.CheckVoucherHeaderId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (getCvDetails == null || getCvDetails.CheckVoucherHeaderId != cv.CVId)
                    {
                        continue;
                    }

                    getCvDetails.AmountPaid += cv.AmountPaid;
                    isForTheBir = getCvDetails.SupplierId == 133
                                  && invoice.SupplierId != 133
                                  && !getCvDetails.IsUserSelected; //BIR Supplier Id
                }

                #endregion

                #region -- Saving the default entries --

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                #region -- Check Voucher Header --

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.PONo = invoicingVoucher.Select(i => i.PONo).FirstOrDefault();
                existingHeaderModel.SINo = invoicingVoucher.Select(i => i.SINo).FirstOrDefault();
                existingHeaderModel.SupplierId = viewModel.MultipleSupplierId;
                existingHeaderModel.Particulars = $"{viewModel.Particulars}. Payment for {string.Join(",", invoicingVoucher.Select(i => i.CheckVoucherHeaderNo))}";
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Category = "Non-Trade";
                existingHeaderModel.CvType = nameof(CVType.Payment);
                existingHeaderModel.Reference = string.Join(", ", invoicingVoucher.Select(inv => inv.CheckVoucherHeaderNo));
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.Address = viewModel.PayeeAddress;
                existingHeaderModel.Tin = viewModel.PayeeTin;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.CheckAmount = viewModel.Total;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.OldCvNo = viewModel.OldCVNo;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;

                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Check Voucher Header --

                #region -- Multiple Payment Storing --

                foreach (var cv in getCVs)
                {
                    if (isForTheBir)
                    {
                        continue;
                    }

                    cv.CheckVoucherHeaderInvoice!.AmountPaid -= cv.AmountPaid;
                    cv.CheckVoucherHeaderInvoice.IsPaid = false;
                }

                _dbContext.RemoveRange(getCVs);

                foreach (var paymentDetail in viewModel.PaymentDetails)
                {
                    FilprideMultipleCheckVoucherPayment multipleCheckVoucherPayment = new()
                    {
                        Id = Guid.NewGuid(),
                        CheckVoucherHeaderPaymentId = existingHeaderModel.CheckVoucherHeaderId,
                        CheckVoucherHeaderInvoiceId = paymentDetail.CVId,
                        AmountPaid = paymentDetail.AmountPaid,
                    };

                    _dbContext.Add(multipleCheckVoucherPayment);
                    await _unitOfWork.SaveAsync(cancellationToken);
                }

                #region--Update invoicing voucher

                var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(mcvp => viewModel.MultipleCvId!.Contains(mcvp.CheckVoucherHeaderInvoiceId)
                                   && mcvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                    .ToListAsync(cancellationToken);

                foreach (var payment in updateMultipleInvoicingVoucher)
                {
                    if (isForTheBir)
                    {
                        continue;
                    }

                    payment.CheckVoucherHeaderInvoice!.AmountPaid += payment.AmountPaid;
                    if (payment.CheckVoucherHeaderInvoice?.AmountPaid >= payment.CheckVoucherHeaderInvoice?.InvoiceAmount)
                    {
                        payment.CheckVoucherHeaderInvoice.IsPaid = true;
                    }
                }

                #endregion

                #endregion -- Multiple Payment Storing --

                #region -- Check Voucher Details --

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var details = new List<FilprideCheckVoucherDetail>();

                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    details.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        Amount = 0,
                        SupplierId = viewModel.AccountTitle[i] != "Cash in Bank" ? viewModel.MultipleSupplierId : null,
                        BankId = viewModel.AccountTitle[i] == "Cash in Bank" ? viewModel.BankId : null,
                    });
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                #endregion -- Check Voucher Details --

                #endregion -- Saving the default entries --

                #region -- Uploading file --
                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }
                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher payment edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMultipleSupplierDetails(int cvId, int suppId, CancellationToken cancellationToken)
        {
            var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == suppId, cancellationToken);

            var credit = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.SupplierId == suppId && cvd.CheckVoucherHeaderId == cvId)
                .Include(cvd => cvd.CheckVoucherHeader)
                .Select(cvd => new
                {
                    RemainingCredit = cvd.Credit - cvd.AmountPaid,
                    cvd.CheckVoucherHeader!.Particulars
                })
                .FirstOrDefaultAsync(cancellationToken);

            // Ensure that cv is not null before proceeding
            if (supplier == null || credit == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Payee = supplier.SupplierName,
                PayeeAddress = supplier.SupplierAddress,
                PayeeTin = supplier.SupplierTin,
                credit.Particulars,
                Total = credit.RemainingCredit,
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetMultipleSupplier(int cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return Json(null);
            }

            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(c => c.CheckVoucherHeaderId == cvId, cancellationToken);

            // Ensure that cv is not null before proceeding
            if (cv == null)
            {
                return Json(null);
            }

            // Fetch suppliers whose IDs are in the supplierIds list
            var suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

            return Json(new
            {
                SupplierList = suppliers
            });
        }

        [HttpGet]
        public async Task<JsonResult> MultipleSupplierDetails(int suppId, int cvId, CancellationToken cancellationToken)
        {
            var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(s => s.SupplierId == suppId, cancellationToken);

            var credit = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.SupplierId == suppId && cvd.CheckVoucherHeaderId == cvId)
                .Include(cvd => cvd.CheckVoucherHeader)
                .Select(cvd => new
                {
                    RemainingCredit = cvd.Credit - cvd.AmountPaid,
                    cvd.CheckVoucherHeader!.Particulars
                })
                .FirstOrDefaultAsync(cancellationToken);


            // Ensure that cv is not null before proceeding
            if (supplier == null || credit == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Payee = supplier.SupplierName,
                PayeeAddress = supplier.SupplierAddress,
                PayeeTin = supplier.SupplierTin,
                credit.Particulars,
                Total = credit.RemainingCredit
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckVoucherInvoiceDetails(int? invoiceId, CancellationToken cancellationToken)
        {
            if (invoiceId == null)
            {
                return Json(null);
            }

            var invoice = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(i => i.CheckVoucherHeaderId == invoiceId, cancellationToken);

            if (invoice == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Payee = invoice.Supplier!.SupplierName,
                PayeeAddress = invoice.Supplier.SupplierAddress,
                PayeeTin = invoice.Supplier.SupplierTin,
                invoice.Particulars,
                Total = invoice.InvoiceAmount
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region--Get Check Voucher Invoicing

                bool isForTheBir = false;

                var invoicingVoucher = (await _unitOfWork.FilprideCheckVoucher
                        .GetAllAsync(cv => viewModel.MultipleCvId!.Contains(cv.CheckVoucherHeaderId), cancellationToken))
                    .OrderBy(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                foreach (var invoice in invoicingVoucher)
                {
                    var cv = viewModel.PaymentDetails.FirstOrDefault(c => c.CVId == invoice.CheckVoucherHeaderId);

                    if (cv == null)
                    {
                        return NotFound();
                    }

                    var getCvDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(i => cv.CVId == i.CheckVoucherHeaderId &&
                                    i.SupplierId != null &&
                                    i.SupplierId == viewModel.MultipleSupplierId &&
                                    i.CheckVoucherHeader!.CvType == nameof(CVType.Invoicing) &&
                                    i.Amount > 0m)
                        .OrderBy(i => i.CheckVoucherHeaderId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (getCvDetails == null || getCvDetails.CheckVoucherHeaderId != cv.CVId)
                    {
                        continue;
                    }

                    getCvDetails.AmountPaid += cv.AmountPaid;
                    isForTheBir = getCvDetails.SupplierId == 133
                                  && invoice.SupplierId != 133
                                  && !getCvDetails.IsUserSelected; //BIR Supplier Id
                }

                #endregion

                #region -- Saving the default entries --

                #region -- Get Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.MultipleSupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                #region -- Check Voucher Header --

                FilprideCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultiplePaymentAsync(companyClaims, invoicingVoucher.Select(i => i.Type).FirstOrDefault() ?? throw new InvalidOperationException(), cancellationToken),
                    Date = viewModel.TransactionDate,
                    PONo = invoicingVoucher.Select(i => i.PONo).FirstOrDefault(),
                    SINo = invoicingVoucher.Select(i => i.SINo).FirstOrDefault(),
                    SupplierId = viewModel.MultipleSupplierId,
                    Particulars = $"{viewModel.Particulars}. Payment for {string.Join(",", invoicingVoucher.Select(i => i.CheckVoucherHeaderNo))}",
                    Total = viewModel.Total,
                    CreatedBy = GetUserFullName(),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Payment),
                    Reference = string.Join(", ", invoicingVoucher.Select(inv => inv.CheckVoucherHeaderNo)),
                    BankId = viewModel.BankId,
                    Payee = viewModel.Payee,
                    Address = viewModel.PayeeAddress,
                    Tin = viewModel.PayeeTin,
                    CheckNo = viewModel.CheckNo,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.Total,
                    Company = companyClaims,
                    Type = invoicingVoucher.Select(i => i.Type).First(),
                    OldCvNo = viewModel.OldCVNo,
                    SupplierName = supplier.SupplierName,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    TaxType = string.Empty,
                    VatType = string.Empty,
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(checkVoucherHeader, cancellationToken);

                #endregion -- Check Voucher Header --

                #region -- Multiple Payment Storing --

                foreach (var paymentDetail in viewModel.PaymentDetails)
                {
                    FilprideMultipleCheckVoucherPayment multipleCheckVoucherPayment = new()
                    {
                        Id = Guid.NewGuid(),
                        CheckVoucherHeaderPaymentId = checkVoucherHeader.CheckVoucherHeaderId,
                        CheckVoucherHeaderInvoiceId = paymentDetail.CVId,
                        AmountPaid = paymentDetail.AmountPaid,
                    };

                    _dbContext.Add(multipleCheckVoucherPayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #region--Update invoicing voucher

                var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(mcvp => viewModel.MultipleCvId!.Contains(mcvp.CheckVoucherHeaderInvoiceId) && mcvp.CheckVoucherHeaderPaymentId == checkVoucherHeader.CheckVoucherHeaderId)
                    .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                    .ToListAsync(cancellationToken);

                foreach (var payment in updateMultipleInvoicingVoucher)
                {
                    if (isForTheBir)
                    {
                        continue;
                    }

                    payment.CheckVoucherHeaderInvoice!.AmountPaid += payment.AmountPaid;
                    if (payment.CheckVoucherHeaderInvoice?.AmountPaid >= payment.CheckVoucherHeaderInvoice?.InvoiceAmount)
                    {
                        payment.CheckVoucherHeaderInvoice.IsPaid = true;
                    }
                }

                #endregion

                #endregion -- Multiple Payment Storing --

                #region -- Check Voucher Details --

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
                            Amount = 0,
                            SupplierId = viewModel.AccountTitle[i] != "Cash in Bank" ? viewModel.MultipleSupplierId : null,
                            BankId = viewModel.AccountTitle[i] == "Cash in Bank" ? viewModel.BankId : null,
                        });
                    }
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- Check Voucher Details --

                #endregion -- Saving the default entries --

                #region -- Uploading file --
                if (file != null && file.Length > 0)
                {
                    checkVoucherHeader.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    checkVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, checkVoucherHeader.SupportingFileSavedFileName!);
                }
                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", checkVoucherHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment #{checkVoucherHeader.CheckVoucherHeaderNo} created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                viewModel.Suppliers = await _unitOfWork.GetFilprideNonTradeSupplierListAsyncById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId, CancellationToken cancellationToken = default)
        {
            if (supplierId == null)
            {
                return Json(null);
            }

            var supplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId, cancellationToken);

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
                WithholdingTax = supplier.WithholdingTaxTitle
            });
        }

        public async Task<IActionResult> GetCVs(int supplierId, int? paymentId, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.FilprideCheckVoucherDetails
                    .Include(cvd => cvd.CheckVoucherHeader)
                    .Where(cvd =>
                        cvd.CheckVoucherHeader!.PostedBy != null &&
                        cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                        (
                            (cvd.CheckVoucherHeader.SupplierId != null &&
                             cvd.CheckVoucherHeader.SupplierId == supplierId &&
                             !cvd.CheckVoucherHeader.IsPaid) ||
                            (cvd.SupplierId != null &&
                             cvd.SupplierId == supplierId &&
                             cvd.Amount > cvd.AmountPaid)
                        ));

                if (paymentId != null)
                {
                    var existingInvoiceIds = await _dbContext.FilprideMultipleCheckVoucherPayments
                        .Where(m => m.CheckVoucherHeaderPaymentId == paymentId)
                        .Select(m => m.CheckVoucherHeaderInvoiceId)
                        .ToListAsync(cancellationToken);

                    // Include existing records in the query
                    query = query.Union(_dbContext.FilprideCheckVoucherDetails
                        .Include(cvd => cvd.CheckVoucherHeader)
                        .Where(cvd => cvd.SupplierId == supplierId && existingInvoiceIds.Contains(cvd.CheckVoucherHeaderId)));
                }

                var checkVouchers = await query.ToListAsync(cancellationToken);

                if (!checkVouchers.Any())
                {
                    return Json(null);
                }

                var cvList = checkVouchers
                    .OrderBy(cv => cv.CheckVoucherDetailId)
                    .Select(cv => new
                    {
                        Id = cv.CheckVoucherHeader!.CheckVoucherHeaderId,
                        CVNumber = cv.CheckVoucherHeader.CheckVoucherHeaderNo
                    })
                    .Distinct()
                    .ToList();

                return Json(cvList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get check voucher. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] cvId, int supplierId, CancellationToken cancellationToken)
        {
            if (cvId.Length == 0)
            {
                return Json(null);
            }

            var invoices = await _dbContext.FilprideCheckVoucherDetails
                .Include(i => i.Supplier)
                .Include(i => i.CheckVoucherHeader)
                .Where(i => cvId.Contains(i.CheckVoucherHeaderId) &&
                            i.SupplierId == supplierId &&
                            i.AccountName == "AP-Non Trade Payable")
                .ToListAsync(cancellationToken);

            // Get the first CV's particulars
            var firstParticulars = invoices.FirstOrDefault()?.CheckVoucherHeader?.Particulars ?? "";

            var journalEntries = new List<object>();
            var totalDebit = 0m;
            var cvBalances = new List<object>();

            var groupedInvoices = invoices.GroupBy(i => i.AccountNo);

            foreach (var invoice in invoices)
            {
                cvBalances.Add(new
                {
                    CvId = invoice.CheckVoucherHeaderId,
                    CvNumber = invoice.TransactionNo,
                    Balance = invoice.Credit,
                });
            }


            foreach (var invoice in groupedInvoices)
            {
                var balance = invoice.Sum(i => i.Credit);
                journalEntries.Add(new
                {
                    AccountNumber = invoice.First().AccountNo,
                    AccountTitle = invoice.First().AccountName,
                    Debit = balance,
                    Credit = 0m
                });
                totalDebit += balance;
            }

            // Add the "Cash in Bank" entry
            journalEntries.Add(new
            {
                AccountNumber = "101010100",
                AccountTitle = "Cash in Bank",
                Debit = 0m,
                Credit = totalDebit
            });

            return Json(new
            {
                JournalEntries = journalEntries,
                TotalDebit = totalDebit,
                TotalCredit = totalDebit,
                Particulars = firstParticulars,
                CvBalances = cvBalances
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdvancesToEmployee(CancellationToken cancellationToken)
        {
            var viewModel = new AdvancesToEmployeeViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);

            viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdvancesToEmployee(AdvancesToEmployeeViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                viewModel.Employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region Save Record

                #region Header

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                FilprideCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultiplePaymentAsync(companyClaims, viewModel.DocumentType!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Particulars = viewModel.Particulars,
                    PONo = [],
                    SINo = [],
                    Total = viewModel.Total,
                    CreatedBy = GetUserFullName(),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Payment),
                    BankId = viewModel.BankId,
                    Payee = viewModel.Payee,
                    SupplierName = viewModel.Payee,
                    Address = viewModel.PayeeAddress,
                    Tin = viewModel.PayeeTin,
                    CheckNo = viewModel.CheckNo,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.Total,
                    Company = companyClaims,
                    Type = viewModel.DocumentType,
                    IsAdvances = true,
                    EmployeeId = viewModel.EmployeeId,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    TaxType = string.Empty,
                    VatType = string.Empty
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(checkVoucherHeader, cancellationToken);

                #endregion

                #region Details

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToOfficerTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020400") ?? throw new ArgumentException($"Account title '101020400' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException($"Account title '101010100' not found.");

                var checkVoucherDetails = new List<FilprideCheckVoucherDetail>
                {
                    new()
                    {
                        AccountNo = advancesToOfficerTitle.AccountNumber,
                        AccountName = advancesToOfficerTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = viewModel.Total,
                        Credit = 0,
                        EmployeeId = viewModel.EmployeeId,
                    },

                    new()
                    {
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountName = cashInBankTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = viewModel.Total,
                        BankId = viewModel.BankId,
                    },
                };

                await _dbContext.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion

                #endregion

                #region Uploading File

                if (viewModel.SupportingFile != null && viewModel.SupportingFile.Length > 0)
                {
                    checkVoucherHeader.SupportingFileSavedFileName = GenerateFileNameToSave(viewModel.SupportingFile.FileName);
                    checkVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(viewModel.SupportingFile, checkVoucherHeader.SupportingFileSavedFileName!);
                }

                #endregion Uploading File

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", checkVoucherHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment #{checkVoucherHeader.CheckVoucherHeaderNo} created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create advances to employee. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);

                viewModel.Employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);

                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditAdvancesToEmployee(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

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

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);

                var bankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                AdvancesToEmployeeViewModel model = new()
                {
                    CvId = existingHeaderModel.CheckVoucherHeaderId,
                    TransactionDate = existingHeaderModel.Date,
                    EmployeeId = existingHeaderModel.EmployeeId ?? 0,
                    Employees = employees,
                    Payee = existingHeaderModel.Payee!,
                    PayeeAddress = existingHeaderModel.Address,
                    PayeeTin = existingHeaderModel.Tin,
                    Total = existingHeaderModel.Total,
                    BankId = existingHeaderModel.BankId ?? 0,
                    Banks = bankAccounts,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? default,
                    Particulars = existingHeaderModel.Particulars!,
                    MinDate = minDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv non trade advances to employee. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdvancesToEmployee(AdvancesToEmployeeViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                #region Update Record

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                #region Header

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.SupplierName = viewModel.Payee;
                existingHeaderModel.Address = viewModel.PayeeAddress;
                existingHeaderModel.Tin = viewModel.PayeeTin;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.CheckAmount = viewModel.Total;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;

                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion Header

                #region Details

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToOfficerTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020400") ?? throw new ArgumentException($"Account title '101020400' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException($"Account title '101010100' not found.");

                var checkVoucherDetails = new List<FilprideCheckVoucherDetail>
                {
                    new()
                    {
                        AccountNo = advancesToOfficerTitle.AccountNumber,
                        AccountName = advancesToOfficerTitle.AccountName,
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = viewModel.Total,
                        Credit = 0,
                        EmployeeId = viewModel.EmployeeId,
                    },

                    new()
                    {
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountName = cashInBankTitle.AccountName,
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = viewModel.Total,
                        BankId = viewModel.BankId,
                    },
                };

                await _dbContext.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion Details

                #endregion Update Record

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher payment edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit advances to employee. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.Employees = await _unitOfWork.GetFilprideEmployeeListById(cancellationToken);

                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }


        public async Task<IActionResult> GetEmployeeDetails(int? employeeId)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (employeeId == null)
            {
                return Json(null);
            }

            var employee = await _unitOfWork.FilprideEmployee
                .GetAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Name = $"{employee.FirstName} {employee.LastName}",
                employee.Address,
                employee.TinNo,
            });

        }

        [HttpGet]
        public async Task<IActionResult> CreateAdvancesToSupplier(CancellationToken cancellationToken)
        {
            var viewModel = new AdvancesToSupplierViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

            viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdvancesToSupplier(AdvancesToSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region Save Record

                #region Header

                #region -- Get Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve Supplier

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                FilprideCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultiplePaymentAsync(companyClaims, viewModel.DocumentType!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Particulars = viewModel.Particulars,
                    PONo = [],
                    SINo = [],
                    Total = viewModel.Total,
                    CreatedBy = GetUserFullName(),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Payment),
                    BankId = viewModel.BankId,
                    Payee = viewModel.Payee,
                    Address = viewModel.PayeeAddress,
                    Tin = viewModel.PayeeTin,
                    CheckNo = viewModel.CheckNo,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.Total,
                    Company = companyClaims,
                    Type = viewModel.DocumentType,
                    IsAdvances = true,
                    SupplierId = viewModel.SupplierId,
                    SupplierName = supplier.SupplierName,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    TaxType = supplier.TaxType,
                    VatType = supplier.VatType,
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(checkVoucherHeader, cancellationToken);

                #endregion

                #region Details

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToSupplierTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060100") ?? throw new ArgumentException("Account title '101060100' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

                var grossAmount = viewModel.Total;
                var ewtAmount = _unitOfWork.FilprideCheckVoucher.ComputeEwtAmount(grossAmount, 0.01m);
                var netOfEwtAmount = _unitOfWork.FilprideCheckVoucher.ComputeNetOfEwt(grossAmount, ewtAmount);

                var checkVoucherDetails = new List<FilprideCheckVoucherDetail>
                {
                    new()
                    {
                        AccountNo = advancesToSupplierTitle.AccountNumber,
                        AccountName = advancesToSupplierTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = grossAmount,
                        Credit = 0,
                        SupplierId = viewModel.SupplierId,
                    },

                    new()
                    {
                        AccountNo = ewtTitle.AccountNumber,
                        AccountName = ewtTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtAmount,
                    },

                    new()
                    {
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountName = cashInBankTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = netOfEwtAmount,
                        BankId = viewModel.BankId,
                    },
                };

                await _dbContext.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion

                #endregion

                #region Uploading File

                if (viewModel.SupportingFile != null && viewModel.SupportingFile.Length > 0)
                {
                    checkVoucherHeader.SupportingFileSavedFileName = GenerateFileNameToSave(viewModel.SupportingFile.FileName);
                    checkVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(viewModel.SupportingFile, checkVoucherHeader.SupportingFileSavedFileName!);
                }

                #endregion Uploading File

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", checkVoucherHeader.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment #{checkVoucherHeader.CheckVoucherHeaderNo} created successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create advances to supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["Error"] = ex.Message;
                await transaction.RollbackAsync(cancellationToken);

                viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditAdvancesToSupplier(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

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

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var supplier =
                    await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims,
                        cancellationToken);

                var bankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                AdvancesToSupplierViewModel model = new()
                {
                    CvId = existingHeaderModel.CheckVoucherHeaderId,
                    TransactionDate = existingHeaderModel.Date,
                    SupplierId = existingHeaderModel.SupplierId ?? 0,
                    Suppliers = supplier,
                    Payee = existingHeaderModel.Payee!,
                    PayeeAddress = existingHeaderModel.Address,
                    PayeeTin = existingHeaderModel.Tin,
                    Total = existingHeaderModel.Total,
                    BankId = existingHeaderModel.BankId ?? 0,
                    Banks = bankAccounts,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? default,
                    Particulars = existingHeaderModel.Particulars!,
                    MinDate = minDate
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv non trade advances to supplier. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdvancesToSupplier(AdvancesToSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);
                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
                viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                #region Update Record

                #region Header

                #region -- Get Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve Supplier

                #region  -- Get bank account

                var bank = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (bank == null)
                {
                    return NotFound();
                }

                #endregion

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.Address = viewModel.PayeeAddress;
                existingHeaderModel.Tin = viewModel.PayeeTin;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.CheckAmount = viewModel.Total;
                existingHeaderModel.SupplierId = viewModel.SupplierId;
                existingHeaderModel.SupplierName = supplier.SupplierName;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;

                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion Header

                #region Details

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToSupplierTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060100") ?? throw new ArgumentException("Account title '101060100' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

                var grossAmount = viewModel.Total;
                var ewtAmount = _unitOfWork.FilprideCheckVoucher.ComputeEwtAmount(grossAmount, 0.01m);
                var netOfEwtAmount = _unitOfWork.FilprideCheckVoucher.ComputeNetOfEwt(grossAmount, ewtAmount);

                var checkVoucherDetails = new List<FilprideCheckVoucherDetail>
                {
                    new()
                    {
                        AccountNo = advancesToSupplierTitle.AccountNumber,
                        AccountName = advancesToSupplierTitle.AccountName,
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = grossAmount,
                        Credit = 0,
                        SupplierId = viewModel.SupplierId,
                    },

                    new()
                    {
                        AccountNo = ewtTitle.AccountNumber,
                        AccountName = ewtTitle.AccountName,
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtAmount,
                    },

                    new()
                    {
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountName = cashInBankTitle.AccountName,
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = netOfEwtAmount,
                        BankId = viewModel.BankId,
                    },
                };

                await _dbContext.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion Details

                #endregion Update Record

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher payment edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit advances to supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                viewModel.Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                viewModel.Banks = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public IActionResult CheckNoIsExist(string checkNo, int? cvId)
        {
            if (cvId.HasValue)
            {
                var existingCheckNo = _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cv => cv.CheckVoucherHeaderId == cvId)
                    .Result?
                    .CheckNo;

                if (checkNo == existingCheckNo)
                {
                    return Json(false);
                }
            }

            var exists = _unitOfWork.FilprideCheckVoucher
                .GetAllAsync(cv => cv.CanceledBy == null && cv.VoidedBy == null)
                .Result
                .Any(cv => cv.CheckNo == checkNo);

            return Json(exists);
        }

    }
}
