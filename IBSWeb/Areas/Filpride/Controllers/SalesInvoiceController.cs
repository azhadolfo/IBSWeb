using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_CreditAndCollection, SD.Department_RCD)]
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

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalesInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.Company == companyClaims, cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    salesInvoices = salesInvoices
                        .Where(s =>
                            s.SalesInvoiceNo.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerName.ToLower().Contains(searchValue) ||
                            s.Customer.CustomerTerms.ToLower().Contains(searchValue) ||
                            s.Product.ProductCode.ToLower().Contains(searchValue) ||
                            s.Product.ProductName.ToLower().Contains(searchValue) ||
                            s.OtherRefNo.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            s.Quantity.ToString().Contains(searchValue) ||
                            s.UnitPrice.ToString().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.Remarks.ToLower().Contains(searchValue) ||
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

                    salesInvoices = salesInvoices
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = salesInvoices.Count();

                var pagedData = salesInvoices
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
            var companyClaims = await GetCompanyClaimAsync();

            FilprideSalesInvoice viewModel = new()
            {
                Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncById(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FilprideSalesInvoice model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    #region Saving Default Entries

                    var existingCustomer = await _unitOfWork.FilprideCustomer
                        .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    model.SalesInvoiceNo = await _unitOfWork.FilprideSalesInvoice.GenerateCodeAsync(companyClaims, model.Type, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Amount = model.Quantity * model.UnitPrice;
                    model.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingCustomer.CustomerTerms, model.TransactionDate);
                    model.Company = companyClaims;

                    if (model.Amount >= model.Discount)
                    {
                        if (existingCustomer.VatType == "Vatable")
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
                        else if (existingCustomer.VatType == "Zero Rated")
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
                        model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                        model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                        TempData["error"] = "Please input below or exact amount based on the Sales Invoice";
                        return View(model);
                    }

                    #endregion Saving Default Entries
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
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
                var companyClaims = await GetCompanyClaimAsync();
                var salesInvoice = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);
                salesInvoice.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                salesInvoice.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                salesInvoice.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(po => po.Company == companyClaims && po.ProductId == salesInvoice.ProductId && po.QuantityReceived != 0 && po.PostedBy != null)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Where(rr => rr.POId == salesInvoice.PurchaseOrderId && rr.ReceivedDate != null)
                    .Select(rr => new
                    {
                        rr.ReceivingReportId,
                        rr.ReceivingReportNo,
                        rr.ReceivedDate
                    })
                    .ToListAsync();

                salesInvoice.RR = receivingReports.Select(rr => new SelectListItem
                {
                    Value = rr.ReceivingReportId.ToString(),
                    Text = rr.ReceivingReportNo
                }).ToList();

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
            var companyClaims = await GetCompanyClaimAsync();

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
                    existingRecord.ReceivingReportId = model.ReceivingReportId;
                    existingRecord.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingRecord.Customer.CustomerTerms, model.TransactionDate);

                    if (existingRecord.Amount >= model.Discount)
                    {
                        if (existingRecord.Customer.VatType == "Vatable")
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
                        else if (existingRecord.Customer.VatType == "Zero Rated")
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

                        existingRecord.EditedBy = _userManager.GetUserName(User);
                        existingRecord.EditedDate = DateTime.Now;

                        await _unitOfWork.SaveAsync(cancellationToken);
                        TempData["success"] = "Sales invoice updated successfully";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(model);
                }
            }

            model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
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
                        model.Status = nameof(Status.Posted);

                        #region --Sales Book Recording

                        var sales = new FilprideSalesBook();

                        if (model.Customer.VatType == "Vatable")
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
                            sales.Company = model.Company;
                        }
                        else if (model.Customer.VatType == "Exempt")
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
                            sales.Company = model.Company;
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
                            sales.Company = model.Company;
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
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
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
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
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
                                    Company = model.Company,
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
                                    Company = model.Company,
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
                                    Company = model.Company,
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
                                    Company = model.Company,
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
                        return RedirectToAction(nameof(Print), new { id });
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

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.SalesInvoiceNo && i.Company == model.Company);

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
                    model.Status = nameof(Status.Voided);

                    await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideSalesBook>(sb => sb.SerialNo == model.SalesInvoiceNo, cancellationToken);
                    await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.SalesInvoiceNo, cancellationToken);
                    await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideInventory>(i => i.Reference == model.SalesInvoiceNo, cancellationToken);
                    await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);
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
                    model.PaymentStatus = nameof(Status.Canceled);
                    model.Status = nameof(Status.Canceled);

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
            var companyClaims = await GetCompanyClaimAsync();

            var purchaseOrders = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.Company == companyClaims && po.ProductId == productId && po.QuantityReceived != 0 && po.PostedBy != null)
                .ToListAsync();

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _unitOfWork.FilprideSalesInvoice.GetAsync(x => x.SalesInvoiceId == id, cancellationToken);
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

        public IActionResult GetRRs(int purchaseOrderId)
        {
            var rrs = _dbContext.FilprideReceivingReports
                              .Where(rr => rr.POId == purchaseOrderId && rr.ReceivedDate != null)
                              .Select(rr => new
                              {
                                  rr.ReceivingReportId,
                                  rr.ReceivingReportNo,
                                  rr.ReceivedDate
                              })
                              .ToList();

            return Json(rrs);
        }
    }
}