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
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class CollectionReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<CollectionReceiptController> _logger;

        private readonly ICloudStorageService _cloudStorageService;

        public CollectionReceiptController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCollectionReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var stationCodeClaims = await GetStationCodeClaimAsync();

                var collectionReceipts = await _unitOfWork.MobilityCollectionReceipt
                    .GetAllAsync(sv => sv.StationCode == stationCodeClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    collectionReceipts = collectionReceipts
                        .Where(s =>
                            s.CollectionReceiptNo!.ToLower().Contains(searchValue) ||
                            s.Customer!.CustomerName.ToLower().Contains(searchValue) ||
                            s.SVNo?.ToLower().Contains(searchValue) == true ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
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
        public async Task<IActionResult> CreateForService(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceiptViewModel();
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            viewModel.Customers = await _unitOfWork.GetMobilityCustomerListAsync(stationCodeClaims, cancellationToken);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateForService(CollectionReceiptViewModel viewModel, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.Customers = await _unitOfWork.GetMobilityCustomerListAsync(stationCodeClaims, cancellationToken);

                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                if (bir2306 != null && bir2306.Length > 0)
                {
                    viewModel.F2306FileName = GenerateFileNameToSave(bir2306.FileName);
                    viewModel.F2306FilePath = await _cloudStorageService.UploadFileAsync(bir2306, viewModel.F2306FileName!);
                    viewModel.IsCertificateUpload = true;
                }

                if (bir2307 != null && bir2307.Length > 0)
                {
                    viewModel.F2307FileName = GenerateFileNameToSave(bir2307.FileName);
                    viewModel.F2307FilePath = await _cloudStorageService.UploadFileAsync(bir2307, viewModel.F2307FileName!);
                    viewModel.IsCertificateUpload = true;
                }

                var computeTotalInModelIfZero = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagerCheckAmount + viewModel.EWT + viewModel.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(viewModel);
                }
                var existingServiceInvoice = await _unitOfWork.MobilityServiceInvoice
                    .GetAsync(si => si.ServiceInvoiceId == viewModel.ServiceInvoiceId, cancellationToken);

                if (existingServiceInvoice == null)
                {
                    return NotFound();
                }

                var generateCRNo = await _unitOfWork.MobilityCollectionReceipt.GenerateCodeAsync(stationCodeClaims, existingServiceInvoice.Type, cancellationToken);

                decimal offsetAmount = 0;

                MobilityCollectionReceipt model = new()
                {
                    F2307FileName = viewModel.F2307FileName,
                    F2307FilePath = viewModel.F2307FilePath,
                    F2306FileName = viewModel.F2306FileName,
                    F2306FilePath = viewModel.F2306FilePath,
                    IsCertificateUpload = viewModel.IsCertificateUpload,
                    SVNo = existingServiceInvoice.ServiceInvoiceNo,
                    CollectionReceiptNo = generateCRNo,
                    CreatedBy = _userManager.GetUserName(this.User),
                    Total = computeTotalInModelIfZero,
                    StationCode = stationCodeClaims,
                    Type = existingServiceInvoice.Type,
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
                    EWT = viewModel.EWT,
                    WVAT = viewModel.WVAT,
                    ServiceInvoiceId = viewModel.ServiceInvoiceId,
                };
                await _dbContext.AddAsync(model, cancellationToken);

                #endregion --Saving default value

                #region --Offsetting function

                var offsettings = new List<MobilityOffsettings>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new MobilityOffsettings
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CollectionReceiptNo,
                            Reference = model.SVNo,
                            Amount = currentAccountAmount,
                            StationCode = model.StationCode,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

                #endregion --Offsetting function

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new collection receipt# {viewModel.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Collection receipt created successfully. Series Number: {model.CollectionReceiptNo}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                viewModel.Customers = await _unitOfWork.GetMobilityCustomerListAsync(stationCodeClaims, cancellationToken);

                viewModel.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);

                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to create collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, User.Identity!.Name);
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.MobilityCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            return View(cr);
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();
            var invoices = await _dbContext
                .MobilityServiceInvoices
                .Where(si => si.StationCode == stationCodeClaims && si.CustomerId == customerNo && !si.IsPaid && si.PostedBy != null)
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
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo, bool isServices, CancellationToken cancellationToken)
        {
            if (isServices)
            {
                var sv = await _unitOfWork.MobilityServiceInvoice
                    .GetAsync(s => s.ServiceInvoiceId == invoiceNo, cancellationToken);

                if (sv == null)
                {
                    return NotFound();
                }

                decimal netOfVatAmount = sv.Customer!.VatType == SD.VatType_Vatable ? _unitOfWork.MobilityServiceInvoice.ComputeNetOfVat(sv.Amount) - sv.Discount : sv.Amount - sv.Discount;
                decimal withHoldingTaxAmount = sv.Customer.WithHoldingTax ? _unitOfWork.MobilityCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = sv.Customer.WithHoldingVat ? _unitOfWork.MobilityCollectionReceipt.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

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
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            if (id == null)
            {
                return NotFound();
            }

            var existingModel = await _unitOfWork.MobilityCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            CollectionReceiptViewModel viewModel = new()
            {
                ServiceInvoices = await _dbContext.MobilityServiceInvoices
                    .Where(si => si.StationCode == stationCodeClaims && !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                    .OrderBy(si => si.ServiceInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceInvoiceId.ToString(),
                        Text = s.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken),
                Customers = await _unitOfWork.GetMobilityCustomerListAsync(stationCodeClaims, cancellationToken),
                ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                CollectionReceiptId = existingModel.CollectionReceiptId,
                SVNo = existingModel.ServiceInvoice!.ServiceInvoiceNo,
                Total = existingModel.Total,
                CustomerId = existingModel.CustomerId,
                TransactionDate = existingModel.TransactionDate,
                ReferenceNo = existingModel.ReferenceNo,
                Remarks = existingModel.Remarks,
                CashAmount = existingModel.CashAmount,
                CheckDate = existingModel.CheckDate,
                CheckNo = existingModel.CheckNo,
                CheckBank = existingModel.CheckBank,
                CheckBranch = existingModel.CheckBranch,
                CheckAmount = existingModel.CheckAmount,
                EWT = existingModel.EWT,
                WVAT = existingModel.WVAT,
                ServiceInvoiceId = existingModel.ServiceInvoiceId,
            };

            var findCustomers = await _dbContext.MobilityCustomers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.MobilityOffsettings
                .Where(offset => offset.StationCode == stationCodeClaims && offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CollectionReceiptViewModel viewModel, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var existingModel = await _unitOfWork.MobilityCollectionReceipt
                .GetAsync(cr => cr.CollectionReceiptId == viewModel.CollectionReceiptId, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return View(viewModel);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region --Saving default value

                var computeTotalInModelIfZero = viewModel.CashAmount + viewModel.CheckAmount + viewModel.ManagerCheckAmount + viewModel.EWT + viewModel.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(viewModel);
                }

                if (bir2306 != null && bir2306.Length > 0)
                {
                    viewModel.F2306FileName = GenerateFileNameToSave(bir2306.FileName);
                    viewModel.F2306FilePath = await _cloudStorageService.UploadFileAsync(bir2306, viewModel.F2306FileName!);
                    viewModel.IsCertificateUpload = true;
                }

                if (bir2307 != null && bir2307.Length > 0)
                {
                    viewModel.F2307FileName = GenerateFileNameToSave(bir2307.FileName);
                    viewModel.F2307FilePath = await _cloudStorageService.UploadFileAsync(bir2307, viewModel.F2307FileName!);
                    viewModel.IsCertificateUpload = true;
                }

                existingModel.Total = computeTotalInModelIfZero;
                existingModel.CustomerId = viewModel.CustomerId;
                existingModel.TransactionDate = viewModel.TransactionDate;
                existingModel.ReferenceNo = viewModel.ReferenceNo;
                existingModel.Remarks = viewModel.Remarks;
                existingModel.CashAmount = viewModel.CashAmount;
                existingModel.CheckDate = viewModel.CheckDate;
                existingModel.CheckNo = viewModel.CheckNo;
                existingModel.CheckBank = viewModel.CheckBank;
                existingModel.CheckBranch = viewModel.CheckBranch;
                existingModel.CheckAmount = viewModel.CheckAmount;
                existingModel.EWT = viewModel.EWT;
                existingModel.WVAT = viewModel.WVAT;
                if (bir2307 != null)
                {
                    existingModel.F2307FileName = viewModel.F2307FileName;
                    existingModel.F2307FilePath = viewModel.F2307FilePath;
                    existingModel.IsCertificateUpload = viewModel.IsCertificateUpload;
                }
                if (bir2306 != null)
                {
                    existingModel.F2306FileName = viewModel.F2306FileName;
                    existingModel.F2306FilePath = viewModel.F2306FilePath;
                    existingModel.IsCertificateUpload = viewModel.IsCertificateUpload;
                }
                existingModel.ServiceInvoiceId = viewModel.ServiceInvoiceId;
                existingModel.EditedBy = _userManager.GetUserName(User);
                existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var findOffsettings = await _dbContext.MobilityOffsettings
                .Where(offset => offset.StationCode == stationCodeClaims && offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

                var accountTitleSet = new HashSet<string>(accountTitle);

                // Remove records not in accountTitle
                foreach (var offsetting in findOffsettings)
                {
                    if (!accountTitleSet.Contains(offsetting.AccountNo))
                    {
                        _dbContext.MobilityOffsettings.Remove(offsetting);
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
                        offsetting.StationCode = stationCodeClaims;

                        if (ids.Count == 0)
                        {
                            accountTitleDict.Remove(accountNo);
                        }
                    }
                    else
                    {
                        // Add new record
                        var newOffsetting = new MobilityOffsettings
                        {
                            AccountNo = accountNo,
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = existingModel.CollectionReceiptNo!,
                            Reference = existingModel.SVNo,
                            Amount = currentAccountAmount,
                            CreatedBy = _userManager.GetUserName(this.User),
                            CreatedDate = DateTimeHelper.GetCurrentPhilippineTime(),
                            StationCode = existingModel.StationCode
                        };
                        _dbContext.MobilityOffsettings.Add(newOffsetting);
                    }
                }

                // Remove remaining records that were duplicates
                foreach (var ids in accountTitleDict.Values)
                {
                    foreach (var id in ids)
                    {
                        var offsetting = findOffsettings.First(o => o.OffSettingId == id);
                        _dbContext.MobilityOffsettings.Remove(offsetting);
                    }
                }

                #endregion --Offsetting function

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingModel.EditedBy!, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = "Collection receipt successfully updated.";
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to edit collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            var model = await _unitOfWork.MobilityCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

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

                        List<MobilityOffsettings>? offset = new List<MobilityOffsettings>();
                        var offsetAmount = 0m;

                        offset = await _unitOfWork.MobilityCollectionReceipt.GetOffsettings(model.CollectionReceiptNo!, model.SVNo!, stationCodeClaims, cancellationToken);
                        offsetAmount = offset.Sum(o => o.Amount);

                        ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
                        //await _unitOfWork.FilprideCollectionReceipt.PostAsync(model, offset, cancellationToken);

                        await _unitOfWork.MobilityCollectionReceipt.UpdateSV(model.ServiceInvoice!.ServiceInvoiceId, model.Total, offsetAmount, cancellationToken);

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
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
                    _logger.LogError(ex, "Failed to post collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, User.Identity!.Name);
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
            var model = await _unitOfWork.MobilityCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);
            var stationCodeClaims = await GetStationCodeClaimAsync();
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
                        var series = model.SVNo;

                        var findOffsetting = await _dbContext.MobilityOffsettings.Where(offset => offset.StationCode == stationCodeClaims && offset.Source == model.CollectionReceiptNo && offset.Reference == series).ToListAsync(cancellationToken);

                        ///TODO: waiting for ma'am LSA decision if the mobility station is separated GL
                        //await _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideCashReceiptBook>(crb => crb.RefNo == model.CollectionReceiptNo, cancellationToken);
                        // _unitOfWork.FilprideCollectionReceipt.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CollectionReceiptNo, cancellationToken);

                        if (findOffsetting.Any())
                        {
                            await _unitOfWork.MobilityCollectionReceipt.RemoveRecords<MobilityOffsettings>(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series, cancellationToken);
                        }
                        if (model.SVNo != null)
                        {
                            await _unitOfWork.MobilityCollectionReceipt.RemoveSVPayment(model.ServiceInvoice!.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else
                        {
                            TempData["error"] = "No series number found";
                            return RedirectToAction(nameof(Index));
                        }

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
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
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.MobilityCollectionReceipt.GetAsync(cr => cr.CollectionReceiptId == id, cancellationToken);

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

                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
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
                _logger.LogError(ex, "Failed to cancel collection receipt. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cr = await _unitOfWork.MobilityCollectionReceipt
                .GetAsync(x => x.CollectionReceiptId == id, cancellationToken);

            if (cr == null)
            {
                return NotFound();
            }

            if (!cr.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of collection receipt# {cr.CollectionReceiptNo}", "Collection Receipt", nameof(Mobility));
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cr.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}
