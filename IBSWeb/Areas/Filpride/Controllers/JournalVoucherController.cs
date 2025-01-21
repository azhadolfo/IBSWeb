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
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using IBS.Models.Filpride;
using Microsoft.AspNetCore.Authorization;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
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
        public async Task<IActionResult> GetJournalVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var journalVoucherHeader = await _unitOfWork.FilprideJournalVoucher
                    .GetAllAsync(jv => jv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    journalVoucherHeader = journalVoucherHeader
                    .Where(s =>
                        s.JournalVoucherHeaderNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.References?.Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo.Contains(searchValue) == true ||
                        s.Particulars.ToLower().Contains(searchValue) == true ||
                        s.CRNo?.ToLower().Contains(searchValue) == true ||
                        s.JVReason.ToLower().ToString().Contains(searchValue) ||
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

            viewModel.Header.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Header.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Where(c => c.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JournalVoucherVM? model, CancellationToken cancellationToken, string[] accountNumber, decimal[]? debit, decimal[]? credit)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Header.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Header.CheckVoucherHeaders = await _dbContext.FilprideCheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Where(c => c.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --CV Details Entry

                    var generateJVNo = await _unitOfWork.FilprideJournalVoucher.GenerateCodeAsync(companyClaims, cancellationToken);
                    var cvDetails = new List<FilprideJournalVoucherDetail>();

                    var totalDebit = 0m;
                    var totalCredit = 0m;
                    if (totalDebit != totalCredit)
                    {
                        TempData["error"] = "The debit and credit should be equal!";
                        return View(model);
                    }

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    //JV Header Entry
                    model.Header.JournalVoucherHeaderNo = generateJVNo;
                    model.Header.CreatedBy = _userManager.GetUserName(this.User);
                    model.Header.Company = companyClaims;

                    #endregion --Saving the default entries

                    await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    for (int i = 0; i < accountNumber.Length; i++)
                    {
                        var currentAccountNumber = accountNumber[i];
                        var accountTitle = await _dbContext.FilprideChartOfAccounts
                            .FirstOrDefaultAsync(coa => coa.AccountNumber == currentAccountNumber);
                        var currentDebit = debit[i];
                        var currentCredit = credit[i];
                        totalDebit += debit[i];
                        totalCredit += credit[i];

                        cvDetails.Add(
                            new FilprideJournalVoucherDetail
                            {
                                AccountNo = currentAccountNumber,
                                AccountName = accountTitle.AccountName,
                                TransactionNo = generateJVNo,
                                JournalVoucherHeaderId = model.Header.JournalVoucherHeaderId,
                                Debit = currentDebit,
                                Credit = currentCredit
                            }
                        );
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.Header.CreatedBy, $"Created new journal voucher# {model.Header.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, model.Header.Company);
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
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> GetCV(int id)
        {
            var header = _dbContext.FilprideCheckVoucherHeaders
                .Include(s => s.Supplier)
                .FirstOrDefault(cvh => cvh.CheckVoucherHeaderId == id);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideCheckVoucherDetails
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
                var name = viewModel.Header.Supplier.SupplierName;
                var address = viewModel.Header.Supplier.SupplierAddress;
                var tinNo = viewModel.Header.Supplier.SupplierTin;
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

            var header = await _dbContext.FilprideJournalVoucherHeaders
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier.Supplier)
                .FirstOrDefaultAsync(jvh => jvh.JournalVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideJournalVoucherDetails
                .Where(jvd => jvd.JournalVoucherHeaderId == header.JournalVoucherHeaderId)
                .ToListAsync(cancellationToken);

            //if (header.Category == "Trade")
            //{
            //    var siArray = new string[header.RRNo.Length];
            //    for (int i = 0; i < header.RRNo.Length; i++)
            //    {
            //        var rrValue = header.RRNo[i];

            //        var rr = await _dbContext.FilprideReceivingReports
            //                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

            //        siArray[i] = rr.SupplierInvoiceNumber;
            //    }

            //    ViewBag.SINoArray = siArray;
            //}

            var viewModel = new JournalVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(id, cancellationToken);
            var modelDetails = await _dbContext.FilprideJournalVoucherDetails.Where(jvd => jvd.JournalVoucherHeaderId == modelHeader.JournalVoucherHeaderId).ToListAsync();

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

                        var ledgers = new List<FilprideGeneralLedgerBook>();
                        foreach (var details in modelDetails)
                        {
                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.JournalVoucherHeaderNo,
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
                                        Reference = modelHeader.JournalVoucherHeaderNo,
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

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(modelHeader.PostedBy, $"Posted journal voucher# {modelHeader.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, modelHeader.Company);
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
            var model = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(id, cancellationToken);

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

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, model.Company);
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
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(id, cancellationToken);

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
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders
                .Include(jv => jv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);
            var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails
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
            var poIds = _dbContext.FilpridePurchaseOrders.Where(model => model.Company == companyClaims && existingHeaderModel.CheckVoucherHeader.PONo.Contains(model.PurchaseOrderNo)).Select(model => model.PurchaseOrderId).ToArray();
            var rrIds = _dbContext.FilprideReceivingReports.Where(model => model.Company == companyClaims && existingHeaderModel.CheckVoucherHeader.RRNo.Contains(model.ReceivingReportNo)).Select(model => model.ReceivingReportId).ToArray();

            var coa = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

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
                .Where(c => c.Company == companyClaims)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken),
                COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(JournalVoucherViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --CV Details Entry

                    var existingHeaderModel = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(viewModel.JVId, cancellationToken);
                    var existingDetailsModel = await _dbContext.FilprideJournalVoucherDetails.Where(d => d.JournalVoucherHeaderId == existingHeaderModel.JournalVoucherHeaderId).ToListAsync();

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

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = viewModel.JVNo;
                            details.JournalVoucherHeaderId = viewModel.JVId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new FilprideJournalVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = viewModel.JVNo,
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
                    //existingHeaderModel.CVId = viewModel.CVId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.CRNo = viewModel.CRNo;
                    existingHeaderModel.JVReason = viewModel.JVReason;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(this.User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy, $"Edited journal voucher# {existingHeaderModel.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, existingHeaderModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher edited successfully";
                    return RedirectToAction(nameof(Index));
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
            var cv = await _unitOfWork.FilprideJournalVoucher.GetAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);
            if (cv?.IsPrinted == false)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of journal voucher# {cv.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress, cv.Company);
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

                    row++;
                }

                #endregion -- Journal Voucher Header Export --

                #region -- Check Vocher Header Export (Trade and Invoicing) --

                    int cvhRow = 2;
                    var currentCVTradeAndInvoicing = "";

                    foreach (var item in selectedList)
                    {
                        if (item.CheckVoucherHeader == null)
                        {
                            continue;
                        }
                        if (item.CheckVoucherHeader.CheckVoucherHeaderNo == currentCVTradeAndInvoicing)
                        {
                            continue;
                        }

                        currentCVTradeAndInvoicing = item.CheckVoucherHeader.CheckVoucherHeaderNo;
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

                        cvhRow++;
                    }

                #endregion -- Check Vocher Header Export (Trade and Invoicing) --

                #region -- Check Vocher Header Export (Payment) --

                    var cvNos = selectedList.Select(item => item.CheckVoucherHeader.CheckVoucherHeaderNo).ToList();
                    var currentCVPayment = "";

                    var checkVoucherPayment = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cvh => cvh.Reference != null && cvNos.Contains(cvh.Reference))
                        .ToListAsync();

                    foreach (var item in checkVoucherPayment)
                    {
                        if (item.CheckVoucherHeaderNo == currentCVPayment)
                        {
                            continue;
                        }

                        currentCVPayment = item.CheckVoucherHeaderNo;
                        worksheet5.Cells[cvhRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        if (item.RRNo != null && !item.RRNo.Contains(null))
                        {
                            worksheet5.Cells[cvhRow, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                        }
                        if (item.SINo != null && !item.SINo.Contains(null))
                        {
                            worksheet5.Cells[cvhRow, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                        }
                        if (item.PONo != null && !item.PONo.Contains(null))
                        {
                            worksheet5.Cells[cvhRow, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                        }

                        worksheet5.Cells[cvhRow, 5].Value = item.Particulars;
                        worksheet5.Cells[cvhRow, 6].Value = item.CheckNo;
                        worksheet5.Cells[cvhRow, 7].Value = item.Category;
                        worksheet5.Cells[cvhRow, 8].Value = item.Payee;
                        worksheet5.Cells[cvhRow, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                        worksheet5.Cells[cvhRow, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                        worksheet5.Cells[cvhRow, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                        worksheet5.Cells[cvhRow, 12].Value = item.NumberOfMonths;
                        worksheet5.Cells[cvhRow, 13].Value = item.NumberOfMonthsCreated;
                        worksheet5.Cells[cvhRow, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet5.Cells[cvhRow, 15].Value = item.AmountPerMonth;
                        worksheet5.Cells[cvhRow, 16].Value = item.IsComplete;
                        worksheet5.Cells[cvhRow, 17].Value = item.AccruedType;
                        worksheet5.Cells[cvhRow, 18].Value = item.Reference;
                        worksheet5.Cells[cvhRow, 19].Value = item.CreatedBy;
                        worksheet5.Cells[cvhRow, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet5.Cells[cvhRow, 21].Value = item.Total;
                        if (item.Amount != null)
                        {
                            worksheet5.Cells[cvhRow, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                        }
                        worksheet5.Cells[cvhRow, 23].Value = item.CheckAmount;
                        worksheet5.Cells[cvhRow, 24].Value = item.CvType;
                        worksheet5.Cells[cvhRow, 25].Value = item.AmountPaid;
                        worksheet5.Cells[cvhRow, 26].Value = item.IsPaid;
                        worksheet5.Cells[cvhRow, 27].Value = item.CancellationRemarks;
                        worksheet5.Cells[cvhRow, 28].Value = item.BankId;
                        worksheet5.Cells[cvhRow, 29].Value = item.CheckVoucherHeaderNo;
                        worksheet5.Cells[cvhRow, 30].Value = item.SupplierId;
                        worksheet5.Cells[cvhRow, 31].Value = item.CheckVoucherHeaderId;

                        cvhRow++;
                    }

                #endregion -- Check Vocher Header Export (Payment) --

                #region -- Journal Voucher Details Export --

                var jvNos = selectedList.Select(item => item.JournalVoucherHeaderNo).ToList();

                var getJVDetails = await _dbContext.FilprideJournalVoucherDetails
                    .Where(jvd => jvNos.Contains(jvd.TransactionNo))
                    .OrderBy(jvd => jvd.JournalVoucherDetailId)
                    .ToListAsync();

                int jvdRow = 2;

                foreach (var item in getJVDetails)
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

                #region -- Check Voucher Details Export (Trade and Invoicing) --

                List<FilprideCheckVoucherDetail> getCVDetails = new();

                getCVDetails = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => selectedList.Select(jvh => jvh.CheckVoucherHeader.CheckVoucherHeaderNo).Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                int cvdRow = 2;

                foreach (var item in getCVDetails)
                {
                    worksheet6.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet6.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet6.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet6.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet6.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet6.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet6.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;

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
                    worksheet6.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet6.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet6.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet6.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet6.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet6.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet6.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Payment) --

                #region -- Receving Report Export --

                List<FilprideReceivingReport> getReceivingReport = new List<FilprideReceivingReport>();

                getReceivingReport = _dbContext.FilprideReceivingReports
                    .AsEnumerable()
                    .Where(rr => selectedList?.Select(item => item?.CheckVoucherHeader?.RRNo).Any(rrs => rrs?.Contains(rr.ReceivingReportNo) == true) == true)
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

                #endregion -- Receving Report Export --

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

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "JournalVoucherList.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllJournalVoucherIds()
        {
            var jvIds = _dbContext.FilprideJournalVoucherHeaders
                                     .Where(jv => jv.Type == nameof(DocumentType.Documented))
                                     .Select(jv => jv.JournalVoucherHeaderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(jvIds);
        }
    }
}
