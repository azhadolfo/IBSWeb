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
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_Accounting, SD.Department_RCD)]
    public class CheckVoucherTradeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherTradeController> _logger;

        public CheckVoucherTradeController(IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherTradeController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _cloudStorageService = cloudStorageService;
            _logger = logger;
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
                    .GetAllAsync(cv => cv.Company == companyClaims && cv.Category == "Trade");

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                    .Where(s =>
                        s.CheckVoucherHeaderNo.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
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

            CheckVoucherTradeViewModel model = new();
            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                .OrderBy(supp => supp.SupplierCode)
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
        public async Task<IActionResult> Create(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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
                    var cashInBank = viewModel.Credit[1];
                    var cvh = new FilprideCheckVoucherHeader
                    {
                        CheckVoucherHeaderNo = generateCVNo,
                        Date = viewModel.TransactionDate,
                        PONo = viewModel.POSeries,
                        SupplierId = viewModel.SupplierId,
                        Particulars = $"{viewModel.Particulars}. Advances# {viewModel.AdvancesCVNo}",
                        Reference = viewModel.AdvancesCVNo,
                        BankId = viewModel.BankId,
                        CheckNo = viewModel.CheckNo,
                        Category = "Trade",
                        Payee = viewModel.Payee,
                        CheckDate = viewModel.CheckDate,
                        Total = cashInBank,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Company = companyClaims,
                        Type = getPurchaseOrder.Type,
                        CvType = "Supplier",
                        Address = supplier.SupplierAddress,
                        Tin = supplier.SupplierTin,
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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 2 ? viewModel.BankId : null,
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Partial payment of RR's

                    var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.RRs)
                    {
                        var getReceivingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.Id, cancellationToken);
                        getReceivingReport.AmountPaid += item.Amount;

                        cvTradePaymentModel.Add(
                            new FilprideCVTradePayment
                            {
                                DocumentId = getReceivingReport.ReceivingReportId,
                                DocumentType = "RR",
                                CheckVoucherId = cvh.CheckVoucherHeaderId,
                                AmountPaid = item.Amount
                            });
                    }

                    await _dbContext.AddRangeAsync(cvTradePaymentModel);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName);
                    }


                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, cvh.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Check voucher trade created successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _unitOfWork.FilpridePurchaseOrder
                .GetAllAsync(po => po.SupplierId == supplierId && po.PostedBy != null);

            if (purchaseOrders != null && purchaseOrders.Any())
            {
                var poList = purchaseOrders.Where(p => !p.IsSubPo)
                                        .OrderBy(po => po.PurchaseOrderNo)
                                        .Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo })
                                        .ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, int? cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var query = _dbContext.FilprideReceivingReports
                .Where(rr => rr.Company == companyClaims
                             && !rr.IsPaid
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
                .ThenInclude(rr => rr.Supplier)
                .OrderBy(rr => rr.ReceivingReportNo)
                .ToListAsync(cancellationToken);

            if (receivingReports.Any())
            {
                var rrList = receivingReports
                    .Select(rr => {
                        var netOfVatAmount = _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(rr.Amount);

                        var ewtAmount = rr.PurchaseOrder?.Supplier?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, 0.01m)
                            : 0.0000m;

                        var netOfEwtAmount = rr.PurchaseOrder?.Supplier?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(rr.Amount, ewtAmount)
                            : netOfVatAmount;

                        return new {
                            Id = rr.ReceivingReportId,
                            ReceivingReportNo = rr.ReceivingReportNo,
                            AmountPaid = rr.AmountPaid.ToString(SD.Two_Decimal_Format),
                            NetOfEwtAmount = netOfEwtAmount.ToString(SD.Two_Decimal_Format)
                        };
                    }).ToList();
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

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            CheckVoucherTradeViewModel model = new()
            {
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee,
                SupplierAddress = existingHeaderModel.Address,
                SupplierTinNo = existingHeaderModel.Tin,
                POSeries = existingHeaderModel.PONo,
                TransactionDate = existingHeaderModel.Date,
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                CVNo = existingHeaderModel.CheckVoucherHeaderNo,
                CreatedBy = _userManager.GetUserName(this.User),
                RRs = new List<ReceivingReportList>()
            };

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Trade")
                .OrderBy(supp => supp.SupplierCode)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

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

            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PONo = await _dbContext.FilpridePurchaseOrders
                .Where(p => !p.IsSubPo)
                .OrderBy(s => s.PurchaseOrderNo)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderNo,
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

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
        public async Task<IActionResult> Edit(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);
                var companyClaims = await GetCompanyClaimAsync();

                try
                {
                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = viewModel.CVId,
                            SupplierId = i == 0 ? viewModel.SupplierId : null,
                            BankId = i == 2 ? viewModel.BankId : null,
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.PONo = viewModel.POSeries;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    existingHeaderModel.Address = viewModel.SupplierAddress;
                    existingHeaderModel.Tin = viewModel.SupplierTinNo;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    existingHeaderModel.Reference = viewModel.AdvancesCVNo;

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's

                    var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                        .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "RR")
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        var recevingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                        recevingReport.AmountPaid -= item.AmountPaid;
                    }

                    _dbContext.RemoveRange(getCheckVoucherTradePayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.RRs)
                    {
                        var getReceivingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.Id, cancellationToken);
                        getReceivingReport.AmountPaid += item.Amount;

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
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName);
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
                    _logger.LogError(ex, "Failed to edit check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.PONo = await _dbContext.FilpridePurchaseOrders
                        .Where(p => !p.IsSubPo)
                        .OrderBy(s => s.PurchaseOrderNo)
                        .Select(s => new SelectListItem
                        {
                            Value = s.PurchaseOrderNo,
                            Text = s.PurchaseOrderNo
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

                    viewModel.Suppliers =
                        await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
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
                Details = details,
                Supplier = getSupplier
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
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
            return RedirectToAction(nameof(Print), new { id, supplierId });
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
                    modelHeader.Status = nameof(Status.Posted);

                    #region -- Recalculate payment of RR's or DR's

                    var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                        .Where(cv => cv.CheckVoucherId == id)
                        .Include(cv => cv.CV)
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        if (item.DocumentType == "RR")
                        {
                            var receivingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                            receivingReport.IsPaid = true;
                            receivingReport.PaidDate = DateTimeHelper.GetCurrentPhilippineTime();
                        }
                        if (item.DocumentType == "DR")
                        {
                            var deliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.DocumentId, cancellationToken);
                            if (item.CV.CvType == "Commission")
                            {
                                deliveryReceipt.IsCommissionPaid = true;
                            }
                            if (item.CV.CvType == "Hauler")
                            {
                                deliveryReceipt.IsFreightPaid = true;
                            }
                        }
                    }

                    #endregion -- Recalculate payment of RR's or DR's

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
                    foreach (var details in modelDetails)
                    {
                        var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account title '{details.AccountNo}' not found.");
                        ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = modelHeader.Date,
                                    Reference = modelHeader.CheckVoucherHeaderNo,
                                    Description = modelHeader.Particulars,
                                    AccountId = account.AccountId,
                                    AccountNo = account.AccountNumber,
                                    AccountTitle = account.AccountName,
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

                    await _dbContext.SaveChangesAsync(cancellationToken);
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

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

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

                        #region -- Recalculate payment of RR's or DR's

                        var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                                receivingReport.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                            if (item.DocumentType == "DR")
                            {
                                var deliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.DocumentId, cancellationToken);
                                if (item.CV.CvType == "Commission")
                                {
                                    deliveryReceipt.IsCommissionPaid = false;
                                    deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                                }
                                if (item.CV.CvType == "Hauler")
                                {
                                    deliveryReceipt.IsFreightPaid = false;
                                    deliveryReceipt.FreightAmountPaid -= item.AmountPaid;
                                }
                            }
                        }

                        #endregion -- Recalculate payment of RR's or DR's

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        TempData["success"] = "Check Voucher has been Cancelled.";
                    }

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

                        #region -- Recalculate payment of RR's or DR's

                        var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.FilprideReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                                receivingReport.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                            if (item.DocumentType == "DR")
                            {
                                var deliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.DocumentId, cancellationToken);
                                if (item.CV.CvType == "Commission")
                                {
                                    deliveryReceipt.IsCommissionPaid = false;
                                    deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                                }
                                if (item.CV.CvType == "Hauler")
                                {
                                    deliveryReceipt.IsFreightPaid = false;
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

                            advances.AmountPaid -= advances.AmountPaid;
                        }

                        #endregion

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
                    _logger.LogError(ex, "Failed to void check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
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

                    List<FilprideReceivingReport> getReceivingReport = new List<FilprideReceivingReport>();

                    var selectedIds = selectedList.Select(item => item.CheckVoucherHeaderId).ToList();

                    var cvTradePaymentList = await _dbContext.FilprideCVTradePayments
                        .Where(cvtp => selectedIds.Contains(cvtp.CheckVoucherId))
                        .ToListAsync();

                    var rrIds = cvTradePaymentList.Select(item => item.DocumentId).ToList();

                    getReceivingReport = await _dbContext.FilprideReceivingReports
                        .Where(rr => rrIds.Contains(rr.ReceivingReportId))
                        .ToListAsync();

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
                        worksheet4.Cells[rrRow, 23].Value = item.PostedBy;
                        worksheet4.Cells[rrRow, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CheckVoucherList_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
                }
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
        public IActionResult GetAllCheckVoucherIds()
        {
            var cvIds = _dbContext.FilprideCheckVoucherHeaders
                                     .Where(cv => cv.Type == nameof(DocumentType.Documented))
                                     .Select(cv => cv.CheckVoucherHeaderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(cvIds);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCommissionPayment(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            CommissionPaymentViewModel model = new();
            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Commissionee")
                .OrderBy(supp => supp.SupplierCode)
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
        public async Task<IActionResult> CreateCommissionPayment(CommissionPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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

                    #region -- Get PO --

                    var getDeliveryReceipt = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.DeliveryReceiptId == viewModel.DRs.Select(dr => dr.Id).FirstOrDefault())
                        .Include(dr => dr.PurchaseOrder)
                        .FirstOrDefaultAsync();

                    #endregion -- Get PO --

                    #region --Saving the default entries

                    var generateCVNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, getDeliveryReceipt.PurchaseOrder.Type, cancellationToken);
                    var cashInBank = viewModel.Credit[1];
                    var cvh = new FilprideCheckVoucherHeader
                    {
                        CheckVoucherHeaderNo = generateCVNo,
                        Date = viewModel.TransactionDate,
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        BankId = viewModel.BankId,
                        CheckNo = viewModel.CheckNo,
                        Category = "Trade",
                        Payee = viewModel.Payee,
                        CheckDate = viewModel.CheckDate,
                        Total = cashInBank,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Company = companyClaims,
                        Type = getDeliveryReceipt.PurchaseOrder.Type,
                        CvType = "Commission",
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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 2 ? viewModel.BankId : null,
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Partial payment of DR's

                    var cVTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.DRs)
                    {
                        var getDeliveryReceipts = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.Id, cancellationToken);
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

                    await _dbContext.AddRangeAsync(cVTradePaymentModel);

                    #endregion -- Partial payment of DR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName);
                    }


                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, cvh.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Check voucher trade created successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create commission payment. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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
        public async Task<IActionResult> CreateHaulerPayment(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            HaulerPaymentViewModel model = new();
            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Hauler")
                .OrderBy(supp => supp.SupplierCode)
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
        public async Task<IActionResult> CreateHaulerPayment(HaulerPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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

                    #region -- Get PO --

                    var getDeliveryReceipt = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.DeliveryReceiptId == viewModel.DRs.Select(dr => dr.Id).FirstOrDefault())
                        .Include(dr => dr.PurchaseOrder)
                        .FirstOrDefaultAsync();

                    #endregion -- Get PO --

                    #region --Saving the default entries

                    var generateCVNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeAsync(companyClaims, getDeliveryReceipt.PurchaseOrder.Type, cancellationToken);
                    var cashInBank = viewModel.Credit[1];
                    var cvh = new FilprideCheckVoucherHeader
                    {
                        CheckVoucherHeaderNo = generateCVNo,
                        Date = viewModel.TransactionDate,
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        BankId = viewModel.BankId,
                        CheckNo = viewModel.CheckNo,
                        Category = "Trade",
                        Payee = viewModel.Payee,
                        CheckDate = viewModel.CheckDate,
                        Total = cashInBank,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Company = companyClaims,
                        Type = getDeliveryReceipt.PurchaseOrder.Type,
                        CvType = "Hauler"
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
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null,
                                BankId = i == 2 ? viewModel.BankId : null,
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Partial payment of DR's

                    var cVTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.DRs)
                    {
                        var getDeliveryReceipts = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.Id, cancellationToken);
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

                    await _dbContext.AddRangeAsync(cVTradePaymentModel);

                    #endregion -- Partial payment of DR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        cvh.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName);
                    }


                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, cvh.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Check voucher trade created successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create hauler payment. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
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

        public async Task<IActionResult> GetCommissioneeDRs(int? commissioneeId, int? cvId, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var query = _dbContext.FilprideDeliveryReceipts
                .Where(dr => dr.Company == companyClaims
                             && commissioneeId == dr.CommissioneeId
                             && dr.CommissionAmount != 0
                             && !dr.IsCommissionPaid
                             && dr.PostedBy != null);

            if (cvId != null)
            {
                var drIds = await _dbContext.FilprideCVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "DR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.FilprideDeliveryReceipts
                    .Where(dr => commissioneeId ==dr.CommissioneeId && drIds.Contains(dr.DeliveryReceiptId)));
            }

            var deliverReceipt = await query
                .Include(dr => dr.Commissionee)
                .OrderBy(dr => dr.DeliveryReceiptNo)
                .ToListAsync();

            if (query.Any())
            {
                var drList = deliverReceipt
                    .Select(dr => {
                        var ewtAmount = dr.Commissionee?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(dr.CommissionAmount, 0.05m)
                            : 0m;

                        var netOfEwtAmount = dr.Commissionee?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(dr.CommissionAmount, ewtAmount)
                            : dr.CommissionAmount;

                        return new {
                            Id = dr.DeliveryReceiptId,
                            DeliveryReceiptNo = dr.DeliveryReceiptNo,
                            AmountPaid = dr.CommissionAmountPaid.ToString(SD.Two_Decimal_Format),
                            NetOfEwtAmount = netOfEwtAmount.ToString(SD.Two_Decimal_Format)
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
                             && dr.FreightAmount != 0
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
                .ToListAsync();

            if (query.Any())
            {
                var drList = deliverReceipt
                    .Select(dr => {
                        var netOfVatAmount = _unitOfWork.FilprideReceivingReport.ComputeNetOfVat(dr.FreightAmount);

                        var ewtAmount = dr.Hauler?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, 0.02m)
                            : 0.0000m;

                        var netOfEwtAmount = dr.Hauler?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.FilprideReceivingReport.ComputeNetOfEwt(dr.FreightAmount, ewtAmount)
                            : netOfVatAmount;

                        return new {
                            Id = dr.DeliveryReceiptId,
                            DeliveryReceiptNo = dr.DeliveryReceiptNo,
                            AmountPaid = dr.FreightAmountPaid.ToString(SD.Two_Decimal_Format),
                            NetOfEwtAmount = netOfEwtAmount.ToString(SD.Two_Decimal_Format)
                        };
                    }).ToList();
                return Json(drList);
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> EditCommissionPayment(int? id, CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            CommissionPaymentViewModel model = new()
            {
                CvId = existingHeaderModel.CheckVoucherHeaderId,
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee,
                SupplierAddress = existingHeaderModel.Supplier.SupplierAddress,
                SupplierTinNo = existingHeaderModel.Supplier.SupplierTin,
                TransactionDate = existingHeaderModel.Date,
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars,
                CreatedBy = _userManager.GetUserName(this.User),
                DRs = new List<DRDetailsViewModel>()
            };

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Commissionee")
                .OrderBy(supp => supp.SupplierCode)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

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

            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

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
        public async Task<IActionResult> EditCommissionPayment(CommissionPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);
                var companyClaims = await GetCompanyClaimAsync();

                try
                {
                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = viewModel.CvId,
                            SupplierId = i == 0 ? viewModel.SupplierId : null,
                            BankId = i == 2 ? viewModel.BankId : null,
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region -- Partial payment

                    var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                        .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "DR")
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        var deliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.DocumentId, cancellationToken);

                        deliveryReceipt.CommissionAmountPaid -= item.AmountPaid;
                    }

                    _dbContext.RemoveRange(getCheckVoucherTradePayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.DRs)
                    {
                        var getDeliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.Id, cancellationToken);
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
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName);
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
                    _logger.LogError(ex, "Failed to edit commission payment. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
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

                    viewModel.Suppliers =
                        await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditHaulerPayment(int? id, CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            HaulerPaymentViewModel model = new()
            {
                CvId = existingHeaderModel.CheckVoucherHeaderId,
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee,
                SupplierAddress = existingHeaderModel.Supplier.SupplierAddress,
                SupplierTinNo = existingHeaderModel.Supplier.SupplierTin,
                TransactionDate = existingHeaderModel.Date,
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars,
                CreatedBy = _userManager.GetUserName(this.User),
                DRs = new List<DRDetailsViewModel>()
            };

            model.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Hauler")
                .OrderBy(supp => supp.SupplierCode)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

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

            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

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
        public async Task<IActionResult> EditHaulerPayment(HaulerPaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var existingHeaderModel = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CvId, cancellationToken);
                var companyClaims = await GetCompanyClaimAsync();

                try
                {
                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = viewModel.CvId,
                            SupplierId = i == 0 ? viewModel.SupplierId : null,
                            BankId = i == 2 ? viewModel.BankId : null,
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                    existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region -- Partial payment

                    var getCheckVoucherTradePayment = await _dbContext.FilprideCVTradePayments
                        .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "DR")
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        var deliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.DocumentId, cancellationToken);

                        deliveryReceipt.FreightAmountPaid -= item.AmountPaid;
                    }

                    _dbContext.RemoveRange(getCheckVoucherTradePayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var cvTradePaymentModel = new List<FilprideCVTradePayment>();
                    foreach (var item in viewModel.DRs)
                    {
                        var getDeliveryReceipt = await _dbContext.FilprideDeliveryReceipts.FindAsync(item.Id, cancellationToken);
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
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName);
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
                    _logger.LogError(ex, "Failed to edit hauler payment. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.COA = await _dbContext.FilprideChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
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

                    viewModel.Suppliers =
                        await _unitOfWork.GetFilprideSupplierListAsyncById(companyClaims, cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> CheckPOPaymentTerms(string[] poNumbers, CancellationToken cancellationToken)
        {
            bool hasCodOrPrepaid = false;
            decimal advanceAmount = 0;
            string advanceCVNo = string.Empty;

            var companyClaims = await GetCompanyClaimAsync();

            foreach (var poNumber in poNumbers)
            {
                var po = await _dbContext.FilpridePurchaseOrders
                    .FirstOrDefaultAsync(p => p.PurchaseOrderNo == poNumber && p.Company == companyClaims, cancellationToken);

                if (po != null && (po.Terms == SD.Terms_Cod || po.Terms == SD.Terms_Prepaid) && advanceAmount == 0)
                {
                    var (cvNo, amount) = await CalculateAdvanceAmount(po.SupplierId);
                    advanceAmount += amount;
                    advanceCVNo = cvNo;

                    // If this is the first or largest advance, use its CVNo
                    if (!string.IsNullOrEmpty(advanceCVNo) && amount > 0)
                    {
                        advanceCVNo = cvNo;
                        hasCodOrPrepaid = true;
                    }
                }
            }

            return Json(new { hasCodOrPrepaid, advanceAmount, advanceCVNo });

        }

        private async Task<(string CVNo, decimal Amount)> CalculateAdvanceAmount(int supplierId)
        {
            var advancesVoucher = await _dbContext.FilprideCheckVoucherDetails
                .Include(cv => cv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cv =>
                        cv.CheckVoucherHeader.SupplierId == supplierId &&
                        cv.CheckVoucherHeader.IsAdvances &&
                        cv.CheckVoucherHeader.Total > cv.CheckVoucherHeader.AmountPaid &&
                        cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                        cv.AccountName.Contains("Advances to Suppliers"));

            if (advancesVoucher == null)
            {
                return (string.Empty, 0 );
            }

            return (advancesVoucher.CheckVoucherHeader.CheckVoucherHeaderNo, advancesVoucher.CheckVoucherHeader.Total - advancesVoucher.CheckVoucherHeader.AmountPaid);
        }
    }
}
