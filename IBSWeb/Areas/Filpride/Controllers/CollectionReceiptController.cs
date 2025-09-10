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
using IBS.Models.Filpride.ViewModels;
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
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class CollectionReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CollectionReceiptController> _logger;

        private readonly ICloudStorageService _cloudStorageService;

        public CollectionReceiptController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<CollectionReceiptController> logger,
            ICloudStorageService cloudStorageService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cloudStorageService = cloudStorageService;
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
            if (view != nameof(DynamicView.CollectionReceipt))
            {
                return View();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var collectionReceipts = await _unitOfWork.FilprideCollectionReceipt
                .GetAllAsync(sv => sv.Company == companyClaims && sv.Type == nameof(DocumentType.Documented), cancellationToken);

            return View("ExportIndex", collectionReceipts);

        }

        public IActionResult ServiceInvoiceIndex()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCollectionReceipts([FromForm] DataTablesParameters parameters, string invoiceType, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var collectionReceipts = await _unitOfWork.FilprideCollectionReceipt
                    .GetAllAsync(sv => sv.Company == companyClaims, cancellationToken);

                switch (invoiceType)
                {
                    case "Sales":
                        collectionReceipts = collectionReceipts
                            .Where(s => s.SalesInvoiceId != null || s.MultipleSIId != null)
                            .ToList();
                        break;
                    case "Service":
                        collectionReceipts = collectionReceipts
                            .Where(s => s.ServiceInvoiceId != null)
                            .ToList();
                        break;
                }

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    collectionReceipts = collectionReceipts
                        .Where(s =>
                            s.CollectionReceiptNo!.ToLower().Contains(searchValue) ||
                            s.Customer!.CustomerName.ToLower().Contains(searchValue) ||
                            s.SINo?.ToLower().Contains(searchValue) == true ||
                            s.SVNo?.ToLower().Contains(searchValue) == true ||
                            s.MultipleSI?.Contains(searchValue) == true ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
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
                _logger.LogError(ex, "Failed to get collection receipts. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> SingleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceiptSingleSiViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByAccountTitle(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            return View(viewModel);
        }

        public async Task<IActionResult> GetBanks(CancellationToken cancellationToken = default)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            return Json(await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken));
        }

        [DepartmentAuthorize(SD.Department_TradeAndSupply, SD.Department_RCD)]
        [HttpGet]
        public async Task<IActionResult> Deposit(int id, int bankId, DateOnly depositDate, CancellationToken cancellationToken)
        {
            var bank = await _unitOfWork.FilprideBankAccount
                .GetAsync(b => b.BankAccountId == bankId, cancellationToken);

            if (bank == null)
            {
                return NotFound();
            }

            var model = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.DepositedDate = depositDate;
                model.BankId = bank.BankAccountId;
                model.BankAccountName = bank.AccountName;
                model.BankAccountNumber = bank.AccountNo;

                await _unitOfWork.FilprideCollectionReceipt.DepositAsync(model, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!,
                    $"Record deposit date of collection receipt#{model.CollectionReceiptNo}", "Collection Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Collection Receipt deposited date has been recorded successfully.";

                if (model.SalesInvoiceId != null || model.MultipleSIId != null)
                {
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(ServiceInvoiceIndex));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to record deposit date. Error: {ErrorMessage}, Stack: {StackTrace}. Recorded by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));

                if (model.SalesInvoiceId != null || model.MultipleSIId != null)
                {
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(ServiceInvoiceIndex));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SingleCollectionCreateForSales(CollectionReceiptSingleSiViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                    && !si.IsPaid
                    && si.CustomerId == viewModel.CustomerId
                    && si.PostedBy != null, cancellationToken))
                .OrderBy(s => s.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["warning"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var existingSalesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAsync(si => si.SalesInvoiceId == viewModel.SalesInvoiceId, cancellationToken);

                if (existingSalesInvoice == null)
                {
                    return NotFound();
                }

                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                var model = new FilprideCollectionReceipt
                {
                    CollectionReceiptNo = await _unitOfWork.FilprideCollectionReceipt
                        .GenerateCodeAsync(companyClaims, existingSalesInvoice.Type, cancellationToken),
                    SalesInvoiceId = existingSalesInvoice.SalesInvoiceId,
                    SINo = existingSalesInvoice.SalesInvoiceNo,
                    CustomerId = viewModel.CustomerId,
                    TransactionDate = viewModel.TransactionDate,
                    ReferenceNo = viewModel.ReferenceNo,
                    Remarks = viewModel.Remarks,
                    CashAmount = viewModel.CashAmount,
                    CheckDate = viewModel.CheckDate,
                    CheckNo = viewModel.CheckNo,
                    CheckBank = viewModel.CheckBank,
                    CheckBranch = viewModel.CheckBranch,
                    CheckAmount = viewModel.CheckAmount,
                    ManagersCheckDate = viewModel.ManagersCheckDate,
                    ManagersCheckNo = viewModel.ManagersCheckNo,
                    ManagersCheckBank = viewModel.ManagersCheckBank,
                    ManagersCheckBranch = viewModel.ManagersCheckBranch,
                    ManagersCheckAmount = viewModel.ManagersCheckAmount,
                    EWT = viewModel.EWT,
                    WVAT = viewModel.WVAT,
                    Total = total,
                    CreatedBy = User.Identity!.Name,
                    Company = companyClaims,
                    Type = existingSalesInvoice.Type,
                };

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    model.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    model.F2306FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, model.F2306FileName!);
                    model.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    model.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    model.F2307FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, model.F2307FileName!);
                    model.IsCertificateUpload = true;
                }

                await _unitOfWork.FilprideCollectionReceipt.AddAsync(model, cancellationToken);

                var details = new FilprideCollectionReceiptDetail
                {
                    CollectionReceiptId = model.CollectionReceiptId,
                    CollectionReceiptNo = model.CollectionReceiptNo,
                    InvoiceDate = DateOnly.FromDateTime(existingSalesInvoice.CreatedDate),
                    InvoiceNo = existingSalesInvoice.SalesInvoiceNo!,
                    Amount = model.Total
                };

                await _dbContext.FilprideCollectionReceiptDetails.AddAsync(details, cancellationToken);

                #endregion --Saving default value

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                //
                // var offsettings = new List<FilprideOffsettings>();
                //
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     offsettings.Add(
                //         new FilprideOffsettings
                //         {
                //             AccountNo = accountTitle[i],
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = viewModel.CollectionReceiptNo,
                //             Reference = viewModel.SINo,
                //             Amount = currentAccountAmount,
                //             Company = viewModel.Company,
                //             CreatedBy = viewModel.CreatedBy,
                //             CreatedDate = viewModel.CreatedDate
                //         }
                //     );
                // }
                //
                // await _dbContext.AddRangeAsync(offsettings, cancellationToken);
                //
                // #endregion --Offsetting function

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SINo!, model.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateInvoice(model.SalesInvoice!.SalesInvoiceId, model.Total, offsetAmount, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt",
                    model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Collection receipt #{model.CollectionReceiptNo} created successfully.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create sales invoice single collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceiptMultipleSiViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CollectionReceiptMultipleSiViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                    && !si.IsPaid
                    && si.CustomerId == viewModel.CustomerId
                    && si.PostedBy != null, cancellationToken))
                .OrderBy(s => s.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["warning"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                #region --Saving default value

                var model = new FilprideCollectionReceipt
                {
                    TransactionDate = viewModel.TransactionDate,
                    CustomerId = viewModel.CustomerId,
                    ReferenceNo = viewModel.ReferenceNo,
                    Remarks = viewModel.Remarks,
                    CashAmount = viewModel.CashAmount,
                    CheckAmount = viewModel.CheckAmount,
                    CheckNo = viewModel.CheckNo,
                    CheckBranch = viewModel.CheckBranch,
                    CheckDate = viewModel.CheckDate,
                    CheckBank = viewModel.CheckBank,
                    ManagersCheckDate = viewModel.ManagersCheckDate,
                    ManagersCheckNo = viewModel.ManagersCheckNo,
                    ManagersCheckBank = viewModel.ManagersCheckBank,
                    ManagersCheckBranch = viewModel.ManagersCheckBranch,
                    ManagersCheckAmount = viewModel.ManagersCheckAmount,
                    EWT = viewModel.EWT,
                    WVAT = viewModel.WVAT,
                    Total = total,
                    CreatedBy = User.Identity!.Name,
                    Company = companyClaims,
                    MultipleSIId = viewModel.MultipleSIId,
                    SIMultipleAmount = viewModel.SIMultipleAmount,
                };

                model.MultipleSI = new string[model.MultipleSIId.Length];
                model.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];

                await _unitOfWork.FilprideCollectionReceipt.AddAsync(model, cancellationToken);

                var details = new List<FilprideCollectionReceiptDetail>();

                for (var i = 0; i < viewModel.MultipleSIId.Length; i++)
                {
                    var siId = viewModel.MultipleSIId[i];
                    var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAsync(si => si.SalesInvoiceId == siId, cancellationToken);

                    if (salesInvoice == null)
                    {
                        throw new InvalidOperationException("Sales Invoice not found");
                    }

                    model.MultipleSI[i] = salesInvoice.SalesInvoiceNo!;
                    model.MultipleTransactionDate[i] = salesInvoice.TransactionDate;

                    if (model.Type == null)
                    {
                        model.Type = salesInvoice.Type;

                        model.CollectionReceiptNo = await _unitOfWork.FilprideCollectionReceipt
                            .GenerateCodeAsync(companyClaims, model.Type!, cancellationToken);
                    }

                    details.Add(new FilprideCollectionReceiptDetail
                    {
                        CollectionReceiptId = model.CollectionReceiptId,
                        CollectionReceiptNo = model.CollectionReceiptNo!,
                        InvoiceDate = DateOnly.FromDateTime(salesInvoice.CreatedDate),
                        InvoiceNo = salesInvoice.SalesInvoiceNo!,
                        Amount = viewModel.SIMultipleAmount[i],
                    });
                }

                await _dbContext.FilprideCollectionReceiptDetails.AddRangeAsync(details, cancellationToken);

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    model.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    model.F2306FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, model.F2306FileName!);
                    model.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    model.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    model.F2307FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, model.F2307FileName!);
                    model.IsCertificateUpload = true;
                }

                #endregion --Saving default value

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                // var offsettings = new List<FilprideOffsettings>();
                //
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     offsettings.Add(
                //         new FilprideOffsettings
                //         {
                //             AccountNo = accountTitle[i],
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = viewModel.CollectionReceiptNo,
                //             Reference = viewModel.SINo,
                //             Amount = currentAccountAmount,
                //             Company = viewModel.Company,
                //             CreatedBy = viewModel.CreatedBy,
                //             CreatedDate = viewModel.CreatedDate
                //         }
                //     );
                // }
                //
                // await _dbContext.AddRangeAsync(offsettings, cancellationToken);
                //
                // #endregion --Offsetting function

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SINo!, model.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateMultipleInvoice(model.MultipleSI!, model.SIMultipleAmount!, offsetAmount, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt",
                    model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = $"Collection receipt #{model.CollectionReceiptNo} created successfully.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create sales invoice multiple collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionEdit(int? id, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var listOfDetails = await _dbContext.FilprideCollectionReceiptDetails
                .Where(x => x.CollectionReceiptId == id).ToListAsync(cancellationToken);

            var crPayments = new List<InvoicePayment>();

            foreach (var detail in listOfDetails)
            {
                var crPayment = new InvoicePayment
                {
                    InvoiceId = (await _dbContext.FilprideSalesInvoices
                            .Where(si => si.SalesInvoiceNo == detail.InvoiceNo).FirstOrDefaultAsync(cancellationToken))!
                        .SalesInvoiceId,
                    InvoiceNumber = detail.InvoiceNo,
                    PaymentAmount = detail.Amount
                };
                crPayments.Add(crPayment);
            }

            var viewModel = new CollectionReceiptMultipleSiViewModel
            {
                CollectionReceiptId = existingModel.CollectionReceiptId,
                CustomerId = existingModel.CustomerId,
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                TransactionDate = existingModel.TransactionDate,
                ReferenceNo = existingModel.ReferenceNo,
                Remarks = existingModel.Remarks,
                MultipleSIId = existingModel.MultipleSIId!,
                SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                                           && !si.IsPaid
                                           && si.CustomerId == existingModel.CustomerId
                                           && si.PostedBy != null, cancellationToken))
                    .OrderBy(s => s.SalesInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SalesInvoiceId.ToString(),
                        Text = s.SalesInvoiceNo
                    })
                    .ToList(),
                CashAmount = existingModel.CashAmount,
                CheckBranch = existingModel.CheckBranch,
                CheckNo = existingModel.CheckNo,
                CheckDate = existingModel.CheckDate,
                CheckAmount = existingModel.CheckAmount,
                CheckBank = existingModel.CheckBank,
                ManagersCheckDate = existingModel.ManagersCheckDate,
                ManagersCheckNo = existingModel.ManagersCheckNo,
                ManagersCheckBank = existingModel.ManagersCheckBank,
                ManagersCheckBranch = existingModel.ManagersCheckBranch,
                ManagersCheckAmount = existingModel.ManagersCheckAmount,
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                EWT = existingModel.EWT,
                WVAT = existingModel.WVAT,
                HasAlready2306 = existingModel.F2306FilePath != null,
                HasAlready2307 = existingModel.F2307FilePath != null,
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                SIMultipleAmount = existingModel.SIMultipleAmount!,
                InvoicePayments = crPayments
            };

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.Offsettings = offsettings;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MultipleCollectionEdit(CollectionReceiptMultipleSiViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == viewModel.CollectionReceiptId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                    && !si.IsPaid
                    && si.CustomerId == existingModel.CustomerId
                    && si.PostedBy != null, cancellationToken))
                .OrderBy(s => s.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["error"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                existingModel.CustomerId = viewModel.CustomerId;
                existingModel.TransactionDate = viewModel.TransactionDate;
                existingModel.ReferenceNo = viewModel.ReferenceNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.CashAmount = viewModel.CashAmount;
                existingModel.CheckAmount = viewModel.CheckAmount;
                existingModel.CheckNo = viewModel.CheckNo;
                existingModel.CheckBranch = viewModel.CheckBranch;
                existingModel.CheckDate = viewModel.CheckDate;
                existingModel.CheckBank = viewModel.CheckBank;
                existingModel.ManagersCheckDate = viewModel.ManagersCheckDate;
                existingModel.ManagersCheckNo = viewModel.ManagersCheckNo;
                existingModel.ManagersCheckBank = viewModel.ManagersCheckBank;
                existingModel.ManagersCheckBranch = viewModel.ManagersCheckBranch;
                existingModel.ManagersCheckAmount = viewModel.ManagersCheckAmount;
                existingModel.EWT = viewModel.EWT;
                existingModel.WVAT = viewModel.WVAT;
                existingModel.Total = total;
                existingModel.MultipleSIId = new int[viewModel.MultipleSIId.Length];
                existingModel.MultipleSI = new string[viewModel.MultipleSIId.Length];
                existingModel.SIMultipleAmount = new decimal[viewModel.MultipleSIId.Length];
                existingModel.MultipleTransactionDate = new DateOnly[viewModel.MultipleSIId.Length];

                var listOfDetails = await _dbContext.FilprideCollectionReceiptDetails
                    .Where(crd => crd.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .ToListAsync(cancellationToken);

                foreach (var detail in listOfDetails)
                {
                    await _unitOfWork.FilprideCollectionReceipt.UndoSalesInvoiceChanges(detail, cancellationToken);
                }

                await _dbContext.FilprideCollectionReceiptDetails
                    .Where(x => x.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .ExecuteDeleteAsync(cancellationToken);

                var details = new List<FilprideCollectionReceiptDetail>();

                // looping all the new SI
                for (var i = 0; i < viewModel.MultipleSIId.Length; i++)
                {
                    var siId = viewModel.MultipleSIId[i];
                    var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAsync(si => si.SalesInvoiceId == siId, cancellationToken);

                    if (salesInvoice == null)
                    {
                        throw new InvalidOperationException("Sales Invoice not found");
                    }

                    existingModel.MultipleSIId[i] = viewModel.MultipleSIId[i];
                    existingModel.MultipleSI[i] = salesInvoice.SalesInvoiceNo!;
                    existingModel.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                    existingModel.SIMultipleAmount[i] = viewModel.SIMultipleAmount[i];

                    details.Add(new  FilprideCollectionReceiptDetail
                    {
                        CollectionReceiptId = existingModel.CollectionReceiptId,
                        CollectionReceiptNo = existingModel.CollectionReceiptNo!,
                        InvoiceDate = salesInvoice.TransactionDate,
                        InvoiceNo = salesInvoice.SalesInvoiceNo!,
                        Amount = existingModel.SIMultipleAmount[i],
                    });
                }

                await _dbContext.FilprideCollectionReceiptDetails.AddRangeAsync(details, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(existingModel.CollectionReceiptNo!, existingModel.SINo!, existingModel.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateMultipleInvoice(existingModel.MultipleSI!, existingModel.SIMultipleAmount!, offsetAmount, cancellationToken);

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    existingModel.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    existingModel.F2306FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, existingModel.F2306FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    existingModel.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    existingModel.F2307FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, existingModel.F2307FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                #endregion --Saving default value

                if (!_dbContext.ChangeTracker.HasChanges())
                {
                    TempData["warning"] = "No data changes!";
                    return View(viewModel);
                }

                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                //
                // var findOffsettings = await _dbContext.FilprideOffsettings
                // .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                // .ToListAsync(cancellationToken);
                //
                // var accountTitleSet = new HashSet<string>(accountTitle);
                //
                // // Remove records not in accountTitle
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleSet.Contains(offsetting.AccountNo))
                //     {
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // // Dictionary to keep track of AccountNo and their ids for comparison
                // var accountTitleDict = new Dictionary<string, List<int>>();
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                //     {
                //         accountTitleDict[offsetting.AccountNo] = new List<int>();
                //     }
                //     accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                // }
                //
                // // Add or update records
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var accountNo = accountTitle[i];
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     if (accountTitleDict.TryGetValue(accountNo, out var ids))
                //     {
                //         // Update the first matching record and remove it from the list
                //         var offsettingId = ids.First();
                //         ids.RemoveAt(0);
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);
                //
                //         offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                //         offsetting.Amount = currentAccountAmount;
                //         offsetting.CreatedBy = _userManager.GetUserName(this.User);
                //         offsetting.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                //
                //         if (ids.Count == 0)
                //         {
                //             accountTitleDict.Remove(accountNo);
                //         }
                //     }
                //     else
                //     {
                //         // Add new record
                //         var newOffsetting = new FilprideOffsettings
                //         {
                //             AccountNo = accountNo,
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = existingModel.CollectionReceiptNo!,
                //             Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                //             Amount = currentAccountAmount,
                //             CreatedBy = _userManager.GetUserName(this.User),
                //             CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                //         };
                //         _dbContext.FilprideOffsettings.Add(newOffsetting);
                //     }
                // }
                //
                // // Remove remaining records that were duplicates
                // foreach (var ids in accountTitleDict.Values)
                // {
                //     foreach (var id in ids)
                //     {
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // #endregion --Offsetting function

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Collection Receipt updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update sales invoice multiple collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateForService(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceiptServiceViewModel();
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForService(CollectionReceiptServiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);
            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            viewModel.ServiceInvoices = (await _unitOfWork.FilprideServiceInvoice
                .GetAllAsync(si => si.Company == companyClaims
                                   && !si.IsPaid
                                   && si.CustomerId == viewModel.CustomerId
                                   && si.PostedBy != null, cancellationToken))
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["warning"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var existingServiceInvoice = await _dbContext.FilprideServiceInvoices
                    .FirstOrDefaultAsync(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId,
                        cancellationToken);

                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                if (existingServiceInvoice == null)
                {
                    return NotFound();
                }

                var model = new FilprideCollectionReceipt
                {
                    CollectionReceiptNo = await _unitOfWork.FilprideCollectionReceipt
                        .GenerateCodeAsync(companyClaims, existingServiceInvoice.Type, cancellationToken),
                    ServiceInvoiceId = existingServiceInvoice.ServiceInvoiceId,
                    SVNo = existingServiceInvoice.ServiceInvoiceNo,
                    CustomerId = viewModel.CustomerId,
                    TransactionDate = viewModel.TransactionDate,
                    ReferenceNo = viewModel.ReferenceNo,
                    Remarks = viewModel.Remarks,
                    CashAmount = viewModel.CashAmount,
                    CheckNo = viewModel.CheckNo,
                    CheckBranch = viewModel.CheckBranch,
                    CheckDate = viewModel.CheckDate,
                    CheckAmount = viewModel.CheckAmount,
                    CheckBank = viewModel.CheckBank,
                    ManagersCheckDate = viewModel.ManagersCheckDate,
                    ManagersCheckNo = viewModel.ManagersCheckNo,
                    ManagersCheckBank = viewModel.ManagersCheckBank,
                    ManagersCheckBranch = viewModel.ManagersCheckBranch,
                    ManagersCheckAmount = viewModel.ManagersCheckAmount,
                    EWT = viewModel.EWT,
                    WVAT = viewModel.WVAT,
                    Total = total,
                    CreatedBy = User.Identity!.Name,
                    Company = companyClaims,
                    Type = existingServiceInvoice.Type
                };

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SINo!, model.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateSV(model.ServiceInvoice!.ServiceInvoiceId, model.Total, offsetAmount, cancellationToken);

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    model.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    model.F2306FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, model.F2306FileName!);
                    model.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    model.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    model.F2307FilePath =
                        await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, model.F2307FileName!);
                    model.IsCertificateUpload = true;
                }

                await _unitOfWork.FilprideCollectionReceipt.AddAsync(model, cancellationToken);

                var details = new FilprideCollectionReceiptDetail
                {
                    CollectionReceiptId = model.CollectionReceiptId,
                    CollectionReceiptNo = model.CollectionReceiptNo,
                    InvoiceDate = DateOnly.FromDateTime(existingServiceInvoice.CreatedDate),
                    InvoiceNo = existingServiceInvoice.ServiceInvoiceNo,
                    Amount = model.Total
                };

                await _dbContext.FilprideCollectionReceiptDetails.AddAsync(details, cancellationToken);

                #endregion --Saving default value

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                // var offsettings = new List<FilprideOffsettings>();
                //
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     offsettings.Add(
                //         new FilprideOffsettings
                //         {
                //             AccountNo = accountTitle[i],
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = viewModel.CollectionReceiptNo,
                //             Reference = viewModel.SVNo,
                //             Amount = currentAccountAmount,
                //             Company = viewModel.Company,
                //             CreatedBy = viewModel.CreatedBy,
                //             CreatedDate = viewModel.CreatedDate
                //         }
                //     );
                // }
                //
                // await _dbContext.AddRangeAsync(offsettings, cancellationToken);
                //
                // #endregion --Offsetting function

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!,
                    $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt",
                    model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Collection receipt #{model.CollectionReceiptNo} created successfully.";
                return RedirectToAction(nameof(ServiceInvoiceIndex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create service invoice collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (cr == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview collection receipt# {cr.CollectionReceiptNo}", "Collection Receipt", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(cr);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            var invoices = (await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(si => si.Company == companyClaims
                                   && !si.IsPaid
                                   && si.CustomerId == customerNo
                                   && si.PostedBy != null, cancellationToken))
                .OrderBy(si => si.SalesInvoiceId)
                .ToList();

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
            var companyClaims = await GetCompanyClaimAsync();

            var invoices = (await _unitOfWork.FilprideServiceInvoice
                .GetAllAsync(si => si.Company == companyClaims
                                   && si.CustomerId == customerNo
                                   && !si.IsPaid
                                   && si.PostedBy != null, cancellationToken))
                .OrderBy(si => si.ServiceInvoiceId)
                .ToList();

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
                var si = await _unitOfWork.FilprideSalesInvoice
                    .GetAsync(s => s.SalesInvoiceId == invoiceNo, cancellationToken);

                if (si == null)
                {
                    return NotFound();
                }

                var vatType = si.CustomerOrderSlip?.VatType ?? si.Customer!.VatType;
                var hasEwt = si.CustomerOrderSlip?.HasEWT ?? si.Customer!.WithHoldingTax;
                var hasWvat = si.CustomerOrderSlip?.HasWVAT ?? si.Customer!.WithHoldingVat;

                var netDiscount = si.Amount - si.Discount;
                var netOfVatAmount = vatType == SD.VatType_Vatable
                    ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount)
                    : netDiscount;
                var withHoldingTaxAmount = hasEwt
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m)
                    : 0;
                var withHoldingVatAmount = hasWvat
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m)
                    : 0;

                return Json(new
                {
                    Amount = netDiscount.ToString(SD.Two_Decimal_Format),
                    AmountPaid = si.AmountPaid.ToString(SD.Two_Decimal_Format),
                    Balance = si.Balance.ToString(SD.Two_Decimal_Format),
                    Ewt = withHoldingTaxAmount.ToString(SD.Two_Decimal_Format),
                    Wvat = withHoldingVatAmount.ToString(SD.Two_Decimal_Format),
                    Total = (netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)).ToString(SD.Two_Decimal_Format)
                });
            }

            if (isServices && !isSales)
            {
                var sv = await _unitOfWork.FilprideServiceInvoice
                    .GetAsync(s => s.ServiceInvoiceId == invoiceNo, cancellationToken);

                if (sv == null)
                {
                    return NotFound();
                }

                var netOfVatAmount = sv.VatType == SD.VatType_Vatable
                    ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(sv.Total) - sv.Discount
                    : sv.Total - sv.Discount;
                var withHoldingTaxAmount = sv.HasEwt
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m)
                    : 0;
                var withHoldingVatAmount = sv.HasWvat
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m)
                    : 0;

                return Json(new
                {
                    Amount = sv.Total.ToString(SD.Two_Decimal_Format),
                    AmountPaid = sv.AmountPaid.ToString(SD.Two_Decimal_Format),
                    Balance = sv.Balance.ToString(SD.Two_Decimal_Format),
                    Ewt = withHoldingTaxAmount.ToString(SD.Two_Decimal_Format),
                    Wvat = withHoldingVatAmount.ToString(SD.Two_Decimal_Format),
                    Total = (sv.Total - (withHoldingTaxAmount + withHoldingVatAmount)).ToString(SD.Two_Decimal_Format)
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] siNo, bool isSales, CancellationToken cancellationToken)
        {
            if (isSales)
            {
                var si = await _unitOfWork.FilprideSalesInvoice
                    .GetAsync(si => siNo.Contains(si.SalesInvoiceId), cancellationToken);

                if (si == null)
                {
                    return Json(null);
                }

                var vatType = si.CustomerOrderSlip?.VatType ?? si.Customer!.VatType;
                var hasEwt = si.CustomerOrderSlip?.HasEWT ?? si.Customer!.WithHoldingTax;
                var hasWvat = si.CustomerOrderSlip?.HasWVAT ?? si.Customer!.WithHoldingVat;

                var netDiscount = si.Amount - si.Discount;
                var netOfVatAmount = vatType == SD.VatType_Vatable
                    ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount)
                    : netDiscount;
                var withHoldingTaxAmount = hasEwt
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m)
                    : 0;
                var withHoldingVatAmount = hasWvat
                    ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m)
                    : 0;

                return Json(new
                {
                    Amount = netDiscount,
                    si.AmountPaid,
                    si.Balance,
                    WithholdingTax = withHoldingTaxAmount,
                    WithholdingVat = withHoldingVatAmount,
                    Total = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)
                });
            }
            else
            {
                var sv = await _unitOfWork.FilprideServiceInvoice
                    .GetAsync(sv => siNo.Contains(sv.ServiceInvoiceId), cancellationToken);

                if (sv == null)
                {
                    return Json(null);
                }

                decimal netDiscount = sv.Total - sv.Discount;
                decimal netOfVatAmount = sv.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount) : netDiscount;
                decimal withHoldingTaxAmount = sv.HasEwt ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = sv.HasWvat ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = netDiscount,
                    sv.AmountPaid,
                    sv.Balance,
                    WithholdingTax = withHoldingTaxAmount,
                    WithholdingVat = withHoldingVatAmount,
                    Total = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditForSales(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var viewModel = new CollectionReceiptSingleSiViewModel
            {
                CollectionReceiptId = existingModel.CollectionReceiptId,
                CustomerId = existingModel.CustomerId,
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                TransactionDate = existingModel.TransactionDate,
                ReferenceNo = existingModel.ReferenceNo,
                Remarks = existingModel.Remarks,
                SalesInvoiceId = existingModel.SalesInvoiceId ?? 0,
                SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                        && !si.IsPaid
                        && si.CustomerId == existingModel.CustomerId
                        && si.PostedBy != null, cancellationToken))
                    .OrderBy(s => s.SalesInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SalesInvoiceId.ToString(),
                        Text = s.SalesInvoiceNo
                    })
                    .ToList(),
                CashAmount = existingModel.CashAmount,
                CheckDate = existingModel.CheckDate,
                CheckNo = existingModel.CheckNo,
                CheckBranch = existingModel.CheckBranch,
                CheckAmount = existingModel.CheckAmount,
                CheckBank = existingModel.CheckBank,
                ManagersCheckDate = existingModel.ManagersCheckDate,
                ManagersCheckNo = existingModel.ManagersCheckNo,
                ManagersCheckBank = existingModel.ManagersCheckBank,
                ManagersCheckBranch = existingModel.ManagersCheckBranch,
                ManagersCheckAmount = existingModel.ManagersCheckAmount,
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                EWT = existingModel.EWT,
                WVAT = existingModel.WVAT,
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                HasAlready2306 = existingModel.F2306FilePath != null,
                HasAlready2307 = existingModel.F2307FileName != null
            };

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Company == companyClaims
                                 && offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.Offsettings = offsettings;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditForSales(CollectionReceiptSingleSiViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == viewModel.CollectionReceiptId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice.GetAllAsync(si => si.Company == companyClaims
                    && !si.IsPaid
                    && si.CustomerId == existingModel.CustomerId
                    && si.PostedBy != null, cancellationToken))
                .OrderBy(s => s.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["warning"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var existingSalesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAsync(si => si.SalesInvoiceId == viewModel.SalesInvoiceId, cancellationToken);

                if (existingSalesInvoice == null)
                {
                    return NotFound();
                }

                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                existingModel.SalesInvoiceId = existingSalesInvoice.SalesInvoiceId;
                existingModel.SINo = existingSalesInvoice.SalesInvoiceNo;
                existingModel.CustomerId = viewModel.CustomerId;
                existingModel.TransactionDate = viewModel.TransactionDate;
                existingModel.ReferenceNo = viewModel.ReferenceNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.CheckDate = viewModel.CheckDate;
                existingModel.CheckNo = viewModel.CheckNo;
                existingModel.CheckBranch = viewModel.CheckBranch;
                existingModel.CheckAmount = viewModel.CheckAmount;
                existingModel.CheckBank = viewModel.CheckBank;
                existingModel.ManagersCheckDate = viewModel.ManagersCheckDate;
                existingModel.ManagersCheckNo = viewModel.ManagersCheckNo;
                existingModel.ManagersCheckBank = viewModel.ManagersCheckBank;
                existingModel.ManagersCheckBranch = viewModel.ManagersCheckBranch;
                existingModel.ManagersCheckAmount = viewModel.ManagersCheckAmount;
                existingModel.CashAmount = viewModel.CashAmount;
                existingModel.EWT = viewModel.EWT;
                existingModel.WVAT = viewModel.WVAT;
                existingModel.Total = total;

                var detail = await _dbContext.FilprideCollectionReceiptDetails
                    .Where(crd => crd.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (detail == null)
                {
                    throw new NullReferenceException("Collection Receipt Details Not Found.");
                }

                await _unitOfWork.FilprideCollectionReceipt.UndoSalesInvoiceChanges(detail, cancellationToken);

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    existingModel.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    existingModel.F2306FilePath = await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, existingModel.F2306FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    existingModel.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    existingModel.F2307FilePath = await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, existingModel.F2307FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                if (!_dbContext.ChangeTracker.HasChanges())
                {
                    TempData["warning"] = "No data changes!";
                    return View(viewModel);
                }

                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await _dbContext.FilprideCollectionReceiptDetails
                    .Where(x => x.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .ExecuteDeleteAsync(cancellationToken);

                var details = new FilprideCollectionReceiptDetail
                {
                    CollectionReceiptId = existingModel.CollectionReceiptId,
                    CollectionReceiptNo = existingModel.CollectionReceiptNo!,
                    InvoiceDate = DateOnly.FromDateTime(existingSalesInvoice.CreatedDate),
                    InvoiceNo = existingSalesInvoice.SalesInvoiceNo!,
                    Amount = existingModel.Total
                };

                await _dbContext.FilprideCollectionReceiptDetails.AddAsync(details, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(existingModel.CollectionReceiptNo!, existingModel.SINo!, existingModel.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateInvoice(existingModel.SalesInvoice!.SalesInvoiceId, existingModel.Total, offsetAmount, cancellationToken);

                #endregion --Saving default value

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                //
                // var findOffsettings = await _dbContext.FilprideOffsettings
                // .Where(offset => offset.Company == companyClaims && offset.Source == existingModel.CollectionReceiptNo)
                // .ToListAsync(cancellationToken);
                //
                // var accountTitleSet = new HashSet<string>(accountTitle);
                //
                // // Remove records not in accountTitle
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleSet.Contains(offsetting.AccountNo))
                //     {
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // // Dictionary to keep track of AccountNo and their ids for comparison
                // var accountTitleDict = new Dictionary<string, List<int>>();
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                //     {
                //         accountTitleDict[offsetting.AccountNo] = new List<int>();
                //     }
                //     accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                // }
                //
                // // Add or update records
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var accountNo = accountTitle[i];
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     if (accountTitleDict.TryGetValue(accountNo, out var ids))
                //     {
                //         // Update the first matching record and remove it from the list
                //         var offsettingId = ids.First();
                //         ids.RemoveAt(0);
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);
                //
                //         offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                //         offsetting.Amount = currentAccountAmount;
                //         offsetting.CreatedBy = _userManager.GetUserName(this.User);
                //         offsetting.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                //         offsetting.Company = companyClaims;
                //
                //         if (ids.Count == 0)
                //         {
                //             accountTitleDict.Remove(accountNo);
                //         }
                //     }
                //     else
                //     {
                //         // Add new record
                //         var newOffsetting = new FilprideOffsettings
                //         {
                //             AccountNo = accountNo,
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = existingModel.CollectionReceiptNo!,
                //             Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                //             Amount = currentAccountAmount,
                //             CreatedBy = _userManager.GetUserName(this.User),
                //             CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                //         };
                //         _dbContext.FilprideOffsettings.Add(newOffsetting);
                //     }
                // }
                //
                // // Remove remaining records that were duplicates
                // foreach (var ids in accountTitleDict.Values)
                // {
                //     foreach (var id in ids)
                //     {
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // #endregion --Offsetting function

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = "Collection receipt successfully updated.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditForService(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            var viewModel = new CollectionReceiptServiceViewModel
            {
                CollectionReceiptId = existingModel.CollectionReceiptId,
                CustomerId = existingModel.CustomerId,
                Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken),
                TransactionDate = existingModel.TransactionDate,
                ReferenceNo = existingModel.ReferenceNo,
                Remarks = existingModel.Remarks,
                ServiceInvoiceId = existingModel.ServiceInvoiceId ?? 0,
                ServiceInvoices = (await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(si => si.Company == companyClaims
                                 && !si.IsPaid
                                 && si.CustomerId == existingModel.CustomerId
                                 && si.PostedBy != null, cancellationToken))
                    .OrderBy(si => si.ServiceInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceInvoiceId.ToString(),
                        Text = s.ServiceInvoiceNo
                    })
                    .ToList(),
                CashAmount = existingModel.CashAmount,
                CheckDate = existingModel.CheckDate,
                CheckNo = existingModel.CheckNo,
                CheckBank = existingModel.CheckBank,
                CheckBranch = existingModel.CheckBranch,
                CheckAmount = existingModel.CheckAmount,
                ManagersCheckDate = existingModel.ManagersCheckDate,
                ManagersCheckNo = existingModel.ManagersCheckNo,
                ManagersCheckBank = existingModel.ManagersCheckBank,
                ManagersCheckBranch = existingModel.ManagersCheckBranch,
                ManagersCheckAmount = existingModel.ManagersCheckAmount,
                BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken),
                EWT = existingModel.EWT,
                WVAT = existingModel.WVAT,
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                HasAlready2306 = existingModel.F2306FilePath != null,
                HasAlready2307 = existingModel.F2307FileName != null
            };

            var offsettings = await _dbContext.FilprideOffsettings
                .Where(offset => offset.Company == companyClaims
                                 && offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.Offsettings = offsettings;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditForService(CollectionReceiptServiceViewModel viewModel, CancellationToken cancellationToken)
        {
            var existingModel = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == viewModel.CollectionReceiptId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims, cancellationToken);

            viewModel.ServiceInvoices = (await _unitOfWork.FilprideServiceInvoice
                    .GetAllAsync(si => si.Company == companyClaims
                                       && !si.IsPaid
                                       && si.CustomerId == existingModel.CustomerId
                                       && si.PostedBy != null, cancellationToken))
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToList();

            viewModel.ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken);

            viewModel.BankAccounts = await _unitOfWork.GetFilprideBankAccountListById(companyClaims, cancellationToken);

            var total = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagersCheckAmount + viewModel.EWT + viewModel.WVAT;
            if (total == 0)
            {
                TempData["warning"] = "Please input at least one type form of payment";
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The information you submitted is not valid!";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var existingServiceInvoice = await _unitOfWork.FilprideServiceInvoice
                    .GetAsync(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId, cancellationToken);

                if (existingServiceInvoice == null)
                {
                    return NotFound();
                }

                var bankAccount = await _unitOfWork.FilprideBankAccount
                    .GetAsync(b => b.BankAccountId == viewModel.BankId, cancellationToken);

                existingModel.ServiceInvoiceId = existingServiceInvoice.ServiceInvoiceId;
                existingModel.SVNo = existingServiceInvoice.ServiceInvoiceNo;
                existingModel.CustomerId = viewModel.CustomerId;
                existingModel.TransactionDate = viewModel.TransactionDate;
                existingModel.ReferenceNo = viewModel.ReferenceNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.CheckDate = viewModel.CheckDate;
                existingModel.CheckNo = viewModel.CheckNo;
                existingModel.CheckBranch = viewModel.CheckBranch;
                existingModel.CheckAmount = viewModel.CheckAmount;
                existingModel.CheckBank = viewModel.CheckBank;
                existingModel.ManagersCheckDate = viewModel.ManagersCheckDate;
                existingModel.ManagersCheckNo = viewModel.ManagersCheckNo;
                existingModel.ManagersCheckBank = viewModel.ManagersCheckBank;
                existingModel.ManagersCheckBranch = viewModel.ManagersCheckBranch;
                existingModel.ManagersCheckAmount = viewModel.ManagersCheckAmount;
                existingModel.CashAmount = viewModel.CashAmount;
                existingModel.EWT = viewModel.EWT;
                existingModel.WVAT = viewModel.WVAT;
                existingModel.Total = total;

                var detail = await _dbContext.FilprideCollectionReceiptDetails
                    .Where(crd => crd.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (detail == null)
                {
                    throw new NullReferenceException("Collection Receipt Details Not Found.");
                }

                await _unitOfWork.FilprideCollectionReceipt.UndoServiceInvoiceChanges(detail, cancellationToken);

                if (viewModel.Bir2306 != null && viewModel.Bir2306.Length > 0)
                {
                    existingModel.F2306FileName = GenerateFileNameToSave(viewModel.Bir2306.FileName);
                    existingModel.F2306FilePath = await _cloudStorageService.UploadFileAsync(viewModel.Bir2306, existingModel.F2306FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                if (viewModel.Bir2307 != null && viewModel.Bir2307.Length > 0)
                {
                    existingModel.F2307FileName = GenerateFileNameToSave(viewModel.Bir2307.FileName);
                    existingModel.F2307FilePath = await _cloudStorageService.UploadFileAsync(viewModel.Bir2307, existingModel.F2307FileName!);
                    existingModel.IsCertificateUpload = true;
                }

                if (!_dbContext.ChangeTracker.HasChanges())
                {
                    TempData["warning"] = "No data changes!";
                    return View(viewModel);
                }

                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                await _dbContext.FilprideCollectionReceiptDetails
                    .Where(x => x.CollectionReceiptId == existingModel.CollectionReceiptId)
                    .ExecuteDeleteAsync(cancellationToken);

                var details = new FilprideCollectionReceiptDetail
                {
                    CollectionReceiptId = existingModel.CollectionReceiptId,
                    CollectionReceiptNo = existingModel.CollectionReceiptNo!,
                    InvoiceDate = DateOnly.FromDateTime(existingServiceInvoice.CreatedDate),
                    InvoiceNo = existingServiceInvoice.ServiceInvoiceNo,
                    Amount = existingModel.Total
                };

                await _dbContext.FilprideCollectionReceiptDetails.AddAsync(details, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);

                var offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(existingModel.CollectionReceiptNo!, existingModel.SINo!, existingModel.Company, cancellationToken);
                var offsetAmount = offset.Sum(o => o.Amount);
                await _unitOfWork.FilprideCollectionReceipt.UpdateSV(existingModel.ServiceInvoice!.ServiceInvoiceId, existingModel.Total, offsetAmount, cancellationToken);

                #endregion --Saving default value

                // #region --Offsetting function
                //
                // decimal offsetAmount = 0;
                //
                // var findOffsettings = await _dbContext.FilprideOffsettings
                // .Where(offset => offset.Company == companyClaims && offset.Source == existingModel.CollectionReceiptNo)
                // .ToListAsync(cancellationToken);
                //
                // var accountTitleSet = new HashSet<string>(accountTitle);
                //
                // // Remove records not in accountTitle
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleSet.Contains(offsetting.AccountNo))
                //     {
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // // Dictionary to keep track of AccountNo and their ids for comparison
                // var accountTitleDict = new Dictionary<string, List<int>>();
                // foreach (var offsetting in findOffsettings)
                // {
                //     if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                //     {
                //         accountTitleDict[offsetting.AccountNo] = new List<int>();
                //     }
                //     accountTitleDict[offsetting.AccountNo].Add(offsetting.OffSettingId);
                // }
                //
                // // Add or update records
                // for (int i = 0; i < accountTitle.Length; i++)
                // {
                //     var accountNo = accountTitle[i];
                //     var currentAccountTitle = accountTitleText[i];
                //     var currentAccountAmount = accountAmount[i];
                //     offsetAmount += accountAmount[i];
                //
                //     var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);
                //
                //     if (accountTitleDict.TryGetValue(accountNo, out var ids))
                //     {
                //         // Update the first matching record and remove it from the list
                //         var offsettingId = ids.First();
                //         ids.RemoveAt(0);
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == offsettingId);
                //
                //         offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                //         offsetting.Amount = currentAccountAmount;
                //         offsetting.CreatedBy = _userManager.GetUserName(this.User);
                //         offsetting.CreatedDate = DateTimeHelper.GetCurrentPhilippineTime();
                //         offsetting.Company = companyClaims;
                //
                //         if (ids.Count == 0)
                //         {
                //             accountTitleDict.Remove(accountNo);
                //         }
                //     }
                //     else
                //     {
                //         // Add new record
                //         var newOffsetting = new FilprideOffsettings
                //         {
                //             AccountNo = accountNo,
                //             AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                //             Source = existingModel.CollectionReceiptNo!,
                //             Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                //             Amount = currentAccountAmount,
                //             CreatedBy = _userManager.GetUserName(this.User),
                //             CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
                //         };
                //         _dbContext.FilprideOffsettings.Add(newOffsetting);
                //     }
                // }
                //
                // // Remove remaining records that were duplicates
                // foreach (var ids in accountTitleDict.Values)
                // {
                //     foreach (var id in ids)
                //     {
                //         var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                //         _dbContext.FilprideOffsettings.Remove(offsetting);
                //     }
                // }
                //
                // #endregion --Offsetting function

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", existingModel.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = "Collection receipt successfully updated.";
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(ServiceInvoiceIndex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.PostedBy = _userManager.GetUserName(this.User);
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);
                bool isMultipleSi = false;

                List<FilprideOffsettings>? offset;

                if (model.SalesInvoiceId != null)
                {
                    offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SINo!, model.Company, cancellationToken);
                }
                else
                {
                    offset = await _unitOfWork.FilprideCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SVNo!, model.Company, cancellationToken);
                }

                await _unitOfWork.FilprideCollectionReceipt.PostAsync(model, offset, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Collection Receipt has been Posted.";

                return RedirectToAction(isMultipleSi ? nameof(MultipleCollectionPrint) : nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                model.PostedBy = null;
                model.VoidedBy = _userManager.GetUserName(this.User);
                model.VoidedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Voided);
                var series = model.SINo ?? model.SVNo;

                var findOffsetting = await _dbContext.FilprideOffsettings.Where(offset => offset.Company == model.Company && offset.Source == model.CollectionReceiptNo && offset.Reference == series).ToListAsync(cancellationToken);

                await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideCashReceiptBook>(crb => crb.RefNo == model.CollectionReceiptNo, cancellationToken);
                await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CollectionReceiptNo, cancellationToken);

                if (findOffsetting.Any())
                {
                    await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideOffsettings>(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series, cancellationToken);
                }
                if (model.SINo != null)
                {
                    await _unitOfWork.FilprideCollectionReceipt.RemoveSIPayment(model.SalesInvoice!.SalesInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                }
                else if (model.SVNo != null)
                {
                    await _unitOfWork.FilprideCollectionReceipt.RemoveSVPayment(model.ServiceInvoice!.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                }
                else if (model.MultipleSI != null)
                {
                    await _unitOfWork.FilprideCollectionReceipt.RemoveMultipleSIPayment(model.MultipleSIId!, model.SIMultipleAmount!, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                }
                else
                {
                    TempData["info"] = "No series number found";
                    return RedirectToAction(nameof(Index));
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Collection Receipt has been Voided.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = _userManager.GetUserName(this.User);
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Canceled);
                model.CancellationRemarks = cancellationRemarks;

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Collection Receipt has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (cr == null)
            {
                return NotFound();
            }

            if (!cr.IsPrinted)
            {
                cr.IsPrinted = true;

                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrail = new(printedBy, $"Printed original copy of collection receipt# {cr.CollectionReceiptNo}", "Collection Receipt", cr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }
            else
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrail = new(printedBy, $"Printed re-printed copy of collection receipt# {cr.CollectionReceiptNo}", "Collection Receipt", cr.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> MultipleInvoiceBalance(int siNo)
        {
            var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                .GetAsync(si => si.SalesInvoiceId == siNo);

            if (salesInvoice == null)
            {
                return Json(null);
            }

            var vatType = salesInvoice.CustomerOrderSlip?.VatType ?? salesInvoice.Customer!.VatType;
            var hasEwt = salesInvoice.CustomerOrderSlip?.HasEWT ?? salesInvoice.Customer!.WithHoldingTax;
            var hasWvat = salesInvoice.CustomerOrderSlip?.HasWVAT ?? salesInvoice.Customer!.WithHoldingVat;

            var amount = salesInvoice.Amount;
            var amountPaid = salesInvoice.AmountPaid;
            var netDiscount = salesInvoice.Amount - salesInvoice.Discount;
            var netOfVatAmount = vatType == SD.VatType_Vatable
                ? _unitOfWork.FilprideCollectionReceipt.ComputeNetOfVat(netDiscount)
                : netDiscount;
            var vatAmount = vatType == SD.VatType_Vatable
                ? _unitOfWork.FilprideCollectionReceipt.ComputeVatAmount(netOfVatAmount)
                : 0m;
            var ewtAmount = hasEwt
                ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m)
                : 0m;
            var wvatAmount = hasWvat
                ? _unitOfWork.FilprideCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m)
                : 0m;
            var balance = amount - amountPaid;

            return Json(new
            {
                Amount = amount,
                AmountPaid = amountPaid,
                NetAmount = netOfVatAmount,
                VatAmount = vatAmount,
                EwtAmount = ewtAmount,
                WvatAmount = wvatAmount,
                Balance = balance
            });
        }

        public async Task<IActionResult> MultipleCollectionPrint(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.FilprideCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (cr == null)
            {
                return NotFound();
            }

            return View(cr);
        }

        public async Task<IActionResult> PrintedMultipleCR(int id, CancellationToken cancellationToken)
        {
            var findIdOfCr = await _unitOfWork.FilprideCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (findIdOfCr == null || findIdOfCr.IsPrinted)
            {
                return RedirectToAction(nameof(MultipleCollectionPrint), new { id });
            }

            findIdOfCr.IsPrinted = true;

            #region --Audit Trail Recording

            var printedBy = _userManager.GetUserName(User)!;
            FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of collection receipt# {findIdOfCr.CollectionReceiptNo}", "Collection Receipt", findIdOfCr.Company);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording
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
            var selectedList = await _unitOfWork.FilprideCollectionReceipt
                .GetAllAsync(cr => recordIds.Contains(cr.CollectionReceiptId));

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
                worksheet3.Cells["W1"].Value = "PostedBy";
                worksheet3.Cells["X1"].Value = "PostedDate";

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
                worksheet4.Cells["T1"].Value = "PostedBy";
                worksheet4.Cells["U1"].Value = "PostedDate";

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
                worksheet.Cells["AG1"].Value = "PostedBy";
                worksheet.Cells["AH1"].Value = "PostedDate";

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
                    worksheet.Cells[row, 5].Value = item.CheckDate?.ToString("yyyy-MM-dd") ?? null;
                    worksheet.Cells[row, 6].Value = item.CheckNo;
                    worksheet.Cells[row, 7].Value = item.BankAccount?.Bank;
                    worksheet.Cells[row, 8].Value = item.CheckBranch;
                    worksheet.Cells[row, 9].Value = item.CheckAmount;
                    worksheet.Cells[row, 10].Value = null;
                    worksheet.Cells[row, 11].Value = null;
                    worksheet.Cells[row, 12].Value = null;
                    worksheet.Cells[row, 13].Value = null;
                    worksheet.Cells[row, 14].Value = null;
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
                        worksheet.Cells[row, 24].Value = string.Join(", ", item.MultipleSI!.Select(si => si.ToString()));
                        worksheet.Cells[row, 25].Value = string.Join(", ", item.MultipleSIId.Select(siId => siId.ToString()));
                        worksheet.Cells[row, 26].Value = string.Join(" ", item.SIMultipleAmount!.Select(multipleSi => multipleSi.ToString(SD.Two_Decimal_Format)));
                        worksheet.Cells[row, 27].Value = string.Join(", ", item.MultipleTransactionDate!.Select(multipleTransactionDate => multipleTransactionDate.ToString("yyyy-MM-dd")));
                    }
                    worksheet.Cells[row, 28].Value = item.CustomerId;
                    worksheet.Cells[row, 29].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 30].Value = item.CollectionReceiptNo;
                    worksheet.Cells[row, 31].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 32].Value = item.CollectionReceiptId;
                    worksheet.Cells[row, 33].Value = item.PostedBy;
                    worksheet.Cells[row, 34].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Collection Receipt Export --

                #region Sales Invoice Export --

                int siRow = 2;
                var currentSi = "";

                foreach (var item in selectedList)
                {
                    if (item.SalesInvoice == null)
                    {
                        continue;
                    }
                    if (item.SalesInvoice.SalesInvoiceNo == currentSi)
                    {
                        continue;
                    }

                    currentSi = item.SalesInvoice.SalesInvoiceNo;
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
                    worksheet3.Cells[siRow, 23].Value = item.SalesInvoice.PostedBy;
                    worksheet3.Cells[siRow, 24].Value = item.SalesInvoice.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    siRow++;
                }

                #endregion Sales Invoice Export --

                #region -- Service Invoice Export --

                int svRow = 2;
                var currentSv = "";

                foreach (var item in selectedList)
                {
                    if (item.ServiceInvoice == null)
                    {
                        continue;
                    }
                    if (item.ServiceInvoice.ServiceInvoiceNo == currentSv)
                    {
                        continue;
                    }

                    currentSv = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet4.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet4.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                    worksheet4.Cells[svRow, 3].Value = item.ServiceInvoice.Total;
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
                    worksheet4.Cells[svRow, 20].Value = item.ServiceInvoice.PostedBy;
                    worksheet4.Cells[svRow, 21].Value = item.ServiceInvoice.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                #region -- Collection Receipt Export (Multiple SI)--

                var getSalesInvoice = _dbContext.FilprideSalesInvoices
                    .AsEnumerable()
                    .Where(s => selectedList
                        .Select(item => item.MultipleSI)
                        .Any(si => si?
                            .Contains(s.SalesInvoiceNo) == true))
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
                    worksheet3.Cells[siRow, 23].Value = item.PostedBy;
                    worksheet3.Cells[siRow, 24].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

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

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CollectionReceiptList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllCollectionReceiptIds()
        {
            var crIds = (await _unitOfWork.FilprideCollectionReceipt
                                     .GetAllAsync(cr => cr.Type == nameof(DocumentType.Documented)))
                                     .Select(cr => cr.CollectionReceiptId)
                                     .ToList();
            return Json(crIds);
        }
    }
}
