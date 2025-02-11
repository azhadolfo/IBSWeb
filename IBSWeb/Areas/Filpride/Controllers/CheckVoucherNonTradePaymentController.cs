using System.Collections;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using IBS.Services;
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
    public class CheckVoucherNonTradePaymentController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ICloudStorageService _cloudStorageService;

        public CheckVoucherNonTradePaymentController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ICloudStorageService cloudStorageService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _cloudStorageService = cloudStorageService;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        private string? GenerateFileNameToSave(string incomingFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(incomingFileName);
            var extension = Path.GetExtension(incomingFileName);
            return $"{fileName}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }

        private async Task GenerateSignedUrl(FilprideCheckVoucherHeader model)
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
        public async Task<IActionResult> GetPaymentCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherDetails = await _dbContext.FilprideCheckVoucherDetails
                                                        .Where(cvd => cvd.CheckVoucherHeader.Company == companyClaims && cvd.CheckVoucherHeader.CvType == nameof(CVType.Payment) && cvd.AccountName.StartsWith("Cash in Bank"))
                                                        .Include(cvd => cvd.CheckVoucherHeader)
                                                        .ThenInclude(cvh => cvh.Supplier)
                                                        .Include(cvd => cvd.Supplier)
                                                        .ToListAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherDetails = checkVoucherDetails
                    .Where(s =>
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo?.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Supplier?.SupplierId.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Total.ToString().Contains(searchValue) == true ||
                        s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.AmountPaid.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeaderId.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Reference?.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.CreatedDate.ToString().Contains(searchValue) == true
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, int? supplierId, CancellationToken cancellationToken)
        {
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

            var getSupplier = await _dbContext.FilprideSuppliers
                .FindAsync(supplierId, cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);
            var modelDetails = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId).ToListAsync();
            var supplierName = await _dbContext.FilprideSuppliers.Where(s => s.SupplierId == supplierId).Select(s => s.SupplierName).FirstOrDefaultAsync(cancellationToken);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        modelHeader.Status = nameof(CheckVoucherPaymentStatus.Posted);

                        #region --General Ledger Book Recording(CV)--

                        var ledgers = new List<FilprideGeneralLedgerBook>();
                        foreach (var details in modelDetails)
                        {
                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.CheckVoucherHeaderNo,
                                        Description = modelHeader.Particulars,
                                        AccountNo = details.AccountNo,
                                        AccountTitle = details.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        Company = modelHeader.Company,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate,
                                        BankAccountId = modelHeader.BankId
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
                            var bank = _dbContext.FilprideBankAccounts.FirstOrDefault(model => model.BankAccountId == modelHeader.BankId);
                            disbursement.Add(
                                    new FilprideDisbursementBook
                                    {
                                        Date = modelHeader.Date,
                                        CVNo = modelHeader.CheckVoucherHeaderNo,
                                        Payee = modelHeader.Payee != null ? modelHeader.Payee : supplierName,
                                        Amount = modelHeader.Total,
                                        Particulars = modelHeader.Particulars,
                                        Bank = bank != null ? bank.Branch : "N/A",
                                        CheckNo = !string.IsNullOrEmpty(modelHeader.CheckNo) ? modelHeader.CheckNo : "N/A",
                                        CheckDate = modelHeader.CheckDate != null ? modelHeader.CheckDate?.ToString("MM/dd/yyyy") : "N/A",
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

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy, $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, modelHeader.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                            .Where(mcvp => mcvp.CheckVoucherHeaderPaymentId == id)
                            .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                            .ToListAsync(cancellationToken);

                        for (int j = 0; j < updateMultipleInvoicingVoucher.Count; j++)
                        {
                            if (updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.IsPaid)
                            {
                                updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.Status = nameof(CheckVoucherInvoiceStatus.Paid);
                            }
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";

                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

            FilprideCheckVoucherDetail getPaymentDetails = new();
            FilprideCheckVoucherHeader getInvoicingReference = new();
            FilprideCheckVoucherDetail getInvoicingDetails = new();

            getPaymentDetails = await _dbContext.FilprideCheckVoucherDetails
                                        .Where(cvd => cvd.SupplierId == model.SupplierId && cvd.CheckVoucherHeaderId == id)
                                        .FirstOrDefaultAsync(cancellationToken);

            getInvoicingReference = await _dbContext.FilprideCheckVoucherHeaders
                                    .Where(cvh => cvh.CheckVoucherHeaderNo == model.Reference)
                                    .FirstOrDefaultAsync(cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(CheckVoucherPaymentStatus.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    if (model.CvType == nameof(CVType.Payment) && getPaymentDetails != null)
                    {
                        getPaymentDetails.AmountPaid -= model.Total;
                        getInvoicingDetails.AmountPaid -= model.Total;
                    }
                    getInvoicingReference.AmountPaid -= model.Total;

                    if (getInvoicingReference.IsPaid)
                    {
                        getInvoicingReference.IsPaid = false;
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Cancelled.";
                }

                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

            FilprideCheckVoucherDetail getPaymentDetails = new();
            FilprideCheckVoucherHeader getInvoicingReference = new();
            FilprideCheckVoucherDetail getInvoicingDetails = new();

            getPaymentDetails = await _dbContext.FilprideCheckVoucherDetails
                                        .Where(cvd => cvd.SupplierId != null && cvd.CheckVoucherHeaderId == id)
                                        .FirstOrDefaultAsync(cancellationToken);

            getInvoicingReference = await _dbContext.FilprideCheckVoucherHeaders
                                    .Where(cvh => cvh.CheckVoucherHeaderNo == model.Reference)
                                    .FirstOrDefaultAsync(cancellationToken);

            if (model.SupplierId != null)
            {
                getInvoicingDetails = await _dbContext.FilprideCheckVoucherDetails
                                    .Where(cvd => cvd.SupplierId != null && cvd.AccountNo == "202010200" && cvd.CheckVoucherHeaderId == getInvoicingReference.CheckVoucherHeaderId)
                                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                getInvoicingDetails = await _dbContext.FilprideCheckVoucherDetails
                                    .Where(cvd => cvd.SupplierId == getPaymentDetails.SupplierId && cvd.CheckVoucherHeaderId == getInvoicingReference.CheckVoucherHeaderId)
                                    .FirstOrDefaultAsync(cancellationToken);
            }

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
                        model.Status = nameof(CheckVoucherPaymentStatus.Voided);

                        if (model.CvType == nameof(CVType.Payment) && getPaymentDetails != null)
                        {
                            getPaymentDetails.AmountPaid -= model.Total;
                            getInvoicingDetails.AmountPaid -= model.Total;
                        }

                        getInvoicingReference.AmountPaid -= model.Total;

                        if (getInvoicingReference.IsPaid)
                        {
                            getInvoicingReference.IsPaid = false;
                        }

                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo);
                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo);

                        //re-compute amount paid in trade and payment voucher
                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Voided.";

                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
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

            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(cvh => cvh.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .Include(cvd => cvd.Supplier)
                .FirstOrDefaultAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var checkVoucher = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeader.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) ||
                              cvd.SupplierId != null && cvd.CheckVoucherHeader.PostedBy != null && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) && cvd.CheckVoucherHeaderId == cvd.CheckVoucherHeader.CheckVoucherHeaderId)
                .Include(cvd => cvd.CheckVoucherHeader)
                .OrderBy(cvd => cvd.CheckVoucherDetailId)
                .Select(cvd => new SelectListItem
                {
                    Value = cvd.CheckVoucherHeaderId.ToString(),
                    Text = cvd.CheckVoucherHeader.CheckVoucherHeaderNo
                })
                .Distinct()
                .ToListAsync();

            var suppliers = await _dbContext.FilprideSuppliers
                .Where(cvh => cvh.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.SupplierId.ToString(),
                    Text = cvh.SupplierName
                })
                .ToListAsync();

            var bankAccounts = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
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
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? default,
                Particulars = index >= 0 ? particulars.Substring(0, index).Trim() : particulars,
                Payee = existingHeaderModel.SupplierId != null ? existingHeaderModel.Supplier.SupplierName : existingDetailsModel.Supplier.SupplierName,
                PayeeAddress = existingHeaderModel.SupplierId != null ? existingHeaderModel.Supplier.SupplierAddress : existingDetailsModel.Supplier.SupplierAddress,
                PayeeTin = existingHeaderModel.SupplierId != null ? existingHeaderModel.Supplier.SupplierTin : existingDetailsModel.Supplier.SupplierTin,
                MultipleSupplierId = existingHeaderModel.SupplierId != null ? existingHeaderModel.SupplierId : existingDetailsModel.SupplierId,
                CvPaymentId = existingDetailsModel.CheckVoucherDetailId,
                Suppliers = suppliers,
                CvId = existingDetailsModel.CheckVoucherHeader.CheckVoucherHeaderId
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region--Get Check Voucher Invoicing

                    var existingHeaderModel = await _dbContext.FilprideCheckVoucherDetails
                        .Include(cv => cv.Supplier)
                        .Include(cv => cv.CheckVoucherHeader)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherDetailId == viewModel.CvPaymentId, cancellationToken);

                    var invoicingVoucher = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cv => viewModel.MultipleCvId.Contains(cv.CheckVoucherHeaderId))
                        .OrderBy(cv => cv.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                    bool isForTheBir = false;

                    var getCVDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(i => viewModel.MultipleCvId.Contains(i.CheckVoucherHeaderId) && i.SupplierId != null && i.SupplierId == viewModel.MultipleSupplierId && i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing))
                        .OrderBy(i => i.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);
                        // for paid in details model
                        for (int j = 0; j < getCVDetails.Count; j++)
                        {
                            if (getCVDetails.Any())
                            {
                                getCVDetails[j].AmountPaid += viewModel.AmountPaid[j] - getCVDetails[j].AmountPaid;
                                isForTheBir = getCVDetails[j].SupplierId == 133 && !getCVDetails[j].IsUserSelected; //BIR Supplier Id
                            }
                        }

                    var voucherIds = invoicingVoucher.Select(i => i.CheckVoucherHeaderId).ToList();
                    var getCVInvoicingSupplier = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvd.SupplierId != null && cvd.SupplierId == viewModel.MultipleSupplierId && voucherIds.Contains(cvd.CheckVoucherHeaderId))
                        .Include(cvd => cvd.CheckVoucherHeader)
                        .Select(cvd => cvd.Credit)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion

                    #region -- Saving the default entries --

                    #region -- Check Voucher Header --

                    if (existingHeaderModel != null)
                    {
                        existingHeaderModel.CheckVoucherHeader.Date = viewModel.TransactionDate;
                        existingHeaderModel.CheckVoucherHeader.PONo = invoicingVoucher.Select(i => i.PONo).FirstOrDefault();
                        existingHeaderModel.CheckVoucherHeader.SINo = invoicingVoucher.Select(i => i.SINo).FirstOrDefault();
                        existingHeaderModel.SupplierId = viewModel.MultipleSupplierId;
                        existingHeaderModel.CheckVoucherHeader.Particulars = $"{viewModel.Particulars} Payment for {string.Join(",", invoicingVoucher.Select(i => i.CheckVoucherHeaderNo))}";
                        existingHeaderModel.CheckVoucherHeader.Total = viewModel.Total;
                        existingHeaderModel.CheckVoucherHeader.EditedBy = _userManager.GetUserName(this.User);
                        existingHeaderModel.CheckVoucherHeader.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        existingHeaderModel.CheckVoucherHeader.Category = "Non-Trade";
                        existingHeaderModel.CheckVoucherHeader.CvType = nameof(CVType.Payment);
                        existingHeaderModel.CheckVoucherHeader.Reference = string.Join(", ", invoicingVoucher.Select(inv => inv.CheckVoucherHeaderNo));
                        existingHeaderModel.CheckVoucherHeader.BankId = viewModel.BankId;
                        existingHeaderModel.CheckVoucherHeader.Payee = viewModel.Payee;
                        existingHeaderModel.CheckVoucherHeader.CheckNo = viewModel.CheckNo;
                        existingHeaderModel.CheckVoucherHeader.CheckDate = viewModel.CheckDate;
                        existingHeaderModel.CheckVoucherHeader.CheckAmount = viewModel.Total;
                        existingHeaderModel.CheckVoucherHeader.Total = viewModel.Total;
                        existingHeaderModel.CheckVoucherHeader.Company = companyClaims;
                        existingHeaderModel.CheckVoucherHeader.Type = string.Join(", ", invoicingVoucher.Select(inv => inv.Type));
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Check Voucher Header --

                    #region -- Multiple Payment Storing --

                    var getCVs = await _dbContext.FilprideMultipleCheckVoucherPayments
                        .Where(cvp => cvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                        .Include(cvp => cvp.CheckVoucherHeaderInvoice)
                        .Include(cvp => cvp.CheckVoucherHeaderPayment)
                        .ToListAsync(cancellationToken);

                    if (getCVs.Any())
                    {
                        for (int i = 0; i < getCVs.Count; i++)
                        {

                            if (isForTheBir)
                            {
                                continue;
                            }

                            getCVs[i].CheckVoucherHeaderInvoice.AmountPaid -= getCVs[i].AmountPaid;
                            if (getCVs[i].CheckVoucherHeaderInvoice?.AmountPaid >= getCVs[i].CheckVoucherHeaderInvoice?.InvoiceAmount)
                            {
                                getCVs[i].CheckVoucherHeaderInvoice.IsPaid = true;
                            }
                        }
                    }

                    _dbContext.RemoveRange(getCVs);

                    var multipleCheckVoucherPayment = new List<FilprideMultipleCheckVoucherPayment>();
                    for (int i = 0; i < viewModel.MultipleCvId.Length; i++)
                    {
                        var cvId = viewModel.MultipleCvId[i];
                        multipleCheckVoucherPayment.Add(new FilprideMultipleCheckVoucherPayment
                        {
                            Id = Guid.NewGuid(),
                            CheckVoucherHeaderPaymentId = existingHeaderModel.CheckVoucherHeader.CheckVoucherHeaderId,
                            CheckVoucherHeaderInvoiceId = cvId,
                            AmountPaid = viewModel.AmountPaid[i],
                        });
                    }
                    await _dbContext.FilprideMultipleCheckVoucherPayments.AddRangeAsync(multipleCheckVoucherPayment, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #region--Update invoicing voucher

                    var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                        .Where(mcvp => viewModel.MultipleCvId.Contains(mcvp.CheckVoucherHeaderInvoiceId) && mcvp.CheckVoucherHeaderPaymentId == existingHeaderModel.CheckVoucherHeaderId)
                        .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                        .ToListAsync(cancellationToken);

                    if (updateMultipleInvoicingVoucher.Any())
                    {
                        for (int j = 0; j < updateMultipleInvoicingVoucher.Count; j++)
                        {
                            updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.AmountPaid += viewModel.AmountPaid[j];
                            if (updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice?.AmountPaid >= updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice?.InvoiceAmount)
                            {
                                updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.IsPaid = true;
                                updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.Status = nameof(CheckVoucherInvoiceStatus.Paid);
                            }
                        }
                    }

                    #endregion

                    #endregion -- Multiple Payment Storing --

                    #region -- Check Voucher Details --

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var details = new List<FilprideCheckVoucherDetail>();

                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        details.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = viewModel.AccountNumber[i],
                            AccountName = viewModel.AccountTitle[i],
                            TransactionNo = existingHeaderModel.CheckVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeader.CheckVoucherHeaderId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            Amount = viewModel.MultipleSupplierId != null ? getCVInvoicingSupplier : 0,
                            SupplierId = viewModel.MultipleSupplierId != null ? viewModel.MultipleSupplierId : null
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    #endregion -- Check Voucher Details --

                    #endregion -- Saving the default entries --

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        existingHeaderModel.CheckVoucherHeader.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingHeaderModel.CheckVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.CheckVoucherHeader.SupportingFileSavedFileName);
                    }
                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.CheckVoucherHeader.EditedBy, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingHeaderModel.CheckVoucherHeader.Company);
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

                    viewModel.Banks = await _dbContext.FilprideBankAccounts
                        .Where(ba => ba.Company == companyClaims)
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);


            viewModel.Banks = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<JsonResult> GetMultipleSupplierDetails(int cvId, int suppId, CancellationToken cancellationToken)
        {
            var supplier = await _dbContext.FilprideSuppliers
                    .FindAsync(suppId, cancellationToken);

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
            var cv = await _dbContext.FilprideCheckVoucherHeaders
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(c => c.CheckVoucherHeaderId == cvId, cancellationToken);

            // Ensure that cv is not null before proceeding
            if (cv == null)
            {
                return Json(null);
            }

            // Retrieve the list of supplier IDs from the check voucher details
            var supplierIds = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == cv.CheckVoucherHeaderNo)
                .Select(cvd => cvd.SupplierId)
                .ToListAsync(cancellationToken);

            // Fetch suppliers whose IDs are in the supplierIds list
            var suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supplierIds.Contains(supp.SupplierId))
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
            var supplier = await _dbContext.FilprideSuppliers
                    .FindAsync(suppId, cancellationToken);

            var credit = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.SupplierId == suppId && cvd.CheckVoucherHeaderId == cvId)
                .Include(cvd => cvd.CheckVoucherHeader)
                .Select(cvd => new
                {
                    RemainingCredit = cvd.Credit - cvd.AmountPaid,
                    Particulars = cvd.CheckVoucherHeader.Particulars
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

            var invoice = await _dbContext.FilprideCheckVoucherHeaders
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(i => i.CheckVoucherHeaderId == invoiceId, cancellationToken);

            if (invoice != null)
            {
                return Json(new
                {
                    Payee = invoice.Supplier.SupplierName,
                    PayeeAddress = invoice.Supplier.SupplierAddress,
                    PayeeTin = invoice.Supplier.SupplierTin,
                    invoice.Particulars,
                    Total = invoice.InvoiceAmount
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int cvId, int? supplierId, CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.CvId = cvId;
            viewModel.MultipleSupplierId = supplierId;

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(cvh => cvh.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.SupplierId.ToString(),
                    Text = cvh.SupplierName
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region--Get Check Voucher Invoicing

                    bool isForTheBir = false;
                    var invoicingVoucher = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cv => viewModel.MultipleCvId.Contains(cv.CheckVoucherHeaderId))
                        .OrderBy(cv => cv.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                        // for paid in details model
                        for (int j = 0; j < invoicingVoucher.Count; j++)
                        {
                            var getCVDetails = await _dbContext.FilprideCheckVoucherDetails
                                .Where(i => viewModel.MultipleCvId[j] == i.CheckVoucherHeaderId && i.SupplierId != null && i.SupplierId == viewModel.MultipleSupplierId && i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing))
                                .OrderBy(i => i.CheckVoucherHeaderId)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (getCVDetails != null && getCVDetails.CheckVoucherHeaderId == viewModel.MultipleCvId[j])
                            {
                                getCVDetails.AmountPaid += viewModel.AmountPaid[j];
                                isForTheBir = getCVDetails.SupplierId == 133 && !getCVDetails.IsUserSelected; //BIR Supplier Id
                            }
                        }

                    var voucherIds = invoicingVoucher.Select(i => i.CheckVoucherHeaderId).ToList();
                    var getCVInvoicingSupplier = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvd.SupplierId != null && cvd.SupplierId == viewModel.MultipleSupplierId && voucherIds.Contains(cvd.CheckVoucherHeaderId))
                        .Include(cvd => cvd.CheckVoucherHeader)
                        .Select(cvd => cvd.Credit)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion

                    #region -- Saving the default entries --

                    #region -- Check Voucher Header --

                    FilprideCheckVoucherHeader checkVoucherHeader = new()
                    {
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultiplePaymentAsync(companyClaims, invoicingVoucher.Select(i => i.Type).FirstOrDefault(), cancellationToken),
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
                        Payee = invoicingVoucher.Select(i => i.Payee).FirstOrDefault(),
                        CheckNo = viewModel.CheckNo,
                        CheckDate = viewModel.CheckDate,
                        CheckAmount = viewModel.Total,
                        Company = companyClaims,
                        Type = invoicingVoucher.Select(i => i.Type).First()
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Check Voucher Header --

                    #region -- Multiple Payment Storing --

                    for (int i = 0; i < viewModel.MultipleCvId.Length; i++)
                    {
                        var cvId = viewModel.MultipleCvId[i];
                        FilprideMultipleCheckVoucherPayment multipleCheckVoucherPayment = new()
                        {
                            Id = Guid.NewGuid(),
                            CheckVoucherHeaderPaymentId = checkVoucherHeader.CheckVoucherHeaderId,
                            CheckVoucherHeaderInvoiceId = cvId,
                            AmountPaid = viewModel.AmountPaid[i],
                        };

                        _dbContext.Add(multipleCheckVoucherPayment);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #region--Update invoicing voucher

                    // await _unitOfWork.FilprideCheckVoucher.UpdateMultipleInvoicingVoucher(checkVoucherHeader.Total, viewModel.CvId, cancellationToken);
                    var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                        .Where(mcvp => viewModel.MultipleCvId.Contains(mcvp.CheckVoucherHeaderInvoiceId) && mcvp.CheckVoucherHeaderPaymentId == checkVoucherHeader.CheckVoucherHeaderId)
                        .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                        .ToListAsync(cancellationToken);

                    if (updateMultipleInvoicingVoucher.Any())
                    {
                        for (int j = 0; j < updateMultipleInvoicingVoucher.Count; j++)
                        {
                            if (isForTheBir)
                            {
                                continue;
                            }


                            updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.AmountPaid += viewModel.AmountPaid[j];
                            if (updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice?.AmountPaid >= updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice?.InvoiceAmount)
                            {
                                updateMultipleInvoicingVoucher[j].CheckVoucherHeaderInvoice.IsPaid = true;
                            }
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
                                Amount = viewModel.MultipleSupplierId != null ? getCVInvoicingSupplier : 0,
                                SupplierId = viewModel.MultipleSupplierId != null ? viewModel.MultipleSupplierId : null
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
                        checkVoucherHeader.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, checkVoucherHeader.SupportingFileSavedFileName);
                    }
                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, checkVoucherHeader.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check voucher payment created successfully";
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

                    viewModel.Banks = await _dbContext.FilprideBankAccounts
                        .Where(ba => ba.Company == companyClaims)
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);


            viewModel.Banks = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            if (supplierId != null)
            {
                var supplier = await _dbContext.FilprideSuppliers
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

        public async Task<IActionResult> GetCVs(int supplierId, CancellationToken cancellationToken)
        {

            if (supplierId == 133)//For the BIR purposes
            {
                var birInvoices = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd =>
                        cvd.CheckVoucherHeader.SupplierId != null &&
                        cvd.CheckVoucherHeader.SupplierId == supplierId &&
                        cvd.CheckVoucherHeader.PostedBy != null &&
                        cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                        !cvd.CheckVoucherHeader.IsPaid ||
                        cvd.SupplierId != null &&
                        cvd.SupplierId == supplierId &&
                        cvd.CheckVoucherHeader.PostedBy != null &&
                        cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                        cvd.CheckVoucherHeaderId == cvd.CheckVoucherHeader.CheckVoucherHeaderId &&
                        cvd.Amount > cvd.AmountPaid)
                    .Include(cvd => cvd.CheckVoucherHeader)
                    .ToListAsync(cancellationToken);

                if (birInvoices.Any())
                {
                    var cvList = birInvoices
                        .OrderBy(cv => cv.CheckVoucherDetailId)
                        .Select(cv => new { Id = cv.CheckVoucherHeader.CheckVoucherHeaderId, CVNumber = cv.CheckVoucherHeader.CheckVoucherHeaderNo })
                        .Distinct()
                        .ToList();
                    return Json(cvList);
                }

            }

            var checkVoucher = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd =>
                    cvd.CheckVoucherHeader.SupplierId != null &&
                    cvd.CheckVoucherHeader.SupplierId == supplierId &&
                    cvd.CheckVoucherHeader.PostedBy != null &&
                    cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                    !cvd.CheckVoucherHeader.IsPaid ||
                    cvd.SupplierId != null &&
                    cvd.SupplierId == supplierId &&
                    cvd.CheckVoucherHeader.PostedBy != null &&
                    cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) &&
                    !cvd.CheckVoucherHeader.IsPaid &&
                    cvd.CheckVoucherHeaderId == cvd.CheckVoucherHeader.CheckVoucherHeaderId &&
                    cvd.Amount > cvd.AmountPaid)
                .Include(cvd => cvd.CheckVoucherHeader)
                .ToListAsync(cancellationToken);

            if (checkVoucher.Any())
            {
                var cvList = checkVoucher
                    .OrderBy(cv => cv.CheckVoucherDetailId)
                    .Select(cv => new { Id = cv.CheckVoucherHeader.CheckVoucherHeaderId, CVNumber = cv.CheckVoucherHeader.CheckVoucherHeaderNo })
                    .Distinct()
                    .ToList();
                return Json(cvList);
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] cvId, int supplierId, CancellationToken cancellationToken)
        {
            if (cvId == null)
            {
                return Json(null);
            }

            var invoices = await _dbContext.FilprideCheckVoucherDetails
                .Where(i =>
                    cvId.Contains(i.CheckVoucherHeaderId) &&
                    i.SupplierId != null &&
                    i.SupplierId == supplierId &&
                    i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) ||
                    cvId.Contains(i.CheckVoucherHeader.CheckVoucherHeaderId) &&
                    i.CheckVoucherHeader.SupplierId != null &&
                    i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing))
                .Include(i => i.Supplier)
                .Include(i => i.CheckVoucherHeader)
                .ToListAsync(cancellationToken);

            var totalInvoiceAmount = 0m;
            var invoiceAmount = 0m;
            var particulars = invoices.Select(cvh => cvh.CheckVoucherHeader.Particulars).FirstOrDefault();
            var accountNumber = "202010200";
            var accountTitle = "AP-Non Trade Payable";
            foreach (var invoice in invoices.Where(i => i.Credit > 0))
            {

                if (invoice.CheckVoucherHeader.SupplierId != null && supplierId != invoice.SupplierId)
                {
                    var getBalance = invoice.CheckVoucherHeader.InvoiceAmount - invoice.CheckVoucherHeader.AmountPaid;
                    totalInvoiceAmount += getBalance;
                    invoiceAmount = getBalance;

                }
                else
                {
                    var getBalance = invoice.Amount - invoice.AmountPaid;
                    accountNumber = invoice.AccountNo;
                    accountTitle = invoice.AccountName;
                    totalInvoiceAmount += getBalance;
                    invoiceAmount = getBalance;
                }
            }

            if (invoices != null)
            {
                return Json(new
                {
                    Amount = totalInvoiceAmount,
                    InvoiceAmount = invoiceAmount,
                    Particulars = particulars,
                    ApAccount = new
                    {
                        AccountNumber = accountNumber,
                        AccountTitle = accountTitle,
                    }
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetAmountPaid(int[] cvId, int supplierId, int checkVoucherPaymentId, CancellationToken cancellationToken)
        {
            if (cvId == null)
            {
                return Json(null);
            }

            var invoice = await _dbContext.FilprideCheckVoucherDetails
                .Where(i => cvId.Contains(i.CheckVoucherHeaderId) && i.SupplierId != null && i.SupplierId == supplierId && i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) ||
                            cvId.Contains(i.CheckVoucherHeader.CheckVoucherHeaderId) && i.CheckVoucherHeader.SupplierId != null && i.CheckVoucherHeader.CvType == nameof(CVType.Invoicing))
                .Include(i => i.Supplier)
                .Include(i => i.CheckVoucherHeader)
                .ToListAsync(cancellationToken);

            var updateMultipleInvoicingVoucher = await _dbContext.FilprideMultipleCheckVoucherPayments
                .Where(mcvp => cvId.Contains(mcvp.CheckVoucherHeaderInvoiceId) && mcvp.CheckVoucherHeaderPaymentId == checkVoucherPaymentId)
                .Include(mcvp => mcvp.CheckVoucherHeaderInvoice)
                .ToListAsync(cancellationToken);

            var totalInvoiceAmount = 0m;
            var invoiceAmount = 0m;
            var particulars = invoice.Select(cvh => cvh.CheckVoucherHeader.Particulars).FirstOrDefault();
            for (int i = 0; i < updateMultipleInvoicingVoucher.Count; i++)
            {
                totalInvoiceAmount += updateMultipleInvoicingVoucher[i].AmountPaid;
                invoiceAmount = updateMultipleInvoicingVoucher[i].AmountPaid;
            }

            if (invoice != null)
            {
                return Json(new
                {
                    Amount = totalInvoiceAmount,
                    InvoiceAmount = invoiceAmount,
                    Particulars = particulars
                });
            }

            return Json(null);
        }
    }
}
