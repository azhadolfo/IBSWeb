using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SalesInvoiceController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        public SalesInvoiceController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(null, cancellationToken);

            return View(salesInvoices);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            FilprideSalesInvoice viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideSalesInvoice model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region Saving Default Entries

                    var existingCustomer = await _unitOfWork.FilprideCustomer
                        .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    model.SalesInvoiceNo = await _unitOfWork.FilprideSalesInvoice.GenerateCodeAsync(cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Amount = model.Quantity * model.UnitPrice;
                    model.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingCustomer.CustomerTerms, model.TransactionDate);

                    if (model.Amount >= model.Discount)
                    {
                        if (existingCustomer.CustomerType == "Vatable")
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.VatableSales = model.NetDiscount / 1.12m;
                            model.VatAmount = model.NetDiscount - model.VatableSales;
                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                            }
                        }
                        else if (existingCustomer.CustomerType == "Zero Rated")
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.ZeroRated = model.Amount;

                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.ZeroRated * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.ZeroRated * 0.05m;
                            }
                        }
                        else
                        {
                            model.NetDiscount = model.Amount - model.Discount;
                            model.VatExempt = model.Amount;
                            if (existingCustomer.WithHoldingTax)
                            {
                                model.WithHoldingTaxAmount = model.VatExempt * 0.01m;
                            }
                            if (existingCustomer.WithHoldingVat)
                            {
                                model.WithHoldingVatAmount = model.VatExempt * 0.05m;
                            }
                        }

                        await _unitOfWork.FilprideSalesInvoice.AddAsync(model, cancellationToken);
                        await _unitOfWork.SaveAsync(cancellationToken);
                        TempData["success"] = "Sales invoice created successfully";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                        model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                        TempData["error"] = "Please input below or exact amount based on the Sales Invoice";
                        return View(model);
                    }

                    #endregion Saving Default Entries
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _unitOfWork.FilprideCustomer.GetAsync(c => c.CustomerId == customerId, cancellationToken);
            if (customer != null)
            {
                return Json(new
                {
                    SoldTo = customer.CustomerName,
                    Address = customer.CustomerAddress,
                    TinNo = customer.CustomerTin,
                    customer.BusinessStyle,
                    Terms = customer.CustomerTerms,
                    customer.CustomerType,
                    customer.WithHoldingTax
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public async Task<JsonResult> GetProductDetails(int productId, CancellationToken cancellationToken)
        {
            var product = await _unitOfWork.Product.GetAsync(c => c.ProductId == productId, cancellationToken);
            if (product != null)
            {
                return Json(new
                {
                    product.ProductName,
                    product.ProductUnit
                });
            }
            return Json(null); // Return null if no matching product is found
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var salesInvoice = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);
                salesInvoice.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                salesInvoice.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                return View(salesInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(FilprideSalesInvoice model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingRecord = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    existingRecord.TransactionDate = model.TransactionDate;
                    existingRecord.OtherRefNo = model.OtherRefNo;
                    existingRecord.PurchaseOrderId = model.PurchaseOrderId;
                    existingRecord.Quantity = model.Quantity;
                    existingRecord.UnitPrice = model.UnitPrice;
                    existingRecord.Remarks = model.Remarks;
                    existingRecord.Discount = model.Discount;
                    existingRecord.Amount = model.Quantity * model.UnitPrice;
                    existingRecord.ProductId = model.ProductId;

                    if (existingRecord.Amount >= model.Discount)
                    {
                        if (existingRecord.Customer.CustomerType == "Vatable")
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.VatableSales = existingRecord.NetDiscount / 1.12m;
                            existingRecord.VatAmount = existingRecord.NetDiscount - existingRecord.VatableSales;
                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.VatableSales * (decimal)0.01;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.VatableSales * (decimal)0.05;
                            }
                        }
                        else if (existingRecord.Customer.CustomerType == "Zero Rated")
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.ZeroRated = existingRecord.Amount;

                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.ZeroRated * 0.01m;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.ZeroRated * 0.05m;
                            }
                        }
                        else
                        {
                            existingRecord.NetDiscount = existingRecord.Amount - model.Discount;
                            existingRecord.VatExempt = existingRecord.Amount;
                            if (existingRecord.Customer.WithHoldingTax)
                            {
                                existingRecord.WithHoldingTaxAmount = existingRecord.VatExempt * 0.01m;
                            }
                            if (existingRecord.Customer.WithHoldingVat)
                            {
                                existingRecord.WithHoldingVatAmount = existingRecord.VatExempt * 0.05m;
                            }
                        }

                        await _unitOfWork.SaveAsync(cancellationToken);
                        TempData["success"] = "Sales invoice updated successfully";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(cancellationToken);
            model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
            TempData["error"] = "The submitted information is invalid.";
            return View(model);
        }

        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            var sales = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);
            return View(sales);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            var invoice = await _unitOfWork.FilprideSalesInvoice.GetAsync(s => s.SalesInvoiceId == id, cancellationToken);
            return PartialView("_PreviewPartialView", invoice);
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(s => s.SalesInvoiceId == id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (model.PostedBy == null)
                    {
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --Sales Book Recording

                        var sales = new FilprideSalesBook();

                        if (model.Customer.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SalesInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Product.ProductName;
                            sales.Amount = model.Amount;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.VatableSales;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount / 1.12m;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else if (model.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SalesInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Product.ProductName;
                            sales.Amount = model.Amount;
                            sales.VatExemptSales = model.Amount;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SalesInvoiceNo;
                            sales.SoldTo = model.Customer.CustomerName;
                            sales.TinNo = model.Customer.CustomerTin;
                            sales.Address = model.Customer.CustomerAddress;
                            sales.Description = model.Product.ProductName;
                            sales.Amount = model.Amount;
                            sales.ZeroRated = model.Amount;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }

                        await _dbContext.FilprideSalesBooks.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.SalesInvoiceNo,
                                Description = model.Product.ProductName,
                                AccountNo = "1010201",
                                AccountTitle = "AR-Trade Receivable",
                                Debit = model.NetDiscount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
                                Credit = 0,
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
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = model.WithHoldingTaxAmount,
                                    Credit = 0,
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
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = model.WithHoldingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.Product.ProductName == "BIODIESEL")
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "4010101",
                                    AccountTitle = "Sales - Biodiesel",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.Product.ProductName == "ECONOGAS")
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "4010102",
                                    AccountTitle = "Sales - Econogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.Product.ProductName == "ENVIROGAS")
                        {
                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "4010103",
                                    AccountTitle = "Sales - Envirogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
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
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = model.VatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_unitOfWork.FilprideSalesInvoice.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.FilprideGeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        #region--Inventory Recording

                        await _unitOfWork.FilprideInventory.AddSalesToInventoryAsync(model, cancellationToken);

                        #endregion

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Posted.";
                        return RedirectToAction("Index");
                    }
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
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

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

                    //await _generalRepo.RemoveRecords<SalesBook>(sb => sb.SerialNo == model.SINo, cancellationToken);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SINo, cancellationToken);
                    //await _generalRepo.RemoveRecords<Inventory>(i => i.Reference == model.SINo, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = "Cancelled";

                    ///PENDING
                    //model.CancellationRemarks = cancellationRemarks;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> GetPOs(int productId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.ProductId == productId && po.QuantityReceived != 0 && po.PostedBy != null)
                .ToListAsync();

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }
    }
}