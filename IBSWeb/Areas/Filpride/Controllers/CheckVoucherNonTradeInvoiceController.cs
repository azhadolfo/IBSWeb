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
    public class CheckVoucherNonTradeInvoiceController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ICloudStorageService _cloudStorageService;

        public CheckVoucherNonTradeInvoiceController(IUnitOfWork unitOfWork,
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            ViewData["Department"] = user.Department;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoiceCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var checkVoucherDetails = await _dbContext.FilprideCheckVoucherDetails
                                                        .Where(cvd => cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing) && (cvd.SupplierId != null || cvd.CheckVoucherHeader.SupplierId != null && cvd.AccountName == "AP-Non Trade Payable"))
                                                        .Include(cvd => cvd.Supplier)
                                                        .Include(cvd => cvd.CheckVoucherHeader)
                                                        .ThenInclude(cvh => cvh.Supplier)
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
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
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultipleInvoiceAsync(companyClaims, viewModel.Type, cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = viewModel.SupplierName,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = nameof(CVType.Invoicing),
                        Company = companyClaims,
                        Type = viewModel.Type,
                        Total = viewModel.Total
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

                    foreach (var accountEntry in viewModel.AccountingEntries)
                    {
                        var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                        var accountNo = parts[0];
                        var accountName = parts[1];

                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
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
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
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
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = apNonTradeTitle.AccountNumber,
                            AccountName = apNonTradeTitle.AccountName,
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = apNontradeAmount,
                        });
                    }

                    if (ewtOnePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtOnePercent.AccountNumber,
                            AccountName = ewtOnePercent.AccountName,
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtOnePercentAmount,
                            Amount = ewtOnePercentAmount,
                            SupplierId = 133
                        });
                    }

                    if (ewtTwoPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtTwoPercent.AccountNumber,
                            AccountName = ewtTwoPercent.AccountName,
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtTwoPercentAmount,
                            Amount = ewtTwoPercentAmount,
                            SupplierId = 133
                        });
                    }

                    if (ewtFivePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtFivePercent.AccountNumber,
                            AccountName = ewtFivePercent.AccountName,
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtFivePercentAmount,
                            Amount = ewtFivePercentAmount,
                            SupplierId = 133
                        });
                    }

                    if (ewtTenPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtTenPercent.AccountNumber,
                            AccountName = ewtTenPercent.AccountName,
                            TransactionNo = checkVoucherHeader.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = checkVoucherHeader.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtTenPercentAmount,
                            Amount = ewtTenPercentAmount,
                            SupplierId = 133
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

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
                    TempData["success"] = "Check voucher invoicing created successfully.";
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

                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                        .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
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
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
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
        public async Task<IActionResult> CreatePayrollInvoice(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                        CheckVoucherHeaderNo = await _unitOfWork.FilprideCheckVoucher.GenerateCodeMultipleInvoiceAsync(companyClaims, viewModel.Type, cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = null,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = null,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = nameof(CVType.Invoicing),
                        Company = companyClaims,
                        Type = viewModel.Type,
                        InvoiceAmount = viewModel.Total
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
                                Credit = viewModel.Credit[i],
                                Amount = viewModel.Credit[i],
                                SupplierId = viewModel.MultipleSupplierId[i] != 0 ? viewModel.MultipleSupplierId[i] : null,
                                IsUserSelected = true
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

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
                    TempData["success"] = "Check voucher invoicing created successfully.";
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

                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                        .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
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
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
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
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var existingModel = await _dbContext.FilprideCheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                .Where(d => d.IsUserSelected && d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId )
                .ToListAsync(cancellationToken);

            existingModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims)
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
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                        existingModel.Particulars = viewModel.Particulars;
                        existingModel.Total = viewModel.Total;
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

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == existingModel.CheckVoucherHeaderId).
                        ToListAsync(cancellationToken);

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var checkVoucherDetails = new List<FilprideCheckVoucherDetail>();

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

                    foreach (var accountEntry in viewModel.AccountingEntries)
                    {
                        var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                        var accountNo = parts[0];
                        var accountName = parts[1];

                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = accountNo,
                            AccountName = accountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = accountEntry.NetOfVatAmount,
                            Credit = 0,
                            IsVatable = accountEntry.Vatable,
                            EwtPercent = accountEntry.TaxPercentage,
                            IsUserSelected = true,
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
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountName = vatInputTitle.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = vatAmount,
                            Credit = 0,
                        });
                    }

                    if (apNontradeAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = apNonTradeTitle.AccountNumber,
                            AccountName = apNonTradeTitle.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = apNontradeAmount,
                        });
                    }

                    if (ewtOnePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtOnePercent.AccountNumber,
                            AccountName = ewtOnePercent.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtOnePercentAmount,
                        });
                    }

                    if (ewtTwoPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtTwoPercent.AccountNumber,
                            AccountName = ewtTwoPercent.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtTwoPercentAmount,
                        });
                    }

                    if (ewtFivePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtFivePercent.AccountNumber,
                            AccountName = ewtFivePercent.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtFivePercentAmount,
                        });
                    }

                    if (ewtTenPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new FilprideCheckVoucherDetail
                        {
                            AccountNo = ewtTenPercent.AccountNumber,
                            AccountName = ewtTenPercent.AccountName,
                            TransactionNo = existingModel.CheckVoucherHeaderNo,
                            CheckVoucherHeaderId = existingModel.CheckVoucherHeaderId,
                            Debit = 0,
                            Credit = ewtTenPercentAmount,
                        });
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        existingModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingModel.SupportingFileSavedFileName);
                    }

                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited check voucher# {existingModel.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Non-trade invoicing edited successfully";
                    return RedirectToAction(nameof(Index));
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
            var modelHeader = await _unitOfWork.FilprideCheckVoucher.GetAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);
            var modelDetails = await _dbContext.FilprideCheckVoucherDetails.Where(cvd => cvd.CheckVoucherHeaderId == modelHeader.CheckVoucherHeaderId).ToListAsync();
            var supplierName = await _dbContext.FilprideSuppliers.Where(s => s.SupplierId == supplierId).Select(s => s.SupplierName).FirstOrDefaultAsync(cancellationToken);

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

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";
                    }
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

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.Status = nameof(CheckVoucherInvoiceStatus.Canceled);
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

                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideDisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo, cancellationToken);
                        await _unitOfWork.FilprideCheckVoucher.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo, cancellationToken);

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

        [HttpGet]
        public async Task<IActionResult> EditPayrollInvoice(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders.FindAsync(id, cancellationToken);

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

            var companyClaims = await GetCompanyClaimAsync();

            var coa = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync();

            var details = await _dbContext.FilprideCheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId)
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync();

            var getSupplierId = await _dbContext.FilprideCheckVoucherDetails
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
                Particulars = existingHeaderModel.Particulars,
                Total = existingHeaderModel.Total,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                ChartOfAccounts = coa,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                PoNo = existingHeaderModel.PONo.First(),
                SiNo = existingHeaderModel.SINo.First(),
                Type = existingHeaderModel.Type,
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditPayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Saving the default entries --

                    var existingHeaderModel = await _dbContext.FilprideCheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);

                    if (existingHeaderModel != null)
                    {
                        existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                        existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        existingHeaderModel.Date = viewModel.TransactionDate;
                        existingHeaderModel.PONo = [viewModel.PoNo];
                        existingHeaderModel.SINo = [viewModel.SiNo];
                        existingHeaderModel.Particulars = viewModel.Particulars;
                        existingHeaderModel.Total = viewModel.Total;
                    }

                    #endregion -- Saving the default entries --

                    #region -- Get Supplier --

                    var supplier = await _dbContext.FilprideSuppliers
                        .Where(s => s.SupplierId == viewModel.SupplierId)
                        .FirstOrDefaultAsync(cancellationToken);

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

                    var existingDetailsModel = await _dbContext.FilprideCheckVoucherDetails.Where(d => d.CheckVoucherHeaderId == existingHeaderModel.CheckVoucherHeaderId).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
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
                                TransactionNo = existingHeaderModel.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = viewModel.CVId,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                Amount = viewModel.Credit[i],
                                SupplierId = viewModel.MultipleSupplierId[i] != 0 ? viewModel.MultipleSupplierId[i] : null
                            });
                        }
                    }

                    await _dbContext.FilprideCheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        existingHeaderModel.SupportingFileSavedFileName = GenerateFileNameToSave(file.FileName);
                        existingHeaderModel.SupportingFileSavedUrl = await _cloudStorageService.UploadFileAsync(file, existingHeaderModel.SupportingFileSavedFileName);
                    }

                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingHeaderModel.CreatedBy, $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher", ipAddress, existingHeaderModel.Company);
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

                    viewModel.Suppliers = await _dbContext.FilprideSuppliers
                        .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
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
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.FilprideSuppliers
                .Where(supp => supp.Company == companyClaims && supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
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
    }
}
