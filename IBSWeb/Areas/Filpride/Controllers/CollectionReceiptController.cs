using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
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
using Microsoft.AspNetCore.Authorization;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class CollectionReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<CollectionReceiptController> _logger;

        public CollectionReceiptController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<CollectionReceiptController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.CollectionReceipt))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var collectionReceipts = await _unitOfWork.FilprideCollectionReceipt
                    .GetAllAsync(sv => sv.Company == companyClaims && sv.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex");
            }

            return View();
        }

        public async Task<IActionResult> ServiceInvoiceIndex(CancellationToken cancellationToken)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCollectionReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var collectionReceipts = await _unitOfWork.FilprideCollectionReceipt
                    .GetAllAsync(sv => sv.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    collectionReceipts = collectionReceipts
                        .Where(s =>
                            s.CollectionReceiptNo.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.SINo?.ToLower().Contains(searchValue) == true ||
                            s.SVNo?.ToLower().Contains(searchValue) == true ||
                            s.MultipleSI?.Contains(searchValue) == true ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
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

                    collectionReceipts = collectionReceipts
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = collectionReceipts.Count();

                var pagedData = collectionReceipts
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
                _logger.LogError(ex, "Failed to get collection receipts.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> SingleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SingleCollectionCreateForSales(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        return View(model);
                    }
                    var existingSalesInvoice = await _dbContext.FilprideSalesInvoices
                                                   .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeForSIAsync(companyClaims, existingSalesInvoice.Type, cancellationToken);

                    model.SINo = existingSalesInvoice.SalesInvoiceNo;
                    model.CollectionReceiptNo = generateCRNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;
                    model.Company = companyClaims;
                    model.Type = existingSalesInvoice.Type;

                    try
                    {
                        if (bir2306 != null && bir2306.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2306.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2306.CopyToAsync(stream);
                            }

                            model.F2306FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }

                        if (bir2307 != null && bir2307.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2307.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2307.CopyToAsync(stream);
                            }

                            model.F2307FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload file in collection receipt. Uploaded by: {UserName}", _userManager.GetUserName(User));
                        TempData["error"] = ex.Message;
                        return View(model);
                    }

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var offsettings = new List<FilprideOffsettings>();

                    for (int i = 0; i < accountTitle.Length; i++)
                    {
                        var currentAccountTitle = accountTitleText[i];
                        var currentAccountAmount = accountAmount[i];
                        offsetAmount += accountAmount[i];

                        var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                        offsettings.Add(
                            new FilprideOffsettings
                            {
                                AccountNo = accountTitle[i],
                                AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                                Source = model.CollectionReceiptNo,
                                Reference = model.SINo,
                                Amount = currentAccountAmount,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                    #endregion --Offsetting function

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Collection receipt created successfully.";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create sales invoice single collection receipt. Created by: {UserName}", _userManager.GetUserName(User));
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

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultipleCollectionCreateForSales(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
            model.Company = companyClaims;

            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        return View(model);
                    }
                    var existingSalesInvoice = await _dbContext.FilprideSalesInvoices
                                                   .Where(si => model.MultipleSIId.Contains(si.SalesInvoiceId))
                                                   .ToListAsync(cancellationToken);

                    model.MultipleSI = new string[model.MultipleSIId.Length];
                    model.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];
                    var salesInvoice = new FilprideSalesInvoice();
                    for (int i = 0; i < model.MultipleSIId.Length; i++)
                    {
                        var siId = model.MultipleSIId[i];
                        salesInvoice = await _dbContext.FilprideSalesInvoices
                                    .FirstOrDefaultAsync(si => si.SalesInvoiceId == siId);

                        if (salesInvoice != null)
                        {
                            model.MultipleSI[i] = salesInvoice.SalesInvoiceNo;
                            model.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                            model.Type = salesInvoice.Type;
                        }
                    }

                    var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeForSIAsync(companyClaims, model.Type, cancellationToken);

                    model.CollectionReceiptNo = generateCRNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;

                    try
                    {
                        if (bir2306 != null && bir2306.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2306.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2306.CopyToAsync(stream);
                            }

                            model.F2306FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }

                        if (bir2307 != null && bir2307.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2307.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2307.CopyToAsync(stream);
                            }

                            model.F2307FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload file in collection receipt. Uploaded by: {UserName}", _userManager.GetUserName(User));
                        TempData["error"] = ex.Message;
                        return View(model);
                    }

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var offsettings = new List<FilprideOffsettings>();

                    for (int i = 0; i < accountTitle.Length; i++)
                    {
                        var currentAccountTitle = accountTitleText[i];
                        var currentAccountAmount = accountAmount[i];
                        offsetAmount += accountAmount[i];

                        var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                        offsettings.Add(
                            new FilprideOffsettings
                            {
                                AccountNo = accountTitle[i],
                                AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                                Source = model.CollectionReceiptNo,
                                Reference = model.SINo,
                                Amount = currentAccountAmount,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                    #endregion --Offsetting function

                    TempData["success"] = "Multilple collection receipt successfully created!";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create sales invoice multiple collection receipt. Created by: {UserName}", _userManager.GetUserName(User));
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

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionEdit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.FilprideCollectionReceipts.FindAsync(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.FilprideCustomers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            existingModel.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.FilprideCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultipleCollectionEdit(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == model.CollectionReceiptId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        return View(model);
                    }
                    var existingSalesInvoice = await _dbContext.FilprideSalesInvoices
                                                   .Where(si => model.MultipleSIId.Contains(si.SalesInvoiceId))
                                                   .ToListAsync(cancellationToken);

                    existingModel.MultipleSIId = new int[model.MultipleSIId.Length];
                    existingModel.MultipleSI = new string[model.MultipleSIId.Length];
                    existingModel.SIMultipleAmount = new decimal[model.MultipleSIId.Length];
                    existingModel.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];
                    var salesInvoice = new FilprideSalesInvoice();
                    for (int i = 0; i < model.MultipleSIId.Length; i++)
                    {
                        var siId = model.MultipleSIId[i];
                        salesInvoice = await _dbContext.FilprideSalesInvoices
                                    .FirstOrDefaultAsync(si => si.SalesInvoiceId == siId);

                        if (salesInvoice != null)
                        {
                            existingModel.MultipleSIId[i] = model.MultipleSIId[i];
                            existingModel.MultipleSI[i] = salesInvoice.SalesInvoiceNo;
                            existingModel.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                            existingModel.SIMultipleAmount[i] = model.SIMultipleAmount[i];
                        }
                    }

                    existingModel.TransactionDate = model.TransactionDate;
                    existingModel.ReferenceNo = model.ReferenceNo;
                    existingModel.Remarks = model.Remarks;
                    existingModel.CheckDate = model.CheckDate;
                    existingModel.CheckNo = model.CheckNo;
                    existingModel.CheckBank = model.CheckBank;
                    existingModel.CheckBranch = model.CheckBranch;
                    existingModel.CashAmount = model.CashAmount;
                    existingModel.CheckAmount = model.CheckAmount;
                    existingModel.ManagerCheckAmount = model.ManagerCheckAmount;
                    existingModel.EWT = model.EWT;
                    existingModel.WVAT = model.WVAT;
                    existingModel.Total = computeTotalInModelIfZero;

                    try
                    {
                        if (bir2306 != null && bir2306.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2306.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2306.CopyToAsync(stream);
                            }

                            existingModel.F2306FilePath = fileSavePath;
                            existingModel.IsCertificateUpload = true;
                        }

                        if (bir2307 != null && bir2307.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2307.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2307.CopyToAsync(stream);
                            }

                            existingModel.F2307FilePath = fileSavePath;
                            existingModel.IsCertificateUpload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload file in collection receipt. Uploaded by: {UserName}", _userManager.GetUserName(User));
                        TempData["error"] = ex.Message;
                        return View(model);
                    }

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var findOffsettings = await _dbContext.FilprideOffsettings
                    .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                    .ToListAsync(cancellationToken);

                    var accountTitleSet = new HashSet<string>(accountTitle);

                    // Remove records not in accountTitle
                    foreach (var offsetting in findOffsettings)
                    {
                        if (!accountTitleSet.Contains(offsetting.AccountNo))
                        {
                            _dbContext.FilprideOffsettings.Remove(offsetting);
                        }
                    }

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var offsetting in findOffsettings)
                    {
                        if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                        {
                            accountTitleDict[offsetting.AccountNo] = new List<int>();
                        }
                        accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                    }

                    // Add or update records
                    for (int i = 0; i < accountTitle.Length; i++)
                    {
                        var accountNo = accountTitle[i];
                        var currentAccountTitle = accountTitleText[i];
                        var currentAccountAmount = accountAmount[i];
                        offsetAmount += accountAmount[i];

                        var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                        if (accountTitleDict.TryGetValue(accountNo, out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var offsettingId = ids.First();
                            ids.RemoveAt(0);
                            var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);

                            offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                            offsetting.Amount = currentAccountAmount;
                            offsetting.CreatedBy = _userManager.GetUserName(this.User);
                            offsetting.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(accountNo);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newOffsetting = new FilprideOffsettings
                            {
                                AccountNo = accountNo,
                                AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                                Source = existingModel.CollectionReceiptNo,
                                Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                                Amount = currentAccountAmount,
                                CreatedBy = _userManager.GetUserName(this.User),
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                            };
                            _dbContext.FilprideOffsettings.Add(newOffsetting);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                            _dbContext.FilprideOffsettings.Remove(offsetting);
                        }
                    }

                    #endregion --Offsetting function

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit sales invoice multiple collection receipt. Edited by: {UserName}", _userManager.GetUserName(User));
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

        [HttpGet]
        public async Task<IActionResult> CreateForService(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCollectionReceipt();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateForService(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            model.SalesInvoices = await _dbContext.FilprideServiceInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == model.CustomerId && si.PostedBy != null)
                .OrderBy(si => si.ServiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.CustomerId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        return View(model);
                    }
                    var existingServiceInvoice = await _dbContext.FilprideServiceInvoices
                                                   .FirstOrDefaultAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);
                    var generateCRNo = await _unitOfWork.FilprideCollectionReceipt.GenerateCodeAsync(companyClaims, existingServiceInvoice.Type, cancellationToken);

                    model.SVNo = existingServiceInvoice.ServiceInvoiceNo;
                    model.CollectionReceiptNo = generateCRNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;
                    model.Company = companyClaims;
                    model.Type = existingServiceInvoice.Type;

                    try
                    {
                        if (bir2306 != null && bir2306.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2306.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2306.CopyToAsync(stream);
                            }

                            model.F2306FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }

                        if (bir2307 != null && bir2307.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2307.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2307.CopyToAsync(stream);
                            }

                            model.F2307FilePath = fileSavePath;
                            model.IsCertificateUpload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload file in collection receipt. Uploaded by: {UserName}", _userManager.GetUserName(User));
                        TempData["error"] = ex.Message;
                        return View(model);
                    }

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var offsettings = new List<FilprideOffsettings>();

                    for (int i = 0; i < accountTitle.Length; i++)
                    {
                        var currentAccountTitle = accountTitleText[i];
                        var currentAccountAmount = accountAmount[i];
                        offsetAmount += accountAmount[i];

                        var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                        offsettings.Add(
                            new FilprideOffsettings
                            {
                                AccountNo = accountTitle[i],
                                AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                                Source = model.CollectionReceiptNo,
                                Reference = model.SVNo,
                                Amount = currentAccountAmount,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                    #endregion --Offsetting function

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Collection receipt created successfully.";
                    return RedirectToAction(nameof(ServiceInvoiceIndex));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create service invoice collection receipt. Created by: {UserName}", _userManager.GetUserName(User));
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

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            return View(cr);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            return PartialView("_CollectionPreviewPartialView", cr);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var invoices = await _dbContext
                .FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == customerNo && si.PostedBy != null)
                .OrderBy(si => si.SalesInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.SalesInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.SalesInvoiceNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .FilprideServiceInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.PostedBy != null)
                .OrderBy(si => si.ServiceInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.ServiceInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.ServiceInvoiceNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo, bool isSales, bool isServices, CancellationToken cancellationToken)
        {
            if (isSales && !isServices)
            {
                var si = await _unitOfWork.FilprideSalesInvoice.GetAsync(s => s.SalesInvoiceId == invoiceNo, cancellationToken);

                decimal netDiscount = si.Amount - si.Discount;
                decimal netOfVatAmount = si.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount) : netDiscount;
                decimal withHoldingTaxAmount = si.Customer.WithHoldingTax ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = si.Customer.WithHoldingVat ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = netDiscount.ToString("N4"),
                    AmountPaid = si.AmountPaid.ToString("N4"),
                    Balance = si.Balance.ToString("N4"),
                    Ewt = withHoldingTaxAmount.ToString("N4"),
                    Wvat = withHoldingVatAmount.ToString("N4"),
                    Total = (netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)).ToString("N4")
                });
            }
            else if (isServices && !isSales)
            {
                var sv = await _unitOfWork.FilprideServiceInvoice.GetAsync(s => s.ServiceInvoiceId == invoiceNo, cancellationToken);

                decimal netOfVatAmount = sv.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(sv.Amount) - sv.Discount : sv.Amount - sv.Discount;
                decimal withHoldingTaxAmount = sv.Customer.WithHoldingTax ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = sv.Customer.WithHoldingVat ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = sv.Total.ToString("N4"),
                    AmountPaid = sv.AmountPaid.ToString("N4"),
                    Balance = sv.Balance.ToString("N4"),
                    Ewt = withHoldingTaxAmount.ToString("N4"),
                    Wvat = withHoldingVatAmount.ToString("N4"),
                    Total = (sv.Total - (withHoldingTaxAmount + withHoldingVatAmount)).ToString("N4")
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] siNo, bool isSales, CancellationToken cancellationToken)
        {
            if (isSales)
            {
                var si = await _dbContext
                .FilprideSalesInvoices
                .FirstOrDefaultAsync(si => siNo.Contains(si.SalesInvoiceId), cancellationToken);

                decimal netDiscount = si.Amount - si.Discount;
                decimal netOfVatAmount = si.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount) : netDiscount;
                decimal withHoldingTaxAmount = si.Customer.WithHoldingTax ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = si.Customer.WithHoldingVat ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = netDiscount,
                    AmountPaid = si.AmountPaid,
                    Balance = si.Balance,
                    WithholdingTax = withHoldingTaxAmount,
                    WithholdingVat = withHoldingVatAmount,
                    Total = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)
                });
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
            var existingModel = await _dbContext.FilprideCollectionReceipts.FindAsync(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            existingModel.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);

            existingModel.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(si => si.Company == companyClaims && !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.FilprideCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Company == companyClaims && offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideCollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == model.CollectionReceiptId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var companyClaims = await GetCompanyClaimAsync();

                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        return View(model);
                    }

                    existingModel.TransactionDate = model.TransactionDate;
                    existingModel.ReferenceNo = model.ReferenceNo;
                    existingModel.Remarks = model.Remarks;
                    existingModel.CheckDate = model.CheckDate;
                    existingModel.CheckNo = model.CheckNo;
                    existingModel.CheckBank = model.CheckBank;
                    existingModel.CheckBranch = model.CheckBranch;
                    existingModel.CashAmount = model.CashAmount;
                    existingModel.CheckAmount = model.CheckAmount;
                    existingModel.ManagerCheckAmount = model.ManagerCheckAmount;
                    existingModel.EWT = model.EWT;
                    existingModel.WVAT = model.WVAT;
                    existingModel.Total = computeTotalInModelIfZero;
                    existingModel.EditedBy = _userManager.GetUserName(User);
                    existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    try
                    {
                        if (bir2306 != null && bir2306.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2306.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2306.CopyToAsync(stream);
                            }

                            existingModel.F2306FilePath = fileSavePath;
                            existingModel.IsCertificateUpload = true;
                        }

                        if (bir2307 != null && bir2307.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(bir2307.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await bir2307.CopyToAsync(stream);
                            }

                            existingModel.F2307FilePath = fileSavePath;
                            existingModel.IsCertificateUpload = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload file in collection receipt. Uploaded by: {UserName}", _userManager.GetUserName(User));
                        TempData["error"] = ex.Message;
                        return View(model);
                    }

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var findOffsettings = await _dbContext.FilprideOffsettings
                    .Where(offset => offset.Company == companyClaims && offset.Source == existingModel.CollectionReceiptNo)
                    .ToListAsync(cancellationToken);

                    var accountTitleSet = new HashSet<string>(accountTitle);

                    // Remove records not in accountTitle
                    foreach (var offsetting in findOffsettings)
                    {
                        if (!accountTitleSet.Contains(offsetting.AccountNo))
                        {
                            _dbContext.FilprideOffsettings.Remove(offsetting);
                        }
                    }

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var offsetting in findOffsettings)
                    {
                        if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                        {
                            accountTitleDict[offsetting.AccountNo] = new List<int>();
                        }
                        accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                    }

                    // Add or update records
                    for (int i = 0; i < accountTitle.Length; i++)
                    {
                        var accountNo = accountTitle[i];
                        var currentAccountTitle = accountTitleText[i];
                        var currentAccountAmount = accountAmount[i];
                        offsetAmount += accountAmount[i];

                        var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                        if (accountTitleDict.TryGetValue(accountNo, out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var offsettingId = ids.First();
                            ids.RemoveAt(0);
                            var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);

                            offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                            offsetting.Amount = currentAccountAmount;
                            offsetting.CreatedBy = _userManager.GetUserName(this.User);
                            offsetting.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                            offsetting.Company = companyClaims;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(accountNo);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newOffsetting = new FilprideOffsettings
                            {
                                AccountNo = accountNo,
                                AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                                Source = existingModel.CollectionReceiptNo,
                                Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                                Amount = currentAccountAmount,
                                CreatedBy = _userManager.GetUserName(this.User),
                                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                            };
                            _dbContext.FilprideOffsettings.Add(newOffsetting);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                            _dbContext.FilprideOffsettings.Remove(offsetting);
                        }
                    }

                    #endregion --Offsetting function

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", ipAddress, existingModel.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Collection receipt successfully updated.";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit collection receipt. Edited by: {UserName}", _userManager.GetUserName(User));
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

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Posted);

                        List<FilprideOffsettings>? offset = new List<FilprideOffsettings>();
                        var offsetAmount = 0m;

                        if (model.SalesInvoiceId != null)
                        {
                            offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo, model.SINo, model.Company, cancellationToken);
                            offsetAmount = offset.Sum(o => o.Amount);
                        }
                        else
                        {
                            offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo, model.SVNo, model.Company, cancellationToken);
                            offsetAmount = offset.Sum(o => o.Amount);
                        }

                        await _unitOfWork.FilprideCollectionReceipt.PostAsync(model, offset, cancellationToken);

                        if (model.SalesInvoiceId != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateInvoice(model.SalesInvoice.SalesInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }
                        else if (model.MultipleSIId != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateMutipleInvoice(model.MultipleSI, model.SIMultipleAmount, offsetAmount, cancellationToken);
                        }
                        else
                        {
                            await _unitOfWork.FilprideCollectionReceipt.UpdateSV(model.ServiceInvoice.ServiceInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Posted.";
                    }

                    return RedirectToAction(nameof(Print), new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post collection receipt. Posted by: {UserName}", _userManager.GetUserName(User));
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
            var model = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        model.Status = nameof(Status.Voided);
                        var series = model.SINo != null ? model.SINo : model.SVNo;

                        var findOffsetting = await _dbContext.FilprideOffsettings.Where(offset => offset.Company == model.Company && offset.Source == model.CollectionReceiptNo && offset.Reference == series).ToListAsync(cancellationToken);

                        await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideCashReceiptBook>(crb => crb.RefNo == model.CollectionReceiptNo, cancellationToken);
                        await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CollectionReceiptNo, cancellationToken);

                        if (findOffsetting.Any())
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideOffsettings>(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series, cancellationToken);
                        }
                        if (model.SINo != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveSIPayment(model.SalesInvoice.SalesInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else if (model.SVNo != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveSVPayment(model.ServiceInvoice.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else if (model.MultipleSI != null)
                        {
                            await _unitOfWork.FilprideCollectionReceipt.RemoveMultipleSIPayment(model.MultipleSIId, model.SIMultipleAmount, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else
                        {
                            TempData["error"] = "No series number found";
                            return RedirectToAction(nameof(Index));
                        }

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to void collection receipt. Voided by: {UserName}", _userManager.GetUserName(User));
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["error"] = ex.Message;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCollectionReceipts.FindAsync(id, cancellationToken);

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

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel collection receipt. Canceled by: {UserName}", _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(x => x.CollectionReceiptId == id, cancellationToken);
            if (!cr.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of collection receipt# {cr.CollectionReceiptNo}", "Collection Receipt", ipAddress, cr.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> MultipleInvoiceBalance(int siNo)
        {
            var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                .GetAsync(si => si.SalesInvoiceId == siNo);

            if (salesInvoice != null)
            {
                var amount = salesInvoice.Amount;
                var amountPaid = salesInvoice.AmountPaid;
                var netAmount = salesInvoice.Amount - salesInvoice.Discount;
                var vatAmount = salesInvoice.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideCollectionReceipt.ComputeVatAmount((netAmount / 1.12m) * 0.12m) : 0;
                var ewtAmount = salesInvoice.Customer.WithHoldingTax ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount((netAmount / 1.12m), 0.01m) : 0;
                var wvatAmount = salesInvoice.Customer.WithHoldingVat ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount((netAmount / 1.12m), 0.05m) : 0;
                var balance = amount - amountPaid;

                return Json(new
                {
                    Amount = amount,
                    AmountPaid = amountPaid,
                    NetAmount = netAmount,
                    VatAmount = vatAmount,
                    EwtAmount = ewtAmount,
                    WvatAmount = wvatAmount,
                    Balance = balance
                });
            }
            return Json(null);
        }

        public async Task<IActionResult> MultipleCollectionPrint(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            return View(cr);
        }

        public async Task<IActionResult> PrintedMultipleCR(int id, CancellationToken cancellationToken)
        {
            var findIdOfCR = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (findIdOfCR != null && !findIdOfCR.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of collection receipt# {findIdOfCR.CollectionReceiptNo}", "Collection Receipt", ipAddress, findIdOfCR.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfCR.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(MultipleCollectionPrint), new { id });
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
            var selectedList = await _dbContext.FilprideCollectionReceipts
                .Where(cr => recordIds.Contains(cr.CollectionReceiptId))
                .Include(cr => cr.SalesInvoice)
                .Include(cr => cr.ServiceInvoice)
                .OrderBy(cr => cr.CollectionReceiptNo)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                #region -- Sales Invoice Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("SalesInvoice");

                worksheet3.Cells["A1"].Value = "OtherRefNo";
                worksheet3.Cells["B1"].Value = "Quantity";
                worksheet3.Cells["C1"].Value = "UnitPrice";
                worksheet3.Cells["D1"].Value = "Amount";
                worksheet3.Cells["E1"].Value = "Remarks";
                worksheet3.Cells["F1"].Value = "Status";
                worksheet3.Cells["G1"].Value = "TransactionDate";
                worksheet3.Cells["H1"].Value = "Discount";
                worksheet3.Cells["I1"].Value = "AmountPaid";
                worksheet3.Cells["J1"].Value = "Balance";
                worksheet3.Cells["K1"].Value = "IsPaid";
                worksheet3.Cells["L1"].Value = "IsTaxAndVatPaid";
                worksheet3.Cells["M1"].Value = "DueDate";
                worksheet3.Cells["N1"].Value = "CreatedBy";
                worksheet3.Cells["O1"].Value = "CreatedDate";
                worksheet3.Cells["P1"].Value = "CancellationRemarks";
                worksheet3.Cells["Q1"].Value = "OriginalReceivingReportId";
                worksheet3.Cells["R1"].Value = "OriginalCustomerId";
                worksheet3.Cells["S1"].Value = "OriginalPOId";
                worksheet3.Cells["T1"].Value = "OriginalProductId";
                worksheet3.Cells["U1"].Value = "OriginalSeriesNumber";
                worksheet3.Cells["V1"].Value = "OriginalDocumentId";

                #endregion -- Sales Invoice Table Header --

                #region -- Service Invoice Table Header --

                var worksheet4 = package.Workbook.Worksheets.Add("ServiceInvoice");

                worksheet4.Cells["A1"].Value = "DueDate";
                worksheet4.Cells["B1"].Value = "Period";
                worksheet4.Cells["C1"].Value = "Amount";
                worksheet4.Cells["D1"].Value = "Total";
                worksheet4.Cells["E1"].Value = "Discount";
                worksheet4.Cells["F1"].Value = "CurrentAndPreviousMonth";
                worksheet4.Cells["G1"].Value = "UnearnedAmount";
                worksheet4.Cells["H1"].Value = "Status";
                worksheet4.Cells["I1"].Value = "AmountPaid";
                worksheet4.Cells["J1"].Value = "Balance";
                worksheet4.Cells["K1"].Value = "Instructions";
                worksheet4.Cells["L1"].Value = "IsPaid";
                worksheet4.Cells["M1"].Value = "CreatedBy";
                worksheet4.Cells["N1"].Value = "CreatedDate";
                worksheet4.Cells["O1"].Value = "CancellationRemarks";
                worksheet4.Cells["P1"].Value = "OriginalCustomerId";
                worksheet4.Cells["Q1"].Value = "OriginalSeriesNumber";
                worksheet4.Cells["R1"].Value = "OriginalServicesId";
                worksheet4.Cells["S1"].Value = "OriginalDocumentId";

                #endregion -- Service Invoice Table Header --

                #region -- Collection Receipt Table Header --

                var worksheet = package.Workbook.Worksheets.Add("CollectionReceipt");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "ReferenceNo";
                worksheet.Cells["C1"].Value = "Remarks";
                worksheet.Cells["D1"].Value = "CashAmount";
                worksheet.Cells["E1"].Value = "CheckDate";
                worksheet.Cells["F1"].Value = "CheckNo";
                worksheet.Cells["G1"].Value = "CheckBank";
                worksheet.Cells["H1"].Value = "CheckBranch";
                worksheet.Cells["I1"].Value = "CheckAmount";
                worksheet.Cells["J1"].Value = "ManagerCheckDate";
                worksheet.Cells["K1"].Value = "ManagerCheckNo";
                worksheet.Cells["L1"].Value = "ManagerCheckBank";
                worksheet.Cells["M1"].Value = "ManagerCheckBranch";
                worksheet.Cells["N1"].Value = "ManagerCheckAmount";
                worksheet.Cells["O1"].Value = "EWT";
                worksheet.Cells["P1"].Value = "WVAT";
                worksheet.Cells["Q1"].Value = "Total";
                worksheet.Cells["R1"].Value = "IsCertificateUpload";
                worksheet.Cells["S1"].Value = "f2306FilePath";
                worksheet.Cells["T1"].Value = "f2307FilePath";
                worksheet.Cells["U1"].Value = "CreatedBy";
                worksheet.Cells["V1"].Value = "CreatedDate";
                worksheet.Cells["W1"].Value = "CancellationRemarks";
                worksheet.Cells["X1"].Value = "MultipleSI";
                worksheet.Cells["Y1"].Value = "MultipleSIId";
                worksheet.Cells["Z1"].Value = "SIMultipleAmount";
                worksheet.Cells["AA1"].Value = "MultipleTransactionDate";
                worksheet.Cells["AB1"].Value = "OriginalCustomerId";
                worksheet.Cells["AC1"].Value = "OriginalSalesInvoiceId";
                worksheet.Cells["AD1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["AE1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["AF1"].Value = "OriginalDocumentId";

                #endregion -- Collection Receipt Table Header --

                #region -- Offsetting Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("Offsetting");

                worksheet2.Cells["A1"].Value = "AccountNo";
                worksheet2.Cells["B1"].Value = "Source";
                worksheet2.Cells["C1"].Value = "Reference";
                worksheet2.Cells["D1"].Value = "IsRemoved";
                worksheet2.Cells["E1"].Value = "Amount";
                worksheet2.Cells["F1"].Value = "CreatedBy";
                worksheet2.Cells["G1"].Value = "CreatedDate";
                worksheet2.Cells["H1"].Value = "AccountTitle";

                #endregion -- Offsetting Table Header --

                #region -- Collection Receipt Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.ReferenceNo;
                    worksheet.Cells[row, 3].Value = item.Remarks;
                    worksheet.Cells[row, 4].Value = item.CashAmount;
                    worksheet.Cells[row, 5].Value = item.CheckDate;
                    worksheet.Cells[row, 6].Value = item.CheckNo;
                    worksheet.Cells[row, 7].Value = item.CheckBank;
                    worksheet.Cells[row, 8].Value = item.CheckBranch;
                    worksheet.Cells[row, 9].Value = item.CheckAmount;
                    worksheet.Cells[row, 10].Value = item.ManagerCheckDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = item.ManagerCheckNo;
                    worksheet.Cells[row, 12].Value = item.ManagerCheckBank;
                    worksheet.Cells[row, 13].Value = item.ManagerCheckBranch;
                    worksheet.Cells[row, 14].Value = item.ManagerCheckAmount;
                    worksheet.Cells[row, 15].Value = item.EWT;
                    worksheet.Cells[row, 16].Value = item.WVAT;
                    worksheet.Cells[row, 17].Value = item.Total;
                    worksheet.Cells[row, 18].Value = item.IsCertificateUpload;
                    worksheet.Cells[row, 19].Value = item.F2306FilePath;
                    worksheet.Cells[row, 20].Value = item.F2307FilePath;
                    worksheet.Cells[row, 21].Value = item.CreatedBy;
                    worksheet.Cells[row, 22].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 23].Value = item.CancellationRemarks;
                    if (item.MultipleSIId != null)
                    {
                        worksheet.Cells[row, 24].Value = string.Join(", ", item.MultipleSI.Select(si => si.ToString()));
                        worksheet.Cells[row, 25].Value = string.Join(", ", item.MultipleSIId.Select(siId => siId.ToString()));
                        worksheet.Cells[row, 26].Value = string.Join(" ", item.SIMultipleAmount.Select(multipleSI => multipleSI.ToString("N4")));
                        worksheet.Cells[row, 27].Value = string.Join(", ", item.MultipleTransactionDate.Select(multipleTransactionDate => multipleTransactionDate.ToString("yyyy-MM-dd")));
                    }
                    worksheet.Cells[row, 28].Value = item.CustomerId;
                    worksheet.Cells[row, 29].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 30].Value = item.CollectionReceiptNo;
                    worksheet.Cells[row, 31].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 32].Value = item.CollectionReceiptId;

                    row++;
                }

                #endregion -- Collection Receipt Export --

                #region Sales Invoice Export --

                int siRow = 2;
                var currentSI = "";

                foreach (var item in selectedList)
                {
                    if (item.SalesInvoice == null)
                    {
                        continue;
                    }
                    if (item.SalesInvoice.SalesInvoiceNo == currentSI)
                    {
                        continue;
                    }

                    currentSI = item.SalesInvoice.SalesInvoiceNo;
                    worksheet3.Cells[siRow, 1].Value = item.SalesInvoice.OtherRefNo;
                    worksheet3.Cells[siRow, 2].Value = item.SalesInvoice.Quantity;
                    worksheet3.Cells[siRow, 3].Value = item.SalesInvoice.UnitPrice;
                    worksheet3.Cells[siRow, 4].Value = item.SalesInvoice.Amount;
                    worksheet3.Cells[siRow, 5].Value = item.SalesInvoice.Remarks;
                    worksheet3.Cells[siRow, 6].Value = item.SalesInvoice.Status;
                    worksheet3.Cells[siRow, 7].Value = item.SalesInvoice.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[siRow, 8].Value = item.SalesInvoice.Discount;
                    worksheet3.Cells[siRow, 9].Value = item.SalesInvoice.AmountPaid;
                    worksheet3.Cells[siRow, 10].Value = item.SalesInvoice.Balance;
                    worksheet3.Cells[siRow, 11].Value = item.SalesInvoice.IsPaid;
                    worksheet3.Cells[siRow, 12].Value = item.SalesInvoice.IsTaxAndVatPaid;
                    worksheet3.Cells[siRow, 13].Value = item.SalesInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[siRow, 14].Value = item.SalesInvoice.CreatedBy;
                    worksheet3.Cells[siRow, 15].Value = item.SalesInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[siRow, 16].Value = item.SalesInvoice.CancellationRemarks;
                    worksheet3.Cells[siRow, 17].Value = item.SalesInvoice.ReceivingReportId;
                    worksheet3.Cells[siRow, 18].Value = item.SalesInvoice.CustomerId;
                    worksheet3.Cells[siRow, 19].Value = item.SalesInvoice.PurchaseOrderId;
                    worksheet3.Cells[siRow, 20].Value = item.SalesInvoice.ProductId;
                    worksheet3.Cells[siRow, 21].Value = item.SalesInvoice.SalesInvoiceNo;
                    worksheet3.Cells[siRow, 22].Value = item.SalesInvoice.SalesInvoiceId;

                    siRow++;
                }

                #endregion Sales Invoice Export --

                #region -- Service Invoice Export --

                int svRow = 2;
                var currentSV = "";

                foreach (var item in selectedList)
                {
                    if (item.ServiceInvoice == null)
                    {
                        continue;
                    }
                    if (item.ServiceInvoice.ServiceInvoiceNo == currentSV)
                    {
                        continue;
                    }

                    currentSV = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet4.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet4.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                    worksheet4.Cells[svRow, 3].Value = item.ServiceInvoice.Amount;
                    worksheet4.Cells[svRow, 4].Value = item.ServiceInvoice.Total;
                    worksheet4.Cells[svRow, 5].Value = item.ServiceInvoice.Discount;
                    worksheet4.Cells[svRow, 6].Value = item.ServiceInvoice.CurrentAndPreviousAmount;
                    worksheet4.Cells[svRow, 7].Value = item.ServiceInvoice.UnearnedAmount;
                    worksheet4.Cells[svRow, 8].Value = item.ServiceInvoice.Status;
                    worksheet4.Cells[svRow, 9].Value = item.ServiceInvoice.AmountPaid;
                    worksheet4.Cells[svRow, 10].Value = item.ServiceInvoice.Balance;
                    worksheet4.Cells[svRow, 11].Value = item.ServiceInvoice.Instructions;
                    worksheet4.Cells[svRow, 12].Value = item.ServiceInvoice.IsPaid;
                    worksheet4.Cells[svRow, 13].Value = item.ServiceInvoice.CreatedBy;
                    worksheet4.Cells[svRow, 14].Value = item.ServiceInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet4.Cells[svRow, 15].Value = item.ServiceInvoice.CancellationRemarks;
                    worksheet4.Cells[svRow, 16].Value = item.ServiceInvoice.CustomerId;
                    worksheet4.Cells[svRow, 17].Value = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet4.Cells[svRow, 18].Value = item.ServiceInvoice.ServiceId;
                    worksheet4.Cells[svRow, 19].Value = item.ServiceInvoice.ServiceInvoiceId;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                #region -- Collection Receipt Export (Multiple SI)--

                List<FilprideSalesInvoice> getSalesInvoice = new List<FilprideSalesInvoice>();

                getSalesInvoice = _dbContext.FilprideSalesInvoices
                    .AsEnumerable()
                    .Where(s => selectedList?.Select(item => item?.MultipleSI).Any(si => si?.Contains(s.SalesInvoiceNo) == true) == true)
                    .OrderBy(si => si.SalesInvoiceNo)
                    .ToList();

                foreach (var item in getSalesInvoice)
                {
                    worksheet3.Cells[siRow, 1].Value = item.OtherRefNo;
                    worksheet3.Cells[siRow, 2].Value = item.Quantity;
                    worksheet3.Cells[siRow, 3].Value = item.UnitPrice;
                    worksheet3.Cells[siRow, 4].Value = item.Amount;
                    worksheet3.Cells[siRow, 5].Value = item.Remarks;
                    worksheet3.Cells[siRow, 6].Value = item.Status;
                    worksheet3.Cells[siRow, 7].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[siRow, 8].Value = item.Discount;
                    worksheet3.Cells[siRow, 9].Value = item.AmountPaid;
                    worksheet3.Cells[siRow, 10].Value = item.Balance;
                    worksheet3.Cells[siRow, 11].Value = item.IsPaid;
                    worksheet3.Cells[siRow, 12].Value = item.IsTaxAndVatPaid;
                    worksheet3.Cells[siRow, 13].Value = item.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[siRow, 14].Value = item.CreatedBy;
                    worksheet3.Cells[siRow, 15].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[siRow, 16].Value = item.CancellationRemarks;
                    worksheet3.Cells[siRow, 17].Value = item.ReceivingReportId;
                    worksheet3.Cells[siRow, 18].Value = item.CustomerId;
                    worksheet3.Cells[siRow, 19].Value = item.PurchaseOrderId;
                    worksheet3.Cells[siRow, 20].Value = item.ProductId;
                    worksheet3.Cells[siRow, 21].Value = item.SalesInvoiceNo;
                    worksheet3.Cells[siRow, 22].Value = item.SalesInvoiceId;

                    siRow++;
                }

                #endregion -- Collection Receipt Export (Multiple SI)--

                #region -- Offsetting Export --

                var crNos = selectedList.Select(item => item.CollectionReceiptNo).ToList();

                var getOffsetting = await _dbContext.FilprideOffsettings
                    .Where(offset => crNos.Contains(offset.Source))
                    .OrderBy(offset => offset.OffSettingId)
                    .ToListAsync();

                int offsetRow = 2;

                foreach (var item in getOffsetting)
                {
                    worksheet2.Cells[offsetRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[offsetRow, 2].Value = item.Source;
                    worksheet2.Cells[offsetRow, 3].Value = item.Reference;
                    worksheet2.Cells[offsetRow, 4].Value = item.IsRemoved;
                    worksheet2.Cells[offsetRow, 5].Value = item.Amount;
                    worksheet2.Cells[offsetRow, 6].Value = item.CreatedBy;
                    worksheet2.Cells[offsetRow, 7].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[offsetRow, 8].Value = item.AccountTitle;

                    offsetRow++;
                }

                #endregion -- Offsetting Export --

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CollectionReceiptList.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCollectionReceiptIds()
        {
            var crIds = _dbContext.FilprideCollectionReceipts
                                     .Where(cr => cr.Type == nameof(DocumentType.Documented))
                                     .Select(cr => cr.CollectionReceiptId) // Assuming Id is the primary key
                                     .ToList();

            return Json(crIds);
        }
    }
}
