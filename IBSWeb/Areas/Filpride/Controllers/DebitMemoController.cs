using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride;
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

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
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
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    debitMemos = debitMemos
                        .Where(s =>
                            s.DebitMemoNo.ToLower().Contains(searchValue) ||
                            (s.SalesInvoice?.SalesInvoiceNo.ToLower().Contains(searchValue) == true) ||
                            (s.ServiceInvoice?.ServiceInvoiceNo.ToLower().Contains(searchValue) == true) ||
                            s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            s.DebitAmount.ToString().Contains(searchValue) ||
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
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideDebitMemo();
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
        public async Task<IActionResult> Create(FilprideDebitMemo model, CancellationToken cancellationToken)
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

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- checking for unposted DM or CM

                    if (model.SalesInvoiceId != null)
                    {
                        var existingSIDMs = _dbContext.FilprideDebitMemos
                                      .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                      .OrderBy(s => s.DebitMemoId)
                                      .ToList();
                        if (existingSIDMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSIDMs.First().DebitMemoNo}");
                            return View(model);
                        }

                        var existingSICMs = _dbContext.FilprideCreditMemos
                                          .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                          .OrderBy(s => s.CreditMemoId)
                                          .ToList();
                        if (existingSICMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSICMs.First().CreditMemoNo}");
                            return View(model);
                        }
                    }
                    else
                    {
                        var existingSVDMs = _dbContext.FilprideDebitMemos
                                      .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                      .OrderBy(s => s.DebitMemoId)
                                      .ToList();
                        if (existingSVDMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVDMs.First().DebitMemoNo}");
                            return View(model);
                        }

                        var existingSVCMs = _dbContext.FilprideCreditMemos
                                          .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && si.PostedBy != null && si.CanceledBy != null && si.VoidedBy != null)
                                          .OrderBy(s => s.CreditMemoId)
                                          .ToList();
                        if (existingSVCMs.Count > 0)
                        {
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVCMs.First().CreditMemoNo}");
                            return View(model);
                        }
                    }

                    #endregion -- checking for unposted DM or CM


                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Company = companyClaims;

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;
                        model.DebitMemoNo = await _unitOfWork.FilprideDebitMemo.GenerateCodeAsync(companyClaims, existingSalesInvoice.Type, cancellationToken);
                        model.Type = existingSalesInvoice.Type;
                        model.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;
                        model.DebitMemoNo = await _unitOfWork.FilprideDebitMemo.GenerateCodeAsync(companyClaims, existingSv.Type, cancellationToken);
                        model.Type = existingSv.Type;
                        model.DebitAmount = model.Amount ?? 0;
                    }

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new debit memo# {model.DebitMemoNo}", "Debit Memo", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Debit memo created successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideDebitMemos == null)
            {
                return NotFound();
            }

            var debitMemo = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);
            if (debitMemo == null)
            {
                return NotFound();
            }
            return View(debitMemo);
        }

        public async Task<IActionResult> Post(int id, ViewModelDMCM viewModelDMCM, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

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

                        if (model.SalesInvoiceId != null)
                        {
                            #region --Retrieval of SI

                            var existingSI = await _dbContext
                                .FilprideSalesInvoices
                                .Include(c => c.Customer)
                                .Include(s => s.Product)
                                .FirstOrDefaultAsync(invoice => invoice.SalesInvoiceId == model.SalesInvoiceId);

                            #endregion --Retrieval of SI

                            #region --Sales Book Recording(SI)--

                            var sales = new FilprideSalesBook();

                            if (model.SalesInvoice.Customer.VatType == "Vatable")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.DebitMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.DebitAmount;
                                sales.VatableSales = _unitOfWork.FilprideDebitMemo.ComputeNetOfVat(sales.Amount);
                                sales.VatAmount = _unitOfWork.FilprideDebitMemo.ComputeVatAmount(sales.VatableSales);
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
                                sales.SerialNo = model.DebitMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.DebitAmount;
                                sales.VatExemptSales = model.DebitAmount;
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
                                sales.SerialNo = model.DebitMemoNo;
                                sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                                sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                                sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                                sales.Description = model.SalesInvoice.Product.ProductName;
                                sales.Amount = model.DebitAmount;
                                sales.ZeroRated = model.DebitAmount;
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
                                netOfVatAmount = _unitOfWork.FilprideCreditMemo.ComputeNetOfVat(model.DebitAmount);
                                vatAmount = _unitOfWork.FilprideCreditMemo.ComputeVatAmount(netOfVatAmount);
                            }
                            else
                            {
                                netOfVatAmount = model.DebitAmount;
                            }

                            if (model.SalesInvoice.Customer.WithHoldingTax)
                            {
                                withHoldingTaxAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                            }

                            if (model.SalesInvoice.Customer.WithHoldingVat)
                            {
                                withHoldingVatAmount = _unitOfWork.FilprideCreditMemo.ComputeEwtAmount(netOfVatAmount, 0.05m);
                            }

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.DebitMemoNo,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = "101020100",
                                    AccountTitle = "AR-Trade Receivable",
                                    Debit = model.DebitAmount - (withHoldingTaxAmount + withHoldingVatAmount),
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                            if (withHoldingTaxAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "101060500",
                                        AccountTitle = "Deferred Withholding Tax",
                                        Debit = withHoldingTaxAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (withHoldingVatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "101060700",
                                        AccountTitle = "Deferred Withholding Vat Input",
                                        Debit = withHoldingVatAmount,
                                        Credit = 0,
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
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "401010100",
                                        AccountTitle = "Sales - Biodiesel",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
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
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "401010200",
                                        AccountTitle = "Sales - Econogas",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
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
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "401010300",
                                        AccountTitle = "Sales - Envirogas",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (vatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "201030100",
                                        AccountTitle = "Vat Output",
                                        Debit = 0,
                                        Credit = vatAmount,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
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
                            var existingSv = await _dbContext.FilprideServiceInvoices
                                .Include(sv => sv.Customer)
                                .Include(sv => sv.Service)
                                .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                viewModelDMCM.Total = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.NetAmount = _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(viewModelDMCM.Total);
                                viewModelDMCM.VatAmount = _unitOfWork.FilprideServiceInvoice.ComputeVatAmount(viewModelDMCM.NetAmount);
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service.Percent / 100m : 0);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service.Percent / 100m : 0);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }

                            #endregion --SV Computation--

                            #region --Sales Book Recording(SV)--

                            var sales = new FilprideSalesBook();

                            if (model.ServiceInvoice.Customer.VatType == "Vatable")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.DebitMemoNo;
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
                                sales.DocumentId = existingSv.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            else if (model.ServiceInvoice.Customer.VatType == "Exempt")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.DebitMemoNo;
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
                                sales.DocumentId = existingSv.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            else
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.DebitMemoNo;
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
                                sales.DocumentId = existingSv.ServiceInvoiceId;
                                sales.Company = model.Company;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SV)--

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DebitMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "101020500",
                                        AccountTitle = "AR-Non Trade Receivable",
                                        Debit = viewModelDMCM.Total - (viewModelDMCM.WithholdingTaxAmount + viewModelDMCM.WithholdingVatAmount),
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            if (viewModelDMCM.WithholdingTaxAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DebitMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "101060500",
                                        AccountTitle = "Deferred Withholding Tax",
                                        Debit = viewModelDMCM.WithholdingTaxAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (viewModelDMCM.WithholdingVatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DebitMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "101060700",
                                        AccountTitle = "Deferred Withholding Vat Input",
                                        Debit = viewModelDMCM.WithholdingVatAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (viewModelDMCM.Total > 0)
                            {
                                ledgers.Add(new FilprideGeneralLedgerBook
                                {
                                    Date = viewModelDMCM.Period,
                                    Reference = model.DebitMemoNo,
                                    Description = model.ServiceInvoice.Service.Name,
                                    AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                    AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
                                    Debit = 0,
                                    Credit = viewModelDMCM.NetAmount,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                });
                            }

                            if (viewModelDMCM.VatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DebitMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "201030400",
                                        AccountTitle = "Deferred Vat Output",
                                        Debit = 0,
                                        Credit = viewModelDMCM.VatAmount,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
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

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted debit memo# {model.DebitMemoNo}", "Debit Memo", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
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

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideDebitMemos.FindAsync(id, cancellationToken);

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

                        await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideSalesBook>(crb => crb.SerialNo == model.DebitMemoNo);
                        await _unitOfWork.FilprideDebitMemo.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.DebitMemoNo);

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided debit memo# {model.DebitMemoNo}", "Debit Memo", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Voided.";
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
            var model = await _dbContext.FilprideDebitMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.CancellationRemarks = cancellationRemarks;
                    model.Status = nameof(Status.Canceled);

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled debit memo# {model.DebitMemoNo}", "Debit Memo", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var dm = await _dbContext.FilprideDebitMemos.FindAsync(id, cancellationToken);
            return PartialView("_PreviewDebit", dm);
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

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.FilprideDebitMemos == null)
            {
                return NotFound();
            }

            var companyClaims = await GetCompanyClaimAsync();

            var debitMemo = await _unitOfWork.FilprideDebitMemo.GetAsync(dm => dm.DebitMemoId == id, cancellationToken);

            if (debitMemo == null)
            {
                return NotFound();
            }

            debitMemo.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.Company == companyClaims && si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            debitMemo.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.Company == companyClaims && sv.PostedBy != null)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            return View(debitMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideDebitMemo model, CancellationToken cancellationToken)
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
                    var existingDM = await _dbContext
                                    .FilprideDebitMemos
                                    .FirstOrDefaultAsync(dm => dm.DebitMemoId == model.DebitMemoId);

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingDM.TransactionDate = model.TransactionDate;
                        existingDM.SalesInvoiceId = model.SalesInvoiceId;
                        existingDM.Quantity = model.Quantity;
                        existingDM.AdjustedPrice = model.AdjustedPrice;
                        existingDM.Description = model.Description;
                        existingDM.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingDM.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingDM.TransactionDate = model.TransactionDate;
                        existingDM.ServiceInvoiceId = model.ServiceInvoiceId;
                        existingDM.Period = model.Period;
                        existingDM.Amount = model.Amount;
                        existingDM.Description = model.Description;
                        existingDM.Remarks = model.Remarks;

                        #endregion -- Saving Default Enries --

                        existingDM.DebitAmount = model.Amount ?? 0;
                    }

                    existingDM.EditedBy = _userManager.GetUserName(User);
                    existingDM.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingDM.EditedBy, $"Edited debit memo# {existingDM.DebitMemoNo}", "Debit Memo", ipAddress, existingSv.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Debit Memo edited successfully";
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

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var dm = await _unitOfWork.FilprideDebitMemo.GetAsync(x => x.DebitMemoId == id, cancellationToken);
            if (!dm.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of debit memo# {dm.DebitMemoNo}", "Debit Memo", ipAddress, dm.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                dm.IsPrinted = true;
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
            var selectedList = await _dbContext.FilprideDebitMemos
                .Where(dm => recordIds.Contains(dm.DebitMemoId) && dm.Type == nameof(DocumentType.Documented))
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .OrderBy(dm => dm.DebitMemoNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
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

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DebitMemoList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllDebitMemoIds()
        {
            var dmIds = _dbContext.FilprideDebitMemos
                                     .Select(dm => dm.DebitMemoId) // Assuming Id is the primary key
                                     .ToList();

            return Json(dmIds);
        }
    }
}
