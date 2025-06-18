using System.Globalization;
using System.Linq.Expressions;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Integrated;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Color = System.Drawing.Color;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class AccountsReceivableReport : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public AccountsReceivableReport(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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

        [HttpGet]
        public IActionResult COSUnservedVolume()
        {
            return View();
        }

        #region -- Generated COS Unserved Volume Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GenerateCOSUnservedVolume(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(COSUnservedVolume));
            }

            try
            {
                var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo, companyClaims!);

                if (!cosSummary.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(COSUnservedVolume));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                            page.Size(PageSizes.Legal.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(50).Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item()
                                        .Text("COS UNSERVED VOLUME REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date From: ").SemiBold();
                                        text.Span(model.DateFrom.ToString(SD.Date_Format));
                                    });

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date To: ").SemiBold();
                                        text.Span(model.DateTo.ToString(SD.Date_Format));
                                    });
                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                #region -- Columns Definition

                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                #endregion

                                #region -- Table Header

                                    table.Header(header =>
                                    {
                                        header.Cell().ColumnSpan(11).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("SUMMARY OF BOOKED SALES").SemiBold();

                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date of Del").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO No.").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS No.").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Unserved Volume").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amount").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS Status").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Exp of COS").SemiBold();
                                    });

                                #endregion

                                #region -- Loop to Show Records

                                foreach (var record in cosSummary)
                                {
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Product?.ProductName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerPoNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlipNo);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.DeliveredPrice != 0 ? record.DeliveredPrice < 0 ? $"({Math.Abs(record.DeliveredPrice).ToString(SD.Four_Decimal_Format)})" : record.DeliveredPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(record.DeliveredPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.TotalAmount != 0 ? record.TotalAmount < 0 ? $"({Math.Abs(record.TotalAmount).ToString(SD.Two_Decimal_Format)})" : record.TotalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(record.TotalAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).Text("APPROVED");
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ExpirationDate.ToString());
                                }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(7).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(cosSummary.Sum(cos => cos.Quantity - cos.DeliveredQuantity) != 0 ? cosSummary.Sum(cos => cos.Quantity - cos.DeliveredQuantity) < 0 ? $"({Math.Abs(cosSummary.Sum(cos => cos.Quantity - cos.DeliveredQuantity)).ToString(SD.Two_Decimal_Format)})" : cosSummary.Sum(cos => cos.Quantity - cos.DeliveredQuantity).ToString(SD.Two_Decimal_Format) : null).FontColor(cosSummary.Sum(cos => cos.Quantity - cos.DeliveredQuantity) < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(cosSummary.Sum(cos => cos.TotalAmount) != 0 ? cosSummary.Sum(cos => cos.TotalAmount) < 0 ? $"({Math.Abs(cosSummary.Sum(cos => cos.TotalAmount)).ToString(SD.Two_Decimal_Format)})" : cosSummary.Sum(cos => cos.TotalAmount).ToString(SD.Two_Decimal_Format) : null).FontColor(cosSummary.Sum(cos => cos.TotalAmount) < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().ColumnSpan(2).Border(0.5f);

                                #endregion
                            });
                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cos unserved volume report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(COSUnservedVolume));
            }
        }

        #endregion

        #region -- Generate COS Unserved Volume as Excel File --

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
                        worksheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#9966ff"));
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

        [HttpGet]
        public IActionResult DispatchReport()
        {
            return View();
        }

        #region -- Generated Dispatch Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedDispatchReport(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(DispatchReport));
            }

            try
            {
                if (string.IsNullOrEmpty(viewModel.ReportType))
                {
                    return BadRequest();
                }

                var firstDayOfMonth = new DateOnly(viewModel.DateFrom.Year, viewModel.DateFrom.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(i => i.Company == companyClaims
                                      && i.AuthorityToLoadNo != null
                                      && i.Date >= firstDayOfMonth
                                      && i.Date <= lastDayOfMonth
                                      && (viewModel.ReportType == "AllDeliveries" || i.Status == nameof(DRStatus.PendingDelivery)), cancellationToken);

                if (!deliveryReceipts.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(DispatchReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("DISPATCH REPORT")
                                    .FontSize(20).SemiBold();

                                column.Item().Text(text =>
                                {
                                    text.Span("Date From: ").SemiBold();
                                    text.Span(firstDayOfMonth.ToString(SD.Date_Format));
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Date To: ").SemiBold();
                                    text.Span(lastDayOfMonth.ToString(SD.Date_Format));
                                });
                            });

                            row.ConstantItem(size: 100)
                                .Height(50)
                                .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                        });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                #region -- Columns Definition

                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                #endregion

                                #region -- Table Header

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Type").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Products").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Pick-up Point").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ATL#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Hauler Name").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight Charge").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ECC").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total Freight").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Delivery Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Status").SemiBold();
                                    });

                                #endregion

                                #region -- Initialize Variable for Computation

                                    decimal totalQuantity = 0m;
                                    decimal totalFreightCharge = 0m;
                                    decimal totalECC = 0m;
                                    decimal totalFreightAmount = 0m;

                                #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in deliveryReceipts)
                                    {
                                        var quantity = record.Quantity;
                                        var freightCharge = record.Freight;
                                        var ecc = record.ECC;
                                        var totalFreight = quantity * (freightCharge + ecc);

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.Product?.ProductName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity != 0 ? quantity < 0 ? $"({Math.Abs(quantity).ToString(SD.Two_Decimal_Format)})" : quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.PickUpPoint?.Depot);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.AuthorityToLoadNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerOrderSlipNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Hauler?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.Supplier?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightCharge != 0 ? freightCharge < 0 ? $"({Math.Abs(freightCharge).ToString(SD.Four_Decimal_Format)})" : freightCharge.ToString(SD.Four_Decimal_Format) : null).FontColor(freightCharge < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(ecc != 0 ? ecc < 0 ? $"({Math.Abs(ecc).ToString(SD.Four_Decimal_Format)})" : ecc.ToString(SD.Four_Decimal_Format) : null).FontColor(ecc < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalFreight != 0 ? totalFreight < 0 ? $"({Math.Abs(totalFreight).ToString(SD.Two_Decimal_Format)})" : totalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveredDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Status);

                                        totalQuantity += quantity;
                                        totalFreightCharge += freightCharge;
                                        totalECC += ecc;
                                        totalFreightAmount += totalFreight;
                                    }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantity != 0 ? totalQuantity < 0 ? $"({Math.Abs(totalQuantity).ToString(SD.Two_Decimal_Format)})" : totalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten1).Border(0.5f);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightCharge != 0 ? totalFreightCharge < 0 ? $"({Math.Abs(totalFreightCharge).ToString(SD.Four_Decimal_Format)})" : totalFreightCharge.ToString(SD.Four_Decimal_Format) : null).FontColor(totalFreightCharge < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalECC != 0 ? totalECC < 0 ? $"({Math.Abs(totalECC).ToString(SD.Four_Decimal_Format)})" : totalECC.ToString(SD.Four_Decimal_Format) : null).FontColor(totalECC < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightAmount != 0 ? totalFreightAmount < 0 ? $"({Math.Abs(totalFreightAmount).ToString(SD.Two_Decimal_Format)})" : totalFreightAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreightAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f);
                                #endregion
                            });
                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate dispatch report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(DispatchReport));
            }
        }

        #endregion

        #region -- Generate Dispatch Report Excel File --
        public async Task<IActionResult> GenerateDispatchReportExcelFile(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (viewModel.DateFrom == default && viewModel.ReportType != "InTransit")
            {
                throw new NullReferenceException("Please enter a valid Date From");
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
                Expression<Func<FilprideDeliveryReceipt, bool>>? filter = default;
                string dateRangeType = viewModel.DateTo.HasValue ? "ByRange" : "AsOf";
                string currencyFormatTwoDecimal = "#,##0.00";

                if(viewModel.ReportType == "Delivered")
                {
                    if (dateRangeType == "AsOf")
                    {
                        filter = i => i.Company == companyClaims
                                      && i.Date <= viewModel.DateFrom
                                      && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                    }
                    else
                    {
                        filter = i => i.Company == companyClaims
                                      && i.Date >= viewModel.DateFrom
                                      && i.Date <= viewModel.DateTo
                                      && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                    }
                }
                else
                {
                    filter = i => i.Company == companyClaims
                                  && i.Status == nameof(DRStatus.PendingDelivery);
                }

                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(filter, cancellationToken);

                deliveryReceipts = deliveryReceipts.OrderBy(dr => dr.Date);

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

                    if (dateRangeType == "AsOf")
                    {
                        if (viewModel.DateFrom == default)
                        {
                            mergedCellsA6.Value = $"DISPATCH REPORT AS OF {DateTimeHelper.GetCurrentPhilippineTime():dd MMM, yyyy}";
                        }
                        else
                        {
                            mergedCellsA6.Value = $"DISPATCH REPORT AS OF {viewModel.DateFrom:dd MMM, yyyy}";
                        }
                    }
                    else
                    {
                        mergedCellsA6.Value = $"DISPATCH REPORT AS OF {viewModel.DateTo:dd MMM, yyyy}";
                    }

                    var mergedCellsA7 = worksheet.Cells["A7:B7"];
                    mergedCellsA7.Merge = true;
                    mergedCellsA7.Value = viewModel.ReportType == "Delivered" ? "DELIVERED" : "IN TRANSIT";

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

                    //TODO Remove this in the future
                    worksheet.Cells["P9"].Value = "OTC COS No.";
                    worksheet.Cells["Q9"].Value = "OTC DR No.";

                    if (viewModel.ReportType == "Delivered")
                    {
                        worksheet.Cells["R9"].Value = "DELIVERED DATE";
                        worksheet.Cells["S9"].Value = "STATUS";
                    }

                    int currentRow = 10;
                    string headerColumn = viewModel.ReportType == "Delivered" ? "S9" : "Q9";
                    int grandTotalColumn = viewModel.ReportType == "Delivered" ? 19 : 17;
                    decimal grandSumOfTotalFreightAmount = 0;
                    decimal grandTotalQuantity = 0;

                    foreach (var dr in deliveryReceipts)
                    {
                        var quantity = dr.Quantity;
                        var freightCharge = dr.Freight;
                        var ecc = dr.ECC;
                        var totalFreightAmount = quantity * (freightCharge + ecc);

                        if (viewModel.ReportType == "Delivered" && dateRangeType == "AsOf" &&
                            dr.Date != viewModel.DateFrom)
                        {
                            // Don't show record of other dates if entry is "as of" and "delivered"
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 1].Value = dr.Date;
                            worksheet.Cells[currentRow, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                            worksheet.Cells[currentRow, 2].Value = dr.Customer?.CustomerName;
                            worksheet.Cells[currentRow, 3].Value = dr.Customer?.CustomerType;
                            worksheet.Cells[currentRow, 4].Value = dr.DeliveryReceiptNo;
                            worksheet.Cells[currentRow, 5].Value = dr.PurchaseOrder!.Product!.ProductName;
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

                            if (viewModel.ReportType == "Delivered")
                            {
                                worksheet.Cells[currentRow, 18].Value = dr.DeliveredDate;
                                worksheet.Cells[currentRow, 18].Style.Numberformat.Format = "MMM/dd/yyyy";
                                worksheet.Cells[currentRow, 19].Value = dr.Status == nameof(DRStatus.PendingDelivery) ? "IN TRANSIT" : dr.Status.ToUpper();
                            }

                            currentRow++;
                        }

                        grandTotalQuantity += quantity;
                        grandSumOfTotalFreightAmount += totalFreightAmount;
                    }

                    // Grand Total row
                    worksheet.Cells[currentRow, 5].Value = "GRAND TOTAL";

                    if (viewModel.ReportType == "Delivered" && dateRangeType == "AsOf")
                    {
                        // Don't add record of other dates if entry is "as of" and "delivered"
                        var entriesToday = deliveryReceipts.Where(t => t.Date == viewModel.DateFrom);
                        worksheet.Cells[currentRow, 6].Value = entriesToday.Sum(dr => dr.Quantity);
                        worksheet.Cells[currentRow, 15].Value = entriesToday.Sum(dr => (dr.Quantity * dr.Freight));
                    }
                    else
                    {
                        worksheet.Cells[currentRow, 6].Value = grandTotalQuantity;
                        worksheet.Cells[currentRow, 15].Value = grandSumOfTotalFreightAmount;
                    }

                    // Adding borders and bold styling to the total row
                    using (var totalRowRange = worksheet.Cells[currentRow, 1, currentRow, grandTotalColumn]) // Whole row
                    {
                        totalRowRange.Style.Font.Bold = true; // Make text bold
                        totalRowRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        totalRowRange.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }

                    currentRow += 3;
                    var startOfSummary = currentRow;

                    // Generated by, checked by, received by footer
                    worksheet.Cells[currentRow, 1, currentRow, 2].Merge = true;
                    worksheet.Cells[currentRow, 1].Value = "Generated by:";
                    worksheet.Cells[currentRow, 4].Value = "Noted & Checked by:";
                    worksheet.Cells[currentRow, 8].Value = "Received by:";

                    currentRow += 1;

                    worksheet.Cells[currentRow, 1, currentRow, 2].Merge = true;
                    worksheet.Cells[currentRow, 1].Value = currentUser.ToUpper();

                    currentRow += 1;

                    worksheet.Cells[currentRow, 1, currentRow, 2].Merge = true;
                    worksheet.Cells[currentRow, 1].Value = $"Date & Time: {today:MM/dd/yyyy - hh:mm tt}";
                    worksheet.Cells[currentRow, 4].Value = "LOGISTICS SUPERVISOR";
                    worksheet.Cells[currentRow, 8].Value = "CNC SUPERVISOR";

                    // Styling and formatting (optional)
                    worksheet.Cells["M:N"].Style.Numberformat.Format = "#,##0.0000";
                    worksheet.Cells["F,O"].Style.Numberformat.Format = "#,##0.00";

                    using (var range = worksheet.Cells[$"A9:{headerColumn}"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 102, 204));
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    // Summary
                    if (dateRangeType == "AsOf" && viewModel.ReportType == "Delivered")
                    {
                        string[] customerTypes = { "Retail", "Industrial", "Government" };
                        var groupByCustomerType = deliveryReceipts.GroupBy(dr => dr.Customer!.CustomerType);

                        foreach (var customerType in customerTypes)
                        {
                            string[] productList = { "BIODIESEL", "ECONOGAS", "ENVIROGAS" };
                            using (var range = worksheet.Cells[startOfSummary, 11, startOfSummary, 15])
                            {
                                range.Style.Font.Bold = true;
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            }
                            worksheet.Cells[startOfSummary, 11].Value = customerType;
                            worksheet.Cells[startOfSummary, 11].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 204, 204));
                            worksheet.Cells[startOfSummary, 12].Value = "TOTAL (VOLUME)";
                            worksheet.Cells[startOfSummary, 12].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 156));
                            worksheet.Cells[startOfSummary, 13].Value = "BIODIESEL";
                            worksheet.Cells[startOfSummary, 13].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 4));
                            worksheet.Cells[startOfSummary, 14].Value = "ECONOGAS";
                            worksheet.Cells[startOfSummary, 14].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 156, 100));
                            worksheet.Cells[startOfSummary, 15].Value = "ENVIROGAS";
                            worksheet.Cells[startOfSummary, 15].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255,4,4));

                            startOfSummary++;
                            var selectedGroup = groupByCustomerType.FirstOrDefault(g => g.Key == customerType);
                            worksheet.Cells[startOfSummary, 11].Value = "TOTAL TODAY";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Where(t => t.Date == viewModel.DateFrom).Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup
                                    .Where(dr => dr.Date == viewModel.DateFrom)
                                    .GroupBy(dr => dr.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            startOfSummary++;
                            worksheet.Cells[startOfSummary, 11].Value = "CUM. AS OF YESTERDAY";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Where(dr => dr.Date < viewModel.DateFrom).Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup
                                    .Where(dr => dr.Date < viewModel.DateFrom)
                                    .GroupBy(g => g.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            startOfSummary++;
                            worksheet.Cells[startOfSummary, 11].Value = "MONTH TO DATE";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup.GroupBy(g => g.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            worksheet.Cells[startOfSummary, 11, startOfSummary, 15].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            startOfSummary += 2;

                        }

                        // All product types
                        using (var testTemp = worksheet.Cells[1, 1])
                        {
                            string[] productList = { "BIODIESEL", "ECONOGAS", "ENVIROGAS" };
                            using (var range = worksheet.Cells[startOfSummary, 11, startOfSummary, 15])
                            {
                                range.Style.Font.Bold = true;
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            }
                            worksheet.Cells[startOfSummary, 11].Value = "ALL";
                            worksheet.Cells[startOfSummary, 11].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 204, 204));
                            worksheet.Cells[startOfSummary, 12].Value = "TOTAL (VOLUME)";
                            worksheet.Cells[startOfSummary, 12].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 156));
                            worksheet.Cells[startOfSummary, 13].Value = "BIODIESEL";
                            worksheet.Cells[startOfSummary, 13].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 4));
                            worksheet.Cells[startOfSummary, 14].Value = "ECONOGAS";
                            worksheet.Cells[startOfSummary, 14].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 156, 100));
                            worksheet.Cells[startOfSummary, 15].Value = "ENVIROGAS";
                            worksheet.Cells[startOfSummary, 15].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255,4,4));

                            startOfSummary++;
                            var selectedGroup = deliveryReceipts;
                            worksheet.Cells[startOfSummary, 11].Value = "TOTAL TODAY";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Where(t => t.Date == viewModel.DateFrom).Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup
                                    .Where(dr => dr.Date == viewModel.DateFrom)
                                    .GroupBy(dr => dr.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            startOfSummary++;
                            worksheet.Cells[startOfSummary, 11].Value = "CUM. AS OF YESTERDAY";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Where(dr => dr.Date < viewModel.DateFrom).Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup
                                    .Where(dr => dr.Date < viewModel.DateFrom)
                                    .GroupBy(g => g.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            startOfSummary++;
                            worksheet.Cells[startOfSummary, 11].Value = "MONTH TO DATE";
                            worksheet.Cells[startOfSummary, 11].Style.Font.Bold = true;

                            if (selectedGroup != null)
                            {
                                decimal totalToday = selectedGroup.Sum(dr => dr.Quantity);
                                worksheet.Cells[startOfSummary, 12].Value = totalToday;
                                worksheet.Cells[startOfSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                int columnStart = 13;
                                var groupByProduct = selectedGroup.GroupBy(g => g.PurchaseOrder?.Product!.ProductName);
                                foreach (string product in productList)
                                {
                                    decimal selectedGroupByProduct = groupByProduct
                                        .FirstOrDefault(g => g.FirstOrDefault()?.PurchaseOrder?.Product!.ProductName == product)?
                                        .Sum(dr => dr.Quantity) ?? 0m;
                                    worksheet.Cells[startOfSummary, columnStart].Value = selectedGroupByProduct;
                                    worksheet.Cells[startOfSummary, columnStart].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                    columnStart++;
                                }
                            }

                            worksheet.Cells[startOfSummary, 11, startOfSummary, 15].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            startOfSummary += 2;
                        }
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

        public IActionResult SalesReport()
        {
            return View();
        }

        #region -- Generated Sales Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedSalesReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(SalesReport));
            }

            try
            {
                var sales = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims!);

                if (!sales.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(SalesReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("SALES REPORT")
                                    .FontSize(20).SemiBold();

                                column.Item().Text(text =>
                                {
                                    text.Span("Date From: ").SemiBold();
                                    text.Span(model.DateFrom.ToString(SD.Date_Format));
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Date To: ").SemiBold();
                                    text.Span(model.DateTo.ToString(SD.Date_Format));
                                });
                            });

                            row.ConstantItem(size: 100)
                                .Height(50)
                                .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                        });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                #region -- Columns Definition

                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                #endregion

                                #region -- Table Header

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date Delivered").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Segment").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Specialist").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("SI#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO#").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Delivery Option").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Items").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Sales G. VAT").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("VAT").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Sales N. VAT").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight N. VAT").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Commission").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Commissionee").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Remarks").SemiBold();
                                    });

                                #endregion

                                #region -- Initialize Variable for Computation

                                    var totalFreight = 0m;
                                    var totalVat = 0m;
                                    var totalSalesNetOfVat = 0m;
                                    var totalFreightNetOfVat = 0m;
                                    var totalCommissionRate = 0m;

                                    var overallTotalQuantity = 0m;
                                    var overallTotalAmount = 0m;

                                #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in sales)
                                    {
                                        var quantity = record.DeliveryReceipt.Quantity;
                                        var freight = (record.DeliveryReceipt?.Freight ?? 0) * quantity;
                                        var freightNetOfVat = freight / 1.12m;
                                        var salesNetOfVat = record.DeliveryReceipt!.TotalAmount != 0 ? record.DeliveryReceipt.TotalAmount / 1.12m : 0;
                                        var vat = salesNetOfVat * .12m;

                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.DeliveredDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.Customer?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.AccountSpecialist);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoiceNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.DeliveryReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.PurchaseOrder?.PurchaseOrderNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.DeliveryOption);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.Product?.ProductName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity != 0 ? quantity < 0 ? $"({Math.Abs(quantity).ToString(SD.Two_Decimal_Format)})" : quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freight != 0 ? freight < 0 ? $"({Math.Abs(freight).ToString(SD.Two_Decimal_Format)})" : freight.ToString(SD.Two_Decimal_Format) : null).FontColor(freight < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.TotalAmount != 0 ? record.DeliveryReceipt.TotalAmount < 0 ? $"({Math.Abs(record.DeliveryReceipt.TotalAmount).ToString(SD.Two_Decimal_Format)})" : record.DeliveryReceipt.TotalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(record.DeliveryReceipt.TotalAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vat != 0 ? vat < 0 ? $"({Math.Abs(vat).ToString(SD.Two_Decimal_Format)})" : vat.ToString(SD.Two_Decimal_Format) : null).FontColor(vat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(salesNetOfVat != 0 ? salesNetOfVat < 0 ? $"({Math.Abs(salesNetOfVat).ToString(SD.Two_Decimal_Format)})" : salesNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(salesNetOfVat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightNetOfVat != 0 ? freightNetOfVat < 0 ? $"({Math.Abs(freightNetOfVat).ToString(SD.Two_Decimal_Format)})" : freightNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(freightNetOfVat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate != 0 ? record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate < 0 ? $"({Math.Abs(record.DeliveryReceipt.CustomerOrderSlip.CommissionRate).ToString(SD.Four_Decimal_Format)})" : record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate.ToString(SD.Four_Decimal_Format) : null).FontColor(record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.Commissionee?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.Remarks);

                                        overallTotalQuantity += record.DeliveryReceipt.Quantity;
                                        totalFreight += freight;
                                        overallTotalAmount += record.DeliveryReceipt.TotalAmount;
                                        totalVat += vat;
                                        totalSalesNetOfVat += salesNetOfVat;
                                        totalFreightNetOfVat += freightNetOfVat;
                                        totalCommissionRate += record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate ?? 0;
                                    }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(10).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity != 0 ? overallTotalQuantity < 0 ? $"({Math.Abs(overallTotalQuantity).ToString(SD.Two_Decimal_Format)})" : overallTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreight != 0 ? totalFreight < 0 ? $"({Math.Abs(totalFreight).ToString(SD.Two_Decimal_Format)})" : totalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreight < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalAmount != 0 ? overallTotalAmount < 0 ? $"({Math.Abs(overallTotalAmount).ToString(SD.Two_Decimal_Format)})" : overallTotalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVat != 0 ? totalVat < 0 ? $"({Math.Abs(totalVat).ToString(SD.Two_Decimal_Format)})" : totalVat.ToString(SD.Two_Decimal_Format) : null).FontColor(totalVat < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat != 0 ? totalSalesNetOfVat < 0 ? $"({Math.Abs(totalSalesNetOfVat).ToString(SD.Two_Decimal_Format)})" : totalSalesNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(totalSalesNetOfVat < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightNetOfVat != 0 ? totalFreightNetOfVat < 0 ? $"({Math.Abs(totalFreightNetOfVat).ToString(SD.Two_Decimal_Format)})" : totalFreightNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreightNetOfVat < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCommissionRate != 0 ? totalCommissionRate < 0 ? $"({Math.Abs(totalCommissionRate).ToString(SD.Four_Decimal_Format)})" : totalCommissionRate.ToString(SD.Four_Decimal_Format) : null).FontColor(totalCommissionRate < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f);

                                #endregion

                                //Summary Table
                                col.Item().PaddingTop(50).Text("SUMMARY").Bold().FontSize(14);

                                #region -- Overall Summary

                                    col.Item().PaddingTop(10).Table(table =>
                                    {
                                        #region -- Columns Definition

                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.ConstantColumn(5);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.ConstantColumn(5);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.ConstantColumn(5);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });

                                        #endregion

                                        #region -- Table Header

                                            table.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Overall").AlignCenter().SemiBold();
                                                header.Cell();
                                                header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Biodiesel").AlignCenter().SemiBold();
                                                header.Cell();
                                                header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Econogas").AlignCenter().SemiBold();
                                                header.Cell();
                                                header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Envirogas").AlignCenter().SemiBold();

                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Segment").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();
                                                header.Cell();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();
                                                header.Cell();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();
                                                header.Cell();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();

                                            });

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                        var totalQuantityForBiodiesel = 0m;
                                        var totalAmountForBiodiesel = 0m;

                                        var totalQuantityForEconogas = 0m;
                                        var totalAmountForEconogas = 0m;

                                        var totalQuantityForEnvirogas = 0m;
                                        var totalAmountForEnvirogas  = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                #region Computation for Overall

                                                var list = sales.Where(s => s.DeliveryReceipt.Customer?.CustomerType == customerType.ToString()).ToList();

                                                var overAllQuantitySum = list.Sum(s => s.DeliveryReceipt.Quantity);
                                                var overallAmountSum = list.Sum(s => s.DeliveryReceipt.TotalAmount);
                                                var overallNetOfAmountSum = overallAmountSum != 0m ? overallAmountSum / 1.12m : 0;
                                                var overallAverageSellingPrice = overallNetOfAmountSum != 0m || overAllQuantitySum != 0m ? overallNetOfAmountSum / overAllQuantitySum : 0m;

                                                #endregion

                                                #region Computation for Biodiesel

                                                var listForBiodiesel = list.Where(s => s.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL").ToList();

                                                var biodieselQuantitySum = listForBiodiesel.Sum(s => s.DeliveryReceipt.Quantity);
                                                var biodieselAmountSum = listForBiodiesel.Sum(s => s.DeliveryReceipt.TotalAmount);
                                                var biodieselNetOfAmountSum = biodieselAmountSum != 0m ? biodieselAmountSum / 1.12m : 0;
                                                var biodieselAverageSellingPrice = biodieselNetOfAmountSum != 0m || biodieselQuantitySum != 0m ? biodieselNetOfAmountSum / biodieselQuantitySum : 0m;

                                                #endregion

                                                #region Computation for Econogas

                                                var listForEconogas = list.Where(s => s.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS").ToList();

                                                var econogasQuantitySum = listForEconogas.Sum(s => s.DeliveryReceipt.Quantity);
                                                var econogasAmountSum = listForEconogas.Sum(s => s.DeliveryReceipt.TotalAmount);
                                                var econogasNetOfAmountSum = econogasAmountSum != 0m ? econogasAmountSum / 1.12m : 0;
                                                var econogasAverageSellingPrice = econogasNetOfAmountSum != 0m && econogasQuantitySum != 0m ? econogasNetOfAmountSum / econogasQuantitySum : 0m;

                                                #endregion

                                                #region Computation for Envirogas

                                                var listForEnvirogas = list.Where(s => s.DeliveryReceipt.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS").ToList();

                                                var envirogasQuantitySum = listForEnvirogas.Sum(s => s.DeliveryReceipt.Quantity);
                                                var envirogasAmountSum = listForEnvirogas.Sum(s => s.DeliveryReceipt.TotalAmount);
                                                var envirogasNetOfAmountSum = envirogasAmountSum != 0m ? envirogasAmountSum / 1.12m : 0;
                                                var envirogasAverageSellingPrice = envirogasNetOfAmountSum != 0m && envirogasQuantitySum != 0m ? envirogasNetOfAmountSum / envirogasQuantitySum : 0m;

                                                #endregion

                                                table.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overAllQuantitySum != 0 ? overAllQuantitySum < 0 ? $"({Math.Abs(overAllQuantitySum).ToString(SD.Two_Decimal_Format)})" : overAllQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(overAllQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfAmountSum != 0 ? overallNetOfAmountSum < 0 ? $"({Math.Abs(overallNetOfAmountSum).ToString(SD.Two_Decimal_Format)})" : overallNetOfAmountSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetOfAmountSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallAverageSellingPrice != 0 ? overallAverageSellingPrice < 0 ? $"({Math.Abs(overallAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : overallAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(overallAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselQuantitySum != 0 ? biodieselQuantitySum < 0 ? $"({Math.Abs(biodieselQuantitySum).ToString(SD.Two_Decimal_Format)})" : biodieselQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetOfAmountSum != 0 ? biodieselNetOfAmountSum < 0 ? $"({Math.Abs(biodieselNetOfAmountSum).ToString(SD.Two_Decimal_Format)})" : biodieselNetOfAmountSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselNetOfAmountSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselAverageSellingPrice != 0 ? biodieselAverageSellingPrice < 0 ? $"({Math.Abs(biodieselAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : biodieselAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(biodieselAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasQuantitySum != 0 ? econogasQuantitySum < 0 ? $"({Math.Abs(econogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : econogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetOfAmountSum != 0 ? econogasNetOfAmountSum < 0 ? $"({Math.Abs(econogasNetOfAmountSum).ToString(SD.Two_Decimal_Format)})" : econogasNetOfAmountSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasNetOfAmountSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasAverageSellingPrice != 0 ? econogasAverageSellingPrice < 0 ? $"({Math.Abs(econogasAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : econogasAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(econogasAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasQuantitySum != 0 ? envirogasQuantitySum < 0 ? $"({Math.Abs(envirogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : envirogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetOfAmountSum != 0 ? envirogasNetOfAmountSum < 0 ? $"({Math.Abs(envirogasNetOfAmountSum).ToString(SD.Two_Decimal_Format)})" : envirogasNetOfAmountSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasNetOfAmountSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasAverageSellingPrice != 0 ? envirogasAverageSellingPrice < 0 ? $"({Math.Abs(envirogasAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : envirogasAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(envirogasAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);

                                                totalQuantityForBiodiesel += biodieselQuantitySum;
                                                totalAmountForBiodiesel += biodieselNetOfAmountSum;

                                                totalQuantityForEconogas += econogasQuantitySum;
                                                totalAmountForEconogas += econogasNetOfAmountSum;

                                                totalQuantityForEnvirogas += envirogasQuantitySum;
                                                totalAmountForEnvirogas += envirogasNetOfAmountSum;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            var averageSellingPriceForOverAll = totalSalesNetOfVat != 0 && overallTotalQuantity != 0 ? totalSalesNetOfVat / overallTotalQuantity : 0m;
                                            var averageSellingPriceForBiodiesel = totalAmountForBiodiesel != 0 && totalQuantityForBiodiesel != 0 ? totalAmountForBiodiesel / totalQuantityForBiodiesel : 0m;
                                            var averageSellingPriceForEconogas = totalAmountForEconogas != 0 && totalQuantityForEconogas != 0 ? totalAmountForEconogas / totalQuantityForEconogas : 0m;
                                            var averageSellingPriceForEnvirogas = totalAmountForEnvirogas != 0 && totalQuantityForEnvirogas != 0 ? totalAmountForEnvirogas / totalQuantityForEnvirogas : 0m;

                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity != 0 ? overallTotalQuantity < 0 ? $"({Math.Abs(overallTotalQuantity).ToString(SD.Two_Decimal_Format)})" : overallTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat != 0 ? totalSalesNetOfVat < 0 ? $"({Math.Abs(totalSalesNetOfVat).ToString(SD.Two_Decimal_Format)})" : totalSalesNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(totalSalesNetOfVat < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForOverAll != 0 ? averageSellingPriceForOverAll < 0 ? $"({Math.Abs(averageSellingPriceForOverAll).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForOverAll.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForOverAll < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForBiodiesel != 0 ? totalQuantityForBiodiesel < 0 ? $"({Math.Abs(totalQuantityForBiodiesel).ToString(SD.Two_Decimal_Format)})" : totalQuantityForBiodiesel.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForBiodiesel != 0 ? totalAmountForBiodiesel < 0 ? $"({Math.Abs(totalAmountForBiodiesel).ToString(SD.Two_Decimal_Format)})" : totalAmountForBiodiesel.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForBiodiesel != 0 ? averageSellingPriceForBiodiesel < 0 ? $"({Math.Abs(averageSellingPriceForBiodiesel).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForBiodiesel.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEconogas != 0 ? totalQuantityForEconogas < 0 ? $"({Math.Abs(totalQuantityForEconogas).ToString(SD.Two_Decimal_Format)})" : totalQuantityForEconogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEconogas != 0 ? totalAmountForEconogas < 0 ? $"({Math.Abs(totalAmountForEconogas).ToString(SD.Two_Decimal_Format)})" : totalAmountForEconogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForEconogas != 0 ? averageSellingPriceForEconogas < 0 ? $"({Math.Abs(averageSellingPriceForEconogas).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForEconogas.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEnvirogas != 0 ? totalQuantityForEnvirogas < 0 ? $"({Math.Abs(totalQuantityForEnvirogas).ToString(SD.Two_Decimal_Format)})" : totalQuantityForEnvirogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEnvirogas != 0 ? totalAmountForEnvirogas < 0 ? $"({Math.Abs(totalAmountForEnvirogas).ToString(SD.Two_Decimal_Format)})" : totalAmountForEnvirogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForEnvirogas != 0 ? averageSellingPriceForEnvirogas < 0 ? $"({Math.Abs(averageSellingPriceForEnvirogas).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForEnvirogas.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                        #endregion
                                    });

                                #endregion

                            });
                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(SalesReport));
            }
        }

        #endregion

        #region -- Generated Sales Report as Excel File --

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

                var salesReport = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
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

        [HttpGet]
        public IActionResult PostedCollection()
        {
            return View();
        }

        #region -- Generated Posted Collection Report as Quest PDF

        public async Task<IActionResult> GeneratePostedCollection(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PostedCollection));
            }

            try
            {
                var collectionReceiptReport = await _unitOfWork.FilprideReport
                    .GetCollectionReceiptReport(model.DateFrom, model.DateTo, companyClaims!);

                if (!collectionReceiptReport.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(PostedCollection));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Legal.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(50).Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item()
                                        .Text("COLLECTION")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date From: ").SemiBold();
                                        text.Span(model.DateFrom.ToString(SD.Date_Format));
                                    });

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date To: ").SemiBold();
                                        text.Span(model.DateTo.ToString(SD.Date_Format));
                                    });
                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Acc. Type").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Tran. Date(INV)").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Invoice No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Due Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date of Check").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Bank").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Check No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amount").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

                                decimal currentAmount = 0;
                                decimal totalAmount = 0;

                                foreach (var record in collectionReceiptReport)
                                {
                                    currentAmount = (record.CashAmount + record.CheckAmount + record.ManagerCheckAmount);

                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerType);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.TransactionDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.SalesInvoiceNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.DueDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CheckDate);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.BankAccount?.Bank);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CheckNo);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentAmount != 0 ? currentAmount < 0 ? $"({Math.Abs(currentAmount).ToString(SD.Two_Decimal_Format)})" : currentAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(currentAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                    totalAmount += currentAmount;
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmount != 0 ? totalAmount < 0 ? $"({Math.Abs(totalAmount).ToString(SD.Two_Decimal_Format)})" : totalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                            #endregion

                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate posted collection report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PostedCollection));
            }
        }

        #endregion

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
                        currentAmount = (cr.CashAmount + cr.CheckAmount + cr.ManagerCheckAmount);
                        worksheet.Cells[row, 1].Value = cr.Customer?.CustomerCode ?? "";
                        worksheet.Cells[row, 2].Value = cr.Customer?.CustomerName ?? "";
                        worksheet.Cells[row, 3].Value = cr.Customer?.CustomerType ?? "";
                        worksheet.Cells[row, 4].Value = cr.SalesInvoice?.TransactionDate ?? default;
                        worksheet.Cells[row, 5].Value = cr.SalesInvoice?.SalesInvoiceNo ?? "";
                        worksheet.Cells[row, 6].Value = cr.SalesInvoice?.DueDate ?? default;
                        worksheet.Cells[row, 7].Value = cr.CheckDate;
                        worksheet.Cells[row, 8].Value = $"{cr.BankAccount?.Bank} {cr.BankAccount?.AccountNo}";
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

                    int lastRow = row-1;

                    using (var range = worksheet.Cells[startingRow-1, 10, lastRow, 10])
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

        [HttpGet]
        public IActionResult AgingReport()
        {
            return View();
        }

        #region -- Generated Aging Report as Quest PDF

        public async Task<IActionResult> GeneratedAgingReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(AgingReport));
            }

            try
            {
                var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.PostedBy != null && si.AmountPaid == 0 && !si.IsPaid && si.Company == companyClaims, cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(AgingReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Legal.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(50).Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item()
                                        .Text("AGING REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date From: ").SemiBold();
                                        text.Span(model.DateFrom.ToString(SD.Date_Format));
                                    });

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date To: ").SemiBold();
                                        text.Span(model.DateTo.ToString(SD.Date_Format));
                                    });
                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Month").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Acc. Type").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Terms").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT%").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Sales Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Due Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Invoice No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR#").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Partial Collections").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Adjusted Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net of VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("VCF").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Retention Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Adjusted Net").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Days Due").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Current").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("1-30 Days").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("31-60 Days").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("61-90 Days").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Over 90 Days").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

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

                                foreach (var record in salesInvoice)
                                {

                                    var gross = record.Amount;
                                    decimal netDiscount = record.Amount - record.Discount;
                                    decimal netOfVatAmount = record.Customer?.VatType == SD.VatType_Vatable ? netDiscount / 1.12m : netDiscount;
                                    decimal withHoldingTaxAmount = (record.Customer?.WithHoldingTax ?? false) ? (netDiscount / 1.12m) * 0.01m : 0;
                                    decimal retentionAmount = (record.Customer?.RetentionRate ?? 0.0000m) * netOfVatAmount;
                                    decimal vcfAmount = 0.0000m;
                                    decimal adjustedGross = gross - vcfAmount;
                                    decimal adjustedNet = gross - vcfAmount - retentionAmount;

                                    DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                                    int daysDue = (today > record.DueDate) ? (today.DayNumber - record.DueDate.DayNumber) : 0;
                                    var current = (record.DueDate >= today) ? gross : 0.0000m;
                                    var oneToThirtyDays = (daysDue >= 1 && daysDue <= 30) ? gross : 0.0000m;
                                    var thirtyOneToSixtyDays = (daysDue >= 31 && daysDue <= 60) ? gross : 0.0000m;
                                    var sixtyOneToNinetyDays = (daysDue >= 61 && daysDue <= 90) ? gross : 0.0000m;
                                    var overNinetyDays = (daysDue > 90) ? gross : 0.0000m;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.TransactionDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerType);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerTerms);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer!.WithHoldingTax ? 1.ToString() : 0.ToString());
                                    table.Cell().Border(0.5f).Padding(3).Text(record.TransactionDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DueDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoiceNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(gross != 0 ? gross < 0 ? $"({Math.Abs(gross).ToString(SD.Two_Decimal_Format)})" : gross.ToString(SD.Two_Decimal_Format) : null).FontColor(gross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.AmountPaid != 0 ? record.AmountPaid < 0 ? $"({Math.Abs(record.AmountPaid).ToString(SD.Two_Decimal_Format)})" : record.AmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(record.AmountPaid < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(adjustedGross != 0 ? adjustedGross < 0 ? $"({Math.Abs(adjustedGross).ToString(SD.Two_Decimal_Format)})" : adjustedGross.ToString(SD.Two_Decimal_Format) : null).FontColor(adjustedGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(withHoldingTaxAmount != 0 ? withHoldingTaxAmount < 0 ? $"({Math.Abs(withHoldingTaxAmount).ToString(SD.Four_Decimal_Format)})" : withHoldingTaxAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(withHoldingTaxAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(netOfVatAmount != 0 ? netOfVatAmount < 0 ? $"({Math.Abs(netOfVatAmount).ToString(SD.Four_Decimal_Format)})" : netOfVatAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(netOfVatAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vcfAmount != 0 ? vcfAmount < 0 ? $"({Math.Abs(vcfAmount).ToString(SD.Two_Decimal_Format)})" : vcfAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(vcfAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(retentionAmount != 0 ? retentionAmount < 0 ? $"({Math.Abs(retentionAmount).ToString(SD.Four_Decimal_Format)})" : retentionAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(retentionAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(adjustedNet != 0 ? adjustedNet < 0 ? $"({Math.Abs(adjustedNet).ToString(SD.Two_Decimal_Format)})" : adjustedNet.ToString(SD.Two_Decimal_Format) : null).FontColor(adjustedNet < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(daysDue != 0 ? daysDue < 0 ? $"({Math.Abs(daysDue).ToString(SD.Two_Decimal_Format)})" : daysDue.ToString(SD.Two_Decimal_Format) : null).FontColor(daysDue < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(current != 0 ? current < 0 ? $"({Math.Abs(current).ToString(SD.Two_Decimal_Format)})" : current.ToString(SD.Two_Decimal_Format) : null).FontColor(current < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(oneToThirtyDays != 0 ? oneToThirtyDays < 0 ? $"({Math.Abs(oneToThirtyDays).ToString(SD.Two_Decimal_Format)})" : oneToThirtyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(oneToThirtyDays < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalThirtyOneToSixtyDays != 0 ? totalThirtyOneToSixtyDays < 0 ? $"({Math.Abs(totalThirtyOneToSixtyDays).ToString(SD.Two_Decimal_Format)})" : totalThirtyOneToSixtyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(totalThirtyOneToSixtyDays < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(sixtyOneToNinetyDays != 0 ? sixtyOneToNinetyDays < 0 ? $"({Math.Abs(sixtyOneToNinetyDays).ToString(SD.Two_Decimal_Format)})" : sixtyOneToNinetyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(sixtyOneToNinetyDays < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overNinetyDays != 0 ? overNinetyDays < 0 ? $"({Math.Abs(overNinetyDays).ToString(SD.Two_Decimal_Format)})" : overNinetyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(overNinetyDays < 0 ? Colors.Red.Medium : Colors.Black);

                                    totalGrossAmount += record.Amount;
                                    totalAmountPaid += record.AmountPaid;
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

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalGrossAmount != 0 ? totalGrossAmount < 0 ? $"({Math.Abs(totalGrossAmount).ToString(SD.Two_Decimal_Format)})" : totalGrossAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalGrossAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountPaid != 0 ? totalAmountPaid < 0 ? $"({Math.Abs(totalAmountPaid).ToString(SD.Two_Decimal_Format)})" : totalAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAdjustedGross != 0 ? totalAdjustedGross < 0 ? $"({Math.Abs(totalAdjustedGross).ToString(SD.Two_Decimal_Format)})" : totalAdjustedGross.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAdjustedGross < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalWithHoldingTaxAmount != 0 ? totalWithHoldingTaxAmount < 0 ? $"({Math.Abs(totalWithHoldingTaxAmount).ToString(SD.Four_Decimal_Format)})" : totalWithHoldingTaxAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(totalWithHoldingTaxAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetOfVatAmount != 0 ? totalNetOfVatAmount < 0 ? $"({Math.Abs(totalNetOfVatAmount).ToString(SD.Four_Decimal_Format)})" : totalNetOfVatAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(totalNetOfVatAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVcfAmount != 0 ? totalVcfAmount < 0 ? $"({Math.Abs(totalVcfAmount).ToString(SD.Two_Decimal_Format)})" : totalVcfAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalVcfAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalRetentionAmount != 0 ? totalRetentionAmount < 0 ? $"({Math.Abs(totalRetentionAmount).ToString(SD.Four_Decimal_Format)})" : totalRetentionAmount.ToString(SD.Four_Decimal_Format) : null).FontColor(totalRetentionAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAdjustedNet != 0 ? totalAdjustedNet < 0 ? $"({Math.Abs(totalAdjustedNet).ToString(SD.Two_Decimal_Format)})" : totalAdjustedNet.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAdjustedNet < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCurrent != 0 ? totalCurrent < 0 ? $"({Math.Abs(totalCurrent).ToString(SD.Two_Decimal_Format)})" : totalCurrent.ToString(SD.Two_Decimal_Format) : null).FontColor(totalCurrent < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalOneToThirtyDays != 0 ? totalOneToThirtyDays < 0 ? $"({Math.Abs(totalOneToThirtyDays).ToString(SD.Two_Decimal_Format)})" : totalOneToThirtyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(totalOneToThirtyDays < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalThirtyOneToSixtyDays != 0 ? totalThirtyOneToSixtyDays < 0 ? $"({Math.Abs(totalThirtyOneToSixtyDays).ToString(SD.Two_Decimal_Format)})" : totalThirtyOneToSixtyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(totalThirtyOneToSixtyDays < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSixtyOneToNinetyDays != 0 ? totalSixtyOneToNinetyDays < 0 ? $"({Math.Abs(totalSixtyOneToNinetyDays).ToString(SD.Two_Decimal_Format)})" : totalSixtyOneToNinetyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(totalSixtyOneToNinetyDays < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalOverNinetyDays != 0 ? totalOverNinetyDays < 0 ? $"({Math.Abs(totalOverNinetyDays).ToString(SD.Two_Decimal_Format)})" : totalOverNinetyDays.ToString(SD.Two_Decimal_Format) : null).FontColor(totalOverNinetyDays < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                            #endregion

                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate aging report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(AgingReport));
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

        [HttpGet]
        public IActionResult ArPerCustomer()
        {
            return View();
        }

        #region -- Generated AR Per Customer Report as Quest PDF

        public async Task<IActionResult> GeneratedArPerCustomer(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ArPerCustomer));
            }

            try
            {
                var salesInvoice = await _dbContext.FilprideSalesInvoices
                    .Where(si => si.PostedBy != null && si.Company == companyClaims)
                    .Include(si => si.Product)
                    .Include(si => si.Customer)
                    .Include(si => si.DeliveryReceipt)
                    .Include(si => si.CustomerOrderSlip)
                    .ToListAsync(cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ArPerCustomer));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Legal.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(50).Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item()
                                        .Text("AR PER CUSTOMER REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date From: ").SemiBold();
                                        text.Span(model.DateFrom.ToString(SD.Date_Format));
                                    });

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date To: ").SemiBold();
                                        text.Span(model.DateTo.ToString(SD.Date_Format));
                                    });
                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Acc. Type").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Terms").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Tran. Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Due Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Invoice No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Remarks").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Unit").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Unit Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight/Ltr").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("VAT/Ltr").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("VAT Amt.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total Amt.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amt. Paid").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("SI Balance").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT Amt").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT Paid").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CWT Balance").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

                                decimal totalQuantity = 0m;
                                decimal totalFreight = 0m;
                                decimal totalFreightPerLiter = 0m;
                                decimal totalVatPerLiter = 0m;
                                decimal totalVatAmount = 0m;
                                decimal totalGrossAmount = 0m;
                                decimal totalAmountPaid = 0m;
                                decimal totalBalance = 0m;
                                decimal totalEwtAmount = 0m;
                                decimal totalEwtAmountPaid = 0m;
                                decimal totalewtBalance = 0m;

                                foreach (var record in salesInvoice)
                                {

                                    var freight = record.DeliveryReceipt?.Freight * record.DeliveryReceipt?.Quantity;
                                    var grossAmount = record.Amount;
                                    var vatableAmount = grossAmount / 1.12m;
                                    var vatAmount = vatableAmount * .12m;
                                    var vatPerLiter = vatAmount * record.Quantity;
                                    var ewtAmount = vatableAmount * .01m;
                                    var isEwtAmountPaid = record.IsTaxAndVatPaid ? ewtAmount : 0m;
                                    var ewtBalance = ewtAmount - isEwtAmountPaid;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerType);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerTerms);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.TransactionDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DueDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoiceNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.CustomerPoNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Remarks);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Product?.ProductName);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Product?.ProductUnit);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.UnitPrice != 0 ? record.UnitPrice < 0 ? $"({Math.Abs(record.UnitPrice).ToString(SD.Four_Decimal_Format)})" : record.UnitPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(record.UnitPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freight != 0 ? freight < 0 ? $"({Math.Abs(freight ?? 0).ToString(SD.Two_Decimal_Format)})" : freight?.ToString(SD.Two_Decimal_Format) : null).FontColor(freight < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt?.Freight != 0 ? record.DeliveryReceipt?.Freight < 0 ? $"({Math.Abs(record.DeliveryReceipt?.Freight ?? 0).ToString(SD.Four_Decimal_Format)})" : record.DeliveryReceipt?.Freight.ToString(SD.Four_Decimal_Format) : null).FontColor(record.DeliveryReceipt?.Freight < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vatPerLiter != 0 ? vatPerLiter < 0 ? $"({Math.Abs(vatPerLiter).ToString(SD.Two_Decimal_Format)})" : vatPerLiter.ToString(SD.Two_Decimal_Format) : null).FontColor(vatPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vatAmount != 0 ? vatAmount < 0 ? $"({Math.Abs(vatAmount).ToString(SD.Two_Decimal_Format)})" : vatAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(vatAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(grossAmount != 0 ? grossAmount < 0 ? $"({Math.Abs(grossAmount).ToString(SD.Two_Decimal_Format)})" : grossAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(grossAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.AmountPaid != 0 ? record.AmountPaid < 0 ? $"({Math.Abs(record.AmountPaid).ToString(SD.Two_Decimal_Format)})" : record.AmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(record.AmountPaid < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Balance != 0 ? record.Balance < 0 ? $"({Math.Abs(record.Balance).ToString(SD.Two_Decimal_Format)})" : record.Balance.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Balance < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(ewtAmount != 0 ? ewtAmount < 0 ? $"({Math.Abs(ewtAmount).ToString(SD.Two_Decimal_Format)})" : ewtAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(ewtAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(isEwtAmountPaid != 0 ? isEwtAmountPaid < 0 ? $"({Math.Abs(isEwtAmountPaid).ToString(SD.Two_Decimal_Format)})" : isEwtAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(isEwtAmountPaid < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(ewtBalance != 0 ? ewtBalance < 0 ? $"({Math.Abs(ewtBalance).ToString(SD.Two_Decimal_Format)})" : ewtBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(ewtBalance < 0 ? Colors.Red.Medium : Colors.Black);

                                    totalQuantity += record.Quantity;
                                    totalFreight += freight ?? 0m;
                                    totalFreightPerLiter += record.DeliveryReceipt?.Freight ?? 0m;
                                    totalVatPerLiter += vatPerLiter;
                                    totalVatAmount += vatAmount;
                                    totalGrossAmount += grossAmount;
                                    totalAmountPaid += record.AmountPaid;
                                    totalBalance += record.Balance;
                                    totalEwtAmount += ewtAmount;
                                    totalEwtAmountPaid += isEwtAmountPaid;
                                    totalewtBalance += ewtBalance;
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                var unitPrice = totalGrossAmount / totalQuantity;

                                table.Cell().ColumnSpan(12).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantity != 0 ? totalQuantity < 0 ? $"({Math.Abs(totalQuantity).ToString(SD.Two_Decimal_Format)})" : totalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(unitPrice != 0 ? unitPrice < 0 ? $"({Math.Abs(unitPrice).ToString(SD.Four_Decimal_Format)})" : unitPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(unitPrice < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreight != 0 ? totalFreight < 0 ? $"({Math.Abs(totalFreight).ToString(SD.Two_Decimal_Format)})" : totalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreight < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightPerLiter != 0 ? totalFreightPerLiter < 0 ? $"({Math.Abs(totalFreightPerLiter).ToString(SD.Four_Decimal_Format)})" : totalFreightPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(totalFreightPerLiter < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVatPerLiter != 0 ? totalVatPerLiter < 0 ? $"({Math.Abs(totalVatPerLiter).ToString(SD.Two_Decimal_Format)})" : totalVatPerLiter.ToString(SD.Two_Decimal_Format) : null).FontColor(totalVatPerLiter < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVatAmount != 0 ? totalVatAmount < 0 ? $"({Math.Abs(totalVatAmount).ToString(SD.Two_Decimal_Format)})" : totalVatAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalVatAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalGrossAmount != 0 ? totalGrossAmount < 0 ? $"({Math.Abs(totalGrossAmount).ToString(SD.Two_Decimal_Format)})" : totalGrossAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalGrossAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountPaid != 0 ? totalAmountPaid < 0 ? $"({Math.Abs(totalAmountPaid).ToString(SD.Two_Decimal_Format)})" : totalAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalBalance != 0 ? totalBalance < 0 ? $"({Math.Abs(totalBalance).ToString(SD.Two_Decimal_Format)})" : totalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(totalBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalEwtAmount != 0 ? totalEwtAmount < 0 ? $"({Math.Abs(totalEwtAmount).ToString(SD.Two_Decimal_Format)})" : totalEwtAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalEwtAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalEwtAmountPaid != 0 ? totalEwtAmountPaid < 0 ? $"({Math.Abs(totalEwtAmountPaid).ToString(SD.Two_Decimal_Format)})" : totalEwtAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(totalEwtAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalewtBalance != 0 ? totalewtBalance < 0 ? $"({Math.Abs(totalewtBalance).ToString(SD.Two_Decimal_Format)})" : totalewtBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(totalewtBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                            #endregion

                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate AR per customer report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ArPerCustomer));
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

        [HttpGet]
        public IActionResult ServiceInvoiceReport()
        {
            return View();
        }

        #region -- Generated Service Invoice Report as Quest PDF

        public async Task<IActionResult> GeneratedServiceInvoiceReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ServiceInvoiceReport));
            }

            try
            {
                var serviceInvoice = await _unitOfWork.FilprideReport.GetServiceInvoiceReport(model.DateFrom, model.DateTo, companyClaims!);

                if (!serviceInvoice.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ServiceInvoiceReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Legal.Landscape());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(50).Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item()
                                        .Text("SERVICE REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date From: ").SemiBold();
                                        text.Span(model.DateFrom.ToString(SD.Date_Format));
                                    });

                                    column.Item().Text(text =>
                                    {
                                        text.Span("Date To: ").SemiBold();
                                        text.Span(model.DateTo.ToString(SD.Date_Format));
                                    });
                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Transaction Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Address").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer TIN").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Service Invoice#").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Service").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Period").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Due Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("G. Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amount Paid").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Payment Status").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Instructions").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Type").SemiBold();
                                });

                            #endregion

                             #region -- Loop to Show Records

                                var totalAmount = 0m;
                                var totalAmountPaid = 0m;

                                foreach (var record in serviceInvoice)
                                {
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CreatedDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerAddress);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerTin);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoiceNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Service?.Name);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Period.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DueDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Amount != 0 ? record.Amount < 0 ? $"({Math.Abs(record.Amount).ToString(SD.Two_Decimal_Format)})" : record.Amount.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Amount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.AmountPaid != 0 ? record.AmountPaid < 0 ? $"({Math.Abs(record.AmountPaid).ToString(SD.Two_Decimal_Format)})" : record.AmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(record.AmountPaid < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PaymentStatus);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Instructions);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Type);

                                    totalAmount += record.Amount;
                                    totalAmountPaid += record.AmountPaid;
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(8).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmount != 0 ? totalAmount < 0 ? $"({Math.Abs(totalAmount).ToString(SD.Two_Decimal_Format)})" : totalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountPaid != 0 ? totalAmountPaid < 0 ? $"({Math.Abs(totalAmountPaid).ToString(SD.Two_Decimal_Format)})" : totalAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(totalAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f);

                            #endregion

                        });

                        #endregion

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion
                    });
                });

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate service invoice report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ServiceInvoiceReport));
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
                    worksheet.Cells[row, 9].Value = sv.Amount;
                    worksheet.Cells[row, 10].Value = sv.AmountPaid;
                    worksheet.Cells[row, 11].Value = sv.PaymentStatus;
                    worksheet.Cells[row, 12].Value = sv.Instructions;
                    worksheet.Cells[row, 13].Value = sv.Type;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "MMM yyyy";
                    worksheet.Cells[row, 8].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;


                    totalAmount += sv.Amount;
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
