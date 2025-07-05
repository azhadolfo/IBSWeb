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
        public async Task<IActionResult> GetCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var checkVoucherHeaders = await _unitOfWork.MobilityCheckVoucher
                    .GetAllAsync(cv => cv.StationCode == stationCodeClaims && cv.Category == "Trade");

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                    .Where(s =>
                        s.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                        s.Date.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.Total.ToString().Contains(searchValue) ||
                        s.Amount?.ToString()!.Contains(searchValue) == true ||
                        s.Category.ToLower().Contains(searchValue) ||
                        s.CvType?.ToLower().Contains(searchValue) == true ||
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
            var stationCodeClaims = await GetStationCodeClaimAsync();

            CheckVoucherTradeViewModel model = new();
            model.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.Category == "Trade" && supp.StationCode == stationCodeClaims)
                .OrderBy(supp => supp.SupplierCode)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            model.BankAccounts = await _dbContext.MobilityBankAccounts
                .Where(b => b.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.COA = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.Category == "Trade" && supp.StationCode == stationCodeClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();

                viewModel.PONo = await _dbContext.MobilityPurchaseOrders
                    .Where(po => po.StationCode == stationCodeClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                    .Select(po => new SelectListItem
                    {
                        Value = po.PurchaseOrderNo.ToString(),
                        Text = po.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken);

                viewModel.BankAccounts = await _dbContext.MobilityBankAccounts
                    .Where(b => b.StationCode == stationCodeClaims)
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
                #region --Check if duplicate record

                if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                {
                    var cv = await _unitOfWork
                    .MobilityCheckVoucher
                    .GetAllAsync(cv => cv.StationCode == stationCodeClaims && cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId, cancellationToken);
                    if (cv.Count() > 0)
                    {
                        viewModel.COA = await _dbContext.FilprideChartOfAccounts
                            .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToListAsync(cancellationToken);

                        viewModel.Suppliers = await _dbContext.MobilitySuppliers
                            .Where(supp => supp.Category == "Trade" && supp.StationCode == stationCodeClaims)
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToListAsync();

                        viewModel.PONo = await _dbContext.MobilityPurchaseOrders
                            .Where(po => po.StationCode == stationCodeClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                            .Select(po => new SelectListItem
                            {
                                Value = po.PurchaseOrderNo.ToString(),
                                Text = po.PurchaseOrderNo
                            })
                            .ToListAsync(cancellationToken);

                        viewModel.BankAccounts = await _dbContext.MobilityBankAccounts
                            .Where(b => b.StationCode == stationCodeClaims)
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

                var supplier = await _unitOfWork
                            .MobilitySupplier
                            .GetAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion --Retrieve Supplier

                #region -- Get PO --

                var getPurchaseOrder = await _unitOfWork.MobilityPurchaseOrder
                                                .GetAsync(po => viewModel.POSeries!.Contains(po.PurchaseOrderNo), cancellationToken);

                if (getPurchaseOrder == null)
                {
                    return NotFound();
                }

                #endregion -- Get PO --

                #region --Saving the default entries

                var generateCVNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeAsync(stationCodeClaims, getPurchaseOrder.Type, cancellationToken);
                var cashInBank = viewModel.Credit[1];
                var cvh = new MobilityCheckVoucherHeader
                {
                    CheckVoucherHeaderNo = generateCVNo,
                    Date = viewModel.TransactionDate,
                    PONo = viewModel.POSeries,
                    SupplierId = viewModel.SupplierId,
                    Particulars = $"{viewModel.Particulars} {(viewModel.AdvancesCVNo != null ? "Advances#" + viewModel.AdvancesCVNo : "")}.",
                    Reference = viewModel.AdvancesCVNo,
                    BankId = viewModel.BankId,
                    CheckNo = viewModel.CheckNo,
                    Category = "Trade",
                    Payee = viewModel.Payee,
                    CheckDate = viewModel.CheckDate,
                    Total = cashInBank,
                    CreatedBy = _userManager.GetUserName(this.User),
                    StationCode = stationCodeClaims,
                    Type = getPurchaseOrder.Type,
                    CvType = "Supplier",
                    Address = supplier.SupplierAddress,
                    Tin = supplier.SupplierTin,
                };

                await _dbContext.MobilityCheckVoucherHeaders.AddAsync(cvh, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion --Saving the default entries

                #region --CV Details Entry

                var cvDetails = new List<MobilityCheckVoucherDetail>();
                for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                    {
                        cvDetails.Add(
                        new MobilityCheckVoucherDetail
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

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Partial payment of RR's

                var cvTradePaymentModel = new List<MobilityCVTradePayment>();
                foreach (var item in viewModel.RRs)
                {
                    var getReceivingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.Id, cancellationToken);

                    if (getReceivingReport == null)
                    {
                        return NotFound();
                    }

                    getReceivingReport.AmountPaid += item.Amount;

                    cvTradePaymentModel.Add(
                        new MobilityCVTradePayment
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
                    cvh.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, cvh.SupportingFileSavedFileName!);
                }


                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(cvh.CreatedBy!, $"Created new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Check voucher trade created successfully. Series Number: {cvh.CheckVoucherHeaderNo}.";
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));

                #endregion -- Uploading file --
            }
            catch (Exception ex)
            {
                viewModel.COA = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.Category == "Trade" && supp.StationCode == stationCodeClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();

                viewModel.PONo = await _dbContext.MobilityPurchaseOrders
                    .Where(po => po.StationCode == stationCodeClaims && po.SupplierId == viewModel.SupplierId && po.PostedBy != null)
                    .Select(po => new SelectListItem
                    {
                        Value = po.PurchaseOrderNo.ToString(),
                        Text = po.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken);

                viewModel.BankAccounts = await _dbContext.MobilityBankAccounts
                    .Where(b => b.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create check voucher trade. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var purchaseOrders = await _unitOfWork.MobilityPurchaseOrder
                .GetAllAsync(po => po.SupplierId == supplierId && po.PostedBy != null && po.StationCode == stationCodeClaims);

            if (purchaseOrders != null && purchaseOrders.Any())
            {
                var poList = purchaseOrders.OrderBy(po => po.PurchaseOrderNo)
                                        .Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo })
                                        .ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, int? cvId, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var query = _dbContext.MobilityReceivingReports
                .Where(rr => rr.StationCode == stationCodeClaims
                             && !rr.IsPaid
                             && poNumber.Contains(rr.PurchaseOrderNo)
                             && rr.PostedBy != null);

            if (cvId != null)
            {
                var rrIds = await _dbContext.MobilityCVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "RR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.MobilityReceivingReports
                    .Where(rr => poNumber.Contains(rr.PurchaseOrderNo) && rrIds.Contains(rr.ReceivingReportId)));
            }

            var receivingReports = await query
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(rr => rr!.Supplier)
                .OrderBy(rr => rr.ReceivingReportNo)
                .ToListAsync(cancellationToken);

            if (receivingReports.Any())
            {
                var rrList = receivingReports
                    .Select(rr =>
                    {
                        var netOfVatAmount = _unitOfWork.MobilityReceivingReport.ComputeNetOfVat(rr.Amount);

                        var ewtAmount = rr.PurchaseOrder?.Supplier?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.MobilityReceivingReport.ComputeEwtAmount(netOfVatAmount, 0.01m)
                            : 0.0000m;

                        var netOfEwtAmount = rr.PurchaseOrder?.Supplier?.TaxType == SD.TaxType_WithTax
                            ? _unitOfWork.MobilityReceivingReport.ComputeNetOfEwt(rr.Amount, ewtAmount)
                            : netOfVatAmount;

                        return new
                        {
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
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (supplierId != null)
            {
                var supplier = await _unitOfWork.MobilitySupplier
                    .GetAsync(s => s.SupplierId == supplierId && s.StationCode == stationCodeClaims);

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

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
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
                RRs = new List<ReceivingReportList>()
            };

            model.Suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Trade")
                .OrderBy(supp => supp.SupplierCode)
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            var getCheckVoucherTradePayment = await _dbContext.MobilityCVTradePayments
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
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PONo = await _dbContext.MobilityPurchaseOrders
                .Where(p => p.StationCode == stationCodeClaims)
                .OrderBy(s => s.PurchaseOrderNo)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderNo,
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            model.BankAccounts = await _dbContext.MobilityBankAccounts
                .Where(b => b.StationCode == stationCodeClaims)
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var existingHeaderModel = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var stationCodeClaims = await GetStationCodeClaimAsync();
            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.COA = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.PONo = await _dbContext.MobilityPurchaseOrders
                    .Where(p => p.StationCode == stationCodeClaims)
                    .OrderBy(s => s.PurchaseOrderNo)
                    .Select(s => new SelectListItem
                    {
                        Value = s.PurchaseOrderNo,
                        Text = s.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken);

                viewModel.BankAccounts = await _dbContext.MobilityBankAccounts
                    .Where(b => b.StationCode == stationCodeClaims)
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.AccountNo + " " + ba.AccountName
                    })
                    .ToListAsync();

                viewModel.Suppliers =
                    await _unitOfWork.GetMobilitySupplierListAsyncById(stationCodeClaims, cancellationToken);

                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --CV Details Entry

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                    .ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var details = new List<MobilityCheckVoucherDetail>();

                var cashInBank = 0m;
                for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                {
                    cashInBank = viewModel.Credit[1];

                    details.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = viewModel.AccountNumber[i],
                        AccountName = viewModel.AccountTitle[i],
                        Debit = viewModel.Debit[i],
                        Credit = viewModel.Credit[i],
                        TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = viewModel.CVId,
                        SupplierId = i == 0 ? viewModel.SupplierId : null,
                        BankId = i == 2 ? viewModel.BankId : null,
                    });
                }

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(details, cancellationToken);

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

                var getCheckVoucherTradePayment = await _dbContext.MobilityCVTradePayments
                    .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "RR")
                    .ToListAsync(cancellationToken);

                foreach (var item in getCheckVoucherTradePayment)
                {
                    var recevingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                    if (recevingReport == null)
                    {
                        return NotFound();
                    }

                    recevingReport.AmountPaid -= item.AmountPaid;
                }

                _dbContext.RemoveRange(getCheckVoucherTradePayment);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var cvTradePaymentModel = new List<MobilityCVTradePayment>();
                foreach (var item in viewModel.RRs)
                {
                    var getReceivingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.Id, cancellationToken);

                    if (getReceivingReport == null)
                    {
                        return NotFound();
                    }

                    getReceivingReport.AmountPaid += item.Amount;

                    cvTradePaymentModel.Add(
                        new MobilityCVTradePayment
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
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.EditedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
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
                _logger.LogError(ex, "Failed to edit check voucher trade. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, int? supplierId, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

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

            var getSupplier = await _unitOfWork.MobilitySupplier
                .GetAsync(s => s.SupplierId == supplierId && s.StationCode == stationCodeClaims, cancellationToken);

            if (header.Category == "Trade" && header.RRNo != null)
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.MobilityReceivingReports
                                .FirstOrDefaultAsync(p => p.StationCode == stationCodeClaims && p.ReceivingReportNo == rrValue, cancellationToken);

                    if (rr != null)
                    {
                        siArray[i] = rr.SupplierInvoiceNumber!;
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
            var cv = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            if (cv == null)
            {
                return NotFound();
            }

            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id, supplierId });
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    modelHeader.PostedBy = _userManager.GetUserName(this.User);
                    modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                    modelHeader.Status = nameof(Status.Posted);

                    #region -- Recalculate payment of RR's

                    var getCheckVoucherTradePayment = await _dbContext.MobilityCVTradePayments
                        .Where(cv => cv.CheckVoucherId == id)
                        .Include(cv => cv.CV)
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        if (item.DocumentType == "RR")
                        {
                            var receivingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                            if (receivingReport == null)
                            {
                                return NotFound();
                            }

                            receivingReport.IsPaid = true;
                            receivingReport.PaidDate = DateTimeHelper.GetCurrentPhilippineTime();
                        }
                    }

                    #endregion -- Recalculate payment of RR's

                    #region Add amount paid for the advances if applicable

                    if (modelHeader.Reference != null)
                    {
                        var advances = await _unitOfWork.MobilityCheckVoucher
                            .GetAsync(cv =>
                                    cv.CheckVoucherHeaderNo == modelHeader.Reference &&
                                    cv.StationCode == modelHeader.StationCode,
                                cancellationToken);

                        if (advances == null)
                        {
                            throw new NullReferenceException($"Advance check voucher not found. Check Voucher Header No: {modelHeader.Reference}");
                        }

                        advances.AmountPaid += advances.Total;

                    }

                    #endregion

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
                    //                 BankAccountId = modelHeader.BankId
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

                    ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
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
                    //                 Payee = modelHeader.Payee != null ? modelHeader.Payee : supplierName,
                    //                 Amount = modelHeader.Total,
                    //                 Particulars = modelHeader.Particulars,
                    //                 Bank = bank != null ? bank.Branch : "N/A",
                    //                 CheckNo = !string.IsNullOrEmpty(modelHeader.CheckNo) ? modelHeader.CheckNo : "N/A",
                    //                 CheckDate = modelHeader.CheckDate != null ? modelHeader.CheckDate?.ToString("MM/dd/yyyy") : "N/A",
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

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Posted.";
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, User.Identity!.Name);
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = User.Identity!.Name;
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region -- Recalculate payment of RR's

                        var getCheckVoucherTradePayment = await _dbContext.MobilityCVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                                if (receivingReport == null)
                                {
                                    return NotFound();
                                }

                                receivingReport.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                        }

                        #endregion -- Recalculate payment of RR's

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
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
            var model = await _unitOfWork.MobilityCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

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

                        model.VoidedBy = User.Identity!.Name;
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);

                        ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
                        //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo);
                        //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo);

                        #region -- Recalculate payment of RR's

                        var getCheckVoucherTradePayment = await _dbContext.MobilityCVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.MobilityReceivingReports.FindAsync(item.DocumentId, cancellationToken);

                                receivingReport!.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                        }

                        #endregion -- Recalculate payment of RR's

                        #region Revert the amount paid of advances

                        if (model.Reference != null)
                        {
                            var advances = await _unitOfWork.MobilityCheckVoucher
                                .GetAsync(cv =>
                                        cv.CheckVoucherHeaderNo == model.Reference &&
                                        cv.StationCode == model.StationCode,
                                    cancellationToken);

                            if (advances == null)
                            {
                                return NotFound();
                            }

                            advances.AmountPaid -= advances.AmountPaid;
                        }

                        #endregion

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
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

        public async Task<IActionResult> CheckPOPaymentTerms(string[] poNumbers, CancellationToken cancellationToken)
        {
            bool hasCodOrPrepaid = false;
            decimal advanceAmount = 0;
            string advanceCVNo = string.Empty;

            var stationCodeClaims = await GetStationCodeClaimAsync();

            foreach (var poNumber in poNumbers)
            {
                var po = await _dbContext.MobilityPurchaseOrders
                    .FirstOrDefaultAsync(p => p.PurchaseOrderNo == poNumber && p.StationCode == stationCodeClaims, cancellationToken);

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
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var advancesVoucher = await _dbContext.MobilityCheckVoucherDetails
                .Include(cv => cv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cv =>
                        cv.CheckVoucherHeader!.SupplierId == supplierId &&
                        cv.CheckVoucherHeader.IsAdvances &&
                        cv.CheckVoucherHeader.Total > cv.CheckVoucherHeader.AmountPaid &&
                        cv.CheckVoucherHeader.Status == nameof(CheckVoucherPaymentStatus.Posted) &&
                        cv.AccountName.Contains("Advances to Suppliers") &&
                        cv.CheckVoucherHeader.StationCode == stationCodeClaims);

            if (advancesVoucher == null)
            {
                return (string.Empty, 0);
            }

            return (advancesVoucher.CheckVoucherHeader!.CheckVoucherHeaderNo!, advancesVoucher.CheckVoucherHeader.Total - advancesVoucher.CheckVoucherHeader.AmountPaid);
        }
    }
}
