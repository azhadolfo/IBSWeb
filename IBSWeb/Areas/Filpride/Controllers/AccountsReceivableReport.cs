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
using OfficeOpenXml;
using OfficeOpenXml.Style;

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
    }
}
