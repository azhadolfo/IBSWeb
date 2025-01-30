using System.Drawing;
using System.Text;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
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

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
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
                         .ThenInclude(po => po.Supplier)
                         .Include(rr => rr.PurchaseOrder)
                         .ThenInclude(po => po.Product)
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
            if (ModelState.IsValid)
            {
                try
                {
                    var inventoryBooks = _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
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

            if (ModelState.IsValid)
            {
                try
                {
                    var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo, companyClaims);

                    return View(cosSummary);
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

        public async Task<IActionResult> GenerateCOSUnservedVolumeToExcel(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom.ToString("MMMM dd, yyyy");
            ViewBag.DateTo = model.DateTo.ToString("MMMM dd, yyyy");
            var companyClaims = await GetCompanyClaimAsync();

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
                        "COS Date", "Date of Del", "Customer", "Product", "P.O. No.",
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
                    foreach (var item in cosSummary)
                    {
                        worksheet.Cells[row, 1].Value = item.Date;
                        worksheet.Cells[row, 2].Value = item.Date;
                        worksheet.Cells[row, 3].Value = item.Customer.CustomerName;
                        worksheet.Cells[row, 4].Value = item.Product.ProductName;
                        worksheet.Cells[row, 5].Value = item.CustomerPoNo;
                        worksheet.Cells[row, 6].Value = item.CustomerOrderSlipNo;
                        worksheet.Cells[row, 7].Value = item.DeliveredPrice;
                        worksheet.Cells[row, 8].Value = item.Quantity - item.DeliveredQuantity;
                        worksheet.Cells[row, 9].Value = item.TotalAmount;
                        worksheet.Cells[row, 10].Value = "APPROVED";
                        worksheet.Cells[row, 11].Value = item.ExpirationDate;
                        worksheet.Cells[row, 12].Value = item.OldCosNo;

                        worksheet.Cells[row, 7, row, 9].Style.Numberformat.Format = "#,##0.0000";
                        row++;
                    }

                    // Add total row
                    worksheet.Cells[row, 7].Value = "TOTAL";
                    worksheet.Cells[row, 8].Formula = $"SUM(H4:H{row - 1})";
                    worksheet.Cells[row, 9].Formula = $"SUM(I4:I{row - 1})";
                    worksheet.Cells[row, 7, row, 9].Style.Font.Bold = true;
                    worksheet.Cells[row, 8, row, 9].Style.Numberformat.Format = "#,##0.0000";

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

        [HttpGet]
        public IActionResult DispatchReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DispatchReport(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(viewModel.ReportType))
            {
                return BadRequest();
            }

            var companyClaims = await GetCompanyClaimAsync();
            var currentUser = _userManager.GetUserName(User);
            var today = DateTimeHelper.GetCurrentPhilippineTime();
            var firstDayOfMonth = new DateOnly(viewModel.AsOf.Year, viewModel.AsOf.Month, 1);
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

                worksheet.Cells["A5"].Value = "OPERATION - LOGISTICS";
                worksheet.Cells["A6"].Value = $"DISPATCH REPORT AS OF {lastDayOfMonth:dd MMM, yyyy}";
                worksheet.Cells["A7"].Value = viewModel.ReportType == "AllDeliveries" ? "ALL DELIVERIES" : "IN TRANSIT DELIVERIES";

                // Add column headers
                var headers = new[]
                {
                    "DR DATE", "CUSTOMER NAME", "TYPE", "DR NO.", "PRODUCTS", "QTY.", "AMOUNT", "PICK-UP POINT",
                    "PO #", "ATL#/SO#", "COS NO.", "HAULER NAME", "SUPPLIER", "COST", "FREIGHT", "ECC", "TOTAL FREIGHT"
                };

                if (viewModel.ReportType == "AllDeliveries")
                {
                    headers = headers.Concat(new[] { "DELIVERY DATE", "STATUS" }).ToArray();
                }

                // Table headers
                worksheet.Cells["A9"].Value = "DR DATE";
                worksheet.Cells["A9"].AutoFitColumns();
                worksheet.Cells["B9"].Value = "CUSTOMER NAME";
                worksheet.Cells["C9"].Value = "TYPE";
                worksheet.Cells["D9"].Value = "DR NO.";
                worksheet.Cells["E9"].Value = "PRODUCTS";
                worksheet.Cells["F9"].Value = "QTY.";
                worksheet.Cells["G9"].Value = "AMOUNT";
                worksheet.Cells["H9"].Value = "PICK-UP POINT";
                worksheet.Cells["I9"].Value = "PO #";
                worksheet.Cells["J9"].Value = "ATL#/SO#";
                worksheet.Cells["K9"].Value = "COS NO.";
                worksheet.Cells["L9"].Value = "HAULER NAME";
                worksheet.Cells["M9"].Value = "SUPPLIER";
                worksheet.Cells["N9"].Value = "COST";
                worksheet.Cells["O9"].Value = "FREIGHT";
                worksheet.Cells["P9"].Value = "ECC";
                worksheet.Cells["Q9"].Value = "TOTAL FREIGHT";

                //Remove this in the future
                worksheet.Cells["R9"].Value = "OTC COS No.";
                worksheet.Cells["S9"].Value = "OTC DR No.";

                if (viewModel.ReportType == "AllDeliveries")
                {
                    worksheet.Cells["T9"].Value = "DELIVERY DATE";
                    worksheet.Cells["U9"].Value = "STATUS";
                }


                int currentRow = 10;
                string headerColumn = viewModel.ReportType == "AllDeliveries" ? "U9" : "S9";

                var groupedReceipts = deliveryReceipts
                    .OrderBy(d => d.CustomerOrderSlip.ProductId)
                    .ThenBy(d => d.Date)
                    .GroupBy(d => d.CustomerOrderSlip.ProductId);

                decimal grandSumOfCost = 0;
                decimal grandSumOfFreight = 0;
                decimal grandSumOfECC = 0;
                decimal grandSumOfTotalFreight = 0;
                decimal grandTotalQuantity = 0;
                decimal grandTotalAmount = 0;

                foreach (var group in groupedReceipts)
                {
                    string productName = group.First().CustomerOrderSlip.Product.ProductName;
                    decimal sumOfCost = 0;
                    decimal sumOfFreight = 0;
                    decimal sumOfECC = 0;
                    decimal sumOfTotalFreight = 0;
                    decimal totalQuantity = 0;
                    decimal totalAmount = 0;

                    foreach (var dr in group)
                    {
                        worksheet.Cells[currentRow, 1].Value = dr.Date.ToString("dd-MMM-yy");
                        worksheet.Cells[currentRow, 2].Value = dr.Customer.CustomerName;
                        worksheet.Cells[currentRow, 3].Value = dr.Customer.CustomerType;
                        worksheet.Cells[currentRow, 4].Value = dr.DeliveryReceiptNo;
                        worksheet.Cells[currentRow, 5].Value = dr.CustomerOrderSlip.Product.ProductName;
                        worksheet.Cells[currentRow, 6].Value = dr.Quantity;
                        worksheet.Cells[currentRow, 7].Value = dr.TotalAmount;
                        worksheet.Cells[currentRow, 8].Value = dr.CustomerOrderSlip.PickUpPoint.Depot;
                        worksheet.Cells[currentRow, 9].Value = dr.PurchaseOrder.PurchaseOrderNo;
                        worksheet.Cells[currentRow, 10].Value = dr.AuthorityToLoadNo;
                        worksheet.Cells[currentRow, 11].Value = dr.CustomerOrderSlip.CustomerOrderSlipNo;
                        worksheet.Cells[currentRow, 12].Value = dr.Hauler?.SupplierName;
                        worksheet.Cells[currentRow, 13].Value = dr.PurchaseOrder.Supplier.SupplierName;

                        var cost = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.PurchaseOrder.Price) * dr.Quantity;
                        worksheet.Cells[currentRow, 14].Value = cost;
                        sumOfCost += cost;

                        var freight = (dr.Freight > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.Freight) : dr.Freight) * dr.Quantity;
                        worksheet.Cells[currentRow, 15].Value = freight;
                        sumOfFreight += freight;

                        var ecc = (dr.ECC > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.ECC) : dr.ECC) * dr.Quantity;
                        worksheet.Cells[currentRow, 16].Value = ecc;
                        sumOfECC += ecc;

                        var totalFreight = dr.Freight + dr.ECC;
                        var netTotalFreight = (totalFreight > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(totalFreight) : totalFreight) * dr.Quantity;
                        worksheet.Cells[currentRow, 17].Value = netTotalFreight;
                        sumOfTotalFreight += netTotalFreight;

                        totalQuantity += dr.Quantity;
                        totalAmount += dr.TotalAmount;

                        worksheet.Cells[currentRow, 18].Value = dr.CustomerOrderSlip.OldCosNo;
                        worksheet.Cells[currentRow, 19].Value = dr.ManualDrNo;

                        if (viewModel.ReportType == "AllDeliveries")
                        {
                            worksheet.Cells[currentRow, 20].Value = dr.DeliveredDate?.ToString("dd-MMM-yy");
                            worksheet.Cells[currentRow, 21].Value = dr.Status == nameof(DRStatus.PendingDelivery) ? "IN TRANSIT" : dr.Status.ToUpper();
                        }

                        currentRow++;
                    }

                    // Subtotal row for each product
                    worksheet.Cells[currentRow, 5].Value = "SUB TOTAL";
                    worksheet.Cells[currentRow, 6].Value = totalQuantity;
                    worksheet.Cells[currentRow, 7].Value = totalAmount;
                    worksheet.Cells[currentRow, 14].Value = sumOfCost;
                    worksheet.Cells[currentRow, 15].Value = sumOfFreight;
                    worksheet.Cells[currentRow, 16].Value = sumOfECC;
                    worksheet.Cells[currentRow, 17].Value = sumOfTotalFreight;

                    using (var subtotalRowRange = worksheet.Cells[currentRow, 1, currentRow, 17]) // Adjust range as needed
                    {
                        subtotalRowRange.Style.Font.Bold = true; // Make text bold
                        subtotalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        subtotalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        subtotalRowRange.Style.Numberformat.Format = "#,##0.0000"; // Optional: Format numbers
                    }

                    grandSumOfCost += sumOfCost;
                    grandSumOfFreight += sumOfFreight;
                    grandSumOfECC += sumOfECC;
                    grandSumOfTotalFreight += sumOfTotalFreight;
                    grandTotalQuantity += totalQuantity;
                    grandTotalAmount += totalAmount;

                    currentRow += 2;
                }

                // Grand Total row
                worksheet.Cells[currentRow, 5].Value = "GRAND TOTAL";
                worksheet.Cells[currentRow, 6].Value = grandTotalQuantity;
                worksheet.Cells[currentRow, 7].Value = grandTotalAmount;
                worksheet.Cells[currentRow, 14].Value = grandSumOfCost;
                worksheet.Cells[currentRow, 15].Value = grandSumOfFreight;
                worksheet.Cells[currentRow, 16].Value = grandSumOfECC;
                worksheet.Cells[currentRow, 17].Value = grandSumOfTotalFreight;

                // Adding borders and bold styling to the total row
                using (var totalRowRange = worksheet.Cells[currentRow, 1, currentRow, 17]) // Whole row
                {
                    totalRowRange.Style.Font.Bold = true; // Make text bold
                    totalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    totalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    totalRowRange.Style.Numberformat.Format = "#,##0.0000"; // Optional: Format numbers
                }

                // Generated by, checked by, received by footer
                worksheet.Cells[currentRow + 3, 1].Value = "Generated by:";
                worksheet.Cells[currentRow + 3, 4].Value = "Noted & Checked by:";
                worksheet.Cells[currentRow + 3, 8].Value = "Received by:";

                worksheet.Cells[currentRow + 4, 1].Value = currentUser.ToUpper();
                worksheet.Cells[currentRow + 4, 4].Value = "JOEYLITO M. CAILAN";
                worksheet.Cells[currentRow + 4, 8].Value = "IVY PAGKATIPUNAN";

                worksheet.Cells[currentRow + 5, 1].Value = $"Date & Time: {today:MM/dd/yyyy - hh:mm tt}";
                worksheet.Cells[currentRow + 5, 4].Value = "LOGISTICS SUPERVISOR";
                worksheet.Cells[currentRow + 5, 8].Value = "CNC SUPERVISOR";

                // Styling and formatting (optional)
                worksheet.Cells["B:T"].AutoFitColumns();
                worksheet.Cells["F,G,N:Q"].Style.Numberformat.Format = "#,##0.0000";

                using (var range = worksheet.Cells[$"A9:{headerColumn}"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Return Excel file as response
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                var fileName = $"DispatchReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
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
            if (ModelState.IsValid)
            {
                try
                {
                    var sales = _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims);

                    return View(sales);
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
            if (ModelState.IsValid)
            {
                try
                {
                    var purchaseOrder = _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims);

                    return View(purchaseOrder);
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

        public async Task<IActionResult> PurchaseReport()
        {
            return View();
        }

        public async Task<IActionResult> GmReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedGmReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    // get data by chosen date
                    var purchaseReport = _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

                    return View(purchaseReport);
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

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseReport(ViewModelBook model)
        {
            ViewData["DateFrom"] = model.DateFrom;
            ViewData["DateTo"] = model.DateTo;
            var companyClaims = await GetCompanyClaimAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    // get data by chosen date
                    var purchaseReport = _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

                    #region -- Assign collected data to purchaseReportVM --

                    //assign data to vm model
                    var purchaseReportVm = purchaseReport.Select(rr => new PurchaseReportViewModel
                    {
                        Date = rr.Date,
                        SupplierName = rr.PurchaseOrder?.Supplier?.SupplierName,
                        SupplierTin = rr.PurchaseOrder?.Supplier?.SupplierTin,
                        SupplierAddress = rr.PurchaseOrder?.Supplier?.SupplierAddress,
                        PurchaseOrderNo = rr.PurchaseOrder?.PurchaseOrderNo,
                        FilprideRR = rr.ReceivingReportNo,
                        FilprideDR = rr.DeliveryReceipt?.DeliveryReceiptNo,
                        ATLNo = rr.DeliveryReceipt?.AuthorityToLoadNo,
                        CustomerName = rr.DeliveryReceipt?.Customer?.CustomerName,
                        Product = rr.PurchaseOrder?.Product?.ProductName,
                        Volume = rr.QuantityReceived,
                        CostPerLiter = rr.PurchaseOrder?.Price,
                        CostAmount = rr.QuantityReceived * rr.PurchaseOrder?.Price,
                        VATAmount = ((rr.QuantityReceived * rr.PurchaseOrder?.Price) / 1.12m) * 0.12m,
                        WHTAmount = ((rr.QuantityReceived * rr.PurchaseOrder?.Price) / 1.12m) * 0.01m,
                        NetPurchases = (rr.QuantityReceived * rr.PurchaseOrder?.Price) / 1.12m,
                        AccountSpecialist = rr.DeliveryReceipt?.CustomerOrderSlip?.AccountSpecialist,
                        COSPrice = rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice,
                        COSAmount = rr.QuantityReceived * rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice,
                        COSPerLiter = rr.PurchaseOrder?.Price,
                        GMPerLiter = rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice - rr.PurchaseOrder?.Price,
                        GMAmount = rr.QuantityReceived * (rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice - rr.PurchaseOrder?.Price),
                        HaulerName = rr.DeliveryReceipt?.Hauler?.SupplierName,
                        FreightCharge = rr.DeliveryReceipt?.Freight,
                        FCAmount = rr.QuantityReceived * rr.DeliveryReceipt?.Freight,
                        CommissionPerLiter = rr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate,
                        CommissionAmount = rr.QuantityReceived * rr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate,
                        NetMarginPerLiter = (rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice - rr.PurchaseOrder?.Price) - rr.DeliveryReceipt?.Freight,
                        NetMarginAmount = rr.QuantityReceived * ((rr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice - rr.PurchaseOrder?.Price) - rr.DeliveryReceipt?.Freight),
                        SupplierSalesInvoice = rr.SupplierInvoiceNumber,
                        SupplierDR = rr.SupplierDrNo,
                        SupplierWC = rr.WithdrawalCertificate

                    }).ToList();

                    #endregion -- Assign collected data to purchaseReportVM --

                    return View(purchaseReport);
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

        public async Task<IActionResult> OtcFuelSalesReport()
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

                    return View(salesInvoice);
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
                        .GetAllAsync(si => si.PostedBy != null, cancellationToken);

                    return View(salesInvoice);
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"25"}\t{"25"}\t{firstRecord.Date}");
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

                    var generalBooks = _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    fileContent.AppendLine($"{"",-10}\t{"",-20}\t{"",-50}\t{"TOTAL:",2}\t{totalQuantity,18}\t{totalPriceUnitAmount.ToString("N4"),18}\t{totalAmount,18}");

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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"Date",-18}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
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
                    var extractedBy = _userManager.GetUserName(this.User);
                    var companyClaims = await GetCompanyClaimAsync();

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
                    fileContent.AppendLine($"{"TransactionDate",-18}\t{"Tran. Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.TransactionDate}");
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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = cv.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = cv.CVNo;
                worksheet.Cells[row, 3].Value = cv.Payee;
                worksheet.Cells[row, 4].Value = cv.Particulars;
                worksheet.Cells[row, 5].Value = cv.Bank;
                worksheet.Cells[row, 6].Value = cv.CheckNo;
                worksheet.Cells[row, 7].Value = cv.CheckDate;
                worksheet.Cells[row, 8].Value = cv.ChartOfAccount;

                worksheet.Cells[row, 9].Value = cv.Debit;
                worksheet.Cells[row, 10].Value = cv.Credit;

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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = cashReceipt.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = cashReceipt.RefNo;
                worksheet.Cells[row, 3].Value = cashReceipt.CustomerName;
                worksheet.Cells[row, 4].Value = cashReceipt.Bank;
                worksheet.Cells[row, 5].Value = cashReceipt.CheckNo;
                worksheet.Cells[row, 6].Value = cashReceipt.COA;
                worksheet.Cells[row, 7].Value = cashReceipt.Particulars;

                worksheet.Cells[row, 8].Value = cashReceipt.Debit;
                worksheet.Cells[row, 9].Value = cashReceipt.Credit;

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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var generalBooks = _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
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
                worksheet.Cells[row, 1].Value = gl.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = gl.Reference;
                worksheet.Cells[row, 3].Value = gl.Description;
                worksheet.Cells[row, 4].Value = $"{gl.AccountNo} {gl.AccountTitle}";

                worksheet.Cells[row, 5].Value = gl.Debit;
                worksheet.Cells[row, 6].Value = gl.Credit;

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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = inventory.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = inventory.Product.ProductCode;
                worksheet.Cells[row, 3].Value = inventory.Product.ProductName;
                worksheet.Cells[row, 4].Value = inventory.Product.ProductUnit;

                worksheet.Cells[row, 5].Value = inventory.Quantity;
                worksheet.Cells[row, 6].Value = inventory.Cost;
                worksheet.Cells[row, 7].Value = inventory.Total;

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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = jv.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = jv.Reference;
                worksheet.Cells[row, 3].Value = jv.Description;
                worksheet.Cells[row, 4].Value = jv.AccountTitle;

                worksheet.Cells[row, 5].Value = jv.Debit;
                worksheet.Cells[row, 6].Value = jv.Credit;

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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = pb.Date.ToString("dd-MMM-yyyy");
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
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

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
                worksheet.Cells[row, 1].Value = cv.TransactionDate.ToString("dd-MMM-yyyy");
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

        //reports
        #region -- Generate Sales Report Excel File --

        public async Task<IActionResult> GenerateSalesReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var salesReport = _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims);
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
            worksheet.Cells["U7"].Value = "Remarks";

            // Apply styling to the header row
            using (var range = worksheet.Cells["A7:U7"])
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

            var totalFreight = 0m;
            var totalSalesNetOfVat = 0m;
            var totalFreightNetOfVat = 0m;
            var totalCommissionRate = 0m;
            var totalVat = 0m;
            foreach (var si in salesReport)
            {
                var quantity = si.Quantity;
                var freight = (si.DeliveryReceipt?.Freight ?? 0m) * quantity;
                var segment = si.Amount;
                var salesNetOfVat = segment != 0 ? segment / 1.12m : 0;
                var vat = salesNetOfVat * .12m;
                var freightNetOfVat = freight / 1.12m;

                worksheet.Cells[row, 1].Value = si.TransactionDate.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 2].Value = si.Customer?.CustomerName;
                worksheet.Cells[row, 3].Value = si.Customer?.CustomerType;
                worksheet.Cells[row, 4].Value = si.CustomerOrderSlip?.AccountSpecialist;
                worksheet.Cells[row, 5].Value = si.SalesInvoiceNo;
                worksheet.Cells[row, 6].Value = si.CustomerOrderSlip?.CustomerOrderSlipNo;
                worksheet.Cells[row, 7].Value = si.CustomerOrderSlip?.OldCosNo;
                worksheet.Cells[row, 8].Value = si.DeliveryReceipt?.DeliveryReceiptNo;
                worksheet.Cells[row, 9].Value = si.DeliveryReceipt?.ManualDrNo;
                worksheet.Cells[row, 10].Value = si.DeliveryReceipt?.PurchaseOrder?.PurchaseOrderNo;
                worksheet.Cells[row, 11].Value = si.DeliveryReceipt?.PurchaseOrder?.OldPoNo;
                worksheet.Cells[row, 12].Value = si.DeliveryReceipt?.CustomerOrderSlip?.DeliveryOption;
                worksheet.Cells[row, 13].Value = si.Product?.ProductName;
                worksheet.Cells[row, 14].Value = si.Quantity;
                worksheet.Cells[row, 15].Value = freight;
                worksheet.Cells[row, 16].Value = segment;
                worksheet.Cells[row, 17].Value = vat;
                worksheet.Cells[row, 18].Value = salesNetOfVat;
                worksheet.Cells[row, 19].Value = freightNetOfVat;
                worksheet.Cells[row, 20].Value = si.CustomerOrderSlip?.CommissionRate;
                worksheet.Cells[row, 21].Value = si.Remarks;

                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                row++;

                totalFreight += freight;
                totalVat += vat;
                totalSalesNetOfVat += salesNetOfVat;
                totalFreightNetOfVat += freightNetOfVat;
                totalCommissionRate += si.CustomerOrderSlip?.CommissionRate ?? 0m;
            }

            worksheet.Cells[row, 13].Value = "Total ";
            worksheet.Cells[row, 14].Value = totalQuantity;
            worksheet.Cells[row, 15].Value = totalFreight;
            worksheet.Cells[row, 16].Value = totalAmount;
            worksheet.Cells[row, 17].Value = totalVat;
            worksheet.Cells[row, 18].Value = totalSalesNetOfVat;
            worksheet.Cells[row, 19].Value = totalFreightNetOfVat;
            worksheet.Cells[row, 20].Value = totalCommissionRate;

            worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

            // Apply style to subtotal row
            using (var range = worksheet.Cells[row, 1, row, 21])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
            }

            using (var range = worksheet.Cells[row, 10, row, 21])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            var rowForSummary = row + 8;

            // Set the column headers
            var mergedCellForBiodiesel = worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5];
            mergedCellForBiodiesel.Merge = true;
            mergedCellForBiodiesel.Value = "Diesel";
            mergedCellForBiodiesel.Style.Font.Size = 13;
            mergedCellForBiodiesel.Style.Font.Bold = true;
            worksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var textStyleForSummarry = worksheet.Cells[rowForSummary - 3, 2];
            textStyleForSummarry.Style.Font.Size = 16;
            textStyleForSummarry.Style.Font.Bold = true;

            worksheet.Cells[rowForSummary - 3, 2].Value = "Summary";
            worksheet.Cells[rowForSummary - 1, 2].Value = "Segment";
            worksheet.Cells[rowForSummary - 1, 3].Value = "Volume";
            worksheet.Cells[rowForSummary - 1, 4].Value = "Sales N. VAT";
            worksheet.Cells[rowForSummary - 1, 5].Value = "Ave. SP";

            worksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Apply styling to the header row for Biodiesel
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

            // Apply style to subtotal row for Biodiesel
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
            var mergedCellForEconogas = worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9];
            mergedCellForEconogas.Merge = true;
            mergedCellForEconogas.Value = "Econo";
            mergedCellForEconogas.Style.Font.Size = 13;
            mergedCellForEconogas.Style.Font.Bold = true;
            worksheet.Cells[rowForSummary - 2, 7, rowForSummary - 2, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells[rowForSummary - 1, 7].Value = "Volume";
            worksheet.Cells[rowForSummary - 1, 8].Value = "Sales N. VAT";
            worksheet.Cells[rowForSummary - 1, 9].Value = "Ave. SP";

            worksheet.Cells[rowForSummary - 1, 7, rowForSummary - 1, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Apply styling to the header row for Econogas
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

            // Apply style to subtotal row for Econogas
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
            var mergedCellForEnvirogas = worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13];
            mergedCellForEnvirogas.Merge = true;
            mergedCellForEnvirogas.Value = "Enviro";
            mergedCellForEnvirogas.Style.Font.Size = 13;
            mergedCellForEnvirogas.Style.Font.Bold = true;
            worksheet.Cells[rowForSummary - 2, 11, rowForSummary - 2, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            //inset data/value in excel
            worksheet.Cells[rowForSummary - 1, 11].Value = "Volume";
            worksheet.Cells[rowForSummary - 1, 12].Value = "Sales N. VAT";
            worksheet.Cells[rowForSummary - 1, 13].Value = "Ave. SP";

            worksheet.Cells[rowForSummary - 1, 11, rowForSummary - 1, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Apply styling to the header row for Envirogas
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

            // Apply style to subtotal row for Envirogas
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

            var listForBiodiesel = new List<FilprideSalesInvoice>();
            var listForEconogas = new List<FilprideSalesInvoice>();
            var listForEnvirogas = new List<FilprideSalesInvoice>();

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

                // Computation for Biodiesel
                var biodieselQuantitySum = listForBiodiesel.Sum(s => s.Quantity);
                var biodieselAmountSum = listForBiodiesel.Sum(s => s.Amount);
                var biodieselNetOfAmountSum = biodieselAmountSum != 0m ? biodieselAmountSum / 1.12m : 0;

                worksheet.Cells[rowForSummary, 2].Value = customerType.ToString();
                worksheet.Cells[rowForSummary, 3].Value = biodieselQuantitySum;
                worksheet.Cells[rowForSummary, 4].Value = biodieselNetOfAmountSum;
                worksheet.Cells[rowForSummary, 5].Value = biodieselNetOfAmountSum != 0m || biodieselQuantitySum != 0m ? biodieselNetOfAmountSum / biodieselQuantitySum : 0m;

                worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                // Computation for Econogas
                var econogasQuantitySum = listForEconogas.Sum(s => s.Quantity);
                var econogasAmountSum = listForEconogas.Sum(s => s.Amount);
                var econogasNetOfAmountSum = econogasAmountSum != 0m ? econogasAmountSum / 1.12m : 0;

                worksheet.Cells[rowForSummary, 7].Value = econogasQuantitySum;
                worksheet.Cells[rowForSummary, 8].Value = econogasNetOfAmountSum;
                worksheet.Cells[rowForSummary, 9].Value = econogasNetOfAmountSum != 0m || econogasQuantitySum != 0m ? econogasNetOfAmountSum / econogasQuantitySum : 0m;

                worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

                // Computation for Envirogas
                var envirogasQuantitySum = listForEnvirogas.Sum(s => s.Quantity);
                var envirogasAmountSum = listForEnvirogas.Sum(s => s.Amount);
                var envirogasNetOfAmountSum = envirogasAmountSum != 0m ? envirogasAmountSum / 1.12m : 0;

                worksheet.Cells[rowForSummary, 11].Value = envirogasQuantitySum;
                worksheet.Cells[rowForSummary, 12].Value = envirogasNetOfAmountSum;
                worksheet.Cells[rowForSummary, 13].Value = envirogasNetOfAmountSum != 0m || envirogasQuantitySum != 0m ? envirogasNetOfAmountSum / envirogasQuantitySum : 0;

                worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

                rowForSummary++;

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

            worksheet.Cells[rowForSummary, 3].Value = totalQuantityForBiodiesel;
            worksheet.Cells[rowForSummary, 4].Value = totalAmountForBiodiesel;
            worksheet.Cells[rowForSummary, 5].Value = totalAmountForBiodiesel != 0m || totalQuantityForBiodiesel != 0m ? totalAmountForBiodiesel / totalQuantityForBiodiesel : 0;

            worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

            worksheet.Cells[rowForSummary, 7].Value = totalQuantityForEconogas;
            worksheet.Cells[rowForSummary, 8].Value = totalAmountForEconogas;
            worksheet.Cells[rowForSummary, 9].Value = totalAmountForEconogas != 0m || totalQuantityForEconogas != 0m ? totalAmountForEconogas / totalQuantityForEconogas : 0;

            worksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormat;

            worksheet.Cells[rowForSummary, 11].Value = totalQuantityForEnvirogas;
            worksheet.Cells[rowForSummary, 12].Value = totalAmountForEnvirogas;
            worksheet.Cells[rowForSummary, 13].Value = totalAmountForEnvirogas != 0m || totalQuantityForEnvirogas != 0m ? totalAmountForEnvirogas / totalQuantityForEnvirogas : 0;

            worksheet.Cells[rowForSummary, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
            worksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormat;

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 3);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SalesReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion

        #region -- Generate Purchase Order Report Excel File --

        public async Task<IActionResult> GeneratePurchaseOrderReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var PurchaseOrderReport = _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims);
            if (PurchaseOrderReport.Count == 0)
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
            string currencyFormat = "#,##0.0000";

            foreach (var po in PurchaseOrderReport)
            {
                worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                worksheet.Cells[row, 2].Value = po.OldPoNo;
                worksheet.Cells[row, 3].Value = po.Date.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 4].Value = po.Supplier?.SupplierName;
                worksheet.Cells[row, 5].Value = po.Product?.ProductName;
                worksheet.Cells[row, 6].Value = po.Quantity;
                worksheet.Cells[row, 7].Value = po.Product?.ProductUnit;
                worksheet.Cells[row, 8].Value = po.Price;
                worksheet.Cells[row, 9].Value = po.Amount;
                worksheet.Cells[row, 10].Value = po.Remarks;

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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PurchaseOrderReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion

        #region -- Generate Purchase Report Excel File --

        public async Task<IActionResult> GeneratePurchaseReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(this.User);
                var companyClaims = await GetCompanyClaimAsync();

                // get rr data from chosen date
                var purchaseReport = _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

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
                decimal totalCommissionPerLiter =  purchaseReport.Sum(pr => (pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m));
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

                    purchaseReportWorksheet.Cells["A7"].Value = "DELIVERY DATE";
                    purchaseReportWorksheet.Cells["B7"].Value = "CUSTOMER RECEIVE DATE"; // move down from here:
                    purchaseReportWorksheet.Cells["C7"].Value = "SUPPLIER NAME";
                    purchaseReportWorksheet.Cells["D7"].Value = "SUPPLIER TIN";
                    purchaseReportWorksheet.Cells["E7"].Value = "SUPPLIER ADDRESS";
                    purchaseReportWorksheet.Cells["F7"].Value = "PO NO.";
                    purchaseReportWorksheet.Cells["G7"].Value = "FILPRIDE RR";
                    purchaseReportWorksheet.Cells["H7"].Value = "FILPRIDE DR";
                    purchaseReportWorksheet.Cells["I7"].Value = "ATL #";
                    purchaseReportWorksheet.Cells["J7"].Value = "SUPPLIER'S SI";
                    purchaseReportWorksheet.Cells["K7"].Value = "SUPPLIER'S DR";
                    purchaseReportWorksheet.Cells["L7"].Value = "SUPPLIER'S WC";
                    purchaseReportWorksheet.Cells["M7"].Value = "CUSTOMER NAME";
                    purchaseReportWorksheet.Cells["N7"].Value = "PRODUCT";
                    purchaseReportWorksheet.Cells["O7"].Value = "VOLUME";
                    purchaseReportWorksheet.Cells["P7"].Value = "CPL G.VAT";
                    purchaseReportWorksheet.Cells["Q7"].Value = "PURCHASES G.VAT";
                    purchaseReportWorksheet.Cells["R7"].Value = "VAT AMOUNT";
                    purchaseReportWorksheet.Cells["S7"].Value = "WHT AMOUNT";
                    purchaseReportWorksheet.Cells["T7"].Value = "HAULER'S NAME";
                    purchaseReportWorksheet.Cells["U7"].Value = "PURCHASES N.VAT";
                    purchaseReportWorksheet.Cells["V7"].Value = "FREIGHT N.VAT";
                    purchaseReportWorksheet.Cells["W7"].Value = "COMMISSION";
                    purchaseReportWorksheet.Cells["X7"].Value = "MANUAL DR NO.";
                    purchaseReportWorksheet.Cells["Y7"].Value = "OLD PO NO.";

                    #endregion

                    #region -- Apply styling to the header row --

                    using (var range = purchaseReportWorksheet.Cells["A7:Y7"])
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
                    string currencyFormat0 = "#,##0"; // numbers format

                    #region -- Populate data rows --

                    foreach (var pr in purchaseReport)
                    {
                        #region -- Variables and Formulas --

                        // calculate values, put in variables to be displayed per cell
                        var volume = pr.QuantityReceived; // volume
                        var costAmount = pr.QuantityReceived * (pr.PurchaseOrder?.Price ?? 0m); // purchase total gross
                        var netPurchases = costAmount / 1.12m; // purchase total net
                        var netFreight = pr.DeliveryReceipt?.Freight / 1.12m; // purchase total net
                        var vatAmount = costAmount * 0.12m; // vat total
                        var whtAmount = netPurchases * 0.01m; // wht total
                        var cosAmount = (pr.QuantityReceived * (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m)); // sale total gross
                        var costPerLiter = (pr.PurchaseOrder?.Price ?? 0m); // sale price per liter
                        var commission = ((pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m) * volume);

                        #endregion

                        #region -- Assign Values to Cells --

                        purchaseReportWorksheet.Cells[row, 1].Value = pr.DeliveryReceipt?.Date.ToString("dd-MMM-yyyy"); // Date ====== dr date
                        purchaseReportWorksheet.Cells[row, 2].Value = pr.Date.ToString("dd-MMM-yyyy"); // Date ====== rr date
                        purchaseReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.Supplier?.SupplierName; // Supplier Name
                        purchaseReportWorksheet.Cells[row, 4].Value = pr.PurchaseOrder?.Supplier?.SupplierTin; // Supplier Tin
                        purchaseReportWorksheet.Cells[row, 5].Value = pr.PurchaseOrder?.Supplier?.SupplierAddress; // Supplier Address
                        purchaseReportWorksheet.Cells[row, 6].Value = pr.PurchaseOrder?.PurchaseOrderNo; // PO No.
                        purchaseReportWorksheet.Cells[row, 7].Value = pr.ReceivingReportNo; // Filpride RR
                        purchaseReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride DR
                        purchaseReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.AuthorityToLoadNo; // ATL #
                        purchaseReportWorksheet.Cells[row, 10].Value = pr.SupplierInvoiceNumber; // Supplier's Sales Invoice
                        purchaseReportWorksheet.Cells[row, 11].Value = pr.SupplierDrNo; // Supplier's DR
                        purchaseReportWorksheet.Cells[row, 12].Value = pr.WithdrawalCertificate; // Supplier's WC
                        purchaseReportWorksheet.Cells[row, 13].Value = pr.DeliveryReceipt?.Customer?.CustomerName; // Customer Name
                        purchaseReportWorksheet.Cells[row, 14].Value = pr.PurchaseOrder?.Product?.ProductName; // Product
                        purchaseReportWorksheet.Cells[row, 15].Value = volume; // Volume
                        purchaseReportWorksheet.Cells[row, 16].Value = costPerLiter; // Purchase price per liter
                        purchaseReportWorksheet.Cells[row, 17].Value = costAmount; // Purchase total gross
                        purchaseReportWorksheet.Cells[row, 18].Value = vatAmount; // Vat total
                        // Def Vat Amount
                        purchaseReportWorksheet.Cells[row, 19].Value = whtAmount; // WHT total
                        purchaseReportWorksheet.Cells[row, 20].Value = pr.DeliveryReceipt?.Hauler?.SupplierName; // Hauler's Name
                        purchaseReportWorksheet.Cells[row, 21].Value = netPurchases; // Purchase total net ======== move to third last
                        purchaseReportWorksheet.Cells[row, 22].Value = netFreight; // freight n vat ============
                        purchaseReportWorksheet.Cells[row, 23].Value = commission; // commission =========
                        purchaseReportWorksheet.Cells[row, 24].Value = pr.DeliveryReceipt?.ManualDrNo; // commission =========
                        purchaseReportWorksheet.Cells[row, 25].Value = pr.PurchaseOrder?.OldPoNo; // commission =========

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

                        purchaseReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat0;
                        purchaseReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                        purchaseReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                        purchaseReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat2;

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

                    purchaseReportWorksheet.Cells[row, 14].Value = "Total: ";
                    purchaseReportWorksheet.Cells[row, 15].Value = totalVolume;
                    purchaseReportWorksheet.Cells[row, 16].Value = totalCostPerLiter;
                    purchaseReportWorksheet.Cells[row, 17].Value = totalCostAmount;
                    purchaseReportWorksheet.Cells[row, 18].Value = totalVatAmount;
                    purchaseReportWorksheet.Cells[row, 19].Value = totalWHTAmount;
                    purchaseReportWorksheet.Cells[row, 21].Value = totalNetPurchases;
                    purchaseReportWorksheet.Cells[row, 22].Value = totalNetFreight;
                    purchaseReportWorksheet.Cells[row, 23].Value = totalCommission;

                    purchaseReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat0;
                    purchaseReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;

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
                    using (var range = purchaseReportWorksheet.Cells[row, 14, row, 24])
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
                        purchaseReportWorksheet.Cells[row, i+1].Value = "VOLUME";
                        purchaseReportWorksheet.Cells[row, i+2].Value = "PURCHASES N.VAT";
                        purchaseReportWorksheet.Cells[row, i+3].Value = "AVE. CPL";
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                        using (var range = purchaseReportWorksheet.Cells[row, i+1, row, i+3])
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
                        .OrderBy(rr => rr.PurchaseOrder.Supplier.SupplierName)
                        .GroupBy(rr => rr.PurchaseOrder.Supplier.SupplierName);

                    // for each supplier
                    foreach (var rrSupplier in supplierByRr)
                    {
                        int startingColumn = 2;

                        // get name of group supplier
                        purchaseReportWorksheet.Cells[row, 2].Value = rrSupplier.First().PurchaseOrder.Supplier.SupplierName;
                        purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                        purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;

                        // group each product of supplier
                        var productBySupplier = rrSupplier
                            .OrderBy(p => p.PurchaseOrder.Product.ProductName)
                            .GroupBy(rr => rr.PurchaseOrder.Product.ProductName);

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
                                var debugValue = (grandTotalVolume != 0m ? grandTotalPurchaseNet / grandTotalVolume : 0m);
                                purchaseReportWorksheet.Cells[row, startingColumn+1, row, startingColumn + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                purchaseReportWorksheet.Cells[row, startingColumn + 1].Style.Numberformat.Format = currencyFormat0;
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


                        purchaseReportWorksheet.Cells[row, i+1].Style.Numberformat.Format = currencyFormat0;
                        purchaseReportWorksheet.Cells[row, i+2].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, i+3].Style.Numberformat.Format = currencyFormat;

                        mergedCells = purchaseReportWorksheet.Cells[row, i + 1, row, i + 3];
                        mergedCells.Style.Font.Bold = true;
                        mergedCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        mergedCells.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                        mergedCells.Style.Font.Size = 11;
                        mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin ;
                        mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Double ;
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
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(this.User);
                var companyClaims = await GetCompanyClaimAsync();

                // fetch sales report
                var salesReport = _unitOfWork.FilprideReport.GetOtcFuelSalesReport(model.DateFrom, model.DateTo, companyClaims);

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
                    .OrderBy(sr => sr.Product?.ProductName)
                    .GroupBy(sr => sr.Product?.ProductName);

                foreach (var productReport in groupedByProductReport)
                {
                    var productName = productReport.First().Product?.ProductName;

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
                    worksheet.Cells["F8"].Value = "DR #";;
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
                        .OrderBy(pr => pr.Customer?.CustomerName)
                        .GroupBy(pr => pr.Customer?.CustomerName);

                    foreach (var customer in groupedByCustomer)
                    {
                        var sortedByDateCustomer = customer
                            .OrderBy(c => c.TransactionDate)
                            .ToList();

                        totalVolume = 0m;
                        totalAmount = 0m;

                        foreach (var transaction in sortedByDateCustomer)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.TransactionDate.ToString("dd-MMM-yyyy"); // Date
                            worksheet.Cells[row, 2].Value = transaction.Customer?.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.Customer?.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt?.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt?.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.Product?.ProductName; // Items
                            worksheet.Cells[row, 9].Value = transaction.Quantity; // Volume
                            worksheet.Cells[row, 10].Value = transaction.Amount; // Total
                            worksheet.Cells[row, 11].Value = transaction.Remarks; // Remarks

                            #endregion -- Assign Values to Cells --

                            // increment totals and format it
                            totalVolume += transaction.Quantity;
                            totalAmount += transaction.Amount;

                            // format cells with number
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
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
                    worksheet.Cells["F8"].Value = "DR #";;
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
                        .OrderBy(sr => sr.Product?.ProductName)
                        .ThenBy(sr => sr.Customer.CustomerName)
                        .ThenBy(sr => sr.TransactionDate)
                        .GroupBy(sr => sr.Product?.ProductName);

                    // shows by product
                    foreach (var product in groupedByProductReport)
                    {
                        totalVolume = 0m;
                        totalAmount = 0m;

                        foreach (var transaction in product)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.TransactionDate.ToString("dd-MMM-yyyy"); // Date
                            worksheet.Cells[row, 2].Value = transaction.Customer?.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.Customer?.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt?.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt?.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.Product?.ProductName; // Items
                            worksheet.Cells[row, 9].Value = transaction.Quantity; // Volume
                            worksheet.Cells[row, 10].Value = transaction.Amount; // Total
                            worksheet.Cells[row, 11].Value = transaction.Remarks; // Remarks

                            #endregion -- Assign Values to Cells --

                            // increment totals
                            totalVolume += transaction.Quantity;
                            totalAmount += transaction.Amount;

                            // format cells with number
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
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
                    string currencyFormat = "#,##0.00";
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
                        .OrderBy(sr => sr.Customer?.CustomerType)
                        .GroupBy(sr => sr.Customer?.CustomerType)
                        .OrderBy(g => g.Key != "Retail")
                        .ThenBy(g => g.Key);

                    #region == Contents ==

                    foreach (var ct in groupByCustomerType)
                    {
                        worksheet.Cells[row, 1].Value = ct.First().Customer?.CustomerType;
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
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        var rowToResize = row;
                        row++;

                        var groupByCustomerName = ct
                            .OrderBy(sr => sr.Customer?.CustomerName)
                            .GroupBy(sr => sr.Customer?.CustomerName);

                        foreach (var customerGroup in groupByCustomerName)
                        {
                            worksheet.Cells[row, 1].Value = customerGroup.First().Customer?.CustomerName;
                            worksheet.Cells[row, 1].Style.Font.Bold = true;

                            worksheet.Cells[row, 2].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "BIODIESEL")
                                .Sum(cg => cg.Quantity);
                            worksheet.Cells[row, 3].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "BIODIESEL")
                                .Sum(cg => cg.Amount);
                            worksheet.Cells[row, 4].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "ECONOGAS")
                                .Sum(cg => cg.Quantity);
                            worksheet.Cells[row, 5].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "ECONOGAS")
                                .Sum(cg => cg.Amount);
                            worksheet.Cells[row, 6].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "ENVIROGAS")
                                .Sum(cg => cg.Quantity);
                            worksheet.Cells[row, 7].Value = customerGroup
                                .Where(cg => cg.Product?.ProductName == "ENVIROGAS")
                                .Sum(cg => cg.Amount);
                            worksheet.Cells[row, 8].Value = customerGroup
                                .Sum(cg => cg.Quantity);
                            worksheet.Cells[row, 9].Value = customerGroup
                                .Sum(cg => cg.Amount);

                            worksheet.Cells[row, 2, row, 9].Style.Numberformat.Format = "#,##0.00";

                            row++;
                        }

                        worksheet.Cells[row, 1].Value = "Total";
                        worksheet.Cells[row, 2].Value = ct
                            .Where(si => si.Product.ProductName == "BIODIESEL")
                            .Sum(si => si.Quantity); // Total Volume
                        worksheet.Cells[row, 3].Value = ct
                            .Where(si => si.Product.ProductName == "BIODIESEL")
                            .Sum(si => si.Amount); // Total Amount
                        worksheet.Cells[row, 4].Value = ct
                            .Where(si => si.Product.ProductName == "ECONOGAS")
                            .Sum(si => si.Quantity); // Total Volume
                        worksheet.Cells[row, 5].Value = ct
                            .Where(si => si.Product.ProductName == "ECONOGAS")
                            .Sum(si => si.Amount); // Total Amount
                        worksheet.Cells[row, 6].Value = ct
                            .Where(si => si.Product.ProductName == "ENVIROGAS")
                            .Sum(si => si.Quantity); // Total Volume
                        worksheet.Cells[row, 7].Value = ct
                            .Where(si => si.Product.ProductName == "ENVIROGAS")
                            .Sum(si => si.Amount); // Total Amount
                        worksheet.Cells[row, 8].Value = ct
                            .Sum(si => si.Quantity); // Total Volume
                        worksheet.Cells[row, 9].Value = ct
                            .Sum(si => si.Amount); // Total Amount

                        var tillRowToResize = row;
                        worksheet.Cells[rowToResize, 1, tillRowToResize, 9].Style.Font.Size = 10;

                        using (var range = worksheet.Cells[row, 1, row, 9])
                        {
                            range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
                            range.Style.Numberformat.Format = "#,##0.00";
                        }

                        row += 2;
                        isStation = false;
                    }

                    #endregion == Contents ==

                    worksheet.Cells[row, 1].Value = "Grand Total";
                    worksheet.Cells[row, 2].Value = salesReport
                        .Where(si => si.Product?.ProductName == "BIODIESEL")
                        .Sum(si => si.Quantity);
                    worksheet.Cells[row, 3].Value = salesReport
                        .Where(si => si.Product?.ProductName == "BIODIESEL")
                        .Sum(si => si.Amount);
                    worksheet.Cells[row, 4].Value = salesReport
                        .Where(si => si.Product?.ProductName == "ECONOGAS")
                        .Sum(si => si.Quantity);
                    worksheet.Cells[row, 5].Value = salesReport
                        .Where(si => si.Product?.ProductName == "ECONOGAS")
                        .Sum(si => si.Amount);
                    worksheet.Cells[row, 6].Value = salesReport
                        .Where(si => si.Product?.ProductName == "ENVIROGAS")
                        .Sum(si => si.Quantity);
                    worksheet.Cells[row, 7].Value = salesReport
                        .Where(si => si.Product?.ProductName == "ENVIROGAS")
                        .Sum(si => si.Amount);
                    worksheet.Cells[row, 8].Value = salesReport
                        .Sum(si => si.Quantity);
                    worksheet.Cells[row, 9].Value = salesReport
                        .Sum(si => si.Amount);

                    using (var range = worksheet.Cells[row, 1, row, 9])
                    {
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.Font.Bold = true;
                        range.Style.Font.Italic = true;
                    }

                    row++;

                    // summary values
                    foreach (var typeGroup in groupByCustomerType)
                    {
                        worksheet.Cells[row, 1].Value = typeGroup.First().Customer?.CustomerType;
                        worksheet.Cells[row, 1].Style.Font.Italic = true;
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 2].Value = typeGroup
                            .Where(tg => tg.Product?.ProductName == "BIODIESEL")
                            .Sum(tg => tg.Quantity);
                        worksheet.Cells[row, 3].Value = typeGroup
                            .Where(tg => tg.Product?.ProductName == "ECONOGAS")
                            .Sum(tg => tg.Quantity);
                        worksheet.Cells[row, 4].Value = typeGroup
                            .Where(tg => tg.Product?.ProductName == "ENVIROGAS")
                            .Sum(tg => tg.Quantity);
                        worksheet.Cells[row, 5].Value = typeGroup
                            .Sum(tg => tg.Quantity);
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
                    worksheet.Cells[row, 5].Value = salesReport.Sum(si => si.Quantity);
                    worksheet.Cells[row, 5].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204,156,252));
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
            try
            {
                var startingSummaryTableRow = 0;
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;

                var extractedBy = _userManager.GetUserName(this.User);
                var companyClaims = await GetCompanyClaimAsync();

                using var package = new ExcelPackage();
                var gmReportWorksheet = package.Workbook.Worksheets.Add("GMReport");

                var purchaseReport = _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

                if (purchaseReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                #region -- Initialize "total" Variables for operations --

                var totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
                decimal totalCostPerLiter = 0m;
                decimal totalCostAmount = 0m;
                decimal totalVatAmount = 0m;
                decimal totalWHTAmount = 0m;
                decimal totalNetPurchases = 0m;
                var totalCOSPrice = purchaseReport.Sum(pr => pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice);
                decimal totalCOSAmount = 0m;
                decimal totalCosNet = 0m;
                decimal totalGMPerLiter = 0m;
                decimal totalGmAmount = 0m;
                var totalFreightCharge = purchaseReport.Sum(pr => pr.DeliveryReceipt?.Freight);
                decimal totalFCAmount = 0m;
                decimal totalFCNet = 0m;
                var totalCommissionPerLiter =  purchaseReport.Sum(pr => pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate);
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
                gmReportWorksheet.Cells["H7"].Value = "VOLUME";
                gmReportWorksheet.Cells["I7"].Value = "CPL G.VAT";
                gmReportWorksheet.Cells["J7"].Value = "PURCHASES G.VAT";
                gmReportWorksheet.Cells["K7"].Value = "VAT";
                gmReportWorksheet.Cells["L7"].Value = "DEF VAT";
                gmReportWorksheet.Cells["M7"].Value = "PURCHASES N.VAT";
                gmReportWorksheet.Cells["N7"].Value = "SPECIALIST";
                gmReportWorksheet.Cells["O7"].Value = "COS PRICE";
                gmReportWorksheet.Cells["P7"].Value = "COS AMOUNT";
                gmReportWorksheet.Cells["Q7"].Value = "SALES N.VAT";
                gmReportWorksheet.Cells["R7"].Value = "GM/LITER";
                gmReportWorksheet.Cells["S7"].Value = "GM AMOUNT";
                gmReportWorksheet.Cells["T7"].Value = "HAULER'S NAME";
                gmReportWorksheet.Cells["U7"].Value = "FREIGHT CHARGE";
                gmReportWorksheet.Cells["V7"].Value = "FC AMOUNT";
                gmReportWorksheet.Cells["W7"].Value = "FC N.VAT";
                gmReportWorksheet.Cells["X7"].Value = "COMMISSION/LITER";
                gmReportWorksheet.Cells["Y7"].Value = "COMMISSION AMOUNT";
                gmReportWorksheet.Cells["Z7"].Value = "NET MARGIN/LIT";
                gmReportWorksheet.Cells["AA7"].Value = "NET MARGIN AMOUNT";

                #endregion

                #region -- Apply styling to the header row --

                    using (var range = gmReportWorksheet.Cells["A7:AA7"])
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

                #region -- Populate data rows --

                foreach (var pr in purchaseReport)
                {
                    #region -- Variables and Formulas --

                    // calculate values, put in variables to be displayed per cell
                    var volume = pr.QuantityReceived;
                    var costAmount = volume * (pr.PurchaseOrder?.Price ?? 0m); // purchase total
                    var cosAmount = (volume * (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m)); // sales total
                    var costPerLiter = (pr.PurchaseOrder?.Price ?? 0m); // purchase per liter
                    var netPurchases = costAmount / 1.12m; // purchase total net
                    var vatAmount = costAmount * 0.12m; // purchase total vat
                    var whtAmount = netPurchases * 0.01m; // purchase wht
                    var freightCharge = (pr.DeliveryReceipt?.Freight ?? 0m); // freight charge per liter
                    var freightChargeAmount = volume * freightCharge; // freight charge total
                    var commissionPerLiter = (pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m); // commission rate
                    var cosPricePerLiter = (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m); // sales per liter
                    var commissionAmount = volume * commissionPerLiter; // commission total
                    var cosNet = cosAmount / 1.12m;
                    var gmAmount = cosNet - netPurchases; // gross margin total
                    var gmPerLiter = gmAmount/volume; // gross margin per liter
                    var freightChargeNet = freightChargeAmount / 1.12m;
                    var netMarginAmount = (gmAmount - freightChargeNet - commissionAmount);
                    var netMarginPerLiter = netMarginAmount / volume; // net margin per liter

                    #endregion

                    #region -- Assign Values to Cells --

                    gmReportWorksheet.Cells[row, 1].Value = pr.Date.ToString("dd-MMM-yyyy"); // Date
                    gmReportWorksheet.Cells[row, 2].Value = pr.PurchaseOrder?.Supplier?.SupplierName; // Supplier Name
                    gmReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.PurchaseOrderNo; // PO No.
                    gmReportWorksheet.Cells[row, 4].Value = pr.ReceivingReportNo; // Filpride RR
                    gmReportWorksheet.Cells[row, 5].Value = pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride DR
                    gmReportWorksheet.Cells[row, 6].Value = pr.DeliveryReceipt?.Customer?.CustomerName; // Customer Name
                    gmReportWorksheet.Cells[row, 7].Value = pr.PurchaseOrder?.Product?.ProductName; // Product Name
                    gmReportWorksheet.Cells[row, 8].Value = volume; // Volume
                    gmReportWorksheet.Cells[row, 9].Value = costPerLiter; // Purchase Cost per Liter
                    gmReportWorksheet.Cells[row, 10].Value = costAmount; // Cost Amount / Purchase Total Gross
                    gmReportWorksheet.Cells[row, 11].Value = vatAmount; // Purchases Vat
                    gmReportWorksheet.Cells[row, 13].Value = netPurchases; // Purchase Total Nett
                    gmReportWorksheet.Cells[row, 14].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.AccountSpecialist; // Account Specialist
                    gmReportWorksheet.Cells[row, 15].Value = cosPricePerLiter; // COS Price / Sale Price Gross
                    gmReportWorksheet.Cells[row, 16].Value = cosAmount; // COS Amount / Sales Total Price
                    gmReportWorksheet.Cells[row, 17].Value = cosNet; // COS Net / Sales Total Net // RECALCULATE
                    gmReportWorksheet.Cells[row, 18].Value = gmPerLiter; // Gross Margin per liter // RECALCULATE
                    gmReportWorksheet.Cells[row, 19].Value = gmAmount; // Gross Margin per Amount // RECALCULATE
                    gmReportWorksheet.Cells[row, 20].Value = pr.DeliveryReceipt?.Hauler?.SupplierName; // Hauler's Name
                    gmReportWorksheet.Cells[row, 21].Value = freightCharge; // Freight Charge
                    gmReportWorksheet.Cells[row, 22].Value = freightChargeAmount; // Freight Charge Amount
                    gmReportWorksheet.Cells[row, 23].Value = freightChargeNet; // Freight Charge Amount // RECALCULATE
                    gmReportWorksheet.Cells[row, 24].Value = commissionPerLiter; // Commission per liter
                    gmReportWorksheet.Cells[row, 25].Value = commissionAmount; // Commission Amount
                    gmReportWorksheet.Cells[row, 26].Value = netMarginPerLiter; // Net Margin liter
                    gmReportWorksheet.Cells[row, 27].Value = netMarginAmount; // Net Margin Amount // RECALCULATE

                    #endregion -- Assign Values to Cells --

                    #region -- Add the values to total and format number cells --

                    totalCostAmount += costAmount;
                    totalVatAmount += vatAmount;
                    totalNetPurchases += netPurchases;
                    totalWHTAmount += whtAmount;
                    totalCOSAmount += cosAmount;
                    totalGMPerLiter += gmPerLiter;
                    totalGmAmount += gmAmount;
                    totalFCAmount += freightChargeAmount;
                    totalCommissionAmount += commissionAmount;
                    totalNetMarginPerLiter += netMarginPerLiter;
                    totalNetMarginAmount += netMarginAmount;
                    totalCosNet += cosNet;
                    totalFCNet += freightChargeNet;

                    gmReportWorksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat;
                    gmReportWorksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormat;

                    #endregion -- Add the values to total and format number cells --

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Other subtotal values and formatting of subtotal cells --

                totalCostPerLiter = totalCostAmount / totalVolume;
                totalCOSPrice = totalCOSAmount / totalVolume;
                totalGMPerLiter = totalGmAmount / totalVolume;
                totalFreightCharge = totalFCAmount / totalVolume;
                totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                gmReportWorksheet.Cells[row, 7].Value = "Total: ";
                gmReportWorksheet.Cells[row, 8].Value = totalVolume; // Volume
                gmReportWorksheet.Cells[row, 9].Value = totalCostPerLiter; // Purchase Amount per liter
                gmReportWorksheet.Cells[row, 10].Value = totalCostAmount; // Purchase Total Amount Net
                gmReportWorksheet.Cells[row, 11].Value = totalVatAmount; // Purchase Vat
                gmReportWorksheet.Cells[row, 13].Value = totalNetPurchases; // Purchase Net
                gmReportWorksheet.Cells[row, 15].Value = totalCOSPrice; // Sales Price per Liter
                gmReportWorksheet.Cells[row, 16].Value = totalCOSAmount; // Sales Total Gross
                gmReportWorksheet.Cells[row, 17].Value = totalCosNet; // Sales Total Net
                gmReportWorksheet.Cells[row, 18].Value = totalGMPerLiter; // GM Per Liter
                gmReportWorksheet.Cells[row, 19].Value = totalGmAmount; // Gross Margin Amount
                gmReportWorksheet.Cells[row, 21].Value = totalFreightCharge; // Freight Charge
                gmReportWorksheet.Cells[row, 22].Value = totalFCAmount; // Freight Charge Amount Gross
                gmReportWorksheet.Cells[row, 23].Value = totalFCNet; // Freight Charge Amount Net
                gmReportWorksheet.Cells[row, 24].Value = totalCommissionPerLiter; // Commission Per Liter
                gmReportWorksheet.Cells[row, 25].Value = totalCommissionAmount; // Commission Amount
                gmReportWorksheet.Cells[row, 26].Value = totalNetMarginPerLiter; // Net Margin Per Liter
                gmReportWorksheet.Cells[row, 27].Value = totalNetMarginAmount; // Net Margin Amount

                gmReportWorksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormat;

                #endregion -- Assign values of other totals and formatting of total cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = gmReportWorksheet.Cells[row, 1, row, 27])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = gmReportWorksheet.Cells[row, 7, row, 27])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                row += 2;

                #region -- Summary Header --

                gmReportWorksheet.Cells[row, 2].Value = "SUMMARY: ";
                gmReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                gmReportWorksheet.Cells[row, 2].Style.Font.Size = 16;

                row++;

                var firstRowEnclosure = row;

                string[] productList = { "DIESEL", "ECONO", "ENVIRO" };

                for (int i = 3, index = 0; i != 27; i += 8, index++)
                {
                    mergedCells = gmReportWorksheet.Cells[row, i, row, i + 7];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 16;
                    mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    mergedCells.Merge = true;
                    mergedCells.Value = productList[index];
                }

                row++;

                gmReportWorksheet.Cells[row, 2].Value = "SUPPLIERS";
                gmReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                gmReportWorksheet.Cells[row, 2].Style.Font.Italic = true;
                gmReportWorksheet.Cells[row, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                gmReportWorksheet.Cells[row, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                gmReportWorksheet.Cells[row, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                gmReportWorksheet.Cells[row, 2].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                gmReportWorksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                for (int i = 2; i != 26; i += 8)
                {
                    gmReportWorksheet.Cells[row, i+1].Value = "VOLUME";
                    gmReportWorksheet.Cells[row, i+2].Value = "SALES N.VAT";
                    gmReportWorksheet.Cells[row, i+3].Value = "PURCHASES N.VAT";
                    gmReportWorksheet.Cells[row, i+4].Value = "GROSS MARGIN";
                    gmReportWorksheet.Cells[row, i+5].Value = "FREIGHT N.VAT";
                    gmReportWorksheet.Cells[row, i+6].Value = "COMMISSION";
                    gmReportWorksheet.Cells[row, i+7].Value = "NET MARGIN";
                    gmReportWorksheet.Cells[row, i+8].Value = "NET GM/LIT";
                    gmReportWorksheet.Cells[row, i+1, row, i+8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    gmReportWorksheet.Cells[row, i+1, row, i+8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                    using (var range = gmReportWorksheet.Cells[row, i+1, row, i+8])
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

                var rrBySupplier = purchaseReport
                    .OrderBy(rr => rr.PurchaseOrder.Supplier.SupplierName)
                    .GroupBy(rr => rr.PurchaseOrder.Supplier.SupplierName);

                startingSummaryTableRow = row;

                foreach (var rrSupplier in rrBySupplier)
                {
                    int startingColumn = 2;

                    gmReportWorksheet.Cells[row, 2].Value = rrSupplier.First().PurchaseOrder.Supplier.SupplierName;
                    gmReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                    gmReportWorksheet.Cells[row, 2].Style.Font.Italic = true;

                    var productBySupplier = rrSupplier
                        .OrderBy(rr => rr.PurchaseOrder.Product.ProductName)
                        .GroupBy(rr => rr.PurchaseOrder.Product.ProductName);

                    foreach (var product in productBySupplier)
                    {
                        if (product.Any())
                        {
                            var grandTotalVolume = product
                                .Sum(pr => pr.QuantityReceived); // volume
                            var grandTotalCosNetAmount = product
                                .Sum(pr => pr.QuantityReceived * (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m) / 1.12m); // Sales Net Total
                            var grandTotalPurchasesNetAmount = product
                                .Sum(pr => (pr.QuantityReceived * pr.PurchaseOrder?.Price ?? 0m) / 1.12m); // Purchase Net Total
                            var grandTotalFreightNVat = product
                                .Sum(pr => pr.QuantityReceived * ((pr.DeliveryReceipt?.Freight ?? 0m) / 1.12m)); // Freight Net Total
                            var grandTotalCommissionAmount = product
                                .Sum(pr => pr.QuantityReceived * (pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m)); // Commission Gross
                            var grandTotalGmAmount = grandTotalCosNetAmount - grandTotalPurchasesNetAmount;
                            var grandTotalNetMarginAmount = (grandTotalGmAmount - grandTotalFreightNVat - grandTotalCommissionAmount);
                            var grandTotalNetMarginPerLiter = grandTotalNetMarginAmount / grandTotalVolume;

                            gmReportWorksheet.Cells[row, startingColumn + 1].Value = grandTotalVolume; //grand volume
                            gmReportWorksheet.Cells[row, startingColumn + 2].Value = grandTotalCosNetAmount; // grand sales n.vat
                            gmReportWorksheet.Cells[row, startingColumn + 3].Value = grandTotalPurchasesNetAmount; // grand purchases nvat
                            gmReportWorksheet.Cells[row, startingColumn + 4].Value = grandTotalGmAmount; // gm amount sales - purchases
                            gmReportWorksheet.Cells[row, startingColumn + 5].Value = grandTotalFreightNVat;// freight nvat
                            gmReportWorksheet.Cells[row, startingColumn + 6].Value = grandTotalCommissionAmount; // commission
                            gmReportWorksheet.Cells[row, startingColumn + 7].Value = grandTotalNetMarginAmount; // Net Margin Amount
                            gmReportWorksheet.Cells[row, startingColumn + 8].Value = grandTotalNetMarginPerLiter; // Net Margin Per Lit

                            gmReportWorksheet.Cells[row, startingColumn + 1, row, startingColumn + 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            gmReportWorksheet.Cells[row, startingColumn + 1, row, startingColumn + 8].Style.Numberformat.Format = currencyFormat;
                        }

                        startingColumn += 8;
                    }

                    row++;
                }

                var endingSummaryTableRow = row - 1;

                row++;

                var lastRowEnclosure = row;


                for (int i = 2; i != 26; i += 8)
                {
                    gmReportWorksheet.Cells[row, i + 1].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+1].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+1].Address})";
                    gmReportWorksheet.Cells[row, i + 2].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+2].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+2].Address})";
                    gmReportWorksheet.Cells[row, i + 3].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+3].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+3].Address})";
                    gmReportWorksheet.Cells[row, i + 4].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+4].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+4].Address})";
                    gmReportWorksheet.Cells[row, i + 5].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+5].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+5].Address})";
                    gmReportWorksheet.Cells[row, i + 6].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+6].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+6].Address})";
                    gmReportWorksheet.Cells[row, i + 7].Formula = $"SUM({gmReportWorksheet.Cells[startingSummaryTableRow, i+7].Address}:{gmReportWorksheet.Cells[endingSummaryTableRow, i+7].Address})";
                    gmReportWorksheet.Cells[row, i + 8].Formula = $"=({gmReportWorksheet.Cells[row, i+7].Address}/{gmReportWorksheet.Cells[row, i+1].Address})";

                    mergedCells = gmReportWorksheet.Cells[row, i + 1, row, i + 8];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Numberformat.Format = currencyFormat;
                    mergedCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    mergedCells.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    mergedCells.Style.Font.Size = 11;
                    mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin ;
                    mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Double ;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var enclosure = gmReportWorksheet.Cells[firstRowEnclosure, 2, lastRowEnclosure, 2];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                enclosure = gmReportWorksheet.Cells[firstRowEnclosure, 3, lastRowEnclosure, 10];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                enclosure = gmReportWorksheet.Cells[firstRowEnclosure, 11, lastRowEnclosure, 18];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                enclosure = gmReportWorksheet.Cells[firstRowEnclosure, 19, lastRowEnclosure, 26];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                #endregion == Summary Contents ==

                #endregion -- Summary Rows --

                // Auto-fit columns for better readability
                gmReportWorksheet.Cells.AutoFitColumns();
                gmReportWorksheet.View.FreezePanes(8, 1);


                for (int col = 1; col <= 25; col++)
                {
                    double currentWidth = gmReportWorksheet.Column(col).Width;
                    if (currentWidth > 25)
                    {
                        gmReportWorksheet.Column(col).Width = 24;
                    }
                }

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

        #region -- Generate AR Per Customer Excel File --

        public async Task<IActionResult> GenerateArPerCustomerExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var salesInvoice = await _dbContext.FilprideSalesInvoices
                .Where(si => si.PostedBy != null)
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
            string currencyFormatNoDecimal = "#,##0";

            var totalQuantity = 0m;
            var totalUnitPrice = 0m;
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
                worksheet.Cells[row, 5].Value = si.TransactionDate.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 6].Value = si.DueDate.ToString("dd-MMM-yyyy");
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

                worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatNoDecimal;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;

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
            worksheet.Cells[row, 15].Value = totalGrossAmount/totalQuantity;
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

            worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatNoDecimal;
            worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 23].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;

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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ArPerCustomerReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion

        #region -- Generate Aging Report Excel File --

        public async Task<IActionResult> GenerateAgingReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                        .GetAllAsync(si => si.PostedBy != null && si.AmountPaid == 0 && !si.IsPaid, cancellationToken);

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
            string currencyFormat = "#,##0.0000";
            string currencyFormatTwoDecimal = "#,##0.00";

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
                decimal netOfVatAmount = si.Customer?.VatType == SD.VatType_Vatable ? netDiscount / 1.12m : netDiscount;
                decimal withHoldingTaxAmount = (si.Customer?.WithHoldingTax ?? false) ? (netDiscount / 1.12m) * 0.01m : 0;
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
                worksheet.Cells[row, 6].Value = si.TransactionDate.ToString("dd-MMM-yyyy");
                worksheet.Cells[row, 7].Value = si.DueDate.ToString("dd-MMM-yyyy");
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AgingReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion
    }
}
