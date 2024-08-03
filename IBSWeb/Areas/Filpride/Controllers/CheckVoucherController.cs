﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class CheckVoucherController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        public CheckVoucherController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.FilprideCheckVoucherHeaders
                .Include(s => s.Supplier)
                .ToListAsync(cancellationToken);

            var details = await _dbContext.FilprideCheckVoucherDetails
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objects
            var checkVoucherVMs = new List<CheckVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerDetails = details.Where(d => d.TransactionNo == header.CheckVoucherHeaderNo).ToList();

                if (header.Category == "Trade" && header.RRNo != null)
                {
                    var siArray = new string[header.RRNo.Length];
                    for (int i = 0; i < header.RRNo.Length; i++)
                    {
                        var rrValue = header.RRNo[i];

                        var rr = await _dbContext.ReceivingReports
                                    .FirstOrDefaultAsync(p => p.ReceivingReportNo == rrValue);
                        if (rr != null)
                        {
                            siArray[i] = rr.SupplierInvoiceNumber;
                        }
                    }

                    ViewBag.SINoArray = siArray;
                }
                // Create a new CheckVoucherVM object for each header and its associated details
                var checkVoucherVM = new CheckVoucherVM
                {
                    Header = header,
                    Details = headerDetails
                };

                // Add the CheckVoucherVM object to the list
                checkVoucherVMs.Add(checkVoucherVM);
            }

            return View(checkVoucherVMs);
        }

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId && po.PostedBy != null)
                .ToListAsync();

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, string? criteria)
        {
            var receivingReports = await _dbContext.ReceivingReports
            .Where(rr => poNumber.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
            .OrderBy(rr => criteria == "Transaction Date" ? rr.Date : rr.DueDate)
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
                        SupplierName = supplier.SupplierName,
                        SupplierAddress = supplier.SupplierAddress,
                        SupplierTinNo = supplier.SupplierTin,
                        TaxType = supplier.TaxType,
                        ///PENDING CV
                        //Category = supplier.Category,
                        //TaxPercent = supplier.WithholdingTaxPercent,
                        //VatType = supplier.VatType,
                        //DefaultExpense = supplier.DefaultExpenseNumber,
                        //WithholdingTax = supplier.WithholdingTaxtitle
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        public async Task<IActionResult> RRBalance(string rrNo)
        {
            var receivingReport = await _dbContext.ReceivingReports
                .FirstOrDefaultAsync(rr => rr.ReceivingReportNo == rrNo);
            if (receivingReport != null)
            {
                var amount = receivingReport.Amount;
                var amountPaid = receivingReport.AmountPaid;
                var netAmount = receivingReport.NetAmount;
                var vatAmount = receivingReport.VatAmount;
                var ewtAmount = receivingReport.EwtAmount;
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

        public async Task<IActionResult> GetBankAccount(int bankId)
        {
            if (bankId != 0)
            {
                var existingBankAccount = await _dbContext.FilprideBankAccounts.FindAsync(bankId);
                return Json(new { AccountNoCOA = existingBankAccount.AccountNoCOA, AccountNo = existingBankAccount.AccountNo, AccountName = existingBankAccount.AccountName });
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

            var header = await _dbContext.FilprideCheckVoucherHeaders
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == header.CheckVoucherHeaderNo)
                .ToListAsync(cancellationToken);

            if (header.Category == "Trade" && header.RRNo != null)
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.ReceivingReports
                                .FirstOrDefaultAsync(p => p.ReceivingReportNo == rrValue);

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

        public async Task<IActionResult> Post(int cvId, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(cvId, cancellationToken);
            var modelDetails = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.TransactionNo == modelHeader.CheckVoucherHeaderNo).ToListAsync();

            if (modelHeader != null)
            {
                try
                {
                    if (modelHeader.PostedBy == null)
                    {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTime.Now;

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
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        if (_unitOfWork.FilprideCheckVoucher.IsJournalEntriesBalanced(ledgers))
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
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        await _dbContext.FilprideDisbursementBooks.AddRangeAsync(disbursement, cancellationToken);

                        #endregion --Disbursement Book Recording(CV)--

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("Index");
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
            var exisitngCV = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);
            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(supp => supp.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);
            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == existingHeaderModel.CheckVoucherHeaderNo)
                .ToListAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();
            var poIds = _dbContext.PurchaseOrders.Where(model => exisitngCV.PONo.Contains(model.PurchaseOrderNo)).Select(model => model.PurchaseOrderId).ToArray();
            var rrIds = _dbContext.ReceivingReports.Where(model => exisitngCV.RRNo.Contains(model.ReceivingReportNo)).Select(model => model.ReceivingReportId).ToArray();

            var coa = await _dbContext.ChartOfAccounts
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
                Suppliers = await _unitOfWork.GetFilprideSupplierListAsyncById(cancellationToken),
                RRSeries = existingHeaderModel.RRNo,
                ///PENDING
                //RR = await _generalRepo.GetReceivingReportListAsync(existingHeaderModel.RRNo, cancellationToken),
                POSeries = existingHeaderModel.PONo,
                //PONo = await _generalRepo.GetPurchaseOrderListAsync(cancellationToken),
                TransactionDate = existingHeaderModel.Date,
                //BankAccounts = await _generalRepo.GetBankAccountListAsync(cancellationToken),
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
                CVId = exisitngCV.CheckVoucherHeaderId,
                CVNo = exisitngCV.CheckVoucherHeaderNo,
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
                try
                {
                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(viewModel.CVId, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .FilprideCheckVoucherHeaders
                        .Where(cv => cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo))
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.TransactionNo == existingHeaderModel.CheckVoucherHeaderNo).ToListAsync();

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
                        cashInBank = viewModel.Credit[3];

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
                                TransactionNo = viewModel.CVNo
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
                        var receivingReport = new ReceivingReport();
                        for (int i = 0; i < viewModel.RRSeries.Length; i++)
                        {
                            var rrValue = viewModel.RRSeries[i];
                            receivingReport = await _dbContext.ReceivingReports
                                        .FirstOrDefaultAsync(p => p.ReceivingReportNo == rrValue);

                            if (i < existingHeaderModel.Amount.Length)
                            {
                                var amount = Math.Round(viewModel.Amount[i] - existingHeaderModel.Amount[i], 2);
                                receivingReport.AmountPaid += amount;
                            }
                            else
                            {
                                receivingReport.AmountPaid += viewModel.Amount[i];
                            }

                            if (receivingReport.Amount <= receivingReport.AmountPaid)
                            {
                                receivingReport.IsPaid = true;
                                receivingReport.PaidDate = DateTime.Now;
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
                    existingHeaderModel.CreatedBy = viewModel.CreatedBy;

                    #endregion --Saving the default entries

                    #region -- Uploading file --

                    /// PENDING
                    //if (file != null && file.Length > 0)
                    //{
                    //    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", viewModel.CVNo);

                    //    if (!Directory.Exists(uploadsFolder))
                    //    {
                    //        Directory.CreateDirectory(uploadsFolder);
                    //    }

                    //    string fileName = Path.GetFileName(file.FileName);
                    //    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    //    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    //    {
                    //        await file.CopyToAsync(stream);
                    //    }

                    //    //if necessary add field to store location path
                    //    // model.Header.SupportingFilePath = fileSavePath
                    //}

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Trade edited successfully";
                    return RedirectToAction("Index");
                    #endregion -- Uploading file --
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

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

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

                    ///PENDING
                    //await _generalRepo.RemoveRecords<DisbursementBook>(db => db.CVNo == model.CVNo);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CVNo);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Trade(CancellationToken cancellationToken)
        {
            CheckVoucherTradeViewModel model = new();
            model.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            ///PENDING
            //model.Suppliers = await _dbContext.FilprideSuppliers
            //    .Where(supp => supp.Category == "Trade")
            //    .Select(sup => new SelectListItem
            //    {
            //        Value = sup.Id.ToString(),
            //        Text = sup.Name
            //    })
            //    .ToListAsync();
            model.BankAccounts = await _dbContext.FilprideBankAccounts
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
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Check if duplicate record
                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .FilprideCheckVoucherHeaders
                        .Where(cv => cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId)
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            viewModel.COA = await _dbContext.ChartOfAccounts
                                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.AccountNumber,
                                    Text = s.AccountNumber + " " + s.AccountName
                                })
                                .ToListAsync(cancellationToken);

                            ///PENDING
                            //viewModel.Suppliers = await _dbContext.FilprideSuppliers
                            //    .Where(supp => supp.Category == "Trade")
                            //    .Select(sup => new SelectListItem
                            //    {
                            //        Value = sup.Id.ToString(),
                            //        Text = sup.Name
                            //    })
                            //    .ToListAsync();

                            viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.RR = await _dbContext.ReceivingReports
                                .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                                .Select(rr => new SelectListItem
                                {
                                    Value = rr.ReceivingReportNo.ToString(),
                                    Text = rr.ReceivingReportNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
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

                    #region --CV Details Entry
                    var generateCVNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(cancellationToken);
                    var cvDetails = new List<FilprideCheckVoucherDetail>();
                    var cashInBank = 0m;
                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            cashInBank = viewModel.Credit[3];
                            cvDetails.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = generateCVNo
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);
                    #endregion --CV Details Entry

                    #region --Saving the default entries
                    var cvh = new List<FilprideCheckVoucherHeader>();
                    cvh.Add(
                            new FilprideCheckVoucherHeader
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
                                CreatedBy = _userManager.GetUserName(this.User)
                            }
                    );

                    await _dbContext.FilprideCheckVoucherHeaders.AddRangeAsync(cvh, cancellationToken);

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's
                    if (viewModel.Amount != null)
                    {
                        var receivingReport = new ReceivingReport();
                        for (int i = 0; i < viewModel.RRSeries.Length; i++)
                        {
                            var rrValue = viewModel.RRSeries[i];
                            receivingReport = await _dbContext.ReceivingReports
                                        .FirstOrDefaultAsync(p => p.ReceivingReportNo == rrValue);

                            receivingReport.AmountPaid += viewModel.Amount[i];

                            if (receivingReport.Amount <= receivingReport.AmountPaid)
                            {
                                receivingReport.IsPaid = true;
                                receivingReport.PaidDate = DateTime.Now;
                            }
                        }
                    }

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --
                    foreach (var item in cvh.ToList())
                    {
                        ///PENDING
                        //if (file != null && file.Length > 0)
                        //{
                        //    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", item.CVNo);

                        //    if (!Directory.Exists(uploadsFolder))
                        //    {
                        //        Directory.CreateDirectory(uploadsFolder);
                        //    }

                        //    string fileName = Path.GetFileName(file.FileName);
                        //    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        //    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        //    {
                        //        await file.CopyToAsync(stream);
                        //    }

                        //    //if necessary add field to store location path
                        //    // model.Header.SupportingFilePath = fileSavePath
                        //}
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    return RedirectToAction("Index");
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    /// PENDING
                    //viewModel.Suppliers = await _dbContext.FilprideSuppliers
                    //        .Where(supp => supp.Category == "Trade")
                    //        .Select(sup => new SelectListItem
                    //        {
                    //            Value = sup.Id.ToString(),
                    //            Text = sup.Name
                    //        })
                    //        .ToListAsync();

                    viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                    viewModel.RR = await _dbContext.ReceivingReports
                        .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                        .Select(rr => new SelectListItem
                        {
                            Value = rr.ReceivingReportNo.ToString(),
                            Text = rr.ReceivingReportNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync();

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            /// PENDING
            //viewModel.Suppliers = await _dbContext.Suppliers
            //    .Where(supp => supp.Category == "Trade")
            //    .Select(sup => new SelectListItem
            //    {
            //        Value = sup.Id.ToString(),
            //        Text = sup.Name
            //    })
            //    .ToListAsync();

            viewModel.PONo = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.RR = await _dbContext.ReceivingReports
                .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.PostedBy != null)
                .Select(rr => new SelectListItem
                {
                    Value = rr.ReceivingReportNo.ToString(),
                    Text = rr.ReceivingReportNo
                })
                .ToListAsync(cancellationToken);

            viewModel.BankAccounts = await _dbContext.FilprideBankAccounts
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

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            ///PENDING
            //viewModel.Suppliers = await _dbContext.FilprideSuppliers
            //    .Where(supp => supp.Category == "Non-Trade")
            //    .Select(sup => new SelectListItem
            //    {
            //        Value = sup.Id.ToString(),
            //        Text = sup.Name
            //    })
            //    .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region -- Saving the default entries --

                    FilprideCheckVoucherHeader checkVoucherHeader = new()
                    {
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = viewModel.SupplierName,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Invoicing"
                    };

                    #endregion -- Saving the default entries --

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
                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                                break;
                            }
                        }

                        if (amount.HasValue)
                        {
                            checkVoucherHeader.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

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
                        ///PENDING
                        //string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CVNo);

                        //if (!Directory.Exists(uploadsFolder))
                        //{
                        //    Directory.CreateDirectory(uploadsFolder);
                        //}

                        //string fileName = Path.GetFileName(file.FileName);
                        //string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        //using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        //{
                        //    await file.CopyToAsync(stream);
                        //}

                        ////if necessary add field to store location path
                        //// model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    ///PENDING
                    //viewModel.Suppliers = await _dbContext.FilprideSuppliers
                    //    .Where(supp => supp.Category == "Non-Trade")
                    //    .Select(sup => new SelectListItem
                    //    {
                    //        Value = sup.Id.ToString(),
                    //        Text = sup.Name
                    //    })
                    //    .ToListAsync();

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            /// PENDING
            //viewModel.Suppliers = await _dbContext.FilprideSuppliers
            //    .Where(supp => supp.Category == "Non-Trade")
            //    .Select(sup => new SelectListItem
            //    {
            //        Value = sup.Id.ToString(),
            //        Text = sup.Name
            //    })
            //    .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> NonTradePayment(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.FilprideBankAccounts
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
            if (ModelState.IsValid)
            {
                try
                {
                    #region--Get Check Voucher Invoicing

                    var invoicingVoucher = await _dbContext.FilprideCheckVoucherHeaders
                        .FindAsync(viewModel.CvId, cancellationToken);

                    #endregion

                    #region--Saving the default entries

                    FilprideCheckVoucherHeader checkVoucherHeader = new()
                    {
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(cancellationToken),
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
                        CheckAmount = viewModel.Total
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

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
                        /// PENDING
                        //string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CVNo);

                        //if (!Directory.Exists(uploadsFolder))
                        //{
                        //    Directory.CreateDirectory(uploadsFolder);
                        //}

                        //string fileName = Path.GetFileName(file.FileName);
                        //string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        //using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        //{
                        //    await file.CopyToAsync(stream);
                        //}

                        ////if necessary add field to store location path
                        //// model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region--Update invoicing voucher

                    await _unitOfWork.FilprideCheckVoucher.UpdateInvoicingVoucher(checkVoucherHeader.Total, viewModel.CvId, cancellationToken);

                    #endregion

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync();

                    viewModel.Banks = await _dbContext.FilprideBankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync();

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.FilprideBankAccounts
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

                if (cv != null)
                {
                    return Json(new
                    {
                        Payee = cv.Supplier.SupplierName,
                        PayeeAddress = cv.Supplier.SupplierAddress,
                        PayeeTin = cv.Supplier.SupplierTin,
                        Total = cv.Total
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> EditNonTradeInvoicing(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CheckVoucherHeaderNo).ToListAsync();

            existingModel.Suppliers = await _dbContext.FilprideSuppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            existingModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

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
                Debit = debit,
                Credit = credit
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditNonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Saving the default entries

                    var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                    if (existingModel != null)
                    {
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

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CheckVoucherHeaderNo).ToListAsync();

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

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingModel.CheckVoucherHeaderNo;

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
                                TransactionNo = existingModel.CheckVoucherHeaderNo
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
                        ///PENDING
                        //string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingModel.CVNo);

                        //if (!Directory.Exists(uploadsFolder))
                        //{
                        //    Directory.CreateDirectory(uploadsFolder);
                        //}

                        //string fileName = Path.GetFileName(file.FileName);
                        //string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        //using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        //{
                        //    await file.CopyToAsync(stream);
                        //}

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Non-trade invoicing edited successfully";
                    return RedirectToAction("Index");
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _dbContext.FilprideSuppliers
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
            var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CheckVoucherHeaderNo).ToListAsync(cancellationToken);
            var invoicing = await _dbContext.FilprideCheckVoucherHeaders.FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderNo == existingModel.Reference, cancellationToken);

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            #region -- insert fetch data into viewModel --

            CheckVoucherNonTradePaymentViewModel viewModel = new()
            {
                CvId = invoicing.CheckVoucherHeaderId,
                CVId = existingModel.CheckVoucherHeaderId,
                TransactionDate = existingModel.Date,
                Payee = existingModel.Payee,
                PayeeAddress = existingModel.Supplier.SupplierAddress,
                PayeeTin = existingModel.Supplier.SupplierTin,
                Total = existingModel.Total,
                BankId = existingModel.BankId ?? 0,
                CheckNo = existingModel.CheckNo,
                CheckDate = existingModel.CheckDate ?? default,
                Particulars = existingModel.Particulars,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,

                CheckVouchers = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && cvh.PostedBy != null)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken),

                Banks = await _dbContext.FilprideBankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken),

                ChartOfAccounts = await _dbContext.ChartOfAccounts
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
                try
                {
                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);
                    var invoicing = await _dbContext.FilprideCheckVoucherHeaders.FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderNo == existingHeaderModel.Reference, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .FilprideCheckVoucherHeaders
                        .Where(cv => cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo))
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.TransactionNo == existingHeaderModel.CheckVoucherHeaderNo).ToListAsync();

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
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo
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

                    existingHeaderModel.Reference = invoicing.CheckVoucherHeaderNo;
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
                    existingHeaderModel.CreatedBy = _userManager.GetUserName(this.User);

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's
                    //if (viewModel.Amount != null)
                    //{
                    //    var receivingReport = new ReceivingReport();
                    //    for (int i = 0; i < viewModel.RRSeries.Length; i++)
                    //    {
                    //        var rrValue = viewModel.RRSeries[i];
                    //        receivingReport = await _dbContext.ReceivingReports
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
                        ///PENDING
                        //string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingHeaderModel.CVNo);

                        //if (!Directory.Exists(uploadsFolder))
                        //{
                        //    Directory.CreateDirectory(uploadsFolder);
                        //}

                        //string fileName = Path.GetFileName(file.FileName);
                        //string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        //using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        //{
                        //    await file.CopyToAsync(stream);
                        //}

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Non-trade payment edited successfully";
                    return RedirectToAction("Index");
                    #endregion -- Uploading file --
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
    }
}