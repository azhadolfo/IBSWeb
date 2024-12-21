using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CheckVoucherController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public CheckVoucherController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.CheckVoucher))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cv => cv.Company == companyClaims && cv.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", checkVoucherHeaders);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cv => cv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                    .Where(s =>
                        s.CheckVoucherHeaderNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.Total.ToString().Contains(searchValue) ||
                        s.Amount?.ToString().Contains(searchValue) == true ||
                        s.Category.ToLower().Contains(searchValue) ||
                        s.CvType?.ToLower().Contains(searchValue) == true ||
                        s.CreatedBy.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetAllAsync(po => po.SupplierId == supplierId && po.PostedBy != null);

            if (purchaseOrders != null && purchaseOrders.Any())
            {
                var poList = purchaseOrders.OrderBy(po => po.PurchaseOrderNo).Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, string? criteria)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var receivingReports = await _dbContext.FilprideReceivingReports
            .Where(rr => rr.Company == companyClaims && poNumber.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
            .OrderBy(rr => rr.ReceivingReportNo)
            .ThenBy(rr => criteria == "Transaction Date" ? rr.Date : rr.DueDate)
            .ToListAsync();

            if (receivingReports != null && receivingReports.Count > 0)
            {
                var rrList = receivingReports.Select(rr => new { Id = rr.ReceivingReportId, RRNumber = rr.ReceivingReportNo }).ToList();
                return Json(rrList);
            }

            return Json(null);
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

        public async Task<IActionResult> GetDefaultExpense(int? supplierId)
        {
            var supplier = await _dbContext.FilprideSuppliers
                .Where(supp => supp.SupplierId == supplierId)
                .Select(supp => supp.DefaultExpenseNumber)
                .FirstOrDefaultAsync();

            var defaultExpense = await _dbContext.FilprideChartOfAccounts
                .Where(coa => (coa.Level == 4 || coa.Level == 5))
                .OrderBy(coa => coa.AccountId)
                .ToListAsync();

            if (defaultExpense != null && defaultExpense.Count > 0)
            {
                var defaultExpenseList = defaultExpense.Select(coa => new
                {
                    AccountNumber = coa.AccountNumber,
                    AccountTitle = coa.AccountName,
                    IsSelected = coa.AccountNumber == supplier?.Split(' ')[0]
                }).ToList();

                return Json(defaultExpenseList);
            }

            return Json(null);
        }

        public async Task<IActionResult> RRBalance(string rrNo)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var receivingReport = await _unitOfWork.FilprideReceivingReport
                .GetAsync(rr => rr.Company == companyClaims && rr.ReceivingReportNo == rrNo);

            if (receivingReport != null)
            {
                var amount = receivingReport.Amount;
                var amountPaid = receivingReport.AmountPaid;
                var netAmount = _unitOfWork.FilprideCheckVoucher.ComputeNetOfVat(amount);
                var vatAmount = _unitOfWork.FilprideCheckVoucher.ComputeVatAmount(netAmount);
                var ewtAmount = _unitOfWork.FilprideCheckVoucher.ComputeEwtAmount(netAmount, 0.01m);
                var balance = amount - amountPaid;

                return Json(new
                {
                    Amount = amount,
                    AmountPaid = amountPaid,
                    NetAmount = netAmount,
                    VatAmount = vatAmount,
                    EwtAmount = ewtAmount,
                    Balance = balance
                });
            }
            return Json(null);
        }

        public async Task<IActionResult> GetAutomaticEntry(DateTime startDate, DateTime? endDate)
        {
            if (startDate != default && endDate != default)
            {
                return Json(true);
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

            var header = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var companyClaims = await GetCompanyClaimAsync();

            if (header.Category == "Trade" && header.RRNo != null)
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.FilprideReceivingReports
                                .FirstOrDefaultAsync(p => p.Company == companyClaims && p.ReceivingReportNo == rrValue);

                    if (rr != null)
                    {
                        siArray[i] = rr.SupplierInvoiceNumber;
                    }
                }

                ViewBag.SINoArray = siArray;
            }

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);
            var modelDetails = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId).ToListAsync();

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

                        #region -- Partial payment of RR's
                        if (modelHeader.Amount != null)
                        {
                            var receivingReport = new FilprideReceivingReport();
                            for (int i = 0; i < modelHeader.RRNo.Length; i++)
                            {
                                var rrValue = modelHeader.RRNo[i];
                                receivingReport = await _dbContext.FilprideReceivingReports
                                            .FirstOrDefaultAsync(p => p.Company == modelHeader.Company && p.ReceivingReportNo == rrValue);

                                receivingReport.AmountPaid += modelHeader.Amount[i];

                                if (receivingReport.Amount <= receivingReport.AmountPaid)
                                {
                                    receivingReport.IsPaid = true;
                                    receivingReport.PaidDate = DateTimeHelper.GetCurrentPhilippineTime();
                                }
                            }
                        }

                        #endregion -- Partial payment of RR's

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
                                        CreatedDate = modelHeader.CreatedDate
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
                                        Payee = modelHeader.Payee,
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

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
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
        public async Task<IActionResult> EditTrade(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .ToListAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();
            var poIds = _dbContext.FilpridePurchaseOrders.Where(model => model.Company == companyClaims && existingHeaderModel.PONo.Contains(model.PurchaseOrderNo)).Select(model => model.PurchaseOrderId).ToArray();
            var rrIds = _dbContext.FilprideReceivingReports.Where(model => model.Company == companyClaims && existingHeaderModel.RRNo.Contains(model.ReceivingReportNo)).Select(model => model.ReceivingReportId).ToArray();

            var coa = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            CheckVoucherTradeViewModel model = new()
            {
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee,
                SupplierAddress = existingHeaderModel.Supplier.SupplierAddress,
                SupplierTinNo = existingHeaderModel.Supplier.SupplierTin,
                Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken),
                RRSeries = existingHeaderModel.RRNo,
                RR = await _unitOfWork.FilprideReceivingReport.GetReceivingReportListAsync(existingHeaderModel.RRNo, companyClaims, cancellationToken),
                POSeries = existingHeaderModel.PONo,
                PONo = await _unitOfWork.FilpridePurchaseOrder.GetPurchaseOrderListAsyncByCode(companyClaims, cancellationToken),
                TransactionDate = existingHeaderModel.Date,
                BankAccounts = await _unitOfWork.FilprideBankAccount.GetBankAccountListAsync(companyClaims, cancellationToken),
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars,
                Amount = existingHeaderModel.Amount,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                COA = coa,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                CVNo = existingHeaderModel.CheckVoucherHeaderNo,
                CreatedBy = _userManager.GetUserName(this.User),
                POId = poIds,
                RRId = rrIds
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditTrade(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var companyClaims = await GetCompanyClaimAsync();

                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _unitOfWork
                        .FilprideCheckVoucher
                        .GetAllAsync(cv => cv.Company == companyClaims && cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo), cancellationToken);

                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.CheckVoucherDetailId);
                    }

                    var cashInBank = 0m;
                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        cashInBank = viewModel.Credit[2];

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == detailsId);

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = viewModel.CVNo;
                            details.CheckVoucherHeaderId = viewModel.CVId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new FilprideCheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = viewModel.CVNo,
                                CheckVoucherHeaderId = viewModel.CVId
                            };
                            _dbContext.FilprideCheckVoucherDetails.Add(newDetails);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == id);
                            _dbContext.FilprideCheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region -- Partial payment of RR's

                    if (viewModel.Amount != null)
                    {
                        var receivingReport = new FilprideReceivingReport();
                        for (int i = 0; i < viewModel.RRSeries.Length; i++)
                        {
                            var rrValue = viewModel.RRSeries[i];
                            receivingReport = await _dbContext.FilprideReceivingReports
                                        .FirstOrDefaultAsync(p => p.Company == companyClaims && p.ReceivingReportNo == rrValue);

                            if (i < existingHeaderModel.Amount.Length)
                            {
                                var amount = Math.Round(viewModel.Amount[i] - existingHeaderModel.Amount[i], 4);
                                receivingReport.AmountPaid += amount;
                            }
                            else
                            {
                                receivingReport.AmountPaid += viewModel.Amount[i];
                            }

                            if (receivingReport.Amount <= receivingReport.AmountPaid)
                            {
                                receivingReport.IsPaid = true;
                                receivingReport.PaidDate = DateTimeHelper.GetCurrentPhilippineTime();
                            }
                            else
                            {
                                receivingReport.IsPaid = false;
                                receivingReport.PaidDate = DateTime.MaxValue;
                            }
                        }
                    }

                    #endregion -- Partial payment of RR's

                    #region --Saving the default entries

                    existingHeaderModel.CheckVoucherHeaderNo = viewModel.CVNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.RRNo = viewModel.RRSeries;
                    existingHeaderModel.PONo = viewModel.POSeries;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.Amount = viewModel.Amount;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", viewModel.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingHeaderModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Trade edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

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

                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo);
                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo);

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

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

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

        [HttpGet]
        public async Task<IActionResult> Trade(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            CheckVoucherTradeViewModel model = new();
            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            model.BankAccounts = await _dbContext.FilprideBankAccounts
                .Where(b => b.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Trade(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Check if duplicate record
                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .FilprideCheckVoucherHeaders
                        .Where(cv => cv.Company == companyClaims && cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId)
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            viewModel.COA = await _dbContext.FilprideChartOfAccounts
                                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.AccountNumber,
                                    Text = s.AccountNumber + " " + s.AccountName
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                                .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                                .Select(sup => new SelectListItem
                                {
                                    Value = sup.SupplierId.ToString(),
                                    Text = sup.SupplierName
                                })
                                .ToListAsync();

                            viewModel.PONo = await _dbContext.FilpridePurchaseOrders
                                .Where(po => po.Company == companyClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.RR = await _dbContext.FilprideReceivingReports
                                .Where(rr => rr.Company == companyClaims && viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                                .Select(rr => new SelectListItem
                                {
                                    Value = rr.ReceivingReportNo.ToString(),
                                    Text = rr.ReceivingReportNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
                                .Where(b => b.Company == companyClaims)
                                .Select(ba => new SelectListItem
                                {
                                    Value = ba.BankAccountId.ToString(),
                                    Text = ba.AccountNo + " " + ba.AccountName
                                })
                                .ToListAsync();

                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate record

                    #region --Retrieve Supplier
                    var supplier = await _dbContext
                                .FilprideSuppliers
                                .FirstOrDefaultAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                    #endregion --Retrieve Supplier

                    #region -- Get PO --

                    var getPurchaseOrder = await _dbContext.FilpridePurchaseOrders
                                                    .Where(po => viewModel.POSeries.Contains(po.PurchaseOrderNo))
                                                    .FirstOrDefaultAsync();

                    #endregion -- Get PO --

                    #region --Saving the default entries
                    var generateCVNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, getPurchaseOrder.Type, cancellationToken);
                    var cashInBank = viewModel.Credit[2]; ;
                    var cvh = new FilprideCheckVoucherHeader
                    {
                        CheckVoucherHeaderNo = generateCVNo,
                        Date = viewModel.TransactionDate,
                        RRNo = viewModel.RRSeries,
                        PONo = viewModel.POSeries,
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        BankId = viewModel.BankId,
                        CheckNo = viewModel.CheckNo,
                        Category = "Trade",
                        Payee = viewModel.Payee,
                        CheckDate = viewModel.CheckDate,
                        Total = cashInBank,
                        Amount = viewModel.Amount,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Company = companyClaims,
                        Type = getPurchaseOrder.Type,
                    };

                    await _dbContext.FilprideCheckVoucherHeaders.AddAsync(cvh, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion --Saving the default entries

                    #region --CV Details Entry
                    var cvDetails = new List<FilprideCheckVoucherDetail>();
                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            cvDetails.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = cvh.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);
                    #endregion --CV Details Entry

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", cvh.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    TempData["success"] = "Check voucher trade created successfully";

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, cvh.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                            .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToListAsync();

                    viewModel.PONo = await _dbContext.FilpridePurchaseOrders
                                .Where(po => po.Company == companyClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                    viewModel.RR = await _dbContext.FilprideReceivingReports
                        .Where(rr => rr.Company == companyClaims && viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                        .Select(rr => new SelectListItem
                        {
                            Value = rr.ReceivingReportNo.ToString(),
                            Text = rr.ReceivingReportNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
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
            viewModel.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            viewModel.PONo = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.Company == companyClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.RR = await _dbContext.FilprideReceivingReports
                .Where(rr => rr.Company == companyClaims && viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                .Select(rr => new SelectListItem
                {
                    Value = rr.ReceivingReportNo.ToString(),
                    Text = rr.ReceivingReportNo
                })
                .ToListAsync(cancellationToken);

            viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
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
        public async Task<IActionResult> NonTradeInvoicing(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category != "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Saving the default entries --

                    FilprideCheckVoucherHeader checkVoucherHeader = new()
                    {
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, viewModel.Type, cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = viewModel.SupplierName,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Invoicing",
                        Company = companyClaims,
                        Type = viewModel.Type
                    };

                    #endregion -- Saving the default entries --

                    #region -- Get Supplier --

                    var supplier = await _dbContext.FilprideSuppliers
                        .Where(s => s.SupplierId == viewModel.SupplierId)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- Get Supplier --

                    #region -- Automatic entry --

                    if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                    {
                        checkVoucherHeader.StartDate = viewModel.StartDate;
                        checkVoucherHeader.EndDate = checkVoucherHeader.StartDate.Value.AddYears(viewModel.NumberOfYears);
                        checkVoucherHeader.NumberOfMonths = (viewModel.NumberOfYears * 12);

                        // Identify the account with a number that starts with '10201'
                        decimal? amount = null;
                        for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                        {
                            if (supplier.TaxType == "Exempt" && (i == 2 || i == 3))
                            {
                                continue;
                            }

                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                            }
                        }

                        if (amount.HasValue)
                        {
                            checkVoucherHeader.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Automatic entry --

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
                                Credit = viewModel.Credit[i]
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy, $"Created new check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, checkVoucherHeader.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check voucher invoicing created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                        .Where(supp => supp.Company == companyClaims && supp.Category != "Trade")
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.SupplierId.ToString(),
                            Text = sup.SupplierName
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category != "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> NonTradePayment(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.Company == companyClaims && cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradePayment(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region--Get Check Voucher Invoicing

                    var invoicingVoucher = await _dbContext.FilprideCheckVoucherHeaders
                        .FindAsync(viewModel.CvId, cancellationToken);

                    #endregion

                    #region--Saving the default entries

                    FilprideCheckVoucherHeader checkVoucherHeader = new()
                    {
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, invoicingVoucher.Type, cancellationToken),
                        Date = viewModel.TransactionDate,
                        PONo = invoicingVoucher.PONo,
                        SINo = invoicingVoucher.SINo,
                        SupplierId = invoicingVoucher.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Payment",
                        Reference = invoicingVoucher.CheckVoucherHeaderNo,
                        BankId = viewModel.BankId,
                        Payee = viewModel.Payee,
                        CheckNo = viewModel.CheckNo,
                        CheckDate = viewModel.CheckDate,
                        CheckAmount = viewModel.Total,
                        Company = companyClaims,
                        Type = invoicingVoucher.Type
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                                Credit = viewModel.Credit[i]
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region--Update invoicing voucher

                    await _unitOfWork.FilprideCheckVoucher.UpdateInvoicingVoucher(checkVoucherHeader.Total, viewModel.CvId, cancellationToken);

                    #endregion

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
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cvh => cvh.Company == companyClaims && cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync();

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
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.Company == companyClaims && cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync();

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
        public async Task<IActionResult> GetCheckVoucherDetails(int? cvId)
        {
            if (cvId != null)
            {
                var cv = await _dbContext.FilprideCheckVoucherHeaders
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(c => c.CheckVoucherHeaderId == cvId);

                var cvd = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.TransactionNo == cv.CheckVoucherHeaderNo && cvd.AccountNo == "2010102").Select(cvd => cvd.Credit).FirstOrDefaultAsync();

                if (cv != null)
                {
                    return Json(new
                    {
                        Payee = cv.Supplier.SupplierName,
                        PayeeAddress = cv.Supplier.SupplierAddress,
                        PayeeTin = cv.Supplier.SupplierTin,
                        Total = cvd
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> EditNonTradeInvoicing(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId).ToListAsync();

            existingModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            existingModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            var accountNumbers = existingDetailsModel.OrderBy(x => x.CheckVoucherDetailId).Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.OrderBy(x => x.CheckVoucherDetailId).Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.OrderBy(x => x.CheckVoucherDetailId).Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.OrderBy(x => x.CheckVoucherDetailId).Select(model => model.Credit).ToArray();

            CheckVoucherNonTradeInvoicingViewModel viewModel = new()
            {
                CVId = existingModel.CheckVoucherHeaderId,
                Suppliers = existingModel.Suppliers,
                SupplierName = existingModel.Supplier.SupplierName,
                ChartOfAccounts = existingModel.COA,
                TransactionDate = existingModel.Date,
                SupplierId = existingModel.SupplierId ?? 0,
                SupplierAddress = existingModel.Supplier.SupplierAddress,
                SupplierTinNo = existingModel.Supplier.SupplierTin,
                PoNo = existingModel.PONo?.FirstOrDefault(),
                SiNo = existingModel.SINo?.FirstOrDefault(),
                Total = existingModel.Total,
                Particulars = existingModel.Particulars,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                DefaultExpenses = await _dbContext.FilprideChartOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountName
                })
                .ToListAsync(cancellationToken),
                Debit = debit,
                Credit = credit
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditNonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving the default entries

                    var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                    if (existingModel != null)
                    {
                        existingModel.EditedBy = _userManager.GetUserName(User);
                        existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        existingModel.Date = viewModel.TransactionDate;
                        existingModel.SupplierId = viewModel.SupplierId;
                        existingModel.PONo = [viewModel.PoNo];
                        existingModel.SINo = [viewModel.SiNo];
                        existingModel.Total = viewModel.Total;
                        existingModel.Particulars = viewModel.Particulars;
                    }

                    //For automation purposes
                    if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                    {
                        existingModel.StartDate = viewModel.StartDate;
                        existingModel.EndDate = existingModel.StartDate.Value.AddYears(viewModel.NumberOfYears);
                        existingModel.NumberOfMonths = (viewModel.NumberOfYears * 12);

                        // Identify the account with a number that starts with '10201'
                        decimal? amount = null;
                        for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                        {
                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                                break;
                            }
                        }

                        if (amount.HasValue)
                        {
                            existingModel.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }
                    else
                    {
                        existingModel.StartDate = null;
                        existingModel.EndDate = null;
                        existingModel.NumberOfMonths = 0;
                        existingModel.AmountPerMonth = 0;
                    }

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId).ToListAsync();

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.CheckVoucherDetailId);
                    }

                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == detailsId);

                            var acctNo = await _dbContext.FilprideChartOfAccounts
                                .FirstOrDefaultAsync(x => x.AccountName == viewModel.AccountTitle[i]);

                            details.AccountNo = acctNo.AccountNumber ?? throw new ArgumentNullException("Account title not found!");
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingModel.CheckVoucherHeaderNo;
                            details.CheckVoucherHeaderId = viewModel.CVId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new FilprideCheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = viewModel.CVId
                            };
                            _dbContext.FilprideCheckVoucherDetails.Add(newDetails);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == id);
                            _dbContext.FilprideCheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingModel.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited check voucher# {existingModel.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Non-trade invoicing edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                    .Where(sup => sup.Company == companyClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                    .Where(sup => sup.Company == companyClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditNonTradePayment(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync(cancellationToken);

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            #region -- insert fetch data into viewModel --

            CheckVoucherNonTradePaymentViewModel viewModel = new()
            {
                CvId = existingHeaderModel.CheckVoucherHeaderId,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                TransactionDate = existingHeaderModel.Date,
                Payee = existingHeaderModel.Payee,
                PayeeAddress = existingHeaderModel.Supplier.SupplierAddress,
                PayeeTin = existingHeaderModel.Supplier.SupplierTin,
                Total = existingHeaderModel.Total,
                BankId = existingHeaderModel.BankId ?? 0,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? default,
                Particulars = existingHeaderModel.Particulars,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,

                CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.Company == companyClaims && cvh.CvType == "Invoicing" && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken),

                Banks = await _dbContext.FilprideBankAccounts
                .Where(ba => ba.Company == companyClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken),

                ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken)
            };

            #endregion -- insert fetch data into viewModel --

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditNonTradePayment(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var companyClaims = await GetCompanyClaimAsync();

                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .FilprideCheckVoucherHeaders
                        .Where(cv => cv.Company == companyClaims && cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo))
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.CheckVoucherDetailId);
                    }

                    var cashInBank = 0m;
                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        cashInBank = viewModel.Credit[1];

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == detailsId);

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingHeaderModel.CheckVoucherHeaderNo;
                            details.CheckVoucherHeaderId = viewModel.CVId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new FilprideCheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = viewModel.CVId
                            };
                            _dbContext.FilprideCheckVoucherDetails.Add(newDetails);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.CheckVoucherDetailId == id);
                            _dbContext.FilprideCheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.Reference = existingHeaderModel.CheckVoucherHeaderNo;
                    existingHeaderModel.CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId;
                    existingHeaderModel.CheckVoucherHeaderNo = existingHeaderModel.CheckVoucherHeaderNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.SupplierId = existingHeaderModel.Supplier.SupplierId;
                    existingHeaderModel.Supplier.SupplierAddress = viewModel.PayeeAddress;
                    existingHeaderModel.Supplier.SupplierTin = viewModel.PayeeTin;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Non-Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's
                    //if (viewModel.Amount != null)
                    //{
                    //    var receivingReport = new ReceivingReport();
                    //    for (int i = 0; i < viewModel.RRSeries.Length; i++)
                    //    {
                    //        var rrValue = viewModel.RRSeries[i];
                    //        receivingReport = await _dbContext.FilprideReceivingReports
                    //                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                    //        receivingReport.AmountPaid += viewModel.Amount[i];

                    //        if (receivingReport.Amount <= receivingReport.AmountPaid)
                    //        {
                    //            receivingReport.IsPaid = true;
                    //            receivingReport.PaidDate = DateTime.Now;
                    //        }
                    //    }
                    //}

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingHeaderModel.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingHeaderModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Non-trade payment edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher.GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);
            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, cv.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
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

            try
            {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cvh => recordIds.Contains(cvh.CheckVoucherHeaderId) && cvh.Type == nameof(DocumentType.Documented));

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

                    #endregion -- Purchase Order Table Header --

                    #region -- Receving Report Table Header --

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

                    #endregion -- Receving Report Table Header --

                    #region -- Check Voucher Header Table Header --

                    var worksheet = package.Workbook.Worksheets.Add("CheckVoucherHeader");

                    worksheet.Cells["A1"].Value = "TransactionDate";
                    worksheet.Cells["B1"].Value = "ReceivingReportNo";
                    worksheet.Cells["C1"].Value = "SalesInvoiceNo";
                    worksheet.Cells["D1"].Value = "PurchaseOrderNo";
                    worksheet.Cells["E1"].Value = "Particulars";
                    worksheet.Cells["F1"].Value = "CheckNo";
                    worksheet.Cells["G1"].Value = "Category";
                    worksheet.Cells["H1"].Value = "Payee";
                    worksheet.Cells["I1"].Value = "CheckDate";
                    worksheet.Cells["J1"].Value = "StartDate";
                    worksheet.Cells["K1"].Value = "EndDate";
                    worksheet.Cells["L1"].Value = "NumberOfMonths";
                    worksheet.Cells["M1"].Value = "NumberOfMonthsCreated";
                    worksheet.Cells["N1"].Value = "LastCreatedDate";
                    worksheet.Cells["O1"].Value = "AmountPerMonth";
                    worksheet.Cells["P1"].Value = "IsComplete";
                    worksheet.Cells["Q1"].Value = "AccruedType";
                    worksheet.Cells["R1"].Value = "Reference";
                    worksheet.Cells["S1"].Value = "CreatedBy";
                    worksheet.Cells["T1"].Value = "CreatedDate";
                    worksheet.Cells["U1"].Value = "Total";
                    worksheet.Cells["V1"].Value = "Amount";
                    worksheet.Cells["W1"].Value = "CheckAmount";
                    worksheet.Cells["X1"].Value = "CVType";
                    worksheet.Cells["Y1"].Value = "AmountPaid";
                    worksheet.Cells["Z1"].Value = "IsPaid";
                    worksheet.Cells["AA1"].Value = "CancellationRemarks";
                    worksheet.Cells["AB1"].Value = "OriginalBankId";
                    worksheet.Cells["AC1"].Value = "OriginalSeriesNumber";
                    worksheet.Cells["AD1"].Value = "OriginalSupplierId";
                    worksheet.Cells["AE1"].Value = "OriginalDocumentId";

                    #endregion -- Check Voucher Header Table Header --

                    #region -- Check Voucher Details Table Header --

                    var worksheet2 = package.Workbook.Worksheets.Add("CheckVoucherDetails");

                    worksheet2.Cells["A1"].Value = "AccountNo";
                    worksheet2.Cells["B1"].Value = "AccountName";
                    worksheet2.Cells["C1"].Value = "TransactionNo";
                    worksheet2.Cells["D1"].Value = "Debit";
                    worksheet2.Cells["E1"].Value = "Credit";
                    worksheet2.Cells["F1"].Value = "CVHeaderId";
                    worksheet2.Cells["G1"].Value = "OriginalDocumentId";

                    #endregion -- Check Voucher Details Table Header --

                    #region -- Check Voucher Header Export (Trade and Invoicing)--

                    int row = 2;

                    foreach (var item in selectedList)
                    {
                        worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        if (item.RRNo != null && !item.RRNo.Contains(null))
                        {
                            worksheet.Cells[row, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                        }
                        if (item.SINo != null && !item.SINo.Contains(null))
                        {
                            worksheet.Cells[row, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                        }
                        if (item.PONo != null && !item.PONo.Contains(null))
                        {
                            worksheet.Cells[row, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                        }

                        worksheet.Cells[row, 5].Value = item.Particulars;
                        worksheet.Cells[row, 6].Value = item.CheckNo;
                        worksheet.Cells[row, 7].Value = item.Category;
                        worksheet.Cells[row, 8].Value = item.Payee;
                        worksheet.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 12].Value = item.NumberOfMonths;
                        worksheet.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                        worksheet.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 15].Value = item.AmountPerMonth;
                        worksheet.Cells[row, 16].Value = item.IsComplete;
                        worksheet.Cells[row, 17].Value = item.AccruedType;
                        worksheet.Cells[row, 18].Value = item.Reference;
                        worksheet.Cells[row, 19].Value = item.CreatedBy;
                        worksheet.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 21].Value = item.Total;
                        if (item.Amount != null)
                        {
                            worksheet.Cells[row, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                        }
                        worksheet.Cells[row, 23].Value = item.CheckAmount;
                        worksheet.Cells[row, 24].Value = item.CvType;
                        worksheet.Cells[row, 25].Value = item.AmountPaid;
                        worksheet.Cells[row, 26].Value = item.IsPaid;
                        worksheet.Cells[row, 27].Value = item.CancellationRemarks;
                        worksheet.Cells[row, 28].Value = item.BankId;
                        worksheet.Cells[row, 29].Value = item.CheckVoucherHeaderNo;
                        worksheet.Cells[row, 30].Value = item.SupplierId;
                        worksheet.Cells[row, 31].Value = item.CheckVoucherHeaderId;

                        row++;
                    }

                    #endregion -- Check Voucher Header Export (Trade and Invoicing) --

                    #region -- Check Voucher Header Export (Payment) --

                    var cvNos = selectedList.Select(item => item.CheckVoucherHeaderNo).ToList();

                    var checkVoucherPayment = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cvh => cvh.Reference != null && cvNos.Contains(cvh.Reference))
                        .ToListAsync();

                    foreach (var item in checkVoucherPayment)
                    {
                        worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        if (item.RRNo != null && !item.RRNo.Contains(null))
                        {
                            worksheet.Cells[row, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                        }
                        if (item.SINo != null && !item.SINo.Contains(null))
                        {
                            worksheet.Cells[row, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                        }
                        if (item.PONo != null && !item.PONo.Contains(null))
                        {
                            worksheet.Cells[row, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                        }

                        worksheet.Cells[row, 5].Value = item.Particulars;
                        worksheet.Cells[row, 6].Value = item.CheckNo;
                        worksheet.Cells[row, 7].Value = item.Category;
                        worksheet.Cells[row, 8].Value = item.Payee;
                        worksheet.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 12].Value = item.NumberOfMonths;
                        worksheet.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                        worksheet.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 15].Value = item.AmountPerMonth;
                        worksheet.Cells[row, 16].Value = item.IsComplete;
                        worksheet.Cells[row, 17].Value = item.AccruedType;
                        worksheet.Cells[row, 18].Value = item.Reference;
                        worksheet.Cells[row, 19].Value = item.CreatedBy;
                        worksheet.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 21].Value = item.Total;
                        if (item.Amount != null)
                        {
                            worksheet.Cells[row, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                        }
                        worksheet.Cells[row, 23].Value = item.CheckAmount;
                        worksheet.Cells[row, 24].Value = item.CvType;
                        worksheet.Cells[row, 25].Value = item.AmountPaid;
                        worksheet.Cells[row, 26].Value = item.IsPaid;
                        worksheet.Cells[row, 27].Value = item.CancellationRemarks;
                        worksheet.Cells[row, 28].Value = item.BankId;
                        worksheet.Cells[row, 29].Value = item.CheckVoucherHeaderNo;
                        worksheet.Cells[row, 30].Value = item.SupplierId;
                        worksheet.Cells[row, 31].Value = item.CheckVoucherHeaderId;

                        row++;
                    }

                    #endregion -- Check Voucher Header Export (Payment) --

                    #region -- Check Voucher Details Export (Trade and Invoicing) --

                    List<FilprideCheckVoucherDetail> getCVDetails = new();

                    getCVDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                        .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                        .ToListAsync();

                    int cvdRow = 2;

                    foreach (var item in getCVDetails)
                    {
                        worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                        worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                        worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                        worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                        worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                        worksheet2.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                        worksheet2.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;

                        cvdRow++;
                    }

                    #endregion -- Check Voucher Details Export (Trade and Invoicing) --

                    #region -- Check Voucher Details Export (Payment) --

                    List<FilprideCheckVoucherDetail> getCvPaymentDetails = new();

                    getCvPaymentDetails = await _dbContext.FilprideCheckVoucherDetails
                        .Where(cvd => checkVoucherPayment.Select(cvh => cvh.CheckVoucherHeaderNo).Contains(cvd.TransactionNo))
                        .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                        .ToListAsync();

                    foreach (var item in getCvPaymentDetails)
                    {
                        worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                        worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                        worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                        worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                        worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                        worksheet2.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                        worksheet2.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;

                        cvdRow++;
                    }

                    #endregion -- Check Voucher Details Export (Payment) --

                    #region -- Receiving Report Export --

                    List<FilprideReceivingReport> getReceivingReport = new List<FilprideReceivingReport>();

                    getReceivingReport = _dbContext.FilprideReceivingReports
                        .AsEnumerable()
                        .Where(rr => selectedList?.Select(item => item?.RRNo).Any(rrs => rrs?.Contains(rr.ReceivingReportNo) == true) == true)
                        .OrderBy(rr => rr.ReceivingReportNo)
                        .ToList();

                    int rrRow = 2;
                    var currentRR = "";

                    foreach (var item in getReceivingReport)
                    {
                        if (item.ReceivingReportNo == currentRR)
                        {
                            continue;
                        }

                        currentRR = item.ReceivingReportNo;
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

                        rrRow++;
                    }

                    #endregion -- Receiving Report Export --

                    #region -- Purchase Order Export --

                    List<FilpridePurchaseOrder> getPurchaseOrder = new List<FilpridePurchaseOrder>();

                    getPurchaseOrder = await _dbContext.FilpridePurchaseOrders
                        .Where(po => getReceivingReport.Select(item => item.POId).Contains(po.PurchaseOrderId))
                        .OrderBy(po => po.PurchaseOrderNo)
                        .ToListAsync();

                    int poRow = 2;
                    var currentPO = "";

                    foreach (var item in getPurchaseOrder)
                    {
                        if (item.PurchaseOrderNo == currentPO)
                        {
                            continue;
                        }

                        currentPO = item.PurchaseOrderNo;
                        worksheet3.Cells[poRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        worksheet3.Cells[poRow, 2].Value = item.Terms;
                        worksheet3.Cells[poRow, 3].Value = item.Quantity;
                        worksheet3.Cells[poRow, 4].Value = item.Price;
                        worksheet3.Cells[poRow, 5].Value = item.Amount;
                        worksheet3.Cells[poRow, 6].Value = item.FinalPrice;
                        worksheet3.Cells[poRow, 7].Value = item.QuantityReceived;
                        worksheet3.Cells[poRow, 8].Value = item.IsReceived;
                        worksheet3.Cells[poRow, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : default;
                        worksheet3.Cells[poRow, 10].Value = item.Remarks;
                        worksheet3.Cells[poRow, 11].Value = item.CreatedBy;
                        worksheet3.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet3.Cells[poRow, 13].Value = item.IsClosed;
                        worksheet3.Cells[poRow, 14].Value = item.CancellationRemarks;
                        worksheet3.Cells[poRow, 15].Value = item.ProductId;
                        worksheet3.Cells[poRow, 16].Value = item.PurchaseOrderNo;
                        worksheet3.Cells[poRow, 17].Value = item.SupplierId;
                        worksheet3.Cells[poRow, 18].Value = item.PurchaseOrderId;

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

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CheckVoucherList.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCheckVoucherIds()
        {
            var cvIds = _dbContext.FilprideCheckVoucherHeaders
                                     .Select(cv => cv.CheckVoucherHeaderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(cvIds);
        }
    }
}
