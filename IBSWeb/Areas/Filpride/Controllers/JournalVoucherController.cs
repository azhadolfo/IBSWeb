using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;

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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.JournalVoucher))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var journalVoucherHeader = await _unitOfWork.FilprideJournalVoucher
                    .GetAllAsync(jv => jv.Company == companyClaims && jv.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", journalVoucherHeader);
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
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo!.Contains(searchValue) == true ||
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
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new JournalVoucherVM
            {
                Header = new FilprideJournalVoucherHeader(),
                Details = new List<FilprideJournalVoucherDetail>()
            };

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Header.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.Header.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Where(c => c.Company == companyClaims &&
                            c.CvType == nameof(CVType.Payment) &&
                            c.PostedBy != null) ///TODO in the future show only the cleared payment
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalVoucherVM? model, CancellationToken cancellationToken, string[] accountNumber, decimal[]? debit, decimal[]? credit)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            model!.Header!.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            model.Header.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Where(c => c.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --CV Details Entry

                var generateJvNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, model.Header.Type, cancellationToken);
                var cvDetails = new List<FilprideJournalVoucherDetail>();

                var totalDebit = 0m;
                var totalCredit = 0m;
                if (totalDebit != totalCredit)
                {
                    TempData["warning"] = "The debit and credit should be equal!";
                    return View(model);
                }

                #endregion --CV Details Entry

                #region --Saving the default entries

                //JV Header Entry
                model.Header.JournalVoucherHeaderNo = generateJvNo;
                model.Header.CreatedBy = GetUserFullName();
                model.Header.Company = companyClaims;

                #endregion --Saving the default entries

                await _dbContext.AddAsync(model.Header, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                for (int i = 0; i < accountNumber.Length; i++)
                {
                    var currentAccountNumber = accountNumber[i];
                    var accountTitle = await _unitOfWork.FilprideChartOfAccount
                        .GetAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken);
                    var currentDebit = debit![i];
                    var currentCredit = credit![i];
                    totalDebit += debit[i];
                    totalCredit += credit[i];

                    cvDetails.Add(
                        new FilprideJournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle!.AccountName,
                            TransactionNo = generateJvNo,
                            JournalVoucherHeaderId = model.Header.JournalVoucherHeaderId,
                            Debit = currentDebit,
                            Credit = currentCredit
                        }
                    );
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.Header.CreatedBy!, $"Created new journal voucher# {model.Header.JournalVoucherHeaderNo}", "Journal Voucher", model.Header.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddRangeAsync(cvDetails, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal voucher created successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(model);
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
                    TotalDebit = model.Details!.Select(cvd => cvd.Debit).Sum(),
                    TotalCredit = model.Details!.Select(cvd => cvd.Credit).Sum(),
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
                .FirstOrDefaultAsync(jvh => jvh.JournalVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideJournalVoucherDetails
                .Where(jvd => jvd.JournalVoucherHeaderId == header.JournalVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var viewModel = new JournalVoucherVM
            {
                Header = header,
                Details = details
            };

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(GetUserFullName(), $"Preview journal voucher# {header.JournalVoucherHeaderNo}", "Journal Voucher", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.FilprideJournalVoucherHeaders
                .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            var modelDetails = await _dbContext.FilprideJournalVoucherDetails
                .Where(jvd => jvd.JournalVoucherHeaderId == modelHeader.JournalVoucherHeaderId)
                .ToListAsync(cancellationToken: cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                modelHeader.PostedBy = GetUserFullName();
                modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                modelHeader.Status = nameof(Status.Posted);

                #region --General Ledger Book Recording(GL)--

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var ledgers = new List<FilprideGeneralLedgerBook>();

                foreach (var details in modelDetails)
                {
                    var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account title '{details.AccountNo}' not found.");
                    ledgers.Add(
                        new FilprideGeneralLedgerBook
                        {
                            Date = modelHeader.Date,
                            Reference = modelHeader.JournalVoucherHeaderNo!,
                            Description = modelHeader.Particulars,
                            AccountId = account.AccountId,
                            AccountNo = account.AccountNumber,
                            AccountTitle = account.AccountName,
                            Debit = details.Debit,
                            Credit = details.Credit,
                            Company = modelHeader.Company,
                            CreatedBy = modelHeader.CreatedBy,
                            CreatedDate = modelHeader.CreatedDate
                        }
                    );
                }

                if (!_unitOfWork.FilprideJournalVoucher.IsJournalEntriesBalanced(ledgers))
                {
                    throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                }

                await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                #endregion --General Ledger Book Recording(GL)--

                #region --Journal Book Recording(JV)--

                var journalBook = new List<FilprideJournalBook>();
                foreach (var details in modelDetails)
                {
                    journalBook.Add(
                        new FilprideJournalBook
                        {
                            Date = modelHeader.Date,
                            Reference = modelHeader.JournalVoucherHeaderNo!,
                            Description = modelHeader.Particulars,
                            AccountTitle = details.AccountNo + " " + details.AccountName,
                            Debit = details.Debit,
                            Credit = details.Credit,
                            Company = modelHeader.Company,
                            CreatedBy = modelHeader.CreatedBy,
                            CreatedDate = modelHeader.CreatedDate
                        }
                    );
                }

                await _dbContext.FilprideJournalBooks.AddRangeAsync(journalBook, cancellationToken);

                #endregion --Journal Book Recording(JV)--

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
            var model = await _dbContext.FilprideJournalVoucherHeaders
                .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            try
            {
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
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                .Include(jv => jv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
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
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.Company == companyClaims &&
                                c.CvType == nameof(CVType.Payment) &&
                                c.PostedBy != null)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken),
                COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken)
            };


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JournalVoucherViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --CV Details Entry

                var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                    .FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == viewModel.JVId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails
                    .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                // Dictionary to keep track of AccountNo and their ids for comparison
                var accountTitleDict = new Dictionary<string, List<int>>();
                foreach (var details in existingDetailsModel)
                {
                    if (!accountTitleDict.ContainsKey(details.AccountNo))
                    {
                        accountTitleDict[details.AccountNo] = new List<int>();
                    }
                    accountTitleDict[details.AccountNo].Add(details.JournalVoucherDetailId);
                }

                // Add or update records
                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                    {
                        // Update the first matching record and remove it from the list
                        var detailsId = ids.First();
                        ids.RemoveAt(0);
                        var details = existingDetailsModel.First(o => o.JournalVoucherDetailId == detailsId);
                        var currentAccountNumber = viewModel.AccountNumber[i];
                        var accountTitle = await _unitOfWork.FilprideChartOfAccount
                            .GetAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken);

                        details.AccountNo = currentAccountNumber;
                        details.AccountName = accountTitle!.AccountName;
                        details.Debit = viewModel.Debit[i];
                        details.Credit = viewModel.Credit[i];
                        details.TransactionNo = viewModel.JVNo!;
                        details.JournalVoucherHeaderId = viewModel.JVId;

                        if (ids.Count == 0)
                        {
                            accountTitleDict.Remove(viewModel.AccountNumber[i]);
                        }
                    }
                    else
                    {
                        var currentAccountNumber = viewModel.AccountNumber[i];
                        var accountTitle = await _unitOfWork.FilprideChartOfAccount
                            .GetAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken);
                        // Add new record
                        var newDetails = new FilprideJournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle!.AccountName,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            TransactionNo = viewModel.JVNo!,
                            JournalVoucherHeaderId = viewModel.JVId
                        };
                        _dbContext.Add(newDetails);
                    }
                }

                // Remove remaining records that were duplicates
                foreach (var ids in accountTitleDict.Values)
                {
                    foreach (var id in ids)
                    {
                        var details = existingDetailsModel.First(o => o.JournalVoucherDetailId == id);
                        _dbContext.Remove(details);
                    }
                }

                #endregion --CV Details Entry

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

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Multiple Payment Table Header --

                var worksheet8 = package.Workbook.Worksheets.Add("MultipleCheckVoucherPayments");

                worksheet8.Cells["A1"].Value = "Id";
                worksheet8.Cells["B1"].Value = "CheckVoucherHeaderPaymentId";
                worksheet8.Cells["C1"].Value = "CheckVoucherHeaderInvoiceId";
                worksheet8.Cells["D1"].Value = "AmountPaid";

                #endregion

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
                    worksheet.Cells[row, 7].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 8].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 9].Value = item.CVId;
                    worksheet.Cells[row, 10].Value = item.JournalVoucherHeaderNo;
                    worksheet.Cells[row, 11].Value = item.JournalVoucherHeaderId;
                    worksheet.Cells[row, 12].Value = item.PostedBy;
                    worksheet.Cells[row, 13].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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
                    worksheet5.Cells[cvhRow, 10].Value = item.CheckVoucherHeader.StartDate?.ToString("yyyy-MM-dd");
                    worksheet5.Cells[cvhRow, 11].Value = item.CheckVoucherHeader.EndDate?.ToString("yyyy-MM-dd");
                    worksheet5.Cells[cvhRow, 12].Value = item.CheckVoucherHeader.NumberOfMonths;
                    worksheet5.Cells[cvhRow, 13].Value = item.CheckVoucherHeader.NumberOfMonthsCreated;
                    worksheet5.Cells[cvhRow, 14].Value = item.CheckVoucherHeader.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet5.Cells[cvhRow, 15].Value = item.CheckVoucherHeader.AmountPerMonth;
                    worksheet5.Cells[cvhRow, 16].Value = item.CheckVoucherHeader.IsComplete;
                    worksheet5.Cells[cvhRow, 17].Value = item.CheckVoucherHeader.AccruedType;
                    worksheet5.Cells[cvhRow, 18].Value = item.CheckVoucherHeader.Reference;
                    worksheet5.Cells[cvhRow, 19].Value = item.CheckVoucherHeader.CreatedBy;
                    worksheet5.Cells[cvhRow, 20].Value = item.CheckVoucherHeader.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
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
                    worksheet5.Cells[cvhRow, 33].Value = item.CheckVoucherHeader.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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

                #endregion

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

                #endregion

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
                    worksheet6.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet6.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet6.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet6.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Trade and Invoicing) --

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
                    worksheet4.Cells[rrRow, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet4.Cells[rrRow, 15].Value = item.CanceledQuantity;
                    worksheet4.Cells[rrRow, 16].Value = item.CreatedBy;
                    worksheet4.Cells[rrRow, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet4.Cells[rrRow, 18].Value = item.CancellationRemarks;
                    worksheet4.Cells[rrRow, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                    worksheet4.Cells[rrRow, 20].Value = item.POId;
                    worksheet4.Cells[rrRow, 21].Value = item.ReceivingReportNo;
                    worksheet4.Cells[rrRow, 22].Value = item.ReceivingReportId;
                    worksheet4.Cells[rrRow, 23].Value = item.PostedBy;
                    worksheet4.Cells[rrRow, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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
                    worksheet3.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[poRow, 13].Value = item.IsClosed;
                    worksheet3.Cells[poRow, 14].Value = item.CancellationRemarks;
                    worksheet3.Cells[poRow, 15].Value = item.ProductId;
                    worksheet3.Cells[poRow, 16].Value = item.PurchaseOrderNo;
                    worksheet3.Cells[poRow, 17].Value = item.SupplierId;
                    worksheet3.Cells[poRow, 18].Value = item.PurchaseOrderId;
                    worksheet3.Cells[poRow, 19].Value = item.PostedBy;
                    worksheet3.Cells[poRow, 20].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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
    }
}
