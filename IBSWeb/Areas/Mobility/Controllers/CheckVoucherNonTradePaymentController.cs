using System.Linq.Dynamic.Core;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
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

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class CheckVoucherNonTradePaymentController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradePaymentController> _logger;

        public CheckVoucherNonTradePaymentController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradePaymentController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _cloudStorageService = cloudStorageService;
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

        private string? GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}{extension}";
        }

        private async Task GenerateSignedUrl(MobilityCheckVoucherHeader model)
        {
            // Get Signed URL only when Saved File Name is available.
            if (!string.IsNullOrWhiteSpace(model.SupportingFileSavedFileName))
            {
                model.SupportingFileSavedUrl = await _cloudStorageService.GetSignedUrlAsync(model.SupportingFileSavedFileName);
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPaymentCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var checkVoucherDetails = await _dbContext.MobilityCheckVoucherDetails
                                                        .Where(cvd => cvd.CheckVoucherHeader!.StationCode == stationCodeClaims && cvd.CheckVoucherHeader.CvType == nameof(CVType.Payment) && cvd.AccountName.StartsWith("Cash in Bank"))
                                                        .Include(cvd => cvd.CheckVoucherHeader)
                                                        .ThenInclude(cvh => cvh!.Supplier)
                                                        .Include(cvd => cvd.Supplier)
                                                        .ToListAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherDetails = checkVoucherDetails
                    .Where(s =>
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo?.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Total.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Payee?.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Date.ToString(SD.Date_Format).Contains(searchValue) == true ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.AmountPaid.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeader?.Reference?.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader!.Status.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    checkVoucherDetails = checkVoucherDetails
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = checkVoucherDetails.Count();

                var pagedData = checkVoucherDetails
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
            if (id == null)
            {
                return NotFound();
            }

            var header = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    modelHeader.PostedBy = _userManager.GetUserName(this.User);
                    modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    modelHeader.Status = nameof(CheckVoucherPaymentStatus.Posted);

                    ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
                    #region --General Ledger Book Recording(CV)--

                    // var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                    // var ledgers = new List<FilprideGeneralLedgerBook>();
                    // foreach (var details in modelDetails)
                    // {
                    //     var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account title '{details.AccountNo}' not found.");
                    //     ledgers.Add(
                    //             new FilprideGeneralLedgerBook
                    //             {
                    //                 Date = modelHeader.Date,
                    //                 Reference = modelHeader.CheckVoucherHeaderNo,
                    //                 Description = modelHeader.Particulars,
                    //                 AccountId = account.AccountId,
                    //                 AccountNo = account.AccountNumber,
                    //                 AccountTitle = account.AccountName,
                    //                 Debit = details.Debit,
                    //                 Credit = details.Credit,
                    //                 Company = modelHeader.Company,
                    //                 CreatedBy = modelHeader.CreatedBy,
                    //                 CreatedDate = modelHeader.CreatedDate,
                    //                 BankAccountId = details.BankId,
                    //                 SupplierId = details.SupplierId,
                    //                 CustomerId = details.CustomerId,
                    //                 EmployeeId = details.EmployeeId,
                    //                 CompanyId = details.CompanyId,
                    //             }
                    //         );
                    // }
                    //
                    // if (!_unitOfWork.FilprideCheckVoucher.IsJournalEntriesBalanced(ledgers))
                    // {
                    //     throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                    // }
                    //
                    // await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                    #endregion --General Ledger Book Recording(CV)--

                    #region --Disbursement Book Recording(CV)--

                    // var disbursement = new List<FilprideDisbursementBook>();
                    // foreach (var details in modelDetails)
                    // {
                    //     var bank = _dbContext.FilprideBankAccounts.FirstOrDefault(model => model.BankAccountId == modelHeader.BankId);
                    //     disbursement.Add(
                    //             new FilprideDisbursementBook
                    //             {
                    //                 Date = modelHeader.Date,
                    //                 CVNo = modelHeader.CheckVoucherHeaderNo,
                    //                 Payee = modelHeader.Payee,
                    //                 Amount = modelHeader.Total,
                    //                 Particulars = modelHeader.Particulars,
                    //                 Bank = bank != null ? bank.Branch : "N/A",
                    //                 CheckNo = modelHeader.CheckNo,
                    //                 CheckDate = modelHeader.CheckDate?.ToString("MM/dd/yyyy"),
                    //                 ChartOfAccount = details.AccountNo + " " + details.AccountName,
                    //                 Debit = details.Debit,
                    //                 Credit = details.Credit,
                    //                 Company = modelHeader.Company,
                    //                 CreatedBy = modelHeader.CreatedBy,
                    //                 CreatedDate = modelHeader.CreatedDate
                    //             }
                    //         );
                    // }
                    //
                    // await _dbContext.FilprideDisbursementBooks.AddRangeAsync(disbursement, cancellationToken);

                    #endregion --Disbursement Book Recording(CV)--

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy!, $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    var updateMultipleInvoicingVoucher = await _dbContext.MobilityMultipleCheckVoucherPayments
                        .Where(mcvp => mcvp.CheckVoucherHeaderPaymentId == id)
                        .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                        .ToListAsync(cancellationToken);

                    for (int j = 0; j < updateMultipleInvoicingVoucher.Count; j++)
                    {
                        if (updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice!.IsPaid)
                        {
                            updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice!.Status = nameof(CheckVoucherInvoiceStatus.Paid);
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
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

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (existingHeaderModel != null)
                {
                    existingHeaderModel.CanceledBy = User.Identity!.Name;
                    existingHeaderModel.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingHeaderModel.Status = nameof(CheckVoucherPaymentStatus.Canceled);
                    existingHeaderModel.CancellationRemarks = cancellationRemarks;

                    var IsForTheBir = existingHeaderModel.SupplierId == 133; //BIR

                    var getCVs = await _dbContext.MobilityMultipleCheckVoucherPayments
                        .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                        .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                        .Include(cvp => cvp.CheckVoucherHeaderPayment)
                        .ToListAsync(cancellationToken);

                    if (IsForTheBir)
                    {
                        foreach (var cv in getCVs)
                        {
                            var existingDetails = await _dbContext.MobilityCheckVoucherDetails
                                .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                            d.SupplierId == existingHeaderModel.SupplierId)
                                .ToListAsync(cancellationToken);

                            foreach (var existingDetail in existingDetails)
                            {
                                existingDetail.AmountPaid = 0;
                            }

                        }
                    }
                    else
                    {
                        foreach (var cv in getCVs)
                        {
                            cv.CheckVoucherHeaderInvoice!.AmountPaid -= cv.AmountPaid;
                            cv.CheckVoucherHeaderInvoice.IsPaid = false;
                        }
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.CanceledBy!, $"Canceled check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Cancelled.";

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var IsForTheBir = existingHeaderModel.SupplierId == 133; //BIR

                    var getCVs = await _dbContext.MobilityMultipleCheckVoucherPayments
                        .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                        .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                        .Include(cvp => cvp.CheckVoucherHeaderPayment)
                        .ToListAsync(cancellationToken);

                    if (IsForTheBir)
                    {
                        foreach (var cv in getCVs)
                        {
                            var existingDetails = await _dbContext.MobilityCheckVoucherDetails
                                .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                            d.SupplierId == existingHeaderModel.SupplierId)
                                .ToListAsync(cancellationToken);

                            foreach (var existingDetail in existingDetails)
                            {
                                existingDetail.AmountPaid = 0;
                            }

                        }
                    }
                    else
                    {
                        foreach (var cv in getCVs)
                        {
                            cv.CheckVoucherHeaderInvoice!.AmountPaid -= cv.AmountPaid;
                            cv.CheckVoucherHeaderInvoice.IsPaid = false;
                            cv.CheckVoucherHeaderInvoice.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);
                        }
                    }

                    existingHeaderModel.PostedBy = null;
                    existingHeaderModel.VoidedBy = User.Identity!.Name;
                    existingHeaderModel.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingHeaderModel.Status = nameof(CheckVoucherPaymentStatus.Voided);

                    ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
                    //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == existingHeaderModel.CheckVoucherHeaderNo);
                    //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == existingHeaderModel.CheckVoucherHeaderNo);

                    //re-compute amount paid in trade and payment voucher
                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.VoidedBy!, $"Voided check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
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

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .Include(cvd => cvd.Supplier)
                .FirstOrDefaultAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var checkVoucher = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeader!.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) && cvd.CheckVoucherHeader.StationCode == stationCodeClaims ||
                              cvd.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) && cvd.CheckVoucherHeaderId == cvd.CheckVoucherHeader.CheckVoucherHeaderId && cvd.CheckVoucherHeader.StationCode == stationCodeClaims)
                .Include(cvd => cvd.CheckVoucherHeader)
                .OrderBy(cvd => cvd.CheckVoucherDetailId)
                .Select(cvd => new SelectListItem
                {
                    Value = cvd.CheckVoucherHeaderId.ToString(),
                    Text = cvd.CheckVoucherHeader!.CheckVoucherHeaderNo
                })
                .Distinct()
                .ToListAsync();

            var suppliers = await _dbContext.MobilitySuppliers
                .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.SupplierId.ToString(),
                    Text = cvh.SupplierName
                })
                .ToListAsync();

            var bankAccounts = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            var getCVs = await _dbContext.MobilityMultipleCheckVoucherPayments
                .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                .Select(cvp => cvp.CheckVoucherHeaderInvoiceId)
                .ToListAsync(cancellationToken);

            //for trim the system generated invoice reference to payment
            string particulars = existingHeaderModel.Particulars ?? "";
            int index = particulars.IndexOf("Payment for");

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
                Payee = existingHeaderModel.SupplierId != null ? existingHeaderModel.Supplier!.SupplierName : existingDetailsModel.Supplier!.SupplierName,
                PayeeAddress = existingHeaderModel.Address,
                PayeeTin = existingHeaderModel.Tin,
                MultipleSupplierId = existingHeaderModel.SupplierId != null ? existingHeaderModel.SupplierId : existingDetailsModel.SupplierId,
                Suppliers = suppliers,
                CvId = existingHeaderModel.CheckVoucherHeaderId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.SupplierId.ToString(),
                        Text = cvh.SupplierName
                    })
                    .ToListAsync();
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region--Get Check Voucher Invoicing

                var existingHeaderModel = await _dbContext.MobilityCheckVoucherHeaders
                    .Include(cv => cv.Supplier)
                    .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var getCVs = await _dbContext.MobilityMultipleCheckVoucherPayments
                    .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                    .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                    .Include(cvp => cvp.CheckVoucherHeaderPayment)
                    .ToListAsync(cancellationToken);

                if (existingHeaderModel.SupplierId == 133)//BIR
                {
                    foreach (var cv in getCVs)
                    {
                        var existingDetails = await _dbContext.MobilityCheckVoucherDetails
                            .Where(d => d.CheckVoucherHeaderId == cv.CheckVoucherHeaderInvoiceId &&
                                        d.SupplierId == existingHeaderModel.SupplierId)
                            .ToListAsync(cancellationToken);

                        foreach (var existingDetail in existingDetails)
                        {
                            existingDetail.AmountPaid = 0;
                        }

                    }
                }

                var invoicingVoucher = await _dbContext.MobilityCheckVoucherHeaders
                    .Where(cv => viewModel.MultipleCvId!.Contains(cv.CheckVoucherHeaderId))
                    .OrderBy(cv => cv.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                bool isForTheBir = false;

                foreach (var invoice in invoicingVoucher)
                {
                    var cv = viewModel.PaymentDetails.FirstOrDefault(c => c.CVId == invoice.CheckVoucherHeaderId);

                    var getCVDetails = await _dbContext.MobilityCheckVoucherDetails
                        .Where(i => cv!.CVId == i.CheckVoucherHeaderId &&
                                    i.SupplierId != null &&
                                    i.SupplierId == viewModel.MultipleSupplierId &&
                                    i.CheckVoucherHeader!.CvType == nameof(CVType.Invoicing))
                        .OrderBy(i => i.CheckVoucherHeaderId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (getCVDetails != null && getCVDetails.CheckVoucherHeaderId == cv!.CVId)
                    {
                        getCVDetails.AmountPaid += cv.AmountPaid;
                        isForTheBir = getCVDetails.SupplierId == 133 && !getCVDetails.IsUserSelected; //BIR Supplier Id
                    }
                }

                #endregion

                #region -- Saving the default entries --

                #region -- Check Voucher Header --

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.PONo = invoicingVoucher.Select(i => i.PONo).FirstOrDefault();
                existingHeaderModel.SINo = invoicingVoucher.Select(i => i.SINo).FirstOrDefault();
                existingHeaderModel.SupplierId = viewModel.MultipleSupplierId;
                existingHeaderModel.Particulars = $"{viewModel.Particulars} Payment for {string.Join(",", invoicingVoucher.Select(i => i.CheckVoucherHeaderNo))}";
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
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

                await _dbContext.SaveChangesAsync(cancellationToken);

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
                    MobilityMultipleCheckVoucherPayment multipleCheckVoucherPayment = new()
                    {
                        Id = Guid.NewGuid(),
                        CheckVoucherHeaderPaymentId = existingHeaderModel.CheckVoucherHeaderId,
                        CheckVoucherHeaderInvoiceId = paymentDetail.CVId,
                        AmountPaid = paymentDetail.AmountPaid,
                    };

                    _dbContext.Add(multipleCheckVoucherPayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #region--Update invoicing voucher

                var updateMultipleInvoicingVoucher = await _dbContext.MobilityMultipleCheckVoucherPayments
                    .Where(mcvp => viewModel.MultipleCvId!.Contains(mcvp.CheckVoucherHeaderInvoiceId) && mcvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
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

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var details = new List<MobilityCheckVoucherDetail>();

                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    details.Add(new MobilityCheckVoucherDetail
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

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(details, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

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

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher payment edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.SupplierId.ToString(),
                        Text = cvh.SupplierName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit check voucher non trade payment. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMultipleSupplierDetails(int cvId, int suppId, CancellationToken cancellationToken)
        {
            var supplier = await _dbContext.MobilitySuppliers
                    .FindAsync(suppId, cancellationToken);

            var credit = await _dbContext.MobilityCheckVoucherDetails
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
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var cv = await _dbContext.MobilityCheckVoucherHeaders
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(c => c.CheckVoucherHeaderId == cvId, cancellationToken);

            // Ensure that cv is not null before proceeding
            if (cv == null)
            {
                return Json(null);
            }

            // Retrieve the list of supplier IDs from the check voucher details
            var supplierIds = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == cv.CheckVoucherHeaderNo)
                .Select(cvd => cvd.SupplierId)
                .ToListAsync(cancellationToken);

            // Fetch suppliers whose IDs are in the supplierIds list
            var suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supplierIds.Contains(supp.SupplierId) && supp.StationCode == stationCodeClaims)
                .OrderBy(supp => supp.SupplierId)
                .Select(supp => new SelectListItem
                {
                    Value = supp.SupplierId.ToString(),
                    Text = supp.SupplierName
                })
                .ToListAsync(cancellationToken);

            return Json(new
            {
                SupplierList = suppliers
            });
        }

        [HttpGet]
        public async Task<JsonResult> MultipleSupplierDetails(int suppId, int cvId, CancellationToken cancellationToken)
        {
            var supplier = await _dbContext.MobilitySuppliers
                    .FindAsync(suppId, cancellationToken);

            var credit = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.SupplierId == suppId && cvd.CheckVoucherHeaderId == cvId)
                .Include(cvd => cvd.CheckVoucherHeader)
                .Select(cvd => new
                {
                    RemainingCredit = cvd.Credit - cvd.AmountPaid,
                    Particulars = cvd.CheckVoucherHeader!.Particulars
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

            var invoice = await _dbContext.MobilityCheckVoucherHeaders
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.CheckVoucherHeaderId == invoiceId, cancellationToken);

            if (invoice != null)
            {
                return Json(new
                {
                    Payee = invoice.Supplier!.SupplierName,
                    PayeeAddress = invoice.Supplier.SupplierAddress,
                    PayeeTin = invoice.Supplier.SupplierTin,
                    invoice.Particulars,
                    Total = invoice.InvoiceAmount
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.MobilitySuppliers
                .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.SupplierId.ToString(),
                    Text = cvh.SupplierName
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                TempData["error"] = "The submitted information is invalid.";
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.SupplierId.ToString(),
                        Text = cvh.SupplierName
                    })
                    .ToListAsync();
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region--Get Check Voucher Invoicing

                bool isForTheBir = false;
                var invoicingVoucher = await _dbContext.MobilityCheckVoucherHeaders
                    .Where(cv => viewModel.MultipleCvId!.Contains(cv.CheckVoucherHeaderId))
                    .OrderBy(cv => cv.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                foreach (var invoice in invoicingVoucher)
                {
                    var cv = viewModel.PaymentDetails.FirstOrDefault(c => c.CVId == invoice.CheckVoucherHeaderId);

                    if (cv == null)
                    {
                        return NotFound();
                    }

                    var getCVDetails = await _dbContext.MobilityCheckVoucherDetails
                        .Where(i => cv.CVId == i.CheckVoucherHeaderId &&
                                    i.SupplierId != null &&
                                    i.SupplierId == viewModel.MultipleSupplierId &&
                                    i.CheckVoucherHeader!.CvType == nameof(CVType.Invoicing))
                        .OrderBy(i => i.CheckVoucherHeaderId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (getCVDetails != null && getCVDetails.CheckVoucherHeaderId == cv.CVId)
                    {
                        getCVDetails.AmountPaid += cv.AmountPaid;
                        isForTheBir = getCVDetails.SupplierId == 133 && !getCVDetails.IsUserSelected; //BIR Supplier Id
                    }
                }

                #endregion

                #region -- Saving the default entries --

                #region -- Check Voucher Header --

                MobilityCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeMultiplePaymentAsync(stationCodeClaims, invoicingVoucher.Select(i => i.Type).FirstOrDefault() ?? throw new InvalidOperationException(), cancellationToken),
                    Date = viewModel.TransactionDate,
                    PONo = invoicingVoucher.Select(i => i.PONo).FirstOrDefault(),
                    SINo = invoicingVoucher.Select(i => i.SINo).FirstOrDefault(),
                    SupplierId = viewModel.MultipleSupplierId,
                    Particulars = $"{viewModel.Particulars}. Payment for {string.Join(",", invoicingVoucher.Select(i => i.CheckVoucherHeaderNo))}",
                    Total = viewModel.Total,
                    CreatedBy = _userManager.GetUserName(this.User),
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
                    StationCode = stationCodeClaims,
                    Type = invoicingVoucher.Select(i => i.Type).First()
                };

                await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- Check Voucher Header --

                #region -- Multiple Payment Storing --

                foreach (var paymentDetail in viewModel.PaymentDetails)
                {
                    MobilityMultipleCheckVoucherPayment multipleCheckVoucherPayment = new()
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

                var updateMultipleInvoicingVoucher = await _dbContext.MobilityMultipleCheckVoucherPayments
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

                List<MobilityCheckVoucherDetail> checkVoucherDetails = new();

                for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                    {
                        checkVoucherDetails.Add(new MobilityCheckVoucherDetail
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

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

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

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment created successfully. Series Number: {checkVoucherHeader.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(cvh => cvh.StationCode == stationCodeClaims && cvh.Category == "Non-Trade")
                    .Select(cvh => new SelectListItem
                    {
                        Value = cvh.SupplierId.ToString(),
                        Text = cvh.SupplierName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create check voucher payment. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            if (supplierId != null)
            {
                var supplier = await _dbContext.MobilitySuppliers
                    .FindAsync(supplierId);

                if (supplier != null)
                {
                    return Json(new
                    {
                        Name = supplier.SupplierName,
                        Address = supplier.SupplierAddress,
                        TinNo = supplier.SupplierTin,
                        TaxType = supplier.TaxType,
                        Category = supplier.Category,
                        TaxPercent = supplier.WithholdingTaxPercent,
                        VatType = supplier.VatType,
                        DefaultExpense = supplier.DefaultExpenseNumber,
                        WithholdingTax = supplier.WithholdingTaxtitle
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        public async Task<IActionResult> GetCVs(int supplierId, int? paymentId, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.MobilityCheckVoucherDetails
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
                             cvd.Credit > cvd.AmountPaid)
                        ));

                if (paymentId != null)
                {
                    var existingInvoiceIds = await _dbContext.MobilityMultipleCheckVoucherPayments
                        .Where(m => m.CheckVoucherHeaderPaymentId == paymentId)
                        .Select(m => m.CheckVoucherHeaderInvoiceId)
                        .ToListAsync(cancellationToken);

                    // Include existing records in the query
                    query = query.Union(_dbContext.MobilityCheckVoucherDetails
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
            if (cvId == null)
            {
                return Json(null);
            }

            var invoices = await _dbContext.MobilityCheckVoucherDetails
                .Where(i =>
                    cvId.Contains(i.CheckVoucherHeaderId) &&
                    i.SupplierId == supplierId)
                .Include(i => i.Supplier)
                .Include(i => i.CheckVoucherHeader)
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
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.Employees = await _dbContext.MobilityStationEmployees
                .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                })
                .ToListAsync(cancellationToken);

            viewModel.Banks = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdvancesToEmployee(AdvancesToEmployeeViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Employees = await _dbContext.MobilityStationEmployees
                    .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmployeeId.ToString(),
                        Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region Save Record

                #region Header

                MobilityCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeMultiplePaymentAsync(stationCodeClaims, viewModel.DocumentType!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Particulars = viewModel.Particulars,
                    PONo = [],
                    SINo = [],
                    Total = viewModel.Total,
                    CreatedBy = _userManager.GetUserName(this.User),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Payment),
                    BankId = viewModel.BankId,
                    Payee = viewModel.Payee,
                    Address = viewModel.PayeeAddress,
                    Tin = viewModel.PayeeTin,
                    CheckNo = viewModel.CheckNo,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.Total,
                    StationCode = stationCodeClaims,
                    Type = viewModel.DocumentType,
                    IsAdvances = true,
                    EmployeeId = viewModel.EmployeeId,
                };

                await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion

                #region Details

                var accountTitlesDto = await _unitOfWork.MobilityCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToOfficerTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020400") ?? throw new ArgumentException($"Account title '101020400' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException($"Account title '101010100' not found.");

                var checkVoucherDetails = new List<MobilityCheckVoucherDetail>
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

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment created successfully. Series Number: {checkVoucherHeader.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Employees = await _dbContext.MobilityStationEmployees
                    .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmployeeId.ToString(),
                        Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create advances to employee. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
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

            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var employees = await _dbContext.MobilityStationEmployees
                .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                })
                .ToListAsync(cancellationToken);

            var bankAccounts = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

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
                Particulars = existingHeaderModel.Particulars!
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdvancesToEmployee(AdvancesToEmployeeViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel.Employees = await _dbContext.MobilityStationEmployees
                    .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmployeeId.ToString(),
                        Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _dbContext.MobilityCheckVoucherHeaders
                        .FindAsync(viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                #region Update Record

                #region Header

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.Address = viewModel.PayeeAddress;
                existingHeaderModel.Tin = viewModel.PayeeTin;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.CheckAmount = viewModel.Total;
                existingHeaderModel.EmployeeId = viewModel.EmployeeId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion Header

                #region Details

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var details = new List<MobilityCheckVoucherDetail>();

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToOfficerTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020400") ?? throw new ArgumentException($"Account title '101020400' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException($"Account title '101010100' not found.");

                var checkVoucherDetails = new List<MobilityCheckVoucherDetail>
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

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher payment edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Employees = await _dbContext.MobilityStationEmployees
                    .Where(e => e.IsActive && e.StationCode == stationCodeClaims)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmployeeId.ToString(),
                        Text = $"{e.EmployeeNumber} - {e.FirstName} {e.LastName}"
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit advances to employee. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }


        public async Task<IActionResult> GetEmployeeDetails(int? employeeId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (employeeId == null)
            {
                return Json(null);
            }

            var employee = await _dbContext.MobilityStationEmployees
                .Where(e => e.StationCode == stationCodeClaims)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.StationCode == stationCodeClaims);

            if (employee == null)
            {
                return Json(null);
            }

            return Json(new
            {
                Name = $"{employee.FirstName} {employee.LastName}",
                Address = employee.Address,
                TinNo = employee.TinNo,
            });

        }

        [HttpGet]
        public async Task<IActionResult> CreateAdvancesToSupplier(CancellationToken cancellationToken)
        {
            var viewModel = new AdvancesToSupplierViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(stationCodeClaims, cancellationToken);

            viewModel.Banks = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdvancesToSupplier(AdvancesToSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(stationCodeClaims, cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region Save Record

                #region Header

                MobilityCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeMultiplePaymentAsync(stationCodeClaims, viewModel.DocumentType!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Particulars = viewModel.Particulars,
                    PONo = [],
                    SINo = [],
                    Total = viewModel.Total,
                    CreatedBy = _userManager.GetUserName(this.User),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Payment),
                    BankId = viewModel.BankId,
                    Payee = viewModel.Payee,
                    Address = viewModel.PayeeAddress,
                    Tin = viewModel.PayeeTin,
                    CheckNo = viewModel.CheckNo,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.Total,
                    StationCode = stationCodeClaims,
                    Type = viewModel.DocumentType,
                    IsAdvances = true,
                    SupplierId = viewModel.SupplierId,
                };

                await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion

                #region Details

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToSupplierTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060100") ?? throw new ArgumentException("Account title '101060100' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

                var grossAmount = viewModel.Total;
                var ewtAmount = _unitOfWork.MobilityCheckVoucher.ComputeEwtAmount(grossAmount, 0.01m);
                var netOfEwtAmount = _unitOfWork.MobilityCheckVoucher.ComputeNetOfEwt(grossAmount, ewtAmount);

                var checkVoucherDetails = new List<MobilityCheckVoucherDetail>
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

                FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy!, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment created successfully. Series Number: {checkVoucherHeader.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Suppliers = await _unitOfWork.GetMobilitySupplierListAsyncById(stationCodeClaims, cancellationToken);

                viewModel.Banks = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create advances to supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
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

            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var supplier = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

            var bankAccounts = await _dbContext.MobilityBankAccounts
                .Where(ba => ba.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

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
                Particulars = existingHeaderModel.Particulars!
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdvancesToSupplier(AdvancesToSupplierViewModel viewModel, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var supplier = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

                var bankAccounts = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingHeaderModel = await _dbContext.MobilityCheckVoucherHeaders
                        .FindAsync(viewModel.CvId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                #region Update Record

                #region Header

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;
                existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.Address = viewModel.PayeeAddress;
                existingHeaderModel.Tin = viewModel.PayeeTin;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.CheckAmount = viewModel.Total;
                existingHeaderModel.SupplierId = viewModel.SupplierId;

                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion Header

                #region Details

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var advancesToSupplierTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060100") ?? throw new ArgumentException("Account title '101060100' not found.");
                var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
                var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");

                var grossAmount = viewModel.Total;
                var ewtAmount = _unitOfWork.MobilityCheckVoucher.ComputeEwtAmount(grossAmount, 0.01m);
                var netOfEwtAmount = _unitOfWork.MobilityCheckVoucher.ComputeNetOfEwt(grossAmount, ewtAmount);

                var checkVoucherDetails = new List<MobilityCheckVoucherDetail>
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

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Check voucher payment edited successfully. Series Number: {existingHeaderModel.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var supplier = await _unitOfWork.MobilitySupplier.GetMobilityTradeSupplierListAsyncById(stationCodeClaims, cancellationToken);

                var bankAccounts = await _dbContext.MobilityBankAccounts
                    .Where(ba => ba.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit advances to supplier. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

    }
}
