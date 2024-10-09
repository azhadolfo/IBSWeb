using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class CreditMemoController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        public CreditMemoController(IUnitOfWork unitOfWork, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
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
        public async Task<IActionResult> GetCreditMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var creditMemos = await _unitOfWork.FilprideCreditMemo
                    .GetAllAsync(cm => cm.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    creditMemos = creditMemos
                    .Where(s =>
                        s.CreditMemoNo.ToLower().Contains(searchValue) ||
                        (s.SalesInvoice?.SalesInvoiceNo.ToLower().Contains(searchValue) == true) ||
                        (s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true) ||
                        s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                        s.CreditAmount.ToString().Contains(searchValue) ||
                        s.Remarks?.ToLower().Contains(searchValue) == true ||
                        s.Description.ToLower().Contains(searchValue) ||
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCreditMemo();
            var companyClaims = await GetCompanyClaimAsync();

            viewModel.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            viewModel.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.Company == companyClaims && sv.PostedBy != null)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilprideCreditMemo model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
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
                        .FirstOrDefaultAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId);

            var existingSv = await _dbContext.FilprideServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice > existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input more than the existing SI unit price!");
                }
                if (model.Quantity > existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount > existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input more than the existing SV amount!");
                }
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
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

                    model.CreditMemoNo = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(companyClaims, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Company = companyClaims;

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;

                        model.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region --Retrieval of Services

                        model.ServicesId = existingSv.ServiceId;

                        var services = await _dbContext
                        .FilprideServices
                        .FirstOrDefaultAsync(s => s.ServiceId == model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        model.CreditAmount = -model.Amount ?? 0;
                    }

                    await _dbContext.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Credit memo created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
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
            if (id == null || _dbContext.FilprideCreditMemos == null)
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
                    Value = sv.ServiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            return View(creditMemo);
        }

        [HttpPost]
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

            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice > existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input more than the existing SI unit price!");
                }
                if (model.Quantity > existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount > existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input more than the existing SV amount!");
                }
            }

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingCM = await _dbContext
                                    .FilprideCreditMemos
                                    .FirstOrDefaultAsync(cm => cm.CreditMemoId == model.CreditMemoId);

                    model.EditedBy = _userManager.GetUserName(this.User);
                    model.EditedDate = DateTime.Now;

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

                        existingCM.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region --Retrieval of Services

                        existingCM.ServicesId = existingSv.ServiceId;

                        var services = await _dbContext
                        .FilprideServices
                        .FirstOrDefaultAsync(s => s.ServiceId == existingCM.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

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

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.EditedBy, $"Edited credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Credit Memo edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
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
                        model.PostedDate = DateTime.Now;
                        model.Status = nameof(Status.Posted);

                        if (model.SalesInvoiceId != null)
                        {
                            #region --Retrieval of SI and SOA--

                            var existingSI = await _dbContext.FilprideSalesInvoices
                                                        .Include(s => s.Customer)
                                                        .Include(s => s.Product)
                                                        .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                            #endregion --Retrieval of SI and SOA--

                            #region --Sales Book Recording(SI)--

                            var sales = new FilprideSalesBook();

                            if (model.SalesInvoice.Customer.VatType == "Vatable")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.CreditAmount;
                                sales.VatableSales = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                                sales.VatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(sales.VatableSales))) * -1;
                                //sales.Discount = model.Discount;
                                sales.NetSales = sales.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                                sales.Company = model.Company;
                            }
                            else if (model.SalesInvoice.Customer.VatType == "Exempt")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.CreditAmount;
                                sales.VatExemptSales = model.CreditAmount;
                                //sales.Discount = model.Discount;
                                sales.NetSales = sales.Amount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                                sales.Company = model.Company;
                            }
                            else
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.CreditAmount;
                                sales.ZeroRated = model.CreditAmount;
                                //sales.Discount = model.Discount;
                                sales.NetSales = sales.Amount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                                sales.Company = model.Company;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

                            decimal withHoldingTaxAmount = 0;
                            decimal withHoldingVatAmount = 0;
                            decimal netOfVatAmount = 0;
                            decimal vatAmount = 0;

                            if (model.SalesInvoice.Customer.VatType == SD.VatType_Vatable)
                            {
                                netOfVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                                vatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(netOfVatAmount))) * -1;
                            }
                            else
                            {
                                netOfVatAmount = model.CreditAmount;
                            }

                            if (model.SalesInvoice.Customer.WithHoldingTax)
                            {
                                withHoldingTaxAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                            }

                            if (model.SalesInvoice.Customer.WithHoldingVat)
                            {
                                withHoldingVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                            }

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = "1010201",
                                    AccountTitle = "AR-Trade Receivable",
                                    Debit = 0,
                                    Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                            if (withHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
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
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
                                        Debit = 0,
                                        Credit = Math.Abs(withHoldingVatAmount),
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.SalesInvoice.Product.ProductName == "BIODIESEL")
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "4010101",
                                        AccountTitle = "Sales - Biodiesel",
                                        Debit = netOfVatAmount,
                                        CreatedBy = model.CreatedBy,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            else if (model.SalesInvoice.Product.ProductName == "ECONOGAS")
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "4010102",
                                        AccountTitle = "Sales - Econogas",
                                        Debit = netOfVatAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            else if (model.SalesInvoice.Product.ProductName == "ENVIROGAS")
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "4010103",
                                        AccountTitle = "Sales - Envirogas",
                                        Debit = netOfVatAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (vatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "2010301",
                                        AccountTitle = "Vat Output",
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

                            #endregion --General Ledger Book Recording(SI)--
                        }

                        if (model.ServiceInvoiceId != null)
                        {
                            var existingSv = await _dbContext.FilprideServiceInvoices
                                                    .Include(sv => sv.Customer)
                                                    .FirstOrDefaultAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                            #region --Retrieval of Services

                            var services = await _dbContext.FilprideServices.FindAsync(model.ServicesId, cancellationToken);

                            #endregion --Retrieval of Services

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                viewModelDMCM.Total = -model.Amount ?? 0;
                                viewModelDMCM.NetAmount = (model.Amount ?? 0 - existingSv.Discount) / 1.12m;
                                viewModelDMCM.VatAmount = (model.Amount ?? 0 - existingSv.Discount) - viewModelDMCM.NetAmount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                var total = Math.Round(model.Amount ?? 0 / 1.12m, 2);

                                var roundedNetAmount = Math.Round(viewModelDMCM.NetAmount, 2);

                                if (roundedNetAmount > total)
                                {
                                    var shortAmount = viewModelDMCM.NetAmount - total;

                                    viewModelDMCM.Amount += shortAmount;
                                }
                            }

                            #endregion --SV Computation--

                            #region --Sales Book Recording(SV)--

                            var sales = new FilprideSalesBook();

                            if (model.ServiceInvoice.Customer.VatType == "Vatable")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.VatAmount = viewModelDMCM.VatAmount;
                                sales.VatableSales = viewModelDMCM.Total / 1.12m;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            else if (model.ServiceInvoice.Customer.VatType == "Exempt")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.VatExemptSales = viewModelDMCM.Total;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            else
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CreditMemoNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                                sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                                sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.ZeroRated = viewModelDMCM.Total;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SV)--

                            decimal withHoldingTaxAmount = 0;
                            decimal withHoldingVatAmount = 0;
                            decimal netOfVatAmount = 0;
                            decimal vatAmount = 0;

                            if (model.ServiceInvoice.Customer.VatType == SD.VatType_Vatable)
                            {
                                netOfVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                                vatAmount = (_unitOfWork.FilprideCreditMemo.ComputeNetOfVat(Math.Abs(netOfVatAmount))) * -1;
                            }
                            else
                            {
                                netOfVatAmount = model.CreditAmount;
                            }

                            if (model.ServiceInvoice.Customer.WithHoldingTax)
                            {
                                withHoldingTaxAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                            }

                            if (model.ServiceInvoice.Customer.WithHoldingVat)
                            {
                                withHoldingVatAmount = (_unitOfWork.FilprideCreditMemo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                            }

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CreditMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010204",
                                        AccountTitle = "AR-Non Trade Receivable",
                                        Debit = 0,
                                        Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            if (withHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CreditMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
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
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CreditMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
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
                                Date = viewModelDMCM.Period,
                                Reference = model.CreditMemoNo,
                                Description = model.ServiceInvoice.Service.Name,
                                AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
                                Debit = Math.Round(viewModelDMCM.NetAmount),
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
                                        Reference = model.CreditMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "2010304",
                                        AccountTitle = "Deferred Vat Output",
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

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Posted.";
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(Print), new { id });
            }

            return NotFound();
        }

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
                        model.VoidedDate = DateTime.Now;
                        model.Status = nameof(Status.Voided);

                        await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.CreditMemoNo, cancellationToken);
                        await _unitOfWork.FilprideCreditMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.CreditMemoNo, cancellationToken);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, model.Company);
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

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var cm = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);
            return PartialView("_PreviewCredit", cm);
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
                    model.Amount
                });
            }

            return Json(null);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cm = await _unitOfWork.FilprideCreditMemo.GetAsync(x => x.CreditMemoId == id, cancellationToken);
            if (!cm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of credit memo# {cm.CreditMemoNo}", "Credit Memo", ipAddress, cm.Company);
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
                .OrderBy(cm => cm.CreditMemoNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
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
                worksheet.Cells[row, 12].Value = item.ServicesId;
                worksheet.Cells[row, 13].Value = item.CreatedBy;
                worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 15].Value = item.CancellationRemarks;
                worksheet.Cells[row, 16].Value = item.SalesInvoiceId;
                worksheet.Cells[row, 17].Value = item.CreditMemoNo;
                worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                worksheet.Cells[row, 19].Value = item.CreditMemoId;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CreditMemoList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllCreditMemoIds()
        {
            var cmIds = _dbContext.FilprideCreditMemos
                                     .Select(cm => cm.CreditMemoId) // Assuming Id is the primary key
                                     .ToList();

            return Json(cmIds);
        }
    }
}