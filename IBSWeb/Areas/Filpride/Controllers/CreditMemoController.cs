using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cm = await _unitOfWork.FilprideCreditMemo.GetAllAsync(null, cancellationToken);

            return View(cm);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new FilprideCreditMemo();
            viewModel.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            viewModel.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.PostedBy != null)
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
            model.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            model.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.PostedBy != null)
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

                var generatedCM = await _unitOfWork.FilprideCreditMemo.GenerateCodeAsync(cancellationToken);

                model.CreditMemoNo = generatedCM;
                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    model.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.VatType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;

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
                        model.TotalSales = model.CreditAmount;
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

                    model.CreditAmount = -model.Amount ?? 0;

                    if (existingSv.Customer.VatType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        model.TotalSales = model.CreditAmount;
                    }
                }

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Credit memo created successfully.";
                return RedirectToAction("Index");
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

            var creditMemo = await _unitOfWork.FilprideCreditMemo.GetAsync(c => c.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.SalesInvoices = await _dbContext.FilprideSalesInvoices
                .Where(si => si.PostedBy != null)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            creditMemo.ServiceInvoices = await _dbContext.FilprideServiceInvoices
                .Where(sv => sv.PostedBy != null)
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
                var existingCM = await _dbContext
                        .FilprideCreditMemos
                        .FirstOrDefaultAsync(cm => cm.CreditMemoId == model.CreditMemoId);

                model.CreatedBy = _userManager.GetUserName(this.User);

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

                    if (existingSalesInvoice.Customer.VatType == "Vatable")
                    {
                        existingCM.VatableSales = existingCM.CreditAmount / 1.12m;
                        existingCM.VatAmount = existingCM.CreditAmount - existingCM.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            existingCM.WithHoldingTaxAmount = existingCM.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            existingCM.WithHoldingVatAmount = existingCM.VatableSales * 0.05m;
                        }
                        existingCM.TotalSales = existingCM.VatableSales + existingCM.VatAmount;
                    }
                    else
                    {
                        existingCM.TotalSales = existingCM.CreditAmount;
                    }
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

                    if (existingSv.Customer.VatType == "Vatable")
                    {
                        existingCM.VatableSales = existingCM.CreditAmount / 1.12m;
                        existingCM.VatAmount = existingCM.CreditAmount - existingCM.VatableSales;
                        existingCM.TotalSales = existingCM.VatableSales + existingCM.VatAmount;
                        existingCM.WithHoldingTaxAmount = existingCM.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            existingCM.WithHoldingVatAmount = existingCM.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        existingCM.TotalSales = existingCM.CreditAmount;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Credit Memo edited successfully";
                return RedirectToAction("Index");
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
                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

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
                                sales.VatAmount = model.VatAmount;
                                sales.VatableSales = model.VatableSales;
                                //sales.Discount = model.Discount;
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
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
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
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
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

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
                                    Credit = Math.Abs(model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount)),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                            if (model.WithHoldingTaxAmount < 0)
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
                                        Credit = Math.Abs(model.WithHoldingTaxAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.WithHoldingVatAmount < 0)
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
                                        Credit = Math.Abs(model.WithHoldingVatAmount),
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
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        CreatedBy = model.CreatedBy,
                                        Credit = 0,
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
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        Credit = 0,
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
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        Credit = 0,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (model.VatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.SalesInvoice.Product.ProductName,
                                        AccountNo = "2010301",
                                        AccountTitle = "Vat Output",
                                        Debit = Math.Abs(model.VatAmount),
                                        Credit = 0,
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
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SV)--

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
                                        Credit = Math.Abs(model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount)),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            if (model.WithHoldingTaxAmount < 0)
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
                                        Credit = Math.Abs(model.WithHoldingTaxAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.WithHoldingVatAmount < 0)
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
                                        Credit = Math.Abs(model.WithHoldingVatAmount),
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
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });

                            if (model.VatAmount < 0)
                            {
                                ledgers.Add(
                                    new FilprideGeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CreditMemoNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "2010304",
                                        AccountTitle = "Deferred Vat Output",
                                        Debit = Math.Abs(model.VatAmount),
                                        Credit = 0,
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

                        //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Posted.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("Index");
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCreditMemos.FindAsync(id, cancellationToken);

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
                    //await _generalRepo.RemoveRecords<SalesBook>(crb => crb.SerialNo == model.CMNo);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CMNo);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.FilprideCreditMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Cancelled.";
                }
                return RedirectToAction("Index");
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
    }
}