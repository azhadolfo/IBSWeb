using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();
            var dm = await _unitOfWork.FilprideDebitMemo.GetAllAsync(dm => dm.Company == companyClaims, cancellationToken);

            return View(dm);
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
            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice < existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input less than the existing SI unit price!");
                }
                if (model.Quantity < existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount < existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input less than the existing SV amount!");
                }
            }

            if (ModelState.IsValid)
            {
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

                model.DebitMemoNo = await _unitOfWork.FilprideDebitMemo.GenerateCodeAsync(companyClaims, cancellationToken);
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Company = companyClaims;

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    model.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.VatType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / 1.12m;
                        model.VatAmount = model.DebitAmount - model.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }

                        model.TotalSales = model.VatableSales + model.VatAmount;
                    }
                    else
                    {
                        model.TotalSales = model.DebitAmount;
                    }
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

                    model.DebitAmount = model.Amount ?? 0;

                    if (existingSv.Customer.VatType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / 1.12m;
                        model.VatAmount = model.DebitAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        model.TotalSales = model.DebitAmount;
                    }
                }

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Debit memo created successfully.";

                return RedirectToAction("Index");
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
                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

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
                                sales.VatAmount = model.VatAmount;
                                sales.VatableSales = model.VatableSales;
                                //sales.Discount = model.Discount;
                                sales.NetSales = model.VatableSales;
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
                                sales.NetSales = model.VatableSales;
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
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                                sales.Company = model.Company;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

                            var ledgers = new List<FilprideGeneralLedgerBook>();

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.DebitMemoNo,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = "1010201",
                                    AccountTitle = "AR-Trade Receivable",
                                    Debit = model.DebitAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                            if (model.WithHoldingTaxAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
                                        Debit = model.WithHoldingTaxAmount,
                                        Credit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.WithHoldingVatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
                                        Debit = model.WithHoldingVatAmount,
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
                                        AccountNo = "4010101",
                                        AccountTitle = "Sales - Biodiesel",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = model.VatableSales > 0
                                                    ? model.VatableSales
                                                    : model.DebitAmount,
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
                                        AccountNo = "4010102",
                                        AccountTitle = "Sales - Econogas",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = model.VatableSales > 0
                                                    ? model.VatableSales
                                                    : model.DebitAmount,
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
                                        AccountNo = "4010103",
                                        AccountTitle = "Sales - Envirogas",
                                        Debit = 0,
                                        Company = model.Company,
                                        CreatedBy = model.CreatedBy,
                                        Credit = model.VatableSales > 0
                                                    ? model.VatableSales
                                                    : model.DebitAmount,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (model.VatAmount > 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DebitMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "2010301",
                                        AccountTitle = "Vat Output",
                                        Debit = 0,
                                        Credit = model.VatAmount,
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
                                .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                            #region --Retrieval of Services

                            var services = await _dbContext.FilprideServices.FindAsync(model.ServicesId, cancellationToken);

                            #endregion --Retrieval of Services

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.VatType == "Vatable")
                            {
                                viewModelDMCM.Total = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.NetAmount = _unitOfWork.FilprideServiceInvoice.ComputeNetOfVat(viewModelDMCM.Total);
                                viewModelDMCM.VatAmount = _unitOfWork.FilprideServiceInvoice.ComputeVatAmount(viewModelDMCM.Total);
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? services.Percent / 100m : 0);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? services.Percent / 100m : 0);
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
                                        AccountNo = "1010204",
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
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
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
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
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
                                        AccountNo = "2010304",
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

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Posted.";
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("Index");
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideDebitMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.VoidedBy == null)
                {
                    if (model.PostedBy != null)
                    {
                        model.PostedBy = null;
                    }

                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    ///PENDING - further discussion
                    //await _generalRepo.RemoveRecords<SalesBook>(crb => crb.SerialNo == model.DebitMemoNo);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.DebitMemoNo);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideDebitMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Cancelled.";
                }
                return RedirectToAction("Index");
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

            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice < existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input less than the existing SI unit price!");
                }
                if (model.Quantity < existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input less than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount < existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input less than the existing SV amount!");
                }
            }
            if (ModelState.IsValid)
            {
                var existingDM = await _dbContext
                        .FilprideDebitMemos
                        .FirstOrDefaultAsync(dm => dm.DebitMemoId == model.DebitMemoId);

                model.CreatedBy = _userManager.GetUserName(this.User);

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

                    if (existingSalesInvoice.Customer.VatType == "Vatable")
                    {
                        existingDM.VatableSales = existingDM.DebitAmount / 1.12m;
                        existingDM.VatAmount = existingDM.DebitAmount - existingDM.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            existingDM.WithHoldingTaxAmount = existingDM.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            existingDM.WithHoldingVatAmount = existingDM.VatableSales * 0.05m;
                        }
                        existingDM.TotalSales = existingDM.VatableSales + existingDM.VatAmount;
                    }
                    else
                    {
                        existingDM.TotalSales = existingDM.DebitAmount;
                    }
                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    existingDM.ServicesId = existingSv.ServiceId;

                    var services = await _dbContext
                    .FilprideServices
                    .FirstOrDefaultAsync(s => s.ServiceId == existingDM.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region -- Saving Default Enries --

                    existingDM.TransactionDate = model.TransactionDate;
                    existingDM.ServiceInvoiceId = model.ServiceInvoiceId;
                    existingDM.Period = model.Period;
                    existingDM.Amount = model.Amount;
                    existingDM.Description = model.Description;
                    existingDM.Remarks = model.Remarks;

                    #endregion -- Saving Default Enries --

                    existingDM.DebitAmount = model.Amount ?? 0;

                    if (existingSv.Customer.VatType == "Vatable")
                    {
                        existingDM.VatableSales = existingDM.DebitAmount / 1.12m;
                        existingDM.VatAmount = existingDM.DebitAmount - existingDM.VatableSales;
                        existingDM.TotalSales = existingDM.VatableSales + existingDM.VatAmount;
                        existingDM.WithHoldingTaxAmount = existingDM.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            existingDM.WithHoldingVatAmount = existingDM.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        existingDM.TotalSales = existingDM.DebitAmount;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Debit Memo edited successfully";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideDebitMemo.GetAsync(x => x.DebitMemoId == id, cancellationToken);
            if (cv?.IsPrinted == false)
            {
                #region --Audit Trail Recording

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of cv# {cv.CVNo}", "Check Vouchers");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }
    }
}