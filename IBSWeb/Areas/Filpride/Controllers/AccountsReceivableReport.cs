using System.Globalization;
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
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.DeliveredPrice.ToString(SD.Four_Decimal_Format));
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.Quantity.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.TotalAmount.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text("APPROVED");
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ExpirationDate.ToString());
                                }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(7).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(cosSummary.Sum(cos => (cos.Quantity - cos.DeliveredQuantity)).ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(cosSummary.Sum(cos => cos.TotalAmount).ToString(SD.Two_Decimal_Format)).SemiBold();
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

                var firstDayOfMonth = new DateOnly(viewModel.AsOf.Year, viewModel.AsOf.Month, 1);
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
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.PickUpPoint?.Depot);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.AuthorityToLoadNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerOrderSlipNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Hauler?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.Supplier?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightCharge.ToString(SD.Four_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(ecc.ToString(SD.Four_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalFreight.ToString(SD.Two_Decimal_Format));
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
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantity.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten1).Border(0.5f);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightCharge.ToString(SD.Four_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalECC.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightAmount.ToString(SD.Four_Decimal_Format)).SemiBold();
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

                    //TODO Remove this in the future
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
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 102, 204));
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
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freight.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.TotalAmount.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vat.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(salesNetOfVat.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightNetOfVat.ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate.ToString(SD.Four_Decimal_Format));
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.CustomerOrderSlip?.Commissionee?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.Remarks);

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
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreight.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalAmount.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVat.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightNetOfVat.ToString(SD.Two_Decimal_Format)).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCommissionRate.ToString(SD.Four_Decimal_Format)).SemiBold();
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
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overAllQuantitySum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfAmountSum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallAverageSellingPrice.ToString(SD.Four_Decimal_Format));
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselQuantitySum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetOfAmountSum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselAverageSellingPrice.ToString(SD.Four_Decimal_Format));
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasQuantitySum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetOfAmountSum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasAverageSellingPrice.ToString(SD.Four_Decimal_Format));
                                                table.Cell();
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasQuantitySum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetOfAmountSum.ToString(SD.Two_Decimal_Format));
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasAverageSellingPrice.ToString(SD.Four_Decimal_Format));

                                                totalQuantityForBiodiesel += biodieselQuantitySum;
                                                totalAmountForBiodiesel += biodieselNetOfAmountSum;

                                                totalQuantityForEconogas += econogasQuantitySum;
                                                totalAmountForEconogas += econogasNetOfAmountSum;

                                                totalQuantityForEnvirogas += envirogasQuantitySum;
                                                totalAmountForEnvirogas += envirogasNetOfAmountSum;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat != 0 && overallTotalQuantity != 0 ? (totalSalesNetOfVat / overallTotalQuantity).ToString(SD.Four_Decimal_Format) : 0m.ToString(SD.Four_Decimal_Format));
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForBiodiesel.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForBiodiesel.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForBiodiesel != 0 && totalQuantityForBiodiesel != 0 ? (totalAmountForBiodiesel / totalQuantityForBiodiesel).ToString(SD.Four_Decimal_Format) : 0m.ToString(SD.Four_Decimal_Format));
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEconogas.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEconogas.ToString(SD.Four_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEconogas != 0 && totalQuantityForEconogas != 0 ? (totalAmountForEconogas / totalQuantityForEconogas).ToString(SD.Four_Decimal_Format) : 0m.ToString(SD.Four_Decimal_Format));
                                            table.Cell();
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEnvirogas.ToString(SD.Two_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEnvirogas.ToString(SD.Four_Decimal_Format));
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmountForEnvirogas != 0 && totalQuantityForEnvirogas != 0 ? (totalAmountForEnvirogas / totalQuantityForEnvirogas).ToString(SD.Four_Decimal_Format) : 0m.ToString(SD.Four_Decimal_Format));

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
    }
}
