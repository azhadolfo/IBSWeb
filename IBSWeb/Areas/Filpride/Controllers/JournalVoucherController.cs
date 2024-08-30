using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

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

        public IActionResult Index()
        {
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
                        s.Date.ToString().Contains(searchValue) ||
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

            viewModel.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
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

            model.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
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
                    var accountTitle = await _dbContext.ChartOfAccounts
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

                await _dbContext.AddRangeAsync(cvDetails, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Journal voucher created successfully";

                return RedirectToAction(nameof(Index));
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
                try
                {
                    if (modelHeader.PostedBy == null)
                    {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTime.Now;
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

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("Index");
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;
                    model.Status = nameof(Status.Voided);

                    await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideJournalBook>(crb => crb.Reference == model.JournalVoucherHeaderNo);
                    await _unitOfWork.FilprideJournalVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.JournalVoucherHeaderNo);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideJournalVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = nameof(Status.Canceled);

                    ///PENDING - leo
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Cancelled.";
                }
                return RedirectToAction("Index");
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

            var coa = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
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
                COA = await _dbContext.ChartOfAccounts
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
                    existingHeaderModel.CreatedBy = _userManager.GetUserName(this.User);

                    #endregion --Saving the default entries

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Journal Voucher edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
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

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of jv# {jv.CVNo}", "Check Vouchers");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}