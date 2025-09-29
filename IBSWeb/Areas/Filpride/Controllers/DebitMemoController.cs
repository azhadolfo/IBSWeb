using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using IBS.Models.Filpride.AccountsReceivable;
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
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<DebitMemoController> _logger;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<DebitMemoController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.DebitMemo))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var debitMemos = await _unitOfWork.FilprideDebitMemo
                    .GetAllAsync(dm => dm.Company == companyClaims && dm.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", debitMemos);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDebitMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var debitMemos = await _unitOfWork.FilprideDebitMemo
                    .GetAllAsync(dm => dm.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    debitMemos = debitMemos
                        .Where(s =>
                            s.DebitMemoNo!.ToLower().Contains(searchValue) ||
                            s.SalesInvoice?.SalesInvoiceNo!.ToLower().Contains(searchValue) == true ||
                            s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true ||
                            s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                            s.DebitAmount.ToString().Contains(searchValue) ||
                            s.Remarks?.ToLower().Contains(searchValue) == true ||
                            s.Description.ToLower().Contains(searchValue) ||
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

                    debitMemos = debitMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = debitMemos.Count();

                var pagedData = debitMemos
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
                _logger.LogError(ex, "Failed to get debit memo. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new DebitMemoViewModel();
            await IncludeSelectLists(viewModel, cancellationToken);
            return View(viewModel);
        }

        public async Task IncludeSelectLists(DebitMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(si => si.Company == companyClaims && si.PostedBy != null, cancellationToken))
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToList();

            viewModel.ServiceInvoices = (await _unitOfWork.FilprideServiceInvoice
                .GetAllAsync(sv => sv.Company == companyClaims && sv.PostedBy != null, cancellationToken))
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToList();

            viewModel.MinDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DebitMemo, cancellationToken);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebitMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if(!ModelState.IsValid)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(viewModel);
            }

            var model = new FilprideDebitMemo
            {
                Source = viewModel.Source,
                TransactionDate = viewModel.TransactionDate,
                SalesInvoiceId = viewModel.SalesInvoiceId,
                Quantity = viewModel.Quantity,
                AdjustedPrice = viewModel.AdjustedPrice,
                ServiceInvoiceId = viewModel.ServiceInvoiceId,
                Period = viewModel.Period,
                Amount = viewModel.Amount,
                Remarks = viewModel.Remarks,
                Description = viewModel.Description,
            };

            var existingSalesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

            var existingSv = await _unitOfWork.FilprideServiceInvoice
                        .GetAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                #region -- checking for unposted DM or CM

                if (model.SalesInvoiceId != null)
                {
                    var existingSIDMs = (await _unitOfWork.FilprideDebitMemo
                                  .GetAllAsync(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                  .OrderBy(s => s.DebitMemoId)
                                  .ToList();

                    if (existingSIDMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSIDMs.First().DebitMemoNo}");
                        return View(viewModel);
                    }

                    var existingSICMs = (await _unitOfWork.FilprideCreditMemo
                                      .GetAllAsync(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                      .OrderBy(s => s.CreditMemoId)
                                      .ToList();

                    if (existingSICMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSICMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }
                }
                else
                {
                    var existingSVDMs = (await _unitOfWork.FilprideDebitMemo
                                  .GetAllAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                  .OrderBy(s => s.DebitMemoId)
                                  .ToList();
                    if (existingSVDMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVDMs.First().DebitMemoNo}");
                        return View(viewModel);
                    }

                    var existingSVCMs = (await _unitOfWork.FilprideCreditMemo
                                      .GetAllAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null, cancellationToken))
                                      .OrderBy(s => s.CreditMemoId)
                                      .ToList();
                    if (existingSVCMs.Count > 0)
                    {
                        await IncludeSelectLists(viewModel, cancellationToken);
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVCMs.First().CreditMemoNo}");
                        return View(viewModel);
                    }
                }

                #endregion -- checking for unposted DM or CM


                model.CreatedBy = GetUserFullName();
                model.Company = companyClaims;

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;
                    model.DebitMemoNo = await _unitOfWork.FilprideDebitMemo.GenerateCodeAsync(companyClaims, existingSalesInvoice!.Type, cancellationToken);
                    model.Type = existingSalesInvoice.Type;
                    model.DebitAmount = (decimal)(model.Quantity! * model.AdjustedPrice!);
                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;
                    model.DebitMemoNo = await _unitOfWork.FilprideDebitMemo.GenerateCodeAsync(companyClaims, existingSv!.Type, cancellationToken);
                    model.Type = existingSv.Type;
                    model.DebitAmount = model.Amount ?? 0;
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new debit memo# {model.DebitMemoNo}", "Debit Memo", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.FilprideDebitMemo.AddAsync(model, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = $"Debit memo #{model.DebitMemoNo} created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                _logger.LogError(ex, "Failed to create debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debitMemo = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);
            if (debitMemo == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            #region --Audit Trail Recording

            FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, $"Preview debit memo# {debitMemo.DebitMemoNo}", "Debit Memo", companyClaims!);
            await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

            #endregion --Audit Trail Recording

            return View(debitMemo);
        }

        public async Task<IActionResult> Post(int id, ViewModelDMCM viewModelDmcm, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DebitMemo, cancellationToken);
                if (model.TransactionDate < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException($"Cannot post this record because the period {model.TransactionDate:MMM yyyy} is already closed.");
                }

                model.PostedBy = GetUserFullName();
                model.PostedDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.Status = nameof(Status.Posted);

                var accountTitlesDto = await _unitOfWork.FilprideServiceInvoice.GetListOfAccountTitleDto(cancellationToken);
                var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
                var arNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account title '101020500' not found.");
                var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
                var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
                var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");


                if (model.SalesInvoiceId != null)
                {
                    var (salesAcctNo, salesAcctTitle) = _unitOfWork.FilprideSalesInvoice.GetSalesAccountTitle(model.SalesInvoice!.Product!.ProductCode);
                    var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");

                    #region --Retrieval of SI

                    var existingSi = await _unitOfWork
                        .FilprideSalesInvoice
                        .GetAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    #endregion --Retrieval of SI

                    #region --Sales Book Recording(SI)--

                    var sales = new FilprideSalesBook
                    {
                        TransactionDate = model.TransactionDate,
                        SerialNo = model.DebitMemoNo!,
                        SoldTo = model.SalesInvoice.CustomerOrderSlip!.CustomerName,
                        TinNo = model.SalesInvoice.CustomerOrderSlip!.CustomerTin,
                        Address = model.SalesInvoice.CustomerOrderSlip!.CustomerAddress,
                        Description = model.SalesInvoice.CustomerOrderSlip!.ProductName,
                        Amount = model.DebitAmount
                    };

                    switch (model.SalesInvoice.CustomerOrderSlip?.VatType)
                    {
                        case SD.VatType_Vatable:
                            sales.VatableSales = _unitOfWork.FilprideDebitMemo.ComputeNetOfVat(sales.Amount);
                            sales.VatAmount = _unitOfWork.FilprideDebitMemo.ComputeVatAmount(sales.VatableSales);
                            sales.NetSales = sales.VatableSales - sales.Discount;
                            break;
                        case SD.VatType_Exempt:
                            sales.VatExemptSales = sales.Amount;
                            sales.NetSales = sales.VatExemptSales - sales.Discount;
                            break;
                        default:
                            sales.ZeroRated = sales.Amount;
                            sales.NetSales = sales.ZeroRated - sales.Discount;
                            break;
                    }

                    sales.CreatedBy = model.CreatedBy;
                    sales.CreatedDate = model.CreatedDate;
                    sales.DueDate = existingSi!.DueDate;
                    sales.DocumentId = model.SalesInvoiceId;
                    sales.Company = model.Company;

                    await _dbContext.FilprideSalesBooks.AddAsync(sales, cancellationToken);

                    #endregion --Sales Book Recording(SI)--

                    #region --General Ledger Book Recording(SI)--

                    var netOfVatAmount = model.SalesInvoice.CustomerOrderSlip?.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(model.DebitAmount)
                        : model.DebitAmount;

                    var vatAmount = model.SalesInvoice.CustomerOrderSlip?.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideCreditMemo.ComputeVatAmount(netOfVatAmount)
                        : 0m;

                    var withHoldingTaxAmount = model.SalesInvoice.CustomerOrderSlip!.HasEWT
                        ? _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.01m)
                        : 0m;

                    var withHoldingVatAmount = model.SalesInvoice.CustomerOrderSlip!.HasWVAT
                        ? _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.05m)
                        : 0m;

                    var ledgers = new List<FilprideGeneralLedgerBook>
                    {
                        new()
                        {
                            Date = model.TransactionDate,
                            Reference = model.DebitMemoNo!,
                            Description = model.SalesInvoice.Product.ProductName,
                            AccountId = arTradeReceivableTitle.AccountId,
                            AccountNo = arTradeReceivableTitle.AccountNumber,
                            AccountTitle = arTradeReceivableTitle.AccountName,
                            Debit = model.DebitAmount - (withHoldingTaxAmount + withHoldingVatAmount),
                            Credit = 0,
                            Company = model.Company,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate,
                            CustomerId = model.SalesInvoice.CustomerOrderSlip.CustomerId,
                            CustomerName = model.SalesInvoice.CustomerOrderSlip.CustomerName,
                            ModuleType = nameof(ModuleType.DebitMemo)
                        }
                    };

                    if (withHoldingTaxAmount > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.SalesInvoice.Product.ProductName,
                                AccountId = arTradeCwt.AccountId,
                                AccountNo = arTradeCwt.AccountNumber,
                                AccountTitle = arTradeCwt.AccountName,
                                Debit = withHoldingTaxAmount,
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }
                    if (withHoldingVatAmount > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.SalesInvoice.Product.ProductName,
                                AccountId = arTradeCwv.AccountId,
                                AccountNo = arTradeCwv.AccountNumber,
                                AccountTitle = arTradeCwv.AccountName,
                                Debit = withHoldingVatAmount,
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }

                    ledgers.Add(
                        new FilprideGeneralLedgerBook
                        {
                            Date = model.TransactionDate,
                            Reference = model.DebitMemoNo!,
                            Description = model.SalesInvoice.Product.ProductName,
                            AccountId = salesTitle.AccountId,
                            AccountNo = salesTitle.AccountNumber,
                            AccountTitle = salesTitle.AccountName,
                            Debit = 0,
                            Company = model.Company,
                            CreatedBy = model.CreatedBy,
                            Credit = netOfVatAmount,
                            CreatedDate = model.CreatedDate,
                            ModuleType = nameof(ModuleType.DebitMemo)
                        }
                    );

                    if (vatAmount > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.SalesInvoice.Product.ProductName,
                                AccountId = vatOutputTitle.AccountId,
                                AccountNo = vatOutputTitle.AccountNumber,
                                AccountTitle = vatOutputTitle.AccountName,
                                Debit = 0,
                                Credit = vatAmount,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate,
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }

                    if (!_unitOfWork.FilprideDebitMemo.IsJournalEntriesBalanced(ledgers))
                    {
                        throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                    }

                    await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                    #endregion --General Ledger Book Recording(SI)--
                }

                if (model.ServiceInvoiceId != null)
                {
                    var existingSv = await _unitOfWork.FilprideServiceInvoice
                        .GetAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                    #region -- Computation --

                    viewModelDmcm.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                    var netDiscount = model.Amount ?? 0 - existingSv!.Discount;
                    var netOfVatAmount = model.ServiceInvoice!.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(netDiscount)
                        : netDiscount;
                    var vatAmount = model.ServiceInvoice!.VatType == SD.VatType_Vatable
                        ? _unitOfWork.FilprideServiceInvoice.ComputeVatAmount(netOfVatAmount)
                        : 0m;
                    var ewt = model.ServiceInvoice!.HasEwt
                        ? _unitOfWork.FilprideDebitMemo.ComputeEwtAmount(netOfVatAmount, existingSv!.ServicePercent / 100m)
                        : 0m;

                    var wvat = model.ServiceInvoice!.HasWvat
                        ? _unitOfWork.FilprideDebitMemo.ComputeEwtAmount(netOfVatAmount, 0.05m)
                        : 0m;

                    #endregion -- Computation --

                    #region --Sales Book Recording(SV)--

                    var sales = new FilprideSalesBook
                    {
                        TransactionDate = model.TransactionDate,
                        SerialNo = model.DebitMemoNo!,
                        SoldTo = model.ServiceInvoice.CustomerName,
                        TinNo = model.ServiceInvoice.CustomerTin,
                        Address = model.ServiceInvoice.CustomerAddress,
                        Description = model.ServiceInvoice.ServiceName,
                        Amount = model.DebitAmount
                    };

                    switch (model.ServiceInvoice.VatType)
                    {
                        case SD.VatType_Vatable:
                            sales.VatableSales = _unitOfWork.FilprideDebitMemo.ComputeNetOfVat(sales.Amount);
                            sales.VatAmount = _unitOfWork.FilprideDebitMemo.ComputeVatAmount(sales.VatableSales);
                            sales.NetSales = sales.VatableSales - sales.Discount;
                            break;
                        case SD.VatType_Exempt:
                            sales.VatExemptSales = sales.Amount;
                            sales.NetSales = sales.VatExemptSales - sales.Discount;
                            break;
                        default:
                            sales.ZeroRated = sales.Amount;
                            sales.NetSales = sales.ZeroRated - sales.Discount;
                            break;
                    }

                    sales.CreatedBy = model.CreatedBy;
                    sales.CreatedDate = model.CreatedDate;
                    sales.DueDate = existingSv!.DueDate;
                    sales.DocumentId = existingSv.ServiceInvoiceId;
                    sales.Company = model.Company;

                    await _dbContext.AddAsync(sales, cancellationToken);

                    #endregion --Sales Book Recording(SV)--

                    #region --General Ledger Book Recording(SV)--

                    var ledgers = new List<FilprideGeneralLedgerBook>
                    {
                        new()
                        {
                            Date = model.TransactionDate,
                            Reference = model.DebitMemoNo!,
                            Description = model.ServiceInvoice.ServiceName,
                            AccountId = arNonTradeTitle.AccountId,
                            AccountNo = arNonTradeTitle.AccountNumber,
                            AccountTitle = arNonTradeTitle.AccountName,
                            Debit = netDiscount - (ewt + wvat),
                            Credit = 0,
                            Company = model.Company,
                            CreatedBy = model.PostedBy,
                            CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                            CustomerId = model.ServiceInvoice.CustomerId,
                            CustomerName = model.ServiceInvoice.CustomerName,
                            ModuleType = nameof(ModuleType.DebitMemo)
                        }
                    };

                    if (ewt > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.ServiceInvoice.ServiceName,
                                AccountId = arTradeCwt.AccountId,
                                AccountNo = arTradeCwt.AccountNumber,
                                AccountTitle = arTradeCwt.AccountName,
                                Debit = ewt,
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.PostedBy,
                                CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }
                    if (wvat > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.ServiceInvoice.ServiceName,
                                AccountId = arTradeCwv.AccountId,
                                AccountNo = arTradeCwv.AccountNumber,
                                AccountTitle = arTradeCwv.AccountName,
                                Debit = wvat,
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.PostedBy,
                                CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }

                    if (netOfVatAmount > 0)
                    {
                        ledgers.Add(new FilprideGeneralLedgerBook
                        {
                            Date = model.TransactionDate,
                            Reference = model.DebitMemoNo!,
                            Description = model.ServiceInvoice.ServiceName,
                            AccountNo = model.ServiceInvoice.Service!.CurrentAndPreviousNo!,
                            AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle!,
                            Debit = 0,
                            Credit = netOfVatAmount,
                            Company = model.Company,
                            CreatedBy = model.PostedBy,
                            CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                            ModuleType = nameof(ModuleType.DebitMemo)
                        });
                    }

                    if (vatAmount > 0)
                    {
                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.DebitMemoNo!,
                                Description = model.ServiceInvoice.ServiceName,
                                AccountId = vatOutputTitle.AccountId,
                                AccountNo = vatOutputTitle.AccountNumber,
                                AccountTitle = vatOutputTitle.AccountName,
                                Debit = 0,
                                Credit = vatAmount,
                                Company = model.Company,
                                CreatedBy = model.PostedBy,
                                CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                ModuleType = nameof(ModuleType.DebitMemo)
                            }
                        );
                    }

                    if (!_unitOfWork.FilprideDebitMemo.IsJournalEntriesBalanced(ledgers))
                    {
                        throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                    }

                    await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                    #endregion --General Ledger Book Recording(SV)--
                }

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted debit memo# {model.DebitMemoNo}", "Debit Memo", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Debit Memo has been Posted.";
                return RedirectToAction(nameof(Print), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

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

                await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.DebitMemoNo, cancellationToken);
                await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DebitMemoNo, cancellationToken);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided debit memo# {model.DebitMemoNo}", "Debit Memo", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Debit Memo has been Voided.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to void debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (model == null)
            {
                return NotFound();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                model.CanceledBy = GetUserFullName();
                model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                model.CancellationRemarks = cancellationRemarks;
                model.Status = nameof(Status.Canceled);

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled debit memo# {model.DebitMemoNo}", "Debit Memo", model.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Debit Memo has been Cancelled.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideServiceInvoice.GetAsync(sv => sv.ServiceInvoiceId == svId, cancellationToken);
            if (model != null)
            {
                return Json(new
                {
                    model.Period,
                    model.Total
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

            try
            {
                var debitMemo =
                    await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

                if (debitMemo == null)
                {
                    return NotFound();
                }

                var minDate = await _unitOfWork.GetMinimumPeriodBasedOnThePostedPeriods(Module.DebitMemo, cancellationToken);
                if (debitMemo.TransactionDate < DateOnly.FromDateTime(minDate))
                {
                    throw new ArgumentException(
                        $"Cannot edit this record because the period {debitMemo.TransactionDate:MMM yyyy} is already closed.");
                }

                var viewModel = new DebitMemoViewModel
                {
                    DebitMemoId = debitMemo.DebitMemoId,
                    Source = debitMemo.Source,
                    TransactionDate = debitMemo.TransactionDate,
                    SalesInvoiceId = debitMemo.SalesInvoiceId,
                    Quantity = debitMemo.Quantity,
                    AdjustedPrice = debitMemo.AdjustedPrice,
                    ServiceInvoiceId = debitMemo.ServiceInvoiceId,
                    Period = debitMemo.Period,
                    Amount = debitMemo.Amount,
                    Remarks = debitMemo.Remarks,
                    Description = debitMemo.Description,
                    MinDate = minDate,
                };

                await IncludeSelectLists(viewModel, cancellationToken);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to fetch debit memo. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DebitMemoViewModel viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(viewModel);
            }

            var model = new FilprideDebitMemo
            {
                DebitMemoId = viewModel.DebitMemoId,
                Source = viewModel.Source,
                TransactionDate = viewModel.TransactionDate,
                SalesInvoiceId = viewModel.SalesInvoiceId,
                Quantity = viewModel.Quantity,
                AdjustedPrice = viewModel.AdjustedPrice,
                ServiceInvoiceId = viewModel.ServiceInvoiceId,
                Period = viewModel.Period,
                Amount = viewModel.Amount,
                Remarks = viewModel.Remarks,
                Description = viewModel.Description,
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingDm = await _unitOfWork.FilprideDebitMemo
                    .GetAsync(dm => dm.DebitMemoId == model.DebitMemoId, cancellationToken);

                if (existingDm == null)
                {
                    return NotFound();
                }

                switch (model.Source)
                {
                    case "Sales Invoice":
                        model.ServiceInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingDm.TransactionDate = model.TransactionDate;
                        existingDm.SalesInvoiceId = model.SalesInvoiceId;
                        existingDm.Quantity = model.Quantity;
                        existingDm.AdjustedPrice = model.AdjustedPrice;
                        existingDm.Description = model.Description;
                        existingDm.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingDm.DebitAmount = (decimal)(model.Quantity! * model.AdjustedPrice!);
                        break;
                    case "Service Invoice":
                        model.SalesInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingDm.TransactionDate = model.TransactionDate;
                        existingDm.ServiceInvoiceId = model.ServiceInvoiceId;
                        existingDm.Period = model.Period;
                        existingDm.Amount = model.Amount;
                        existingDm.Description = model.Description;
                        existingDm.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingDm.DebitAmount = model.Amount ?? 0;
                        break;
                }

                existingDm.EditedBy = GetUserFullName();
                existingDm.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                #region --Audit Trail Recording

                FilprideAuditTrail auditTrailBook = new(existingDm.EditedBy!, $"Edited debit memo# {existingDm.DebitMemoNo}", "Debit Memo", existingDm.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                await _unitOfWork.SaveAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                TempData["success"] = "Debit Memo edited successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await IncludeSelectLists(viewModel, cancellationToken);
                _logger.LogError(ex, "Failed to edit debit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var dm = await _unitOfWork.FilprideDebitMemo
                .GetAsync(x => x.DebitMemoId == id, cancellationToken);

            if (dm == null)
            {
                return NotFound();
            }

            if (!dm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of debit memo# {dm.DebitMemoNo}", "Debit Memo", dm.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                dm.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            else
            {
                #region --Audit Trail Recording

                FilprideAuditTrail auditTrail = new(User.Identity!.Name!, $"Printed re-printed copy of debit memo# {dm.DebitMemoNo}", "Debit Memo", dm.Company);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }

            return RedirectToAction(nameof(Print), new { id });
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
            var selectedList = (await _unitOfWork.FilprideDebitMemo
                    .GetAllAsync(dm => recordIds.Contains(dm.DebitMemoId)))
                    .OrderBy(dm => dm.DebitMemoNo)
                    .ToList();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                #region -- Sales Invoice Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("SalesInvoice");

                worksheet2.Cells["A1"].Value = "OtherRefNo";
                worksheet2.Cells["B1"].Value = "Quantity";
                worksheet2.Cells["C1"].Value = "UnitPrice";
                worksheet2.Cells["D1"].Value = "Amount";
                worksheet2.Cells["E1"].Value = "Remarks";
                worksheet2.Cells["F1"].Value = "Status";
                worksheet2.Cells["G1"].Value = "TransactionDate";
                worksheet2.Cells["H1"].Value = "Discount";
                worksheet2.Cells["I1"].Value = "AmountPaid";
                worksheet2.Cells["J1"].Value = "Balance";
                worksheet2.Cells["K1"].Value = "IsPaid";
                worksheet2.Cells["L1"].Value = "IsTaxAndVatPaid";
                worksheet2.Cells["M1"].Value = "DueDate";
                worksheet2.Cells["N1"].Value = "CreatedBy";
                worksheet2.Cells["O1"].Value = "CreatedDate";
                worksheet2.Cells["P1"].Value = "CancellationRemarks";
                worksheet2.Cells["Q1"].Value = "OriginalReceivingReportId";
                worksheet2.Cells["R1"].Value = "OriginalCustomerId";
                worksheet2.Cells["S1"].Value = "OriginalPOId";
                worksheet2.Cells["T1"].Value = "OriginalProductId";
                worksheet2.Cells["U1"].Value = "OriginalSeriesNumber";
                worksheet2.Cells["V1"].Value = "OriginalDocumentId";
                worksheet2.Cells["W1"].Value = "PostedBy";
                worksheet2.Cells["X1"].Value = "PostedDate";

                #endregion -- Sales Invoice Table Header --

                #region -- Service Invoice Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("ServiceInvoice");

                worksheet3.Cells["A1"].Value = "DueDate";
                worksheet3.Cells["B1"].Value = "Period";
                worksheet3.Cells["C1"].Value = "Amount";
                worksheet3.Cells["D1"].Value = "Total";
                worksheet3.Cells["E1"].Value = "Discount";
                worksheet3.Cells["F1"].Value = "CurrentAndPreviousMonth";
                worksheet3.Cells["G1"].Value = "UnearnedAmount";
                worksheet3.Cells["H1"].Value = "Status";
                worksheet3.Cells["I1"].Value = "AmountPaid";
                worksheet3.Cells["J1"].Value = "Balance";
                worksheet3.Cells["K1"].Value = "Instructions";
                worksheet3.Cells["L1"].Value = "IsPaid";
                worksheet3.Cells["M1"].Value = "CreatedBy";
                worksheet3.Cells["N1"].Value = "CreatedDate";
                worksheet3.Cells["O1"].Value = "CancellationRemarks";
                worksheet3.Cells["P1"].Value = "OriginalCustomerId";
                worksheet3.Cells["Q1"].Value = "OriginalSeriesNumber";
                worksheet3.Cells["R1"].Value = "OriginalServicesId";
                worksheet3.Cells["S1"].Value = "OriginalDocumentId";
                worksheet3.Cells["T1"].Value = "PostedBy";
                worksheet3.Cells["U1"].Value = "PostedDate";

                #endregion -- Service Invoice Table Header --

                #region -- Debit Memo Table Header --

                var worksheet = package.Workbook.Worksheets.Add("DebitMemo");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "DebitAmount";
                worksheet.Cells["C1"].Value = "Description";
                worksheet.Cells["D1"].Value = "AdjustedPrice";
                worksheet.Cells["E1"].Value = "Quantity";
                worksheet.Cells["F1"].Value = "Source";
                worksheet.Cells["G1"].Value = "Remarks";
                worksheet.Cells["H1"].Value = "Period";
                worksheet.Cells["I1"].Value = "Amount";
                worksheet.Cells["J1"].Value = "CurrentAndPreviousAmount";
                worksheet.Cells["K1"].Value = "UnearnedAmount";
                worksheet.Cells["L1"].Value = "ServicesId";
                worksheet.Cells["M1"].Value = "CreatedBy";
                worksheet.Cells["N1"].Value = "CreatedDate";
                worksheet.Cells["O1"].Value = "CancellationRemarks";
                worksheet.Cells["P1"].Value = "OriginalSalesInvoiceId";
                worksheet.Cells["Q1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["R1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["S1"].Value = "OriginalDocumentId";
                worksheet.Cells["T1"].Value = "PostedBy";
                worksheet.Cells["U1"].Value = "PostedDate";

                #endregion -- Debit Memo Table Header --

                #region -- Debit Memo Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.DebitAmount;
                    worksheet.Cells[row, 3].Value = item.Description;
                    worksheet.Cells[row, 4].Value = item.AdjustedPrice;
                    worksheet.Cells[row, 5].Value = item.Quantity;
                    worksheet.Cells[row, 6].Value = item.Source;
                    worksheet.Cells[row, 7].Value = item.Remarks;
                    worksheet.Cells[row, 8].Value = item.Period;
                    worksheet.Cells[row, 9].Value = item.Amount;
                    worksheet.Cells[row, 10].Value = item.CurrentAndPreviousAmount;
                    worksheet.Cells[row, 11].Value = item.UnearnedAmount;
                    worksheet.Cells[row, 12].Value = item.ServiceInvoice?.ServiceId;
                    worksheet.Cells[row, 13].Value = item.CreatedBy;
                    worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 16].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 17].Value = item.DebitMemoNo;
                    worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 19].Value = item.DebitMemoId;
                    worksheet.Cells[row, 20].Value = item.PostedBy;
                    worksheet.Cells[row, 21].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Debit Memo Export --

                #region -- Sales Invcoie Export --

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
                    worksheet2.Cells[siRow, 1].Value = item.SalesInvoice.OtherRefNo;
                    worksheet2.Cells[siRow, 2].Value = item.SalesInvoice.Quantity;
                    worksheet2.Cells[siRow, 3].Value = item.SalesInvoice.UnitPrice;
                    worksheet2.Cells[siRow, 4].Value = item.SalesInvoice.Amount;
                    worksheet2.Cells[siRow, 5].Value = item.SalesInvoice.Remarks;
                    worksheet2.Cells[siRow, 6].Value = item.SalesInvoice.Status;
                    worksheet2.Cells[siRow, 7].Value = item.SalesInvoice.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 8].Value = item.SalesInvoice.Discount;
                    worksheet2.Cells[siRow, 9].Value = item.SalesInvoice.AmountPaid;
                    worksheet2.Cells[siRow, 10].Value = item.SalesInvoice.Balance;
                    worksheet2.Cells[siRow, 11].Value = item.SalesInvoice.IsPaid;
                    worksheet2.Cells[siRow, 12].Value = item.SalesInvoice.IsTaxAndVatPaid;
                    worksheet2.Cells[siRow, 13].Value = item.SalesInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 14].Value = item.SalesInvoice.CreatedBy;
                    worksheet2.Cells[siRow, 15].Value = item.SalesInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[siRow, 16].Value = item.SalesInvoice.CancellationRemarks;
                    worksheet2.Cells[siRow, 17].Value = item.SalesInvoice.ReceivingReportId;
                    worksheet2.Cells[siRow, 18].Value = item.SalesInvoice.CustomerId;
                    worksheet2.Cells[siRow, 19].Value = item.SalesInvoice.PurchaseOrderId;
                    worksheet2.Cells[siRow, 20].Value = item.SalesInvoice.ProductId;
                    worksheet2.Cells[siRow, 21].Value = item.SalesInvoice.SalesInvoiceNo;
                    worksheet2.Cells[siRow, 22].Value = item.SalesInvoice.SalesInvoiceId;
                    worksheet2.Cells[siRow, 23].Value = item.SalesInvoice.PostedBy;
                    worksheet2.Cells[siRow, 24].Value = item.SalesInvoice.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    siRow++;
                }

                #endregion -- Sales Invcoie Export --

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
                    worksheet3.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 3].Value = item.ServiceInvoice.Total;
                    worksheet3.Cells[svRow, 4].Value = item.ServiceInvoice.Total;
                    worksheet3.Cells[svRow, 5].Value = item.ServiceInvoice.Discount;
                    worksheet3.Cells[svRow, 6].Value = item.ServiceInvoice.CurrentAndPreviousAmount;
                    worksheet3.Cells[svRow, 7].Value = item.ServiceInvoice.UnearnedAmount;
                    worksheet3.Cells[svRow, 8].Value = item.ServiceInvoice.Status;
                    worksheet3.Cells[svRow, 9].Value = item.ServiceInvoice.AmountPaid;
                    worksheet3.Cells[svRow, 10].Value = item.ServiceInvoice.Balance;
                    worksheet3.Cells[svRow, 11].Value = item.ServiceInvoice.Instructions;
                    worksheet3.Cells[svRow, 12].Value = item.ServiceInvoice.IsPaid;
                    worksheet3.Cells[svRow, 13].Value = item.ServiceInvoice.CreatedBy;
                    worksheet3.Cells[svRow, 14].Value = item.ServiceInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[svRow, 15].Value = item.ServiceInvoice.CancellationRemarks;
                    worksheet3.Cells[svRow, 16].Value = item.ServiceInvoice.CustomerId;
                    worksheet3.Cells[svRow, 17].Value = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet3.Cells[svRow, 18].Value = item.ServiceInvoice.ServiceId;
                    worksheet3.Cells[svRow, 19].Value = item.ServiceInvoice.ServiceInvoiceId;
                    worksheet3.Cells[svRow, 20].Value = item.ServiceInvoice.PostedBy;
                    worksheet3.Cells[svRow, 21].Value = item.ServiceInvoice.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                //Set password in Excel
                foreach (var excelWorkSheet in package.Workbook.Worksheets)
                {
                    excelWorkSheet.Protection.SetPassword("mis123");
                }

                package.Workbook.Protection.SetPassword("mis123");

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"DebitMemoList_IBS_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public async Task<IActionResult> GetAllDebitMemoIds()
        {
            var dmIds = (await _unitOfWork.FilprideDebitMemo
                 .GetAllAsync(dm => dm.Type == nameof(DocumentType.Documented)))
                 .Select(dm => dm.DebitMemoId)
                 .ToList();

            return Json(dmIds);
        }
    }
}
