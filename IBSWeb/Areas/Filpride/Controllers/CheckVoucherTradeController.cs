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
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class CheckVoucherTradeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherTradeController> _logger;

        public CheckVoucherTradeController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherTradeController> logger)
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.CheckVoucher))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherHeaders = await _unitOfWork.FilprideCheckVoucher
                    .GetAllAsync(cv => cv.Company == companyClaims && cv.Type == nameof(DocumentType.Documented) && cv.CvType != "Payment", cancellationToken);

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
                    .GetAllAsync(cv => cv.Company == companyClaims && cv.Category == "Trade", cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                        .Where(s =>
                            s.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                            s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                            s.Total.ToString().Contains(searchValue) ||
                            s.Amount?.ToString()?.Contains(searchValue) == true ||
                            s.Category.ToLower().Contains(searchValue) ||
                            s.CvType?.ToLower().Contains(searchValue) == true ||
                            s.CreatedBy!.ToLower().Contains(searchValue)
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
                _logger.LogError(ex, "Failed to get check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            CheckVoucherTradeViewModel model = new()
            {
                Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken),
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.PONo = (await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(
                        po => po.Company == companyClaims && po.SupplierId == viewModel.SupplierId &&
                              po.PostedBy != null, cancellationToken))
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo!.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToList();
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Check if duplicate record

                if (!viewModel.CheckNo.Contains("DM"))
                {
                    var cv = await _unitOfWork.FilprideCheckVoucher
                        .GetAllAsync(cv => cv.Company == companyClaims && cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId, cancellationToken);

                    if (cv.Any())
                    {
                        viewModel.COA = (await _unitOfWork.FilprideChartOfAccount
                                .GetAllAsync(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren, cancellationToken))
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToList();

                        viewModel.Suppliers = (await _unitOfWork.FilprideSupplier
                                .GetAllAsync(supp => (companyClaims == nameof(Filpride) ? supp.IsFilpride : supp.IsMobility) && supp.Category == "Trade", cancellationToken))
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToList();

                        viewModel.PONo = (await _unitOfWork.FilpridePurchaseOrder
                                .GetAllAsync(po => po.Company == companyClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null, cancellationToken))
                            .Select(po => new SelectListItem
                            {
                                Value = po.PurchaseOrderNo!.ToString(),
                                Text = po.PurchaseOrderNo
                            })
                            .ToList();

                        viewModel.BankAccounts = (await _unitOfWork.FilprideBankAccount
                                .GetAllAsync(b => (companyClaims == nameof(Filpride) ? b.IsFilpride : b.IsMobility), cancellationToken))
                            .Select(ba => new SelectListItem
                            {
                                Value = ba.BankAccountId.ToString(),
                                Text = ba.AccountNo + " " + ba.AccountName
                            })
                            .ToList();

                        TempData["info"] = "Check No. already exists";
                        return View(viewModel);
                    }
                }

                #endregion --Check if duplicate record

                #region -- Get PO --

                var getPurchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(po => viewModel.POSeries!.Contains(po.PurchaseOrderNo), cancellationToken);

                if (getPurchaseOrder == null)
                {
                    return NotFound();
                }

                #endregion -- Get PO --

                #region --Saving the default entries

                var generateCvNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, viewModel.Type!, cancellationToken);
                var cashInBank = viewModel.Credit[1];

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

                var cvh = new FilprideCheckVoucherHeader
                {
                    Type = viewModel.Type!,
                    CheckVoucherHeaderNo = generateCvNo,
                    Date = viewModel.TransactionDate,
                    PONo = viewModel.POSeries,
                    SupplierId = viewModel.SupplierId,
                    SupplierName = supplier.SupplierName,
                    Particulars = $"{viewModel.Particulars} {(viewModel.AdvancesCVNo != null ? "Advances#" + viewModel.AdvancesCVNo : "")}.",
                    Reference = viewModel.AdvancesCVNo,
                    BankId = viewModel.BankId,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    CheckNo = viewModel.CheckNo,
                    Category = "Trade",
                    Payee = viewModel.Payee,
                    CheckDate = viewModel.CheckDate,
                    Total = cashInBank,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    CvType = "Supplier",
                    Address = supplier.SupplierAddress,
                    Tin = supplier.SupplierTin,
                    OldCvNo = viewModel.OldCVNo,
                    VatType = supplier.VatType,
                    TaxType = supplier.TaxType
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(cvh, cancellationToken);

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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 1 ? viewModel.BankId : null,
                            });
                    }
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Partial payment of RR's

                var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.RRs)
                {
                    var getReceivingReport = await _unitOfWork.FilprideReceivingReport.GetAsync(x => x.ReceivingReportId == item.Id, cancellationToken);
                    if (getReceivingReport != null)
                    {
                        getReceivingReport.AmountPaid += item.Amount;
                        cvh.TaxPercent = getReceivingReport.TaxPercentage;

                        cvTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getReceivingReport.ReceivingReportId,
                            DocumentType = "RR",
                            CheckVoucherId = cvh.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                    }
                }

                await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);

                #endregion -- Partial payment of RR's

                #region -- Additional journal entry in details

                var ewtTitle = supplier.WithholdingTaxTitle?.Split(' ',2) ?? [];

                var getWithholdingTaxTitle = await _dbContext.FilprideChartOfAccounts
                    .FirstOrDefaultAsync(x => x.AccountNumber == ewtTitle.FirstOrDefault(), cancellationToken);

                foreach (var cv in cvDetails.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = cvh.VatType == SD.VatType_Vatable;
                    var isTaxable = cvh.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = cvh.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - cvh.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - cvh.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * cvh.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = cv.AccountNo,
                        AccountName = cv.AccountName,
                        Debit = baseAmount,
                        Credit = 0.00m,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        SupplierId = viewModel.SupplierId,
                        IsDisplayEntry = true
                    });

                    if (inputVat != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101060200",
                            AccountName = "Vat - Input",
                            Debit = inputVat,
                            Credit = 0.00m,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    if (ewt != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = getWithholdingTaxTitle!.AccountNumber ?? string.Empty,
                            AccountName = getWithholdingTaxTitle.AccountName,
                            Debit = 0.00m,
                            Credit = ewt,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = "101010100",
                        AccountName = "Cash in Bank",
                        Debit = 0.00m,
                        Credit = netOfEwt,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        BankId = viewModel.BankId,
                        IsDisplayEntry = true
                    });

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                #endregion

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName!);
                }


                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy!, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", cvh.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Check voucher trade #{cvh.CheckVoucherHeaderNo} created successfully";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Include(x => x.ReceivingReports)
                .Where(po => po.SupplierId == supplierId
                             && po.PostedBy != null
                             && po.QuantityReceived > 0
                             && po.Company == companyClaims
                             && po.ReceivingReports != null
                             && po.ReceivingReports.Any(x => !x.IsPaid))
                .ToListAsync();

            if (!purchaseOrders.Any())
            {
                return Json(null);
            }

            var poList = purchaseOrders.Where(p => !p.IsSubPo)
                .OrderBy(po => po.PurchaseOrderNo)
                .Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo })
                .ToList();
            return Json(poList);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, int? cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var query = _dbContext.FilprideReceivingReports
                .Where(rr => rr.Company == companyClaims && !rr.IsPaid
                                                         && rr.AmountPaid == 0
                                                         && poNumber.Contains(rr.PONo)
                                                         && rr.PostedBy != null);

            if (cvId != null)
            {
                var rrIds = await _dbContext.FilprideCVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "RR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.FilprideReceivingReports
                    .Where(rr => poNumber.Contains(rr.PONo) && rrIds.Contains(rr.ReceivingReportId)));
            }

            var receivingReports = await query
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(rr => rr!.Supplier)
                .OrderBy(rr => rr.PurchaseOrder!.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            if (!receivingReports.Any())
            {
                return Json(null);
            }

            var rrList = receivingReports
                .Select(rr =>
                {
                    var netOfVatAmount = rr.PurchaseOrder?.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(rr.Amount)
                        : rr.Amount;

                    var ewtAmount = rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                        ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, rr.TaxPercentage)
                        : 0.0000m;

                    var netOfEwtAmount = rr.PurchaseOrder?.TaxType == SD.TaxType_WithTax
                        ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(rr.Amount, ewtAmount)
                        : rr.Amount;

                    return new
                    {
                        Id = rr.ReceivingReportId,
                        rr.ReceivingReportNo,
                        rr.PurchaseOrder?.PurchaseOrderNo,
                        rr.OldRRNo,
                        AmountPaid = rr.AmountPaid.ToString(SD.Four_Decimal_Format),
                        NetOfEwtAmount = netOfEwtAmount.ToString(SD.Four_Decimal_Format)
                    };
                }).ToList();

            return Json(rrList);
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (supplierId == null)
            {
                return Json(null);
            }

            var supplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId && (companyClaims == nameof(Filpride) ? s.IsFilpride : s.IsMobility));

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

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);
                if (existingHeaderModel.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                CheckVoucherTradeViewModel model = new()
                {
                    SupplierId = existingHeaderModel.SupplierId ?? 0,
                    Payee = existingHeaderModel.Payee!,
                    SupplierAddress = existingHeaderModel.Address,
                    SupplierTinNo = existingHeaderModel.Tin,
                    POSeries = existingHeaderModel.PONo,
                    TransactionDate = existingHeaderModel.Date,
                    BankId = existingHeaderModel.BankId,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                    Particulars = existingHeaderModel.Particulars!,
                    CVId = existingHeaderModel.CheckVoucherHeaderId,
                    CVNo = existingHeaderModel.CheckVoucherHeaderNo,
                    CreatedBy = _userManager.GetUserName(this.User),
                    RRs = new List<ReceivingReportList>(),
                    OldCVNo = existingHeaderModel.OldCvNo,
                    Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims,
                        cancellationToken),
                    MinDate = minDate
                };

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id && cv.DocumentType == "RR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    model.RRs.Add(new ReceivingReportList
                    {
                        Id = item.DocumentId,
                        Amount = item.AmountPaid
                    });
                }

                model.PONo = _dbContext.FilpridePurchaseOrders
                    .Include(x => x.ReceivingReports)
                    .Where(po => po.PostedBy != null
                                 && po.QuantityReceived > 0
                                 && po.Company == companyClaims
                                 && po.ReceivingReports != null
                                 && po.ReceivingReports.Any(x => !x.IsPaid))
                    .OrderBy(s => s.PurchaseOrderNo)
                    .Select(s => new SelectListItem
                    {
                        Value = s.PurchaseOrderNo,
                        Text = s.PurchaseOrderNo
                    })
                    .ToList();

                model.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv trade supplier. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.PONo = (await _unitOfWork.FilpridePurchaseOrder
                    .GetAllAsync(p => !p.IsSubPo && p.Company == companyClaims, cancellationToken))
                .OrderBy(s => s.PurchaseOrderNo)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderNo,
                    Text = s.PurchaseOrderNo
                })
                .ToList();
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.Suppliers = await _unitOfWork.GetFilprideTradeSupplierListAsyncById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId,
                    cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --CV Details Entry

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var details = new List<FilprideCheckVoucherDetail>();

                var cashInBank = 0m;
                for (var i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    if (viewModel.Debit[i] == 0 && viewModel.Credit[i] == 0)
                    {
                        continue;
                    }

                    cashInBank = viewModel.Credit[1];

                    details.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = viewModel.CVId,
                        SupplierId = i == 0 ? viewModel.SupplierId : null,
                        BankId = i == 1 ? viewModel.BankId : null,
                    });
                }

                #endregion --CV Details Entry

                #region --Saving the default entries

                #region -- Get Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

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

                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.PONo = viewModel.POSeries;
                existingHeaderModel.SupplierId = viewModel.SupplierId;
                existingHeaderModel.SupplierName = supplier.SupplierName;
                existingHeaderModel.Address = viewModel.SupplierAddress;
                existingHeaderModel.Tin = viewModel.SupplierTinNo;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.Total = cashInBank;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Reference = viewModel.AdvancesCVNo;
                existingHeaderModel.OldCvNo = viewModel.OldCVNo;
                existingHeaderModel.VatType = supplier.VatType;
                existingHeaderModel.TaxType = supplier.TaxType;

                #endregion --Saving the default entries

                #region -- Partial payment of RR's

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "RR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    var receivingReport = await _unitOfWork.FilprideReceivingReport
                        .GetAsync(rr => rr.ReceivingReportId == item.DocumentId, cancellationToken);

                    if (receivingReport == null)
                    {
                        return NotFound();
                    }

                    receivingReport.AmountPaid -= item.AmountPaid;
                }

                _dbContext.RemoveRange(getCheckVoucherTradePayment);
                await _unitOfWork.SaveAsync(cancellationToken);

                var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.RRs)
                {
                    var getReceivingReport = await _unitOfWork.FilprideReceivingReport
                        .GetAsync(rr => rr.ReceivingReportId == item.Id, cancellationToken);

                    if (getReceivingReport == null)
                    {
                        return NotFound();
                    }

                    getReceivingReport.AmountPaid += item.Amount;
                    existingHeaderModel.TaxPercent = getReceivingReport.TaxPercentage;

                    cvTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getReceivingReport.ReceivingReportId,
                            DocumentType = "RR",
                            CheckVoucherId = existingHeaderModel.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                }

                await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Partial payment of RR's

                #region -- Additional details entry

                var getWithholdingTaxTitle = await _dbContext.FilprideChartOfAccounts
                    .FirstOrDefaultAsync(x => x. AccountName
                        .Contains($"Expanded Withholding Tax {existingHeaderModel.TaxPercent * 100:N0}%"), cancellationToken);

                foreach (var cv in details.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = existingHeaderModel.VatType == SD.VatType_Vatable;
                    var isTaxable = existingHeaderModel.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = existingHeaderModel.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - existingHeaderModel.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - existingHeaderModel.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * existingHeaderModel.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    if(existingHeaderModel.CheckVoucherHeaderNo != null)
                    {
                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = cv.AccountNo,
                            AccountName = cv.AccountName,
                            Debit = baseAmount,
                            Credit = 0.00m,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            SupplierId = viewModel.SupplierId,
                            IsDisplayEntry = true
                        });

                        if (inputVat != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = "101060200",
                                AccountName = "Vat - Input",
                                Debit = inputVat,
                                Credit = 0.00m,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        if (ewt != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = getWithholdingTaxTitle!.AccountNumber ?? string.Empty,
                                AccountName = getWithholdingTaxTitle.AccountName,
                                Debit = 0.00m,
                                Credit = ewt,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101010100",
                            AccountName = "Cash in Bank",
                            Debit = 0.00m,
                            Credit = netOfEwt,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            BankId = viewModel.BankId,
                            IsDisplayEntry = true
                        });
                    }
                    else
                    {
                        throw new Exception("Check voucher header no. not found!");
                    }

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Trade edited successfully";
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
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

            var companyClaims = await GetCompanyClaimAsync();

            var details = await _dbContext.FilprideCheckVoucherDetails
                .Include(cvd => cvd.Supplier)
                .Include(cvd => cvd.BankAccount)
                .Include(cvd => cvd.Company)
                .Include(cvd => cvd.Customer)
                .Include(cvd => cvd.Employee)
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var getSupplier = await _unitOfWork.FilprideSupplier
                .GetAsync(s => s.SupplierId == supplierId && (companyClaims == nameof(Filpride) ? s.IsFilpride : s.IsMobility), cancellationToken);

            if (header.CvType == "Supplier")
            {
                var listOfRrIds = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.CheckVoucherId == header.CheckVoucherHeaderId)
                    .Select(x => x.DocumentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var rrList = await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(x => listOfRrIds.Contains(x.ReceivingReportId) && x.Company == companyClaims, cancellationToken);

                var siArray = rrList
                    .Where(r => !string.IsNullOrWhiteSpace(r.SupplierInvoiceNumber))
                    .OrderBy(r => r.SupplierInvoiceNumber)
                    .Select(r => r.SupplierInvoiceNumber!.Trim())
                    .Distinct()
                    .ToArray();

                ViewBag.SINoArray = siArray;
            }
            else
            {
                ViewBag.SINoArray = header.SINo?
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .Select(s => s.Trim())
                                        .Distinct()
                                        .ToArray()
                                    ?? Array.Empty<string>();
            }



            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview check voucher# {header.CheckVoucherHeaderNo}", "Check Voucher", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed re-printed copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", cv.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id, supplierId });
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader == null)
            {
                return NotFound();
            }

            var modelDetails = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId)
                .Include(cvd => cvd.Supplier)
                .Include(cvd => cvd.BankAccount)
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
                modelHeader.Status = nameof(Status.Posted);

                #region -- Mark as paid the RR's or DR's

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id)
                    .Include(cv => cv.CV)
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    if (item.DocumentType == "RR")
                    {
                        var receivingReport = await _unitOfWork.FilprideReceivingReport
                            .GetAsync(rr => rr.ReceivingReportId == item.DocumentId, cancellationToken);

                        receivingReport!.IsPaid = true;
                        receivingReport.PaidDate = DateTimeHelper.GetCurrentPhilippineTime();
                    }
                    if (item.DocumentType == "DR")
                    {
                        var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);

                        if (item.CV.CvType == "Commission")
                        {
                            deliveryReceipt!.IsCommissionPaid = true;
                        }
                        if (item.CV.CvType == "Hauler")
                        {
                            deliveryReceipt!.IsFreightPaid = true;
                        }
                    }
                }

                #endregion -- Mark as paid the RR's or DR's

                #region Add amount paid for the advances if applicable

                if (modelHeader.Reference != null)
                {
                    var advances = await _unitOfWork.FilprideCheckVoucher
                        .GetAsync(cv =>
                                cv.CheckVoucherHeaderNo == modelHeader.Reference &&
                                cv.Company == modelHeader.Company,
                            cancellationToken);

                    if (advances == null)
                    {
                        throw new NullReferenceException($"Advance check voucher not found. Check Voucher Header No: {modelHeader.Reference}");
                    }

                    advances.AmountPaid += advances.Total;

                }

                #endregion

                #region --General Ledger Book Recording(CV)--

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var ledgers = new List<FilprideGeneralLedgerBook>();
                foreach (var details in modelDetails.Where(x => !x.IsDisplayEntry))
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
                                BankAccountName = details.BankId.HasValue ? $"{details.BankAccount?.AccountNo} {details.BankAccount?.AccountName}" : null,
                                SupplierId = details.SupplierId,
                                SupplierName = details.Supplier?.SupplierName,
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
                                Payee = modelHeader.Payee != null ? modelHeader.Payee! : modelHeader.SupplierName!,
                                Amount = modelHeader.Total,
                                Particulars = modelHeader.Particulars!,
                                Bank = bank != null ? bank.Branch : "N/A",
                                CheckNo = !string.IsNullOrEmpty(modelHeader.CheckNo) ? modelHeader.CheckNo : "N/A",
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

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check Voucher has been Posted.";
                return RedirectToAction(nameof(Print), new { id, supplierId });
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
            var model = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region -- Recalculate payment of RR's or DR's

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id)
                    .Include(cv => cv.CV)
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    if (item.DocumentType == "RR")
                    {
                        var receivingReport = await _unitOfWork.FilprideReceivingReport
                            .GetAsync(rr => rr.ReceivingReportId == item.DocumentId, cancellationToken);

                        receivingReport!.IsPaid = false;
                        receivingReport.AmountPaid -= item.AmountPaid;
                    }
                    if (item.DocumentType == "DR")
                    {
                        var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);

                        if (item.CV.CvType == "Commission")
                        {
                            deliveryReceipt!.IsCommissionPaid = false;
                            deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                        }
                        if (item.CV.CvType == "Hauler")
                        {
                            deliveryReceipt!.IsFreightPaid = false;
                            deliveryReceipt.FreightAmountPaid -= item.AmountPaid;
                        }
                    }
                }

                #endregion -- Recalculate payment of RR's or DR's

                #region Revert the amount paid of advances

                if (model.Reference != null)
                {
                    var advances = await _unitOfWork.FilprideCheckVoucher
                        .GetAsync(cv =>
                                cv.CheckVoucherHeaderNo == model.Reference &&
                                cv.Company == model.Company,
                            cancellationToken);

                    if (advances == null)
                    {
                        return NotFound();
                    }

                    advances.AmountPaid -= advances.AmountPaid;
                }

                #endregion

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

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
            var model = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

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

                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo, cancellationToken);
                await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo, cancellationToken);

                #region -- Recalculate payment of RR's or DR's

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id)
                    .Include(cv => cv.CV)
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    if (item.DocumentType == "RR")
                    {
                        var receivingReport = await _unitOfWork.FilprideReceivingReport
                            .GetAsync(rr => rr.ReceivingReportId == item.DocumentId, cancellationToken);

                        receivingReport!.IsPaid = false;
                        receivingReport.AmountPaid -= item.AmountPaid;
                    }
                    if (item.DocumentType == "DR")
                    {
                        var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);

                        if (item.CV.CvType == "Commission")
                        {
                            deliveryReceipt!.IsCommissionPaid = false;
                            deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                        }
                        if (item.CV.CvType == "Hauler")
                        {
                            deliveryReceipt!.IsFreightPaid = false;
                            deliveryReceipt.FreightAmountPaid -= item.AmountPaid;
                        }
                    }
                }

                #endregion -- Recalculate payment of RR's or DR's

                #region -- Revert the amount paid of advances

                if (model.Reference != null)
                {
                    var advances = await _unitOfWork.FilprideCheckVoucher
                        .GetAsync(cv =>
                                cv.CheckVoucherHeaderNo == model.Reference &&
                                cv.Company == model.Company,
                            cancellationToken);

                    if (advances == null)
                    {
                        return NotFound();
                    }

                    advances.AmountPaid -= advances.AmountPaid;
                }

                #endregion

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", model.Company);
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

                #region -- Revert the tagging of RR's or DR's

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id)
                    .Include(cv => cv.CV)
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    if (item.DocumentType == "RR")
                    {
                        var receivingReport = await _unitOfWork.FilprideReceivingReport
                            .GetAsync(rr => rr.ReceivingReportId == item.DocumentId, cancellationToken);

                        receivingReport!.IsPaid = false;
                    }
                    if (item.DocumentType == "DR")
                    {
                        var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                            .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);
                        if (item.CV.CvType == "Commission")
                        {
                            deliveryReceipt!.IsCommissionPaid = false;
                        }
                        if (item.CV.CvType == "Hauler")
                        {
                            deliveryReceipt!.IsFreightPaid = false;
                        }
                    }
                }

                #endregion -- Recalculate payment of RR's or DR's

                #region -- Revert the amount paid of advances

                if (cvHeader.Reference != null)
                {
                    var advances = await _unitOfWork.FilprideCheckVoucher
                        .GetAsync(cv =>
                                cv.CheckVoucherHeaderNo == cvHeader.Reference &&
                                cv.Company == cvHeader.Company,
                            cancellationToken);

                    if (advances == null)
                    {
                        return NotFound();
                    }

                    advances.AmountPaid -= advances.AmountPaid;
                }

                #endregion

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
                    .GetAllAsync(cvh => recordIds.Contains(cvh.CheckVoucherHeaderId) && cvh.CvType != "Payment");

                // Create the Excel package
                using var package = new ExcelPackage();
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
                worksheet4.Cells["W1"].Value = "PostedBy";
                worksheet4.Cells["X1"].Value = "PostedDate";

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
                worksheet.Cells["AF1"].Value = "PostedBy";
                worksheet.Cells["AG1"].Value = "PostedDate";

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
                worksheet2.Cells["H1"].Value = "Amount";
                worksheet2.Cells["I1"].Value = "AmountPaid";
                worksheet2.Cells["J1"].Value = "SupplierId";
                worksheet2.Cells["K1"].Value = "EwtPercent";
                worksheet2.Cells["L1"].Value = "IsUserSelected";
                worksheet2.Cells["M1"].Value = "IsVatable";

                #endregion -- Check Voucher Details Table Header --

                #region -- Check Voucher Trade Payments Table Header --

                var worksheet5 = package.Workbook.Worksheets.Add("CheckVoucherTradePayments");

                worksheet5.Cells["A1"].Value = "Id";
                worksheet5.Cells["B1"].Value = "DocumentId";
                worksheet5.Cells["C1"].Value = "DocumentType";
                worksheet5.Cells["D1"].Value = "CheckVoucherId";
                worksheet5.Cells["E1"].Value = "AmountPaid";

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Multiple Payment Table Header --

                var worksheet6 = package.Workbook.Worksheets.Add("MultipleCheckVoucherPayments");

                worksheet6.Cells["A1"].Value = "Id";
                worksheet6.Cells["B1"].Value = "CheckVoucherHeaderPaymentId";
                worksheet6.Cells["C1"].Value = "CheckVoucherHeaderInvoiceId";
                worksheet6.Cells["D1"].Value = "AmountPaid";

                #endregion

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
                    worksheet.Cells[row, 32].Value = item.PostedBy;
                    worksheet.Cells[row, 33].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => recordIds.Contains(cv.CheckVoucherId) && cv.DocumentType == "RR")
                    .ToListAsync();

                int cvRow = 2;
                foreach (var payment in getCheckVoucherTradePayment)
                {
                    worksheet5.Cells[cvRow, 1].Value = payment.Id;
                    worksheet5.Cells[cvRow, 2].Value = payment.DocumentId;
                    worksheet5.Cells[cvRow, 3].Value = payment.DocumentType;
                    worksheet5.Cells[cvRow, 4].Value = payment.CheckVoucherId;
                    worksheet5.Cells[cvRow, 5].Value = payment.AmountPaid;

                    cvRow++;
                }

                #endregion -- Check Voucher Header Export (Trade and Invoicing) --

                #region -- Check Voucher Header Export (Payment) --

                var cvNos = selectedList.Select(item => item.CheckVoucherHeaderNo).ToList();

                var checkVoucherPayment = (await _unitOfWork.FilprideCheckVoucher
                        .GetAllAsync(cvh => cvh.Reference != null))
                    .Where(cvh => cvh.Reference != null &&
                        cvh.Reference
                            .Split(',', StringSplitOptions.TrimEntries)
                            .Any(r => cvNos.Contains(r)))
                    .ToList();


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
                    worksheet.Cells[row, 32].Value = item.PostedBy;
                    worksheet.Cells[row, 33].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                var cvPaymentId = checkVoucherPayment.Select(cvn => cvn.CheckVoucherHeaderId).ToList();
                var getCheckVoucherMultiplePayment = await _dbContext.FilprideMultipleCheckVoucherPayments
                    .Where(cv => cvPaymentId.Contains(cv.CheckVoucherHeaderPaymentId))
                    .ToListAsync();

                int cvn = 2;
                foreach (var payment in getCheckVoucherMultiplePayment)
                {
                    worksheet6.Cells[cvn, 1].Value = payment.Id;
                    worksheet6.Cells[cvn, 2].Value = payment.CheckVoucherHeaderPaymentId;
                    worksheet6.Cells[cvn, 3].Value = payment.CheckVoucherHeaderInvoiceId;
                    worksheet6.Cells[cvn, 4].Value = payment.AmountPaid;

                    cvn++;
                }

                #endregion -- Check Voucher Header Export (Payment) --

                #region -- Check Voucher Details Export (Trade and Invoicing) --

                var getCvDetails = await _dbContext.FilprideCheckVoucherDetails
                    .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                var cvdRow = 2;

                foreach (var item in getCvDetails)
                {
                    worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet2.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet2.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet2.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet2.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet2.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet2.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet2.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Trade and Invoicing) --

                #region -- Check Voucher Details Export (Payment) --

                var getCvPaymentDetails = await _dbContext.FilprideCheckVoucherDetails
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
                    worksheet2.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet2.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet2.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet2.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet2.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet2.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Payment) --

                #region -- Receiving Report Export --

                var selectedIds = selectedList.Select(item => item.CheckVoucherHeaderId).ToList();

                var cvTradePaymentList = await _dbContext.FilprideCVTradePayments
                    .Where(p => selectedIds.Contains(p.CheckVoucherId))
                    .ToListAsync();

                var rrIds = cvTradePaymentList.Select(item => item.DocumentId).ToList();

                var getReceivingReport = (await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rrIds.Contains(rr.ReceivingReportId))).ToList();

                var rrRow = 2;
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

                #endregion -- Receiving Report Export --

                #region -- Purchase Order Export --

                var getPurchaseOrder = (await _unitOfWork.FilpridePurchaseOrder
                        .GetAllAsync(po => getReceivingReport.Select(item => item.POId).Contains(po.PurchaseOrderId)))
                    .OrderBy(po => po.PurchaseOrderNo)
                    .ToList();

                var poRow = 2;
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

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CheckVoucherList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export check voucher. Exported by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllCheckVoucherIds()
        {
            var cvIds = (await _unitOfWork.FilprideCheckVoucher
                 .GetAllAsync(cv => cv.Type == nameof(DocumentType.Documented)))
                 .Select(cv => cv.CheckVoucherHeaderId)
                 .ToList();

            return Json(cvIds);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCommissionPayment(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            CommissionPaymentViewModel model = new()
            {
                Suppliers = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCommissionPayment(CommissionPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {

                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Check if duplicate record

                if (!viewModel.CheckNo.Contains("DM"))
                {
                    var cv = await _unitOfWork.FilprideCheckVoucher
                        .GetAllAsync(cv =>
                            cv.Company == companyClaims && cv.CheckNo == viewModel.CheckNo &&
                            cv.BankId == viewModel.BankId, cancellationToken);

                    if (cv.Any())
                    {
                        viewModel.COA = (await _unitOfWork.FilprideChartOfAccount
                                .GetAllAsync(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren, cancellationToken))
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToList();

                        viewModel.Suppliers = (await _unitOfWork.FilprideSupplier
                                .GetAllAsync(supp => (companyClaims == nameof(Filpride) ? supp.IsFilpride : supp.IsMobility) && supp.Category == "Trade", cancellationToken))
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToList();

                        viewModel.BankAccounts = (await _unitOfWork.FilprideBankAccount
                                .GetAllAsync(b => (companyClaims == nameof(Filpride) ? b.IsFilpride : b.IsMobility), cancellationToken))
                            .Select(ba => new SelectListItem
                            {
                                Value = ba.BankAccountId.ToString(),
                                Text = ba.AccountNo + " " + ba.AccountName
                            })
                            .ToList();

                        TempData["info"] = "Check No. Is already exist";
                        return View(viewModel);
                    }
                }

                #endregion --Check if duplicate record

                #region -- Get DR --

                var getDeliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(
                        dr => dr.DeliveryReceiptId == viewModel.DRs.Select(d => d.Id).FirstOrDefault() &&
                              dr.Company == companyClaims, cancellationToken);

                if (getDeliveryReceipt == null)
                {
                    return NotFound();
                }

                #endregion -- Get DR --

                #region --Saving the default entries

                var generateCvNo = await _unitOfWork.FilprideCheckVoucher
                    .GenerateCodeAsync(companyClaims, viewModel.Type!, cancellationToken);
                var cashInBank = viewModel.Credit[1];

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

                var cvh = new FilprideCheckVoucherHeader
                {
                    CheckVoucherHeaderNo = generateCvNo,
                    Date = viewModel.TransactionDate,
                    SupplierId = viewModel.SupplierId,
                    Particulars = viewModel.Particulars,
                    SINo = [viewModel.SiNo ?? string.Empty],
                    BankId = viewModel.BankId,
                    CheckNo = viewModel.CheckNo,
                    Category = "Trade",
                    Payee = viewModel.Payee,
                    CheckDate = viewModel.CheckDate,
                    Total = cashInBank,
                    CreatedBy = GetUserFullName(),
                    Company = companyClaims,
                    Type = viewModel.Type,
                    CvType = "Commission",
                    SupplierName = supplier.SupplierName,
                    Address = supplier.SupplierAddress,
                    Tin = supplier.SupplierTin,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    OldCvNo = viewModel.OldCVNo,
                    VatType = supplier.VatType,
                    TaxType = supplier.TaxType,
                    TaxPercent = supplier.WithholdingTaxPercent ?? 0m
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(cvh, cancellationToken);

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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 1 ? viewModel.BankId : null,
                            });
                    }
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                var parts = (supplier.WithholdingTaxTitle ?? string.Empty).Split(' ', 2);

                foreach (var cv in cvDetails.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = cvh.VatType == SD.VatType_Vatable;
                    var isTaxable = cvh.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = cvh.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - cvh.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - cvh.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * cvh.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = cv.AccountNo,
                        AccountName = cv.AccountName,
                        Debit = baseAmount,
                        Credit = 0.00m,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        SupplierId = viewModel.SupplierId,
                        IsDisplayEntry = true
                    });

                    if (inputVat != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101060200",
                            AccountName = "Vat - Input",
                            Debit = inputVat,
                            Credit = 0.00m,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    if (ewt != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = parts[0],
                            AccountName = parts[1],
                            Debit = 0.00m,
                            Credit = ewt,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = "101010100",
                        AccountName = "Cash in Bank",
                        Debit = 0.00m,
                        Credit = netOfEwt,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        BankId = viewModel.BankId,
                        IsDisplayEntry = true
                    });

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Partial payment of DR's

                var cVTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.DRs)
                {
                    var getDeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.Id, cancellationToken);

                    if (getDeliveryReceipts == null)
                    {
                        return NotFound();
                    }

                    getDeliveryReceipts.CommissionAmountPaid += item.Amount;

                    cVTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getDeliveryReceipts.DeliveryReceiptId,
                            DocumentType = "DR",
                            CheckVoucherId = cvh.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                }

                await _dbContext.AddRangeAsync(cVTradePaymentModel, cancellationToken);

                #endregion -- Partial payment of DR's

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName!);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy!, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", cvh.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Check voucher trade #{cvh.CheckVoucherHeaderNo} created successfully";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create commission payment. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateHaulerPayment(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            HaulerPaymentViewModel model = new()
            {
                Suppliers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken),
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHaulerPayment(HaulerPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Check if duplicate record

                if (!viewModel.CheckNo.Contains("DM"))
                {
                    var cv = await _unitOfWork.FilprideCheckVoucher
                        .GetAllAsync(cv => cv.Company == companyClaims && cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId, cancellationToken);

                    if (cv.Any())
                    {
                        viewModel.COA = (await _unitOfWork.FilprideChartOfAccount
                                .GetAllAsync(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren, cancellationToken))
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToList();

                        viewModel.Suppliers = (await _unitOfWork.FilprideSupplier
                                .GetAllAsync(supp => (companyClaims == nameof(Filpride) ? supp.IsFilpride : supp.IsMobility) && supp.Category == "Trade", cancellationToken))
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToList();

                        viewModel.BankAccounts = (await _unitOfWork.FilprideBankAccount
                                .GetAllAsync(b => (companyClaims == nameof(Filpride) ? b.IsFilpride : b.IsMobility), cancellationToken))
                            .Select(ba => new SelectListItem
                            {
                                Value = ba.BankAccountId.ToString(),
                                Text = ba.AccountNo + " " + ba.AccountName
                            })
                            .ToList();

                        TempData["info"] = "Check No. Is already exist";
                        return View(viewModel);
                    }
                }

                #endregion --Check if duplicate record

                #region -- Get DR --

                var getDeliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAsync(dr => dr.DeliveryReceiptId == viewModel.DRs.Select(d => d.Id).FirstOrDefault()
                                    && dr.Company == companyClaims, cancellationToken);

                if (getDeliveryReceipt == null)
                {
                    return NotFound();
                }

                #endregion -- Get DR --

                #region --Saving the default entries

                var generateCvNo = await _unitOfWork.FilprideCheckVoucher
                    .GenerateCodeAsync(companyClaims, viewModel.Type!, cancellationToken);
                var cashInBank = viewModel.Credit[1];

                #region -- Get Supplier

                var supplier = await _unitOfWork.FilprideSupplier
                    .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

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

                var cvh = new FilprideCheckVoucherHeader
                {
                    CheckVoucherHeaderNo = generateCvNo,
                    Date = viewModel.TransactionDate,
                    SupplierId = viewModel.SupplierId,
                    Total = cashInBank,
                    Particulars = viewModel.Particulars,
                    BankId = viewModel.BankId,
                    CheckNo = viewModel.CheckNo,
                    Category = "Trade",
                    Payee = viewModel.Payee,
                    CheckDate = viewModel.CheckDate,
                    CvType = "Hauler",
                    Company = companyClaims,
                    CreatedBy = GetUserFullName(),
                    CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                    SupplierName = supplier.SupplierName,
                    Address = viewModel.SupplierAddress,
                    Tin = viewModel.SupplierTinNo,
                    Type = viewModel.Type,
                    BankAccountName = bank.AccountName,
                    BankAccountNumber = bank.AccountNo,
                    OldCvNo = viewModel.OldCVNo,
                    SINo = [viewModel.SiNo ?? string.Empty],
                    VatType = supplier.VatType,
                    TaxType = supplier.TaxType,
                    TaxPercent = supplier.WithholdingTaxPercent ?? 0m
                };

                await _unitOfWork.FilprideCheckVoucher.AddAsync(cvh, cancellationToken);

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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 1 ? viewModel.BankId : null,
                            });
                    }
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                var parts = (supplier.WithholdingTaxTitle ?? string.Empty).Split(' ', 2);

                foreach (var cv in cvDetails.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = cvh.VatType == SD.VatType_Vatable;
                    var isTaxable = cvh.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = cvh.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - cvh.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - cvh.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * cvh.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = cv.AccountNo,
                        AccountName = cv.AccountName,
                        Debit = baseAmount,
                        Credit = 0.00m,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        SupplierId = viewModel.SupplierId,
                        IsDisplayEntry = true
                    });

                    if (inputVat != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101060200",
                            AccountName = "Vat - Input",
                            Debit = inputVat,
                            Credit = 0.00m,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    if (ewt != 0)
                    {
                        cvDetails.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = parts[0],
                            AccountName = parts[1],
                            Debit = 0.00m,
                            Credit = ewt,
                            TransactionNo = cvh.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                            IsDisplayEntry = true
                        });
                    }

                    cvDetails.Add(
                    new FilprideCheckVoucherDetail
                    {
                        AccountNo = "101010100",
                        AccountName = "Cash in Bank",
                        Debit = 0.00m,
                        Credit = netOfEwt,
                        TransactionNo = cvh.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                        BankId = viewModel.BankId,
                        IsDisplayEntry = true
                    });

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Partial payment of DR's

                var cVTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.DRs)
                {
                    var getDeliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.Id, cancellationToken);

                    if (getDeliveryReceipts == null)
                    {
                        return NotFound();
                    }

                    getDeliveryReceipts.FreightAmountPaid += item.Amount;

                    cVTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getDeliveryReceipts.DeliveryReceiptId,
                            DocumentType = "DR",
                            CheckVoucherId = cvh.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                }

                await _dbContext.AddRangeAsync(cVTradePaymentModel, cancellationToken);

                #endregion -- Partial payment of DR's

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName!);
                }


                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy!, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", cvh.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Check voucher trade #{cvh.CheckVoucherHeaderNo} created successfully";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create hauler payment. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetCommissioneeDRs(int? commissioneeId, int? cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var query = _dbContext.FilprideDeliveryReceipts
                .Where(dr => companyClaims != null
                             && dr.Company == companyClaims
                             && commissioneeId == dr.CommissioneeId
                             && dr.CommissionAmountPaid == 0
                             && !dr.IsCommissionPaid
                             && dr.PostedBy != null);

            if (cvId != null)
            {
                var drIds = await _dbContext.FilprideCVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "DR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.FilprideDeliveryReceipts
                    .Where(dr => commissioneeId == dr.CommissioneeId && drIds.Contains(dr.DeliveryReceiptId)));
            }

            var deliverReceipt = await query
                .Include(dr => dr.CustomerOrderSlip)
                .OrderBy(dr => dr.DeliveryReceiptNo)
                .ToListAsync(cancellationToken);

            if (query.Any())
            {
                var drList = deliverReceipt
                    .OrderBy(x => x.ManualDrNo)
                    .Select(dr =>
                    {
                        var netOfVatAmount = dr.CustomerOrderSlip!.CommissioneeVatType == SD.VatType_Vatable
                            ? _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(dr.CommissionAmount)
                            : dr.CommissionAmount;

                        var ewtAmount = dr.CustomerOrderSlip!.CommissioneeTaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, dr.Commissionee?.WithholdingTaxPercent ?? 0m)
                            : 0m;

                        var netOfEwtAmount = dr.CustomerOrderSlip!.CommissioneeTaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(dr.CommissionAmount, ewtAmount)
                            : dr.CommissionAmount;

                        return new
                        {
                            Id = dr.DeliveryReceiptId,
                            dr.DeliveryReceiptNo,
                            dr.ManualDrNo,
                            AmountPaid = dr.CommissionAmountPaid.ToString(SD.Four_Decimal_Format),
                            NetOfEwtAmount = netOfEwtAmount.ToString(SD.Four_Decimal_Format)
                        };
                    }).ToList();
                return Json(drList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetHaulerDRs(int? haulerId, int? cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var query = _dbContext.FilprideDeliveryReceipts
                .Where(dr => dr.Company == companyClaims
                             && dr.HaulerId == haulerId
                             && dr.FreightAmountPaid == 0
                             && !dr.IsFreightPaid
                             && dr.PostedBy != null);

            if (cvId != null)
            {
                var drIds = await _dbContext.FilprideCVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "DR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.HaulerId == haulerId && drIds.Contains(dr.DeliveryReceiptId)));
            }

            var deliverReceipt = await query
                .Include(dr => dr.Hauler)
                .OrderBy(dr => dr.DeliveryReceiptNo)
                .ToListAsync(cancellationToken);

            if (!query.Any())
            {
                return Json(null);
            }

            var drList = deliverReceipt
                .OrderBy(x => x.ManualDrNo)
                .Select(dr =>
                {
                    var netOfVatAmount = dr.HaulerVatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(dr.FreightAmount)
                        : dr.FreightAmount;

                    var ewtAmount = dr.HaulerTaxType == SD.TaxType_WithTax
                        ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, dr.Hauler?.WithholdingTaxPercent ?? 0m)
                        : 0.0000m;

                    var netOfEwtAmount = dr.HaulerTaxType == SD.TaxType_WithTax
                        ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(dr.FreightAmount, ewtAmount)
                        : dr.FreightAmount;

                    return new
                    {
                        Id = dr.DeliveryReceiptId,
                        dr.ManualDrNo,
                        AmountPaid = dr.FreightAmountPaid.ToString(SD.Four_Decimal_Format),
                        NetOfEwtAmount = netOfEwtAmount.ToString(SD.Four_Decimal_Format)
                    };
                }).ToList();
            return Json(drList);
        }

        [HttpGet]
        public async Task<IActionResult> EditCommissionPayment(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

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

                CommissionPaymentViewModel model = new()
                {
                    CvId = existingHeaderModel.CheckVoucherHeaderId,
                    SupplierId = existingHeaderModel.SupplierId ?? 0,
                    Payee = existingHeaderModel.Payee!,
                    SupplierAddress = existingHeaderModel.Supplier!.SupplierAddress,
                    SupplierTinNo = existingHeaderModel.Supplier.SupplierTin,
                    TransactionDate = existingHeaderModel.Date,
                    BankId = existingHeaderModel.BankId,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                    Particulars = existingHeaderModel.Particulars!,
                    CreatedBy = _userManager.GetUserName(User),
                    DRs = [],
                    Suppliers =
                        await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken),
                    OldCVNo = existingHeaderModel.OldCvNo,
                    SiNo = existingHeaderModel.SINo?.FirstOrDefault(),
                    MinDate = minDate
                };

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id && cv.DocumentType == "DR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    model.DRs.Add(new DRDetailsViewModel
                    {
                        Id = item.DocumentId,
                        Amount = item.AmountPaid
                    });
                }

                model.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv trade commission. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCommissionPayment(CommissionPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            try
            {
                #region --CV Details Entry

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var details = new List<FilprideCheckVoucherDetail>();

                var cashInBank = 0m;
                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    cashInBank = viewModel.Credit[1];

                    details.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = viewModel.CvId,
                        SupplierId = i == 0 ? viewModel.SupplierId : null,
                        BankId = i == 1 ? viewModel.BankId : null,
                    });
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion --CV Details Entry

                #region --Saving the default entries

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
                existingHeaderModel.SupplierId = viewModel.SupplierId;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.Category = "Trade";
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.Total = cashInBank;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.SupplierName = supplier.SupplierName;
                existingHeaderModel.Address = viewModel.SupplierAddress;
                existingHeaderModel.Tin = viewModel.SupplierTinNo;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;
                existingHeaderModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingHeaderModel.VatType = supplier.VatType;
                existingHeaderModel.TaxType = supplier.TaxType;
                existingHeaderModel.TaxPercent = supplier.WithholdingTaxPercent ?? 0m;

                #endregion --Saving the default entries

                #region -- Additional details entry

                var parts = (supplier.WithholdingTaxTitle ?? string.Empty).Split(' ', 2);

                foreach (var cv in details.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = existingHeaderModel.VatType == SD.VatType_Vatable;
                    var isTaxable = existingHeaderModel.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = existingHeaderModel.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - existingHeaderModel.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - existingHeaderModel.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * existingHeaderModel.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    if(existingHeaderModel.CheckVoucherHeaderNo != null)
                    {
                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = cv.AccountNo,
                            AccountName = cv.AccountName,
                            Debit = baseAmount,
                            Credit = 0.00m,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            SupplierId = viewModel.SupplierId,
                            IsDisplayEntry = true
                        });

                        if (inputVat != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = "101060200",
                                AccountName = "Vat - Input",
                                Debit = inputVat,
                                Credit = 0.00m,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        if (ewt != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = parts[0],
                                AccountName = parts[1],
                                Debit = 0.00m,
                                Credit = ewt,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101010100",
                            AccountName = "Cash in Bank",
                            Debit = 0.00m,
                            Credit = netOfEwt,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            BankId = viewModel.BankId,
                            IsDisplayEntry = true
                        });
                    }
                    else
                    {
                        throw new Exception("Check voucher header no. not found!");
                    }

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion

                #region -- Partial payment

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "DR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);

                    if (deliveryReceipt == null)
                    {
                        return NotFound();
                    }

                    deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                }

                _dbContext.RemoveRange(getCheckVoucherTradePayment);
                await _unitOfWork.SaveAsync(cancellationToken);

                var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.DRs)
                {
                    var getDeliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.Id, cancellationToken);

                    if (getDeliveryReceipt == null)
                    {
                        return NotFound();
                    }

                    getDeliveryReceipt.CommissionAmountPaid += item.Amount;

                    cvTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getDeliveryReceipt.DeliveryReceiptId,
                            DocumentType = "DR",
                            CheckVoucherId = existingHeaderModel.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                }

                await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Partial payment of RR's

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
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
                _logger.LogError(ex, "Failed to edit commission payment. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditHaulerPayment(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                    .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

                if (existingHeaderModel.Date < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {existingHeaderModel.Date:MMM yyyy} is already closed.");
                }

                HaulerPaymentViewModel model = new()
                {
                    CvId = existingHeaderModel.CheckVoucherHeaderId,
                    SupplierId = existingHeaderModel.SupplierId ?? 0,
                    Payee = existingHeaderModel.Payee!,
                    SupplierAddress = existingHeaderModel.Supplier!.SupplierAddress,
                    SupplierTinNo = existingHeaderModel.Supplier.SupplierTin,
                    TransactionDate = existingHeaderModel.Date,
                    BankId = existingHeaderModel.BankId,
                    CheckNo = existingHeaderModel.CheckNo!,
                    CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                    Particulars = existingHeaderModel.Particulars!,
                    CreatedBy = _userManager.GetUserName(this.User),
                    DRs = [],
                    Suppliers = await _unitOfWork.GetFilprideHaulerListAsyncById(companyClaims, cancellationToken),
                    BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                    OldCVNo = existingHeaderModel.OldCvNo,
                    SiNo = existingHeaderModel.SINo?.FirstOrDefault(),
                    MinDate = minDate
                };

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == id && cv.DocumentType == "DR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    model.DRs.Add(new DRDetailsViewModel
                    {
                        Id = item.DocumentId,
                        Amount = item.AmountPaid
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch cv trade hauler. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHaulerPayment(HaulerPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Suppliers = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);
            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.CheckVoucher, cancellationToken);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information provided was invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            try
            {
                #region --CV Details Entry

                var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _unitOfWork.SaveAsync(cancellationToken);

                var details = new List<FilprideCheckVoucherDetail>();

                var cashInBank = 0m;
                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    cashInBank = viewModel.Credit[1];

                    details.Add(new FilprideCheckVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = viewModel.CvId,
                        SupplierId = i == 0 ? viewModel.SupplierId : null,
                        BankId = i == 1 ? viewModel.BankId : null,
                    });
                }

                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion --CV Details Entry

                #region --Saving the default entries

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
                existingHeaderModel.SupplierId = viewModel.SupplierId;
                existingHeaderModel.Total = cashInBank;
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.BankId = viewModel.BankId;
                existingHeaderModel.CheckNo = viewModel.CheckNo;
                existingHeaderModel.Category = "Trade";
                existingHeaderModel.Payee = viewModel.Payee;
                existingHeaderModel.CheckDate = viewModel.CheckDate;
                existingHeaderModel.EditedBy = GetUserFullName();
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.SupplierName = supplier.SupplierName;
                existingHeaderModel.Address = viewModel.SupplierAddress;
                existingHeaderModel.Tin = viewModel.SupplierTinNo;
                existingHeaderModel.BankAccountName = bank.AccountName;
                existingHeaderModel.BankAccountNumber = bank.AccountNo;
                existingHeaderModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingHeaderModel.VatType = supplier.VatType;
                existingHeaderModel.TaxType = supplier.TaxType;
                existingHeaderModel.TaxPercent = supplier.WithholdingTaxPercent ?? 0m;

                #endregion --Saving the default entries

                #region -- Additional details entry

                var parts = (supplier.WithholdingTaxTitle ?? string.Empty).Split(' ', 2);

                foreach (var cv in details.OrderBy(x => x.CheckVoucherDetailId))
                {
                    var isVatable = existingHeaderModel.VatType == SD.VatType_Vatable;
                    var isTaxable = existingHeaderModel.TaxType == SD.TaxType_WithTax;

                    // Net of tax (input)
                    var netAmount = existingHeaderModel.Total;
                    var baseAmount = 0m;

                    // Base computation (reversible correct formula)
                    if (isTaxable)
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / (1.12m - existingHeaderModel.TaxPercent), 4)
                            : Math.Round(netAmount / (1m - existingHeaderModel.TaxPercent), 4);
                    }
                    else
                    {
                        baseAmount = isVatable
                            ? Math.Round(netAmount / 1.12m, 4)
                            : Math.Round(netAmount / 1m, 4);
                    }

                    var inputVat = isVatable
                        ? Math.Round(baseAmount * 0.12m, 4)
                        : 0m;

                    var grossAmount = baseAmount + inputVat;

                    var ewt = isTaxable
                        ? Math.Round(baseAmount * existingHeaderModel.TaxPercent, 4)
                        : 0m;

                    var netOfEwt = grossAmount - ewt;

                    if(existingHeaderModel.CheckVoucherHeaderNo != null)
                    {
                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = cv.AccountNo,
                            AccountName = cv.AccountName,
                            Debit = baseAmount,
                            Credit = 0.00m,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            SupplierId = viewModel.SupplierId,
                            IsDisplayEntry = true
                        });

                        if (inputVat != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = "101060200",
                                AccountName = "Vat - Input",
                                Debit = inputVat,
                                Credit = 0.00m,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        if (ewt != 0)
                        {
                            details.Add(
                            new FilprideCheckVoucherDetail
                            {
                                AccountNo = parts[0],
                                AccountName = parts[1],
                                Debit = 0.00m,
                                Credit = ewt,
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                                IsDisplayEntry = true
                            });
                        }

                        details.Add(
                        new FilprideCheckVoucherDetail
                        {
                            AccountNo = "101010100",
                            AccountName = "Cash in Bank",
                            Debit = 0.00m,
                            Credit = netOfEwt,
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingHeaderModel.CheckVoucherHeaderId,
                            BankId = viewModel.BankId,
                            IsDisplayEntry = true
                        });
                    }
                    else
                    {
                        throw new Exception("Check voucher header no. not found!");
                    }

                    break;
                }
                await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                #endregion

                #region -- Partial payment

                var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                    .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "DR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    var deliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.DocumentId, cancellationToken);

                    if (deliveryReceipt == null)
                    {
                        return NotFound();
                    }

                    deliveryReceipt.FreightAmountPaid -= item.AmountPaid;
                }

                _dbContext.RemoveRange(getCheckVoucherTradePayment);
                await _unitOfWork.SaveAsync(cancellationToken);

                var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                foreach (var item in viewModel.DRs)
                {
                    var getDeliveryReceipt = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAsync(dr => dr.DeliveryReceiptId == item.Id, cancellationToken);

                    if (getDeliveryReceipt == null)
                    {
                        return NotFound();
                    }

                    getDeliveryReceipt.FreightAmountPaid += item.Amount;

                    cvTradePaymentModel.Add(
                        new FilprideCVTradePayment
                        {
                            DocumentId = getDeliveryReceipt.DeliveryReceiptId,
                            DocumentType = "DR",
                            CheckVoucherId = existingHeaderModel.CheckVoucherHeaderId,
                            AmountPaid = item.Amount
                        });
                }

                await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                #endregion -- Partial payment of RR's

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", existingHeaderModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Trade edited successfully";
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit hauler payment. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> CheckPOPaymentTerms(string[] poNumbers, CancellationToken cancellationToken)
        {
            bool hasCodOrPrepaid = false;
            decimal advanceAmount = 0;
            string advanceCvNo = string.Empty;

            var companyClaims = await GetCompanyClaimAsync();

            foreach (var poNumber in poNumbers)
            {
                var po = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(p => p.PurchaseOrderNo == poNumber && p.Company == companyClaims, cancellationToken);

                if (po == null || (po.Terms != SD.Terms_Cod && po.Terms != SD.Terms_Prepaid) || advanceAmount != 0)
                {
                    continue;
                }

                var (cvNo, amount) = await CalculateAdvanceAmount(po.SupplierId);
                advanceAmount += amount;
                advanceCvNo = cvNo;

                if (string.IsNullOrEmpty(advanceCvNo) || amount <= 0)
                {
                    continue;
                }

                advanceCvNo = cvNo;
                hasCodOrPrepaid = true;
            }

            return Json(new { hasCodOrPrepaid, advanceAmount, advanceCVNo = advanceCvNo });

        }

        private async Task<(string CVNo, decimal Amount)> CalculateAdvanceAmount(int supplierId)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var advancesVoucher = await _dbContext.FilprideCheckVoucherDetails
                .Include(cv => cv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cv =>
                        cv.CheckVoucherHeader!.SupplierId == supplierId &&
                        cv.CheckVoucherHeader.IsAdvances &&
                        cv.CheckVoucherHeader.Total > cv.CheckVoucherHeader.AmountPaid &&
                        cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                        cv.AccountName.Contains("Advances to Suppliers") &&
                        cv.CheckVoucherHeader.Company == companyClaims);

            if (advancesVoucher == null)
            {
                return (string.Empty, 0);
            }

            return (advancesVoucher.CheckVoucherHeader!.CheckVoucherHeaderNo!, advancesVoucher.CheckVoucherHeader.Total - advancesVoucher.CheckVoucherHeader.AmountPaid);
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
