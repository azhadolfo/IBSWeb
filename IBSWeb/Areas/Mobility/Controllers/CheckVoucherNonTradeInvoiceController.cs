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
    public class CheckVoucherNonTradeInvoiceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ICloudStorageService _cloudStorageService;

        private readonly ILogger<CheckVoucherNonTradeInvoiceController> _logger;

        public CheckVoucherNonTradeInvoiceController(IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ICloudStorageService cloudStorageService,
            ILogger<CheckVoucherNonTradeInvoiceController> logger)
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

            ViewData["Department"] = user!.Department;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetInvoiceCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var checkVoucherDetails = await _dbContext.MobilityCheckVoucherDetails
                                                        .Where(cvd => cvd.CheckVoucherHeader!.StationCode == stationCodeClaims && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) && (cvd.SupplierId != null || cvd.CheckVoucherHeader.SupplierId != null && cvd.AccountName == "AP-Non Trade Payable"))
                                                        .Include(cvd => cvd.Supplier)
                                                        .Include(cvd => cvd.CheckVoucherHeader)
                                                        .ThenInclude(cvh => cvh!.Supplier)
                                                        .ToListAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherDetails = checkVoucherDetails
                    .Where(s =>
                        s.TransactionNo.ToLower().Contains(searchValue) ||
                        s.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                        s.Amount.ToString().Contains(searchValue) ||
                        s.AmountPaid.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeaderId.ToString().Contains(searchValue) ||
                        s.CheckVoucherHeader?.CreatedDate.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Status.ToLower().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.AmountPaid.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.InvoiceAmount.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.CheckVoucherHeaderNo?.ToString().Contains(searchValue) == true ||
                        s.CheckVoucherHeader?.Supplier?.SupplierName.ToLower().Contains(searchValue) == true
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
                _logger.LogError(ex, "Failed to get invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GetDefaultExpense(int? supplierId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var supplier = await _dbContext.MobilitySuppliers
                .Where(supp => supp.SupplierId == supplierId && supp.StationCode == stationCodeClaims)
                .Select(supp => supp.DefaultExpenseNumber)
                .FirstOrDefaultAsync();

            var defaultExpense = await _dbContext.FilprideChartOfAccounts
                .Where(coa => (coa.Level == 4 || coa.Level == 5))
                .OrderBy(coa => coa.AccountId)
                .ToListAsync();

            if (defaultExpense.Count > 0)
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

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                MobilityCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeMultipleInvoiceAsync(stationCodeClaims, viewModel.Type!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Payee = viewModel.SupplierName,
                    Address = viewModel.SupplierAddress!,
                    Tin = viewModel.SupplierTinNo!,
                    PONo = [viewModel.PoNo ?? string.Empty],
                    SINo = [viewModel.SiNo ?? string.Empty],
                    SupplierId = viewModel.SupplierId,
                    Particulars = viewModel.Particulars,
                    CreatedBy = _userManager.GetUserName(this.User),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Invoicing),
                    StationCode = stationCodeClaims,
                    Type = viewModel.Type,
                    Total = viewModel.Total
                };

                await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- Saving the default entries --

                #region -- cv invoiving details entry --

                List<MobilityCheckVoucherDetail> checkVoucherDetails = new();

                decimal apNontradeAmount = 0;
                decimal vatAmount = 0;
                decimal ewtOnePercentAmount = 0;
                decimal ewtTwoPercentAmount = 0;
                decimal ewtFivePercentAmount = 0;
                decimal ewtTenPercentAmount = 0;

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");

                foreach (var accountEntry in viewModel.AccountingEntries!)
                {
                    var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    var accountNo = parts[0];
                    var accountName = parts[1];

                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = accountNo,
                        AccountName = accountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = accountEntry.NetOfVatAmount,
                        Credit = 0,
                        IsVatable = accountEntry.VatAmount > 0,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        BankId = accountEntry.BankMasterFileId,
                        CompanyId = accountEntry.CompanyMasterFileId,
                        EmployeeId = accountEntry.EmployeeMasterFileId,
                        CustomerId = accountEntry.CustomerMasterFileId,
                        SupplierId = accountEntry.SupplierMasterFileId,
                    });

                    if (accountEntry.VatAmount > 0)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    switch (accountEntry.TaxPercentage)
                    {
                        case 0.01m:
                            ewtOnePercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.02m:
                            ewtTwoPercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.05m:
                            ewtFivePercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.10m:
                            ewtTenPercentAmount += accountEntry.TaxAmount;
                            break;
                    }

                    apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;

                }

                checkVoucherHeader.InvoiceAmount = apNontradeAmount;

                if (vatAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = vatAmount,
                        Credit = 0,
                    });
                }

                if (apNontradeAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = apNonTradeTitle.AccountNumber,
                        AccountName = apNonTradeTitle.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = apNontradeAmount,
                        SupplierId = checkVoucherHeader.SupplierId
                    });
                }

                var supplierBIR = await _dbContext.MobilitySuppliers
                    .Where(s => s.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"))
                    .Select(s => s.SupplierId)
                    .FirstOrDefaultAsync();

                if (ewtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtOnePercentAmount,
                        Amount = ewtOnePercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTwoPercentAmount,
                        Amount = ewtTwoPercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtFivePercentAmount,
                        Amount = ewtFivePercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                        CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTenPercentAmount,
                        Amount = ewtTenPercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- cv invoiving details entry --

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
                TempData["success"] = $"Check voucher invoicing created successfully. Series Number: {checkVoucherHeader.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create check voucher non trade invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayrollInvoice(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                MobilityCheckVoucherHeader checkVoucherHeader = new()
                {
                    CheckVoucherHeaderNo = await _unitOfWork.MobilityCheckVoucher.GenerateCodeMultipleInvoiceAsync(stationCodeClaims, viewModel.Type!, cancellationToken),
                    Date = viewModel.TransactionDate,
                    Payee = null,
                    Address = "",
                    Tin = "",
                    PONo = [viewModel.PoNo ?? string.Empty],
                    SINo = [viewModel.SiNo ?? string.Empty],
                    SupplierId = null,
                    Particulars = viewModel.Particulars,
                    Total = viewModel.Total,
                    CreatedBy = _userManager.GetUserName(this.User),
                    Category = "Non-Trade",
                    CvType = nameof(CVType.Invoicing),
                    StationCode = stationCodeClaims,
                    Type = viewModel.Type,
                    InvoiceAmount = viewModel.Total
                };

                await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- Saving the default entries -

                #region -- cv invoiving details entry --

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
                            Amount = viewModel.Credit[i],
                            SupplierId = viewModel.MultipleSupplierId![i] != 0 ? viewModel.MultipleSupplierId[i] : null,
                            IsUserSelected = true
                        });
                    }
                }

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- cv invoiving details entry --

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
                TempData["success"] = $"Check voucher invoicing created successfully. Series Number: {checkVoucherHeader.CheckVoucherHeaderNo}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create payroll invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            var existingModel = await _dbContext.MobilityCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            if (existingModel == null)
            {
                return BadRequest();
            }

            var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                .Where(d => d.IsUserSelected && d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            existingModel.Suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            existingModel.COA = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            CheckVoucherNonTradeInvoicingViewModel viewModel = new()
            {
                CVId = existingModel.CheckVoucherHeaderId,
                Suppliers = existingModel.Suppliers,
                SupplierName = existingModel.Supplier!.SupplierName,
                ChartOfAccounts = existingModel.COA,
                TransactionDate = existingModel.Date,
                SupplierId = existingModel.SupplierId ?? 0,
                SupplierAddress = existingModel.Address,
                SupplierTinNo = existingModel.Tin,
                PoNo = existingModel.PONo?.FirstOrDefault(),
                SiNo = existingModel.SINo?.FirstOrDefault(),
                Total = existingModel.Total,
                Particulars = existingModel.Particulars!,
                AccountingEntries = []
            };

            foreach (var details in existingDetailsModel)
            {
                viewModel.AccountingEntries.Add(new AccountingEntryViewModel
                {
                    AccountTitle = $"{details.AccountNo} {details.AccountName}",
                    Amount = details.IsVatable ? Math.Round(details.Debit * 1.12m, 2) : Math.Round(details.Debit, 2),
                    Vatable = details.IsVatable,
                    TaxPercentage = details.EwtPercent,
                    BankMasterFileId = details.BankId,
                    CompanyMasterFileId = details.CompanyId,
                    EmployeeMasterFileId = details.EmployeeId,
                    CustomerMasterFileId = details.CustomerId,
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(sup => sup.StationCode == stationCodeClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving the default entries

                var existingModel = await _dbContext.MobilityCheckVoucherHeaders
                    .Include(cv => cv.Supplier)
                    .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                var supplier = await _unitOfWork.MobilitySupplier
                    .GetAsync(s => s.SupplierId == viewModel.SupplierId, cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                existingModel.EditedBy = User.Identity!.Name;
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingModel.Date = viewModel.TransactionDate;
                existingModel.SupplierId = supplier.SupplierId;
                existingModel.Payee = supplier.SupplierName;
                existingModel.Address = supplier.SupplierAddress;
                existingModel.Tin = supplier.SupplierTin;
                existingModel.PONo = [viewModel.PoNo ?? string.Empty];
                existingModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingModel.Particulars = viewModel.Particulars;
                existingModel.Total = viewModel.Total;

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

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
                    .Where(d => d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId).
                    ToListAsync(cancellationToken);

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var checkVoucherDetails = new List<MobilityCheckVoucherDetail>();

                decimal apNontradeAmount = 0;
                decimal vatAmount = 0;
                decimal ewtOnePercentAmount = 0;
                decimal ewtTwoPercentAmount = 0;
                decimal ewtFivePercentAmount = 0;
                decimal ewtTenPercentAmount = 0;

                var accountTitlesDto = await _unitOfWork.FilprideCheckVoucher.GetListOfAccountTitleDto(cancellationToken);
                var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");

                foreach (var accountEntry in viewModel.AccountingEntries!)
                {
                    var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                    var accountNo = parts[0];
                    var accountName = parts[1];

                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = accountNo,
                        AccountName = accountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = accountEntry.NetOfVatAmount,
                        Credit = 0,
                        IsVatable = accountEntry.Vatable,
                        EwtPercent = accountEntry.TaxPercentage,
                        IsUserSelected = true,
                        BankId = accountEntry.BankMasterFileId,
                        CompanyId = accountEntry.CompanyMasterFileId,
                        EmployeeId = accountEntry.EmployeeMasterFileId,
                        CustomerId = accountEntry.CustomerMasterFileId,
                    });

                    if (accountEntry.Vatable)
                    {
                        vatAmount += accountEntry.VatAmount;
                    }

                    // Check EWT percentage
                    switch (accountEntry.TaxPercentage)
                    {
                        case 0.01m:
                            ewtOnePercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.02m:
                            ewtTwoPercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.05m:
                            ewtFivePercentAmount += accountEntry.TaxAmount;
                            break;
                        case 0.10m:
                            ewtTenPercentAmount += accountEntry.TaxAmount;
                            break;
                    }

                    apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;

                }

                existingModel.InvoiceAmount = apNontradeAmount;

                if (vatAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = vatInputTitle.AccountNumber,
                        AccountName = vatInputTitle.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = vatAmount,
                        Credit = 0,
                    });
                }

                if (apNontradeAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = apNonTradeTitle.AccountNumber,
                        AccountName = apNonTradeTitle.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = apNontradeAmount,
                        SupplierId = existingModel.SupplierId
                    });
                }

                var supplierBIR = await _dbContext.MobilitySuppliers
                    .Where(s => s.SupplierName.Contains("BUREAU OF INTERNAL REVENUE"))
                    .Select(s => s.SupplierId)
                    .FirstOrDefaultAsync();

                if (ewtOnePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtOnePercent.AccountNumber,
                        AccountName = ewtOnePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtOnePercentAmount,
                        Amount = ewtOnePercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtTwoPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtTwoPercent.AccountNumber,
                        AccountName = ewtTwoPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTwoPercentAmount,
                        Amount = ewtTwoPercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtFivePercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtFivePercent.AccountNumber,
                        AccountName = ewtFivePercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtFivePercentAmount,
                        Amount = ewtFivePercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                if (ewtTenPercentAmount > 0)
                {
                    checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                    {
                        AccountNo = ewtTenPercent.AccountNumber,
                        AccountName = ewtTenPercent.AccountName,
                        TransactionNo = existingModel.CheckVoucherHeaderNo!,
                        CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                        Debit = 0,
                        Credit = ewtTenPercentAmount,
                        Amount = ewtTenPercentAmount,
                        SupplierId = supplierBIR
                    });
                }

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingModel.SupportingFileSavedFileName!);
                }

                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited check voucher# {existingModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Non-trade invoicing edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(sup => sup.StationCode == stationCodeClaims)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
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

            var header = await _unitOfWork.MobilityCheckVoucher
                .GetAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var getSupplier = await _dbContext.MobilitySuppliers
                .FindAsync(supplierId, cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            return View(viewModel);
        }

        public IActionResult GetAutomaticEntry(DateTime startDate, DateTime? endDate)
        {
            if (startDate != default && endDate != default)
            {
                return Json(true);
            }

            return Json(null);
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
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
                    if (modelHeader.PostedBy == null)
                    {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        modelHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

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
                        //                 CompanyId = details.CompanyId,
                        //                 EmployeeId = details.EmployeeId,
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
                    }
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
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
            var model = await _dbContext.MobilityCheckVoucherHeaders.FindAsync(id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(CheckVoucherInvoiceStatus.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

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
                _logger.LogError(ex, "Failed to cancel invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.MobilityCheckVoucherHeaders.FindAsync(id, cancellationToken);

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
                        model.Status = nameof(CheckVoucherInvoiceStatus.Voided);

                        ///TODO: waiting for ma'am LSA journal entries
                        //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo, cancellationToken);
                        //await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo, cancellationToken);

                        //re-compute amount paid in trade and payment voucher

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
                    _logger.LogError(ex, "Failed to void invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
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

        [HttpGet]
        public async Task<IActionResult> EditPayrollInvoice(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingHeaderModel = await _dbContext.MobilityCheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (existingHeaderModel == null)
            {
                return NotFound();
            }

            var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails
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

            var stationCodeClaims = await GetStationCodeClaimAsync();

            var coa = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var suppliers = await _dbContext.MobilitySuppliers
                .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            var details = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync();

            var getSupplierId = await _dbContext.MobilityCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .OrderBy(s => s.CheckVoucherDetailId)
                .Select(s => s.SupplierId)
                .ToArrayAsync();

            CheckVoucherNonTradeInvoicingViewModel model = new()
            {
                MultipleSupplierId = getSupplierId,
                SupplierAddress = details?.Supplier?.SupplierAddress,
                SupplierTinNo = details?.Supplier?.SupplierTin,
                Suppliers = suppliers,
                TransactionDate = existingHeaderModel.Date,
                Particulars = existingHeaderModel.Particulars!,
                Total = existingHeaderModel.Total,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                ChartOfAccounts = coa,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                PoNo = existingHeaderModel.PONo!.First(),
                SiNo = existingHeaderModel.SINo!.First(),
                Type = existingHeaderModel.Type,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (!ModelState.IsValid)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                TempData["warning"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- Saving the default entries --

                var existingHeaderModel = await _dbContext.MobilityCheckVoucherHeaders
                    .Include(cv => cv.Supplier)
                    .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }

                existingHeaderModel.EditedBy = User.Identity!.Name;
                existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                existingHeaderModel.Date = viewModel.TransactionDate;
                existingHeaderModel.PONo = [viewModel.PoNo ?? string.Empty];
                existingHeaderModel.SINo = [viewModel.SiNo ?? string.Empty];
                existingHeaderModel.Particulars = viewModel.Particulars;
                existingHeaderModel.Total = viewModel.Total;

                #endregion -- Saving the default entries --

                #region -- Get Supplier --

                var supplier = await _dbContext.MobilitySuppliers
                    .Where(s => s.SupplierId == viewModel.SupplierId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (supplier == null)
                {
                    return NotFound();
                }

                #endregion -- Get Supplier --

                #region -- Automatic entry --

                if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                {
                    existingHeaderModel.StartDate = viewModel.StartDate;
                    existingHeaderModel.EndDate = existingHeaderModel.StartDate.Value.AddYears(viewModel.NumberOfYears);
                    existingHeaderModel.NumberOfMonths = (viewModel.NumberOfYears * 12);

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
                        existingHeaderModel.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion -- Automatic entry --

                #region -- cv invoiving details entry --

                var existingDetailsModel = await _dbContext.MobilityCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                _dbContext.RemoveRange(existingDetailsModel);
                await _dbContext.SaveChangesAsync(cancellationToken);

                List<MobilityCheckVoucherDetail> checkVoucherDetails = new();

                for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                {
                    if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                    {
                        checkVoucherDetails.Add(new MobilityCheckVoucherDetail
                        {
                            AccountNo = viewModel.AccountNumber[i],
                            AccountName = viewModel.AccountTitle[i],
                            TransactionNo = existingHeaderModel.CheckVoucherHeaderNo!,
                            CheckVoucherHeaderId = viewModel.CVId,
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            Amount = viewModel.Credit[i],
                            SupplierId = viewModel.MultipleSupplierId![i] != 0 ? viewModel.MultipleSupplierId[i] : null
                        });
                    }
                }

                await _dbContext.MobilityCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                #endregion -- cv invoiving details entry --

                #region -- Uploading file --

                if (file != null && file.Length > 0)
                {
                    existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                    existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName!);
                }

                #endregion -- Uploading file --

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingHeaderModel.CreatedBy!, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Check voucher invoicing edited successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                viewModel.Suppliers = await _dbContext.MobilitySuppliers
                    .Where(supp => supp.StationCode == stationCodeClaims && supp.Category == "Non-Trade")
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync();
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit payroll invoice. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            if (supplierId != null)
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();
                var supplier = await _dbContext.MobilitySuppliers
                    .FirstOrDefaultAsync(s => s.SupplierId == supplierId && s.StationCode == stationCodeClaims);

                if (supplier != null)
                {
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
                        WithholdingTax = supplier.WithholdingTaxtitle,
                        Vatable = supplier.VatType == SD.VatType_Vatable
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccounts()
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            // Replace this with your actual repository/service call
            var bankAccounts = await _unitOfWork.MobilityBankAccount.GetAllAsync(b => b.StationCode == stationCodeClaims);

            return Json(bankAccounts.Select(b => new
            {
                id = b.BankAccountId,
                accountName = b.AccountName,
                accountNumber = b.AccountNo
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccountById(int bankId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var bankAccount = await _unitOfWork.MobilityBankAccount
                .GetAsync(b => b.BankAccountId == bankId && b.StationCode == stationCodeClaims);

            if (bankAccount == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = bankAccount.BankAccountId,
                accountName = bankAccount.AccountName,
                accountNumber = bankAccount.AccountNo
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _unitOfWork.Company.GetAllAsync();

            return Json(companies.OrderBy(c => c.CompanyCode).Select(c => new
            {
                id = c.CompanyId,
                accountName = c.CompanyName,
                accountNumber = c.CompanyCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyById(int companyId)
        {
            var company = await _unitOfWork.Company.GetAsync(c => c.CompanyId == companyId);

            if (company == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = company.CompanyId,
                accountName = company.CompanyName,
                accountNumber = company.CompanyCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _unitOfWork.MobilityEmployee.GetAllAsync();

            return Json(employees.OrderBy(e => e.EmployeeNumber).Select(e => new
            {
                id = e.EmployeeId,
                accountName = $"{e.FirstName} {e.LastName}",
                accountNumber = e.EmployeeNumber
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var employee = await _unitOfWork.MobilityEmployee
                .GetAsync(e => e.EmployeeId == employeeId && e.StationCode == stationCodeClaims);

            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = employee.EmployeeId,
                accountName = $"{employee.FirstName} {employee.LastName}",
                accountNumber = employee.EmployeeNumber
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var employees = await _unitOfWork.MobilityCustomer.GetAllAsync(c => c.StationCode == stationCodeClaims);

            return Json(employees.OrderBy(c => c.CustomerCode).Select(c => new
            {
                id = c.CustomerId,
                accountName = c.CustomerName,
                accountNumber = c.CustomerCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerById(int customerId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var customer = await _unitOfWork.MobilityCustomer
                .GetAsync(e => e.CustomerId == customerId && e.StationCode == stationCodeClaims);

            if (customer == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = customer.CustomerId,
                accountName = customer.CustomerName,
                accountNumber = customer.CustomerCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var suppliers = await _unitOfWork.MobilitySupplier.GetAllAsync(s => s.StationCode == stationCodeClaims);

            return Json(suppliers.OrderBy(c => c.SupplierCode).Select(c => new
            {
                id = c.SupplierId,
                accountName = c.SupplierName,
                accountNumber = c.SupplierCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierById(int supplierId)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var supplier = await _unitOfWork.MobilitySupplier
                .GetAsync(e => e.SupplierId == supplierId && e.StationCode == stationCodeClaims);

            if (supplier == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = supplier.SupplierId,
                accountName = supplier.SupplierName,
                accountNumber = supplier.SupplierCode
            });
        }
    }
}
