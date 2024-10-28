using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;

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
                    var cosSummary = await _unitOfWork.FilprideReport.GetCosUnserveVolume(model.DateFrom, model.DateTo, companyClaims);

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

        [HttpGet]
        public IActionResult DispatchReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DispatchReport(string? reportType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(reportType))
            {
                return BadRequest();
            }

            var companyClaims = await GetCompanyClaimAsync();
            var currentUser = _userManager.GetUserName(User);
            var today = DateTime.Now;
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                .GetAllAsync(i => i.Company == companyClaims
                    && i.AuthorityToLoadNo != null
                    && i.Date >= firstDayOfMonth
                    && i.Date <= lastDayOfMonth
                    && (reportType == "AllDeliveries" || i.Status == nameof(Status.Pending)), cancellationToken);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Dispatch Report");

                // Insert image from root directory
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride.jpg"); // Update this to your image file name
                var picture = worksheet.Drawings.AddPicture("CompanyLogo", new FileInfo(imagePath));
                picture.SetPosition(0, 0, 0, 0); // Adjust position as needed
                picture.SetSize(200, 60); // Adjust size as needed

                // Setting up the header
                worksheet.Cells["A5"].Value = "OPERATION - LOGISTICS";
                worksheet.Cells["A6"].Value = $"DISPATCH REPORT AS OF {lastDayOfMonth:dd MMM, yyyy}";
                worksheet.Cells["A7"].Value = reportType == "AllDeliveries"
                    ? "ALL DELIVERIES"
                    : "IN TRANSIT DELIVERIES";

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
                worksheet.Cells["M9"].Value = "DELIVERY DATE";
                worksheet.Cells["N9"].Value = "STATUS";
                worksheet.Cells["O9"].Value = "SUPPLIER";
                worksheet.Cells["P9"].Value = "COST";
                worksheet.Cells["Q9"].Value = "FREIGHT";
                worksheet.Cells["R9"].Value = "DEMURRAGE";
                worksheet.Cells["S9"].Value = "ECC";
                worksheet.Cells["T9"].Value = "TOTAL FREIGHT";

                int currentRow = 10;
                decimal sumOfCost = 0;
                decimal sumOfFreight = 0;
                decimal sumOfDemurrage = 0;
                decimal sumOfECC = 0;
                decimal sumOfTotalFreight = 0;

                foreach (var dr in deliveryReceipts.OrderBy(d => d.Date))
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
                    worksheet.Cells[currentRow, 13].Value = dr.DeliveredDate?.ToString("dd-MMM-yy");
                    worksheet.Cells[currentRow, 14].Value = dr.Status == nameof(Status.Pending) ? "IN TRANSIT" : dr.Status.ToUpper();
                    worksheet.Cells[currentRow, 15].Value = dr.PurchaseOrder.Supplier.SupplierName;

                    decimal cost = _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.PurchaseOrder.Price) * dr.Quantity;
                    worksheet.Cells[currentRow, 16].Value = cost;
                    sumOfCost += cost;

                    decimal freight = (dr.Freight > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.Freight) : dr.Freight) * dr.Quantity;
                    worksheet.Cells[currentRow, 17].Value = freight;
                    sumOfFreight += freight;

                    decimal demurrage = (dr.Demuragge > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.Demuragge) : dr.Demuragge);
                    worksheet.Cells[currentRow, 18].Value = demurrage;
                    sumOfDemurrage += demurrage;

                    decimal ecc = (dr.ECC > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(dr.ECC) : dr.ECC) * dr.Quantity;
                    worksheet.Cells[currentRow, 19].Value = ecc;
                    sumOfECC += ecc;

                    decimal totalFreight = dr.Freight + dr.ECC;
                    decimal netTotalFreight = (totalFreight > 0 ? _unitOfWork.FilprideDeliveryReceipt.ComputeNetOfVat(totalFreight) : totalFreight) * dr.Quantity;
                    worksheet.Cells[currentRow, 20].Value = netTotalFreight;
                    sumOfTotalFreight += netTotalFreight + dr.Demuragge;

                    currentRow++;
                }


                // Total row
                worksheet.Cells[currentRow + 1, 5].Value = "TOTAL";
                worksheet.Cells[currentRow + 1, 6].Value = deliveryReceipts.Sum(dr => dr.Quantity);
                worksheet.Cells[currentRow + 1, 7].Value = deliveryReceipts.Sum(dr => dr.TotalAmount);
                worksheet.Cells[currentRow + 1, 16].Value = sumOfCost;
                worksheet.Cells[currentRow + 1, 17].Value = sumOfFreight;
                worksheet.Cells[currentRow + 1, 18].Value = sumOfDemurrage;
                worksheet.Cells[currentRow + 1, 19].Value = sumOfECC;
                worksheet.Cells[currentRow + 1, 20].Value = sumOfTotalFreight;

                // Adding borders and bold styling to the total row
                using (var totalRowRange = worksheet.Cells[currentRow + 1, 1, currentRow + 1, 20]) // Whole row
                {
                    totalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    totalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    totalRowRange.Style.Font.Bold = true; // Make text bold
                    totalRowRange.Style.Numberformat.Format = "#,##0.00";
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
                worksheet.Cells["F,G,P:T"].Style.Numberformat.Format = "#,##0.00";

                using (var range = worksheet.Cells["A9:T9"])
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
                var fileName = "DispatchReport.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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
                    fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");

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

        //Generate as .csv file

        #region -- Generate DisbursmentBook .Csv File --

        public async Task<IActionResult> GenerateDisbursementBookCsvFile(ViewModelBook model, CancellationToken cancellationToken)
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DisbursementBook.xlsx");
        }

        #endregion -- Generate DisbursmentBook .Csv File --

        #region -- Generate CashReceiptBook .Csv File --

        public async Task<IActionResult> GenerateCashReceiptBookCsvFile(ViewModelBook model, CancellationToken cancellationToken)
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
                worksheet.Cells[row, 1].Value = cashReceipt.Date;
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CashReceiptBook.xlsx");
        }

        #endregion -- Generate CashReceiptBook .Csv File --

        #region -- Generate GeneralLedgerBook .Csv File --

        public async Task<IActionResult> GenerateGeneralLedgerBookCsvFile(ViewModelBook model, CancellationToken cancellationToken)
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
                worksheet.Cells[row, 1].Value = gl.Date;
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GeneralLedgerBook.xlsx");
        }

        #endregion -- Generate GeneralLedgerBook .Csv File --

        #region -- Generate InventoryBook .Csv File --

        public async Task<IActionResult> GenerateInventoryBookCsvFile(ViewModelBook model, CancellationToken cancellationToken)
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
                worksheet.Cells[row, 1].Value = inventory.Date;
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryBook.xlsx");
        }

        #endregion -- Generate InventoryBook .Csv File --

        #region -- Generate JournalBook .Csv File --

        public async Task<IActionResult> GenerateJournalBookCsvFile(ViewModelBook model, CancellationToken cancellationToken)
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
                worksheet.Cells[row, 1].Value = jv.Date;
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "JournalBook.xlsx");
        }

        #endregion -- Generate JournalBook .Csv File --

        #region -- Generate PurchaseBook .Csv File --

        public async Task<IActionResult> GeneratePurchaseBookCsvFile(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo, CancellationToken cancellationToken)
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PurchaseBook.xlsx");
        }

        #endregion -- Generate PurchaseBook .Csv File --

        #region -- Generate SalesBook .Csv File --

        public async Task<IActionResult> GenerateSalesBookCsvFile(ViewModelBook model, string? selectedDocument, string? soaList, string? siList, CancellationToken cancellationToken)
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

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesBook.xlsx");
        }

        #endregion -- Generate SalesBook .Csv File --

    }
}