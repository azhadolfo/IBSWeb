using System.Drawing;
using System.Globalization;
using System.Text;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ReportController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
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

        public async Task<IActionResult> SalesBook()
        {
            var companyClaims = await GetCompanyClaimAsync();

            var viewModel = new ViewModelBook
            {
                SOA = await _dbContext.FilprideServiceInvoices
                .Where(soa => soa.PostedBy != null && soa.Company == companyClaims)
                .Select(soa => new SelectListItem
                {
                    Value = soa.ServiceInvoiceId.ToString(),
                    Text = soa.ServiceInvoiceNo
                })
                .ToListAsync(),
                SI = await _dbContext.FilprideSalesInvoices
                .Where(si => si.PostedBy != null && si.Company == companyClaims)
                .Select(soa => new SelectListItem
                {
                    Value = soa.SalesInvoiceId.ToString(),
                    Text = soa.SalesInvoiceNo
                })
                .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> SalesBookReport(ViewModelBook model, string? selectedDocument, string? soaList, string? siList)
        {
            ViewBag.DateFrom = model.DateFrom.ToString();
            ViewBag.DateTo = model.DateTo;

            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (soaList != null || siList != null)
                    {
                        return RedirectToAction("TransactionReportsInSOA", new { soaList = soaList, siList = siList });
                    }

                    var salesBook = _unitOfWork.FilprideReport.GetSalesBooks(model.DateFrom, model.DateTo, selectedDocument, companyClaims);
                    var lastRecord = salesBook.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                    ViewBag.SelectedDocument = selectedDocument;

                    return View(salesBook);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(SalesBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(SalesBook));
        }

        public async Task<IActionResult> TransactionReportsInSOA(int? siList, int? soaList)
        {
            ViewBag.SIList = siList;
            ViewBag.SOAList = soaList;
            var id = siList != null ? siList : soaList;
            var sales = await _dbContext
                .FilprideSalesBooks
                .Where(s => s.DocumentId == id)
                .ToListAsync();

            return View(sales);
        }

        public IActionResult CashReceiptBook()
        {
            return View();
        }

        public async Task<IActionResult> CashReceiptBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var cashReceiptBooks = _unitOfWork.FilprideReport.GetCashReceiptBooks(model.DateFrom, model.DateTo, companyClaims);
                    var lastRecord = cashReceiptBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(cashReceiptBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(CashReceiptBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(CashReceiptBook));
        }

        public async Task<IActionResult> PurchaseBook()
        {
            var companyClaims = await GetCompanyClaimAsync();

            var viewModel = new ViewModelBook
            {
                PO = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.PostedBy != null && po.Company == companyClaims)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> PurchaseBookReport(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (poListFrom != null && poListTo != null)
                    {
                        return RedirectToAction(nameof(POLiquidationPerPO), new { poListFrom = poListFrom, poListTo = poListTo });
                    }
                    else if (poListFrom == null && poListTo != null || poListFrom != null && poListTo == null)
                    {
                        TempData["error"] = "Please fill the two select list in PO Liquidation Per PO, lowest to highest";
                        return RedirectToAction(nameof(PurchaseBook));
                    }

                    if (selectedFiltering == "UnpostedRR" || selectedFiltering == "POLiquidation")
                    {
                        return RedirectToAction("GetRR", new { DateFrom = model.DateFrom, DateTo = model.DateTo, selectedFiltering });
                    }

                    var purchaseOrders = _unitOfWork.FilprideReport.GetPurchaseBooks(model.DateFrom, model.DateTo, selectedFiltering, companyClaims);
                    var lastRecord = purchaseOrders.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                    ViewBag.SelectedFiltering = selectedFiltering;

                    return View(purchaseOrders);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(PurchaseBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(PurchaseBook));
        }

        public async Task<IActionResult> GetRR(DateOnly? dateFrom, DateOnly? dateTo, string selectedFiltering)
        {
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.SelectedFiltering = selectedFiltering;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (dateFrom == default && dateTo == default)
            {
                TempData["error"] = "Please input Date From and Date To";
                return RedirectToAction(nameof(PurchaseBook));
            }
            else if (dateFrom == default)
            {
                TempData["error"] = "Please input Date To";
                return RedirectToAction(nameof(PurchaseBook));
            }

            IEnumerable<FilprideReceivingReport> receivingReport = await _unitOfWork.FilprideReport.GetReceivingReportAsync(dateFrom, dateTo, selectedFiltering, companyClaims);
            return View(receivingReport);
        }

        public IActionResult POLiquidationPerPO(int? poListFrom, int? poListTo)
        {
            var from = poListFrom;
            var to = poListTo;

            if (poListFrom > poListTo)
            {
                TempData["error"] = "Please input lowest to highest PO#!";
                return RedirectToAction(nameof(PurchaseBook));
            }

            try
            {
                var po = _dbContext
                         .FilprideReceivingReports
                         .Include(rr => rr.PurchaseOrder)
                         .ThenInclude(po => po!.Supplier)
                         .Include(rr => rr.PurchaseOrder)
                         .ThenInclude(po => po!.Product)
                         .AsEnumerable()
                         .Where(rr => rr.POId >= from && rr.POId <= to && rr.PostedBy != null)
                         .OrderBy(rr => rr.POId)
                         .ToList();
                return View(po);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(PurchaseBook));
            }
        }

        public IActionResult InventoryBook()
        {
            return View();
        }

        public async Task<IActionResult> InventoryBookReportAsync(ViewModelBook model)
        {
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateTo.AddDays(-model.DateTo.Day + 1);
                    ViewBag.DateFrom = dateFrom;
                    var inventoryBooks = _unitOfWork.FilprideReport.GetInventoryBooks(dateFrom, model.DateTo, companyClaims);
                    var lastRecord = inventoryBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    return View(inventoryBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(InventoryBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(InventoryBook));
        }

        public IActionResult GeneralLedgerBook()
        {
            return View();
        }

        public async Task<IActionResult> GeneralLedgerBookReportAsync(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var inventoryBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
                    var lastRecord = inventoryBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(inventoryBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(GeneralLedgerBook));
        }

        public IActionResult DisbursementBook()
        {
            return View();
        }

        public async Task<IActionResult> DisbursementBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var disbursementBooks = _unitOfWork.FilprideReport.GetDisbursementBooks(model.DateFrom, model.DateTo, companyClaims);
                    var lastRecord = disbursementBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(disbursementBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(DisbursementBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(DisbursementBook));
        }

        public IActionResult JournalBook()
        {
            return View();
        }

        public async Task<IActionResult> JournalBookReportAsync(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var journalBooks = _unitOfWork.FilprideReport.GetJournalBooks(model.DateFrom, model.DateTo, companyClaims);
                    var lastRecord = journalBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(journalBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(JournalBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(JournalBook));
        }

        public IActionResult AuditTrail()
        {
            return View();
        }

        public async Task<IActionResult> AuditTrailReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var auditTrail = await _unitOfWork.FilprideReport.GetAuditTrails(model.DateFrom, model.DateTo, companyClaims);
                    var lastRecord = auditTrail.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    return View(auditTrail);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(AuditTrail));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(AuditTrail));
        }

        public IActionResult COSUnservedVolume()
        {
            return View();
        }

        public async Task<IActionResult> GenerateCOSUnservedVolume(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom.ToString("MMMM dd, yyyy");
            ViewBag.DateTo = model.DateTo.ToString("MMMM dd, yyyy"); ;
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo, companyClaims);

                    if (cosSummary.Any())
                    {
                        return View(cosSummary);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(COSUnservedVolume));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(COSUnservedVolume));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(COSUnservedVolume));
        }

        [HttpGet]
        public IActionResult DispatchReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedDispatchReport(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(viewModel.ReportType))
                    {
                        return BadRequest();
                    }

                    var companyClaims = await GetCompanyClaimAsync();
                    var currentUser = _userManager.GetUserName(User);
                    var today = DateTimeHelper.GetCurrentPhilippineTime();
                    var firstDayOfMonth = new DateOnly(viewModel.DateFrom.Year, viewModel.DateFrom.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    ViewData["DateFrom"] = firstDayOfMonth;
                    ViewData["DateTo"] = lastDayOfMonth;

                    var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                        .GetAllAsync(i => i.Company == companyClaims
                                          && i.AuthorityToLoadNo != null
                                          && i.Date >= firstDayOfMonth
                                          && i.Date <= lastDayOfMonth
                                          && (viewModel.ReportType == "AllDeliveries" || i.Status == nameof(DRStatus.PendingDelivery)), cancellationToken);

                    if (deliveryReceipts.Any())
                    {
                        return View(deliveryReceipts);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(DispatchReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(DispatchReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(DispatchReport));
        }

        [HttpGet]
        public IActionResult PostedCollection()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TradePayableReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ApReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SalesReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedSalesReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var sales = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims);

                    if (sales.Any())
                    {
                        return View(sales);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(SalesReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(SalesReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(SalesReport));
        }

        [HttpGet]
        public IActionResult ServiceInvoiceReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedServiceInvoiceReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var serviceInvoice = await _unitOfWork.FilprideReport.GetServiceInvoiceReport(model.DateFrom, model.DateTo, companyClaims);

                    if (serviceInvoice.Any())
                    {
                        return View(serviceInvoice);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ServiceInvoiceReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(ServiceInvoiceReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(ServiceInvoiceReport));
        }


        [HttpGet]
        public IActionResult PurchaseOrderReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseOrderReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var purchaseOrder = await _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims);

                    if (purchaseOrder.Any())
                    {
                        return View(purchaseOrder);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseOrderReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(PurchaseOrderReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(PurchaseOrderReport));
        }

        [HttpGet]
        public IActionResult ClearedDisbursementReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedClearedDisbursementReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var checkVoucherHeader = await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo, companyClaims);

                    if (checkVoucherHeader.Any())
                    {
                        return View(checkVoucherHeader);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ClearedDisbursementReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(ClearedDisbursementReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(ClearedDisbursementReport));
        }

        public IActionResult PurchaseReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // get data by chosen date
                    var purchaseReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, dateSelectionType: model.DateSelectionType);

                    if (purchaseReport.Any())
                    {
                        return View(purchaseReport);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(PurchaseReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(PurchaseReport));
        }

        public async Task<IActionResult> GmReport()
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            ViewModelBook viewmodel = new()
            {
                CustomerList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims)
            };

            return View(viewmodel);
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedGmReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // get data by chosen date
                    var grossMarginReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, model.Customers);

                    if (grossMarginReport.Any())
                    {
                        return View(grossMarginReport);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(GmReport));

                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GmReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(PurchaseReport));
        }

        public IActionResult OtcFuelSalesReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ArPerCustomer()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedArPerCustomer(ViewModelBook model, CancellationToken cancellationToken)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    var salesInvoice = await _dbContext.FilprideSalesInvoices
                        .Where(si => si.PostedBy != null)
                        .Include(si => si.Product)
                        .Include(si => si.Customer)
                        .Include(si => si.DeliveryReceipt)
                        .Include(si => si.CustomerOrderSlip)
                        .ToListAsync(cancellationToken);

                    if (salesInvoice.Any())
                    {
                        return View(salesInvoice);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ArPerCustomer));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(ArPerCustomer));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(ArPerCustomer));
        }

        [HttpGet]
        public IActionResult AgingReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedAgingReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAllAsync(si => si.PostedBy != null && si.AmountPaid == 0 && !si.IsPaid, cancellationToken);

                    if (salesInvoice.Any())
                    {
                        return View(salesInvoice);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(AgingReport));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(AgingReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(AgingReport));
        }

        [HttpGet]
        public async Task<IActionResult> GeneralLedgerReportByAccountNumber()
        {

            var viewModel = new GeneralLedgerReportViewModel
            {
                ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(),
            };

            return View(viewModel);
        }

        public async Task<IActionResult> GenerateGeneralLedgerReportByAccountNumberExcelFile(GeneralLedgerReportViewModel model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var chartOfAccount = await _dbContext.FilprideChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == model.AccountNo);

            var generalLedgerByAccountNo = await _dbContext.FilprideGeneralLedgerBooks
                .Include(g => g.Supplier)
                .Include(g => g.Customer)
                .Where(g =>
                    g.Date >= dateFrom && g.Date <= dateTo &&
                    (model.AccountNo == null || g.AccountNo == model.AccountNo) &&
                    g.Company == companyClaims)
                .ToListAsync(cancellationToken);

            if (generalLedgerByAccountNo.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(GeneralLedgerReportByAccountNumber));
            }

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("GeneralLedger");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "GENERAL LEDGER BY ACCOUNT NUMBER";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Account No:";
            worksheet.Cells["A4"].Value = "Account Title:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{chartOfAccount}";
            worksheet.Cells["B4"].Value = $"{chartOfAccount?.AccountNumber}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Particular";
            worksheet.Cells["C7"].Value = "Account No";
            worksheet.Cells["D7"].Value = "Account Title";
            worksheet.Cells["E7"].Value = "Customer Code";
            worksheet.Cells["F7"].Value = "Customer Name";
            worksheet.Cells["G7"].Value = "Supplier Code";
            worksheet.Cells["H7"].Value = "Supplier Name";
            worksheet.Cells["I7"].Value = "Debit";
            worksheet.Cells["J7"].Value = "Credit";
            worksheet.Cells["K7"].Value = "Balance";

            using (var range = worksheet.Cells["A7:K7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            int row = 8;
            decimal balance = 0;
            string currencyFormat = "#,##0.0000";
            decimal debit = 0;
            decimal credit = 0;
            foreach (var grouped in generalLedgerByAccountNo.OrderBy(g => g.AccountNo).GroupBy(g => g.AccountTitle))
            {
                balance = 0;

                foreach (var journal in grouped.OrderBy(g => g.Date))
                {
                    var account = await _dbContext.FilprideChartOfAccounts
                        .FirstOrDefaultAsync(a => a.AccountNumber == journal.AccountNo);

                    if (balance != 0)
                    {
                        if (account?.NormalBalance == nameof(NormalBalance.Debit))
                        {
                            balance += journal.Debit - journal.Credit;
                        }
                        else
                        {
                            balance -= journal.Debit - journal.Credit;
                        }
                    }
                    else
                    {
                        balance = journal.Debit > 0 ? journal.Debit : journal.Credit;
                    }

                    worksheet.Cells[row, 1].Value = journal.Date.ToString("dd-MMM-yyyy");
                    worksheet.Cells[row, 2].Value = journal.Description;
                    worksheet.Cells[row, 3].Value = journal.AccountNo;
                    worksheet.Cells[row, 4].Value = journal.AccountTitle;
                    worksheet.Cells[row, 5].Value = journal.Customer?.CustomerCode;
                    worksheet.Cells[row, 6].Value = journal.Customer?.CustomerName;
                    worksheet.Cells[row, 7].Value = journal.Supplier?.SupplierCode;
                    worksheet.Cells[row, 8].Value = journal.Supplier?.SupplierName;

                    worksheet.Cells[row, 9].Value = journal.Debit;
                    worksheet.Cells[row, 10].Value = journal.Credit;
                    worksheet.Cells[row, 11].Value = balance;

                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                debit = grouped.Sum(j => j.Debit);
                credit = grouped.Sum(j => j.Credit);
                balance = debit - credit;

                worksheet.Cells[row, 8].Value = "Total " + grouped.Key;
                worksheet.Cells[row, 9].Value = debit;
                worksheet.Cells[row, 10].Value = credit;
                worksheet.Cells[row, 11].Value = balance;

                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                using (var range = worksheet.Cells[row, 1, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                row++;
            }

            using (var range = worksheet.Cells[row, 9, row, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            debit = generalLedgerByAccountNo.Sum(j => j.Debit);
            credit = generalLedgerByAccountNo.Sum(j => j.Credit);
            balance = debit - credit;

            worksheet.Cells[row, 9].Value = "Total";
            worksheet.Cells[row, 9].Style.Font.Bold = true;
            worksheet.Cells[row, 10].Value = debit;
            worksheet.Cells[row, 11].Value = credit;
            worksheet.Cells[row, 12].Value = balance;

            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GeneralLedgerByAccountNo_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        //Generate as .txt file

        #region -- Generate Audit Trail .Txt File --

        public async Task<IActionResult> GenerateAuditTrailTxtFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();

                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var auditTrail = await _unitOfWork.FilprideReport.GetAuditTrails(model.DateFrom, model.DateTo, companyClaims);
                    if (auditTrail.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(AuditTrail));
                    }
                    var lastRecord = auditTrail.LastOrDefault();
                    var firstRecord = auditTrail.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
                    fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Audit Trail Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{auditTrail.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{"N/A"}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"25"}\t{"25"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"Username"}\t{"Username"}\t{"27"}\t{"46"}\t{"20"}\t{firstRecord.Username}");
                    fileContent.AppendLine($"{"MachineName"}\t{"Machine Name"}\t{"48"}\t{"77"}\t{"30"}\t{firstRecord.MachineName}");
                    fileContent.AppendLine($"{"Activity"}\t{"Activity"}\t{"79"}\t{"278"}\t{"200"}\t{firstRecord.Activity}");
                    fileContent.AppendLine($"{"DocumentType"}\t{"Document Type"}\t{"280"}\t{"299"}\t{"20"}\t{firstRecord.DocumentType}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("AUDIT TRAIL REPORT");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-25}\t{"Username",-20}\t{"Machine Name",-30}\t{"Activity",-200}\t{"Document Type",-20}");

                    // Generate the records
                    foreach (var record in auditTrail)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy hh:mm:ss tt"),-25}\t{record.Username,-20}\t{record.MachineName,-30}\t{record.Activity,-200}\t{record.DocumentType,-20}");
                    }

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
                    fileContent.AppendLine($"Version: v1.1");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "AuditTrailReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(AuditTrail));
                }
            }
            return View(model);
        }

        #endregion -- Generate Audit Trail .Txt File --

        #region -- Generate Disbursement Book .Txt File --

        public async Task<IActionResult> GenerateDisbursementBookTxtFile(ViewModelBook model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var disbursementBooks = _unitOfWork.FilprideReport.GetDisbursementBooks(model.DateFrom, model.DateTo, companyClaims);
                    if (disbursementBooks.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(DisbursementBook));
                    }
                    var totalDebit = disbursementBooks.Sum(db => db.Debit);
                    var totalCredit = disbursementBooks.Sum(db => db.Credit);
                    var lastRecord = disbursementBooks.LastOrDefault();
                    var firstRecord = disbursementBooks.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Disbursement Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{disbursementBooks.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"CVNo",-8}\t{"CV No",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.CVNo}");
                    fileContent.AppendLine($"{"Payee",-8}\t{"Payee",-18}\t{"25"}\t{"124"}\t{"100"}\t{firstRecord.Payee}");
                    fileContent.AppendLine($"{"Particulars"}\t{"Particulars",-18}\t{"126"}\t{"325"}\t{"200"}\t{firstRecord.Particulars}");
                    fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-18}\t{"327"}\t{"336"}\t{"10"}\t{firstRecord.Bank}");
                    fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No",-18}\t{"338"}\t{"357"}\t{"20"}\t{firstRecord.CheckNo}");
                    fileContent.AppendLine($"{"CheckDate"}\t{"Check Date",-18}\t{"359"}\t{"368"}\t{"10"}\t{firstRecord.CheckDate}");
                    fileContent.AppendLine($"{"ChartOfAccount"}\t{"Chart Of Account"}\t{"370"}\t{"469"}\t{"100"}\t{firstRecord.ChartOfAccount}");
                    fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-18}\t{"471"}\t{"488"}\t{"18"}\t{firstRecord.Debit}");
                    fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-18}\t{"490"}\t{"507"}\t{"18"}\t{firstRecord.Credit}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("DISBURSEMENT BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"CV No",-12}\t{"Payee",-100}\t{"Particulars",-200}\t{"Bank",-10}\t{"Check No",-20}\t{"Check Date",-10}\t{"Chart Of Account",-100}\t{"Debit",18}\t{"Credit",18}");

                    // Generate the records
                    foreach (var record in disbursementBooks)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.CVNo,-12}\t{record.Payee,-100}\t{record.Particulars,-200}\t{record.Bank,-10}\t{record.CheckNo,-20}\t{record.CheckDate,-10}\t{record.ChartOfAccount,-100}\t{record.Debit,18}\t{record.Credit,18}");
                    }
                    fileContent.AppendLine(new string('-', 547));
                    fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-100}\t{"",-200}\t{"",-10}\t{"",-20}\t{"",-10}\t{"TOTAL:",-100}\t{totalDebit,18}\t{totalCredit,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "DisbursementBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(DisbursementBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Disbursement Book .Txt File --

        #region -- Generate Cash Receipt Book .Txt File --

        public async Task<IActionResult> GenerateCashReceiptBookTxtFile(ViewModelBook model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var cashReceiptBooks = _unitOfWork.FilprideReport.GetCashReceiptBooks(model.DateFrom, model.DateTo, companyClaims);
                    if (cashReceiptBooks.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(CashReceiptBook));
                    }
                    var totalDebit = cashReceiptBooks.Sum(crb => crb.Debit);
                    var totalCredit = cashReceiptBooks.Sum(crb => crb.Credit);
                    var lastRecord = cashReceiptBooks.LastOrDefault();
                    var firstRecord = cashReceiptBooks.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Cash Receipt Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{cashReceiptBooks.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"RefNo",-8}\t{"Ref No.",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.RefNo}");
                    fileContent.AppendLine($"{"CustomerName"}\t{"Customer Name",-18}\t{"25"}\t{"40"}\t{"16"}\t{firstRecord.CustomerName}");
                    fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-18}\t{"42"}\t{"141"}\t{"100"}\t{firstRecord.Bank}");
                    fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No.",-18}\t{"143"}\t{"162"}\t{"20"}\t{firstRecord.CheckNo}");
                    fileContent.AppendLine($"{"COA",-8}\t{"Chart Of Account",-18}\t{"164"}\t{"263"}\t{"100"}\t{firstRecord.COA}");
                    fileContent.AppendLine($"{"Particulars"}\t{"Particulars",-18}\t{"265"}\t{"464"}\t{"200"}\t{firstRecord.Particulars}");
                    fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-18}\t{"466"}\t{"483"}\t{"18"}\t{firstRecord.Debit}");
                    fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-18}\t{"485"}\t{"502"}\t{"18"}\t{firstRecord.Credit}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("CASH RECEIPT BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"Ref No.",-12}\t{"Customer Name",-16}\t{"Bank",-100}\t{"Check No.",-20}\t{"Chart Of Account",-100}\t{"Particulars",-200}\t{"Debit",18}\t{"Credit",18}");

                    // Generate the records
                    foreach (var record in cashReceiptBooks)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.RefNo,-12}\t{record.CustomerName,-16}\t{record.Bank,-100}\t{record.CheckNo,-20}\t{record.COA,-100}\t{record.Particulars,-200}\t{record.Debit,18}\t{record.Credit,18}");
                    }
                    fileContent.AppendLine(new string('-', 539));
                    fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-16}\t{"",-100}\t{"",-20}\t{"",-100}\t{"TOTAL:",200}\t{totalDebit,18}\t{totalCredit,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "CashReceiptBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(CashReceiptBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Cash Receipt Book .Txt File --

        #region -- Generate General Ledger Book .Txt File --

        public async Task<IActionResult> GenerateGeneralLedgerBookTxtFile(ViewModelBook model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var generalBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
                    if (generalBooks.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(GeneralLedgerBook));
                    }
                    var totalDebit = generalBooks.Sum(gb => gb.Debit);
                    var totalCredit = generalBooks.Sum(gb => gb.Credit);
                    var lastRecord = generalBooks.LastOrDefault();
                    var firstRecord = generalBooks.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: General Ledger Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{generalBooks.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"Reference"}\t{"Reference"}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
                    fileContent.AppendLine($"{"Description"}\t{"Description"}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
                    fileContent.AppendLine($"{"AccountTitle"}\t{"Account Title"}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountNo + " " + firstRecord.AccountTitle}");
                    fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
                    fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("GENERAL LEDGER BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",18}\t{"Credit",18}");

                    // Generate the records
                    foreach (var record in generalBooks)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountNo + " " + record.AccountTitle,-50}\t{record.Debit,18}\t{record.Credit,18}");
                    }
                    fileContent.AppendLine(new string('-', 187));
                    fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-50}\t{"TOTAL:",50}\t{totalDebit,18}\t{totalCredit,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "GeneralLedgerBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate General Ledger Book .Txt File --

        #region -- Generate Inventory Book .Txt File --

        public async Task<IActionResult> GenerateInventoryBookTxtFileAsync(ViewModelBook model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateTo = model.DateTo;
                    var dateFrom = dateTo.AddDays(-dateTo.Day + 1);
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var inventoryBooks = _unitOfWork.FilprideReport.GetInventoryBooks(dateFrom, dateTo, companyClaims);
                    if (inventoryBooks.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(InventoryBook));
                    }
                    var totalQuantity = inventoryBooks.Sum(ib => ib.Quantity);
                    var totalAmount = inventoryBooks.Sum(ib => ib.Total);
                    var lastRecord = inventoryBooks.LastOrDefault();
                    var firstRecord = inventoryBooks.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Inventory Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{inventoryBooks.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"ProductCode",-8}\t{"Product Code",-8}\t{"12"}\t{"31"}\t{"20"}\t{firstRecord.Product.ProductCode}");
                    fileContent.AppendLine($"{"ProductName",-8}\t{"Product Name",-8}\t{"33"}\t{"82"}\t{"50"}\t{firstRecord.Product.ProductName}");
                    fileContent.AppendLine($"{"Unit",-8}\t{"Unit",-8}\t{"84"}\t{"85"}\t{"2"}\t{firstRecord.Product.ProductUnit}");
                    fileContent.AppendLine($"{"Quantity",-8}\t{"Quantity",-8}\t{"87"}\t{"104"}\t{"18"}\t{firstRecord.Quantity}");
                    fileContent.AppendLine($"{"Price",-8}\t{"Price Per Unit",-8}\t{"106"}\t{"123"}\t{"18"}\t{firstRecord.Cost}");
                    fileContent.AppendLine($"{"Amount",-8}\t{"Amount",-8}\t{"125"}\t{"142"}\t{"18"}\t{firstRecord.Total}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("INVENTORY BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"Product Code",-20}\t{"Product Name",-50}\t{"Unit",-2}\t{"Quantity",18}\t{"Price Per Unit",18}\t{"Amount",18}");

                    var totalPriceUnitAmount = 0m;
                    // Generate the records
                    foreach (var record in inventoryBooks)
                    {
                        var getLastRecordCost = record.Cost;
                        if (totalAmount != 0 && totalQuantity != 0)
                        {
                            totalPriceUnitAmount = totalAmount / totalQuantity;
                        }
                        else
                        {
                            totalPriceUnitAmount = getLastRecordCost;
                        }
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.Product.ProductCode,-20}\t{record.Product.ProductCode,-50}\t{record.Unit,-2}\t{record.Quantity,18}\t{record.Cost,18}\t{record.Total,18}");
                    }
                    fileContent.AppendLine(new string('-', 171));
                    fileContent.AppendLine($"{"",-10}\t{"",-20}\t{"",-50}\t{"TOTAL:",2}\t{totalQuantity,18}\t{totalPriceUnitAmount.ToString(SD.Four_Decimal_Format),18}\t{totalAmount,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "InventoryBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(InventoryBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Inventory Book .Txt File --

        #region -- Generate Journal Book .Txt File --

        public async Task<IActionResult> GenerateJournalBookTxtFile(ViewModelBook model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var journalBooks = _unitOfWork.FilprideReport.GetJournalBooks(model.DateFrom, model.DateTo, companyClaims);
                    if (journalBooks.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(JournalBook));
                    }
                    var lastRecord = journalBooks.LastOrDefault();
                    var firstRecord = journalBooks.FirstOrDefault();
                    var totalDebit = journalBooks.Sum(jb => jb.Debit);
                    var totalCredit = journalBooks.Sum(jb => jb.Credit);
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Journal Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{journalBooks.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"Reference",-8}\t{"Reference",-8}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
                    fileContent.AppendLine($"{"Description",-8}\t{"Description",-8}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
                    fileContent.AppendLine($"{"AccountTitle",-8}\t{"Account Title",-8}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountTitle}");
                    fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
                    fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("JOURNAL BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",18}\t{"Credit",18}");

                    // Generate the records
                    foreach (var record in journalBooks)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountTitle,-50}\t{record.Debit,18}\t{record.Credit,18}");
                    }
                    fileContent.AppendLine(new string('-', 187));
                    fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-50}\t{"TOTAL:",50}\t{totalDebit,18}\t{totalCredit,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTime().ToString("MM/dd/yyyy hh:mm tt")}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "JournalBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(JournalBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Journal Book .Txt File --

        #region -- Generate Purchase Book .Txt File --

        public async Task<IActionResult> GeneratePurchaseBookTxtFile(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    if (poListFrom != null && poListTo != null)
                    {
                        return RedirectToAction("POLiquidationPerPO", new { poListFrom = poListFrom, poListTo = poListTo });
                    }
                    else if (poListFrom == null && poListTo != null || poListFrom != null && poListTo == null)
                    {
                        TempData["error"] = "Please fill the two select list in PO Liquidation Per PO, lowest to highest";
                        return RedirectToAction(nameof(PurchaseBook));
                    }

                    if (selectedFiltering == "UnpostedRR" || selectedFiltering == "POLiquidation")
                    {
                        return RedirectToAction("GetRR", new { DateFrom = model.DateFrom, DateTo = model.DateTo, selectedFiltering });
                    }

                    var purchaseOrders = _unitOfWork.FilprideReport.GetPurchaseBooks(model.DateFrom, model.DateTo, selectedFiltering, companyClaims);
                    if (purchaseOrders.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(PurchaseBook));
                    }
                    var totalAmount = purchaseOrders.Sum(sb => sb.Amount);
                    var totalVatAmount = purchaseOrders.Sum(sb => sb.VatAmount);
                    var totalWhtAmount = purchaseOrders.Sum(sb => sb.WhtAmount);
                    var totalNetPurchases = purchaseOrders.Sum(sb => sb.NetPurchases);
                    var lastRecord = purchaseOrders.LastOrDefault();
                    var firstRecord = purchaseOrders.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                    ViewBag.SelectedFiltering = selectedFiltering;

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Purchase Journal Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{purchaseOrders.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name",-18}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"Date",-18}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                    fileContent.AppendLine($"{"SupplierName",-18}\t{"Supplier Name",-18}\t{"12"}\t{"61"}\t{"50"}\t{firstRecord.SupplierName}");
                    fileContent.AppendLine($"{"SupplierTin",-18}\t{"Supplier TIN",-18}\t{"63"}\t{"82"}\t{"20"}\t{firstRecord.SupplierTin}");
                    fileContent.AppendLine($"{"SupplierAddress",-18}\t{"Supplier Address",-18}\t{"84"}\t{"283"}\t{"200"}\t{firstRecord.SupplierAddress}");
                    fileContent.AppendLine($"{"PONo",-18}\t{"PO No.",-18}\t{"285"}\t{"296"}\t{"12"}\t{firstRecord.PONo}");
                    fileContent.AppendLine($"{"DocumentNo",-18}\t{"Document No",-18}\t{"298"}\t{"309"}\t{"12"}\t{firstRecord.DocumentNo}");
                    fileContent.AppendLine($"{"Description",-18}\t{"Description",-18}\t{"311"}\t{"360"}\t{"50"}\t{firstRecord.Description}");
                    fileContent.AppendLine($"{"Amount",-18}\t{"Amount",-18}\t{"362"}\t{"379"}\t{"18"}\t{firstRecord.Amount}");
                    fileContent.AppendLine($"{"VatAmount",-18}\t{"Vat Amount",-18}\t{"381"}\t{"398"}\t{"18"}\t{firstRecord.VatAmount}");
                    fileContent.AppendLine($"{"DefAmount",-18}\t{"Def VAT Amount",-18}\t{"400"}\t{"417"}\t{"18"}\t{0.000}");
                    fileContent.AppendLine($"{"WhtAmount",-18}\t{"WHT Amount",-18}\t{"419"}\t{"436"}\t{"18"}\t{firstRecord.WhtAmount}");
                    fileContent.AppendLine($"{"NetPurchases",-18}\t{"Net Purchases",-18}\t{"438"}\t{"455"}\t{"18"}\t{firstRecord.NetPurchases}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("PURCHASE BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Date",-10}\t{"Supplier Name",-50}\t{"Supplier TIN",-20}\t{"Supplier Address",-200}\t{"PO No.",-12}\t{"Document No",-12}\t{"Description",-50}\t{"Amount",18}\t{"Vat Amount",18}\t{"Def VAT Amount",18}\t{"WHT Amount",18}\t{"Net Purchases",18}");

                    // Generate the records
                    foreach (var record in purchaseOrders)
                    {
                        fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.SupplierName,-50}\t{record.SupplierTin,-20}\t{record.SupplierAddress,-200}\t{record.PONo,-12}\t{record.DocumentNo,-12}\t{record.Description,-50}\t{record.Amount,18}\t{record.VatAmount,18}\t{0.00m,18}\t{record.WhtAmount,18}\t{record.NetPurchases,18}");
                    }
                    fileContent.AppendLine(new string('-', 507));
                    fileContent.AppendLine($"{"",-10}\t{"",-50}\t{"",-20}\t{"",-200}\t{"",-12}\t{"",-12}\t{"TOTAL:",50}\t{totalAmount,18}\t{totalVatAmount,18}\t{0.00m,18}\t{totalWhtAmount,18}\t{totalNetPurchases,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "PurchaseBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(PurchaseBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Purchase Book .Txt File --

        #region -- Generate Sales Book .Txt File --

        public async Task<IActionResult> GenerateSalesBookTxtFile(ViewModelBook model, string? selectedDocument, string? soaList, string? siList)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = _userManager.GetUserName(User)!;
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    if (soaList != null || siList != null)
                    {
                        return RedirectToAction("TransactionReportsInSOA", new { soaList = soaList, siList = siList });
                    }

                    var salesBook = _unitOfWork.FilprideReport.GetSalesBooks(model.DateFrom, model.DateTo, selectedDocument, companyClaims);
                    if (salesBook.Count == 0)
                    {
                        TempData["error"] = "No Record Found";
                        return RedirectToAction(nameof(SalesBook));
                    }
                    var totalAmount = salesBook.Sum(sb => sb.Amount);
                    var totalVatAmount = salesBook.Sum(sb => sb.VatAmount);
                    var totalVatableSales = salesBook.Sum(sb => sb.VatableSales);
                    var totalVatExemptSales = salesBook.Sum(sb => sb.VatExemptSales);
                    var totalZeroRatedSales = salesBook.Sum(sb => sb.ZeroRated);
                    var totalDiscount = salesBook.Sum(sb => sb.Discount);
                    var totalNetSales = salesBook.Sum(sb => sb.NetSales);
                    var lastRecord = salesBook.LastOrDefault();
                    var firstRecord = salesBook.FirstOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                    ViewBag.SelectedDocument = selectedDocument;

                    var fileContent = new StringBuilder();

                    fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                    fileContent.AppendLine($"TIN: 000-216-589-00000");
                    fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"Accounting System: Accounting Administration System");
                    fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                    fileContent.AppendLine($"Date Issued:");
                    fileContent.AppendLine();
                    fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                    fileContent.AppendLine("File Name: Sales Book Report");
                    fileContent.AppendLine("File Type: Text File");
                    fileContent.AppendLine($"{"Number of Records: ",-35}{salesBook.Count}");
                    fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
                    fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                    fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                    fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Field Name",-18}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                    fileContent.AppendLine($"{"TransactionDate",-18}\t{"Tran. Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.TransactionDate}");
                    fileContent.AppendLine($"{"SerialNo",-18}\t{"Serial Number",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.SerialNo}");
                    fileContent.AppendLine($"{"Date",-18}\t{"Customer Name",-18}\t{"25"}\t{"124"}\t{"100"}\t{firstRecord.SoldTo}");
                    fileContent.AppendLine($"{"TinNo",-18}\t{"Tin#",-18}\t{"126"}\t{"145"}\t{"20"}\t{firstRecord.TinNo}");
                    fileContent.AppendLine($"{"Address",-18}\t{"Address",-18}\t{"147"}\t{"346"}\t{"200"}\t{firstRecord.Address}");
                    fileContent.AppendLine($"{"Description",-18}\t{"Description",-18}\t{"348"}\t{"397"}\t{"50"}\t{firstRecord.Description}");
                    fileContent.AppendLine($"{"Amount",-18}\t{"Amount",-18}\t{"399"}\t{"416"}\t{"18"}\t{firstRecord.Amount}");
                    fileContent.AppendLine($"{"VatAmount",-18}\t{"Vat Amount",-18}\t{"418"}\t{"435"}\t{"18"}\t{firstRecord.VatAmount}");
                    fileContent.AppendLine($"{"VatableSales",-18}\t{"Vatable Sales",-18}\t{"437"}\t{"454"}\t{"18"}\t{firstRecord.VatableSales}");
                    fileContent.AppendLine($"{"VatExemptSales",-18}\t{"Vat-Exempt Sales",-18}\t{"456"}\t{"473"}\t{"18"}\t{firstRecord.VatExemptSales}");
                    fileContent.AppendLine($"{"ZeroRated",-18}\t{"Zero-Rated Sales",-18}\t{"475"}\t{"492"}\t{"18"}\t{firstRecord.ZeroRated}");
                    fileContent.AppendLine($"{"Discount",-18}\t{"Discount",-18}\t{"494"}\t{"511"}\t{"18"}\t{firstRecord.Discount}");
                    fileContent.AppendLine($"{"NetSales",-18}\t{"Net Sales",-18}\t{"513"}\t{"530"}\t{"18"}\t{firstRecord.NetSales}");
                    fileContent.AppendLine();
                    fileContent.AppendLine("SALES BOOK");
                    fileContent.AppendLine();
                    fileContent.AppendLine($"{"Tran. Date",-10}\t{"Serial Number",-12}\t{"Customer Name",-100}\t{"Tin#",-20}\t{"Address",-200}\t{"Description",-50}\t{"Amount",18}\t{"Vat Amount",18}\t{"Vatable Sales",18}\t{"Vat-Exempt Sales",18}\t{"Zero-Rated Sales",18}\t{"Discount",18}\t{"Net Sales",18}");

                    // Generate the records
                    foreach (var record in salesBook)
                    {
                        fileContent.AppendLine($"{record.TransactionDate.ToString("MM/dd/yyyy"),-10}\t{record.SerialNo,-12}\t{record.SoldTo,-100}\t{record.TinNo,-20}\t{record.Address,-200}\t{record.Description,-50}\t{record.Amount,18}\t{record.VatAmount,18}\t{record.VatableSales,18}\t{record.VatExemptSales,18}\t{record.ZeroRated,18}\t{record.Discount,18}\t{record.NetSales,18}");
                    }
                    fileContent.AppendLine(new string('-', 587));
                    fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-100}\t{"",-20}\t{"",-200}\t{"TOTAL:",50}\t{totalAmount,18}\t{totalVatAmount,18}\t{totalVatableSales,18}\t{totalVatExemptSales,18}\t{totalZeroRatedSales,18}\t{totalDiscount,18}\t{totalNetSales,18}");

                    fileContent.AppendLine();
                    fileContent.AppendLine($"Software Name: {CS.AAS}");
                    fileContent.AppendLine($"Version: {CS.Version}");
                    fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                    fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                    // Convert the content to a byte array
                    var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                    // Return the file to the user
                    return File(bytes, "text/plain", "SalesBookReport.txt");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(SalesBook));
                }
            }
            return View(model);
        }

        #endregion -- Generate Sales Book .Txt File --

        //Generate as Excel file

        #region -- Generate DisbursmentBook Excel File --

        public async Task<IActionResult> GenerateDisbursementBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            var disbursementBooks = _unitOfWork.FilprideReport.GetDisbursementBooks(model.DateFrom, model.DateTo, companyClaims);
            if (disbursementBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(DisbursementBook));
            }
            var totalDebit = disbursementBooks.Sum(crb => crb.Debit);
            var totalCredit = disbursementBooks.Sum(crb => crb.Credit);

            // Create the Excel package
            using var package = new ExcelPackage();

            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("DisbursmentBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "DISBURSEMENT BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "CV No";
            worksheet.Cells["C7"].Value = "Payee";
            worksheet.Cells["D7"].Value = "Particulars";
            worksheet.Cells["E7"].Value = "Bank";
            worksheet.Cells["F7"].Value = "Check No";
            worksheet.Cells["G7"].Value = "Check Date";
            worksheet.Cells["H7"].Value = "Chart Of Account";
            worksheet.Cells["I7"].Value = "Debit";
            worksheet.Cells["J7"].Value = "Credit";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:J7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var cv in disbursementBooks)
            {
                worksheet.Cells[row, 1].Value = cv.Date;
                worksheet.Cells[row, 2].Value = cv.CVNo;
                worksheet.Cells[row, 3].Value = cv.Payee;
                worksheet.Cells[row, 4].Value = cv.Particulars;
                worksheet.Cells[row, 5].Value = cv.Bank;
                worksheet.Cells[row, 6].Value = cv.CheckNo;
                worksheet.Cells[row, 7].Value = cv.CheckDate;
                worksheet.Cells[row, 8].Value = cv.ChartOfAccount;

                worksheet.Cells[row, 9].Value = cv.Debit;
                worksheet.Cells[row, 10].Value = cv.Credit;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 8].Value = "Total ";
            worksheet.Cells[row, 9].Value = totalDebit;
            worksheet.Cells[row, 10].Value = totalCredit;

            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 8, row, 10])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DisbursementBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate DisbursmentBook Excel File --

        #region -- Generate CashReceiptBook Excel File --

        public async Task<IActionResult> GenerateCashReceiptBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            var cashReceiptBooks = _unitOfWork.FilprideReport.GetCashReceiptBooks(model.DateFrom, model.DateTo, companyClaims);
            if (cashReceiptBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(CashReceiptBook));
            }
            var totalDebit = cashReceiptBooks.Sum(crb => crb.Debit);
            var totalCredit = cashReceiptBooks.Sum(crb => crb.Credit);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("CashReceiptBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "CASH RECEIPT BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Ref No";
            worksheet.Cells["C7"].Value = "Customer Name";
            worksheet.Cells["D7"].Value = "Bank";
            worksheet.Cells["E7"].Value = "Check No";
            worksheet.Cells["F7"].Value = "Chart Of Account";
            worksheet.Cells["G7"].Value = "Particulars";
            worksheet.Cells["H7"].Value = "Debit";
            worksheet.Cells["I7"].Value = "Credit";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:I7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var cashReceipt in cashReceiptBooks)
            {
                worksheet.Cells[row, 1].Value = cashReceipt.Date;
                worksheet.Cells[row, 2].Value = cashReceipt.RefNo;
                worksheet.Cells[row, 3].Value = cashReceipt.CustomerName;
                worksheet.Cells[row, 4].Value = cashReceipt.Bank;
                worksheet.Cells[row, 5].Value = cashReceipt.CheckNo;
                worksheet.Cells[row, 6].Value = cashReceipt.COA;
                worksheet.Cells[row, 7].Value = cashReceipt.Particulars;

                worksheet.Cells[row, 8].Value = cashReceipt.Debit;
                worksheet.Cells[row, 9].Value = cashReceipt.Credit;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 7].Value = "Total ";
            worksheet.Cells[row, 8].Value = totalDebit;
            worksheet.Cells[row, 9].Value = totalCredit;

            worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 7, row, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CashReceiptBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate CashReceiptBook Excel File --

        #region -- Generate GeneralLedgerBook Excel File --

        public async Task<IActionResult> GenerateGeneralLedgerBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            var generalBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
            if (generalBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(GeneralLedgerBook));
            }
            var totalDebit = generalBooks.Sum(gb => gb.Debit);
            var totalCredit = generalBooks.Sum(gb => gb.Credit);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("GeneralLedgerBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "GENERAL LEDGER BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Reference";
            worksheet.Cells["C7"].Value = "Description";
            worksheet.Cells["D7"].Value = "Account Title";
            worksheet.Cells["E7"].Value = "Debit";
            worksheet.Cells["F7"].Value = "Credit";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:F7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var gl in generalBooks)
            {
                worksheet.Cells[row, 1].Value = gl.Date;
                worksheet.Cells[row, 2].Value = gl.Reference;
                worksheet.Cells[row, 3].Value = gl.Description;
                worksheet.Cells[row, 4].Value = $"{gl.AccountNo} {gl.AccountTitle}";

                worksheet.Cells[row, 5].Value = gl.Debit;
                worksheet.Cells[row, 6].Value = gl.Credit;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 4].Value = "Total ";
            worksheet.Cells[row, 5].Value = totalDebit;
            worksheet.Cells[row, 6].Value = totalCredit;

            worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 4, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GeneralLedgerBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate GeneralLedgerBook Excel File --

        #region -- Generate InventoryBook Excel File --

        public async Task<IActionResult> GenerateInventoryBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateTo = model.DateTo;
            var dateFrom = dateTo.AddDays(-dateTo.Day + 1);
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            var inventoryBooks = _unitOfWork.FilprideReport.GetInventoryBooks(dateFrom, dateTo, companyClaims);
            if (inventoryBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(InventoryBook));
            }
            var totalQuantity = inventoryBooks.Sum(ib => ib.Quantity);
            var totalAmount = inventoryBooks.Sum(ib => ib.Total);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("InventoryBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "INVENTORY BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Product Code";
            worksheet.Cells["C7"].Value = "Product Name";
            worksheet.Cells["D7"].Value = "Product Unit";
            worksheet.Cells["E7"].Value = "Quantity";
            worksheet.Cells["F7"].Value = "Price Per Unit";
            worksheet.Cells["G7"].Value = "Total";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:G7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";
            var totalPriceUnitAmount = 0m;

            foreach (var inventory in inventoryBooks)
            {
                var getLastRecordCost = inventory.Cost;
                if (totalAmount != 0 && totalQuantity != 0)
                {
                    totalPriceUnitAmount = totalAmount / totalQuantity;
                }
                else
                {
                    totalPriceUnitAmount = getLastRecordCost;
                }

                worksheet.Cells[row, 1].Value = inventory.Date;
                worksheet.Cells[row, 2].Value = inventory.Product.ProductCode;
                worksheet.Cells[row, 3].Value = inventory.Product.ProductName;
                worksheet.Cells[row, 4].Value = inventory.Product.ProductUnit;

                worksheet.Cells[row, 5].Value = inventory.Quantity;
                worksheet.Cells[row, 6].Value = inventory.Cost;
                worksheet.Cells[row, 7].Value = inventory.Total;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 4].Value = "Total ";
            worksheet.Cells[row, 5].Value = totalQuantity;
            worksheet.Cells[row, 6].Value = totalPriceUnitAmount;
            worksheet.Cells[row, 7].Value = totalAmount;

            worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 4, row, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"InventoryBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate InventoryBook Excel File --

        #region -- Generate JournalBook Excel File --

        public async Task<IActionResult> GenerateJournalBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            var journalBooks = _unitOfWork.FilprideReport.GetJournalBooks(model.DateFrom, model.DateTo, companyClaims);
            if (journalBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(JournalBook));
            }
            var totalDebit = journalBooks.Sum(jb => jb.Debit);
            var totalCredit = journalBooks.Sum(jb => jb.Credit);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("JournalBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "JOURNAL BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Reference";
            worksheet.Cells["C7"].Value = "Description";
            worksheet.Cells["D7"].Value = "Account Title";
            worksheet.Cells["E7"].Value = "Debit";
            worksheet.Cells["F7"].Value = "Credit";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:F7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var jv in journalBooks)
            {
                worksheet.Cells[row, 1].Value = jv.Date;
                worksheet.Cells[row, 2].Value = jv.Reference;
                worksheet.Cells[row, 3].Value = jv.Description;
                worksheet.Cells[row, 4].Value = jv.AccountTitle;

                worksheet.Cells[row, 5].Value = jv.Debit;
                worksheet.Cells[row, 6].Value = jv.Credit;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 4].Value = "Total ";
            worksheet.Cells[row, 5].Value = totalDebit;
            worksheet.Cells[row, 6].Value = totalCredit;

            worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 4, row, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JournalBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate JournalBook Excel File --

        #region -- Generate PurchaseBook Excel File --

        public async Task<IActionResult> GeneratePurchaseBookExcelFile(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (poListFrom != null || poListTo != null || selectedFiltering == "UnpostedRR" || selectedFiltering == "POLiquidation" || selectedFiltering == "RRDate" || selectedFiltering == "DueDate")
            {
                TempData["error"] = "Please input date from and date to only!";
                return RedirectToAction(nameof(PurchaseBook));
            }

            var purchaseBooks = _unitOfWork.FilprideReport.GetPurchaseBooks(model.DateFrom, model.DateTo, selectedFiltering, companyClaims);
            if (purchaseBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(PurchaseBook));
            }
            var totalAmount = purchaseBooks.Sum(sb => sb.Amount);
            var totalVatAmount = purchaseBooks.Sum(sb => sb.VatAmount);
            var totalWhtAmount = purchaseBooks.Sum(sb => sb.WhtAmount);
            var totalNetPurchases = purchaseBooks.Sum(sb => sb.NetPurchases);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("PurchaseBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "PURCHASE BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Supplier Name";
            worksheet.Cells["C7"].Value = "Supplier Tin";
            worksheet.Cells["D7"].Value = "Supplier Address";
            worksheet.Cells["E7"].Value = "PO No";
            worksheet.Cells["F7"].Value = "Document No";
            worksheet.Cells["G7"].Value = "Description";
            worksheet.Cells["H7"].Value = "Amount";
            worksheet.Cells["I7"].Value = "Vat Amount";
            worksheet.Cells["J7"].Value = "Def VAT Amount";
            worksheet.Cells["K7"].Value = "WHT Amount";
            worksheet.Cells["L7"].Value = "Net Purchases";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:L7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var pb in purchaseBooks)
            {
                worksheet.Cells[row, 1].Value = pb.Date;
                worksheet.Cells[row, 2].Value = pb.SupplierName;
                worksheet.Cells[row, 3].Value = pb.SupplierTin;
                worksheet.Cells[row, 4].Value = pb.SupplierAddress;
                worksheet.Cells[row, 5].Value = pb.PONo;
                worksheet.Cells[row, 6].Value = pb.DocumentNo;
                worksheet.Cells[row, 7].Value = pb.Description;
                worksheet.Cells[row, 8].Value = pb.Amount;
                worksheet.Cells[row, 9].Value = pb.VatAmount;

                worksheet.Cells[row, 11].Value = pb.WhtAmount;
                worksheet.Cells[row, 12].Value = pb.NetPurchases;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 7].Value = "Total ";
            worksheet.Cells[row, 8].Value = totalAmount;
            worksheet.Cells[row, 9].Value = totalVatAmount;

            worksheet.Cells[row, 11].Value = totalWhtAmount;
            worksheet.Cells[row, 12].Value = totalNetPurchases;

            worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 7, row, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PurchaseBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate PurchaseBook Excel File --

        #region -- Generate SalesBook Excel File --

        public async Task<IActionResult> GenerateSalesBookExcelFile(ViewModelBook model, string? selectedDocument, string? soaList, string? siList, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(User)!;
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (soaList != null || siList != null || selectedDocument != null)
            {
                TempData["error"] = "Please input date from and date to only!";
                return RedirectToAction(nameof(SalesBook));
            }

            var salesBook = _unitOfWork.FilprideReport.GetSalesBooks(model.DateFrom, model.DateTo, selectedDocument, companyClaims);
            if (salesBook.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(SalesBook));
            }
            var totalAmount = salesBook.Sum(sb => sb.Amount);
            var totalVatAmount = salesBook.Sum(sb => sb.VatAmount);
            var totalVatableSales = salesBook.Sum(sb => sb.VatableSales);
            var totalVatExemptSales = salesBook.Sum(sb => sb.VatExemptSales);
            var totalZeroRatedSales = salesBook.Sum(sb => sb.ZeroRated);
            var totalDiscount = salesBook.Sum(sb => sb.Discount);
            var totalNetSales = salesBook.Sum(sb => sb.NetSales);

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("SalesBook");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "SALES BOOK";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Extracted By:";
            worksheet.Cells["A4"].Value = "Company:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{extractedBy}";
            worksheet.Cells["B4"].Value = $"{companyClaims}";

            worksheet.Cells["A7"].Value = "Tran. Date";
            worksheet.Cells["B7"].Value = "Serial Number";
            worksheet.Cells["C7"].Value = "Customer Name";
            worksheet.Cells["D7"].Value = "Tin#";
            worksheet.Cells["E7"].Value = "Address";
            worksheet.Cells["F7"].Value = "Description";
            worksheet.Cells["G7"].Value = "Amount";
            worksheet.Cells["H7"].Value = "Vat Amount";
            worksheet.Cells["I7"].Value = "Vatable Sales";
            worksheet.Cells["J7"].Value = "Vat-Exempt Sales";
            worksheet.Cells["K7"].Value = "Zero-Rated Sales";
            worksheet.Cells["L7"].Value = "Discount";
            worksheet.Cells["M7"].Value = "Net Sales";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:M7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Populate the data rows
            int row = 8;
            string currencyFormat = "#,##0.0000";

            foreach (var cv in salesBook)
            {
                worksheet.Cells[row, 1].Value = cv.TransactionDate;
                worksheet.Cells[row, 2].Value = cv.SerialNo;
                worksheet.Cells[row, 3].Value = cv.SoldTo;
                worksheet.Cells[row, 4].Value = cv.TinNo;
                worksheet.Cells[row, 5].Value = cv.Address;
                worksheet.Cells[row, 6].Value = cv.Description;
                worksheet.Cells[row, 7].Value = cv.Amount;
                worksheet.Cells[row, 8].Value = cv.VatAmount;
                worksheet.Cells[row, 9].Value = cv.VatableSales;
                worksheet.Cells[row, 10].Value = cv.VatExemptSales;
                worksheet.Cells[row, 11].Value = cv.ZeroRated;
                worksheet.Cells[row, 12].Value = cv.Discount;
                worksheet.Cells[row, 13].Value = cv.NetSales;

                worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;

                row++;
            }

            worksheet.Cells[row, 6].Value = "Total ";
            worksheet.Cells[row, 7].Value = totalAmount;
            worksheet.Cells[row, 8].Value = totalVatAmount;
            worksheet.Cells[row, 9].Value = totalVatableSales;
            worksheet.Cells[row, 10].Value = totalVatExemptSales;
            worksheet.Cells[row, 11].Value = totalZeroRatedSales;
            worksheet.Cells[row, 12].Value = totalDiscount;
            worksheet.Cells[row, 13].Value = totalNetSales;

            worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 13])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 6, row, 13])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SalesBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion -- Generate SalesBook Excel File --

        //Reports

        #region -- Generate Sales Report Excel File --

        public async Task<IActionResult> GenerateSalesReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(SalesReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                if (dateTo.Month <= 9 && dateTo.Year == 2024)
                {
                    return RedirectToAction(nameof(GenerateSalesInvoiceReportExcelFile), new { dateFrom = model.DateFrom, dateTo = model.DateTo, cancellationToken });
                }

                var salesReport = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims, model.Commissionee, cancellationToken);
                if (salesReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(SalesReport));
                }
                var totalQuantity = salesReport.Sum(s => s.DeliveryReceipt.Quantity);
                var totalAmount = salesReport.Sum(s => s.DeliveryReceipt.TotalAmount);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("SalesReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SALES REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Date Delivered";
                worksheet.Cells["B7"].Value = "Customer Name";
                worksheet.Cells["C7"].Value = "Segment";
                worksheet.Cells["D7"].Value = "Specialist";
                worksheet.Cells["E7"].Value = "SI No.";
                worksheet.Cells["F7"].Value = "COS #";
                worksheet.Cells["G7"].Value = "OTC COS #";
                worksheet.Cells["H7"].Value = "DR #";
                worksheet.Cells["I7"].Value = "OTC DR #";
                worksheet.Cells["J7"].Value = "PO #";
                worksheet.Cells["K7"].Value = "IS PO #";
                worksheet.Cells["L7"].Value = "Delivery Option";
                worksheet.Cells["M7"].Value = "Items";
                worksheet.Cells["N7"].Value = "Quantity";
                worksheet.Cells["O7"].Value = "Freight";
                worksheet.Cells["P7"].Value = "Sales G. VAT";
                worksheet.Cells["Q7"].Value = "VAT";
                worksheet.Cells["R7"].Value = "Sales N. VAT";
                worksheet.Cells["S7"].Value = "Freight N. VAT";
                worksheet.Cells["T7"].Value = "Commission";
                worksheet.Cells["U7"].Value = "Commissionee";
                worksheet.Cells["V7"].Value = "Remarks";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:V7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";

                var totalFreightAmount = 0m;
                var totalSalesNetOfVat = 0m;
                var totalFreightNetOfVat = 0m;
                var totalCommissionRate = 0m;
                var totalVat = 0m;
                foreach (var dr in salesReport)
                {
                    var quantity = dr.DeliveryReceipt.Quantity;
                    var freightAmount = (dr.DeliveryReceipt?.Freight ?? 0m) * quantity;
                    var segment = dr.DeliveryReceipt!.TotalAmount;
                    var salesNetOfVat = segment != 0 ? segment / 1.12m : 0;
                    var vat = salesNetOfVat * .12m;
                    var freightNetOfVat = freightAmount / 1.12m;

                    worksheet.Cells[row, 1].Value = dr.DeliveryReceipt.DeliveredDate;
                    worksheet.Cells[row, 2].Value = dr.DeliveryReceipt.Customer?.CustomerName;
                    worksheet.Cells[row, 3].Value = dr.DeliveryReceipt.Customer?.CustomerType;
                    worksheet.Cells[row, 4].Value = dr.DeliveryReceipt.CustomerOrderSlip?.AccountSpecialist;
                    worksheet.Cells[row, 5].Value = dr.SalesInvoiceNo;
                    worksheet.Cells[row, 6].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[row, 7].Value = dr.DeliveryReceipt.CustomerOrderSlip?.OldCosNo;
                    worksheet.Cells[row, 8].Value = dr.DeliveryReceipt?.DeliveryReceiptNo;
                    worksheet.Cells[row, 9].Value = dr.DeliveryReceipt?.ManualDrNo;
                    worksheet.Cells[row, 10].Value = dr.DeliveryReceipt?.PurchaseOrder?.PurchaseOrderNo;
                    worksheet.Cells[row, 11].Value = dr.DeliveryReceipt?.PurchaseOrder?.OldPoNo;
                    worksheet.Cells[row, 12].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.DeliveryOption;
                    worksheet.Cells[row, 13].Value = dr.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName;
                    worksheet.Cells[row, 14].Value = dr.DeliveryReceipt.Quantity;
                    worksheet.Cells[row, 15].Value = freightAmount;
                    worksheet.Cells[row, 16].Value = segment;
                    worksheet.Cells[row, 17].Value = vat;
                    worksheet.Cells[row, 18].Value = salesNetOfVat;
                    worksheet.Cells[row, 19].Value = freightNetOfVat;
                    worksheet.Cells[row, 20].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CommissionRate;
                    worksheet.Cells[row, 21].Value = dr.DeliveryReceipt.CustomerOrderSlip?.Commissionee?.SupplierName;
                    worksheet.Cells[row, 22].Value = dr.DeliveryReceipt.Remarks;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                    row++;

                    totalFreightAmount += freightAmount;
                    totalVat += vat;
                    totalSalesNetOfVat += salesNetOfVat;
                    totalFreightNetOfVat += freightNetOfVat;
                    totalCommissionRate += dr.DeliveryReceipt.CustomerOrderSlip?.CommissionRate ?? 0m;
                }

                worksheet.Cells[row, 13].Value = "Total ";
                worksheet.Cells[row, 14].Value = totalQuantity;
                worksheet.Cells[row, 15].Value = totalFreightAmount;
                worksheet.Cells[row, 16].Value = totalAmount;
                worksheet.Cells[row, 17].Value = totalVat;
                worksheet.Cells[row, 18].Value = totalSalesNetOfVat;
                worksheet.Cells[row, 19].Value = totalFreightNetOfVat;
                worksheet.Cells[row, 20].Value = totalCommissionRate;

                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 22])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 13, row, 20])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var rowForSummary = row + 8;

                // Set the column headers
                var mergedCellForOverall = worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5];
                mergedCellForOverall.Merge = true;
                mergedCellForOverall.Value = "Overall";
                mergedCellForOverall.Style.Font.Size = 13;
                mergedCellForOverall.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var textStyleForSummary = worksheet.Cells[rowForSummary - 3, 2];
                textStyleForSummary.Style.Font.Size = 16;
                textStyleForSummary.Style.Font.Bold = true;

                worksheet.Cells[rowForSummary - 3, 2].Value = "Summary";
                worksheet.Cells[rowForSummary - 1, 2].Value = "Segment";
                worksheet.Cells[rowForSummary - 1, 3].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 4].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 5].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Overall
                using (var range = worksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Overall
                using (var range = worksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForBiodiesel = worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9];
                mergedCellForBiodiesel.Merge = true;
                mergedCellForBiodiesel.Value = "Biodiesel";
                mergedCellForBiodiesel.Style.Font.Size = 13;
                mergedCellForBiodiesel.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[rowForSummary - 1, 7].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 8].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 9].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 7, rowForSummary - 1, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Biodiesel
                using (var range = worksheet.Cells[rowForSummary - 1, 7, rowForSummary - 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Biodiesel
                using (var range = worksheet.Cells[rowForSummary + 4, 7, rowForSummary + 4, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 7, rowForSummary + 4, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEconogas = worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13];
                mergedCellForEconogas.Merge = true;
                mergedCellForEconogas.Value = "Econogas";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[rowForSummary - 1, 11].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 12].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 13].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 11, rowForSummary - 1, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Econogas
                using (var range = worksheet.Cells[rowForSummary - 1, 11, rowForSummary - 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Econogas
                using (var range = worksheet.Cells[rowForSummary + 4, 11, rowForSummary + 4, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 11, rowForSummary + 4, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEnvirogas = worksheet.Cells[rowForSummary - 2, 15, rowForSummary - 2, 17];
                mergedCellForEnvirogas.Merge = true;
                mergedCellForEnvirogas.Value = "Envirogas";
                mergedCellForEnvirogas.Style.Font.Size = 13;
                mergedCellForEnvirogas.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 15, rowForSummary - 2, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                //inset data/value in excel
                worksheet.Cells[rowForSummary - 1, 15].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 16].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 17].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 15, rowForSummary - 1, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Envirogas
                using (var range = worksheet.Cells[rowForSummary - 1, 15, rowForSummary - 1, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Envirogas
                using (var range = worksheet.Cells[rowForSummary + 4, 15, rowForSummary + 4, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 15, rowForSummary + 4, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var listForBiodiesel = new List<SalesReportViewModel>();
                var listForEconogas = new List<SalesReportViewModel>();
                var listForEnvirogas = new List<SalesReportViewModel>();

                var totalOverallQuantity = 0m;
                var totalOverallAmount = 0m;

                var totalQuantityForBiodiesel = 0m;
                var totalAmountForBiodiesel = 0m;

                var totalQuantityForEconogas = 0m;
                var totalAmountForEconogas = 0m;

                var totalQuantityForEnvirogas = 0m;
                var totalAmountForEnvirogas = 0m;

                foreach (var customerType in Enum.GetValues<CustomerType>())
                {
                    var list = salesReport.Where(s => s.DeliveryReceipt.Customer?.CustomerType == customerType.ToString()).ToList();
                    listForBiodiesel = list.Where(s => s.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL").ToList();
                    listForEconogas = list.Where(s => s.DeliveryReceipt.PurchaseOrder!.Product?.ProductName == "ECONOGAS").ToList();
                    listForEnvirogas = list.Where(s => s.DeliveryReceipt.PurchaseOrder!.Product?.ProductName == "ENVIROGAS").ToList();

                    // Computation for Overall
                    var overAllQuantitySum = list.Sum(s => s.DeliveryReceipt.Quantity);
                    var overallAmountSum = list.Sum(s => s.DeliveryReceipt.TotalAmount);
                    var overallNetOfAmountSum = overallAmountSum != 0m ? overallAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 2].Value = customerType.ToString();
                    worksheet.Cells[rowForSummary, 3].Value = overAllQuantitySum;
                    worksheet.Cells[rowForSummary, 4].Value = overallNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 5].Value = overallNetOfAmountSum != 0m || overAllQuantitySum != 0m ? overallNetOfAmountSum / overAllQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                    // Computation for Biodiesel
                    var biodieselQuantitySum = listForBiodiesel.Sum(s => s.DeliveryReceipt.Quantity);
                    var biodieselAmountSum = listForBiodiesel.Sum(s => s.DeliveryReceipt.TotalAmount);
                    var biodieselNetOfAmountSum = biodieselAmountSum != 0m ? biodieselAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 7].Value = biodieselQuantitySum;
                    worksheet.Cells[rowForSummary, 8].Value = biodieselNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 9].Value = biodieselNetOfAmountSum != 0m || biodieselQuantitySum != 0m ? biodieselNetOfAmountSum / biodieselQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

                    // Computation for Econogas
                    var econogasQuantitySum = listForEconogas.Sum(s => s.DeliveryReceipt.Quantity);
                    var econogasAmountSum = listForEconogas.Sum(s => s.DeliveryReceipt.TotalAmount);
                    var econogasNetOfAmountSum = econogasAmountSum != 0m ? econogasAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 11].Value = econogasQuantitySum;
                    worksheet.Cells[rowForSummary, 12].Value = econogasNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 13].Value = econogasNetOfAmountSum != 0m || econogasQuantitySum != 0m ? econogasNetOfAmountSum / econogasQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

                    // Computation for Envirogas
                    var envirogasQuantitySum = listForEnvirogas.Sum(s => s.DeliveryReceipt.Quantity);
                    var envirogasAmountSum = listForEnvirogas.Sum(s => s.DeliveryReceipt.TotalAmount);
                    var envirogasNetOfAmountSum = envirogasAmountSum != 0m ? envirogasAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 15].Value = envirogasQuantitySum;
                    worksheet.Cells[rowForSummary, 16].Value = envirogasNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 17].Value = envirogasNetOfAmountSum != 0m || envirogasQuantitySum != 0m ? envirogasNetOfAmountSum / envirogasQuantitySum : 0;

                    worksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormat;

                    rowForSummary++;

                    // Computation of total for Overall
                    totalOverallQuantity += overAllQuantitySum;
                    totalOverallAmount += overallNetOfAmountSum;
                    // Computation of total for Biodiesel
                    totalQuantityForBiodiesel += biodieselQuantitySum;
                    totalAmountForBiodiesel += biodieselNetOfAmountSum;
                    // Computation of total for Econogas
                    totalQuantityForEconogas += econogasQuantitySum;
                    totalAmountForEconogas += econogasNetOfAmountSum;
                    // Computation of total for Envirogas
                    totalQuantityForEnvirogas += envirogasQuantitySum;
                    totalAmountForEnvirogas += envirogasNetOfAmountSum;
                }

                var styleOfTotal = worksheet.Cells[rowForSummary, 2];
                styleOfTotal.Value = "Total";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;

                worksheet.Cells[rowForSummary, 3].Value = totalOverallQuantity;
                worksheet.Cells[rowForSummary, 4].Value = totalOverallAmount;
                worksheet.Cells[rowForSummary, 5].Value = totalOverallAmount != 0m || totalOverallQuantity != 0m ? totalOverallAmount / totalOverallQuantity : 0;

                worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 7].Value = totalQuantityForBiodiesel;
                worksheet.Cells[rowForSummary, 8].Value = totalAmountForBiodiesel;
                worksheet.Cells[rowForSummary, 9].Value = totalAmountForBiodiesel != 0m || totalQuantityForBiodiesel != 0m ? totalAmountForBiodiesel / totalQuantityForBiodiesel : 0;

                worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 11].Value = totalQuantityForEconogas;
                worksheet.Cells[rowForSummary, 12].Value = totalAmountForEconogas;
                worksheet.Cells[rowForSummary, 13].Value = totalAmountForEconogas != 0m || totalQuantityForEconogas != 0m ? totalAmountForEconogas / totalQuantityForEconogas : 0;

                worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 15].Value = totalQuantityForEnvirogas;
                worksheet.Cells[rowForSummary, 16].Value = totalAmountForEnvirogas;
                worksheet.Cells[rowForSummary, 17].Value = totalAmountForEnvirogas != 0m || totalQuantityForEnvirogas != 0m ? totalAmountForEnvirogas / totalQuantityForEnvirogas : 0;

                worksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormat;

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 3);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SalesReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(SalesReport));
            }
        }

        public async Task<IActionResult> GenerateSalesInvoiceReportExcelFile(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(SalesReport));
            }

            try
            {
                var extractedBy = _userManager.GetUserName(this.User);
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var salesReport = await _unitOfWork.FilprideReport.GetSalesInvoiceReport(dateFrom, dateTo, companyClaims, cancellationToken);
                if (salesReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(SalesReport));
                }
                var totalQuantity = salesReport.Sum(s => s.Quantity);
                var totalAmount = salesReport.Sum(s => s.Amount);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("SalesReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SALES REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Date Delivered";
                worksheet.Cells["B7"].Value = "Customer Name";
                worksheet.Cells["C7"].Value = "Segment";
                worksheet.Cells["D7"].Value = "Specialist";
                worksheet.Cells["E7"].Value = "SI No.";
                worksheet.Cells["F7"].Value = "COS #";
                worksheet.Cells["G7"].Value = "OTC COS #";
                worksheet.Cells["H7"].Value = "DR #";
                worksheet.Cells["I7"].Value = "OTC DR #";
                worksheet.Cells["J7"].Value = "PO #";
                worksheet.Cells["K7"].Value = "IS PO #";
                worksheet.Cells["L7"].Value = "Delivery Option";
                worksheet.Cells["M7"].Value = "Items";
                worksheet.Cells["N7"].Value = "Quantity";
                worksheet.Cells["O7"].Value = "Freight";
                worksheet.Cells["P7"].Value = "Sales G. VAT";
                worksheet.Cells["Q7"].Value = "VAT";
                worksheet.Cells["R7"].Value = "Sales N. VAT";
                worksheet.Cells["S7"].Value = "Freight N. VAT";
                worksheet.Cells["T7"].Value = "Commission";
                worksheet.Cells["U7"].Value = "Commissionee";
                worksheet.Cells["V7"].Value = "Remarks";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:V7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";

                var totalFreightAmount = 0m;
                var totalSalesNetOfVat = 0m;
                var totalFreightNetOfVat = 0m;
                var totalCommissionRate = 0m;
                var totalVat = 0m;
                foreach (var dr in salesReport)
                {
                    var quantity = dr.Quantity;
                    var freightAmount = (dr.DeliveryReceipt?.Freight ?? 0m) * quantity;
                    var segment = dr.Amount;
                    var salesNetOfVat = segment != 0 ? segment / 1.12m : 0;
                    var vat = salesNetOfVat * .12m;
                    var freightNetOfVat = freightAmount / 1.12m;

                    worksheet.Cells[row, 1].Value = dr.TransactionDate;
                    worksheet.Cells[row, 2].Value = dr.Customer?.CustomerName;
                    worksheet.Cells[row, 3].Value = dr.Customer?.CustomerType;
                    worksheet.Cells[row, 4].Value = dr.CustomerOrderSlip?.AccountSpecialist;
                    worksheet.Cells[row, 5].Value = dr.SalesInvoiceNo;
                    worksheet.Cells[row, 6].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[row, 7].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo;
                    worksheet.Cells[row, 8].Value = dr.DeliveryReceipt?.DeliveryReceiptNo;
                    worksheet.Cells[row, 9].Value = dr.DeliveryReceipt?.ManualDrNo;
                    worksheet.Cells[row, 10].Value = dr.PurchaseOrder?.PurchaseOrderNo;
                    worksheet.Cells[row, 11].Value = dr.PurchaseOrder?.OldPoNo;
                    worksheet.Cells[row, 12].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.DeliveryOption;
                    worksheet.Cells[row, 13].Value = dr.Product!.ProductName;
                    worksheet.Cells[row, 14].Value = quantity;
                    worksheet.Cells[row, 15].Value = freightAmount;
                    worksheet.Cells[row, 16].Value = segment;
                    worksheet.Cells[row, 17].Value = vat;
                    worksheet.Cells[row, 18].Value = salesNetOfVat;
                    worksheet.Cells[row, 19].Value = freightNetOfVat;
                    worksheet.Cells[row, 20].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate;
                    worksheet.Cells[row, 21].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.Commissionee?.SupplierName;
                    worksheet.Cells[row, 22].Value = dr.Remarks;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                    row++;

                    totalFreightAmount += freightAmount;
                    totalVat += vat;
                    totalSalesNetOfVat += salesNetOfVat;
                    totalFreightNetOfVat += freightNetOfVat;
                    totalCommissionRate += dr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m;
                }

                worksheet.Cells[row, 13].Value = "Total ";
                worksheet.Cells[row, 14].Value = totalQuantity;
                worksheet.Cells[row, 15].Value = totalFreightAmount;
                worksheet.Cells[row, 16].Value = totalAmount;
                worksheet.Cells[row, 17].Value = totalVat;
                worksheet.Cells[row, 18].Value = totalSalesNetOfVat;
                worksheet.Cells[row, 19].Value = totalFreightNetOfVat;
                worksheet.Cells[row, 20].Value = totalCommissionRate;

                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 22])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 13, row, 20])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var rowForSummary = row + 8;

                // Set the column headers
                var mergedCellForOverall = worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5];
                mergedCellForOverall.Merge = true;
                mergedCellForOverall.Value = "Overall";
                mergedCellForOverall.Style.Font.Size = 13;
                mergedCellForOverall.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var textStyleForSummary = worksheet.Cells[rowForSummary - 3, 2];
                textStyleForSummary.Style.Font.Size = 16;
                textStyleForSummary.Style.Font.Bold = true;

                worksheet.Cells[rowForSummary - 3, 2].Value = "Summary";
                worksheet.Cells[rowForSummary - 1, 2].Value = "Segment";
                worksheet.Cells[rowForSummary - 1, 3].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 4].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 5].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Overall
                using (var range = worksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Overall
                using (var range = worksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForBiodiesel = worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9];
                mergedCellForBiodiesel.Merge = true;
                mergedCellForBiodiesel.Value = "Biodiesel";
                mergedCellForBiodiesel.Style.Font.Size = 13;
                mergedCellForBiodiesel.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[rowForSummary - 1, 7].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 8].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 9].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 7, rowForSummary - 1, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Biodiesel
                using (var range = worksheet.Cells[rowForSummary - 1, 7, rowForSummary - 1, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Biodiesel
                using (var range = worksheet.Cells[rowForSummary + 4, 7, rowForSummary + 4, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 7, rowForSummary + 4, 9])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEconogas = worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13];
                mergedCellForEconogas.Merge = true;
                mergedCellForEconogas.Value = "Econogas";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells[rowForSummary - 1, 11].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 12].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 13].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 11, rowForSummary - 1, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Econogas
                using (var range = worksheet.Cells[rowForSummary - 1, 11, rowForSummary - 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Econogas
                using (var range = worksheet.Cells[rowForSummary + 4, 11, rowForSummary + 4, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 11, rowForSummary + 4, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEnvirogas = worksheet.Cells[rowForSummary - 2, 15, rowForSummary - 2, 17];
                mergedCellForEnvirogas.Merge = true;
                mergedCellForEnvirogas.Value = "Envirogas";
                mergedCellForEnvirogas.Style.Font.Size = 13;
                mergedCellForEnvirogas.Style.Font.Bold = true;
                worksheet.Cells[rowForSummary - 2, 15, rowForSummary - 2, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                //inset data/value in excel
                worksheet.Cells[rowForSummary - 1, 15].Value = "Volume";
                worksheet.Cells[rowForSummary - 1, 16].Value = "Sales N. VAT";
                worksheet.Cells[rowForSummary - 1, 17].Value = "Ave. SP";

                worksheet.Cells[rowForSummary - 1, 15, rowForSummary - 1, 17].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Envirogas
                using (var range = worksheet.Cells[rowForSummary - 1, 15, rowForSummary - 1, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Envirogas
                using (var range = worksheet.Cells[rowForSummary + 4, 15, rowForSummary + 4, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = worksheet.Cells[rowForSummary + 4, 15, rowForSummary + 4, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var listForBiodiesel = new List<FilprideSalesInvoice>();
                var listForEconogas = new List<FilprideSalesInvoice>();
                var listForEnvirogas = new List<FilprideSalesInvoice>();

                var totalOverallQuantity = 0m;
                var totalOverallAmount = 0m;

                var totalQuantityForBiodiesel = 0m;
                var totalAmountForBiodiesel = 0m;

                var totalQuantityForEconogas = 0m;
                var totalAmountForEconogas = 0m;

                var totalQuantityForEnvirogas = 0m;
                var totalAmountForEnvirogas = 0m;

                foreach (var customerType in Enum.GetValues<CustomerType>())
                {
                    var list = salesReport.Where(s => s.Customer?.CustomerType == customerType.ToString()).ToList();
                    listForBiodiesel = list.Where(s => s.Product?.ProductName == "BIODIESEL").ToList();
                    listForEconogas = list.Where(s => s.Product?.ProductName == "ECONOGAS").ToList();
                    listForEnvirogas = list.Where(s => s.Product?.ProductName == "ENVIROGAS").ToList();

                    // Computation for Overall
                    var overAllQuantitySum = list.Sum(s => s.Quantity);
                    var overallAmountSum = list.Sum(s => s.Amount);
                    var overallNetOfAmountSum = overallAmountSum != 0m ? overallAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 2].Value = customerType.ToString();
                    worksheet.Cells[rowForSummary, 3].Value = overAllQuantitySum;
                    worksheet.Cells[rowForSummary, 4].Value = overallNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 5].Value = overallNetOfAmountSum != 0m || overAllQuantitySum != 0m ? overallNetOfAmountSum / overAllQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                    // Computation for Biodiesel
                    var biodieselQuantitySum = listForBiodiesel.Sum(s => s.Quantity);
                    var biodieselAmountSum = listForBiodiesel.Sum(s => s.Amount);
                    var biodieselNetOfAmountSum = biodieselAmountSum != 0m ? biodieselAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 7].Value = biodieselQuantitySum;
                    worksheet.Cells[rowForSummary, 8].Value = biodieselNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 9].Value = biodieselNetOfAmountSum != 0m || biodieselQuantitySum != 0m ? biodieselNetOfAmountSum / biodieselQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

                    // Computation for Econogas
                    var econogasQuantitySum = listForEconogas.Sum(s => s.Quantity);
                    var econogasAmountSum = listForEconogas.Sum(s => s.Amount);
                    var econogasNetOfAmountSum = econogasAmountSum != 0m ? econogasAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 11].Value = econogasQuantitySum;
                    worksheet.Cells[rowForSummary, 12].Value = econogasNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 13].Value = econogasNetOfAmountSum != 0m || econogasQuantitySum != 0m ? econogasNetOfAmountSum / econogasQuantitySum : 0m;

                    worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

                    // Computation for Envirogas
                    var envirogasQuantitySum = listForEnvirogas.Sum(s => s.Quantity);
                    var envirogasAmountSum = listForEnvirogas.Sum(s => s.Amount);
                    var envirogasNetOfAmountSum = envirogasAmountSum != 0m ? envirogasAmountSum / 1.12m : 0;

                    worksheet.Cells[rowForSummary, 15].Value = envirogasQuantitySum;
                    worksheet.Cells[rowForSummary, 16].Value = envirogasNetOfAmountSum;
                    worksheet.Cells[rowForSummary, 17].Value = envirogasNetOfAmountSum != 0m || envirogasQuantitySum != 0m ? envirogasNetOfAmountSum / envirogasQuantitySum : 0;

                    worksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormat;

                    rowForSummary++;

                    // Computation of total for Overall
                    totalOverallQuantity += overAllQuantitySum;
                    totalOverallAmount += overallNetOfAmountSum;
                    // Computation of total for Biodiesel
                    totalQuantityForBiodiesel += biodieselQuantitySum;
                    totalAmountForBiodiesel += biodieselNetOfAmountSum;
                    // Computation of total for Econogas
                    totalQuantityForEconogas += econogasQuantitySum;
                    totalAmountForEconogas += econogasNetOfAmountSum;
                    // Computation of total for Envirogas
                    totalQuantityForEnvirogas += envirogasQuantitySum;
                    totalAmountForEnvirogas += envirogasNetOfAmountSum;
                }

                var styleOfTotal = worksheet.Cells[rowForSummary, 2];
                styleOfTotal.Value = "Total";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;

                worksheet.Cells[rowForSummary, 3].Value = totalOverallQuantity;
                worksheet.Cells[rowForSummary, 4].Value = totalOverallAmount;
                worksheet.Cells[rowForSummary, 5].Value = totalOverallAmount != 0m || totalOverallQuantity != 0m ? totalOverallAmount / totalOverallQuantity : 0;

                worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 7].Value = totalQuantityForBiodiesel;
                worksheet.Cells[rowForSummary, 8].Value = totalAmountForBiodiesel;
                worksheet.Cells[rowForSummary, 9].Value = totalAmountForBiodiesel != 0m || totalQuantityForBiodiesel != 0m ? totalAmountForBiodiesel / totalQuantityForBiodiesel : 0;

                worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 11].Value = totalQuantityForEconogas;
                worksheet.Cells[rowForSummary, 12].Value = totalAmountForEconogas;
                worksheet.Cells[rowForSummary, 13].Value = totalAmountForEconogas != 0m || totalQuantityForEconogas != 0m ? totalAmountForEconogas / totalQuantityForEconogas : 0;

                worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells[rowForSummary, 15].Value = totalQuantityForEnvirogas;
                worksheet.Cells[rowForSummary, 16].Value = totalAmountForEnvirogas;
                worksheet.Cells[rowForSummary, 17].Value = totalAmountForEnvirogas != 0m || totalQuantityForEnvirogas != 0m ? totalAmountForEnvirogas / totalQuantityForEnvirogas : 0;

                worksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormat;

                worksheet.View.FreezePanes(8, 3);

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SalesReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(SalesReport));
            }
        }

        #endregion

        #region -- Generate Purchase Order Report Excel File --

        public async Task<IActionResult> GeneratePurchaseOrderReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(PurchaseOrderReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var purchaseOrderReport =
                    await _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo,
                        companyClaims);

                if (purchaseOrderReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseOrderReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("PurchaseOrderReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "PURCHASE ORDER REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "PO #";
                worksheet.Cells["B7"].Value = "IS PO #";
                worksheet.Cells["C7"].Value = "Date";
                worksheet.Cells["D7"].Value = "Supplier";
                worksheet.Cells["E7"].Value = "Product";
                worksheet.Cells["F7"].Value = "Quantity";
                worksheet.Cells["G7"].Value = "Unit";
                worksheet.Cells["H7"].Value = "Price";
                worksheet.Cells["I7"].Value = "Amount";
                worksheet.Cells["J7"].Value = "Remarks";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:J7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.00";

                foreach (var po in purchaseOrderReport)
                {
                    worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                    worksheet.Cells[row, 2].Value = po.OldPoNo;
                    worksheet.Cells[row, 3].Value = po.Date;
                    worksheet.Cells[row, 4].Value = po.Supplier?.SupplierName;
                    worksheet.Cells[row, 5].Value = po.Product?.ProductName;
                    worksheet.Cells[row, 6].Value = po.Quantity;
                    worksheet.Cells[row, 7].Value = po.Product?.ProductUnit;
                    worksheet.Cells[row, 8].Value = po.Price;
                    worksheet.Cells[row, 9].Value = po.Amount;
                    worksheet.Cells[row, 10].Value = po.Remarks;

                    worksheet.Cells[row, 3].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PurchaseOrderReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
        }

        #endregion

        #region -- Generate Cleared Disbursement Report Excel File --

        public async Task<IActionResult> GenerateClearedDisbursementReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var clearedDisbursementReport =
                    await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo,
                        companyClaims);


                if (clearedDisbursementReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(clearedDisbursementReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ClearedDisbursementReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "CLEARED DISBURSEMENT REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Category";
                worksheet.Cells["B7"].Value = "Subcategory";
                worksheet.Cells["C7"].Value = "Payee";
                worksheet.Cells["D7"].Value = "Date";
                worksheet.Cells["E7"].Value = "Voucher #";
                worksheet.Cells["F7"].Value = "Bank Name";
                worksheet.Cells["G7"].Value = "Check #";
                worksheet.Cells["H7"].Value = "Particulars";
                worksheet.Cells["I7"].Value = "Amount";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:I7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.00";

                foreach (var cd in clearedDisbursementReport)
                {

                    var details = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == cd.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                    var invoiceDebit = details
                        .FirstOrDefault(d => d.Debit > 0);

                    var getCategoryInChartOfAccount = await _dbContext.FilprideChartOfAccounts
                        .Include(coa => coa.ParentAccount) // Level 3
                        .ThenInclude(a => a!.ParentAccount) // Level 2
                        .ThenInclude(a => a!.ParentAccount) // Level 1
                        .Where(coa => coa.AccountNumber == invoiceDebit!.AccountNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    var levelOneAccount = getCategoryInChartOfAccount?.ParentAccount?.ParentAccount;

                    worksheet.Cells[row, 1].Value = $"{levelOneAccount!.AccountNumber} " +
                                                    $"{levelOneAccount.AccountName}";
                    worksheet.Cells[row, 2].Value = $"{invoiceDebit!.AccountNo} {invoiceDebit.AccountName}";
                    worksheet.Cells[row, 3].Value = cd.Payee;
                    worksheet.Cells[row, 4].Value = cd.Date;
                    worksheet.Cells[row, 5].Value = cd.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 6].Value = cd.BankAccount!.AccountName;
                    worksheet.Cells[row, 7].Value = cd.CheckNo;
                    worksheet.Cells[row, 8].Value = cd.Particulars;
                    worksheet.Cells[row, 9].Value = cd.Total;

                    worksheet.Cells[row, 4].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                worksheet.Cells[row, 8].Value = "Total: ";
                worksheet.Cells[row, 9].Value = clearedDisbursementReport.Sum(cv => cv.Total);
                using (var range = worksheet.Cells[row, 1, row, 9])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thick; // Apply thick border at the top of the row
                }

                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ClearedDisbursementReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }


        #endregion

        #region -- Generate Purchase Report Excel File --

        public async Task<IActionResult> GeneratePurchaseReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(PurchaseReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // get rr data from chosen date
                var purchaseReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

                // check if there is no record
                if (purchaseReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                #region -- Initialize "total" Variables for operations --

                decimal totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
                decimal totalCostPerLiter = 0m;
                decimal totalCostAmount = 0m;
                decimal totalVatAmount = 0m;
                decimal totalWHTAmount = 0m;
                decimal totalNetPurchases = 0m;
                decimal totalCOSPrice = purchaseReport.Sum(pr => (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m));
                decimal totalCOSAmount = 0m;
                decimal totalGMPerLiter = 0m;
                decimal totalGMAmount = 0m;
                decimal totalFreightCharge = purchaseReport.Sum(pr => (pr.DeliveryReceipt?.Freight ?? 0m));
                decimal totalFCAmount = 0m;
                decimal totalCommissionPerLiter = purchaseReport.Sum(pr => (pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m));
                decimal totalCommissionAmount = 0m;
                decimal totalNetMarginPerLiter = 0m;
                decimal totalNetMarginAmount = 0m;
                decimal totalNetFreight = 0m;
                decimal totalCommission = 0m;

                #endregion

                // Create the Excel package
                using var package = new ExcelPackage();

                // Add a new worksheet to the Excel package
                var purchaseReportWorksheet = package.Workbook.Worksheets.Add("PurchaseReport");

                #region -- Purchase Report Worksheet --

                #region -- Set the column header  --

                var mergedCells = purchaseReportWorksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "PURCHASE REPORT";
                mergedCells.Style.Font.Size = 13;

                purchaseReportWorksheet.Cells["A2"].Value = "Date Range:";
                purchaseReportWorksheet.Cells["A3"].Value = "Extracted By:";
                purchaseReportWorksheet.Cells["A4"].Value = "Company:";

                purchaseReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                purchaseReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                purchaseReportWorksheet.Cells["B4"].Value = $"{companyClaims}";

                purchaseReportWorksheet.Cells["A7"].Value = "TRANSACTION DATE";
                purchaseReportWorksheet.Cells["B7"].Value = "CUSTOMER RECEIVED DATE";
                purchaseReportWorksheet.Cells["C7"].Value = "SUPPLIER NAME";
                purchaseReportWorksheet.Cells["D7"].Value = "SUPPLIER TIN";
                purchaseReportWorksheet.Cells["E7"].Value = "SUPPLIER ADDRESS";
                purchaseReportWorksheet.Cells["F7"].Value = "PO#.";
                purchaseReportWorksheet.Cells["G7"].Value = "FILPRIDE RR";
                purchaseReportWorksheet.Cells["H7"].Value = "COS#";
                purchaseReportWorksheet.Cells["I7"].Value = "FILPRIDE DR";
                purchaseReportWorksheet.Cells["J7"].Value = "DEPOT";
                purchaseReportWorksheet.Cells["K7"].Value = "ATL #";
                purchaseReportWorksheet.Cells["L7"].Value = "SUPPLIER'S SI";
                purchaseReportWorksheet.Cells["M7"].Value = "SI/LIFTING DATE";
                purchaseReportWorksheet.Cells["N7"].Value = "SUPPLIER'S DR";
                purchaseReportWorksheet.Cells["O7"].Value = "SUPPLIER'S WC";
                purchaseReportWorksheet.Cells["P7"].Value = "CUSTOMER NAME";
                purchaseReportWorksheet.Cells["Q7"].Value = "PRODUCT";
                purchaseReportWorksheet.Cells["R7"].Value = "VOLUME";
                purchaseReportWorksheet.Cells["S7"].Value = "CPL G.VAT";
                purchaseReportWorksheet.Cells["T7"].Value = "PURCHASES G.VAT";
                purchaseReportWorksheet.Cells["U7"].Value = "VAT AMOUNT";
                purchaseReportWorksheet.Cells["V7"].Value = "WHT AMOUNT";
                purchaseReportWorksheet.Cells["W7"].Value = "HAULER'S NAME";
                purchaseReportWorksheet.Cells["X7"].Value = "PURCHASES N.VAT";
                purchaseReportWorksheet.Cells["Y7"].Value = "FREIGHT N.VAT";
                purchaseReportWorksheet.Cells["Z7"].Value = "COMMISSION";
                purchaseReportWorksheet.Cells["AA7"].Value = "OTC COS#.";
                purchaseReportWorksheet.Cells["AB7"].Value = "OTC DR#.";
                purchaseReportWorksheet.Cells["AC7"].Value = "IS PO#";
                purchaseReportWorksheet.Cells["AD7"].Value = "IS RR#";

                #endregion

                #region -- Apply styling to the header row --

                using (var range = purchaseReportWorksheet.Cells["A7:AD7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                #endregion

                // Populate the data rows
                int row = 8; // starting row
                string currencyFormat = "#,##0.0000"; // numbers format
                string currencyFormat2 = "#,##0.00"; // numbers format

                #region -- Populate data rows --

                foreach (var pr in purchaseReport)
                {
                    #region -- Variables and Formulas --

                    // calculate values, put in variables to be displayed per cell
                    var volume = pr.QuantityReceived; // volume
                    var costAmount = pr.Amount; // purchase total gross
                    var netPurchases = costAmount / 1.12m; // purchase total net
                    var netFreight = pr.DeliveryReceipt?.Freight / 1.12m ?? 0m; // purchase total net
                    var vatAmount = costAmount * 0.12m; // vat total
                    var whtAmount = netPurchases * 0.01m; // wht total
                    var cosAmount = (pr.QuantityReceived * (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m)); // sale total gross
                    var costPerLiter = costAmount / volume; // sale price per liter
                    var commission = ((pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m) * volume);

                    #endregion

                    #region -- Assign Values to Cells --

                    purchaseReportWorksheet.Cells[row, 1].Value = pr.Date; // Date
                    purchaseReportWorksheet.Cells[row, 2].Value = pr.DeliveryReceipt?.DeliveredDate; // DeliveredDate
                    purchaseReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.Supplier?.SupplierName; // Supplier Name
                    purchaseReportWorksheet.Cells[row, 4].Value = pr.PurchaseOrder?.Supplier?.SupplierTin; // Supplier Tin
                    purchaseReportWorksheet.Cells[row, 5].Value = pr.PurchaseOrder?.Supplier?.SupplierAddress; // Supplier Address
                    purchaseReportWorksheet.Cells[row, 6].Value = pr.PurchaseOrder?.PurchaseOrderNo; // PO No.
                    purchaseReportWorksheet.Cells[row, 7].Value = pr.ReceivingReportNo ?? pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride RR
                    purchaseReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo; // COS
                    purchaseReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride DR
                    purchaseReportWorksheet.Cells[row, 10].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.PickUpPoint?.Depot; // Filpride DR
                    purchaseReportWorksheet.Cells[row, 11].Value = pr.DeliveryReceipt?.AuthorityToLoadNo; // ATL #
                    purchaseReportWorksheet.Cells[row, 12].Value = pr.SupplierInvoiceNumber; // Supplier's Sales Invoice
                    purchaseReportWorksheet.Cells[row, 13].Value = pr.SupplierInvoiceDate; // Supplier's Sales Invoice
                    purchaseReportWorksheet.Cells[row, 14].Value = pr.SupplierDrNo; // Supplier's DR
                    purchaseReportWorksheet.Cells[row, 15].Value = pr.WithdrawalCertificate; // Supplier's WC
                    purchaseReportWorksheet.Cells[row, 16].Value = pr.DeliveryReceipt?.Customer?.CustomerName; // Customer Name
                    purchaseReportWorksheet.Cells[row, 17].Value = pr.PurchaseOrder?.Product?.ProductName; // Product
                    purchaseReportWorksheet.Cells[row, 18].Value = volume; // Volume
                    purchaseReportWorksheet.Cells[row, 19].Value = costPerLiter; // Purchase price per liter
                    purchaseReportWorksheet.Cells[row, 20].Value = costAmount; // Purchase total gross
                    purchaseReportWorksheet.Cells[row, 21].Value = vatAmount; // Vat total
                    purchaseReportWorksheet.Cells[row, 22].Value = whtAmount; // WHT total
                    purchaseReportWorksheet.Cells[row, 23].Value = pr.DeliveryReceipt?.Hauler?.SupplierName; // Hauler's Name
                    purchaseReportWorksheet.Cells[row, 24].Value = netPurchases; // Purchase total net ======== move to third last
                    purchaseReportWorksheet.Cells[row, 25].Value = netFreight; // freight n vat ============
                    purchaseReportWorksheet.Cells[row, 26].Value = commission; // commission =========
                    purchaseReportWorksheet.Cells[row, 27].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo; // OTC COS =========
                    purchaseReportWorksheet.Cells[row, 28].Value = pr.DeliveryReceipt?.ManualDrNo; // OTC DR =========
                    purchaseReportWorksheet.Cells[row, 29].Value = pr.PurchaseOrder?.OldPoNo; // IS PO =========
                    purchaseReportWorksheet.Cells[row, 30].Value = pr.OldRRNo; // IS RR =========

                    #endregion -- Assign Values to Cells --

                    #region -- Add the values to total --

                    totalCostAmount += costAmount;
                    totalVatAmount += vatAmount;
                    totalWHTAmount += whtAmount;
                    totalNetPurchases += netPurchases;
                    totalCOSAmount += cosAmount;
                    totalCommission += commission;

                    #endregion -- Add the values to total and format number cells --

                    #region -- Add format number cells from Assign Values to Cells --

                    purchaseReportWorksheet.Cells[row, 1, row, 2].Style.Numberformat.Format = "MMM/dd/yyyy";
                    purchaseReportWorksheet.Cells[row, 13].Style.Numberformat.Format = "MMM/dd/yyyy";
                    purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat2;

                    #endregion -- Add format number cells from Assign Values to Cells --

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Assign values of other totals and formatting of total cells --

                totalCostPerLiter = totalCostAmount / totalVolume;
                totalCOSPrice = totalCOSAmount / totalVolume;
                totalGMPerLiter = totalGMAmount / totalVolume;
                totalFreightCharge = totalFCAmount / totalVolume;
                totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                purchaseReportWorksheet.Cells[row, 17].Value = "Total: ";
                purchaseReportWorksheet.Cells[row, 18].Value = totalVolume;
                purchaseReportWorksheet.Cells[row, 19].Value = totalCostPerLiter;
                purchaseReportWorksheet.Cells[row, 20].Value = totalCostAmount;
                purchaseReportWorksheet.Cells[row, 21].Value = totalVatAmount;
                purchaseReportWorksheet.Cells[row, 22].Value = totalWHTAmount;
                purchaseReportWorksheet.Cells[row, 24].Value = totalNetPurchases;
                purchaseReportWorksheet.Cells[row, 25].Value = totalNetFreight;
                purchaseReportWorksheet.Cells[row, 26].Value = totalCommission;

                purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                purchaseReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                purchaseReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat2;

                #endregion -- Assign values of other totals and formatting of total cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = purchaseReportWorksheet.Cells[row, 1, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = purchaseReportWorksheet.Cells[row, 14, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                row += 2;

                #region -- Summary Header --

                purchaseReportWorksheet.Cells[row, 2].Value = "SUMMARY: ";
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Size = 16;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.UnderLine = true;

                row++;

                var firstColumnForThickBorder = row;

                var startingSummaryTableRow = row;

                string[] productList = { "DIESEL", "ECONO", "ENVIRO" };

                for (int i = 3, index = 0; i != 12; i += 3, index++)
                {
                    mergedCells = purchaseReportWorksheet.Cells[row, i, row, i + 2];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 16;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    mergedCells.Merge = true;
                    mergedCells.Value = productList[index];
                }

                row++;

                purchaseReportWorksheet.Cells[row, 2].Value = "SUPPLIERS";
                purchaseReportWorksheet.Cells[row, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                purchaseReportWorksheet.Cells[row, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                purchaseReportWorksheet.Cells[row, 2].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                purchaseReportWorksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                for (int i = 2; i != 11; i += 3)
                {
                    purchaseReportWorksheet.Cells[row, i + 1].Value = "VOLUME";
                    purchaseReportWorksheet.Cells[row, i + 2].Value = "PURCHASES N.VAT";
                    purchaseReportWorksheet.Cells[row, i + 3].Value = "AVE. CPL";
                    purchaseReportWorksheet.Cells[row, i + 1, row, i + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    purchaseReportWorksheet.Cells[row, i + 1, row, i + 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    purchaseReportWorksheet.Cells[row, i + 1, row, i + 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                    using (var range = purchaseReportWorksheet.Cells[row, i + 1, row, i + 3])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Italic = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    }
                }

                row += 2;

                #endregion -- Summary Header --

                #region == Summary Contents ==

                // query a group by supplier
                var supplierByRr = purchaseReport
                    .OrderBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName)
                    .GroupBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName);

                // for each supplier
                foreach (var rrSupplier in supplierByRr)
                {
                    int startingColumn = 2;

                    // get name of group supplier
                    purchaseReportWorksheet.Cells[row, 2].Value = rrSupplier.First().PurchaseOrder!.Supplier!.SupplierName;
                    purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                    purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;

                    // group each product of supplier
                    var productBySupplier = rrSupplier
                        .OrderBy(p => p.PurchaseOrder!.Product!.ProductName)
                        .GroupBy(rr => rr.PurchaseOrder!.Product!.ProductName);

                    // get volume, net purchases, and average cost per liter
                    foreach (var product in productBySupplier)
                    {
                        if (product.Any())
                        {
                            var grandTotalVolume = product
                                .Sum(pr => pr.QuantityReceived); // volume
                            var grandTotalPurchaseNet = product
                                .Sum(pr => (pr.QuantityReceived * pr.PurchaseOrder?.Price ?? 0m) / 1.12m); // Purchase Net Total

                            purchaseReportWorksheet.Cells[row, startingColumn + 1].Value = grandTotalVolume;
                            purchaseReportWorksheet.Cells[row, startingColumn + 2].Value = grandTotalPurchaseNet;
                            purchaseReportWorksheet.Cells[row, startingColumn + 3].Value = (grandTotalVolume != 0m ? grandTotalPurchaseNet / grandTotalVolume : 0m); // Gross Margin Per Liter
                            purchaseReportWorksheet.Cells[row, startingColumn + 1, row, startingColumn + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            purchaseReportWorksheet.Cells[row, startingColumn + 1].Style.Numberformat.Format = currencyFormat2;
                            purchaseReportWorksheet.Cells[row, startingColumn + 2].Style.Numberformat.Format = currencyFormat2;
                            purchaseReportWorksheet.Cells[row, startingColumn + 3].Style.Numberformat.Format = currencyFormat;
                        }

                        startingColumn += 3;
                    }

                    row++;
                }

                var endingSummaryTableRow = row - 1;

                row++;

                for (int i = 2; i != 11; i += 3)
                {
                    purchaseReportWorksheet.Cells[row, i + 1].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, i + 1].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, i + 1].Address})";
                    purchaseReportWorksheet.Cells[row, i + 2].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, i + 2].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, i + 2].Address})";
                    purchaseReportWorksheet.Cells[row, i + 3].Formula = $"={purchaseReportWorksheet.Cells[row, i + 2].Address}/{purchaseReportWorksheet.Cells[row, i + 1].Address}";


                    purchaseReportWorksheet.Cells[row, i + 1].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, i + 2].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, i + 3].Style.Numberformat.Format = currencyFormat;

                    mergedCells = purchaseReportWorksheet.Cells[row, i + 1, row, i + 3];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    mergedCells.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    mergedCells.Style.Font.Size = 11;
                    mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var lastColumnForThickBorder = row;

                var Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 2, lastColumnForThickBorder, 2];
                Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 3, lastColumnForThickBorder, 5];
                Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 6, lastColumnForThickBorder, 8];
                Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 9, lastColumnForThickBorder, 11];
                Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                #endregion == Summary Contents ==

                #endregion -- Summary Rows --

                // Auto-fit columns for better readability
                purchaseReportWorksheet.Cells.AutoFitColumns();
                purchaseReportWorksheet.View.FreezePanes(8, 1);

                for (int col = 1; col <= 22; col++)
                {
                    double currentWidth = purchaseReportWorksheet.Column(col).Width;
                    if (currentWidth > 25)
                    {
                        purchaseReportWorksheet.Column(col).Width = 24;
                    }
                }

                #endregion -- Purchase Report Worksheet --

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Purchase Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(PurchaseReport));
            }
        }

        #endregion

        #region -- Generate Fuel Sales Report Excel File --

        public async Task<IActionResult> GenerateOtcFuelSalesReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(OtcFuelSalesReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // fetch sales report
                var salesReport = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims);

                // check if there is no record
                if (salesReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(OtcFuelSalesReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();

                #region == Product worksheets ==

                var groupedByProductReport = salesReport
                    .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip?.Product?.ProductName)
                    .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip?.Product?.ProductName);

                foreach (var productReport in groupedByProductReport)
                {
                    var productName = productReport.First().DeliveryReceipt.CustomerOrderSlip?.Product?.ProductName;

                    var worksheet = package.Workbook.Worksheets.Add(productName);

                    #region == Header Contents and Formatting ==

                    var mergedCells = worksheet.Cells["A1:B1"];
                    mergedCells.Merge = true;
                    mergedCells.Value = productName;
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 15;
                    mergedCells.Style.Font.Name = "Tahoma";
                    mergedCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Row(1).Height = 20;

                    worksheet.Cells["A2"].Value = "Sales Report Per Total";
                    worksheet.Cells["A3"].Value = "Period Covered";
                    worksheet.Cells["A4"].Value = "Date From:";
                    worksheet.Cells["A5"].Value = "Date To:";

                    worksheet.Cells["B4"].Value = $"{dateFrom}";
                    worksheet.Cells["B5"].Value = $"{dateTo}";

                    worksheet.Cells["A1:B5"].Style.Font.Name = "Tahoma";
                    worksheet.Cells["A2:B5"].Style.Font.Size = 11;

                    #endregion == Header Contents and Formatting ==

                    #region == Column Names ==
                    worksheet.Cells["A8"].Value = "DATE";
                    worksheet.Cells["B8"].Value = "ACCOUNT NAME";
                    worksheet.Cells["C8"].Value = "ACCT TYPE";
                    worksheet.Cells["D8"].Value = "COS #";
                    worksheet.Cells["E8"].Value = "OTC COS #";
                    worksheet.Cells["F8"].Value = "DR #"; ;
                    worksheet.Cells["G8"].Value = "OTC DR #";
                    worksheet.Cells["H8"].Value = "ITEMS";
                    worksheet.Cells["I8"].Value = "VOLUME";
                    worksheet.Cells["J8"].Value = "TOTAL";
                    worksheet.Cells["K8"].Value = "REMARKS";
                    #endregion == Column Names ==

                    #region == Initialize condition variables ==
                    int row = 9;
                    string currencyFormat = "#,##0.00";
                    var totalVolume = 0m;
                    var totalAmount = 0m;
                    var grandTotalVolume = 0m;
                    var grandTotalAmount = 0m;
                    #endregion

                    var groupedByCustomer = productReport
                        .OrderBy(pr => pr.DeliveryReceipt.Customer?.CustomerName)
                        .GroupBy(pr => pr.DeliveryReceipt.Customer?.CustomerName);

                    foreach (var customer in groupedByCustomer)
                    {
                        var sortedByDateCustomer = customer
                            .OrderBy(c => c.DeliveryReceipt.DeliveredDate)
                            .ToList();

                        totalVolume = 0m;
                        totalAmount = 0m;

                        foreach (var transaction in sortedByDateCustomer)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.DeliveryReceipt.DeliveredDate; // Date
                            worksheet.Cells[row, 2].Value = transaction.DeliveryReceipt.Customer?.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.DeliveryReceipt.Customer?.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName; // Items
                            worksheet.Cells[row, 9].Value = transaction.DeliveryReceipt.Quantity; // Volume
                            worksheet.Cells[row, 10].Value = transaction.DeliveryReceipt.TotalAmount; // Total
                            worksheet.Cells[row, 11].Value = transaction.DeliveryReceipt.Remarks; // Remarks

                            #endregion -- Assign Values to Cells --

                            // increment totals and format it
                            totalVolume += transaction.DeliveryReceipt.Quantity;
                            totalAmount += transaction.DeliveryReceipt.TotalAmount;

                            // format cells with number
                            worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                            row++;
                        }

                        // put total at the buttom of customer list
                        worksheet.Cells[row, 9].Value = totalVolume;
                        worksheet.Cells[row, 10].Value = totalAmount;

                        //format total
                        worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                        // additional formatting for the subtotal
                        using (var range = worksheet.Cells[row, 9, row, 10])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Font.Bold = true;
                            range.Style.Font.Size = 12;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                        }

                        grandTotalVolume += totalVolume;
                        grandTotalAmount += totalAmount;

                        row++;

                    }

                    row++;

                    worksheet.Cells[row, 8].Value = "Grand Total:";

                    // put total at the buttom of customer list
                    worksheet.Cells[row, 9].Value = grandTotalVolume;
                    worksheet.Cells[row, 10].Value = grandTotalAmount;

                    //format total
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                    // additional formatting for the subtotal
                    using (var range = worksheet.Cells[row, 9, row, 10])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Font.Bold = true;
                        range.Style.Font.Size = 12;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                    }

                    using (var range = worksheet.Cells[$"A9:H{row}"])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    using (var range = worksheet.Cells[$"A9:K{row}"])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // table header
                    using (var range = worksheet.Cells["A7:K7"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thick;
                    }

                    using (var range = worksheet.Cells["A8:K8"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Auto-fit columns for better readability
                    worksheet.Cells.AutoFitColumns();
                    worksheet.View.FreezePanes(9, 1);
                }

                #endregion == Product worksheets ==

                #region == Comparison worksheet ==

                if (true)
                {
                    var worksheet = package.Workbook.Worksheets.Add("COMPARISON");

                    #region == Header Contents and Formatting ==

                    var mergedCells = worksheet.Cells["A1:B1"];
                    mergedCells.Merge = true;
                    mergedCells.Value = "Comparison";
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 15;
                    mergedCells.Style.Font.Name = "Tahoma";
                    mergedCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Row(1).Height = 20;

                    worksheet.Cells["A2"].Value = "Sales Report Per Total";
                    worksheet.Cells["A3"].Value = "Period Covered";
                    worksheet.Cells["A4"].Value = "Date From:";
                    worksheet.Cells["A5"].Value = "Date To:";

                    worksheet.Cells["B4"].Value = $"{dateFrom}";
                    worksheet.Cells["B5"].Value = $"{dateTo}";

                    worksheet.Cells["A1:B5"].Style.Font.Name = "Tahoma";
                    worksheet.Cells["A2:B5"].Style.Font.Size = 11;

                    #endregion == Header Contents and Formatting ==

                    #region == Column Names ==
                    worksheet.Cells["A8"].Value = "DATE";
                    worksheet.Cells["B8"].Value = "ACCOUNT NAME";
                    worksheet.Cells["C8"].Value = "ACCT TYPE";
                    worksheet.Cells["D8"].Value = "COS #";
                    worksheet.Cells["E8"].Value = "OTC COS #";
                    worksheet.Cells["F8"].Value = "DR #";
                    worksheet.Cells["G8"].Value = "OTC DR #";
                    worksheet.Cells["H8"].Value = "ITEMS";
                    worksheet.Cells["I8"].Value = "VOLUME";
                    worksheet.Cells["J8"].Value = "TOTAL";
                    worksheet.Cells["K8"].Value = "REMARKS";
                    #endregion == Column Names ==

                    #region == Initialize condition variables ==
                    int row = 9;
                    string currencyFormat = "#,##0.00";
                    var totalVolume = 0m;
                    var totalAmount = 0m;
                    var grandTotalVolume = 0m;
                    var grandTotalAmount = 0m;
                    #endregion

                    groupedByProductReport = salesReport
                        .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName)
                        .ThenBy(sr => sr.DeliveryReceipt.Customer!.CustomerName)
                        .ThenBy(sr => sr.DeliveryReceipt.DeliveredDate)
                        .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName);

                    // shows by product
                    foreach (var product in groupedByProductReport)
                    {
                        totalVolume = 0m;
                        totalAmount = 0m;

                        foreach (var transaction in product)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.DeliveryReceipt.DeliveredDate; // Date
                            worksheet.Cells[row, 2].Value = transaction.DeliveryReceipt.Customer?.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.DeliveryReceipt.Customer?.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt?.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt?.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName; // Items
                            worksheet.Cells[row, 9].Value = transaction.DeliveryReceipt.Quantity; // Volume
                            worksheet.Cells[row, 10].Value = transaction.DeliveryReceipt.TotalAmount; // Total
                            worksheet.Cells[row, 11].Value = transaction.DeliveryReceipt.Remarks; // Remarks

                            #endregion -- Assign Values to Cells --

                            // increment totals
                            totalVolume += transaction.DeliveryReceipt.Quantity;
                            totalAmount += transaction.DeliveryReceipt.TotalAmount;

                            // format cells with number
                            worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                            row++;
                        }

                        // put total at the buttom of customer list
                        worksheet.Cells[row, 9].Value = totalVolume;
                        worksheet.Cells[row, 10].Value = totalAmount;

                        //format total
                        worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                        // additional formatting for the subtotal
                        using (var range = worksheet.Cells[row, 9, row, 10])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Font.Bold = true;
                            range.Style.Font.Size = 12;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                        }

                        // incrementing for grandtotal
                        grandTotalVolume += totalVolume;
                        grandTotalAmount += totalAmount;

                        row++;
                    }

                    row++;

                    #region == Grandtotal ==
                    // showing grandtotal
                    worksheet.Cells[row, 8].Value = "Grand Total:";
                    worksheet.Cells[row, 9].Value = grandTotalVolume;
                    worksheet.Cells[row, 10].Value = grandTotalAmount;

                    //format grantotal
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                    // additional formatting for the grandtotal
                    using (var range = worksheet.Cells[row, 9, row, 10])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Size = 12;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                    }

                    using (var range = worksheet.Cells["A7:K7"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thick;
                    }

                    using (var range = worksheet.Cells["A8:K8"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Auto-fit columns for better readability
                    worksheet.Cells.AutoFitColumns();
                    worksheet.View.FreezePanes(9, 1);

                    #endregion == Grandtotal ==
                }

                #endregion == Comparison worksheet ==

                #region == Month to Date Sales ==

                if (true)
                {
                    var worksheet = package.Workbook.Worksheets.Add("MONTH TO DATE SALES REPORT");

                    #region == Header Contents and Formatting ==

                    var mergedCells = worksheet.Cells["A1:F1"];
                    mergedCells.Merge = true;
                    mergedCells.Value = "MONTH TO DATE SALES REPORT";
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 18;
                    mergedCells.Style.Font.Name = "Aptos Narrow";
                    mergedCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Row(1).Height = 24;

                    #endregion == Header Contents and Formatting ==

                    int row = 3;
                    bool isStation = true;

                    var groupByCustomerType = salesReport
                        .OrderBy(sr => sr.DeliveryReceipt.Customer?.CustomerType)
                        .GroupBy(sr => sr.DeliveryReceipt.Customer?.CustomerType)
                        .OrderBy(g => g.Key != "Retail")
                        .ThenBy(g => g.Key);

                    #region == Contents ==

                    foreach (var ct in groupByCustomerType)
                    {
                        worksheet.Cells[row, 1].Value = ct.First().DeliveryReceipt.Customer?.CustomerType;
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Italic = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 18;

                        row++;
                        worksheet.Cells[row, 1].Value = isStation ? "STATION" : "ACCOUNTS";

                        worksheet.Cells[row, 2].Value = "BIODIESEL";
                        worksheet.Cells[row, 3].Value = "AMOUNT";
                        worksheet.Cells[row, 4].Value = "ECONOGAS";
                        worksheet.Cells[row, 5].Value = "AMOUNT";
                        worksheet.Cells[row, 6].Value = "ENVIROGAS";
                        worksheet.Cells[row, 7].Value = "AMOUNT";
                        worksheet.Cells[row, 8].Value = "TOTAL";
                        worksheet.Cells[row, 9].Value = "AMOUNT";

                        using (var range = worksheet.Cells[row, 1, row, 9])
                        {
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        var rowToResize = row;
                        row++;

                        var groupByCustomerName = ct
                            .OrderBy(sr => sr.DeliveryReceipt.Customer?.CustomerName)
                            .GroupBy(sr => sr.DeliveryReceipt.Customer?.CustomerName);

                        foreach (var customerGroup in groupByCustomerName)
                        {
                            worksheet.Cells[row, 1].Value = customerGroup.First().DeliveryReceipt.Customer?.CustomerName;
                            worksheet.Cells[row, 1].Style.Font.Bold = true;

                            worksheet.Cells[row, 2].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                                .Sum(cg => cg.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, 3].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                                .Sum(cg => cg.DeliveryReceipt.TotalAmount);
                            worksheet.Cells[row, 4].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                                .Sum(cg => cg.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, 5].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                                .Sum(cg => cg.DeliveryReceipt.TotalAmount);
                            worksheet.Cells[row, 6].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                                .Sum(cg => cg.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, 7].Value = customerGroup
                                .Where(cg => cg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                                .Sum(cg => cg.DeliveryReceipt.TotalAmount);
                            worksheet.Cells[row, 8].Value = customerGroup
                                .Sum(cg => cg.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, 9].Value = customerGroup
                                .Sum(cg => cg.DeliveryReceipt.TotalAmount);

                            worksheet.Cells[row, 2, row, 9].Style.Numberformat.Format = "#,##0.00";

                            row++;
                        }

                        worksheet.Cells[row, 1].Value = "Total";
                        worksheet.Cells[row, 2].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                            .Sum(si => si.DeliveryReceipt.Quantity); // Total Volume
                        worksheet.Cells[row, 3].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                            .Sum(si => si.DeliveryReceipt.TotalAmount); // Total Amount
                        worksheet.Cells[row, 4].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                            .Sum(si => si.DeliveryReceipt.Quantity); // Total Volume
                        worksheet.Cells[row, 5].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                            .Sum(si => si.DeliveryReceipt.TotalAmount); // Total Amount
                        worksheet.Cells[row, 6].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                            .Sum(si => si.DeliveryReceipt.Quantity); // Total Volume
                        worksheet.Cells[row, 7].Value = ct
                            .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                            .Sum(si => si.DeliveryReceipt.TotalAmount); // Total Amount
                        worksheet.Cells[row, 8].Value = ct
                            .Sum(si => si.DeliveryReceipt.Quantity); // Total Volume
                        worksheet.Cells[row, 9].Value = ct
                            .Sum(si => si.DeliveryReceipt.TotalAmount); // Total Amount

                        var tillRowToResize = row;
                        worksheet.Cells[rowToResize, 1, tillRowToResize, 9].Style.Font.Size = 10;

                        using (var range = worksheet.Cells[row, 1, row, 9])
                        {
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                            range.Style.Numberformat.Format = "#,##0.00";
                        }

                        row += 2;
                        isStation = false;
                    }

                    #endregion == Contents ==

                    worksheet.Cells[row, 1].Value = "Grand Total";
                    worksheet.Cells[row, 2].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                        .Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, 3].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                        .Sum(si => si.DeliveryReceipt.TotalAmount);
                    worksheet.Cells[row, 4].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                        .Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, 5].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                        .Sum(si => si.DeliveryReceipt.TotalAmount);
                    worksheet.Cells[row, 6].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                        .Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, 7].Value = salesReport
                        .Where(si => si.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                        .Sum(si => si.DeliveryReceipt.TotalAmount);
                    worksheet.Cells[row, 8].Value = salesReport
                        .Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, 9].Value = salesReport
                        .Sum(si => si.DeliveryReceipt.TotalAmount);

                    using (var range = worksheet.Cells[row, 1, row, 9])
                    {
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                        range.Style.Numberformat.Format = "#,##0.00";
                    }

                    row += 2;

                    var summaryRowStart = row;

                    // summary column names
                    worksheet.Cells[row, 2].Value = "BIODIESEL";
                    worksheet.Cells[row, 3].Value = "ECONOGAS";
                    worksheet.Cells[row, 4].Value = "ENVIROGAS";
                    worksheet.Cells[row, 5].Value = "TOTAL";

                    // summary columns names styling
                    using (var range = worksheet.Cells[row, 2, row, 5])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.Font.Bold = true;
                        range.Style.Font.Italic = true;
                    }

                    row++;

                    // summary values
                    foreach (var typeGroup in groupByCustomerType)
                    {
                        worksheet.Cells[row, 1].Value = typeGroup.First().DeliveryReceipt.Customer?.CustomerType;
                        worksheet.Cells[row, 1].Style.Font.Italic = true;
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 2].Value = typeGroup
                            .Where(tg => tg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL")
                            .Sum(tg => tg.DeliveryReceipt.Quantity);
                        worksheet.Cells[row, 3].Value = typeGroup
                            .Where(tg => tg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS")
                            .Sum(tg => tg.DeliveryReceipt.Quantity);
                        worksheet.Cells[row, 4].Value = typeGroup
                            .Where(tg => tg.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS")
                            .Sum(tg => tg.DeliveryReceipt.Quantity);
                        worksheet.Cells[row, 5].Value = typeGroup
                            .Sum(tg => tg.DeliveryReceipt.Quantity);
                        row++;
                    }

                    // merge cells of "total" label
                    using (var range = worksheet.Cells[row, 1, row, 4])
                    {
                        range.Merge = true;
                        range.Value = "Total:";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    // styling total value
                    worksheet.Cells[row, 5].Value = salesReport.Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, 5].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                    worksheet.Cells[row, 5].Style.Font.Bold = true;

                    var summaryRowEnd = row;

                    // range for the summary
                    using (var range = worksheet.Cells[summaryRowStart, 1, summaryRowEnd, 5])
                    {
                        range.Style.Font.Name = "Aptos Narrow";
                        range.Style.Font.Size = 14;
                        range.Style.Numberformat.Format = "#,##0.00";
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                    }

                    worksheet.Cells.AutoFitColumns();

                    for (int col = 2; col <= 5; col++)
                    {
                        worksheet.Column(col).Width = 20;
                    }

                }

                #endregion == Month to Date Sales ==

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"OTC Fuel Sales Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(OtcFuelSalesReport));
            }
        }

        #endregion

        #region -- Generate GM Report Excel File --

        public async Task<IActionResult> GenerateGmReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(GmReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;

                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                using var package = new ExcelPackage();
                var gmReportWorksheet = package.Workbook.Worksheets.Add("GMReport");

                var purchaseReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, model.Customers, cancellationToken: cancellationToken);

                if (purchaseReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(GmReport));
                }

                #region -- Initialize "total" Variables for operations --

                var totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
                decimal totalCostPerLiter = 0m;
                decimal totalCostAmount = 0m;
                decimal totalNetPurchases = 0m;
                var totalCOSPrice = purchaseReport.Sum(pr => pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice);
                decimal totalSalesAmount = 0m;
                decimal totalNetSales = 0m;
                decimal totalGMPerLiter = 0m;
                decimal totalGmAmount = 0m;
                var totalFreightCharge = purchaseReport.Sum(pr => pr.DeliveryReceipt?.Freight + pr.DeliveryReceipt?.ECC);
                decimal totalFCAmount = 0m;
                decimal totalFCNet = 0m;
                var totalCommissionPerLiter = purchaseReport.Sum(pr => pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate);
                decimal totalCommissionAmount = 0m;
                decimal totalNetMarginPerLiter = 0m;
                decimal totalNetMarginAmount = 0m;

                #endregion

                #region -- Column Names --

                var mergedCells = gmReportWorksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "GM REPORT";
                mergedCells.Style.Font.Size = 13;

                gmReportWorksheet.Cells["A2"].Value = "Date Range:";
                gmReportWorksheet.Cells["A3"].Value = "Extracted By:";
                gmReportWorksheet.Cells["A4"].Value = "Company:";

                gmReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                gmReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                gmReportWorksheet.Cells["B4"].Value = $"{companyClaims}";

                gmReportWorksheet.Cells["A7"].Value = "RR DATE";
                gmReportWorksheet.Cells["B7"].Value = "SUPPLIER NAME";
                gmReportWorksheet.Cells["C7"].Value = "PO NO.";
                gmReportWorksheet.Cells["D7"].Value = "FILPRIDE RR";
                gmReportWorksheet.Cells["E7"].Value = "FILPRIDE DR";
                gmReportWorksheet.Cells["F7"].Value = "CUSTOMER NAME";
                gmReportWorksheet.Cells["G7"].Value = "PRODUCT NAME";
                gmReportWorksheet.Cells["H7"].Value = "ACCOUNT SPECIALIST";
                gmReportWorksheet.Cells["I7"].Value = "HAULER NAME";
                gmReportWorksheet.Cells["J7"].Value = "COMMISSIONEE";
                gmReportWorksheet.Cells["K7"].Value = "VOLUME";
                gmReportWorksheet.Cells["L7"].Value = "COS PRICE";
                gmReportWorksheet.Cells["M7"].Value = "SALES G. VAT";
                gmReportWorksheet.Cells["N7"].Value = "SALES N. VAT";
                gmReportWorksheet.Cells["O7"].Value = "CPL G. VAT";
                gmReportWorksheet.Cells["P7"].Value = "PURCHASES G. VAT";
                gmReportWorksheet.Cells["Q7"].Value = "PURCHASES N.VAT";
                gmReportWorksheet.Cells["R7"].Value = "GM/LITER";
                gmReportWorksheet.Cells["S7"].Value = "GM AMOUNT";
                gmReportWorksheet.Cells["T7"].Value = "FREIGHT CHARGE";
                gmReportWorksheet.Cells["U7"].Value = "FC AMOUNT";
                gmReportWorksheet.Cells["V7"].Value = "FC N.VAT";
                gmReportWorksheet.Cells["W7"].Value = "COMMISSION/LITER";
                gmReportWorksheet.Cells["X7"].Value = "COMMISSION AMOUNT";
                gmReportWorksheet.Cells["Y7"].Value = "NET MARGIN/LIT";
                gmReportWorksheet.Cells["Z7"].Value = "NET MARGIN AMOUNT";

                #endregion

                #region -- Apply styling to the header row --

                using (var range = gmReportWorksheet.Cells["A7:Z7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                #endregion

                // Populate the data row
                int row = 8; // starting row
                string currencyFormat = "#,##0.0000"; // numbers format
                string currencyFormatTwoDecimal = "#,##0.00"; // numbers format

                #region -- Populate data rows --

                foreach (var pr in purchaseReport)
                {
                    #region -- Variables and Formulas --

                    // calculate values, put in variables to be displayed per cell
                    var volume = pr.QuantityReceived;
                    var cosPricePerLiter = pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m; // sales per liter
                    var salesAmount = volume * cosPricePerLiter; // sales total
                    var netSales = salesAmount / 1.12m;
                    var costAmount = pr.Amount; // purchase total
                    var costPerLiter = costAmount / volume; // purchase per liter
                    var netPurchases = costAmount / 1.12m; // purchase total net
                    var gmAmount = netSales - netPurchases; // gross margin total
                    var gmPerLiter = gmAmount / volume; // gross margin per liter
                    var freightCharge = (pr.DeliveryReceipt?.Freight ?? 0m) + (pr.DeliveryReceipt?.ECC ?? 0m); // freight charge per liter
                    var freightChargeAmount = volume * freightCharge; // freight charge total
                    var freightChargeNet = freightChargeAmount / 1.12m;
                    var commissionPerLiter = pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m; // commission rate
                    var commissionAmount = volume * commissionPerLiter; // commission total
                    var netMarginAmount = gmAmount - freightChargeNet - commissionAmount;
                    var netMarginPerLiter = netMarginAmount / volume; // net margin per liter

                    #endregion

                    #region -- Assign Values to Cells --

                    gmReportWorksheet.Cells[row, 1].Value = pr.Date;
                    gmReportWorksheet.Cells[row, 2].Value = pr.PurchaseOrder?.Supplier?.SupplierName;
                    gmReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.PurchaseOrderNo;
                    gmReportWorksheet.Cells[row, 4].Value = pr.ReceivingReportNo;
                    gmReportWorksheet.Cells[row, 5].Value = pr.DeliveryReceipt?.DeliveryReceiptNo;
                    gmReportWorksheet.Cells[row, 6].Value = pr.DeliveryReceipt?.Customer?.CustomerName;
                    gmReportWorksheet.Cells[row, 7].Value = pr.PurchaseOrder?.Product?.ProductName;
                    gmReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.AccountSpecialist;
                    gmReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.Hauler?.SupplierName;
                    gmReportWorksheet.Cells[row, 10].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.Commissionee?.SupplierName;
                    gmReportWorksheet.Cells[row, 11].Value = volume;
                    gmReportWorksheet.Cells[row, 12].Value = cosPricePerLiter;
                    gmReportWorksheet.Cells[row, 13].Value = salesAmount;
                    gmReportWorksheet.Cells[row, 14].Value = netSales;
                    gmReportWorksheet.Cells[row, 15].Value = costPerLiter;
                    gmReportWorksheet.Cells[row, 16].Value = costAmount;
                    gmReportWorksheet.Cells[row, 17].Value = netPurchases;
                    gmReportWorksheet.Cells[row, 18].Value = gmPerLiter;
                    gmReportWorksheet.Cells[row, 19].Value = gmAmount;
                    gmReportWorksheet.Cells[row, 20].Value = freightCharge;
                    gmReportWorksheet.Cells[row, 21].Value = freightChargeAmount;
                    gmReportWorksheet.Cells[row, 22].Value = freightChargeNet;
                    gmReportWorksheet.Cells[row, 23].Value = commissionPerLiter;
                    gmReportWorksheet.Cells[row, 24].Value = commissionAmount;
                    gmReportWorksheet.Cells[row, 25].Value = netMarginPerLiter;
                    gmReportWorksheet.Cells[row, 26].Value = netMarginAmount;

                    #endregion -- Assign Values to Cells --

                    #region -- Add the values to total and format number cells --

                    totalCostAmount += costAmount;
                    totalNetPurchases += netPurchases;
                    totalSalesAmount += salesAmount;
                    totalGMPerLiter += gmPerLiter;
                    totalGmAmount += gmAmount;
                    totalFCAmount += freightChargeAmount;
                    totalCommissionAmount += commissionAmount;
                    totalNetMarginPerLiter += netMarginPerLiter;
                    totalNetMarginAmount += netMarginAmount;
                    totalNetSales += netSales;
                    totalFCNet += freightChargeNet;

                    gmReportWorksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    gmReportWorksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    #endregion -- Add the values to total and format number cells --

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Other subtotal values and formatting of subtotal cells --

                totalCostPerLiter = totalCostAmount / totalVolume;
                totalCOSPrice = totalSalesAmount / totalVolume;
                totalGMPerLiter = totalGmAmount / totalVolume;
                totalFreightCharge = totalFCAmount / totalVolume;
                totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                gmReportWorksheet.Cells[row, 10].Value = "Total: ";
                gmReportWorksheet.Cells[row, 11].Value = totalVolume;
                gmReportWorksheet.Cells[row, 12].Value = totalCOSPrice;
                gmReportWorksheet.Cells[row, 13].Value = totalSalesAmount;
                gmReportWorksheet.Cells[row, 14].Value = totalNetSales;
                gmReportWorksheet.Cells[row, 15].Value = totalCostPerLiter;
                gmReportWorksheet.Cells[row, 16].Value = totalCostAmount;
                gmReportWorksheet.Cells[row, 17].Value = totalNetPurchases;
                gmReportWorksheet.Cells[row, 18].Value = totalGMPerLiter;
                gmReportWorksheet.Cells[row, 19].Value = totalGmAmount;
                gmReportWorksheet.Cells[row, 20].Value = totalFreightCharge;
                gmReportWorksheet.Cells[row, 21].Value = totalFCAmount;
                gmReportWorksheet.Cells[row, 22].Value = totalFCNet;
                gmReportWorksheet.Cells[row, 23].Value = totalCommissionPerLiter;
                gmReportWorksheet.Cells[row, 24].Value = totalCommissionAmount;
                gmReportWorksheet.Cells[row, 25].Value = totalNetMarginPerLiter;
                gmReportWorksheet.Cells[row, 26].Value = totalNetMarginAmount;

                gmReportWorksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;

                #endregion -- Assign values of other totals and formatting of total cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = gmReportWorksheet.Cells[row, 1, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = gmReportWorksheet.Cells[row, 10, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                var rowForSummary = row + 8;

                // Set the column headers
                var mergedCellForOverall = gmReportWorksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 10];
                mergedCellForOverall.Merge = true;
                mergedCellForOverall.Value = "Overall";
                mergedCellForOverall.Style.Font.Size = 13;
                mergedCellForOverall.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var textStyleForSummary = gmReportWorksheet.Cells[rowForSummary - 3, 2];
                textStyleForSummary.Style.Font.Size = 16;
                textStyleForSummary.Style.Font.Bold = true;

                gmReportWorksheet.Cells[rowForSummary - 3, 2].Value = "Summary";
                gmReportWorksheet.Cells[rowForSummary - 1, 2].Value = "Segment";
                gmReportWorksheet.Cells[rowForSummary - 1, 3].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 4].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 5].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 6].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 7].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 8].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 9].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 10].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Overall
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Overall
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForBiodiesel = gmReportWorksheet.Cells[rowForSummary - 2, 12, rowForSummary - 2, 19];
                mergedCellForBiodiesel.Merge = true;
                mergedCellForBiodiesel.Value = "Biodiesel";
                mergedCellForBiodiesel.Style.Font.Size = 13;
                mergedCellForBiodiesel.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 12, rowForSummary - 2, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                gmReportWorksheet.Cells[rowForSummary - 1, 12].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 13].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 14].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 15].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 16].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 17].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 18].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 19].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 12, rowForSummary - 1, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Biodiesel
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 12, rowForSummary - 1, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Biodiesel
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 12, rowForSummary + 4, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 12, rowForSummary + 4, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEconogas = gmReportWorksheet.Cells[rowForSummary - 2, 21, rowForSummary - 2, 28];
                mergedCellForEconogas.Merge = true;
                mergedCellForEconogas.Value = "Econogas";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 21, rowForSummary - 2, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                gmReportWorksheet.Cells[rowForSummary - 1, 21].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 22].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 23].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 24].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 25].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 26].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 27].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 28].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 21, rowForSummary - 1, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Econogas
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 21, rowForSummary - 1, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Econogas
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 21, rowForSummary + 4, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 21, rowForSummary + 4, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEnvirogas = gmReportWorksheet.Cells[rowForSummary - 2, 30, rowForSummary - 2, 37];
                mergedCellForEnvirogas.Merge = true;
                mergedCellForEnvirogas.Value = "Envirogas";
                mergedCellForEnvirogas.Style.Font.Size = 13;
                mergedCellForEnvirogas.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 30, rowForSummary - 2, 37].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                //inset data/value in excel
                gmReportWorksheet.Cells[rowForSummary - 1, 30].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 31].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 32].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 33].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 34].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 35].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 36].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 37].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 30, rowForSummary - 1, 37].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Envirogas
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 30, rowForSummary - 1, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Envirogas
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 30, rowForSummary + 4, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 30, rowForSummary + 4, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var listForBiodiesel = new List<FilprideReceivingReport>();
                var listForEconogas = new List<FilprideReceivingReport>();
                var listForEnvirogas = new List<FilprideReceivingReport>();

                var totalOverallQuantity = 0m;
                var totalOverallNetOfSales = 0m;
                var totalOverallNetOfPurchases = 0m;
                var totalOverallGrossMargin = 0m;
                var totalOverallNetOfFreight = 0m;
                var totalOverallCommission = 0m;
                var totalOverallNetMargin = 0m;
                var totalOverallNetMarginPerLiter = 0m;

                var totalQuantityForBiodiesel = 0m;
                var totalNetOfSalesForBiodiesel = 0m;
                var totalNetOfPurchasesForBiodiesel = 0m;
                var totalGrossMarginForBiodiesel = 0m;
                var totalNetOfFreightForBiodiesel = 0m;
                var totalCommissionForBiodiesel = 0m;
                var totalNetMarginForBiodiesel = 0m;
                var totalNetMarginPerLiterForBiodiesel = 0m;

                var totalQuantityForEconogas = 0m;
                var totalNetOfSalesForEconogas = 0m;
                var totalNetOfPurchasesForEconogas = 0m;
                var totalGrossMarginForEconogas = 0m;
                var totalNetOfFreightForEconogas = 0m;
                var totalCommissionForEconogas = 0m;
                var totalNetMarginForEconogas = 0m;
                var totalNetMarginPerLiterForEconogas = 0m;

                var totalQuantityForEnvirogas = 0m;
                var totalNetOfSalesForEnvirogas = 0m;
                var totalNetOfPurchasesForEnvirogas = 0m;
                var totalGrossMarginForEnvirogas = 0m;
                var totalNetOfFreightForEnvirogas = 0m;
                var totalCommissionForEnvirogas = 0m;
                var totalNetMarginForEnvirogas = 0m;
                var totalNetMarginPerLiterForEnvirogas = 0m;


                foreach (var customerType in Enum.GetValues<CustomerType>())
                {
                    var list = purchaseReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                    listForBiodiesel = list.Where(s => s.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL").ToList();
                    listForEconogas = list.Where(s => s.DeliveryReceipt!.PurchaseOrder!.Product?.ProductName == "ECONOGAS").ToList();
                    listForEnvirogas = list.Where(s => s.DeliveryReceipt!.PurchaseOrder!.Product?.ProductName == "ENVIROGAS").ToList();

                    // Computation for Overall
                    var overallQuantitySum = list.Sum(s => s.DeliveryReceipt!.Quantity);
                    var overallSalesSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var overallNetOfSalesSum = overallSalesSum != 0m ? overallSalesSum / 1.12m : 0;
                    var overallPurchasesSum = list.Sum(s => s.Amount);
                    var overallNetOfPurchasesSum = overallPurchasesSum != 0m ? overallPurchasesSum / 1.12m : 0;
                    var overallGrossMarginSum = overallNetOfSalesSum - overallNetOfPurchasesSum;
                    var overallFreightSum = list.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                    var overallNetOfFreightSum = overallFreightSum != 0m ? overallFreightSum / 1.12m : 0;
                    var overallCommissionSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var overallNetMarginSum = overallGrossMarginSum - (overallFreightSum + overallCommissionSum);
                    var overallNetMarginPerLiterSum = overallNetMarginSum != 0 && overallQuantitySum != 0 ? overallNetMarginSum / overallQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 2].Value = customerType.ToString();
                    gmReportWorksheet.Cells[rowForSummary, 3].Value = overallQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 4].Value = overallNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 5].Value = overallNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 6].Value = overallGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 7].Value = overallNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 8].Value = overallCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 9].Value = overallNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 10].Value = overallNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                    // Computation for Biodiesel
                    var biodieselQuantitySum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity);
                    var biodieselSalesSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var biodieselNetOfSalesSum = biodieselSalesSum != 0m ? biodieselSalesSum / 1.12m : 0;
                    var biodieselPurchasesSum = listForBiodiesel.Sum(s => s.Amount);
                    var biodieselNetOfPurchasesSum = biodieselPurchasesSum != 0m ? biodieselPurchasesSum / 1.12m : 0;
                    var biodieselGrossMarginSum = biodieselNetOfSalesSum - biodieselNetOfPurchasesSum;
                    var biodieselFreightSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                    var biodieselNetOfFreightSum = biodieselFreightSum != 0m ? biodieselFreightSum / 1.12m : 0;
                    var biodieselCommissionSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt.CustomerOrderSlip!.CommissionRate);
                    var biodieselNetMarginSum = biodieselGrossMarginSum - (biodieselFreightSum + biodieselCommissionSum);
                    var biodieselNetMarginPerLiterSum = biodieselNetMarginSum != 0 && biodieselQuantitySum != 0 ? biodieselNetMarginSum / biodieselQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 12].Value = biodieselQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 13].Value = biodieselNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 14].Value = biodieselNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 15].Value = biodieselGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 16].Value = biodieselNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 17].Value = biodieselCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 18].Value = biodieselNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 19].Value = biodieselNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 19].Style.Numberformat.Format = currencyFormat;

                    // Computation for Econogas
                    var econogasQuantitySum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity);
                    var econogasSalesSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt.CustomerOrderSlip!.DeliveredPrice);
                    var econogasNetOfSalesSum = econogasSalesSum != 0m ? econogasSalesSum / 1.12m : 0;
                    var econogasPurchasesSum = listForEconogas.Sum(s => s.Amount);
                    var econogasNetOfPurchasesSum = econogasPurchasesSum != 0m ? econogasPurchasesSum / 1.12m : 0;
                    var econogasGrossMarginSum = econogasNetOfSalesSum - econogasNetOfPurchasesSum;
                    var econogasFreightSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                    var econogasNetOfFreightSum = econogasFreightSum != 0m ? econogasFreightSum / 1.12m : 0;
                    var econogasCommissionSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var econogasNetMarginSum = econogasGrossMarginSum - (econogasFreightSum + econogasCommissionSum);
                    var econogasNetMarginPerLiterSum = econogasNetMarginSum != 0 && econogasQuantitySum != 0 ? econogasNetMarginSum / econogasQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 21].Value = econogasQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 22].Value = econogasNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 23].Value = econogasNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 24].Value = econogasGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 25].Value = econogasNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 26].Value = econogasCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 27].Value = econogasNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 28].Value = econogasNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 28].Style.Numberformat.Format = currencyFormat;

                    // Computation for Envirogas
                    var envirogasQuantitySum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity);
                    var envirogasSalesSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var envirogasNetOfSalesSum = envirogasSalesSum != 0m ? envirogasSalesSum / 1.12m : 0;
                    var envirogasPurchasesSum = listForEnvirogas.Sum(s => s.Amount);
                    var envirogasNetOfPurchasesSum = envirogasPurchasesSum != 0m ? envirogasPurchasesSum / 1.12m : 0;
                    var envirogasGrossMarginSum = envirogasNetOfSalesSum - envirogasNetOfPurchasesSum;
                    var envirogasFreightSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                    var envirogasNetOfFreightSum = envirogasFreightSum != 0m ? envirogasFreightSum / 1.12m : 0;
                    var envirogasCommissionSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var envirogasNetMarginSum = envirogasGrossMarginSum - (envirogasFreightSum + envirogasCommissionSum);
                    var envirogasNetMarginPerLiterSum = envirogasNetMarginSum != 0 && envirogasQuantitySum != 0 ? envirogasNetMarginSum / envirogasQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 30].Value = envirogasQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 31].Value = envirogasNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 32].Value = envirogasNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 33].Value = envirogasGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 34].Value = envirogasNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 35].Value = envirogasCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 36].Value = envirogasNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 37].Value = envirogasNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 31].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 32].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 33].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 34].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 35].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 36].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 37].Style.Numberformat.Format = currencyFormat;

                    rowForSummary++;

                    // Computation of total for Overall
                    totalOverallQuantity += overallQuantitySum;
                    totalOverallNetOfSales += overallNetOfSalesSum;
                    totalOverallNetOfPurchases += overallNetOfPurchasesSum;
                    totalOverallGrossMargin += overallGrossMarginSum;
                    totalOverallNetOfFreight += overallNetOfFreightSum;
                    totalOverallCommission += overallCommissionSum;
                    totalOverallNetMargin += overallNetMarginSum;
                    totalOverallNetMarginPerLiter += totalOverallNetMargin != 0 && totalOverallQuantity != 0 ? totalOverallNetMargin / totalOverallQuantity : 0;

                    // Computation of total for Biodiesel
                    totalQuantityForBiodiesel += biodieselQuantitySum;
                    totalNetOfSalesForBiodiesel += biodieselNetOfSalesSum;
                    totalNetOfPurchasesForBiodiesel += biodieselNetOfPurchasesSum;
                    totalGrossMarginForBiodiesel += biodieselGrossMarginSum;
                    totalNetOfFreightForBiodiesel += biodieselNetOfFreightSum;
                    totalCommissionForBiodiesel += biodieselCommissionSum;
                    totalNetMarginForBiodiesel += biodieselNetMarginSum;
                    totalNetMarginPerLiterForBiodiesel += totalNetMarginForBiodiesel != 0 && totalQuantityForBiodiesel != 0 ? totalNetMarginForBiodiesel / totalQuantityForBiodiesel : 0;

                    // Computation of total for Econogas
                    totalQuantityForEconogas += econogasQuantitySum;
                    totalNetOfSalesForEconogas += econogasNetOfSalesSum;
                    totalNetOfPurchasesForEconogas += econogasNetOfPurchasesSum;
                    totalGrossMarginForEconogas += econogasGrossMarginSum;
                    totalNetOfFreightForEconogas += econogasNetOfFreightSum;
                    totalCommissionForEconogas += econogasCommissionSum;
                    totalNetMarginForEconogas += econogasNetMarginSum;
                    totalNetMarginPerLiterForEconogas += totalNetMarginForEconogas != 0 && totalQuantityForEconogas != 0 ? totalNetMarginForEconogas / totalQuantityForEconogas : 0;

                    // Computation of total for Envirogas
                    totalQuantityForEnvirogas += envirogasQuantitySum;
                    totalNetOfSalesForEnvirogas += envirogasNetOfSalesSum;
                    totalNetOfPurchasesForEnvirogas += envirogasNetOfPurchasesSum;
                    totalGrossMarginForEnvirogas += envirogasGrossMarginSum;
                    totalNetOfFreightForEnvirogas += envirogasNetOfFreightSum;
                    totalCommissionForEnvirogas += envirogasCommissionSum;
                    totalNetMarginForEnvirogas += envirogasNetMarginSum;
                    totalNetMarginPerLiterForEnvirogas += totalNetMarginForEnvirogas != 0 && totalQuantityForEnvirogas != 0 ? totalNetMarginForEnvirogas / totalQuantityForEnvirogas : 0;

                }

                var styleOfTotal = gmReportWorksheet.Cells[rowForSummary, 2];
                styleOfTotal.Value = "Total";

                gmReportWorksheet.Cells[rowForSummary, 3].Value = totalOverallQuantity;
                gmReportWorksheet.Cells[rowForSummary, 4].Value = totalOverallNetOfSales;
                gmReportWorksheet.Cells[rowForSummary, 5].Value = totalOverallNetOfPurchases;
                gmReportWorksheet.Cells[rowForSummary, 6].Value = totalOverallGrossMargin;
                gmReportWorksheet.Cells[rowForSummary, 7].Value = totalOverallNetOfFreight;
                gmReportWorksheet.Cells[rowForSummary, 8].Value = totalOverallCommission;
                gmReportWorksheet.Cells[rowForSummary, 9].Value = totalOverallNetMargin;
                gmReportWorksheet.Cells[rowForSummary, 10].Value = totalOverallNetMarginPerLiter;

                gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 12].Value = totalQuantityForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 13].Value = totalNetOfSalesForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 14].Value = totalNetOfPurchasesForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 15].Value = totalGrossMarginForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 16].Value = totalNetOfFreightForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 17].Value = totalCommissionForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 18].Value = totalNetMarginForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 19].Value = totalNetMarginPerLiterForBiodiesel;

                gmReportWorksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 19].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 21].Value = totalQuantityForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 22].Value = totalNetOfSalesForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 23].Value = totalNetOfPurchasesForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 24].Value = totalGrossMarginForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 25].Value = totalNetOfFreightForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 26].Value = totalCommissionForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 27].Value = totalNetMarginForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 28].Value = totalNetMarginPerLiterForEconogas;

                gmReportWorksheet.Cells[rowForSummary, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 28].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 30].Value = totalQuantityForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 31].Value = totalNetOfSalesForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 32].Value = totalNetOfPurchasesForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 33].Value = totalGrossMarginForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 34].Value = totalNetOfFreightForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 35].Value = totalCommissionForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 36].Value = totalNetMarginForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 37].Value = totalNetMarginPerLiterForEnvirogas;

                gmReportWorksheet.Cells[rowForSummary, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 31].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 32].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 33].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 34].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 35].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 36].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 37].Style.Numberformat.Format = currencyFormat;

                #endregion == Summary Contents ==

                // Auto-fit columns for better readability
                gmReportWorksheet.Cells.AutoFitColumns();
                gmReportWorksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GM Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(GmReport));
            }
        }

        #endregion -- Generate GM Report Excel File --

        #region -- Generate Posted Collection Excel File --

        public async Task<IActionResult> GeneratePostedCollectionExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(PostedCollection));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var collectionReceiptReport = await _unitOfWork.FilprideReport
                    .GetCollectionReceiptReport(model.DateFrom, model.DateTo, companyClaims);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("COLLECTION");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "COLLECTION";
                mergedCells.Style.Font.Size = 16;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "CUSTOMER No.";
                worksheet.Cells["B7"].Value = "CUSTOMER NAME";
                worksheet.Cells["C7"].Value = "ACCT. TYPE";
                worksheet.Cells["D7"].Value = "TRAN. DATE (INV)";
                worksheet.Cells["E7"].Value = "INVOICE No.";
                worksheet.Cells["F7"].Value = "DUE DATE";
                worksheet.Cells["G7"].Value = "DATE OF CHECK";
                worksheet.Cells["H7"].Value = "BANK";
                worksheet.Cells["I7"].Value = "CHECK No.";
                worksheet.Cells["J7"].Value = "AMOUNT";

                var headerCells = worksheet.Cells["A7:J7"];
                headerCells.Style.Font.Size = 11;
                headerCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCells.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
                headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerCells.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                headerCells.Style.Font.Bold = true;

                // initializing records entry
                int row = 8;
                int startingRow = row - 1;
                string currencyFormat = "#,##0.00";
                decimal currentAmount = 0;
                decimal totalAmount = 0;

                foreach (var cr in collectionReceiptReport)
                {
                    currentAmount = cr.CashAmount + cr.CheckAmount;
                    worksheet.Cells[row, 1].Value = cr.Customer?.CustomerCode ?? "";
                    worksheet.Cells[row, 2].Value = cr.Customer?.CustomerName ?? "";
                    worksheet.Cells[row, 3].Value = cr.Customer?.CustomerType ?? "";
                    worksheet.Cells[row, 4].Value = cr.SalesInvoice?.TransactionDate ?? default;
                    worksheet.Cells[row, 5].Value = cr.SalesInvoice?.SalesInvoiceNo ?? "";
                    worksheet.Cells[row, 6].Value = cr.SalesInvoice?.DueDate ?? default;
                    worksheet.Cells[row, 7].Value = cr.CheckDate;
                    worksheet.Cells[row, 8].Value = cr.BankId;
                    worksheet.Cells[row, 9].Value = cr.CheckNo;
                    worksheet.Cells[row, 10].Value = currentAmount;

                    worksheet.Cells[row, 4].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                    totalAmount += currentAmount;
                    row++;
                }

                worksheet.Cells[row, 9].Value = "Total:";
                worksheet.Cells[row, 10].Value = totalAmount;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                using (var range = worksheet.Cells[row, 1, row, 10])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                using (var range = worksheet.Cells[row, 9, row, 10])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Font.Bold = true;
                }

                int lastRow = row - 1;

                using (var range = worksheet.Cells[startingRow - 1, 10, lastRow, 10])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                worksheet.Cells.AutoFitColumns();

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Collection Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                ViewData["error"] = ex.Message;

                return RedirectToAction(nameof(PostedCollection));
            }
        }

        #endregion -- Generate Posted Collection Excel File --

        #region -- Generate AP Trade Report Excel File --

        public async Task<IActionResult> GenerateTradePayableReport(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(TradePayableReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                string currencyFormat = "#,##0.00";

                var tradePayableReport = await _unitOfWork.FilprideReport
                    .GetTradePayableReport(model.DateFrom, model.DateTo, companyClaims);

                var allReportMonthYear = tradePayableReport.GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                var beginning = tradePayableReport
                    .Where(rr => rr.Date < dateFrom)
                    .Where(rr => rr.IsPaid == false)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                var beginningPaid = tradePayableReport
                    .Where(rr => rr.Date < dateFrom)
                    .Where(rr => rr.IsPaid == true)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                var purchases = tradePayableReport
                    .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                var purchasesPaid = tradePayableReport
                    .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo)
                    .Where(rr => rr.IsPaid == true)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Trade Payable");

                #region == Title ==
                var titleCells = worksheet.Cells["A1:B1"];
                titleCells.Merge = true;
                titleCells.Value = "TRADE PAYABLE REPORT";
                titleCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                #endregion

                #region == Header Row ==
                titleCells = worksheet.Cells["A7:B7"];
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A7"].Value = "MONTH";
                worksheet.Cells["A7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["B7"].Value = "SUPPLIER";
                worksheet.Cells["B7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                titleCells = worksheet.Cells["A6:B6"];
                titleCells.Merge = true;
                titleCells.Value = "APTRADE";
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                string[] headers = { "BEGINNING", "PURCHASES", "PAYMENTS", "ENDING" };
                string[] subHeaders = { "VOLUME", "GROSS", "EWT", "NET AMOUNT" };
                int col = 4;

                foreach (var header in headers)
                {
                    foreach (var subheader in subHeaders)
                    {
                        worksheet.Cells[7, col].Value = subheader;
                        worksheet.Cells[7, col].Style.Font.Bold = true;
                        worksheet.Cells[7, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[7, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = col + 1;
                    }

                    titleCells = worksheet.Cells[6, col - 4, 6, col - 1];
                    titleCells.Merge = true;
                    titleCells.Value = header;
                    titleCells.Style.Font.Size = 13;
                    titleCells.Style.Font.Bold = true;
                    titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                    titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    col = col + 1;
                }
                #endregion

                int row = 8;
                IEnumerable<IGrouping<object, FilprideReceivingReport>> categorized = null!;

                #region == Initialize Variables ==

                // initialize subtotals for month/year
                decimal subtotalVolumeBeginning = 0m;
                decimal subtotalGrossBeginning = 0m;
                decimal subtotalEwtBeginning = 0m;
                decimal subtotalNetBeginning = 0m;
                decimal subtotalVolumePurchases = 0m;
                decimal subtotalGrossPurchases = 0m;
                decimal subtotalEwtPurchases = 0m;
                decimal subtotalNetPurchases = 0m;
                decimal subtotalVolumePayments = 0m;
                decimal subtotalGrossPayments = 0m;
                decimal subtotalEwtPayments = 0m;
                decimal subtotalNetPayments = 0m;
                decimal currentVolumeEnding = 0m;
                decimal currentGrossEnding = 0m;
                decimal currentEwtEnding = 0m;
                decimal currentNetEnding = 0m;
                decimal subtotalVolumeEnding = 0m;
                decimal subtotalGrossEnding = 0m;
                decimal subtotalEwtEnding = 0m;
                decimal subtotalNetEnding = 0m;

                decimal grandTotalVolumeBeginning = 0m;
                decimal grandTotalGrossBeginning = 0m;
                decimal grandTotalEwtBeginning = 0m;
                decimal grandTotalNetBeginning = 0m;

                decimal grandTotalVolumePurchases = 0m;
                decimal grandTotalGrossPurchases = 0m;
                decimal grandTotalEwtPurchases = 0m;
                decimal grandTotalNetPurchases = 0m;

                decimal grandTotalVolumePayments = 0m;
                decimal grandTotalGrossPayments = 0m;
                decimal grandTotalEwtPayments = 0m;
                decimal grandTotalNetPayments = 0m;

                decimal grandTotalVolumeEnding = 0m;
                decimal grandTotalGrossEnding = 0m;
                decimal grandTotalEwtEnding = 0m;
                decimal grandTotalNetEnding = 0m;

                #endregion

                // loop for each month
                foreach (var monthYear in allReportMonthYear)
                {
                    // reset placing per category
                    int i = 0;

                    // get the month-year then group by supplier
                    var allSupplier = monthYear.GroupBy(rr => rr.PurchaseOrder?.Supplier?.SupplierName);

                    // enter the month-year to the excel
                    worksheet.Cells[row, 1].Value = (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(allSupplier?.FirstOrDefault()?.FirstOrDefault()?.Date.Month ?? 0) ?? " ")
                                                    + " " +
                                                    (allSupplier?.FirstOrDefault()?.FirstOrDefault()?.Date.Year.ToString() ?? " ");
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    row++;

                    // get the int of the current month-year to be used in condition
                    var monthInt = allSupplier?.FirstOrDefault()?.FirstOrDefault()?.Date.Month ?? 0;
                    var yearInt = allSupplier?.FirstOrDefault()?.FirstOrDefault()?.Date.Year ?? 0;


                    // in the month-year loop by supplier
                    foreach (var supplier in allSupplier!)
                    {
                        // when starting in new supplier, go to beginning
                        i = 1;

                        // write the name of supplier
                        var supplierName = supplier.FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName ?? "";
                        worksheet.Cells[row, 2].Value = supplierName;
                        string whichPayment = string.Empty;
                        bool forPayment = false;
                        bool forEnding = false;

                        // make loop for the two categories
                        for (i = 1; i != 5; i++)
                        {
                            // decide category to use either beginning or purchase
                            switch (i)
                            {
                                case 1:
                                    categorized = beginning;
                                    whichPayment = "beginning";
                                    break;
                                case 2:
                                    categorized = purchases;
                                    whichPayment = "purchases";
                                    break;
                                case 3:
                                    forPayment = true;
                                    break;
                                case 4:
                                    forEnding = true;
                                    break;
                            }

                            if (forPayment)
                            {
                                switch (whichPayment)
                                {
                                    case "beginning":
                                        categorized = beginningPaid;
                                        break;
                                    case "purchases":
                                        categorized = purchasesPaid;
                                        break;
                                }
                            }

                            if (categorized != null)
                            {
                                // iterate through the date range, find which month-year
                                foreach (var monthYearChoice in categorized)
                                {
                                    // if the month-year was found in iteration, write
                                    if (monthYearChoice.FirstOrDefault()?.Date.Month == monthYear.FirstOrDefault()?.Date.Month &&
                                        monthYearChoice.FirstOrDefault()?.Date.Year == monthYear.FirstOrDefault()?.Date.Year)
                                    {
                                        // write the data per category
                                        decimal gross = monthYearChoice
                                            .Where(rr => rr.Date.Month == monthInt && rr.Date.Year == yearInt)
                                            .Where(rr => rr.PurchaseOrder?.Supplier?.SupplierName == supplier
                                                .FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName)
                                            .Sum(rr => rr.Amount);
                                        decimal volume = monthYearChoice
                                            .Where(rr => rr.Date.Month == monthInt && rr.Date.Year == yearInt)
                                            .Where(rr => rr.PurchaseOrder?.Supplier?.SupplierName == supplier
                                                .FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName)
                                            .Sum(rr => rr.QuantityReceived);
                                        decimal ewt = (gross / 1.12m) * 0.01m;
                                        decimal net = gross - ewt;

                                        worksheet.Cells[row, (i * 5) - 1].Value = volume;
                                        worksheet.Cells[row, (i * 5)].Value = gross;
                                        worksheet.Cells[row, (i * 5) + 1].Value = ewt;
                                        worksheet.Cells[row, (i * 5) + 2].Value = net;
                                        worksheet.Cells[row, (i * 5) - 1].Style.Numberformat.Format = currencyFormat;
                                        worksheet.Cells[row, (i * 5)].Style.Numberformat.Format = currencyFormat;
                                        worksheet.Cells[row, (i * 5) + 1].Style.Numberformat.Format = currencyFormat;
                                        worksheet.Cells[row, (i * 5) + 2].Style.Numberformat.Format = currencyFormat;

                                        // add to subtotals
                                        if (i == 1)
                                        {
                                            subtotalVolumeBeginning = subtotalVolumeBeginning + volume;
                                            subtotalGrossBeginning = subtotalGrossBeginning + gross;
                                            subtotalEwtBeginning = subtotalEwtBeginning + ewt;
                                            subtotalNetBeginning = subtotalNetBeginning + net;
                                            currentVolumeEnding = currentVolumeEnding + volume;
                                            currentGrossEnding = currentGrossEnding + gross;
                                            currentEwtEnding = currentEwtEnding + ewt;
                                            currentNetEnding = currentNetEnding + net;
                                        }
                                        if (i == 2)
                                        {
                                            subtotalVolumePurchases = subtotalVolumePurchases + volume;
                                            subtotalGrossPurchases = subtotalGrossPurchases + gross;
                                            subtotalEwtPurchases = subtotalEwtPurchases + ewt;
                                            subtotalNetPurchases = subtotalNetPurchases + net;
                                            currentVolumeEnding = currentVolumeEnding + volume;
                                            currentGrossEnding = currentGrossEnding + gross;
                                            currentEwtEnding = currentEwtEnding + ewt;
                                            currentNetEnding = currentNetEnding + net;
                                        }
                                        if (i == 3)
                                        {
                                            subtotalVolumePayments = subtotalVolumePayments + volume;
                                            subtotalGrossPayments = subtotalGrossPayments + gross;
                                            subtotalEwtPayments = subtotalEwtPayments + ewt;
                                            subtotalNetPayments = subtotalNetPayments + net;
                                            currentVolumeEnding = currentVolumeEnding - volume;
                                            currentGrossEnding = currentGrossEnding - gross;
                                            currentEwtEnding = currentEwtEnding - ewt;
                                            currentNetEnding = currentNetEnding - net;
                                        }
                                    }
                                }
                            }

                            if (forEnding)
                            {
                                worksheet.Cells[row, 19].Value = currentVolumeEnding;
                                worksheet.Cells[row, 20].Value = currentGrossEnding;
                                worksheet.Cells[row, 21].Value = currentEwtEnding;
                                worksheet.Cells[row, 22].Value = currentNetEnding;
                                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                                currentVolumeEnding = 0m;
                                currentGrossEnding = 0m;
                                currentEwtEnding = 0m;
                                currentNetEnding = 0m;
                            }

                            forPayment = false;
                        }
                        // after the four categories, next supplier
                        row++;
                    }

                    #region == Subtotal Inputting ==
                    // after all supplier, input subtotals if not zero
                    if (subtotalGrossBeginning != 0m)
                    {
                        worksheet.Cells[row, 4].Value = subtotalVolumeBeginning;
                        worksheet.Cells[row, 5].Value = subtotalGrossBeginning;
                        worksheet.Cells[row, 6].Value = subtotalEwtBeginning;
                        worksheet.Cells[row, 7].Value = subtotalNetBeginning;

                        using (var range = worksheet.Cells[row, 4, row, 7])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Numberformat.Format = currencyFormat;
                        }
                    }
                    if (subtotalGrossPurchases != 0m)
                    {
                        worksheet.Cells[row, 9].Value = subtotalVolumePurchases;
                        worksheet.Cells[row, 10].Value = subtotalGrossPurchases;
                        worksheet.Cells[row, 11].Value = subtotalEwtPurchases;
                        worksheet.Cells[row, 12].Value = subtotalNetPurchases;

                        using (var range = worksheet.Cells[row, 9, row, 12])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Numberformat.Format = currencyFormat;
                        }
                    }
                    if (subtotalGrossPayments != 0m)
                    {
                        worksheet.Cells[row, 14].Value = subtotalVolumePayments;
                        worksheet.Cells[row, 15].Value = subtotalGrossPayments;
                        worksheet.Cells[row, 16].Value = subtotalEwtPayments;
                        worksheet.Cells[row, 17].Value = subtotalNetPayments;

                        using (var range = worksheet.Cells[row, 14, row, 17])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Numberformat.Format = currencyFormat;
                        }
                    }
                    #endregion

                    #region == Ending Subtotal and Grand Total Processes ==
                    // input subtotal of ending
                    subtotalVolumeEnding = subtotalVolumeBeginning + subtotalVolumePurchases - subtotalVolumePayments;
                    subtotalGrossEnding = subtotalGrossBeginning + subtotalGrossPurchases - subtotalGrossPayments;
                    subtotalEwtEnding = subtotalEwtBeginning + subtotalEwtPurchases - subtotalEwtPayments;
                    subtotalNetEnding = subtotalNetBeginning + subtotalNetPurchases - subtotalNetPayments;

                    worksheet.Cells[row, 19].Value = subtotalVolumeEnding;
                    worksheet.Cells[row, 20].Value = subtotalGrossEnding;
                    worksheet.Cells[row, 21].Value = subtotalEwtEnding;
                    worksheet.Cells[row, 22].Value = subtotalNetEnding;
                    using (var range = worksheet.Cells[row, 19, row, 22])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }

                    // after inputting all subtotals, next row
                    row++;

                    // after inputting all subtotals, add subtotals to grandtotal
                    grandTotalVolumeBeginning = grandTotalVolumeBeginning + subtotalVolumeBeginning;
                    grandTotalGrossBeginning = grandTotalGrossBeginning + subtotalGrossBeginning;
                    grandTotalEwtBeginning = grandTotalEwtBeginning + subtotalEwtBeginning;
                    grandTotalNetBeginning = grandTotalNetBeginning + subtotalNetBeginning;

                    grandTotalVolumePurchases = grandTotalVolumePurchases + subtotalVolumePurchases;
                    grandTotalGrossPurchases = grandTotalGrossPurchases + subtotalGrossPurchases;
                    grandTotalEwtPurchases = grandTotalEwtPurchases + subtotalEwtPurchases;
                    grandTotalNetPurchases = grandTotalNetPurchases + subtotalNetPurchases;

                    grandTotalVolumePayments = grandTotalVolumePayments + subtotalVolumePayments;
                    grandTotalGrossPayments = grandTotalGrossPayments + subtotalGrossPayments;
                    grandTotalEwtPayments = grandTotalEwtPayments + subtotalEwtPayments;
                    grandTotalNetPayments = grandTotalNetPayments + subtotalNetPayments;

                    grandTotalVolumeEnding = grandTotalVolumeEnding + subtotalVolumeEnding;
                    grandTotalGrossEnding = grandTotalGrossEnding + subtotalGrossEnding;
                    grandTotalEwtEnding = grandTotalEwtEnding + subtotalEwtEnding;
                    grandTotalNetEnding = grandTotalNetEnding + subtotalNetEnding;

                    // reset subtotals
                    subtotalVolumePurchases = 0m;
                    subtotalGrossPurchases = 0m;
                    subtotalEwtPurchases = 0m;
                    subtotalNetPurchases = 0m;
                    subtotalVolumeBeginning = 0m;
                    subtotalGrossBeginning = 0m;
                    subtotalEwtBeginning = 0m;
                    subtotalNetBeginning = 0m;
                    currentVolumeEnding = 0m;
                    currentGrossEnding = 0m;
                    currentEwtEnding = 0m;
                    currentNetEnding = 0m;
                    subtotalVolumePayments = 0m;
                    subtotalGrossPayments = 0m;
                    subtotalEwtPayments = 0m;
                    subtotalNetPayments = 0m;

                    #endregion

                }

                row++;

                #region == Grand Total Inputting ==
                worksheet.Cells[row, 2].Value = "GRAND TOTALS:";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 4].Value = grandTotalVolumeBeginning;
                worksheet.Cells[row, 5].Value = grandTotalGrossBeginning;
                worksheet.Cells[row, 6].Value = grandTotalEwtBeginning;
                worksheet.Cells[row, 7].Value = grandTotalNetBeginning;
                worksheet.Cells[row, 9].Value = grandTotalVolumePurchases;
                worksheet.Cells[row, 10].Value = grandTotalGrossPurchases;
                worksheet.Cells[row, 11].Value = grandTotalEwtPurchases;
                worksheet.Cells[row, 12].Value = grandTotalNetPurchases;
                worksheet.Cells[row, 14].Value = grandTotalVolumePayments;
                worksheet.Cells[row, 15].Value = grandTotalGrossPayments;
                worksheet.Cells[row, 16].Value = grandTotalEwtPayments;
                worksheet.Cells[row, 17].Value = grandTotalNetPayments;
                worksheet.Cells[row, 19].Value = grandTotalVolumeEnding;
                worksheet.Cells[row, 20].Value = grandTotalGrossEnding;
                worksheet.Cells[row, 21].Value = grandTotalEwtEnding;
                worksheet.Cells[row, 22].Value = grandTotalNetEnding;
                using (var range = worksheet.Cells[row, 4, row, 22])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Numberformat.Format = currencyFormat;
                }
                using (var range = worksheet.Cells[row, 1, row, 22])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }
                #endregion

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(3).Width = 1;
                worksheet.Column(8).Width = 1;
                worksheet.Column(13).Width = 1;
                worksheet.Column(18).Width = 1;

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Trade Payable Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(TradePayableReport));
            }

        }

        #endregion -- Generate AP Trade Report --

        #region -- Generate Ap Report Excel File --

        [HttpPost]
        public async Task<IActionResult> ApReportExcelFile(DateOnly monthYear, CancellationToken cancellationToken)
        {
            try
            {
                if (monthYear == default)
                {
                    TempData["error"] = "Please enter a valid month";
                    return RedirectToAction(nameof(ApReport));
                }

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";

                // fetch for this month and back
                var apReport = await _unitOfWork.FilprideReport.GetApReport(monthYear, companyClaims, cancellationToken);

                if (apReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ApReport));
                }

                #region == TOPSHEET ==

                // Create the Excel package
                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add("TOPSHEET");
                worksheet.Cells.Style.Font.Name = "Calibri";

                worksheet.Cells[1, 2].Value = "Summary of Purchases";
                worksheet.Cells[1, 2].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = $"AP Monitoring Report for the month of {monthYear.ToString("MMMM")} {monthYear.Year}";
                worksheet.Cells[3, 2].Value = "Filpride Resources, Inc.";
                worksheet.Cells[1, 2, 3, 2].Style.Font.Size = 14;

                worksheet.Cells[5, 2].Value = "SUPPLIER";
                worksheet.Cells[5, 3].Value = "BUYER";
                worksheet.Cells[5, 4].Value = "PRODUCT";
                worksheet.Cells[5, 5].Value = "PAYMENT TERMS";
                worksheet.Cells[5, 6].Value = "ORIGINAL PO VOLUME";
                worksheet.Cells[5, 7].Value = "UNLIFTED LAST MONTH";
                worksheet.Cells[5, 8].Value = "LIFTED THIS MONTH";
                worksheet.Cells[5, 9].Value = "UNLIFTED THIS MONTH";
                worksheet.Cells[5, 10].Value = "PRICE(VAT-EX)";
                worksheet.Cells[5, 11].Value = "PRICE (VAT-INC)";
                worksheet.Cells[5, 12].Value = "GROSS AMOUNT";
                worksheet.Cells[5, 13].Value = "EWT";
                worksheet.Cells[5, 14].Value = "NET OF EWT";

                using (var range = worksheet.Cells[5, 2, 5, 14])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255,204,172));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Row(5).Height = 36;

                var groupBySupplier = apReport
                    .OrderBy(po => po.Date)
                    .ThenBy(po => po.PurchaseOrderNo)
                    .GroupBy(po => po.Supplier);

                int row = 5;
                decimal originalPoGrandTotalBiodiesel = 0m;
                decimal originalPoGrandTotalEconogas = 0m;
                decimal originalPoGrandTotalEnvirogas = 0m;

                decimal unliftedLastMonthGrandTotalBiodiesel = 0m;
                decimal unliftedLastMonthGrandTotalEconogas = 0m;
                decimal unliftedLastMonthGrandTotalEnvirogas = 0m;

                decimal liftedThisMonthGrandTotalBiodiesel = 0m;
                decimal liftedThisMonthGrandTotalEconogas = 0m;
                decimal liftedThisMonthGrandTotalEnvirogas = 0m;

                decimal unliftedThisMonthGrandTotalBiodiesel = 0m;
                decimal unliftedThisMonthGrandTotalEconogas = 0m;
                decimal unliftedThisMonthGrandTotalEnvirogas = 0m;

                decimal grossAmountGrandTotalBiodiesel = 0m;
                decimal grossAmountGrandTotalEconogas = 0m;
                decimal grossAmountGrandTotalEnvirogas = 0m;

                decimal ewtGrandTotalBiodiesel = 0m;
                decimal ewtGrandTotalEconogas = 0m;
                decimal ewtGrandTotalEnvirogas = 0m;

                string[] productList = { "BIODIESEL", "ECONOGAS", "ENVIROGAS" };

                foreach (var sameSupplierGroup in groupBySupplier)
                {
                    // declare relevant variables per supplier
                    row += 2;
                    worksheet.Cells[row, 2].Value = sameSupplierGroup.First().Supplier!.SupplierName;
                    worksheet.Cells[row, 2].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Value = sameSupplierGroup.First().Company;
                    var groupByProduct = sameSupplierGroup.GroupBy(po => po.Product).OrderBy(po => po.Key?.ProductName);
                    decimal poSubtotal = 0m;
                    decimal unliftedLastMonthSubtotal = 0m;
                    decimal liftedThisMonthSubtotal = 0m;
                    decimal unliftedThisMonthSubtotal = 0m;
                    decimal grossAmountSubtotal = 0m;
                    decimal tempForGrandTotal = 0m;

                    foreach (var product in productList)
                    {
                        // declare per product
                        var aGroupByProduct = groupByProduct.FirstOrDefault(g => g.Key?.ProductName == product);
                        decimal grossOfLiftedThisMonth = 0m;
                        worksheet.Cells[row, 4].Value = product;
                        worksheet.Cells[row, 5].Value = groupByProduct.FirstOrDefault()?.FirstOrDefault()?.Supplier?.SupplierTerms;

                        // get the necessary values from po, separate it by variable
                        if (aGroupByProduct != null)
                        {
                            if (aGroupByProduct.Sum(po => po?.Quantity) != 0m)
                            {
                                // original po volume
                                var firstEntry = aGroupByProduct.FirstOrDefault();
                                decimal allPoTotal = 0m;
                                decimal unliftedLastMonth = 0m;
                                decimal liftedThisMonth = 0m;
                                decimal unliftedThisMonth = 0m;

                                foreach (var po in aGroupByProduct)
                                {
                                    decimal rrQtyForUnliftedLastMonth = 0m;
                                    decimal rrQtyForLiftedThisMonth = 0m;
                                    decimal currentPoQuantity = po.Quantity;
                                    allPoTotal += currentPoQuantity;

                                    if (po!.ReceivingReports!.Count != 0)
                                    {
                                        foreach (var rr in po!.ReceivingReports)
                                        {
                                            if (rr.Date < monthYear)
                                            {
                                                rrQtyForUnliftedLastMonth += rr.QuantityReceived;
                                            }
                                            if (rr.Date.Month == monthYear.Month && rr.Date.Year == monthYear.Year)
                                            {
                                                rrQtyForLiftedThisMonth += rr.QuantityReceived;
                                            }
                                        }
                                    }

                                    unliftedLastMonth += currentPoQuantity - rrQtyForUnliftedLastMonth;
                                    liftedThisMonth += rrQtyForLiftedThisMonth;
                                    unliftedThisMonth += currentPoQuantity - rrQtyForLiftedThisMonth -
                                                        rrQtyForUnliftedLastMonth;
                                }

                                if (allPoTotal != 0m)
                                {
                                    poSubtotal += allPoTotal;
                                    tempForGrandTotal += allPoTotal;
                                }

                                // operations per product
                                grossOfLiftedThisMonth = firstEntry!.Price * liftedThisMonth;
                                var ewt = grossOfLiftedThisMonth / 1.12m * 0.01m;

                                // WRITE ORIGINAL PO VOLUME
                                worksheet.Cells[row, 6].Value = allPoTotal;
                                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;

                                // WRITE UNLIFTED LAST MONTH
                                if (unliftedLastMonth != 0m)
                                {
                                    worksheet.Cells[row, 7].Value = unliftedLastMonth;
                                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 7].Value = 0m;
                                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE LIFTED THIS MONTH
                                if (liftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 8].Value = liftedThisMonth;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 8].Value = 0m;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE UNLIFTED THIS MONTH
                                if (unliftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 9].Value = unliftedThisMonth;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 9].Value = 0m;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // operations for grandtotals
                                switch (product)
                                {
                                    case "BIODIESEL":
                                        unliftedLastMonthGrandTotalBiodiesel += unliftedLastMonth;
                                        liftedThisMonthGrandTotalBiodiesel += liftedThisMonth;
                                        unliftedThisMonthGrandTotalBiodiesel += unliftedThisMonth;
                                        grossAmountGrandTotalBiodiesel += grossOfLiftedThisMonth;
                                        ewtGrandTotalBiodiesel += ewt;
                                        break;
                                    case "ECONOGAS":
                                        unliftedLastMonthGrandTotalEconogas += unliftedLastMonth;
                                        liftedThisMonthGrandTotalEconogas += liftedThisMonth;
                                        unliftedThisMonthGrandTotalEconogas += unliftedThisMonth;
                                        grossAmountGrandTotalEconogas += grossOfLiftedThisMonth;
                                        ewtGrandTotalEconogas += ewt;
                                        break;
                                    case "ENVIROGAS":
                                        unliftedLastMonthGrandTotalEnvirogas += unliftedLastMonth;
                                        liftedThisMonthGrandTotalEnvirogas += liftedThisMonth;
                                        unliftedThisMonthGrandTotalEnvirogas += unliftedThisMonth;
                                        grossAmountGrandTotalEnvirogas += grossOfLiftedThisMonth;
                                        ewtGrandTotalEnvirogas += ewt;
                                        break;
                                }

                                // operations for subtotals
                                unliftedLastMonthSubtotal += unliftedLastMonth;
                                liftedThisMonthSubtotal += liftedThisMonth;
                                unliftedThisMonthSubtotal += unliftedThisMonth;
                                grossAmountSubtotal += grossOfLiftedThisMonth;

                                // write per product: price, gross, ewt, net
                                worksheet.Cells[row, 10].Value = firstEntry!.Price / 1.12m;
                                worksheet.Cells[row, 11].Value = firstEntry!.Price;
                                worksheet.Cells[row, 12].Value = grossOfLiftedThisMonth;
                                worksheet.Cells[row, 13].Value = ewt;
                                worksheet.Cells[row, 14].Value = grossOfLiftedThisMonth - ewt;
                                using ( var range = worksheet.Cells[row, 10, row, 14] )
                                {
                                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                            }
                        }

                        switch (product)
                        {
                            case "BIODIESEL":
                                originalPoGrandTotalBiodiesel += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;
                            case "ECONOGAS":
                                originalPoGrandTotalEconogas += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;
                            case "ENVIROGAS":
                                originalPoGrandTotalEnvirogas += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;

                        }

                        row++;
                    }

                    worksheet.Cells[row, 3].Value = "SUB-TOTAL";
                    worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                    worksheet.Cells[row, 6].Value = poSubtotal;
                    worksheet.Cells[row, 7].Value = unliftedLastMonthSubtotal;
                    worksheet.Cells[row, 8].Value = liftedThisMonthSubtotal;
                    worksheet.Cells[row, 9].Value = unliftedThisMonthSubtotal;
                    if (liftedThisMonthSubtotal != 0)
                    {
                        worksheet.Cells[row, 10].Value = ((grossAmountSubtotal / liftedThisMonthSubtotal) / 1.12m);
                        worksheet.Cells[row, 11].Value = grossAmountSubtotal / liftedThisMonthSubtotal;
                        worksheet.Cells[row, 12].Value = grossAmountSubtotal;
                        worksheet.Cells[row, 13].Value = (grossAmountSubtotal / 1.12m * 0.01m);
                        worksheet.Cells[row, 14].Value = (grossAmountSubtotal - (grossAmountSubtotal / 1.12m * 0.01m));
                    }
                    else
                    {
                        worksheet.Cells[row, 10].Value = 0m;
                        worksheet.Cells[row, 11].Value = 0m;
                        worksheet.Cells[row, 12].Value = 0m;
                        worksheet.Cells[row, 13].Value = 0m;
                        worksheet.Cells[row, 14].Value = 0m;
                    }

                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    using (var range = worksheet.Cells[row, 3, row, 14])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Font.Bold = true;
                    }
                }

                row += 2;
                worksheet.Cells[row, 2].Value = "ALL SUPPLIERS";
                worksheet.Cells[row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Value = "FILPRIDE";

                decimal finalPo = originalPoGrandTotalBiodiesel + originalPoGrandTotalEconogas +
                                  originalPoGrandTotalEnvirogas;
                decimal finalUnliftedLastMonth = unliftedLastMonthGrandTotalBiodiesel +
                                                 unliftedLastMonthGrandTotalEconogas + originalPoGrandTotalEnvirogas;
                decimal finalLiftedThisMonth = liftedThisMonthGrandTotalBiodiesel + liftedThisMonthGrandTotalEconogas +
                                               liftedThisMonthGrandTotalEnvirogas;
                decimal finalUnliftedThisMonth = unliftedThisMonthGrandTotalBiodiesel +
                                                 unliftedThisMonthGrandTotalEconogas +
                                                 unliftedThisMonthGrandTotalEnvirogas;
                decimal finalGross = grossAmountGrandTotalBiodiesel + grossAmountGrandTotalEconogas +
                                     grossAmountGrandTotalEnvirogas;
                decimal finalEwt = ewtGrandTotalBiodiesel + ewtGrandTotalEconogas + ewtGrandTotalEnvirogas;

                foreach (var product in productList)
                {
                    worksheet.Cells[row, 4].Value = product;
                    worksheet.Cells[row, 5].Value = "ALL TERMS";

                    switch (product)
                    {
                        case "BIODIESEL":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalBiodiesel;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalBiodiesel;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalBiodiesel;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalBiodiesel;
                            if (liftedThisMonthGrandTotalBiodiesel != 0)
                            {
                                worksheet.Cells[row, 10].Value = (grossAmountGrandTotalBiodiesel / liftedThisMonthGrandTotalBiodiesel) / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalBiodiesel / liftedThisMonthGrandTotalBiodiesel;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalBiodiesel;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalBiodiesel;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalBiodiesel - ewtGrandTotalBiodiesel;
                            break;
                        case "ECONOGAS":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalEconogas;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalEconogas;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalEconogas;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalEconogas;
                            if (liftedThisMonthGrandTotalEconogas != 0)
                            {
                                worksheet.Cells[row, 10].Value = (grossAmountGrandTotalEconogas / liftedThisMonthGrandTotalEconogas) / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalEconogas / liftedThisMonthGrandTotalEconogas;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalEconogas;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalEconogas;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalEconogas - ewtGrandTotalEconogas;
                            break;
                        case "ENVIROGAS":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalEnvirogas;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalEnvirogas;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalEnvirogas;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalEnvirogas;
                            if (liftedThisMonthGrandTotalEnvirogas != 0)
                            {
                                worksheet.Cells[row, 10].Value = (grossAmountGrandTotalEnvirogas / liftedThisMonthGrandTotalEnvirogas) / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalEnvirogas / liftedThisMonthGrandTotalEnvirogas;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalEnvirogas;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalEnvirogas;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalEnvirogas - ewtGrandTotalEnvirogas;
                            break;
                    }

                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    row++;
                }

                // final total
                worksheet.Cells[row, 3].Value = "GRAND-TOTAL";
                worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                worksheet.Cells[row, 6].Value = finalPo;
                worksheet.Cells[row, 7].Value = finalUnliftedLastMonth;
                worksheet.Cells[row, 8].Value = finalLiftedThisMonth;
                worksheet.Cells[row, 9].Value = finalUnliftedThisMonth;
                if (finalLiftedThisMonth != 0)
                {
                    worksheet.Cells[row, 10].Value = ((finalGross / finalLiftedThisMonth) / 1.12m);
                    worksheet.Cells[row, 11].Value = finalGross / finalLiftedThisMonth;
                    worksheet.Cells[row, 12].Value = finalGross;
                    worksheet.Cells[row, 13].Value = finalGross / 1.12m * 0.01m;
                    worksheet.Cells[row, 14].Value = finalGross - (finalGross / 1.12m * 0.01m);
                }
                else
                {
                    worksheet.Cells[row, 10].Value = 0m;
                    worksheet.Cells[row, 11].Value = 0m;
                    worksheet.Cells[row, 12].Value = 0m;
                    worksheet.Cells[row, 13].Value = 0m;
                    worksheet.Cells[row, 14].Value = 0m;
                }

                using (var range = worksheet.Cells[row, 6, row, 14])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                using (var range = worksheet.Cells[row, 3, row, 14])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Font.Bold = true;
                }

                row += 6;
                worksheet.Cells[row, 2].Value = "Prepared by:";
                worksheet.Cells[row, 5].Value = "Approved by:";
                worksheet.Cells[row, 8].Value = "Acknowledged by:";
                worksheet.Cells[row, 11].Value = "Received by:";
                row += 3;
                worksheet.Cells[row, 2].Value = "Gerecho B. Tayco";
                worksheet.Cells[row, 5].Value = "Clifford M. Aranda";
                worksheet.Cells[row, 8].Value = "Aniebeth S. Dionzon";
                worksheet.Cells[row, 11].Value = "Jerrylyn B. Gatoc";
                using (var range = worksheet.Cells[row, 1, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.UnderLine = true;
                }
                row++;
                worksheet.Cells[row, 2].Value = "Pricing Specialist";
                worksheet.Cells[row, 5].Value = "Operations Manager";
                worksheet.Cells[row, 8].Value = "Chief Operating Officer";
                worksheet.Cells[row, 11].Value = "Finance Manager";

                worksheet.Columns.AutoFit();
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 30;

                #endregion == TOPSHEET ==

                #region == BY SUPPLIER ==

                foreach (var aGroupBySupplier in groupBySupplier)
                {
                    var firstRecord = aGroupBySupplier.FirstOrDefault();
                    DateOnly monthYearTemp = new DateOnly(monthYear.Year, (monthYear.Month), 1);
                    DateOnly lastDayOfMonth = monthYearTemp.AddDays(-1);
                    var poGrandTotal = 0m;
                    var unliftedLastMonthGrandTotal = 0m;
                    var liftedThisMonthGrandTotal = 0m;
                    var unliftedThisMonthGrandTotal = 0m;
                    var grossAmountGrandTotal = 0m;
                    var ewtGrandTotal = 0m;
                    var netOfEwtGrandTotal = 0m;

                    worksheet = package.Workbook.Worksheets.Add(firstRecord!.Supplier!.SupplierName);
                    worksheet.Cells.Style.Font.Name = "Calibri";
                    worksheet.Cells[1, 1].Value = $"SUPPLIER: {firstRecord!.Supplier!.SupplierName}";
                    worksheet.Cells[2, 1].Value = "AP MONITORING REPORT (TRADE & SUPPLY GENERATED: PER PO #)";
                    worksheet.Cells[3, 1].Value = "REF: PURCHASE ORDER REPORT-per INTEGRATED BUSINESS SYSTEM";
                    worksheet.Cells[4, 1].Value = $"FOR THE MONTH OF {monthYear.ToString("MMMM")} {monthYear.Year.ToString()}";
                    worksheet.Cells[5, 1].Value = $"DUE DATE: {lastDayOfMonth.ToString("MMMM dd, yyyy")}";
                    worksheet.Cells[1, 1, 5, 1].Style.Font.Bold = true;
                    row = 8;
                    var groupByProduct = aGroupBySupplier.GroupBy(po => po.Product!.ProductName).ToList();

                    foreach (string product in productList)
                    {
                        var aGroupByProduct = groupByProduct.FirstOrDefault(g => g.FirstOrDefault()!.Product!.ProductName == product);

                        if (aGroupByProduct != null)
                        {
                            var poSubtotal = 0m;
                            var unliftedLastMonthSubtotal = 0m;
                            var liftedThisMonthSubtotal = 0m;
                            var unliftedThisMonthSubtotal = 0m;
                            var grossAmountSubtotal = 0m;
                            var ewtSubtotal = 0m;
                            var netOfEwtSubtotal = 0m;

                            worksheet.Cells[row, 1].Value = "PO#";
                            worksheet.Cells[row, 2].Value = "DATE";
                            worksheet.Cells[row, 3].Value = "PRODUCT";
                            worksheet.Cells[row, 4].Value = "PORT";
                            worksheet.Cells[row, 5].Value = "REFERENCE MOPS";
                            worksheet.Cells[row, 6].Value = "ORIGINAL PO VOLUME";
                            worksheet.Cells[row, 7].Value = "UNLIFTED LAST MONTH";
                            worksheet.Cells[row, 8].Value = "LIFTED THIS MONTH";
                            worksheet.Cells[row, 9].Value = "UNLIFTED THIS MONTH";
                            worksheet.Cells[row, 10].Value = "PRICE(VAT-EX)";
                            worksheet.Cells[row, 11].Value = "PRICE(VAT-INC)";
                            worksheet.Cells[row, 12].Value = "GROSS AMOUNT(VAT-INC)";
                            worksheet.Cells[row, 13].Value = "EWT";
                            worksheet.Cells[row, 14].Value = "NET OF EWT";

                            using (var range = worksheet.Cells[row, 1, row, 14])
                            {
                                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255,204,172));
                                range.Style.Font.Bold = true;
                            }

                            worksheet.Row(row).Height = 36;
                            row++;

                            foreach(var po in aGroupByProduct)
                            {
                                // computing the cells variables
                                var poTotal = po.Quantity;
                                decimal grossAmount = (po.Price * po.Quantity);
                                decimal ewt = (grossAmount / 1.12m * 0.01m);
                                decimal unliftedLastMonth = 0m;
                                decimal liftedLastMonthRrQty = 0m;
                                decimal liftedThisMonthRrQty = 0m;
                                decimal unliftedThisMonth = 0m;

                                if (po.ReceivingReports!.Count != 0)
                                {
                                    liftedLastMonthRrQty = po.ReceivingReports
                                        .Where(rr => rr.Date < monthYear)
                                        .Sum(rr => rr.QuantityReceived);

                                    unliftedLastMonth = poTotal - liftedLastMonthRrQty;

                                    liftedThisMonthRrQty = po.ReceivingReports
                                        .Where(rr => rr.Date.Month == monthYear.Month && rr.Date.Year == monthYear.Year)
                                        .Sum(rr => rr.QuantityReceived);

                                    unliftedThisMonth = unliftedLastMonth - liftedThisMonthRrQty;
                                }

                                // incrementing subtotals
                                poSubtotal += poTotal;
                                unliftedLastMonthSubtotal += unliftedLastMonth;
                                liftedThisMonthSubtotal += liftedThisMonthRrQty;
                                unliftedThisMonthSubtotal += unliftedThisMonth;
                                grossAmountSubtotal += grossAmount;
                                ewtSubtotal += ewt;
                                netOfEwtSubtotal += (grossAmountSubtotal - ewt);

                                // writing the values to cells
                                worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                                worksheet.Cells[row, 2].Value = po.Date.ToString("MM/dd/yyyy");
                                worksheet.Cells[row, 3].Value = po.Product!.ProductName;
                                worksheet.Cells[row, 4].Value = po.PickUpPoint!.Depot;
                                worksheet.Cells[row, 5].Value = po.TriggerDate != default ? $"TRIGGER {po.TriggerDate.ToString("MM.dd.yyyy")}" : "UNDETERMINED";
                                worksheet.Cells[row, 6].Value = poTotal;
                                worksheet.Cells[row, 7].Value = unliftedLastMonth;
                                worksheet.Cells[row, 8].Value = liftedThisMonthRrQty;
                                worksheet.Cells[row, 9].Value = unliftedThisMonth;
                                worksheet.Cells[row, 10].Value = (po.Price / 1.12m);
                                worksheet.Cells[row, 11].Value = po.Price;
                                worksheet.Cells[row, 12].Value = grossAmount;
                                worksheet.Cells[row, 13].Value = ewt;
                                worksheet.Cells[row, 14].Value = (grossAmount - ewt);

                                using (var range = worksheet.Cells[row, 6, row, 14])
                                {
                                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                row++;
                            }

                            // incrementing grandtotals
                            poGrandTotal += poSubtotal;
                            unliftedLastMonthGrandTotal += unliftedLastMonthSubtotal;
                            liftedThisMonthGrandTotal += liftedThisMonthSubtotal;
                            unliftedThisMonthGrandTotal += unliftedThisMonthSubtotal;
                            grossAmountGrandTotal += grossAmountSubtotal;
                            ewtGrandTotal += ewtSubtotal;
                            netOfEwtGrandTotal += netOfEwtSubtotal;

                            worksheet.Cells[row, 2].Value = "SUB-TOTAL";
                            worksheet.Cells[row, 6].Value = poSubtotal;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthSubtotal;
                            worksheet.Cells[row, 8].Value = liftedThisMonthSubtotal;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthSubtotal;
                            if (liftedThisMonthSubtotal != 0)
                            {
                                worksheet.Cells[row, 10].Value = ((grossAmountSubtotal / liftedThisMonthSubtotal) / 1.12m);
                                worksheet.Cells[row, 11].Value = (grossAmountSubtotal / liftedThisMonthSubtotal);
                            }
                            worksheet.Cells[row, 12].Value = grossAmountSubtotal;
                            worksheet.Cells[row, 13].Value = ewtSubtotal;
                            worksheet.Cells[row, 14].Value = (grossAmountSubtotal - ewtSubtotal);

                            using (var range = worksheet.Cells[row, 3, row, 5])
                            {
                                range.Merge = true;
                                range.Value = product;
                                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            using (var range = worksheet.Cells[row, 6, row, 14])
                            {
                                range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }
                            using (var range = worksheet.Cells[row, 1, row, 14])
                            {
                                range.Style.Font.Bold = true;
                                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            }

                            row += 2;
                        }
                    }

                    worksheet.Cells[row, 2].Value = "GRAND-TOTAL";
                    worksheet.Cells[row, 6].Value = poGrandTotal;
                    worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotal;
                    worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotal;
                    worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotal;
                    if (liftedThisMonthGrandTotal != 0)
                    {
                        worksheet.Cells[row, 10].Value = ((grossAmountGrandTotal / liftedThisMonthGrandTotal) / 1.12m);
                        worksheet.Cells[row, 11].Value = (grossAmountGrandTotal / liftedThisMonthGrandTotal);
                    }
                    worksheet.Cells[row, 12].Value = grossAmountGrandTotal;
                    worksheet.Cells[row, 13].Value = ewtGrandTotal;
                    worksheet.Cells[row, 14].Value = (grossAmountGrandTotal - ewtGrandTotal);

                    using (var range = worksheet.Cells[row, 3, row, 5])
                    {
                        range.Merge = true;
                        range.Value = "ALL PRODUCTS";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    using (var range = worksheet.Cells[row, 1, row, 14])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }


                    row += 6;
                    worksheet.Cells[row, 1].Value = "Note:   Volume paid is the volume recorded in the Purchase Journal Report.";
                    row += 3;
                    worksheet.Cells[row, 1].Value = "Prepared by:";
                    worksheet.Cells[row, 5].Value = "Approved by:";
                    worksheet.Cells[row, 8].Value = "Acknowledged by:";
                    row += 2;
                    worksheet.Cells[row, 1].Value = "Gerecho B. Tayco";
                    worksheet.Cells[row, 5].Value = "Clifford M. Aranda";
                    worksheet.Cells[row, 8].Value = "Aniebeth S. Dionzon";
                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.UnderLine = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    row++;
                    worksheet.Cells[row, 1].Value = "Pricing Specialist";
                    worksheet.Cells[row, 5].Value = "Operations Manager";
                    worksheet.Cells[row, 8].Value = "Chief Operating Officer";
                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    worksheet.Columns.AutoFit();
                    worksheet.Column(1).Width = 14;
                }

                #endregion == BY SUPPLIER ==

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ApReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(ApReport));
            }
        }

        #endregion

        #region -- Generate AR Per Customer Excel File --

        public async Task<IActionResult> GenerateArPerCustomerExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(ArPerCustomer));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var salesInvoice = await _dbContext.FilprideSalesInvoices
                    .Where(si => si.PostedBy != null && si.Company == companyClaims)
                    .Include(si => si.Product)
                    .Include(si => si.Customer)
                    .Include(si => si.DeliveryReceipt)
                    .Include(si => si.CustomerOrderSlip)
                    .ToListAsync(cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ArPerCustomer));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ARPerCustomer");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "AR PER CUSTOMER";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "CUSTOMER No.";
                worksheet.Cells["B7"].Value = "CUSTOMER NAME";
                worksheet.Cells["C7"].Value = "ACCT. TYPE";
                worksheet.Cells["D7"].Value = "TERMS";
                worksheet.Cells["E7"].Value = "TRAN. DATE";
                worksheet.Cells["F7"].Value = "DUE DATE";
                worksheet.Cells["G7"].Value = "INVOICE No.";
                worksheet.Cells["H7"].Value = "DR No.";
                worksheet.Cells["I7"].Value = "PO No.";
                worksheet.Cells["J7"].Value = "COS No.";
                worksheet.Cells["K7"].Value = "REMARKS";
                worksheet.Cells["L7"].Value = "PRODUCT";
                worksheet.Cells["M7"].Value = "QTY";
                worksheet.Cells["N7"].Value = "UNIT";
                worksheet.Cells["O7"].Value = "UNIT PRICE";
                worksheet.Cells["P7"].Value = "FREIGHT";
                worksheet.Cells["Q7"].Value = "FREIGHT/LTR";
                worksheet.Cells["R7"].Value = "VAT/LTR";
                worksheet.Cells["S7"].Value = "VAT AMT.";
                worksheet.Cells["T7"].Value = "TOTAL AMT. (G. VAT)";
                worksheet.Cells["U7"].Value = "AMT. PAID";
                worksheet.Cells["V7"].Value = "SI BALANCE";
                worksheet.Cells["W7"].Value = "EWT AMT.";
                worksheet.Cells["X7"].Value = "EWT PAID";
                worksheet.Cells["Y7"].Value = "CWT BALANCE";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:Y7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";

                var totalQuantity = 0m;
                var totalFreight = 0m;
                var totalFreightPerLiter = 0m;
                var totalVatPerLiter = 0m;
                var totalVatAmount = 0m;
                var totalGrossAmount = 0m;
                var totalAmountPaid = 0m;
                var totalBalance = 0m;
                var totalEwtAmount = 0m;
                var totalEwtAmountPaid = 0m;
                var totalewtBalance = 0m;

                foreach (var si in salesInvoice)
                {
                    var freight = si.DeliveryReceipt?.Freight * si.DeliveryReceipt?.Quantity;
                    var grossAmount = si.Amount;
                    var vatableAmount = grossAmount / 1.12m;
                    var vatAmount = vatableAmount * .12m;
                    var vatPerLiter = vatAmount * si.Quantity;
                    var ewtAmount = vatableAmount * .01m;
                    var isEwtAmountPaid = si.IsTaxAndVatPaid ? ewtAmount : 0m;
                    var ewtBalance = ewtAmount - isEwtAmountPaid;

                    worksheet.Cells[row, 1].Value = si.Customer?.CustomerCode;
                    worksheet.Cells[row, 2].Value = si.Customer?.CustomerName;
                    worksheet.Cells[row, 3].Value = si.Customer?.CustomerType;
                    worksheet.Cells[row, 4].Value = si.Terms;
                    worksheet.Cells[row, 5].Value = si.TransactionDate;
                    worksheet.Cells[row, 6].Value = si.DueDate;
                    worksheet.Cells[row, 7].Value = si.SalesInvoiceNo;
                    worksheet.Cells[row, 8].Value = si.DeliveryReceipt?.DeliveryReceiptNo;
                    worksheet.Cells[row, 9].Value = si.DeliveryReceipt?.CustomerOrderSlip?.CustomerPoNo;
                    worksheet.Cells[row, 10].Value = si.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[row, 11].Value = si.Remarks;
                    worksheet.Cells[row, 12].Value = si.Product?.ProductName;
                    worksheet.Cells[row, 13].Value = si.Quantity;
                    worksheet.Cells[row, 14].Value = si.Product?.ProductUnit;
                    worksheet.Cells[row, 15].Value = si.UnitPrice;
                    worksheet.Cells[row, 16].Value = freight;
                    worksheet.Cells[row, 17].Value = si.DeliveryReceipt?.Freight;
                    worksheet.Cells[row, 18].Value = vatPerLiter;
                    worksheet.Cells[row, 19].Value = vatAmount;
                    worksheet.Cells[row, 20].Value = grossAmount;
                    worksheet.Cells[row, 21].Value = si.AmountPaid;
                    worksheet.Cells[row, 22].Value = si.Balance;
                    worksheet.Cells[row, 23].Value = ewtAmount;
                    worksheet.Cells[row, 24].Value = isEwtAmountPaid;
                    worksheet.Cells[row, 25].Value = ewtBalance;

                    worksheet.Cells[row, 5].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    row++;

                    totalQuantity += si.Quantity;
                    totalFreight += freight ?? 0m;
                    totalFreightPerLiter += si.DeliveryReceipt?.Freight ?? 0m;
                    totalVatPerLiter += vatPerLiter;
                    totalVatAmount += vatAmount;
                    totalGrossAmount += grossAmount;
                    totalAmountPaid += si.AmountPaid;
                    totalBalance += si.Balance;
                    totalEwtAmount += ewtAmount;
                    totalEwtAmountPaid += isEwtAmountPaid;
                    totalewtBalance += ewtBalance;
                }

                worksheet.Cells[row, 12].Value = "Total ";

                worksheet.Cells[row, 13].Value = totalQuantity;
                worksheet.Cells[row, 15].Value = totalGrossAmount / totalQuantity;
                worksheet.Cells[row, 16].Value = totalFreight;
                worksheet.Cells[row, 17].Value = totalFreightPerLiter;
                worksheet.Cells[row, 18].Value = totalVatPerLiter;
                worksheet.Cells[row, 19].Value = totalVatAmount;
                worksheet.Cells[row, 20].Value = totalGrossAmount;
                worksheet.Cells[row, 21].Value = totalAmountPaid;
                worksheet.Cells[row, 22].Value = totalBalance;
                worksheet.Cells[row, 23].Value = totalEwtAmount;
                worksheet.Cells[row, 24].Value = totalEwtAmountPaid;
                worksheet.Cells[row, 25].Value = totalewtBalance;

                worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 25])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 12, row, 25])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ArPerCustomerReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(ArPerCustomer));
            }
        }

        #endregion

        #region -- Generate Aging Report Excel File --

        public async Task<IActionResult> GenerateAgingReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(AgingReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(this.User);
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(
                        si => si.PostedBy != null && si.AmountPaid == 0 && !si.IsPaid && si.Company == companyClaims,
                        cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(AgingReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("AgingReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "AGING REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "MONTH";
                worksheet.Cells["B7"].Value = "CUSTOMER NAME";
                worksheet.Cells["C7"].Value = "ACCT. TYPE";
                worksheet.Cells["D7"].Value = "TERMS";
                worksheet.Cells["E7"].Value = "EWT %";
                worksheet.Cells["F7"].Value = "SALES DATE";
                worksheet.Cells["G7"].Value = "DUE DATE";
                worksheet.Cells["H7"].Value = "INVOICE No.";
                worksheet.Cells["I7"].Value = "DR";
                worksheet.Cells["J7"].Value = "GROSS";
                worksheet.Cells["K7"].Value = "PARTIAL COLLECTIONS";
                worksheet.Cells["L7"].Value = "ADJUSTED GROSS";
                worksheet.Cells["M7"].Value = "EWT";
                worksheet.Cells["N7"].Value = "NET OF VAT";
                worksheet.Cells["O7"].Value = "VCF";
                worksheet.Cells["P7"].Value = "RETENTION AMOUNT";
                worksheet.Cells["Q7"].Value = "ADJUSTED NET";
                worksheet.Cells["R7"].Value = "DAYS DUE";
                worksheet.Cells["S7"].Value = "CURRENT";
                worksheet.Cells["T7"].Value = "1-30 DAYS";
                worksheet.Cells["U7"].Value = "31-60 DAYS";
                worksheet.Cells["V7"].Value = "61-90 DAYS";
                worksheet.Cells["W7"].Value = "OVER 90 DAYS";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:W7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.00";

                var totalGrossAmount = 0m;
                var totalAmountPaid = 0m;
                var totalAdjustedGross = 0m;
                var totalWithHoldingTaxAmount = 0m;
                var totalNetOfVatAmount = 0m;
                var totalVcfAmount = 0m;
                var totalRetentionAmount = 0m;
                var totalAdjustedNet = 0m;
                var totalCurrent = 0m;
                var totalOneToThirtyDays = 0m;
                var totalThirtyOneToSixtyDays = 0m;
                var totalSixtyOneToNinetyDays = 0m;
                var totalOverNinetyDays = 0m;
                foreach (var si in salesInvoice)
                {
                    var gross = si.Amount;
                    decimal netDiscount = si.Amount - si.Discount;
                    decimal netOfVatAmount =
                        si.Customer?.VatType == SD.VatType_Vatable ? netDiscount / 1.12m : netDiscount;
                    decimal withHoldingTaxAmount =
                        (si.Customer?.WithHoldingTax ?? false) ? (netDiscount / 1.12m) * 0.01m : 0;
                    decimal retentionAmount = (si.Customer?.RetentionRate ?? 0.0000m) * netOfVatAmount;
                    decimal vcfAmount = 0.0000m;
                    decimal adjustedGross = gross - vcfAmount;
                    decimal adjustedNet = gross - vcfAmount - retentionAmount;

                    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                    int daysDue = (today > si.DueDate) ? (today.DayNumber - si.DueDate.DayNumber) : 0;
                    var current = (si.DueDate >= today) ? gross : 0.0000m;
                    var oneToThirtyDays = (daysDue >= 1 && daysDue <= 30) ? gross : 0.0000m;
                    var thirtyOneToSixtyDays = (daysDue >= 31 && daysDue <= 60) ? gross : 0.0000m;
                    var sixtyOneToNinetyDays = (daysDue >= 61 && daysDue <= 90) ? gross : 0.0000m;
                    var overNinetyDays = (daysDue > 90) ? gross : 0.0000m;

                    worksheet.Cells[row, 1].Value = si.TransactionDate.ToString("MMM");
                    worksheet.Cells[row, 2].Value = si.Customer?.CustomerName;
                    worksheet.Cells[row, 3].Value = si.Customer?.CustomerType;
                    worksheet.Cells[row, 4].Value = si.Terms;
                    worksheet.Cells[row, 5].Value = (si.Customer?.WithHoldingTax ?? false) ? "1" : "0";
                    worksheet.Cells[row, 6].Value = si.TransactionDate;
                    worksheet.Cells[row, 7].Value = si.DueDate;
                    worksheet.Cells[row, 8].Value = si.SalesInvoiceNo;
                    worksheet.Cells[row, 9].Value = si.DeliveryReceipt?.DeliveryReceiptNo;
                    worksheet.Cells[row, 10].Value = gross;
                    worksheet.Cells[row, 11].Value = si.AmountPaid;
                    worksheet.Cells[row, 12].Value = adjustedGross;
                    worksheet.Cells[row, 13].Value = withHoldingTaxAmount;
                    worksheet.Cells[row, 14].Value = netOfVatAmount;
                    worksheet.Cells[row, 15].Value = vcfAmount;
                    worksheet.Cells[row, 16].Value = retentionAmount;
                    worksheet.Cells[row, 17].Value = adjustedNet;
                    worksheet.Cells[row, 18].Value = daysDue;
                    worksheet.Cells[row, 19].Value = current;
                    worksheet.Cells[row, 20].Value = oneToThirtyDays;
                    worksheet.Cells[row, 21].Value = thirtyOneToSixtyDays;
                    worksheet.Cells[row, 22].Value = sixtyOneToNinetyDays;
                    worksheet.Cells[row, 23].Value = overNinetyDays;

                    worksheet.Cells[row, 6].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;

                    row++;

                    totalGrossAmount += si.Amount;
                    totalAmountPaid += si.AmountPaid;
                    totalAdjustedGross += adjustedGross;
                    totalWithHoldingTaxAmount += withHoldingTaxAmount;
                    totalNetOfVatAmount += netOfVatAmount;
                    totalVcfAmount += vcfAmount;
                    totalRetentionAmount += retentionAmount;
                    totalAdjustedNet += adjustedNet;
                    totalCurrent += current;
                    totalOneToThirtyDays += oneToThirtyDays;
                    totalThirtyOneToSixtyDays += thirtyOneToSixtyDays;
                    totalSixtyOneToNinetyDays += sixtyOneToNinetyDays;
                    totalOverNinetyDays += overNinetyDays;
                }

                worksheet.Cells[row, 9].Value = "Total ";
                worksheet.Cells[row, 10].Value = totalGrossAmount;
                worksheet.Cells[row, 11].Value = totalAmountPaid;
                worksheet.Cells[row, 12].Value = totalAdjustedGross;
                worksheet.Cells[row, 13].Value = totalWithHoldingTaxAmount;
                worksheet.Cells[row, 14].Value = totalNetOfVatAmount;
                worksheet.Cells[row, 15].Value = totalVcfAmount;
                worksheet.Cells[row, 16].Value = totalRetentionAmount;
                worksheet.Cells[row, 17].Value = totalAdjustedNet;
                worksheet.Cells[row, 19].Value = totalCurrent;
                worksheet.Cells[row, 20].Value = totalOneToThirtyDays;
                worksheet.Cells[row, 21].Value = totalThirtyOneToSixtyDays;
                worksheet.Cells[row, 22].Value = totalSixtyOneToNinetyDays;
                worksheet.Cells[row, 23].Value = totalOverNinetyDays;

                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 23])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 9, row, 23])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"AgingReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(AgingReport));
            }
        }

        #endregion

        #region -- Generate COS Unserved Volume Excel File --

        public async Task<IActionResult> GenerateCOSUnservedVolumeToExcel(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom.ToString("MMMM dd, yyyy");
            ViewBag.DateTo = model.DateTo.ToString("MMMM dd, yyyy");
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo, companyClaims);

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("COS Unserved Volume");

                    // Setting header
                    worksheet.Cells["A1"].Value = "SUMMARY OF BOOKED SALES";
                    worksheet.Cells["A2"].Value = $"{ViewBag.DateFrom} - {ViewBag.DateTo}";
                    worksheet.Cells["A1:L1"].Merge = true;
                    worksheet.Cells["A1:L1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A1:L1"].Style.Font.Bold = true;

                    // Define table headers
                    var headers = new[]
                    {
                        "COS Date", "Customer", "Product", "P.O. No.",
                        "COS No.", "Price", "Unserved Volume", "Amount", "COS Status", "Exp of COS", "OTC COS No."
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[3, i + 1].Value = headers[i];
                        worksheet.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#9966ff"));
                        worksheet.Cells[3, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                    }

                    // Populate data rows
                    int row = 4;
                    string currencyFormat = "#,##0.0000";
                    string currencyFormatTwoDecimal = "#,##0.00";

                    var totalUnservedVolume = 0m;
                    var totalAmount = 0m;

                    foreach (var item in cosSummary)
                    {
                        var unservedVolume = item.Quantity - item.DeliveredQuantity;

                        worksheet.Cells[row, 1].Value = item.Date;
                        worksheet.Cells[row, 2].Value = item.Customer!.CustomerName;
                        worksheet.Cells[row, 3].Value = item.Product!.ProductName;
                        worksheet.Cells[row, 4].Value = item.CustomerPoNo;
                        worksheet.Cells[row, 5].Value = item.CustomerOrderSlipNo;
                        worksheet.Cells[row, 6].Value = item.DeliveredPrice;
                        worksheet.Cells[row, 7].Value = unservedVolume;
                        worksheet.Cells[row, 8].Value = item.TotalAmount;
                        worksheet.Cells[row, 9].Value = "APPROVED";
                        worksheet.Cells[row, 10].Value = item.ExpirationDate?.ToString("dd-MMM-yyyy");
                        worksheet.Cells[row, 11].Value = item.OldCosNo;

                        worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        row++;

                        totalUnservedVolume += unservedVolume;
                        totalAmount += item.TotalAmount;
                    }

                    // Add total row
                    worksheet.Cells[row, 6].Value = "TOTAL";
                    worksheet.Cells[row, 7].Value = totalUnservedVolume;
                    worksheet.Cells[row, 8].Value = totalAmount;
                    worksheet.Cells[row, 6, row, 8].Style.Font.Bold = true;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    // Auto-fit columns for readability
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Return as Excel file
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    var fileName = $"COS_Unserved_Volume_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(COSUnservedVolume));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(COSUnservedVolume));
        }

        #endregion

        #region -- Generate Dispatch Report Excel File --
         public async Task<IActionResult> GenerateDispatchReportExcelFile(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(viewModel.ReportType))
            {
                return BadRequest();
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var currentUser = _userManager.GetUserName(User)!;
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                var firstDayOfMonth = new DateOnly(viewModel.DateFrom.Year, viewModel.DateFrom.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(i => i.Company == companyClaims
                        && i.AuthorityToLoadNo != null
                        && i.Date >= firstDayOfMonth
                        && i.Date <= lastDayOfMonth
                        && (viewModel.ReportType == "AllDeliveries" || i.Status == nameof(DRStatus.PendingDelivery)), cancellationToken);

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dispatch Report");

                    // Insert image from root directory
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride.jpg"); // Update this to your image file name
                    var picture = worksheet.Drawings.AddPicture("CompanyLogo", new FileInfo(imagePath));
                    picture.SetPosition(0, 0, 0, 0); // Adjust position as needed
                    picture.SetSize(200, 60); // Adjust size as needed

                    var mergedCellsA5 = worksheet.Cells["A5:B5"];
                    mergedCellsA5.Merge = true;
                    mergedCellsA5.Value = "OPERATION - LOGISTICS";

                    var mergedCellsA6 = worksheet.Cells["A6:B6"];
                    mergedCellsA6.Merge = true;
                    mergedCellsA6.Value = $"DISPATCH REPORT AS OF {lastDayOfMonth:dd MMM, yyyy}";

                    var mergedCellsA7 = worksheet.Cells["A7:B7"];
                    mergedCellsA7.Merge = true;
                    mergedCellsA7.Value = viewModel.ReportType == "AllDeliveries" ? "ALL DELIVERIES" : "IN TRANSIT DELIVERIES";

                    // Table headers
                    worksheet.Cells["A9"].Value = "DR DATE";
                    worksheet.Cells["B9"].Value = "CUSTOMER NAME";
                    worksheet.Cells["C9"].Value = "TYPE";
                    worksheet.Cells["D9"].Value = "DR NO.";
                    worksheet.Cells["E9"].Value = "PRODUCTS";
                    worksheet.Cells["F9"].Value = "QTY.";
                    worksheet.Cells["G9"].Value = "PICK-UP POINT";
                    worksheet.Cells["H9"].Value = "PO #";
                    worksheet.Cells["I9"].Value = "ATL#";
                    worksheet.Cells["J9"].Value = "COS NO.";
                    worksheet.Cells["K9"].Value = "HAULER NAME";
                    worksheet.Cells["L9"].Value = "SUPPLIER";
                    worksheet.Cells["M9"].Value = "FREIGHT CHARGE";
                    worksheet.Cells["N9"].Value = "ECC";
                    worksheet.Cells["O9"].Value = "TOTAL FREIGHT";

                    ///TODO Remove this in the future
                    worksheet.Cells["P9"].Value = "OTC COS No.";
                    worksheet.Cells["Q9"].Value = "OTC DR No.";

                    if (viewModel.ReportType == "AllDeliveries")
                    {
                        worksheet.Cells["R9"].Value = "DELIVERY DATE";
                        worksheet.Cells["S9"].Value = "STATUS";
                    }


                    int currentRow = 10;
                    string headerColumn = viewModel.ReportType == "AllDeliveries" ? "S9" : "Q9";

                    var groupedReceipts = deliveryReceipts
                        .OrderBy(d => d.CustomerOrderSlip!.ProductId)
                        .ThenBy(d => d.Date)
                        .GroupBy(d => d.CustomerOrderSlip!.ProductId);

                    decimal grandSumOfFreight = 0;
                    decimal grandSumOfECC = 0;
                    decimal grandSumOfTotalFreightAmount = 0;
                    decimal grandTotalQuantity = 0;

                    foreach (var group in groupedReceipts)
                    {
                        string productName = group.First().CustomerOrderSlip!.Product!.ProductName;
                        decimal sumOfFreight = 0;
                        decimal sumOfECC = 0;
                        decimal sumOfTotalFreight = 0;
                        decimal totalQuantity = 0;

                        foreach (var dr in group)
                        {

                            var quantity = dr.Quantity;
                            var freightCharge = dr.Freight;
                            var ecc = dr.ECC;
                            var totalFreightAmount = quantity * (freightCharge + ecc);

                            worksheet.Cells[currentRow, 1].Value = dr.Date;
                            worksheet.Cells[currentRow, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                            worksheet.Cells[currentRow, 2].Value = dr.Customer?.CustomerName;
                            worksheet.Cells[currentRow, 3].Value = dr.Customer?.CustomerType;
                            worksheet.Cells[currentRow, 4].Value = dr.DeliveryReceiptNo;
                            worksheet.Cells[currentRow, 5].Value = productName;
                            worksheet.Cells[currentRow, 6].Value = dr.Quantity;
                            worksheet.Cells[currentRow, 7].Value = dr.CustomerOrderSlip?.PickUpPoint?.Depot;
                            worksheet.Cells[currentRow, 8].Value = dr.PurchaseOrder?.PurchaseOrderNo;
                            worksheet.Cells[currentRow, 9].Value = dr.AuthorityToLoadNo;
                            worksheet.Cells[currentRow, 10].Value = dr.CustomerOrderSlip?.CustomerOrderSlipNo;
                            worksheet.Cells[currentRow, 11].Value = dr.Hauler?.SupplierName;
                            worksheet.Cells[currentRow, 12].Value = dr.PurchaseOrder?.Supplier?.SupplierName;
                            worksheet.Cells[currentRow, 13].Value = freightCharge;
                            worksheet.Cells[currentRow, 14].Value = ecc;
                            worksheet.Cells[currentRow, 15].Value = totalFreightAmount;
                            worksheet.Cells[currentRow, 16].Value = dr.CustomerOrderSlip?.OldCosNo;
                            worksheet.Cells[currentRow, 17].Value = dr.ManualDrNo;

                            if (viewModel.ReportType == "AllDeliveries")
                            {
                                worksheet.Cells[currentRow, 18].Value = dr.DeliveredDate;
                                worksheet.Cells[currentRow, 18].Style.Numberformat.Format = "MMM/dd/yyyy";
                                worksheet.Cells[currentRow, 19].Value = dr.Status == nameof(DRStatus.PendingDelivery) ? "IN TRANSIT" : dr.Status.ToUpper();
                            }

                            currentRow++;
                            totalQuantity += quantity;
                            sumOfFreight += freightCharge;
                            sumOfECC += ecc;
                            sumOfTotalFreight += totalFreightAmount;
                        }

                        // Subtotal row for each product
                        worksheet.Cells[currentRow, 5].Value = "SUB TOTAL";
                        worksheet.Cells[currentRow, 6].Value = totalQuantity;
                        worksheet.Cells[currentRow, 13].Value = sumOfFreight;
                        worksheet.Cells[currentRow, 14].Value = sumOfECC;
                        worksheet.Cells[currentRow, 15].Value = sumOfTotalFreight;

                        using (var subtotalRowRange = worksheet.Cells[currentRow, 1, currentRow, 19]) // Adjust range as needed
                        {
                            subtotalRowRange.Style.Font.Bold = true; // Make text bold
                            subtotalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            subtotalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        }

                        grandSumOfFreight += sumOfFreight;
                        grandSumOfECC += sumOfECC;
                        grandSumOfTotalFreightAmount += sumOfTotalFreight;
                        grandTotalQuantity += totalQuantity;

                        currentRow += 2;
                    }

                    // Grand Total row
                    worksheet.Cells[currentRow, 5].Value = "GRAND TOTAL";
                    worksheet.Cells[currentRow, 6].Value = grandTotalQuantity;
                    worksheet.Cells[currentRow, 13].Value = grandSumOfFreight;
                    worksheet.Cells[currentRow, 14].Value = grandSumOfECC;
                    worksheet.Cells[currentRow, 15].Value = grandSumOfTotalFreightAmount;

                    // Adding borders and bold styling to the total row
                    using (var totalRowRange = worksheet.Cells[currentRow, 1, currentRow, 19]) // Whole row
                    {
                        totalRowRange.Style.Font.Bold = true; // Make text bold
                        totalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        totalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }

                    // Generated by, checked by, received by footer
                    worksheet.Cells[currentRow + 3, 1, currentRow + 3, 2].Merge = true;
                    worksheet.Cells[currentRow + 3, 1].Value = "Generated by:";
                    worksheet.Cells[currentRow + 3, 4].Value = "Noted & Checked by:";
                    worksheet.Cells[currentRow + 3, 8].Value = "Received by:";

                    worksheet.Cells[currentRow + 4, 1, currentRow + 4, 2].Merge = true;
                    worksheet.Cells[currentRow + 4, 1].Value = currentUser.ToUpper();
                    worksheet.Cells[currentRow + 4, 4].Value = "JOEYLITO M. CAILAN";
                    worksheet.Cells[currentRow + 4, 8].Value = "IVY PAGKATIPUNAN";

                    worksheet.Cells[currentRow + 5, 1, currentRow + 5, 2].Merge = true;
                    worksheet.Cells[currentRow + 5, 1].Value = $"Date & Time: {today:MM/dd/yyyy - hh:mm tt}";
                    worksheet.Cells[currentRow + 5, 4].Value = "LOGISTICS SUPERVISOR";
                    worksheet.Cells[currentRow + 5, 8].Value = "CNC SUPERVISOR";

                    // Styling and formatting (optional)
                    worksheet.Cells["M:N"].Style.Numberformat.Format = "#,##0.0000";
                    worksheet.Cells["F,O"].Style.Numberformat.Format = "#,##0.00";

                    using (var range = worksheet.Cells[$"A9:{headerColumn}"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Color.SetColor(Color.White);
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    worksheet.Cells.AutoFitColumns();
                    worksheet.View.FreezePanes(10, 1);
                    // Return Excel file as response
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    var fileName = $"DispatchReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(DispatchReport));
            }
        }

        #endregion

        #region -- Generate Service Invoice Report Excel File --

        public async Task<IActionResult> GenerateServiceInvoiceReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(ServiceInvoiceReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User);
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var serviceReport = await _unitOfWork.FilprideReport.GetServiceInvoiceReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken);

                if (serviceReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ServiceInvoiceReport));
                }
                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ServiceReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SERVICE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Transaction Date";
                worksheet.Cells["B7"].Value = "Customer Name";
                worksheet.Cells["C7"].Value = "Customer Address";
                worksheet.Cells["D7"].Value = "Customer TIN";
                worksheet.Cells["E7"].Value = "Service Invoice#";
                worksheet.Cells["F7"].Value = "Service";
                worksheet.Cells["G7"].Value = "Period";
                worksheet.Cells["H7"].Value = "Due Date";
                worksheet.Cells["I7"].Value = "G. Amount";
                worksheet.Cells["J7"].Value = "Amount Paid";
                worksheet.Cells["K7"].Value = "Payment Status";
                worksheet.Cells["L7"].Value = "Instructions";
                worksheet.Cells["M7"].Value = "Type";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:M7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormatTwoDecimal = "#,##0.00";

                var totalAmount = 0m;
                var totalAmountPaid = 0m;

                foreach (var sv in serviceReport)
                {
                    worksheet.Cells[row, 1].Value = sv.CreatedDate;
                    worksheet.Cells[row, 2].Value = sv.Customer!.CustomerName;
                    worksheet.Cells[row, 3].Value = sv.CustomerAddress;
                    worksheet.Cells[row, 4].Value = sv.CustomerTin;
                    worksheet.Cells[row, 5].Value = sv.ServiceInvoiceNo;
                    worksheet.Cells[row, 6].Value = sv.Service!.Name;
                    worksheet.Cells[row, 7].Value = sv.Period;
                    worksheet.Cells[row, 8].Value = sv.DueDate;
                    worksheet.Cells[row, 9].Value = sv.Total;
                    worksheet.Cells[row, 10].Value = sv.AmountPaid;
                    worksheet.Cells[row, 11].Value = sv.PaymentStatus;
                    worksheet.Cells[row, 12].Value = sv.Instructions;
                    worksheet.Cells[row, 13].Value = sv.Type;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "MMM yyyy";
                    worksheet.Cells[row, 8].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;


                    totalAmount += sv.Total;
                    totalAmountPaid += sv.AmountPaid;
                    row++;
                }

                worksheet.Cells[row, 8].Value = "Total ";
                worksheet.Cells[row, 9].Value = totalAmount;
                worksheet.Cells[row, 10].Value = totalAmountPaid;

                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 8, row, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 3);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ServiceReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(ServiceInvoiceReport));
            }
        }

        #endregion

    }
}
