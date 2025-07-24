using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
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
    public class CreditMemoController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<CreditMemoController> _logger;

        public CreditMemoController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<CreditMemoController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
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
            if (view == nameof(DynamicView.CreditMemo))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var creditMemos = await _unitOfWork.FilprideCreditMemo
                    .GetAllAsync(cm => cm.Company == companyClaims, cancellationToken);

                return View("ExportIndex", creditMemos);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetCreditMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var creditMemos = await _unitOfWork.FilprideCreditMemo
                    .GetAllAsync(cm => cm.Company == companyClaims && cm.Type == nameof(DocumentType.Documented), cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    creditMemos = creditMemos
                    .Where(s =>
                        s.CreditMemoNo!.ToLower().Contains(searchValue) ||
                        (s.SalesInvoice?.SalesInvoiceNo!.ToLower().Contains(searchValue) == true) ||
                        (s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true) ||
                        s.TransactionDate.ToString(SD.Date_Format).ToLower().Contains(searchValue) ||
                        s.CreditAmount.ToString().Contains(searchValue) ||
                        s.Remarks?.ToLower().Contains(searchValue) == true ||
                        s.Description.ToLower().Contains(searchValue) ||
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

                    creditMemos = creditMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = creditMemos.Count();

                var pagedData = creditMemos
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
                _logger.LogError(ex, "Failed to get credit memos. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCreditMemo();
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

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideCreditMemo model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            model.SalesInvoices = (await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(si => si.Company == companyClaims && si.PostedBy != null, cancellationToken))
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToList();
            model.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.Company == companyClaims && sv.PostedBy != null)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            var existingSalesInvoice = await _dbContext
                        .FilprideSalesInvoices
                        .Include(c => c.Customer)
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

            var existingSv = await _dbContext.FilprideServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- check for unposted DM or CM

                    if (model.SalesInvoiceId != null)
                    {
                        var existingSIDMs = _dbContext.FilprideDebitMemos
                                      .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                      .OrderBy(s => s.SalesInvoiceId)
                                      .ToList();
                        if (existingSIDMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSIDMs.First().DebitMemoNo}");
                            return View(model);
                        }

                        var existingSICMs = _dbContext.FilprideCreditMemos
                                          .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                          .OrderBy(s => s.SalesInvoiceId)
                                          .ToList();
                        if (existingSICMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSICMs.First().CreditMemoNo}");
                            return View(model);
                        }
                    }
                    else
                    {
                        var existingSOADMs = _dbContext.FilprideDebitMemos
                                      .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                      .OrderBy(s => s.ServiceInvoiceId)
                                      .ToList();
                        if (existingSOADMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DebitMemoNo}");
                            return View(model);
                        }

                        var existingSOACMs = _dbContext.FilprideCreditMemos
                                          .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                          .OrderBy(s => s.SalesInvoiceId)
                                          .ToList();
                        if (existingSOACMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOACMs.First().CreditMemoNo}");
                            return View(model);
                        }
                    }

                    #endregion -- check for unposted DM or CM

                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Company = companyClaims;

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;
                        model.CreditMemoNo = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(companyClaims, existingSalesInvoice!.Type, cancellationToken);
                        model.Type = existingSalesInvoice.Type;
                        model.CreditAmount = (decimal)(model.Quantity! * -model.AdjustedPrice!);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        model.CreditMemoNo = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(companyClaims, existingSv!.Type, cancellationToken);
                        model.Type = existingSv.Type;
                        model.CreditAmount = -model.Amount ?? 0;
                    }

                    await _dbContext.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new credit memo# {model.CreditMemoNo}", "Credit Memo", model.Company);
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = $"Credit memo #{model.CreditMemoNo} created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var creditMemo = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            creditMemo.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.Company == companyClaims && sv.PostedBy != null)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            return View(creditMemo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilprideCreditMemo model, CancellationToken cancellationToken)
        {
            var existingSalesInvoice = await _dbContext
                        .FilprideSalesInvoices
                        .Include(c => c.Customer)
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId);
            var existingSv = await _dbContext.FilprideServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingCM = await _dbContext
                                    .FilprideCreditMemos
                                    .FirstOrDefaultAsync(cm => cm.CreditMemoId == model.CreditMemoId, cancellationToken);

                    if (existingCM == null)
                    {
                        return NotFound();
                    }

                    model.EditedBy = _userManager.GetUserName(this.User);
                    model.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingCM.TransactionDate = model.TransactionDate;
                        existingCM.SalesInvoiceId = model.SalesInvoiceId;
                        existingCM.Quantity = model.Quantity;
                        existingCM.AdjustedPrice = model.AdjustedPrice;
                        existingCM.Description = model.Description;
                        existingCM.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingCM.CreditAmount = (decimal)(model.Quantity! * -model.AdjustedPrice!);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingCM.TransactionDate = model.TransactionDate;
                        existingCM.ServiceInvoiceId = model.ServiceInvoiceId;
                        existingCM.Period = model.Period;
                        existingCM.Amount = model.Amount;
                        existingCM.Description = model.Description;
                        existingCM.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingCM.CreditAmount = -model.Amount ?? 0;
                    }

                    existingCM.EditedBy = _userManager.GetUserName(User);
                    existingCM.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();


                    #region --Audit Trail Recording

                    FilprideAuditTrail auditTrailBook = new(existingCM.EditedBy!, $"Edited credit memo# {existingCM.CreditMemoNo}", "Credit Memo", existingCM.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Credit Memo edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideCreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            return View(creditMemo);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDMCM)
        {
            var model = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

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

                            #region --Retrieval of SI and SOA--

                            var existingSI = await _dbContext.FilprideSalesInvoices
                                                        .Include(s => s.Customer)
                                                        .Include(s => s.Product)
                                                        .Include(s => s.CustomerOrderSlip)
                                                        .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                            #endregion --Retrieval of SI and SOA--

                            #region --Sales Book Recording(SI)--

                            var sales = new FilprideSalesBook();

                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.SalesInvoice.CustomerOrderSlip!.CustomerName;
                            sales.TinNo = model.SalesInvoice.CustomerOrderSlip.CustomerTin;
                            sales.Address = model.SalesInvoice.CustomerOrderSlip.CustomerAddress;
                            sales.Description = model.SalesInvoice.CustomerOrderSlip.ProductName;
                            sales.Amount = model.CreditAmount;
                            //sales.Discount = model.Discount;

                            switch (model.SalesInvoice.CustomerOrderSlip!.VatType)
                            {
                                case SD.VatType_Vatable:
                                    sales.VatableSales = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                                    sales.VatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(sales.VatableSales))) * -1;
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
                            sales.DueDate = existingSI!.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                            sales.Company = model.Company;

                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

                            decimal withHoldingTaxAmount = 0;
                            decimal withHoldingVatAmount = 0;
                            decimal netOfVatAmount = 0;
                            decimal vatAmount = 0;

                            if (model.SalesInvoice.CustomerOrderSlip.VatType == SD.VatType_Vatable)
                            {
                                netOfVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                                vatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(netOfVatAmount))) * -1;
                            }
                            else
                            {
                                netOfVatAmount = model.CreditAmount;
                            }

                            if (model.SalesInvoice.CustomerOrderSlip.HasEWT)
                            {
                                withHoldingTaxAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                            }

                            if (model.SalesInvoice.CustomerOrderSlip.HasWVAT)
                            {
                                withHoldingVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                            }

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                                    AccountId = arTradeReceivableTitle.AccountId,
                                    AccountNo = arTradeReceivableTitle.AccountNumber,
                                    AccountTitle = arTradeReceivableTitle.AccountName,
                                    Debit = 0,
                                    Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                    Company = model.Company,
                                    CreatedBy = model.PostedBy,
                                    CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                    CustomerId = model.SalesInvoice.CustomerOrderSlip.CustomerId,
                                    CustomerName = model.SalesInvoice.CustomerOrderSlip.CustomerName,
                                }
                            );

                            if (withHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                                        AccountId = arTradeCwt.AccountId,
                                        AccountNo = arTradeCwt.AccountNumber,
                                        AccountTitle = arTradeCwt.AccountName,
                                        Debit = 0,
                                        Credit = Math.Abs(withHoldingTaxAmount),
                                        Company = model.Company,
                                        CreatedBy = model.PostedBy,
                                        CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                    }
                                );
                            }
                            if (withHoldingVatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                                        AccountId = arTradeCwv.AccountId,
                                        AccountNo = arTradeCwv.AccountNumber,
                                        AccountTitle = arTradeCwv.AccountName,
                                        Debit = 0,
                                        Credit = Math.Abs(withHoldingVatAmount),
                                        Company = model.Company,
                                        CreatedBy = model.PostedBy,
                                        CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                    }
                                );
                            }

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                                    AccountId = salesTitle.AccountId,
                                    AccountNo = salesTitle.AccountNumber,
                                    AccountTitle = salesTitle.AccountName,
                                    Debit = Math.Abs(netOfVatAmount),
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.PostedBy,
                                    CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                }
                            );

                            if (vatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.SalesInvoice.CustomerOrderSlip.ProductName,
                                        AccountId = vatOutputTitle.AccountId,
                                        AccountNo = vatOutputTitle.AccountNumber,
                                        AccountTitle = vatOutputTitle.AccountName,
                                        Debit = Math.Abs(vatAmount),
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.PostedBy,
                                        CreatedDate = model.PostedDate ?? DateTimeHelper.GetCurrentPhilippineTime(),
                                    }
                                );
                            }

                            if (!_unitOfWork.FilprideCreditMemo.IsJournalEntriesBalanced(ledgers))
                            {
                                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                            }

                            await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording(SI)--
                        }

                        if (model.ServiceInvoiceId != null)
                        {
                            var existingSv = await _dbContext.FilprideServiceInvoices
                                                    .Include(sv => sv.Customer)
                                                    .Include(sv => sv.Service)
                                                    .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv!.VatType == "Vatable")
                            {
                                viewModelDMCM.Total = -model.Amount ?? 0;
                                viewModelDMCM.NetAmount = (model.Amount ?? 0 - existingSv.Discount) / 1.12m;
                                viewModelDMCM.VatAmount = (model.Amount ?? 0 - existingSv.Discount) - viewModelDMCM.NetAmount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.ServicePercent / 100m);
                                if (existingSv.HasWvat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.ServicePercent / 100m);
                                if (existingSv.HasWvat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }

                            if (existingSv.VatType == "Vatable")
                            {
                                var total = Math.Round(model.Amount ?? 0 / 1.12m, 4);

                                var roundedNetAmount = Math.Round(viewModelDMCM.NetAmount, 4);

                                if (roundedNetAmount > total)
                                {
                                    var shortAmount = viewModelDMCM.NetAmount - total;

                                    viewModelDMCM.Amount += shortAmount;
                                }
                            }

                            #endregion --SV Computation--

                            #region --Sales Book Recording(SV)--

                            var sales = new FilprideSalesBook();

                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.ServiceInvoice!.CustomerName;
                            sales.TinNo = model.ServiceInvoice.CustomerTin;
                            sales.Address = model.ServiceInvoice.CustomerAddress;
                            sales.Description = model.ServiceInvoice!.ServiceName;
                            sales.Amount = model.CreditAmount;

                            switch (model.ServiceInvoice.VatType)
                            {
                                case SD.VatType_Vatable:
                                    sales.VatableSales = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                                    sales.VatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(sales.VatableSales))) * -1;
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

                            //sales.Discount = model.Discount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSv.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                            sales.Company = model.Company;

                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SV)--

                            decimal withHoldingTaxAmount = 0;
                            decimal withHoldingVatAmount = 0;
                            decimal netOfVatAmount = 0;
                            decimal vatAmount = 0;

                            if (model.ServiceInvoice.VatType == SD.VatType_Vatable)
                            {
                                netOfVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                                vatAmount = (_unitOfWork.FilprideCreditMemo.ComputeVatAmount(Math.Abs(netOfVatAmount))) * -1;
                            }
                            else
                            {
                                netOfVatAmount = model.CreditAmount;
                            }

                            if (model.ServiceInvoice.HasEwt)
                            {
                                withHoldingTaxAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                            }

                            if (model.ServiceInvoice.HasWvat)
                            {
                                withHoldingVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                            }

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.ServiceInvoice.ServiceName,
                                        AccountId = arNonTradeTitle.AccountId,
                                        AccountNo = arNonTradeTitle.AccountNumber,
                                        AccountTitle = arNonTradeTitle.AccountName,
                                        Debit = 0,
                                        Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate,
                                        CustomerId = model.ServiceInvoice.CustomerId,
                                        CustomerName = model.ServiceInvoice.CustomerName
                                    }
                                );
                            if (withHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.ServiceInvoice.ServiceName,
                                        AccountId = arTradeCwt.AccountId,
                                        AccountNo = arTradeCwt.AccountNumber,
                                        AccountTitle = arTradeCwt.AccountName,
                                        Debit = 0,
                                        Credit = Math.Abs(withHoldingTaxAmount),
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (withHoldingVatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.ServiceInvoice.ServiceName,
                                        AccountId = arTradeCwv.AccountId,
                                        AccountNo = arTradeCwv.AccountNumber,
                                        AccountTitle = arTradeCwv.AccountName,
                                        Debit = 0,
                                        Credit = Math.Abs(withHoldingVatAmount),
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            ledgers.Add(new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.CreditMemoNo!,
                                Description = model.ServiceInvoice.ServiceName,
                                ///TODO to inquire if needs to store
                                AccountNo = model.ServiceInvoice.Service!.CurrentAndPreviousNo!,
                                AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle!,
                                Debit = viewModelDMCM.NetAmount,
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });

                            if (vatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo!,
                                        Description = model.ServiceInvoice.ServiceName,
                                        AccountId = vatOutputTitle.AccountId,
                                        AccountNo = vatOutputTitle.AccountNumber,
                                        AccountTitle = vatOutputTitle.AccountName,
                                        Debit = Math.Abs(vatAmount),
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (!_unitOfWork.FilprideCreditMemo.IsJournalEntriesBalanced(ledgers))
                            {
                                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                            }

                            await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording(SV)--
                        }

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.PostedBy!, $"Posted credit memo# {model.CreditMemoNo}", "Credit Memo", model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Posted.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(Print), new { id });
            }

            return NotFound();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCreditMemos.FindAsync(id, cancellationToken);

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

                        await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.CreditMemoNo, cancellationToken);
                        await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CreditMemoNo, cancellationToken);

                        #region --Audit Trail Recording

                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided credit memo# {model.CreditMemoNo}", "Credit Memo", model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Voided.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to void credit memo. Voided by: {UserName}", _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCreditMemos.FindAsync(id, cancellationToken);

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

                        FilprideAuditTrail auditTrailBook = new(model.CanceledBy!, $"Canceled credit memo# {model.CreditMemoNo}", "Credit Memo", model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel credit memo. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }


        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideServiceInvoices.FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == svId, cancellationToken);
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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cm = await _unitOfWork.FilprideCreditMemo
                .GetAsync(x => x.CreditMemoId == id, cancellationToken);

            if (cm == null)
            {
                return NotFound();
            }

            if (!cm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User)!;
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of credit memo# {cm.CreditMemoNo}", "Credit Memo", cm.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
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
            var selectedList = await _dbContext.FilprideCreditMemos
                .Where(cm => recordIds.Contains(cm.CreditMemoId))
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .OrderBy(cm => cm.CreditMemoNo)
                .ToListAsync();

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

                #region -- Credit Memo Table Header --

                var worksheet = package.Workbook.Worksheets.Add("CreditMemo");

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

                #endregion -- Credit Memo Table Header --

                #region -- Credit Memo Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.CreditAmount;
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
                    worksheet.Cells[row, 17].Value = item.CreditMemoNo;
                    worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 19].Value = item.CreditMemoId;
                    worksheet.Cells[row, 20].Value = item.PostedBy;
                    worksheet.Cells[row, 21].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                #endregion -- Credit Memo Export --

                #region -- Sales Invoice Export --

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

                #endregion -- Sales Invoice Export --

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
                    $"CreditMemoList_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCreditMemoIds()
        {
            var cmIds = _dbContext.FilprideCreditMemos
                                     .Where(cm => cm.Type == nameof(DocumentType.Documented))
                                     .Select(cm => cm.CreditMemoId) // Assuming Id is the primary key
                                     .ToList();

            return Json(cmIds);
        }
    }
}
