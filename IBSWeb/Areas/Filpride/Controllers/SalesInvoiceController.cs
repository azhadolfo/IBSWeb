using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.SalesInvoice))
            {
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.Company == companyClaims && si.Type == nameof(DocumentType.Documented), cancellationToken);

                return View("ExportIndex", salesInvoices);
            }

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
                            s.Product.ProductName.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.CreatedBy.ToLower().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue) ||
                            s.Remarks.ToLower().Contains(searchValue) ||
                            s.DeliveryReceipt?.DeliveryReceiptNo.ToLower().Contains(searchValue) == true
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
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region Saving Default Entries

                    var existingCustomer = await _unitOfWork.FilprideCustomer
                        .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken);

                    model.SalesInvoiceNo = await _unitOfWork.FilprideSalesInvoice.GenerateCodeAsync(companyClaims, model.Type, cancellationToken);
                    model.CreatedBy = _userManager.GetUserName(User);
                    model.Amount = model.Quantity * model.UnitPrice;
                    model.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(model.Terms, model.TransactionDate);
                    model.Company = companyClaims;

                    if (model.Amount >= model.Discount)
                    {
                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.CreatedBy, $"Create new sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _unitOfWork.FilprideSalesInvoice.AddAsync(model, cancellationToken);
                        await _unitOfWork.SaveAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
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
                    await transaction.RollbackAsync(cancellationToken);
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
                    customer.CustomerType,
                    customer.WithHoldingTax,
                    CosList = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListPerCustomerAsync(customerId, cancellationToken)
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public async Task<JsonResult> GetProductAndDRDetails(int cosId, CancellationToken cancellationToken)
        {
            var cos = await _unitOfWork.FilprideCustomerOrderSlip.GetAsync(c => c.CustomerOrderSlipId == cosId, cancellationToken);
            if (cos != null)
            {
                return Json(new
                {
                    cos.Product.ProductId,
                    ProductName = $"{cos.Product.ProductCode} {cos.Product.ProductName}",
                    cos.Product.ProductUnit,
                    cos.DeliveredPrice,
                    cos.Terms,
                    DrList = await _unitOfWork.FilprideDeliveryReceipt.GetDeliveryReceiptListForSalesInvoice(cos.CustomerOrderSlipId, cancellationToken)
                });
            }
            return Json(null);
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
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var existingRecord = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    var customer = await _unitOfWork.FilprideCustomer
                        .GetAsync(c => c.CustomerId == model.CustomerId, cancellationToken) ?? throw new NullReferenceException();

                    if (existingRecord == null)
                    {
                        return NotFound();
                    }

                    existingRecord.CustomerId = model.CustomerId;
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
                    existingRecord.CustomerOrderSlipId = model.CustomerOrderSlipId;
                    existingRecord.DeliveryReceiptId = model.DeliveryReceiptId;
                    existingRecord.Terms = model.Terms;
                    existingRecord.DueDate = await _unitOfWork.FilprideSalesInvoice.ComputeDueDateAsync(existingRecord.Terms, model.TransactionDate);

                    existingRecord.EditedBy = _userManager.GetUserName(User);
                    existingRecord.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(existingRecord.EditedBy, $"Edited sales invoice# {existingRecord.SalesInvoiceNo}", "Sales Invoice", ipAddress, existingRecord.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _unitOfWork.SaveAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Sales invoice updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    model.Customers = await _unitOfWork.GetFilprideCustomerListAsync(companyClaims, cancellationToken);
                    model.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);
                    await transaction.RollbackAsync(cancellationToken);
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

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(s => s.SalesInvoiceId == id, cancellationToken);

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

                        var accountTitlesDto = await _unitOfWork.FilprideChartOfAccount.GetListOfAccountTitleDto(cancellationToken);

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
                            sales.Amount = model.Amount - model.Discount;
                            sales.VatableSales = _unitOfWork.FilprideSalesInvoice.ComputeNetOfVat(sales.Amount);
                            sales.VatAmount = _unitOfWork.FilprideSalesInvoice.ComputeVatAmount(sales.VatableSales);
                            sales.Discount = model.Discount;
                            sales.NetSales = (model.Amount - model.Discount) / 1.12m;
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
                            sales.NetSales = model.Amount - model.Discount;
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
                            sales.NetSales = model.Amount - model.Discount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                            sales.Company = model.Company;
                        }

                        await _dbContext.FilprideSalesBooks.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        decimal netDiscount = model.Amount - model.Discount;
                        decimal netOfVatAmount = model.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideSalesInvoice.ComputeNetOfVat(netDiscount) : netDiscount;
                        decimal vatAmount = model.Customer.VatType == SD.VatType_Vatable ? _unitOfWork.FilprideSalesInvoice.ComputeVatAmount(netOfVatAmount) : 0;
                        decimal withHoldingTaxAmount = model.Customer.WithHoldingTax ? _unitOfWork.FilprideSalesInvoice.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                        decimal withHoldingVatAmount = model.Customer.WithHoldingVat ? _unitOfWork.FilprideSalesInvoice.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                        var ledgers = new List<FilprideGeneralLedgerBook>();

                        var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");

                        ledgers.Add(
                            new FilprideGeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.SalesInvoiceNo,
                                Description = model.Product.ProductName,
                                AccountNo = arTradeReceivableTitle.AccountNumber,
                                AccountTitle = arTradeReceivableTitle.AccountName,
                                Debit = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount),
                                Credit = 0,
                                Company = model.Company,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                        if (withHoldingTaxAmount > 0)
                        {
                            var withHoldingTaxTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060500") ?? throw new ArgumentException("Account title '101060700' not found.");

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = withHoldingTaxTitle.AccountNumber,
                                    AccountTitle = withHoldingTaxTitle.AccountName,
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
                            var withHoldingVatTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060700") ?? throw new ArgumentException("Account title '101060700' not found.");

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = withHoldingVatTitle.AccountNumber,
                                    AccountTitle = withHoldingVatTitle.AccountName,
                                    Debit = withHoldingVatAmount,
                                    Credit = 0,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        var (salesAccountNo, salesAccountTitle) = _unitOfWork.FilprideSalesInvoice.GetSalesAccountTitle(model.Product.ProductCode);

                        ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = salesAccountNo,
                                    AccountTitle = salesAccountTitle,
                                    Debit = 0,
                                    Credit = netOfVatAmount,
                                    Company = model.Company,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                        if (vatAmount > 0)
                        {
                            var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account title '201030100' not found.");

                            ledgers.Add(
                                new FilprideGeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SalesInvoiceNo,
                                    Description = model.Product.ProductName,
                                    AccountNo = vatOutputTitle.AccountNumber,
                                    AccountTitle = vatOutputTitle.AccountName,
                                    Debit = 0,
                                    Credit = vatAmount,
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

                        #region--DR process

                        if (model.DeliveryReceiptId != null)
                        {
                            var existingDr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(dr => dr.DeliveryReceiptId == model.DeliveryReceiptId, cancellationToken) ?? throw new ArgumentNullException($"The DR#{model.DeliveryReceiptId} not found! Contact MIS Enterprise.");

                            existingDr.HasAlreadyInvoiced = true;
                            existingDr.Status = nameof(DRStatus.Invoiced);
                        }

                        #endregion

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.PostedBy, $"Posted sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Posted.";
                        return RedirectToAction(nameof(Print), new { id });
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            var existingInventory = await _dbContext.FilprideInventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.SalesInvoiceNo && i.Company == model.Company);

            if (model != null && existingInventory != null)
            {
                var hasAlreadyBeenUsed =
                    await _dbContext.FilprideCollectionReceipts.AnyAsync(cr => cr.SalesInvoiceId == model.SalesInvoiceId && cr.Status != nameof(Status.Voided), cancellationToken) ||
                    await _dbContext.FilprideDebitMemos.AnyAsync(dm => dm.SalesInvoiceId == model.SalesInvoiceId && dm.Status != nameof(Status.Voided), cancellationToken) ||
                    await _dbContext.FilprideCreditMemos.AnyAsync(cm => cm.SalesInvoiceId == model.SalesInvoiceId && cm.Status != nameof(Status.Voided), cancellationToken);

                if (hasAlreadyBeenUsed)
                {
                    TempData["error"] = "Please note that this record has already been utilized in collection receipts, debit or credit memo. As a result, voiding it is not permitted.";
                    return RedirectToAction(nameof(Index));
                }

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

                        await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideSalesBook>(sb => sb.SerialNo == model.SalesInvoiceNo, cancellationToken);
                        await _unitOfWork.FilprideSalesInvoice.RemoveRecords<FilprideGeneralLedgerBook>(gl => gl.Reference == model.SalesInvoiceNo, cancellationToken);
                        await _unitOfWork.FilprideInventory.VoidInventory(existingInventory, cancellationToken);

                        var dr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(d => d.HasAlreadyInvoiced && d.DeliveryReceiptId == model.DeliveryReceiptId);

                        if (dr != null)
                        {
                            dr.HasAlreadyInvoiced = false;
                            dr.Status = nameof(DRStatus.ForInvoicing);
                        }

                        #region --Audit Trail Recording

                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        FilprideAuditTrail auditTrailBook = new(model.VoidedBy, $"Voided sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", ipAddress, model.Company);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Voided.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return BadRequest();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _unitOfWork.FilprideSalesInvoice.GetAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (model != null)
            {
                if (model.CanceledBy == null)
                {
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTimeHelper.GetCurrentPhilippineTime();
                    model.PaymentStatus = nameof(Status.Canceled);
                    model.Status = nameof(Status.Canceled);
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    FilprideAuditTrail auditTrailBook = new(model.CanceledBy, $"Canceled sales invoice# {model.SalesInvoiceNo}", "Sales Invoice", ipAddress, model.Company);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
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
            var si = await _unitOfWork.FilprideSalesInvoice.GetAsync(x => x.SalesInvoiceId == id, cancellationToken);
            if (!si.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(User);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                FilprideAuditTrail auditTrailBook = new(printedBy, $"Printed original copy of sales invoice# {si.SalesInvoiceNo}", "Sales Invoice", ipAddress, si.Company);
                await _dbContext.AddAsync(auditTrailBook, cancellationToken);

                #endregion --Audit Trail Recording

                si.IsPrinted = true;
                await _unitOfWork.SaveAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> GetDrDetails(int? drId, CancellationToken cancellationToken)
        {
            var dr = await _unitOfWork.FilprideDeliveryReceipt.GetAsync(d => d.DeliveryReceiptId == drId, cancellationToken);

            if (dr != null)
            {
                var automatedRr = await _unitOfWork.FilprideReceivingReport.GetAsync(rr => rr.DeliveryReceiptId == dr.DeliveryReceiptId && rr.Status == nameof(Status.Posted), cancellationToken);

                return Json(new
                {
                    TransactionDate = dr.DeliveredDate,
                    dr.Quantity,
                    automatedRr.ReceivingReportId,
                    automatedRr.PurchaseOrder.PurchaseOrderId,
                    OtherRefNo = dr.ManualDrNo,
                    Remarks = $"Customer PO# {dr.CustomerOrderSlip.CustomerPoNo}" +
                              (!dr.Customer.HasBranch ? "" : $"\nBranch: {dr.CustomerOrderSlip.Branch}")
                });
            }

            return Json(null);
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

            var selectedList = await _unitOfWork.FilprideSalesInvoice
                .GetAllAsync(invoice => recordIds.Contains(invoice.SalesInvoiceId));

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("SalesInvoice");

            worksheet.Cells["A1"].Value = "OtherRefNo";
            worksheet.Cells["B1"].Value = "Quantity";
            worksheet.Cells["C1"].Value = "UnitPrice";
            worksheet.Cells["D1"].Value = "Amount";
            worksheet.Cells["E1"].Value = "Remarks";
            worksheet.Cells["F1"].Value = "Status";
            worksheet.Cells["G1"].Value = "TransactionDate";
            worksheet.Cells["H1"].Value = "Discount";
            worksheet.Cells["I1"].Value = "AmountPaid";
            worksheet.Cells["J1"].Value = "Balance";
            worksheet.Cells["K1"].Value = "IsPaid";
            worksheet.Cells["L1"].Value = "IsTaxAndVatPaid";
            worksheet.Cells["M1"].Value = "DueDate";
            worksheet.Cells["N1"].Value = "CreatedBy";
            worksheet.Cells["O1"].Value = "CreatedDate";
            worksheet.Cells["P1"].Value = "CancellationRemarks";
            worksheet.Cells["Q1"].Value = "OriginalReceivingReportId";
            worksheet.Cells["R1"].Value = "OriginalCustomerId";
            worksheet.Cells["S1"].Value = "OriginalPOId";
            worksheet.Cells["T1"].Value = "OriginalProductId";
            worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["V1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.OtherRefNo;
                worksheet.Cells[row, 2].Value = item.Quantity;
                worksheet.Cells[row, 3].Value = item.UnitPrice;
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 5].Value = item.Remarks;
                worksheet.Cells[row, 6].Value = item.Status;
                worksheet.Cells[row, 7].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = item.Discount;
                worksheet.Cells[row, 9].Value = item.AmountPaid;
                worksheet.Cells[row, 10].Value = item.Balance;
                worksheet.Cells[row, 11].Value = item.IsPaid;
                worksheet.Cells[row, 12].Value = item.IsTaxAndVatPaid;
                worksheet.Cells[row, 13].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 14].Value = item.CreatedBy;
                worksheet.Cells[row, 15].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 16].Value = item.CancellationRemarks;
                worksheet.Cells[row, 17].Value = item.ReceivingReportId;
                worksheet.Cells[row, 18].Value = item.CustomerId;
                worksheet.Cells[row, 19].Value = item.PurchaseOrderId;
                worksheet.Cells[row, 20].Value = item.ProductId;
                worksheet.Cells[row, 21].Value = item.SalesInvoiceNo;
                worksheet.Cells[row, 22].Value = item.SalesInvoiceId;

                row++;
            }

            //Set password in Excel
            worksheet.Protection.IsProtected = true;
            worksheet.Protection.SetPassword("mis123");

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesInvoiceList.xlsx");
        }

        #endregion -- export xlsx record --

        [HttpGet]
        public IActionResult GetAllSalesInvoiceIds()
        {
            var invoiceIds = _dbContext.FilprideSalesInvoices
                                     .Where(invoice => invoice.Type == nameof(DocumentType.Documented))
                                     .Select(invoice => invoice.SalesInvoiceId) // Assuming Id is the primary key
                                     .ToList();

            return Json(invoiceIds);
        }
    }
}
