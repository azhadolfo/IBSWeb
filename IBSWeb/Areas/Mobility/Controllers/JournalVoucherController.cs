using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<JournalVoucherController> _logger;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, ILogger<JournalVoucherController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetJournalVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var journalVoucherHeader = await _unitOfWork.MobilityJournalVoucher
                    .GetAllAsync(jv => jv.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    journalVoucherHeader = journalVoucherHeader
                    .Where(s =>
                        s.JournalVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.References?.Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo?.Contains(searchValue) == true ||
                        s.Particulars.ToLower().Contains(searchValue) ||
                        s.CRNo?.ToLower().Contains(searchValue) == true ||
                        s.JVReason.ToLower().ToString().Contains(searchValue) ||
                        s.CreatedBy!.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
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
            JournalVoucherViewModel viewModel = new();

            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Where(c => c.StationCode == stationCodeClaims &&
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
        public async Task<IActionResult> Create(JournalVoucherViewModel? viewModel, CancellationToken cancellationToken, string[] accountNumber, decimal[]? debit, decimal[]? credit)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel!.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

                viewModel.CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.StationCode == stationCodeClaims)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                    var totalDebit = 0m;
                    var totalCredit = 0m;
                    if (totalDebit != totalCredit)
                    {
                        TempData["error"] = "The debit and credit should be equal!";
                        return View(viewModel);
                    }

                    var generateJVNo = await _unitOfWork.MobilityJournalVoucher.GenerateCodeAsync(stationCodeClaims, viewModel!.Type, cancellationToken);

                    var model = new MobilityJournalVoucherHeader
                    {
                        JournalVoucherHeaderNo = generateJVNo,
                        Date = viewModel.TransactionDate,
                        References = viewModel.References,
                        CVId = viewModel.CheckVoucherHeaderId,
                        Particulars = viewModel.Particulars,
                        CRNo = viewModel.CRNo,
                        JVReason = viewModel.JVReason,
                        CreatedBy = User.Identity!.Name,
                        CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                        Type = viewModel.Type,
                        StationCode = stationCodeClaims,
                    };

                    await _dbContext.MobilityJournalVoucherHeaders.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region --CV Details Entry

                var jvDetails = new List<MobilityJournalVoucherDetail>();
                for (int i = 0; i < accountNumber.Length; i++)
                {
                    var currentAccountNumber = accountNumber[i];
                    var accountTitle = await _dbContext.FilprideChartOfAccounts
                        .FirstOrDefaultAsync(coa => coa.AccountNumber == currentAccountNumber);

                    if (accountTitle == null)
                    {
                        return NotFound();
                    }

                    var currentDebit = debit![i];
                    var currentCredit = credit![i];
                    totalDebit += debit[i];
                    totalCredit += credit[i];

                    jvDetails.Add(
                        new MobilityJournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle.AccountName,
                            TransactionNo = generateJVNo,
                            JournalVoucherHeaderId = model.JournalVoucherHeaderId,
                            Debit = currentDebit,
                            Credit = currentCredit
                        }
                    );
                }
                await _dbContext.AddRangeAsync(jvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Created new journal voucher# {viewModel.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Journal voucher created successfully. Series Number: {model.JournalVoucherHeaderNo}.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel!.COA = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

                viewModel.CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.StationCode == stationCodeClaims)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create journal voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetCV(int id)
        {
            var header = _dbContext.MobilityCheckVoucherHeaders
                .Include(s => s.Employee)
                .Include(s => s.Supplier)
                .FirstOrDefault(cvh => cvh.CheckVoucherHeaderId == id);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync();

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            if (viewModel != null)
            {
                var cvNo = viewModel.Header.CheckVoucherHeaderNo;
                var date = viewModel.Header.Date;
                var name = viewModel.Header.Payee;
                var address = viewModel.Header.Address;
                var tinNo = viewModel.Header.Tin;
                var poNo = viewModel.Header.PONo;
                var siNo = viewModel.Header.SINo;
                var payee = viewModel.Header.Payee;
                var amount = viewModel.Header.Total;
                var particulars = viewModel.Header.Particulars;
                var checkNo = viewModel.Header.CheckNo;
                var totalDebit = viewModel.Details.Select(cvd => cvd.Debit).Sum();
                var totalCredit = viewModel.Details.Select(cvd => cvd.Credit).Sum();

                return Json(new
                {
                    CVNo = cvNo,
                    Date = date,
                    Name = name,
                    Address = address,
                    TinNo = tinNo,
                    PONo = poNo,
                    SINo = siNo,
                    Payee = payee,
                    Amount = amount,
                    Particulars = particulars,
                    CheckNo = checkNo,
                    ViewModel = viewModel,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
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

            var header = await _unitOfWork.MobilityJournalVoucher
                .GetAsync(jvh => jvh.JournalVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.MobilityJournalVoucherDetails
                .Where(jvd => jvd.JournalVoucherHeaderId == header.JournalVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var viewModel = new JournalVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {

            var modelHeader = await _unitOfWork.MobilityJournalVoucher
                .GetAsync(jv => jv.JournalVoucherHeaderId == id, cancellationToken);

            var modelDetails = await _dbContext.MobilityJournalVoucherDetails
                .Where(jvd => jvd.JournalVoucherHeaderId == modelHeader.JournalVoucherHeaderId)
                .ToListAsync();

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (modelHeader.PostedBy == null)
                    {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        modelHeader.Status = nameof(Status.Posted);

                        #region --General Ledger Book Recording(GL)--

                        var accountTitlesDto = await _unitOfWork.MobilityCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
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
                                        Company = nameof(Mobility),
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
                                        Company = nameof(Mobility),
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        await _dbContext.FilprideJournalBooks.AddRangeAsync(journalBook, cancellationToken);

                        #endregion --Journal Book Recording(JV)--

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy!, $"Posted journal voucher# {modelHeader.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Posted.";
                    }
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

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {

            var model = await _unitOfWork.MobilityJournalVoucher.GetAsync(jv => jv.JournalVoucherHeaderId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideJournalBook>(crb => crb.Reference == model.JournalVoucherHeaderNo);
                        await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.JournalVoucherHeaderNo);

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Voided.";
                        return RedirectToAction(nameof(Index));
                    }
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

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {

            var model = await _unitOfWork.MobilityJournalVoucher.GetAsync(jv => jv.JournalVoucherHeaderId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel journal vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingHeaderModel = await _unitOfWork.MobilityJournalVoucher
                .GetAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);
            var existingDetailsModel = await _dbContext.MobilityJournalVoucherDetails
                .Where(cvd => cvd.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                .ToListAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            JournalVoucherViewModel model = new()
            {
                JournalVoucherHeaderId = existingHeaderModel.JournalVoucherHeaderId,
                JournalVoucherHeaderNo = existingHeaderModel.JournalVoucherHeaderNo,
                TransactionDate = existingHeaderModel.Date,
                References = existingHeaderModel.References,
                CheckVoucherHeaderId = existingHeaderModel.CVId,
                Particulars = existingHeaderModel.Particulars,
                CRNo = existingHeaderModel.CRNo,
                JVReason = existingHeaderModel.JVReason,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.StationCode == stationCodeClaims &&
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
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (!ModelState.IsValid)
            {
                viewModel.CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.StationCode == stationCodeClaims &&
                                c.CvType == nameof(CVType.Payment) &&
                                c.PostedBy != null)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingHeaderModel = await _unitOfWork.MobilityJournalVoucher
                .GetAsync(jv => jv.JournalVoucherHeaderId == viewModel.JournalVoucherHeaderId, cancellationToken);

            var existingDetailsModel = await _dbContext.MobilityJournalVoucherDetails
                .Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId)
                .ToListAsync();

            try
            {
                #region --Saving the default entries

                    existingHeaderModel.JournalVoucherHeaderNo = viewModel.JournalVoucherHeaderNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.References = viewModel.References;
                    existingHeaderModel.CVId = viewModel.CheckVoucherHeaderId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.CRNo = viewModel.CRNo;
                    existingHeaderModel.JVReason = viewModel.JVReason;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #endregion --Saving the default entries

                #region --CV Details Entry

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var details = new List<MobilityJournalVoucherDetail>();

                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    details.Add(new MobilityJournalVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        TransactionNo = viewModel.JournalVoucherHeaderNo!,
                        JournalVoucherHeaderId = viewModel.JournalVoucherHeaderId
                    });
                }

                await _dbContext.MobilityJournalVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion --CV Details Entry

                #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Journal Voucher edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.CheckVoucherHeaders = await _dbContext.MobilityCheckVoucherHeaders
                    .OrderBy(c => c.CheckVoucherHeaderId)
                    .Where(c => c.StationCode == stationCodeClaims &&
                                c.CvType == nameof(CVType.Payment) &&
                                c.PostedBy != null)
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.CheckVoucherHeaderId.ToString(),
                        Text = cvh.CheckVoucherHeaderNo
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit journal voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.MobilityJournalVoucher.GetAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);
            if (cv?.IsPrinted == false)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of journal voucher# {cv.JournalVoucherHeaderNo}", "Journal Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}
