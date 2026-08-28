using System.Drawing;
using System.Linq.Expressions;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Color = System.Drawing.Color;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class AccountsReceivableReport : Controller
    {
        private sealed class SummaryMetric
        {
            public decimal Quantity { get; set; }
            public decimal NetOfSales { get; set; }
        }

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public AccountsReceivableReport(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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

        private static string NormalizeStatusFilter(string? statusFilter) => statusFilter switch
        {
            "All" => "All",
            "InvalidOnly" => "InvalidOnly",
            _ => "ValidOnly"
        };

        private static string GetStatusFilterLabel(string statusFilter) => statusFilter switch
        {
            "All" => "All (Include Voided)",
            "InvalidOnly" => "Voided Only",
            _ => "Valid Only (Exclude Voided)"
        };

        private static decimal RoundToFour(decimal value) => DecimalRoundingHelper.RoundToFour(value);

        private static decimal DivideOrZero(decimal dividend, decimal divisor) => DecimalRoundingHelper.DivideOrZero(dividend, divisor);

        private static decimal NetOfVatOrZero(decimal grossAmount) => DecimalRoundingHelper.ComputeNetOfVat(grossAmount);

        private static decimal VatAmountOrZero(decimal netOfVatAmount) => DecimalRoundingHelper.ComputeVatAmount(netOfVatAmount);

        private static decimal EwtAmountOrZero(decimal netOfVatAmount, decimal percent) => DecimalRoundingHelper.ComputeEwtAmount(netOfVatAmount, percent);

        private static decimal ComputeAverageSellingPrice(decimal netOfSales, decimal quantity)
        {
            return netOfSales != 0m || quantity != 0m
                ? DivideOrZero(netOfSales, quantity)
                : 0m;
        }

        private static List<string> GetOrderedProductNames<T>(IEnumerable<T> records, Func<T, string?> selector)
        {
            return records
                .Select(selector)
                .Where(productName => !string.IsNullOrWhiteSpace(productName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(productName => productName, StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
        }

        private static decimal SumQuantityByProduct<T>(IEnumerable<T> records, string productName, Func<T, string?> selector, Func<T, decimal> quantitySelector)
        {
            return records
                .Where(record => string.Equals(selector(record), productName, StringComparison.OrdinalIgnoreCase))
                .Sum(quantitySelector);
        }

        private static decimal SumAmountByProduct<T>(IEnumerable<T> records, string productName, Func<T, string?> selector, Func<T, decimal> amountSelector)
        {
            return records
                .Where(record => string.Equals(selector(record), productName, StringComparison.OrdinalIgnoreCase))
                .Sum(amountSelector);
        }

        private static Dictionary<string, SummaryMetric> CreateSummaryMetricMap(IEnumerable<string> keys)
        {
            return keys.ToDictionary(
                key => key,
                _ => new SummaryMetric(),
                StringComparer.OrdinalIgnoreCase);
        }

        [HttpGet]
        public IActionResult COSUnservedVolume()
        {
            return View();
        }

        #region -- Generated COS Unserved Volume Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GenerateCOSUnservedVolume(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(COSUnservedVolume));
            }

            try
            {
                var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo);

                if (cosSummary.Count == 0)
                {
                    TempData["info"] = "No records found!";
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
                                        columns.RelativeColumn();
                                    });

                                #endregion

                                #region -- Table Header

                                    table.Header(header =>
                                    {
                                        header.Cell().ColumnSpan(12).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("SUMMARY OF BOOKED SALES").SemiBold();

                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date of Del").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO No.").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS No.").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight").SemiBold();
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
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ProductName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerPoNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlipNo);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.DeliveredPrice != 0 ? record.DeliveredPrice < 0 ? $"({Math.Abs(record.DeliveredPrice).ToString(SD.Four_Decimal_Format)})" : record.DeliveredPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(record.DeliveredPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.Freight != 0 ? record.Freight < 0 ? $"({Math.Abs((decimal)record.Freight).ToString(SD.Four_Decimal_Format)})" : record.Freight?.ToString(SD.Four_Decimal_Format) : null).FontColor(record.Freight < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).AlignRight().Padding(3).Text(record.TotalAmount != 0 ? record.TotalAmount < 0 ? $"({Math.Abs(record.TotalAmount).ToString(SD.Two_Decimal_Format)})" : record.TotalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(record.TotalAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Status.ToUpper());
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ExpirationDate.ToString());
                                }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(8).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate cos unserved volume report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cos unserved volume report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(COSUnservedVolume));
            }
        }

        #endregion

        #region -- Generate COS Unserved Volume as Excel File --

        public async Task<IActionResult> GenerateCOSUnservedVolumeToExcel(ViewModelBook model, CancellationToken cancellationToken)
        {
            ViewBag.DateFrom = model.DateFrom.ToString("MMMM dd, yyyy");
            ViewBag.DateTo = model.DateTo.ToString("MMMM dd, yyyy");
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date from";
                return RedirectToAction(nameof(COSUnservedVolume));
            }

            try
            {
                var cosSummary = await _unitOfWork.FilprideReport.GetCosUnservedVolume(model.DateFrom, model.DateTo);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("COS Unserved Volume");

                // Setting header
                worksheet.Cells["A1"].Value = "SUMMARY OF BOOKED SALES";
                worksheet.Cells["A2:B2"].Value = $"{ViewBag.DateFrom} - {ViewBag.DateTo}";
                worksheet.Cells["A2:B2"].Merge = true;
                worksheet.Cells["A1:N1"].Merge = true;
                worksheet.Cells["A1:N1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1:N1"].Style.Font.Bold = true;
                worksheet.Cells["A3:B3"].Value = $"Date and Time Generated: {DateTimeHelper.GetCurrentPhilippineTime()}";
                worksheet.Cells["A3:B3"].Merge = true;

                // Define table headers
                var headers = new[]
                {
                    "COS Date", "Customer", "Branch", "Product", "P.O. No.",
                    "COS No.", "Price", "Freight", "Unserved Volume", "Amount", "COS Status",
                    "Exp of COS", "Commissionee", "Commission Rate"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[4, i + 1].Value = headers[i];
                    worksheet.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#9966ff"));
                    worksheet.Cells[4, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[4, i + 1].Style.Font.Bold = true;
                }

                // Populate data rows
                int row = 5;
                string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";

                var totalUnservedVolume = 0m;
                var totalAmount = 0m;

                foreach (var item in cosSummary)
                {
                    var unservedVolume = item.Quantity - item.DeliveredQuantity;

                    worksheet.Cells[row, 1].Value = item.Date;
                    worksheet.Cells[row, 2].Value = item.CustomerName;
                    worksheet.Cells[row, 3].Value = item.Branch;
                    worksheet.Cells[row, 4].Value = item.ProductName;
                    worksheet.Cells[row, 5].Value = item.CustomerPoNo;
                    worksheet.Cells[row, 6].Value = item.CustomerOrderSlipNo;
                    worksheet.Cells[row, 7].Value = item.DeliveredPrice;
                    worksheet.Cells[row, 8].Value = item.Freight;
                    worksheet.Cells[row, 9].Value = unservedVolume;
                    worksheet.Cells[row, 10].Value = item.TotalAmount;
                    worksheet.Cells[row, 11].Value = item.Status.ToUpper();
                    worksheet.Cells[row, 12].Value = item.ExpirationDate?.ToString("dd-MMM-yyyy");
                    worksheet.Cells[row, 13].Value = item.CommissioneeName;
                    worksheet.Cells[row, 14].Value = item.CommissionRate;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    row++;

                    totalUnservedVolume += unservedVolume;
                    totalAmount += item.TotalAmount;
                }

                // Add total row
                worksheet.Cells[row, 8].Value = "TOTAL";
                worksheet.Cells[row, 9].Value = totalUnservedVolume;
                worksheet.Cells[row, 10].Value = totalAmount;
                worksheet.Cells[row, 8, row, 10].Style.Font.Bold = true;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;

                // Auto-fit columns for readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate cos unserved volume report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                // Return as Excel file
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                var fileName = $"COS_Unserved_Volume_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cos unserved volume report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(COSUnservedVolume));
            }
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
            var isDeliveredDateRange = viewModel.ReportType == "Delivered" && viewModel.ReportMode == "DateRange";

            if (viewModel.DateFrom == default && viewModel.ReportType != "InTransit")
            {
                TempData["warning"] = "Please enter a valid Date From";
                return RedirectToAction(nameof(DispatchReport));
            }

            if (viewModel.ReportType == "InTransit" && (viewModel.DateFrom == default || viewModel.DateTo == default))
            {
                TempData["warning"] = "Please enter a valid Date From and Date To";
                return RedirectToAction(nameof(DispatchReport));
            }

            if (isDeliveredDateRange && viewModel.DateTo == default)
            {
                TempData["warning"] = "Please enter a valid Date To";
                return RedirectToAction(nameof(DispatchReport));
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                if (string.IsNullOrEmpty(viewModel.ReportType))
                {
                    return BadRequest();
                }

                var currentUser = GetUserFullName();
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                Expression<Func<FilprideDeliveryReceipt, bool>>? filter;
                var dateRangeType = isDeliveredDateRange ? "ByRange" : "AsOf";
                //var currencyFormatTwoDecimal = "#,##0.00";

                if(viewModel.ReportType == "Delivered")
                {
                    if (dateRangeType == "AsOf")
                    {
                        filter = i => true
                                      && i.DeliveredDate <= viewModel.DateFrom
                                      && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                    }
                    else
                    {
                        filter = i => true
                                      && i.DeliveredDate >= viewModel.DateFrom
                                      && i.DeliveredDate <= viewModel.DateTo
                                      && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                    }
                }
                else
                {
                    filter = i => true
                                  && i.Date >= viewModel.DateFrom
                                  && i.Date <= viewModel.DateTo
                                  && (i.DeliveredDate == null || i.DeliveredDate > viewModel.DateTo)
                                  && i.CanceledBy == null
                                  && i.VoidedBy == null;
                }

                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(filter, cancellationToken);

                if (!deliveryReceipts.Any())
                {
                    TempData["info"] = "No records found";
                    return RedirectToAction(nameof(DispatchReport));
                }

                deliveryReceipts = deliveryReceipts.OrderBy(dr => dr.Date);
                var dispatchDrIds = deliveryReceipts.Select(dr => dr.DeliveryReceiptId).ToList();
                var receivingReportAggregates = await _dbContext.FilprideReceivingReports
                    .Where(rr => rr.DeliveryReceiptId.HasValue
                                && dispatchDrIds.Contains(rr.DeliveryReceiptId.Value)
                                && rr.Status == nameof(Status.Posted))
                    .GroupBy(rr => rr.DeliveryReceiptId!.Value)
                    .Select(group => new
                    {
                        DeliveryReceiptId = group.Key,
                        LiftingDate = group.Max(rr => rr.Date),
                        QuantityReceived = group.Sum(rr => rr.QuantityReceived)
                    })
                    .ToDictionaryAsync(x => x.DeliveryReceiptId, cancellationToken);

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

                        page.Header().Height(60).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("OPERATION - LOGISTICS").FontSize(15).SemiBold();
                                if (dateRangeType == "AsOf")
                                {
                                    column.Item().Text($"DISPATCH REPORT AS OF {viewModel.DateFrom:dd MMM, yyyy}").FontSize(15).SemiBold();
                                }
                                else
                                {
                                    column.Item().Text($"DISPATCH REPORT from {viewModel.DateFrom:dd MMM, yyyy} to {viewModel.DateTo:dd MMM, yyyy}").FontSize(15).SemiBold();
                                }

                                column.Item().Text(text =>
                                {
                                    text.Span(viewModel.ReportType == "Delivered" ? "DELIVERED" : "IN TRANSIT").SemiBold();
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
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Delivery Option").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight Charge").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ECC").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total Freight").SemiBold();

                                        //TODO Remove this in the future and remove a cell or update the value of colspan affected, generate report for re-checking
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("OTC COS No").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("OTC DR No").SemiBold();

                                        if (viewModel.ReportType == "Delivered")
                                        {
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Delivery Date").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Status").SemiBold();
                                        }
                                        else
                                        {
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Lifting Date").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Lifting Quantity").SemiBold();
                                        }
                                    });

                                #endregion

                                #region -- Initialize Variable for Computation

                                    decimal totalQuantity = 0m;
                                    decimal totalFreightAmount = 0m;
                                    decimal totalLiftedQuantity = 0m;

                                #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in deliveryReceipts)
                                    {
                                        var quantity = record.Quantity;
                                        var freightCharge = record.Freight;
                                        var ecc = record.ECC;
                                        var totalFreight = record.FreightAmount;
                                        var liftedQuantity = 0m;
                                        receivingReportAggregates.TryGetValue(record.DeliveryReceiptId, out var rrAggregate);

                                        if (viewModel.ReportType == "Delivered" && dateRangeType == "AsOf" &&
                                            record.DeliveredDate != viewModel.DateFrom)
                                        {
                                            // Don't show record of other dates if entry is "as of" and "delivered"
                                        }
                                        else
                                        {
                                            table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerName);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerType);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceiptNo);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.ProductName);
                                            table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity != 0 ? quantity < 0 ? $"({Math.Abs(quantity).ToString(SD.Two_Decimal_Format)})" : quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.Depot);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.AuthorityToLoadNo);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerOrderSlipNo);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.Hauler?.SupplierName);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.SupplierName);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.DeliveryOption);
                                            table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightCharge != 0 ? freightCharge < 0 ? $"({Math.Abs(freightCharge).ToString(SD.Four_Decimal_Format)})" : freightCharge.ToString(SD.Four_Decimal_Format) : null).FontColor(freightCharge < 0 ? Colors.Red.Medium : Colors.Black);
                                            table.Cell().Border(0.5f).Padding(3).AlignRight().Text(ecc != 0 ? ecc < 0 ? $"({Math.Abs(ecc).ToString(SD.Four_Decimal_Format)})" : ecc.ToString(SD.Four_Decimal_Format) : null).FontColor(ecc < 0 ? Colors.Red.Medium : Colors.Black);
                                            table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalFreight != 0 ? totalFreight < 0 ? $"({Math.Abs(totalFreight).ToString(SD.Two_Decimal_Format)})" : totalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.OldCosNo);
                                            table.Cell().Border(0.5f).Padding(3).Text(record.ManualDrNo);

                                            if (viewModel.ReportType == "Delivered")
                                            {
                                                table.Cell().Border(0.5f).Padding(3).Text(record.DeliveredDate?.ToString(SD.Date_Format));
                                                table.Cell().Border(0.5f).Padding(3).Text(record.Status == nameof(DRStatus.PendingDelivery) ? "IN TRANSIT" : record.Status.ToUpper());
                                            }
                                            else
                                            {
                                                if (record.HasReceivingReport && rrAggregate != null)
                                                {
                                                    liftedQuantity = rrAggregate.QuantityReceived;
                                                    table.Cell().Border(0.5f).Padding(3).Text(rrAggregate.LiftingDate.ToString(SD.Date_Format));
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(liftedQuantity != 0 ? liftedQuantity < 0 ? $"({Math.Abs(liftedQuantity).ToString(SD.Two_Decimal_Format)})" : liftedQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(liftedQuantity < 0 ? Colors.Red.Medium : Colors.Black);
                                                }
                                                else
                                                {
                                                    table.Cell().Border(0.5f);
                                                    table.Cell().Border(0.5f);
                                                }
                                            }
                                        }
                                        totalQuantity += quantity;
                                        totalFreightAmount += totalFreight;
                                        totalLiftedQuantity += liftedQuantity;
                                    }

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(5).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("GRAND TOTAL:").SemiBold();
                                    if (viewModel.ReportType == "Delivered" && dateRangeType == "AsOf")
                                    {
                                        // Don't add record of other dates if entry is "as of" and "delivered"
                                        var entriesToday = deliveryReceipts.Where(t => t.DeliveredDate == viewModel.DateFrom).ToList();
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(entriesToday.Sum(dr => dr.Quantity) != 0 ? entriesToday.Sum(dr => dr.Quantity) < 0 ? $"({Math.Abs(entriesToday.Sum(dr => dr.Quantity)).ToString(SD.Two_Decimal_Format)})" : entriesToday.Sum(dr => dr.Quantity).ToString(SD.Two_Decimal_Format) : null).FontColor(entriesToday.Sum(dr => dr.Quantity) < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f);
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(entriesToday.Sum(dr => dr.FreightAmount) != 0 ? entriesToday.Sum(dr => dr.FreightAmount) < 0 ? $"({Math.Abs(entriesToday.Sum(dr => dr.FreightAmount)).ToString(SD.Two_Decimal_Format)})" : entriesToday.Sum(dr => dr.Quantity * dr.Freight).ToString(SD.Two_Decimal_Format) : null).FontColor(entriesToday.Sum(dr => dr.Quantity * dr.Freight) < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f);
                                    }
                                    else
                                    {
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantity != 0 ? totalQuantity < 0 ? $"({Math.Abs(totalQuantity).ToString(SD.Two_Decimal_Format)})" : totalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f);
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightAmount != 0 ? totalFreightAmount < 0 ? $"({Math.Abs(totalFreightAmount).ToString(SD.Two_Decimal_Format)})" : totalFreightAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalFreightAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f);
                                        if (totalLiftedQuantity != 0)
                                        {
                                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalLiftedQuantity != 0 ? totalLiftedQuantity < 0 ? $"({Math.Abs(totalLiftedQuantity).ToString(SD.Two_Decimal_Format)})" : totalLiftedQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalLiftedQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        }
                                        else
                                        {
                                            table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f);
                                        }
                                    }

                                #endregion

                                // Generated by, checked by, received by footer
                                col.Item().PaddingTop(10).Table(content =>
                                {
                                    #region -- Columns Definition

                                    content.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    #endregion

                                    #region -- Loop to Show Records

                                        content.Cell().Padding(3).Text("Generated by:");
                                        content.Cell().Padding(3).Text("Noted & Checked by:");
                                        content.Cell().Padding(3).Text("Received by:");

                                        content.Cell().Padding(3).Text(currentUser.ToUpper());
                                        content.Cell().Height(10).Text(" ");
                                        content.Cell().Height(10).Text(" ");

                                        content.Cell().Padding(3).Text($"Date & Time: {today:MM/dd/yyyy - hh:mm tt}").SemiBold();
                                        content.Cell().Padding(3).Text("LOGISTICS SUPERVISOR").SemiBold();
                                        content.Cell().Padding(3).Text("CNC SUPERVISOR").SemiBold();

                                    #endregion
                                });

                                //Summary Table
                                if (dateRangeType == "AsOf" && viewModel.ReportType == "Delivered")
                                {
                                    var productList = GetOrderedProductNames(
                                        deliveryReceipts,
                                        dr => dr.CustomerOrderSlip?.ProductName);

                                    col.Item().PaddingTop(50).Text("SUMMARY").Bold().FontSize(14);

                                    #region -- Overall Summary

                                    col.Item().PaddingTop(10).Table(content =>
                                    {
                                        #region -- Columns Definition

                                        content.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();

                                            foreach (var _ in productList)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        #endregion

                                        #region -- Loop to Show Records

                                        foreach (var customerType in deliveryReceipts.GroupBy(dr =>
                                                     dr.Customer!.CustomerType))
                                        {

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                                .AlignCenter().Text(customerType.Key).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                                .AlignCenter().Text("TOTAL(VOLUME)").SemiBold();

                                            foreach (var productName in productList)
                                            {
                                                content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                                    .AlignCenter().Text(productName).SemiBold();
                                            }

                                            #region -- Total Today --

                                            content.Cell().Border(0.5f).Padding(3).Text("TOTAL TODAY").SemiBold();

                                            var totalToday = customerType.Where(t => t.Date == viewModel.DateFrom)
                                                .Sum(dr => dr.Quantity);

                                            content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                .Text(totalToday != 0
                                                    ? totalToday < 0
                                                        ? $"({Math.Abs(totalToday).ToString(SD.Two_Decimal_Format)})"
                                                        : totalToday.ToString(SD.Two_Decimal_Format)
                                                    : null)
                                                .FontColor(totalToday < 0 ? Colors.Red.Medium : Colors.Black);

                                            foreach (var productName in productList)
                                            {
                                                var totalProductToday = customerType.Where(x =>
                                                        x.Date == viewModel.DateFrom &&
                                                        x.CustomerOrderSlip?.ProductName == productName)
                                                    .Sum(dr => dr.Quantity);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                    .Text(totalProductToday != 0
                                                        ? totalProductToday < 0
                                                            ? $"({Math.Abs(totalProductToday).ToString(SD.Two_Decimal_Format)})"
                                                            : totalProductToday.ToString(SD.Two_Decimal_Format)
                                                        : null).FontColor(totalProductToday < 0
                                                        ? Colors.Red.Medium
                                                        : Colors.Black);
                                            }

                                            #endregion

                                            #region -- Total Yesterday --

                                            content.Cell().Border(0.5f).Padding(3).Text("CUM. AS OF YESTERDAY")
                                                .SemiBold();

                                            var totalYesterday = customerType.Where(t => t.Date < viewModel.DateFrom)
                                                .Sum(dr => dr.Quantity);

                                            content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                .Text(totalYesterday != 0
                                                    ? totalYesterday < 0
                                                        ? $"({Math.Abs(totalYesterday).ToString(SD.Two_Decimal_Format)})"
                                                        : totalYesterday.ToString(SD.Two_Decimal_Format)
                                                    : null).FontColor(totalYesterday < 0
                                                    ? Colors.Red.Medium
                                                    : Colors.Black);

                                            foreach (var productName in productList)
                                            {
                                                var totalProductYesterday = customerType.Where(x =>
                                                        x.Date < viewModel.DateFrom &&
                                                        x.CustomerOrderSlip?.ProductName == productName)
                                                    .Sum(dr => dr.Quantity);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                    .Text(totalProductYesterday != 0
                                                        ? totalProductYesterday < 0
                                                            ? $"({Math.Abs(totalProductYesterday).ToString(SD.Two_Decimal_Format)})"
                                                            : totalProductYesterday.ToString(SD.Two_Decimal_Format)
                                                        : null).FontColor(totalProductYesterday < 0
                                                        ? Colors.Red.Medium
                                                        : Colors.Black);
                                            }

                                            #endregion

                                            #region -- Total Month ToDate --

                                            content.Cell().Border(0.5f).Padding(3).Text("MONTH TO DATE").SemiBold();

                                            var totalMonthToDate = customerType.Sum(dr => dr.Quantity);

                                            content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                .Text(totalMonthToDate != 0
                                                    ? totalMonthToDate < 0
                                                        ? $"({Math.Abs(totalMonthToDate).ToString(SD.Two_Decimal_Format)})"
                                                        : totalMonthToDate.ToString(SD.Two_Decimal_Format)
                                                    : null).FontColor(totalMonthToDate < 0
                                                    ? Colors.Red.Medium
                                                    : Colors.Black);

                                            foreach (var productName in productList)
                                            {
                                                var totalProductMonthToDate = customerType
                                                    .Where(x => x.CustomerOrderSlip?.ProductName == productName)
                                                    .Sum(dr => dr.Quantity);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                    .Text(totalProductMonthToDate != 0
                                                        ? totalProductMonthToDate < 0
                                                            ? $"({Math.Abs(totalProductMonthToDate).ToString(SD.Two_Decimal_Format)})"
                                                            : totalProductMonthToDate.ToString(SD.Two_Decimal_Format)
                                                        : null).FontColor(totalProductMonthToDate < 0
                                                        ? Colors.Red.Medium
                                                        : Colors.Black);
                                            }

                                            #endregion

                                            for (var spacerIndex = 0; spacerIndex < productList.Count + 2; spacerIndex++)
                                            {
                                                content.Cell().Height(10).Text(" ");
                                            }
                                        }

                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                            .AlignCenter().Text("ALL").SemiBold();
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                            .AlignCenter().Text("TOTAL(VOLUME)").SemiBold();

                                        foreach (var productName in productList)
                                        {
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3)
                                                .AlignCenter().Text(productName).SemiBold();
                                        }

                                        #region -- Total Today --

                                        content.Cell().Border(0.5f).Padding(3).Text("TOTAL TODAY").SemiBold();

                                        var totalTodayOverAll = deliveryReceipts
                                            .Where(t => t.Date == viewModel.DateFrom).Sum(dr => dr.Quantity);

                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalTodayOverAll != 0
                                            ? totalTodayOverAll < 0
                                                ? $"({Math.Abs(totalTodayOverAll).ToString(SD.Two_Decimal_Format)})"
                                                : totalTodayOverAll.ToString(SD.Two_Decimal_Format)
                                            : null).FontColor(totalTodayOverAll < 0 ? Colors.Red.Medium : Colors.Black);

                                        foreach (var productName in productList)
                                        {
                                            var totalProductTodayOverAll = deliveryReceipts.Where(x =>
                                                    x.Date == viewModel.DateFrom &&
                                                    x.CustomerOrderSlip?.ProductName == productName)
                                                .Sum(dr => dr.Quantity);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(
                                                totalProductTodayOverAll != 0
                                                    ? totalProductTodayOverAll < 0
                                                        ? $"({Math.Abs(totalProductTodayOverAll).ToString(SD.Two_Decimal_Format)})"
                                                        : totalProductTodayOverAll.ToString(SD.Two_Decimal_Format)
                                                    : null).FontColor(totalProductTodayOverAll < 0
                                                ? Colors.Red.Medium
                                                : Colors.Black);
                                        }

                                        #endregion

                                        #region -- Total Yesterday --

                                        content.Cell().Border(0.5f).Padding(3).Text("CUM. AS OF YESTERDAY").SemiBold();

                                        var totalYesterdayOverAll = deliveryReceipts
                                            .Where(t => t.Date < viewModel.DateFrom).Sum(dr => dr.Quantity);

                                        content.Cell().Border(0.5f).Padding(3).AlignRight()
                                            .Text(totalYesterdayOverAll != 0
                                                ? totalYesterdayOverAll < 0
                                                    ? $"({Math.Abs(totalYesterdayOverAll).ToString(SD.Two_Decimal_Format)})"
                                                    : totalYesterdayOverAll.ToString(SD.Two_Decimal_Format)
                                                : null).FontColor(totalYesterdayOverAll < 0
                                                ? Colors.Red.Medium
                                                : Colors.Black);

                                        foreach (var productName in productList)
                                        {
                                            var totalProductYesterdayOverAll = deliveryReceipts.Where(x =>
                                                    x.Date < viewModel.DateFrom &&
                                                    x.CustomerOrderSlip?.ProductName == productName)
                                                .Sum(dr => dr.Quantity);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                .Text(totalProductYesterdayOverAll != 0
                                                    ? totalProductYesterdayOverAll < 0
                                                        ? $"({Math.Abs(totalProductYesterdayOverAll).ToString(SD.Two_Decimal_Format)})"
                                                        : totalProductYesterdayOverAll.ToString(SD.Two_Decimal_Format)
                                                    : null).FontColor(totalProductYesterdayOverAll < 0
                                                    ? Colors.Red.Medium
                                                    : Colors.Black);
                                        }

                                        #endregion

                                        #region -- Total Month ToDate --

                                        content.Cell().Border(0.5f).Padding(3).Text("MONTH TO DATE").SemiBold();

                                        var totalMonthToDateOverAll = deliveryReceipts.Sum(dr => dr.Quantity);

                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(
                                            totalMonthToDateOverAll != 0
                                                ? totalMonthToDateOverAll < 0
                                                    ? $"({Math.Abs(totalMonthToDateOverAll).ToString(SD.Two_Decimal_Format)})"
                                                    : totalMonthToDateOverAll.ToString(SD.Two_Decimal_Format)
                                                : null).FontColor(totalMonthToDateOverAll < 0
                                            ? Colors.Red.Medium
                                            : Colors.Black);

                                        foreach (var productName in productList)
                                        {
                                            var totalProductMonthToDateOverAll = deliveryReceipts
                                                .Where(x => x.CustomerOrderSlip?.ProductName == productName)
                                                .Sum(dr => dr.Quantity);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight()
                                                .Text(totalProductMonthToDateOverAll != 0
                                                    ? totalProductMonthToDateOverAll < 0
                                                        ? $"({Math.Abs(totalProductMonthToDateOverAll).ToString(SD.Two_Decimal_Format)})"
                                                        : totalProductMonthToDateOverAll.ToString(SD.Two_Decimal_Format)
                                                    : null).FontColor(totalProductMonthToDateOverAll < 0
                                                    ? Colors.Red.Medium
                                                    : Colors.Black);
                                        }

                                        #endregion

                                        #endregion
                                    });

                                    #endregion
                                }
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate dispatch report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate dispatch report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(DispatchReport));
            }
        }

        #endregion

        #region -- Generate Dispatch Report Excel File --
        public async Task<IActionResult> GenerateDispatchReportExcelFile(DispatchReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var isDeliveredDateRange = viewModel.ReportType == "Delivered" && viewModel.ReportMode == "DateRange";

            if (viewModel.DateFrom == default && viewModel.ReportType != "InTransit")
            {
                TempData["warning"] = "Please enter a valid Date From";
                return RedirectToAction(nameof(DispatchReport));
            }

            if (isDeliveredDateRange && viewModel.DateTo == default)
            {
                TempData["warning"] = "Please enter a valid Date To";
                return RedirectToAction(nameof(DispatchReport));
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var currentUser = GetUserFullName();
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                Expression<Func<FilprideDeliveryReceipt, bool>>? filter;
                var dateRangeType = isDeliveredDateRange ? "ByRange" : "AsOf";
                var currencyFormatTwoDecimal = "#,##0.00";

                var statusFilter = NormalizeStatusFilter(viewModel.DRStatusFilter);

                if (viewModel.ReportType == "Delivered")
                {
                    if (dateRangeType == "AsOf")
                    {
                        if (statusFilter == "InvalidOnly")
                        {
                            filter = i => true
                                        && i.DeliveredDate <= viewModel.DateFrom
                                        && (i.Status == nameof(DRStatus.Voided));
                        }
                        else if (statusFilter == "All")
                        {
                            filter = i => true
                                        && i.DeliveredDate <= viewModel.DateFrom
                                        && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing)
                                            || i.Status == nameof(DRStatus.Voided));
                        }
                        else // ValidOnly
                        {
                            filter = i => true
                                        && i.DeliveredDate <= viewModel.DateFrom
                                        && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                        }
                    }
                    else
                    {
                        if (statusFilter == "InvalidOnly")
                        {
                            filter = i => true
                                        && i.DeliveredDate >= viewModel.DateFrom
                                        && i.DeliveredDate <= viewModel.DateTo
                                        && (i.Status == nameof(DRStatus.Voided) || i.Status == nameof(DRStatus.Canceled));
                        }
                        else if (statusFilter == "All")
                        {
                            filter = i => true
                                        && i.DeliveredDate >= viewModel.DateFrom
                                        && i.DeliveredDate <= viewModel.DateTo
                                        && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing)
                                            || i.Status == nameof(DRStatus.Voided) || i.Status == nameof(DRStatus.Canceled));
                        }
                        else // ValidOnly
                        {
                            filter = i => true
                                        && i.DeliveredDate >= viewModel.DateFrom
                                        && i.DeliveredDate <= viewModel.DateTo
                                        && (i.Status == nameof(DRStatus.Invoiced) || i.Status == nameof(DRStatus.ForInvoicing));
                        }
                    }
                }
                else
                {
                    filter = i => true
                        && i.DeliveredDate == null
                        && i.Status == nameof(DRStatus.PendingDelivery);
                }

                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt
                    .GetAllAsync(filter, cancellationToken);

                if (!deliveryReceipts.Any())
                {
                    TempData["info"] = "No record found";
                    return RedirectToAction(nameof(DispatchReport));
                }

                deliveryReceipts = deliveryReceipts.OrderBy(dr => dr.Date);
                var drIds = deliveryReceipts
                    .Select(dr => dr.DeliveryReceiptId)
                    .ToList();
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Where(rr => rr.DeliveryReceiptId != null
                                && drIds.Contains(rr.DeliveryReceiptId.Value)
                                && rr.Status == nameof(Status.Posted))
                    .GroupBy(rr => rr.DeliveryReceiptId!.Value)
                    .Select(group => new
                    {
                        DeliveryReceiptId = group.Key,
                        ReceivingReportNos = string.Join(", ", group
                            .Select(rr => rr.OldRRNo ?? rr.ReceivingReportNo)
                            .Where(rr => !string.IsNullOrWhiteSpace(rr))
                            .Distinct()),
                        SupplierInvoiceNumbers = string.Join(", ", group
                            .Select(rr => rr.SupplierInvoiceNumber)
                            .Where(si => !string.IsNullOrWhiteSpace(si))
                            .Distinct()),
                        WithdrawalCertificates = string.Join(", ", group
                            .Select(rr => rr.WithdrawalCertificate)
                            .Where(wc => !string.IsNullOrWhiteSpace(wc))
                            .Distinct()),
                        LiftingDate = group.Max(rr => rr.Date),
                        QuantityReceived = group.Sum(rr => rr.QuantityReceived),
                        Amount = group.Sum(rr => rr.Amount)
                    })
                    .ToDictionaryAsync(x => x.DeliveryReceiptId, cancellationToken);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Dispatch Report");

                // Insert image from root directory
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");
                var picture = await worksheet.Drawings.AddPictureAsync("CompanyLogo", new FileInfo(imagePath));
                picture.SetPosition(0, 0, 0, 0);
                picture.SetSize(200, 60);

                var mergedCellsA5 = worksheet.Cells["A5:B5"];
                mergedCellsA5.Merge = true;
                mergedCellsA5.Value = "OPERATION - LOGISTICS";

                var mergedCellsA6 = worksheet.Cells["A6:B6"];
                mergedCellsA6.Merge = true;

                mergedCellsA6.Value =
                    viewModel.ReportType == "InTransit"
                        ? $"DISPATCH REPORT AS OF {DateTimeHelper.GetCurrentPhilippineTime():dd MMM, yyyy}"
                        : dateRangeType == "AsOf"
                            ? $"DISPATCH REPORT AS OF {viewModel.DateFrom:dd MMM, yyyy}"
                            : $"DISPATCH REPORT from {viewModel.DateFrom:dd MMM, yyyy} to {viewModel.DateTo:dd MMM, yyyy}";


                var mergedCellsA7 = worksheet.Cells["A7:B7"];
                mergedCellsA7.Merge = true;
                mergedCellsA7.Value = viewModel.ReportType == "Delivered" ? "DELIVERED" : "IN TRANSIT";

                // Add Status Filter label
                worksheet.Cells["A8"].Value = "Status Filter:";
                worksheet.Cells["B8"].Value = GetStatusFilterLabel(statusFilter);

                worksheet.Cells["A8"].Value = "Date and Time Generated: ";
                worksheet.Cells["B8"].Value = DateTimeHelper.GetCurrentPhilippineTime();
                worksheet.Cells["B8"].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                worksheet.Cells["B8"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                // Table headers
                worksheet.Cells["A10"].Value = "DR DATE";
                worksheet.Cells["B10"].Value = "CUSTOMER NAME";
                worksheet.Cells["C10"].Value = "TYPE";
                worksheet.Cells["D10"].Value = "DR NO.";
                worksheet.Cells["E10"].Value = "PRODUCTS";
                worksheet.Cells["F10"].Value = "SELLING PRICE";
                worksheet.Cells["G10"].Value = "QTY.";
                worksheet.Cells["H10"].Value = "PICK-UP POINT";
                worksheet.Cells["I10"].Value = "PO #";
                worksheet.Cells["J10"].Value = "ATL#";
                worksheet.Cells["K10"].Value = "COS NO.";
                worksheet.Cells["L10"].Value = "HAULER NAME";
                worksheet.Cells["M10"].Value = "SUPPLIER";
                worksheet.Cells["N10"].Value = "DELIVERY OPTION";
                worksheet.Cells["O10"].Value = "FREIGHT CHARGE";
                worksheet.Cells["P10"].Value = "ECC";
                worksheet.Cells["Q10"].Value = "TOTAL FREIGHT";

                #region Remove this in the future
                //TODO Remove this in the future
                worksheet.Cells["R10"].Value = "OTC COS No.";
                worksheet.Cells["S10"].Value = "OTC DR No.";
                #endregion

                worksheet.Cells["T10"].Value = "RR NO.";
                worksheet.Cells["U10"].Value = "UNIT COST.";
                worksheet.Cells["V10"].Value = "SUPPLIER'S SI";
                worksheet.Cells["W10"].Value = "SUPPLIER'S WC";

                if (viewModel.ReportType == "Delivered")
                {
                    worksheet.Cells["X10"].Value = "DELIVERED DATE";
                    worksheet.Cells["Y10"].Value = "STATUS";
                }
                else
                {
                    worksheet.Cells["X10"].Value = "LIFTING DATE";
                    worksheet.Cells["Y10"].Value = "LIFTING QUANTITY";
                }
                worksheet.Cells["Z10"].Value = "TOTAL COST";

                // Audit info columns — only for Delivered + All or InvalidOnly
                bool showVoidCancelColumns = viewModel.ReportType == "Delivered" && statusFilter != "ValidOnly";

                if (showVoidCancelColumns)
                {
                    worksheet.Cells["AA10"].Value = "VOIDED BY";
                    worksheet.Cells["AB10"].Value = "VOIDED DATE";
                }

                int currentRow = 11;
                string headerColumn = showVoidCancelColumns ? "AB10" : "Z10";
                int grandTotalColumn = showVoidCancelColumns ? 28 : 26;
                decimal grandSumOfTotalFreightAmount = 0;
                decimal grandTotalQuantity = 0;
                decimal totalLiftedQuantity = 0;
                decimal grandTotalAmount = 0;

                foreach (var dr in deliveryReceipts)
                {
                    var quantity = dr.Quantity;
                    var freightCharge = dr.Freight;
                    var ecc = dr.ECC;
                    var totalFreightAmount = dr.FreightAmount;
                    var totalAmount = dr.TotalAmount;
                    var liftedQuantity = 0m;
                    receivingReports.TryGetValue(dr.DeliveryReceiptId, out var rr);

                    if (viewModel.ReportType == "Delivered" && dateRangeType == "AsOf" &&
                        dr.DeliveredDate != viewModel.DateFrom)
                    {
                        continue;
                    }

                    worksheet.Cells[currentRow, 1].Value = dr.Date;
                    worksheet.Cells[currentRow, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[currentRow, 2].Value = dr.CustomerOrderSlip?.CustomerName;
                    worksheet.Cells[currentRow, 3].Value = dr.CustomerOrderSlip?.CustomerType;
                    worksheet.Cells[currentRow, 4].Value = dr.DeliveryReceiptNo;
                    worksheet.Cells[currentRow, 5].Value = dr.CustomerOrderSlip?.ProductName;
                    worksheet.Cells[currentRow, 6].Value = dr.CustomerOrderSlip?.DeliveredPrice;
                    worksheet.Cells[currentRow, 7].Value = dr.Quantity;
                    worksheet.Cells[currentRow, 8].Value = dr.CustomerOrderSlip?.Depot;
                    worksheet.Cells[currentRow, 9].Value = dr.PurchaseOrder?.PurchaseOrderNo;
                    worksheet.Cells[currentRow, 10].Value = dr.AuthorityToLoadNo;
                    worksheet.Cells[currentRow, 11].Value = dr.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[currentRow, 12].Value = dr.Hauler?.SupplierName;
                    worksheet.Cells[currentRow, 13].Value = dr.PurchaseOrder?.SupplierName;
                    worksheet.Cells[currentRow, 14].Value = dr.CustomerOrderSlip?.DeliveryOption;
                    worksheet.Cells[currentRow, 15].Value = freightCharge;
                    worksheet.Cells[currentRow, 16].Value = ecc;
                    worksheet.Cells[currentRow, 17].Value = totalFreightAmount;
                    worksheet.Cells[currentRow, 18].Value = dr.CustomerOrderSlip?.OldCosNo;
                    worksheet.Cells[currentRow, 19].Value = dr.ManualDrNo;
                    worksheet.Cells[currentRow, 20].Value = rr?.ReceivingReportNos;
                    worksheet.Cells[currentRow, 21].Value = rr != null
                        ? DivideOrZero(rr.Amount, rr.QuantityReceived)
                        : 0m;
                    worksheet.Cells[currentRow, 22].Value = rr?.SupplierInvoiceNumbers;
                    worksheet.Cells[currentRow, 23].Value = rr?.WithdrawalCertificates;

                    if (viewModel.ReportType == "Delivered")
                    {
                        worksheet.Cells[currentRow, 24].Value = dr.DeliveredDate;
                        worksheet.Cells[currentRow, 24].Style.Numberformat.Format = "MMM/dd/yyyy";
                        worksheet.Cells[currentRow, 25].Value = dr.Status == nameof(DRStatus.PendingDelivery) ? "IN TRANSIT" : dr.Status.ToUpper();
                    }
                    else
                    {
                        if (dr.HasReceivingReport)
                        {
                            liftedQuantity = rr?.QuantityReceived ?? 0m;
                    worksheet.Cells[currentRow, 24].Value = rr?.LiftingDate;
                            worksheet.Cells[currentRow, 24].Style.Numberformat.Format = "MMM/dd/yyyy";
                            worksheet.Cells[currentRow, 25].Value = liftedQuantity;
                            worksheet.Cells[currentRow, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                    }

                    worksheet.Cells[currentRow, 26].Value = totalAmount;
                    worksheet.Cells[currentRow, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[currentRow, 27].Value = dr.VoidedBy;
                        worksheet.Cells[currentRow, 28].Value = dr.VoidedDate;
                        worksheet.Cells[currentRow, 28].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    currentRow++;

                    grandTotalQuantity += quantity;
                    grandSumOfTotalFreightAmount += totalFreightAmount;
                    totalLiftedQuantity += liftedQuantity;
                    grandTotalAmount += totalAmount;
                }

                // Grand Total row
                worksheet.Cells[currentRow, 5].Value = "GRAND TOTAL";

                worksheet.Cells[currentRow, 7].Value = grandTotalQuantity;
                worksheet.Cells[currentRow, 17].Value = grandSumOfTotalFreightAmount;

                if (viewModel.ReportType != "Delivered" && totalLiftedQuantity != 0)
                {
                    worksheet.Cells[currentRow, 25].Value = totalLiftedQuantity;
                    worksheet.Cells[currentRow, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                }
                if (grandTotalAmount != 0)
                {
                    worksheet.Cells[currentRow, 26].Value = grandTotalAmount;
                    worksheet.Cells[currentRow, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                // Adding borders and bold styling to the total row
                using (var totalRowRange = worksheet.Cells[currentRow, 1, currentRow, grandTotalColumn])
                {
                    totalRowRange.Style.Font.Bold = true;
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

                // Styling and formatting
                worksheet.Cells["F,O,P,U"].Style.Numberformat.Format = "#,##0.0000";
                worksheet.Cells["G,Q"].Style.Numberformat.Format = "#,##0.00";

                using (var range = worksheet.Cells[$"A10:{headerColumn}"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Summary
                if (dateRangeType == "AsOf" && viewModel.ReportType == "Delivered")
                {
                    var productList = GetOrderedProductNames(
                        deliveryReceipts,
                        dr => dr.CustomerOrderSlip?.ProductName);
                    var summaryHeaderStartColumn = 11;
                    var summaryOverallColumn = summaryHeaderStartColumn + 1;
                    var summaryProductStartColumn = summaryHeaderStartColumn + 2;
                    var summaryEndColumn = summaryProductStartColumn + productList.Count - 1;

                    foreach (var customerType in deliveryReceipts.GroupBy(dr => dr.Customer!.CustomerType))
                    {
                        using (var range = worksheet.Cells[startOfSummary, summaryHeaderStartColumn, startOfSummary, summaryEndColumn])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        }
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = customerType.Key;
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 204, 204));
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Value = "TOTAL (VOLUME)";
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 156));

                        var productHeaderColumn = summaryProductStartColumn;
                        foreach (var productName in productList)
                        {
                            worksheet.Cells[startOfSummary, productHeaderColumn].Value = productName;
                            worksheet.Cells[startOfSummary, productHeaderColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                            productHeaderColumn++;
                        }

                        #region -- totalToday --

                        startOfSummary++;
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "TOTAL TODAY";
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                        var totalToday = customerType.Where(t => t.DeliveredDate == viewModel.DateFrom).Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalToday != 0 ? totalToday : 0m;
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                        int columnOne = summaryProductStartColumn;
                        foreach (var productName in productList)
                        {
                            var totalProductToday = customerType.Where(x => x.DeliveredDate == viewModel.DateFrom && x.CustomerOrderSlip?.ProductName == productName)
                                .Sum(dr => dr.Quantity);
                            worksheet.Cells[startOfSummary, columnOne].Value = totalProductToday != 0 ? totalProductToday : 0m;
                            worksheet.Cells[startOfSummary, columnOne].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            columnOne++;
                        }

                        #endregion

                        #region -- totalYesterday --

                        startOfSummary++;
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "CUM. AS OF YESTERDAY";
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                        var totalYesterday = customerType.Where(t => t.DeliveredDate < viewModel.DateFrom).Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalYesterday != 0 ? totalYesterday : 0m;
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                        int columnTwo = summaryProductStartColumn;
                        foreach (var productName in productList)
                        {
                            var totalProductYesterday = customerType.Where(x => x.DeliveredDate < viewModel.DateFrom && x.CustomerOrderSlip?.ProductName == productName).Sum(dr => dr.Quantity);
                            worksheet.Cells[startOfSummary, columnTwo].Value = totalProductYesterday != 0 ? totalProductYesterday : 0m;
                            worksheet.Cells[startOfSummary, columnTwo].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            columnTwo++;
                        }

                        #endregion

                        #region -- Month to date --

                        startOfSummary++;
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "MONTH TO DATE";
                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                        var totalMonthToDate = customerType.Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalMonthToDate != 0 ? totalMonthToDate : 0m;
                        worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                        int columnThree = summaryProductStartColumn;
                        foreach (var productName in productList)
                        {
                            var totalProductMonthToDate = customerType.Where(x => x.CustomerOrderSlip?.ProductName == productName).Sum(dr => dr.Quantity);
                            worksheet.Cells[startOfSummary, columnThree].Value = totalProductMonthToDate != 0 ? totalProductMonthToDate : 0m;
                            worksheet.Cells[startOfSummary, columnThree].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            columnThree++;
                        }

                        #endregion

                        worksheet.Cells[startOfSummary, summaryHeaderStartColumn, startOfSummary, summaryEndColumn].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        startOfSummary += 2;
                    }

                    // All product types
                    using (var range = worksheet.Cells[startOfSummary, summaryHeaderStartColumn, startOfSummary, summaryEndColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    }
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "ALL";
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(56, 204, 204));
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Value = "TOTAL (VOLUME)";
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 156));

                    var overallProductHeaderColumn = summaryProductStartColumn;
                    foreach (var productName in productList)
                    {
                        worksheet.Cells[startOfSummary, overallProductHeaderColumn].Value = productName;
                        worksheet.Cells[startOfSummary, overallProductHeaderColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                        overallProductHeaderColumn++;
                    }

                    #region -- totalToday --

                    startOfSummary++;
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "TOTAL TODAY";
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                    var totalTodayOverAll = deliveryReceipts.Where(t => t.DeliveredDate == viewModel.DateFrom).Sum(dr => dr.Quantity);
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalTodayOverAll != 0 ? totalTodayOverAll : 0m;
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    int columnOneOverAll = summaryProductStartColumn;
                    foreach (var productName in productList)
                    {
                        var totalProductToday = deliveryReceipts.Where(x => x.DeliveredDate == viewModel.DateFrom && x.CustomerOrderSlip?.ProductName == productName)
                            .Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, columnOneOverAll].Value = totalProductToday != 0 ? totalProductToday : 0m;
                        worksheet.Cells[startOfSummary, columnOneOverAll].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        columnOneOverAll++;
                    }

                    #endregion

                    #region -- totalYesterday --

                    startOfSummary++;
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "CUM. AS OF YESTERDAY";
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                    var totalYesterdayOverAll = deliveryReceipts.Where(t => t.DeliveredDate < viewModel.DateFrom).Sum(dr => dr.Quantity);
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalYesterdayOverAll != 0 ? totalYesterdayOverAll : 0m;
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    int columnTwoOverAll = summaryProductStartColumn;
                    foreach (var productName in productList)
                    {
                        var totalProductYesterday = deliveryReceipts.Where(x => x.DeliveredDate < viewModel.DateFrom && x.CustomerOrderSlip?.ProductName == productName).Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, columnTwoOverAll].Value = totalProductYesterday != 0 ? totalProductYesterday : 0m;
                        worksheet.Cells[startOfSummary, columnTwoOverAll].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        columnTwoOverAll++;
                    }

                    #endregion

                    #region -- Month to date --

                    startOfSummary++;
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Value = "MONTH TO DATE";
                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn].Style.Font.Bold = true;

                    var totalMonthToDateOverAll = deliveryReceipts.Sum(dr => dr.Quantity);
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Value = totalMonthToDateOverAll != 0 ? totalMonthToDateOverAll : 0m;
                    worksheet.Cells[startOfSummary, summaryOverallColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    int columnThreeOverAll = summaryProductStartColumn;
                    foreach (var productName in productList)
                    {
                        var totalProductMonthToDate = deliveryReceipts.Where(x => x.CustomerOrderSlip?.ProductName == productName).Sum(dr => dr.Quantity);
                        worksheet.Cells[startOfSummary, columnThreeOverAll].Value = totalProductMonthToDate != 0 ? totalProductMonthToDate : 0m;
                        worksheet.Cells[startOfSummary, columnThreeOverAll].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        columnThreeOverAll++;
                    }

                    #endregion

                    worksheet.Cells[startOfSummary, summaryHeaderStartColumn, startOfSummary, summaryEndColumn].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(10, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate dispatch report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                // Return Excel file as response
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                var fileName = $"Dispatch_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate dispatch report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(DispatchReport));
            }
        }

        #endregion

        public async Task<IActionResult> SalesReport()
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            ViewModelBook viewmodel = new()
            {
                CommissioneeList = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims)
            };
            return View(viewmodel);
        }

        #region -- Generated Sales Report as Quest PDF

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratedSalesReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(SalesReport));
            }

            var statusFilter = NormalizeStatusFilter(model.StatusFilter);

            try
            {
                var sales = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, model.Commissionee, statusFilter, cancellationToken);

                if (!sales.Any())
                {
                    TempData["info"] = "No records found";
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

                                    var repoCalculator = _unitOfWork.FilprideDeliveryReceipt;

                                #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in sales)
                                    {
                                        var isCustomerVatable = record.DeliveryReceipt.CustomerOrderSlip?.VatType == SD.VatType_Vatable;
                                        var isHaulerVatable = record.DeliveryReceipt.HaulerVatType == SD.VatType_Vatable;
                                        var poNumbers = string.Join(", ", record.DeliveryReceipt.Details
                                            .Where(detail => detail.PurchaseOrder != null)
                                            .Select(detail => detail.PurchaseOrder!.PurchaseOrderNo)
                                            .Where(value => !string.IsNullOrWhiteSpace(value))
                                            .Distinct(StringComparer.OrdinalIgnoreCase));
                                        var quantity = record.DeliveryReceipt.Quantity;
                                        var freight = record.DeliveryReceipt.FreightAmount;
                                        var freightNetOfVat = isHaulerVatable
                                            ? NetOfVatOrZero(freight)
                                            : freight;
                                        var salesNetOfVat = isCustomerVatable
                                            ? NetOfVatOrZero(record.DeliveryReceipt.TotalAmount)
                                            : record.DeliveryReceipt.TotalAmount;
                                        var vat = isCustomerVatable
                                            ? VatAmountOrZero(salesNetOfVat)
                                            : 0m;

                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.DeliveredDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.AccountSpecialist);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoiceNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.DeliveryReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(!string.IsNullOrWhiteSpace(poNumbers) ? poNumbers : record.DeliveryReceipt.PurchaseOrder?.PurchaseOrderNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.DeliveryOption);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.ProductName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantity != 0 ? quantity < 0 ? $"({Math.Abs(quantity).ToString(SD.Two_Decimal_Format)})" : quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freight != 0 ? freight < 0 ? $"({Math.Abs(freight).ToString(SD.Two_Decimal_Format)})" : freight.ToString(SD.Two_Decimal_Format) : null).FontColor(freight < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.TotalAmount != 0 ? record.DeliveryReceipt.TotalAmount < 0 ? $"({Math.Abs(record.DeliveryReceipt.TotalAmount).ToString(SD.Two_Decimal_Format)})" : record.DeliveryReceipt.TotalAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(record.DeliveryReceipt.TotalAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vat != 0 ? vat < 0 ? $"({Math.Abs(vat).ToString(SD.Two_Decimal_Format)})" : vat.ToString(SD.Two_Decimal_Format) : null).FontColor(vat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(salesNetOfVat != 0 ? salesNetOfVat < 0 ? $"({Math.Abs(salesNetOfVat).ToString(SD.Two_Decimal_Format)})" : salesNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(salesNetOfVat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightNetOfVat != 0 ? freightNetOfVat < 0 ? $"({Math.Abs(freightNetOfVat).ToString(SD.Two_Decimal_Format)})" : freightNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(freightNetOfVat < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate != 0 ? record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate < 0 ? $"({Math.Abs(record.DeliveryReceipt.CustomerOrderSlip.CommissionRate).ToString(SD.Four_Decimal_Format)})" : record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate.ToString(SD.Four_Decimal_Format) : null).FontColor(record.DeliveryReceipt.CustomerOrderSlip?.CommissionRate < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.CustomerOrderSlip?.CommissioneeName);
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
                                var productList = GetOrderedProductNames(
                                    sales,
                                    s => s.DeliveryReceipt.CustomerOrderSlip!.ProductName);

                                #region -- Overall Summary

                                    col.Item().PaddingTop(10).Table(content =>
                                    {
                                        #region -- Columns Definition

                                            content.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();

                                                foreach (var _ in productList)
                                                {
                                                    columns.ConstantColumn(5);
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                    columns.RelativeColumn();
                                                }
                                            });

                                        #endregion

                                        #region -- Table Header

                                            content.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Overall").AlignCenter().SemiBold();

                                                foreach (var productName in productList)
                                                {
                                                    header.Cell();
                                                    header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text(productName).AlignCenter().SemiBold();
                                                }

                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Segment").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();

                                                foreach (var _ in productList)
                                                {
                                                    header.Cell();
                                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. SP").SemiBold();
                                                }

                                            });

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                        var totalsByProduct = CreateSummaryMetricMap(productList);

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                #region Computation for Overall

                                                var list = sales.Where(s => s.DeliveryReceipt.CustomerOrderSlip!.CustomerType == customerType.ToString()).ToList();

                                                var overAllQuantitySum = list.Sum(s => s.DeliveryReceipt.Quantity);
                                                var overallAmountSum = list.Sum(s => s.DeliveryReceipt.TotalAmount);
                                                var overallNetOfAmountSum = NetOfVatOrZero(overallAmountSum);
                                                var overallAverageSellingPrice = ComputeAverageSellingPrice(overallNetOfAmountSum, overAllQuantitySum);

                                                #endregion

                                                var productMetrics = CreateSummaryMetricMap(productList);
                                                foreach (var productName in productList)
                                                {
                                                    var productAmountSum = list
                                                        .Where(s => string.Equals(s.DeliveryReceipt.CustomerOrderSlip!.ProductName, productName, StringComparison.OrdinalIgnoreCase))
                                                        .Sum(s => s.DeliveryReceipt.TotalAmount);
                                                    productMetrics[productName].Quantity = SumQuantityByProduct(
                                                        list,
                                                        productName,
                                                        s => s.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                                        s => s.DeliveryReceipt.Quantity);
                                                    productMetrics[productName].NetOfSales = NetOfVatOrZero(productAmountSum);
                                                }

                                                content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overAllQuantitySum != 0 ? overAllQuantitySum < 0 ? $"({Math.Abs(overAllQuantitySum).ToString(SD.Two_Decimal_Format)})" : overAllQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(overAllQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfAmountSum != 0 ? overallNetOfAmountSum < 0 ? $"({Math.Abs(overallNetOfAmountSum).ToString(SD.Two_Decimal_Format)})" : overallNetOfAmountSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetOfAmountSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallAverageSellingPrice != 0 ? overallAverageSellingPrice < 0 ? $"({Math.Abs(overallAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : overallAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(overallAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);

                                                foreach (var productName in productList)
                                                {
                                                    var productMetric = productMetrics[productName];
                                                    var averageSellingPrice = ComputeAverageSellingPrice(productMetric.NetOfSales, productMetric.Quantity);

                                                    content.Cell();
                                                    content.Cell().Border(0.5f).Padding(3).AlignRight().Text(productMetric.Quantity != 0 ? productMetric.Quantity < 0 ? $"({Math.Abs(productMetric.Quantity).ToString(SD.Two_Decimal_Format)})" : productMetric.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                                    content.Cell().Border(0.5f).Padding(3).AlignRight().Text(productMetric.NetOfSales != 0 ? productMetric.NetOfSales < 0 ? $"({Math.Abs(productMetric.NetOfSales).ToString(SD.Two_Decimal_Format)})" : productMetric.NetOfSales.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.NetOfSales < 0 ? Colors.Red.Medium : Colors.Black);
                                                    content.Cell().Border(0.5f).Padding(3).AlignRight().Text(averageSellingPrice != 0 ? averageSellingPrice < 0 ? $"({Math.Abs(averageSellingPrice).ToString(SD.Four_Decimal_Format)})" : averageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);

                                                    totalsByProduct[productName].Quantity += productMetric.Quantity;
                                                    totalsByProduct[productName].NetOfSales += productMetric.NetOfSales;
                                                }
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            var averageSellingPriceForOverAll = ComputeAverageSellingPrice(totalSalesNetOfVat, overallTotalQuantity);

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity != 0 ? overallTotalQuantity < 0 ? $"({Math.Abs(overallTotalQuantity).ToString(SD.Two_Decimal_Format)})" : overallTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSalesNetOfVat != 0 ? totalSalesNetOfVat < 0 ? $"({Math.Abs(totalSalesNetOfVat).ToString(SD.Two_Decimal_Format)})" : totalSalesNetOfVat.ToString(SD.Two_Decimal_Format) : null).FontColor(totalSalesNetOfVat < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForOverAll != 0 ? averageSellingPriceForOverAll < 0 ? $"({Math.Abs(averageSellingPriceForOverAll).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForOverAll.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForOverAll < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                            foreach (var productName in productList)
                                            {
                                                var productMetric = totalsByProduct[productName];
                                                var averageSellingPrice = ComputeAverageSellingPrice(productMetric.NetOfSales, productMetric.Quantity);

                                                content.Cell();
                                                content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(productMetric.Quantity != 0 ? productMetric.Quantity < 0 ? $"({Math.Abs(productMetric.Quantity).ToString(SD.Two_Decimal_Format)})" : productMetric.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.Quantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                                content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(productMetric.NetOfSales != 0 ? productMetric.NetOfSales < 0 ? $"({Math.Abs(productMetric.NetOfSales).ToString(SD.Two_Decimal_Format)})" : productMetric.NetOfSales.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.NetOfSales < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                                content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPrice != 0 ? averageSellingPrice < 0 ? $"({Math.Abs(averageSellingPrice).ToString(SD.Four_Decimal_Format)})" : averageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            }

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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate sales report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate sales report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(SalesReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                if (dateTo.Month <= 9 && dateTo.Year == 2024)
                {
                    return RedirectToAction(nameof(GenerateSalesInvoiceReportExcelFile),
                        new { dateFrom = model.DateFrom, dateTo = model.DateTo, statusFilter = model.StatusFilter });
                }

                var salesReport = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, model.Commissionee, statusFilter, cancellationToken);

                if (salesReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
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
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                worksheet.Cells["A7"].Value = "Date Delivered";
                worksheet.Cells["B7"].Value = "Customer Name";
                worksheet.Cells["C7"].Value = "Branch";
                worksheet.Cells["D7"].Value = "Segment";
                worksheet.Cells["E7"].Value = "Specialist";
                worksheet.Cells["F7"].Value = "SI No.";
                worksheet.Cells["G7"].Value = "COS #";
                worksheet.Cells["H7"].Value = "OTC COS #";
                worksheet.Cells["I7"].Value = "DR #";
                worksheet.Cells["J7"].Value = "OTC DR #";
                worksheet.Cells["K7"].Value = "PO #";
                worksheet.Cells["L7"].Value = "IS PO #";
                worksheet.Cells["M7"].Value = "Delivery Option";
                worksheet.Cells["N7"].Value = "Items";
                worksheet.Cells["O7"].Value = "Quantity";
                worksheet.Cells["P7"].Value = "Freight";
                worksheet.Cells["Q7"].Value = "Sales G. VAT";
                worksheet.Cells["R7"].Value = "VAT";
                worksheet.Cells["S7"].Value = "Sales N. VAT";
                worksheet.Cells["T7"].Value = "Freight N. VAT";
                worksheet.Cells["U7"].Value = "Commission";
                worksheet.Cells["V7"].Value = "Commissionee";
                worksheet.Cells["W7"].Value = "Remarks";

                // Add void/cancel columns — only for All or InvalidOnly
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                if (showVoidCancelColumns)
                {
                    worksheet.Cells["X7"].Value = "Voided By";
                    worksheet.Cells["Y7"].Value = "Voided Date";
                }

                // Apply styling to the header row
                string headerEndColumn = showVoidCancelColumns ? "Y7" : "W7";
                using (var range = worksheet.Cells[$"A7:{headerEndColumn}"])
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
                var repoCalculator = _unitOfWork.FilprideDeliveryReceipt;
                var productList = GetOrderedProductNames(
                    salesReport,
                    sr => sr.DeliveryReceipt.CustomerOrderSlip!.ProductName);
                var customerTypeNames = Enum.GetValues<CustomerType>()
                    .Select(customerType => customerType.ToString())
                    .ToList();

                #region -- Initialize "Summary" variables

                    #region -- Overall

                        var retailOverallQuantitySum = 0m;
                        var retailOverallNetOfSalesSum = 0m;

                        var industrialOverallQuantitySum = 0m;
                        var industrialOverallNetOfSalesSum = 0m;

                        var governmentOverallQuantitySum = 0m;
                        var governmentOverallNetOfSalesSum = 0m;

                        var resellerOverallQuantitySum = 0m;
                        var resellerOverallNetOfSalesSum = 0m;

                    #endregion

                    #region -- totals of summary

                        var totalOverallQuantity = 0m;
                        var totalOverallNetOfSales = 0m;
                        var totalOverallAverageSellingPrice = 0m;

                    #endregion

                    var productMetricsByCustomerType = customerTypeNames.ToDictionary(
                        customerType => customerType,
                        _ => CreateSummaryMetricMap(productList),
                        StringComparer.OrdinalIgnoreCase);

                    var totalProductMetrics = CreateSummaryMetricMap(productList);

                #endregion

                foreach (var dr in salesReport)
                {
                    var isCustomerVatable = dr.DeliveryReceipt.CustomerOrderSlip?.VatType == SD.VatType_Vatable;
                    var isHaulerVatable = dr.DeliveryReceipt.HaulerVatType == SD.VatType_Vatable;
                    var freightAmount = dr.DeliveryReceipt.FreightAmount;
                    var segment = dr.DeliveryReceipt.TotalAmount;
                    var salesNetOfVat = isCustomerVatable ? NetOfVatOrZero(segment) : segment;
                    var vat = isCustomerVatable ? VatAmountOrZero(salesNetOfVat) : 0m;
                    var freightNetOfVat = isHaulerVatable ? NetOfVatOrZero(freightAmount) : freightAmount;
                    var quantity = dr.DeliveryReceipt.Quantity;

                    var customerType = dr.DeliveryReceipt.CustomerOrderSlip!.CustomerType;
                    var productName = dr.DeliveryReceipt.CustomerOrderSlip!.ProductName;

                    switch (customerType)
                    {
                        case nameof(CustomerType.Retail):
                            retailOverallQuantitySum += quantity;
                            retailOverallNetOfSalesSum += salesNetOfVat;
                            break;

                        case nameof(CustomerType.Industrial):
                            industrialOverallQuantitySum += quantity;
                            industrialOverallNetOfSalesSum += salesNetOfVat;
                            break;

                        case nameof(CustomerType.Government):
                            governmentOverallQuantitySum += quantity;
                            governmentOverallNetOfSalesSum += salesNetOfVat;
                            break;

                        case nameof(CustomerType.Reseller):
                            resellerOverallQuantitySum += quantity;
                            resellerOverallNetOfSalesSum += salesNetOfVat;
                            break;

                        default:
                            throw new ArgumentException("No customer type");
                    }

                    if (productMetricsByCustomerType.TryGetValue(customerType, out var productMetrics)
                        && productMetrics.TryGetValue(productName, out var productMetric))
                    {
                        productMetric.Quantity += quantity;
                        productMetric.NetOfSales += salesNetOfVat;
                    }

                    var poNumbers = string.Join(", ", dr.DeliveryReceipt.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .Select(detail => detail.PurchaseOrder!.PurchaseOrderNo)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                    var oldPoNumbers = string.Join(", ", dr.DeliveryReceipt.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .Select(detail => detail.PurchaseOrder!.OldPoNo)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase));

                    worksheet.Cells[row, 1].Value = dr.DeliveryReceipt.DeliveredDate;
                    worksheet.Cells[row, 2].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CustomerName;
                    worksheet.Cells[row, 3].Value = dr.DeliveryReceipt.CustomerOrderSlip?.Branch;
                    worksheet.Cells[row, 4].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CustomerType;
                    worksheet.Cells[row, 5].Value = dr.DeliveryReceipt.CustomerOrderSlip?.AccountSpecialist;
                    worksheet.Cells[row, 6].Value = dr.SalesInvoiceNo;
                    worksheet.Cells[row, 7].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[row, 8].Value = dr.DeliveryReceipt.CustomerOrderSlip?.OldCosNo;
                    worksheet.Cells[row, 9].Value = dr.DeliveryReceipt.DeliveryReceiptNo;
                    worksheet.Cells[row, 10].Value = dr.DeliveryReceipt.ManualDrNo;
                    worksheet.Cells[row, 11].Value = !string.IsNullOrWhiteSpace(poNumbers) ? poNumbers : dr.DeliveryReceipt.PurchaseOrder?.PurchaseOrderNo;
                    worksheet.Cells[row, 12].Value = !string.IsNullOrWhiteSpace(oldPoNumbers) ? oldPoNumbers : dr.DeliveryReceipt.PurchaseOrder?.OldPoNo;
                    worksheet.Cells[row, 13].Value = dr.DeliveryReceipt.CustomerOrderSlip?.DeliveryOption;
                    worksheet.Cells[row, 14].Value = dr.DeliveryReceipt.CustomerOrderSlip!.ProductName;
                    worksheet.Cells[row, 15].Value = dr.DeliveryReceipt.Quantity;
                    worksheet.Cells[row, 16].Value = freightAmount;
                    worksheet.Cells[row, 17].Value = segment;
                    worksheet.Cells[row, 18].Value = vat;
                    worksheet.Cells[row, 19].Value = salesNetOfVat;
                    worksheet.Cells[row, 20].Value = freightNetOfVat;
                    worksheet.Cells[row, 21].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CommissionRate;
                    worksheet.Cells[row, 22].Value = dr.DeliveryReceipt.CustomerOrderSlip?.CommissioneeName;
                    worksheet.Cells[row, 23].Value = dr.DeliveryReceipt.Remarks;

                    // Add void/cancel data — only for All or InvalidOnly
                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, 24].Value = dr.DeliveryReceipt.VoidedBy;
                        worksheet.Cells[row, 25].Value = dr.DeliveryReceipt.VoidedDate;
                        if (dr.DeliveryReceipt.VoidedDate.HasValue)
                        {
                            worksheet.Cells[row, 25].Style.Numberformat.Format = "MMM/dd/yyyy";
                        }
                    }

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;

                    row++;

                    totalFreightAmount += freightAmount;
                    totalVat += vat;
                    totalSalesNetOfVat += salesNetOfVat;
                    totalFreightNetOfVat += freightNetOfVat;
                    totalCommissionRate += dr.DeliveryReceipt.CustomerOrderSlip?.CommissionRate ?? 0m;
                }

                #region -- Computation of totals for summary --

                // Computation of total for Overall
                totalOverallQuantity = retailOverallQuantitySum + industrialOverallQuantitySum + governmentOverallQuantitySum + resellerOverallQuantitySum;
                totalOverallNetOfSales = retailOverallNetOfSalesSum + industrialOverallNetOfSalesSum + governmentOverallNetOfSalesSum + resellerOverallNetOfSalesSum;
                totalOverallAverageSellingPrice = ComputeAverageSellingPrice(totalOverallNetOfSales, totalOverallQuantity);

                foreach (var productName in productList)
                {
                    var totalMetric = totalProductMetrics[productName];

                    foreach (var customerTypeName in customerTypeNames)
                    {
                        var customerMetric = productMetricsByCustomerType[customerTypeName][productName];
                        totalMetric.Quantity += customerMetric.Quantity;
                        totalMetric.NetOfSales += customerMetric.NetOfSales;
                    }
                }

                #endregion

                worksheet.Cells[row, 14].Value = "Total ";
                worksheet.Cells[row, 15].Value = totalQuantity;
                worksheet.Cells[row, 16].Value = totalFreightAmount;
                worksheet.Cells[row, 17].Value = totalAmount;
                worksheet.Cells[row, 18].Value = totalVat;
                worksheet.Cells[row, 19].Value = totalSalesNetOfVat;
                worksheet.Cells[row, 20].Value = totalFreightNetOfVat;
                worksheet.Cells[row, 21].Value = salesReport.Count > 0 ? DivideOrZero(totalCommissionRate, salesReport.Count) : 0m;

                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                int lastColumn = showVoidCancelColumns ? 25 : 23;
                using (var range = worksheet.Cells[row, 1, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 13, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                var rowForSummary = row + 8;
                var summaryProductSectionStartColumn = 7;
                var summaryProductSectionWidth = 3;
                var summaryProductSectionGap = 1;

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

                foreach (var (productName, index) in productList.Select((productName, index) => (productName, index)))
                {
                    var productSectionStartColumn = summaryProductSectionStartColumn + (index * (summaryProductSectionWidth + summaryProductSectionGap));
                    var productSectionEndColumn = productSectionStartColumn + summaryProductSectionWidth - 1;

                    var mergedProductHeader = worksheet.Cells[rowForSummary - 2, productSectionStartColumn, rowForSummary - 2, productSectionEndColumn];
                    mergedProductHeader.Merge = true;
                    mergedProductHeader.Value = productName;
                    mergedProductHeader.Style.Font.Size = 13;
                    mergedProductHeader.Style.Font.Bold = true;
                    worksheet.Cells[rowForSummary - 2, productSectionStartColumn, rowForSummary - 2, productSectionEndColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells[rowForSummary - 1, productSectionStartColumn].Value = "Volume";
                    worksheet.Cells[rowForSummary - 1, productSectionStartColumn + 1].Value = "Sales N. VAT";
                    worksheet.Cells[rowForSummary - 1, productSectionStartColumn + 2].Value = "Ave. SP";
                    worksheet.Cells[rowForSummary - 1, productSectionStartColumn, rowForSummary - 1, productSectionEndColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    using (var range = worksheet.Cells[rowForSummary - 1, productSectionStartColumn, rowForSummary - 1, productSectionEndColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (var range = worksheet.Cells[rowForSummary + customerTypeNames.Count, productSectionStartColumn, rowForSummary + customerTypeNames.Count, productSectionEndColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                }

                foreach (var customerType in customerTypeNames)
                {
                    SummaryMetric overallMetric;

                    // Assign Values to Cells
                    switch (customerType)
                    {
                        case nameof(CustomerType.Retail):
                            worksheet.Cells[rowForSummary, 2].Value = nameof(CustomerType.Retail);
                            overallMetric = new SummaryMetric
                            {
                                Quantity = retailOverallQuantitySum,
                                NetOfSales = retailOverallNetOfSalesSum
                            };
                            break;

                        case nameof(CustomerType.Industrial):
                            worksheet.Cells[rowForSummary, 2].Value = nameof(CustomerType.Industrial);
                            overallMetric = new SummaryMetric
                            {
                                Quantity = industrialOverallQuantitySum,
                                NetOfSales = industrialOverallNetOfSalesSum
                            };
                            break;

                        case nameof(CustomerType.Government):
                            worksheet.Cells[rowForSummary, 2].Value = nameof(CustomerType.Government);
                            overallMetric = new SummaryMetric
                            {
                                Quantity = governmentOverallQuantitySum,
                                NetOfSales = governmentOverallNetOfSalesSum
                            };
                            break;

                        case nameof(CustomerType.Reseller):
                            worksheet.Cells[rowForSummary, 2].Value = nameof(CustomerType.Reseller);
                            overallMetric = new SummaryMetric
                            {
                                Quantity = resellerOverallQuantitySum,
                                NetOfSales = resellerOverallNetOfSalesSum
                            };
                            break;

                        default:
                            throw new ArgumentException("No customer type");
                    }

                    worksheet.Cells[rowForSummary, 3].Value = overallMetric.Quantity;
                    worksheet.Cells[rowForSummary, 4].Value = overallMetric.NetOfSales;
                    worksheet.Cells[rowForSummary, 5].Value = ComputeAverageSellingPrice(overallMetric.NetOfSales, overallMetric.Quantity);

                    foreach (var (productName, index) in productList.Select((productName, index) => (productName, index)))
                    {
                        var productSectionStartColumn = summaryProductSectionStartColumn + (index * (summaryProductSectionWidth + summaryProductSectionGap));
                        var productMetric = productMetricsByCustomerType[customerType][productName];

                        worksheet.Cells[rowForSummary, productSectionStartColumn].Value = productMetric.Quantity;
                        worksheet.Cells[rowForSummary, productSectionStartColumn + 1].Value = productMetric.NetOfSales;
                        worksheet.Cells[rowForSummary, productSectionStartColumn + 2].Value = ComputeAverageSellingPrice(productMetric.NetOfSales, productMetric.Quantity);
                    }

                    //Column style for Overall summary
                    worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                    foreach (var (_, index) in productList.Select((productName, index) => (productName, index)))
                    {
                        var productSectionStartColumn = summaryProductSectionStartColumn + (index * (summaryProductSectionWidth + summaryProductSectionGap));
                        worksheet.Cells[rowForSummary, productSectionStartColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[rowForSummary, productSectionStartColumn + 1].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[rowForSummary, productSectionStartColumn + 2].Style.Numberformat.Format = currencyFormat;
                    }

                    rowForSummary++;
                }

                var styleOfTotal = worksheet.Cells[rowForSummary, 2];
                styleOfTotal.Value = "Total";
                styleOfTotal.Style.Font.Size = 13;
                styleOfTotal.Style.Font.Bold = true;

                worksheet.Cells[rowForSummary, 3].Value = totalOverallQuantity;
                worksheet.Cells[rowForSummary, 4].Value = totalOverallNetOfSales;
                worksheet.Cells[rowForSummary, 5].Value = totalOverallAverageSellingPrice;

                worksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormat;

                foreach (var (productName, index) in productList.Select((productName, index) => (productName, index)))
                {
                    var productSectionStartColumn = summaryProductSectionStartColumn + (index * (summaryProductSectionWidth + summaryProductSectionGap));
                    var totalMetric = totalProductMetrics[productName];

                    worksheet.Cells[rowForSummary, productSectionStartColumn].Value = totalMetric.Quantity;
                    worksheet.Cells[rowForSummary, productSectionStartColumn + 1].Value = totalMetric.NetOfSales;
                    worksheet.Cells[rowForSummary, productSectionStartColumn + 2].Value = ComputeAverageSellingPrice(totalMetric.NetOfSales, totalMetric.Quantity);

                    worksheet.Cells[rowForSummary, productSectionStartColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, productSectionStartColumn + 1].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[rowForSummary, productSectionStartColumn + 2].Style.Numberformat.Format = currencyFormat;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 3);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate sales report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var fileName = $"Sales_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate sales report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(SalesReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult PostedCollection()
        {
            return View();
        }

        #region -- Generated Collection Report as Quest PDF

        public async Task<IActionResult> GeneratePostedCollection(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PostedCollection));
            }

            try
            {
                var collectionReceiptReport = await _unitOfWork.FilprideReport
                    .GetCollectionReceiptReport(model.DateFrom, model.DateTo, cancellationToken: cancellationToken);

                if (!collectionReceiptReport.Any())
                {
                    TempData["info"] = "No records found";
                    return RedirectToAction(nameof(PostedCollection));
                }

                var multipleSalesInvoicesByCollectionReceiptId = new Dictionary<int, List<FilprideSalesInvoice>>();
                foreach (var receipt in collectionReceiptReport.Where(cr => cr.MultipleSIId != null))
                {
                    var salesInvoices = await _unitOfWork.FilprideSalesInvoice
                        .GetAllAsync(x => receipt.MultipleSIId!.Contains(x.SalesInvoiceId), cancellationToken);

                    multipleSalesInvoicesByCollectionReceiptId[receipt.CollectionReceiptId] = salesInvoices.ToList();
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
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CR No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Invoice No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Terms").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Due Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date of Check").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Deposited Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Bank").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Check No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amount").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

                            decimal totalAmount = 0;

                                foreach (var record in collectionReceiptReport)
                                {
                                    if (record.SalesInvoiceId != null)
                                    {
                                        var currentAmount = record.CashAmount + record.CheckAmount;

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.CustomerOrderSlip?.CustomerName ?? record.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.CustomerOrderSlip?.CustomerType ?? record.Customer?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.TransactionDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CollectionReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.SalesInvoiceNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.Terms);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoice?.DueDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DepositedDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text($"{record.BankAccount?.Bank} {record.BankAccountNumber}");
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckNo);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentAmount != 0 ? currentAmount < 0 ? $"({Math.Abs(currentAmount).ToString(SD.Two_Decimal_Format)})" : currentAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(currentAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                        totalAmount += currentAmount;
                                    }
                                    if (record.ServiceInvoiceId != null)
                                    {
                                        var currentAmount = record.CashAmount + record.CheckAmount;

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoice?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoice?.CreatedDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CollectionReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoice?.ServiceInvoiceNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(string.Empty);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoice?.DueDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DepositedDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text($"{record.BankAccount?.Bank} {record.BankAccountNumber}");
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckNo);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentAmount != 0 ? currentAmount < 0 ? $"({Math.Abs(currentAmount).ToString(SD.Two_Decimal_Format)})" : currentAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(currentAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                        totalAmount += currentAmount;
                                    }
                                    if (record.MultipleSIId != null)
                                    {
                                        var salesInvoices = multipleSalesInvoicesByCollectionReceiptId[record.CollectionReceiptId];
                                        var currentAmount = record.CashAmount + record.CheckAmount;
                                        var firstSalesInvoice = salesInvoices.FirstOrDefault();
                                        var transactionDates = string.Join(", ", salesInvoices.Select(sales => sales.TransactionDate.ToString(SD.Date_Format)));
                                        var invoiceNumbers = string.Join(", ", salesInvoices.Select(sales => sales.SalesInvoiceNo));
                                        var terms = string.Join(", ", salesInvoices.Select(sales => sales.Terms));
                                        var dueDates = string.Join(", ", salesInvoices.Select(sales => sales.DueDate.ToString(SD.Date_Format)));

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(firstSalesInvoice?.CustomerOrderSlip?.CustomerName ?? record.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(firstSalesInvoice?.CustomerOrderSlip?.CustomerType ?? record.Customer?.CustomerType);
                                        table.Cell().Border(0.5f).Padding(3).Text(transactionDates);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CollectionReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(invoiceNumbers);
                                        table.Cell().Border(0.5f).Padding(3).Text(terms);
                                        table.Cell().Border(0.5f).Padding(3).Text(dueDates);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DepositedDate?.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text($"{record.BankAccount?.Bank} {record.BankAccountNumber}");
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CheckNo);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentAmount != 0 ? currentAmount < 0 ? $"({Math.Abs(currentAmount).ToString(SD.Two_Decimal_Format)})" : currentAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(currentAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                        totalAmount += currentAmount;
                                    }
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(12).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate posted collection report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate posted collection report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PostedCollection));
            }
        }

        #endregion

        #region -- Generate Collection Excel File --

            public async Task<IActionResult> GeneratePostedCollectionExcelFile(ViewModelBook model, CancellationToken cancellationToken)
            {
                if (!ModelState.IsValid)
                {
                    TempData["warning"] = "Please input date range";
                    return RedirectToAction(nameof(PostedCollection));
                }

                try
                {
                    var dateFrom = model.DateFrom;
                    var dateTo = model.DateTo;
                    var extractedBy = GetUserFullName();
                    var companyClaims = await GetCompanyClaimAsync();
                    if (companyClaims == null)
                    {
                        return BadRequest();
                    }

                    var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                    var collectionReceiptReport = await _unitOfWork.FilprideReport
                        .GetCollectionReceiptReport(model.DateFrom, model.DateTo, statusFilter, cancellationToken);

                    var multipleSalesInvoiceIds = collectionReceiptReport
                        .Where(cr => cr.MultipleSIId is { Length: > 0 })
                        .SelectMany(cr => cr.MultipleSIId!)
                        .Distinct()
                        .ToList();

                    var salesInvoicesById = multipleSalesInvoiceIds.Count == 0
                        ? new Dictionary<int, FilprideSalesInvoice>()
                        : await _dbContext.FilprideSalesInvoices
                            .AsNoTracking()
                            .Include(si => si.CustomerOrderSlip)
                            .Where(si => multipleSalesInvoiceIds.Contains(si.SalesInvoiceId))
                            .ToDictionaryAsync(si => si.SalesInvoiceId, cancellationToken);

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("COLLECTION");

                    var mergedCells = worksheet.Cells["A1:C1"];
                    mergedCells.Merge = true;
                    mergedCells.Value = "COLLECTION";
                    mergedCells.Style.Font.Size = 16;

                    worksheet.Cells["A2"].Value = "Date Range:";
                    worksheet.Cells["A3"].Value = "Generated By:";
                    worksheet.Cells["A4"].Value = "Company:";
                    worksheet.Cells["A5"].Value = "Status Filter:";
                    worksheet.Cells["A6"].Value = "Date and Time Generated:";

                    worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                    worksheet.Cells["B3"].Value = $"{extractedBy}";
                    worksheet.Cells["B4"].Value = $"{companyClaims}";
                    worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                    worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                    bool showVoidCancelColumns = statusFilter != "ValidOnly";

                    worksheet.Cells["A7"].Value = "CUSTOMER No.";
                    worksheet.Cells["B7"].Value = "CUSTOMER NAME";
                    worksheet.Cells["C7"].Value = "ACCT. TYPE";
                    worksheet.Cells["D7"].Value = "COLLECTION DATE";
                    worksheet.Cells["E7"].Value = "INVOICE DATE";
                    worksheet.Cells["F7"].Value = "CR No.";
                    worksheet.Cells["G7"].Value = "INVOICE No.";
                    worksheet.Cells["H7"].Value = "REFERENCE No.";
                    worksheet.Cells["I7"].Value = "TERMS";
                    worksheet.Cells["J7"].Value = "DUE DATE";
                    worksheet.Cells["K7"].Value = "CHECK DATE";
                    worksheet.Cells["L7"].Value = "DEPOSITED DATE";
                    worksheet.Cells["M7"].Value = "BANK";
                    worksheet.Cells["N7"].Value = "CHECK No.";
                    worksheet.Cells["O7"].Value = "CHECK AMOUNT.";
                    worksheet.Cells["P7"].Value = "EWT";
                    worksheet.Cells["Q7"].Value = "WVAT";
                    worksheet.Cells["R7"].Value = "PREVIOUS";
                    worksheet.Cells["S7"].Value = "CURRENT";
                    worksheet.Cells["T7"].Value = "ADVANCE";
                    worksheet.Cells["U7"].Value = "TOTAL";

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells["V7"].Value = "VOIDED BY";
                        worksheet.Cells["W7"].Value = "VOIDED DATE";
                    }

                    string headerEndColumn = showVoidCancelColumns ? "W7" : "U7";
                    var headerCells = worksheet.Cells[$"A7:{headerEndColumn}"];
                    headerCells.Style.Font.Size = 11;
                    headerCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerCells.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
                    headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    headerCells.Style.WrapText = false;
                    headerCells.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    headerCells.Style.Font.Bold = true;

                    var row = 8;
                    var startingRow = row - 1;
                    var currencyFormat = "#,##0.00";
                    var dateTextFormat = "MMM/dd/yyyy";
                    decimal totalCheckAmount = 0;
                    decimal totalEwtAmount = 0;
                    decimal totalWvatAmount = 0;
                    decimal totalAmount = 0;
                    decimal totalPreviousAmount = 0;
                    decimal totalCurrentAmount = 0;
                    decimal totalAdvanceAmount = 0;

                    void WriteVoidColumns(int targetRow, FilprideCollectionReceipt collectionReceipt)
                    {
                        if (!showVoidCancelColumns)
                        {
                            return;
                        }

                        worksheet.Cells[targetRow, 21].Value = collectionReceipt.VoidedBy;
                        worksheet.Cells[targetRow, 22].Value = collectionReceipt.VoidedDate;
                        if (collectionReceipt.VoidedDate.HasValue)
                        {
                            worksheet.Cells[targetRow, 22].Style.Numberformat.Format = dateTextFormat;
                        }
                    }

                    static void AllocateAmountByInvoiceMonth(
                        DateOnly collectionDate,
                        DateOnly invoiceDate,
                        decimal amount,
                        ref decimal previousAmount,
                        ref decimal currentAmount,
                        ref decimal advanceAmount)
                    {
                        var collectionMonth = (collectionDate.Year * 12) + collectionDate.Month;
                        var invoiceMonth = (invoiceDate.Year * 12) + invoiceDate.Month;

                        if (invoiceMonth < collectionMonth)
                        {
                            previousAmount += amount;
                        }
                        else if (invoiceMonth > collectionMonth)
                        {
                            advanceAmount += amount;
                        }
                        else
                        {
                            currentAmount += amount;
                        }
                    }

                    void WriteCollectionRow(
                        FilprideCollectionReceipt collectionReceipt,
                        string? customerName,
                        string? customerType,
                        IEnumerable<(DateOnly InvoiceDate, decimal Amount)> invoiceAllocations,
                        object? invoiceDate,
                        string? invoiceNo,
                        string? terms,
                        object? dueDate,
                        bool formatInvoiceDate,
                        bool formatDueDate)
                    {
                        decimal previousAmount = 0m;
                        decimal currentAmount = 0m;
                        decimal advanceAmount = 0m;

                        foreach (var allocation in invoiceAllocations)
                        {
                            AllocateAmountByInvoiceMonth(
                                collectionReceipt.TransactionDate,
                                allocation.InvoiceDate,
                                allocation.Amount,
                                ref previousAmount,
                                ref currentAmount,
                                ref advanceAmount);
                        }

                        var totalCollectionAmount = collectionReceipt.Total;

                        worksheet.Cells[row, 1].Value = collectionReceipt.Customer?.CustomerCode;
                        worksheet.Cells[row, 2].Value = customerName;
                        worksheet.Cells[row, 3].Value = customerType;
                        worksheet.Cells[row, 4].Value = collectionReceipt.TransactionDate;
                        worksheet.Cells[row, 5].Value = invoiceDate;
                        worksheet.Cells[row, 6].Value = collectionReceipt.CollectionReceiptNo;
                        worksheet.Cells[row, 7].Value = invoiceNo;
                        worksheet.Cells[row, 8].Value = collectionReceipt.ReferenceNo;
                        worksheet.Cells[row, 9].Value = terms;
                        worksheet.Cells[row, 10].Value = dueDate;
                        worksheet.Cells[row, 11].Value = collectionReceipt.CheckDate;
                        worksheet.Cells[row, 12].Value = collectionReceipt.DepositedDate;
                        worksheet.Cells[row, 13].Value = $"{collectionReceipt.BankAccount?.Bank} {collectionReceipt.BankAccountNumber}";
                        worksheet.Cells[row, 14].Value = collectionReceipt.CheckNo;
                        worksheet.Cells[row, 15].Value = collectionReceipt.CheckAmount != 0 ? collectionReceipt.CheckAmount : null;
                        worksheet.Cells[row, 16].Value = collectionReceipt.EWT != 0 ? collectionReceipt.EWT : null;
                        worksheet.Cells[row, 17].Value = collectionReceipt.WVAT != 0 ? collectionReceipt.WVAT : null;
                        worksheet.Cells[row, 18].Value = previousAmount != 0 ? previousAmount : null;
                        worksheet.Cells[row, 19].Value = currentAmount != 0 ? currentAmount : null;
                        worksheet.Cells[row, 20].Value = advanceAmount != 0 ? advanceAmount : null;
                        worksheet.Cells[row, 21].Value = totalCollectionAmount != 0 ? totalCollectionAmount : null;

                        worksheet.Cells[row, 4].Style.Numberformat.Format = dateTextFormat;

                        if (formatInvoiceDate)
                        {
                            worksheet.Cells[row, 5].Style.Numberformat.Format = dateTextFormat;
                        }

                        if (formatDueDate)
                        {
                            worksheet.Cells[row, 10].Style.Numberformat.Format = dateTextFormat;
                        }

                        if (collectionReceipt.CheckDate.HasValue)
                        {
                            worksheet.Cells[row, 11].Style.Numberformat.Format = dateTextFormat;
                        }

                        if (collectionReceipt.DepositedDate.HasValue)
                        {
                            worksheet.Cells[row, 12].Style.Numberformat.Format = dateTextFormat;
                        }

                        worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;

                        WriteVoidColumns(row, collectionReceipt);

                        totalCheckAmount += collectionReceipt.CheckAmount;
                        totalEwtAmount += collectionReceipt.EWT;
                        totalWvatAmount += collectionReceipt.WVAT;
                        totalPreviousAmount += previousAmount;
                        totalCurrentAmount += currentAmount;
                        totalAdvanceAmount += advanceAmount;
                        totalAmount += totalCollectionAmount;
                        row++;
                    }

                    foreach (var cr in collectionReceiptReport)
                    {
                        if (cr.SalesInvoiceId != null)
                        {
                            WriteCollectionRow(
                                cr,
                                cr.SalesInvoice?.CustomerOrderSlip?.CustomerName ?? cr.Customer?.CustomerName,
                                cr.SalesInvoice?.CustomerOrderSlip?.CustomerType ?? cr.Customer?.CustomerType,
                                cr.SalesInvoice is null
                                    ? Enumerable.Empty<(DateOnly InvoiceDate, decimal Amount)>()
                                    : new List<(DateOnly InvoiceDate, decimal Amount)>
                                    {
                                        (
                                            cr.SalesInvoice.TransactionDate,
                                            cr.Total
                                        )
                                    },
                                cr.SalesInvoice?.TransactionDate,
                                cr.SalesInvoice?.SalesInvoiceNo,
                                cr.SalesInvoice?.Terms,
                                cr.SalesInvoice?.DueDate,
                                formatInvoiceDate: true,
                                formatDueDate: true);
                            continue;
                        }

                        if (cr.ServiceInvoiceId != null)
                        {
                            WriteCollectionRow(
                                cr,
                                cr.ServiceInvoice?.CustomerName,
                                cr.Customer?.CustomerType,
                                cr.ServiceInvoice is null
                                    ? Enumerable.Empty<(DateOnly InvoiceDate, decimal Amount)>()
                                    : new List<(DateOnly InvoiceDate, decimal Amount)>
                                    {
                                        (
                                            DateOnly.FromDateTime(cr.ServiceInvoice.CreatedDate),
                                            cr.Total
                                        )
                                    },
                                cr.ServiceInvoice?.CreatedDate,
                                cr.ServiceInvoice?.ServiceInvoiceNo,
                                null,
                                cr.ServiceInvoice?.DueDate,
                                formatInvoiceDate: true,
                                formatDueDate: true);
                            continue;
                        }

                        if (cr.MultipleSIId != null)
                        {
                            var salesInvoices = new List<FilprideSalesInvoice>();
                            foreach (var salesInvoiceId in cr.MultipleSIId)
                            {
                                if (salesInvoicesById.TryGetValue(salesInvoiceId, out var salesInvoice))
                                {
                                    salesInvoices.Add(salesInvoice);
                                }
                            }

                            var firstSalesInvoice = salesInvoices.FirstOrDefault();
                            var invoiceDates = string.Join(Environment.NewLine, salesInvoices.Select(sales => sales.TransactionDate.ToString(dateTextFormat)));
                            var invoiceNumbers = string.Join(Environment.NewLine, salesInvoices.Select(sales => sales.SalesInvoiceNo));
                            var terms = string.Join(Environment.NewLine, salesInvoices.Select(sales => sales.Terms));
                            var dueDates = string.Join(Environment.NewLine, salesInvoices.Select(sales => sales.DueDate.ToString(dateTextFormat)));
                            var invoiceAllocations = salesInvoices
                                .Select((salesInvoice, index) => (
                                    salesInvoice.TransactionDate,
                                    cr.SIMultipleAmount != null && index < cr.SIMultipleAmount.Length
                                        ? cr.SIMultipleAmount[index]
                                        : 0m))
                                .ToArray();

                            WriteCollectionRow(
                                cr,
                                firstSalesInvoice?.CustomerOrderSlip?.CustomerName ?? cr.Customer?.CustomerName,
                                firstSalesInvoice?.CustomerOrderSlip?.CustomerType ?? cr.Customer?.CustomerType,
                                invoiceAllocations,
                                invoiceDates,
                                invoiceNumbers,
                                terms,
                                dueDates,
                                formatInvoiceDate: false,
                                formatDueDate: false);
                        }
                    }

                    int lastColumn = showVoidCancelColumns ? 23 : 21;

                    if (row == 8)
                    {
                        TempData["info"] = "No records found!";
                        return RedirectToAction(nameof(PostedCollection));
                    }

                    worksheet.Cells[row, 14].Value = "Total:";
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 15].Value = totalCheckAmount;
                    worksheet.Cells[row, 16].Value = totalEwtAmount;
                    worksheet.Cells[row, 17].Value = totalWvatAmount;
                    worksheet.Cells[row, 18].Value = totalPreviousAmount;
                    worksheet.Cells[row, 19].Value = totalCurrentAmount;
                    worksheet.Cells[row, 20].Value = totalAdvanceAmount;
                    worksheet.Cells[row, 21].Value = totalAmount;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;

                    using (var range = worksheet.Cells[row, 1, row, lastColumn])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                    using (var range = worksheet.Cells[row, 13, row, 21])
                    {
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Font.Bold = true;
                    }

                    int lastRow = row - 1;
                    using (var range = worksheet.Cells[8, 1, lastRow, lastColumn])
                    {
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    }

                    using (var range = worksheet.Cells[8, 4, lastRow, 11])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    }

                    using (var range = worksheet.Cells[startingRow - 1, 14, lastRow, 21])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    worksheet.Cells.AutoFitColumns();
                    worksheet.Row(7).Height = 22;
                    worksheet.Column(4).Width = 18;
                    worksheet.Column(5).Width = 22;
                    worksheet.Column(7).Width = 20;
                    worksheet.Column(9).Width = 14;
                    worksheet.Column(10).Width = 22;
                    worksheet.Column(12).Width = 18;
                    worksheet.Column(13).Width = 24;
                    worksheet.Column(4).Style.WrapText = true;
                    worksheet.Column(5).Style.WrapText = true;
                    worksheet.Column(7).Style.WrapText = true;
                    worksheet.Column(9).Style.WrapText = true;
                    worksheet.Column(10).Style.WrapText = true;
                    worksheet.Column(12).Style.WrapText = true;
                    worksheet.Column(13).Style.WrapText = true;
                    worksheet.Column(4).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(5).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(7).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(9).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(10).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(12).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Column(13).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                    worksheet.Row(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Row(7).Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Row(7).Style.WrapText = false;
                    worksheet.Cells[$"A7:{headerEndColumn}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    worksheet.Cells[$"A7:{headerEndColumn}"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    worksheet.Cells[$"A7:{headerEndColumn}"].Style.WrapText = false;
                    worksheet.View.FreezePanes(8, 1);

                    #region -- Audit Trail --

                    FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate posted collection report excel file", "Accounts Receivable Report");
                    await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                    #endregion

                    var fileName = $"Collection_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                    var stream = new MemoryStream();
                    await package.SaveAsAsync(stream, cancellationToken);
                    stream.Position = 0;
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                catch (Exception ex)
                {
                    ViewData["error"] = ex.Message;
                    _logger.LogError(ex, "Failed to generate posted collection report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
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

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(AgingReport));
            }

            try
            {
                var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.PostedBy != null
                                       && si.AmountPaid == 0
                                       && !si.IsPaid, cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["info"] = "No records found!";
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

                                var repoCalculator = _unitOfWork.FilprideSalesInvoice;

                                foreach (var record in salesInvoice)
                                {

                                    var gross = record.Amount;
                                    var netDiscount = record.Amount - record.Discount;
                                    var netOfVatAmount = record.CustomerOrderSlip?.VatType == SD.VatType_Vatable
                                        ? NetOfVatOrZero(netDiscount)
                                        : netDiscount;
                                    var withHoldingTaxAmount = record.CustomerOrderSlip?.HasEWT ?? true
                                        ? EwtAmountOrZero(netOfVatAmount, record.DeliveryReceipt?.CwtPercent ?? 0.0100m)
                                        : 0;
                                    var retentionAmount = (record.Customer?.RetentionRate ?? 0.0000m) * netOfVatAmount;
                                    var vcfAmount = 0.0000m;
                                    var adjustedGross = gross - vcfAmount;
                                    var adjustedNet = gross - vcfAmount - retentionAmount;

                                    var today = DateOnly.FromDateTime(DateTimeHelper.GetCurrentPhilippineTime());
                                    var daysDue = today > record.DueDate ? today.DayNumber - record.DueDate.DayNumber : 0;
                                    var current = (record.DueDate >= today) ? gross : 0.0000m;
                                    var oneToThirtyDays = (daysDue >= 1 && daysDue <= 30) ? gross : 0.0000m;
                                    var thirtyOneToSixtyDays = (daysDue >= 31 && daysDue <= 60) ? gross : 0.0000m;
                                    var sixtyOneToNinetyDays = (daysDue >= 61 && daysDue <= 90) ? gross : 0.0000m;
                                    var overNinetyDays = (daysDue > 90) ? gross : 0.0000m;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.TransactionDate.ToString("MMM yyyy"));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerType);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Terms);
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate aging report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate aging report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(AgingReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                var salesInvoice = await _unitOfWork.FilprideSalesInvoice
                    .GetAllAsync(si => si.PostedBy != null
                                       && si.AmountPaid == 0 && !si.IsPaid
, cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(AgingReport));
                }
                if (companyClaims == null)
                {
                    return BadRequest();
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
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

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
                var repoCalculator = _unitOfWork.FilprideSalesInvoice;

                foreach (var si in salesInvoice)
                {
                    var gross = si.Amount;
                    var netDiscount = si.Amount - si.Discount;
                    var netOfVatAmount = (si.CustomerOrderSlip?.VatType ?? SD.VatType_Vatable) == SD.VatType_Vatable
                        ? NetOfVatOrZero(netDiscount)
                        : netDiscount;
                    var withHoldingTaxAmount = si.CustomerOrderSlip?.HasEWT ?? true
                        ? EwtAmountOrZero(netOfVatAmount, si.DeliveryReceipt?.CwtPercent ?? 0.0100m)
                        : 0;
                    var retentionAmount = (si.Customer?.RetentionRate ?? 0.0000m) * netOfVatAmount;
                    var vcfAmount = 0.0000m;
                    var adjustedGross = gross - vcfAmount;
                    var adjustedNet = gross - vcfAmount - retentionAmount;

                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var daysDue = (today > si.DueDate) ? (today.DayNumber - si.DueDate.DayNumber) : 0;
                    var current = (si.DueDate >= today) ? gross : 0.0000m;
                    var oneToThirtyDays = (daysDue >= 1 && daysDue <= 30) ? gross : 0.0000m;
                    var thirtyOneToSixtyDays = (daysDue >= 31 && daysDue <= 60) ? gross : 0.0000m;
                    var sixtyOneToNinetyDays = (daysDue >= 61 && daysDue <= 90) ? gross : 0.0000m;
                    var overNinetyDays = (daysDue > 90) ? gross : 0.0000m;

                    worksheet.Cells[row, 1].Value = si.TransactionDate.ToString("MMMM yyyy");
                    worksheet.Cells[row, 2].Value = si.CustomerOrderSlip?.CustomerName;
                    worksheet.Cells[row, 3].Value = si.CustomerOrderSlip?.CustomerType;
                    worksheet.Cells[row, 4].Value = si.Terms;
                    worksheet.Cells[row, 5].Value = si.Customer?.WithHoldingTax ?? false ? "1" : "0";
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate aging report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var fileName = $"Aging_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate aging report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(AgingReport));
            }
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> ArPerCustomer()
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

        #region -- Generated AR Per Customer Report as Quest PDF

        public async Task<IActionResult> GeneratedArPerCustomer(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }
            var statusFilter = NormalizeStatusFilter(model.StatusFilter);

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ArPerCustomer));
            }

            try
            {
                var salesInvoice = await _unitOfWork.FilprideReport
                    .GetARPerCustomerReport(model.DateFrom, model.DateTo, model.Customers, statusFilter, cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["info"] = "No records found";
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
                                var totalEwtBalance = 0m;
                                var repoCalculator = _unitOfWork.FilprideDeliveryReceipt;

                                foreach (var groupByCustomer in salesInvoice.GroupBy(x => x.Customer))
                                {
                                    foreach (var record in groupByCustomer)
                                    {
                                        var isVatable = (record.CustomerOrderSlip?.VatType ?? SD.VatType_Vatable) ==
                                                        SD.VatType_Vatable;
                                        var isTaxable = record.CustomerOrderSlip?.HasEWT ?? true;
                                        var freight = record.DeliveryReceipt?.FreightAmount;
                                        var grossAmount = record.Amount;
                                        var netOfVat = isVatable
                                            ? NetOfVatOrZero(grossAmount)
                                            : grossAmount;
                                        var vatAmount = isVatable
                                            ? VatAmountOrZero(netOfVat)
                                            : 0m;
                                        var vatPerLiter = DivideOrZero(vatAmount, record.Quantity);
                                        var ewtAmount = isTaxable
                                            ? EwtAmountOrZero(netOfVat, record.DeliveryReceipt?.CwtPercent ?? 0.0100m)
                                            : 0m;
                                        var isEwtAmountPaid = record.IsTaxAndVatPaid ? ewtAmount : 0m;
                                        var ewtBalance = RoundToFour(ewtAmount - isEwtAmountPaid);

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Customer?.CustomerCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerName ?? record.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerType ?? record.Customer?.Type);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Terms);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.TransactionDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DueDate.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.SalesInvoiceNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerPoNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.CustomerOrderSlipNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Remarks);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.CustomerOrderSlip?.ProductName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Product?.ProductUnit);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.UnitPrice != 0 ? record.UnitPrice < 0 ? $"({Math.Abs(record.UnitPrice).ToString(SD.Four_Decimal_Format)})" : record.UnitPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(record.UnitPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freight != 0 ? freight < 0 ? $"({Math.Abs((decimal)freight).ToString(SD.Two_Decimal_Format)})" : freight?.ToString(SD.Two_Decimal_Format) : null).FontColor(freight < 0 ? Colors.Red.Medium : Colors.Black);
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
                                        totalVatAmount += vatAmount;
                                        totalGrossAmount += grossAmount;
                                        totalAmountPaid += record.AmountPaid;
                                        totalBalance += record.Balance;
                                        totalEwtAmount += ewtAmount;
                                        totalEwtAmountPaid += isEwtAmountPaid;
                                        totalEwtBalance += ewtBalance;
                                    }

                                    var subTotalQuantity = groupByCustomer.Sum(x => x.Quantity);

                                    var isVatableSub = groupByCustomer.Select(x => x.CustomerOrderSlip?.VatType).FirstOrDefault();
                                    var isTaxableSub = groupByCustomer.Select(x => x.CustomerOrderSlip?.HasEWT).FirstOrDefault();
                                    var subTotalFreight = groupByCustomer.Sum(x => x.DeliveryReceipt?.FreightAmount) ?? 0m;
                                    var subTotalFreightPerLiter = subTotalFreight != 0m && subTotalQuantity != 0m ? DivideOrZero(subTotalFreight, subTotalQuantity) : 0m;
                                    var subTotalGrossAmount = groupByCustomer.Sum(x => x.Amount);
                                    var subTotalNetOfVat = isVatableSub == SD.VatType_Vatable
                                        ? NetOfVatOrZero(subTotalGrossAmount)
                                        : subTotalGrossAmount;
                                    var subTotalVatAmount = isVatableSub == SD.VatType_Vatable
                                        ? VatAmountOrZero(subTotalNetOfVat)
                                        : 0m;
                                    var subTotalAmountPaid = groupByCustomer.Sum(x => x.AmountPaid);
                                    var subTotalVatPerLiter = DivideOrZero(subTotalVatAmount, subTotalQuantity);
                                    var subTotalEwtAmount = isTaxableSub == true
                                        ? EwtAmountOrZero(subTotalNetOfVat, groupByCustomer.Select(x => x.DeliveryReceipt != null ? x.DeliveryReceipt.CwtPercent : 0.0100m).FirstOrDefault())
                                        : 0m;
                                    var isEwtAmountPaidSub = groupByCustomer.Select(x => x.IsTaxAndVatPaid).FirstOrDefault() ? subTotalEwtAmount : 0m;
                                    var subTotalEwtBalance = RoundToFour(subTotalEwtAmount - isEwtAmountPaidSub);
                                    var subTotalUnitPrice = DivideOrZero(subTotalGrossAmount, subTotalQuantity);
                                    var subTotalBalance = groupByCustomer.Sum(x => x.Balance);
                                    var subTotalEwtAmountPaid = isEwtAmountPaidSub;

                                    table.Cell().ColumnSpan(12).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("SUB TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalQuantity != 0 ? subTotalQuantity < 0 ? $"({Math.Abs(subTotalQuantity).ToString(SD.Two_Decimal_Format)})" : subTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalUnitPrice != 0 ? subTotalUnitPrice < 0 ? $"({Math.Abs(subTotalUnitPrice).ToString(SD.Four_Decimal_Format)})" : subTotalUnitPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(subTotalUnitPrice < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalFreight != 0 ? subTotalFreight < 0 ? $"({Math.Abs(subTotalFreight).ToString(SD.Two_Decimal_Format)})" : subTotalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalFreight < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalFreightPerLiter != 0 ? subTotalFreightPerLiter < 0 ? $"({Math.Abs(subTotalFreightPerLiter).ToString(SD.Four_Decimal_Format)})" : subTotalFreightPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(subTotalFreightPerLiter < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalVatPerLiter != 0 ? subTotalVatPerLiter < 0 ? $"({Math.Abs(subTotalVatPerLiter).ToString(SD.Two_Decimal_Format)})" : subTotalVatPerLiter.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalVatPerLiter < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalVatAmount != 0 ? subTotalVatAmount < 0 ? $"({Math.Abs(subTotalVatAmount).ToString(SD.Two_Decimal_Format)})" : subTotalVatAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalVatAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalGrossAmount != 0 ? subTotalGrossAmount < 0 ? $"({Math.Abs(subTotalGrossAmount).ToString(SD.Two_Decimal_Format)})" : subTotalGrossAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalGrossAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalAmountPaid != 0 ? subTotalAmountPaid < 0 ? $"({Math.Abs(subTotalAmountPaid).ToString(SD.Two_Decimal_Format)})" : subTotalAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalBalance != 0 ? subTotalBalance < 0 ? $"({Math.Abs(subTotalBalance).ToString(SD.Two_Decimal_Format)})" : subTotalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEwtAmount != 0 ? subTotalEwtAmount < 0 ? $"({Math.Abs(subTotalEwtAmount).ToString(SD.Two_Decimal_Format)})" : subTotalEwtAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalEwtAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEwtAmountPaid != 0 ? subTotalEwtAmountPaid < 0 ? $"({Math.Abs(subTotalEwtAmountPaid).ToString(SD.Two_Decimal_Format)})" : subTotalEwtAmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalEwtAmountPaid < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEwtBalance != 0 ? subTotalEwtBalance < 0 ? $"({Math.Abs(subTotalEwtBalance).ToString(SD.Two_Decimal_Format)})" : subTotalEwtBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalEwtBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                }

                                totalFreightPerLiter = totalFreight != 0 && totalQuantity != 0 ? DivideOrZero(totalFreight, totalQuantity) : 0m;
                                totalVatPerLiter = DivideOrZero(totalVatAmount, totalQuantity);
                            #endregion

                            #region -- Create Table Cell for Totals

                                var unitPrice = DivideOrZero(totalGrossAmount, totalQuantity);

                                table.Cell().ColumnSpan(12).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("GRAND TOTAL:").SemiBold();
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
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalEwtBalance != 0 ? totalEwtBalance < 0 ? $"({Math.Abs(totalEwtBalance).ToString(SD.Two_Decimal_Format)})" : totalEwtBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(totalEwtBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate ar per customer report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate AR per customer report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(ArPerCustomer));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var salesInvoice = await _unitOfWork.FilprideReport
                    .GetARPerCustomerReport(model.DateFrom, model.DateTo, model.Customers, statusFilter, cancellationToken);

                if (!salesInvoice.Any())
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(ArPerCustomer));
                }

                // Create the Excel package
                using var package = new ExcelPackage();

                // Audit info columns — only for All or InvalidOnly
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ARPerCustomer");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "AR PER CUSTOMER";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom.ToString(SD.Date_Format)} - {dateTo.ToString(SD.Date_Format)}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

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
                worksheet.Cells["U7"].Value = "DM";
                worksheet.Cells["V7"].Value = "CM";
                worksheet.Cells["W7"].Value = "AMT. PAID";
                worksheet.Cells["X7"].Value = "SI BALANCE";
                worksheet.Cells["Y7"].Value = "EWT AMT.";
                worksheet.Cells["Z7"].Value = "EWT PAID";
                worksheet.Cells["AA7"].Value = "CWT BALANCE";
                worksheet.Cells["AB7"].Value = "WVAT AMT.";
                worksheet.Cells["AC7"].Value = "WVAT PAID";
                worksheet.Cells["AD7"].Value = "CWVAT BALANCE";

                // Add void/cancel columns — only for All or InvalidOnly
                if (showVoidCancelColumns)
                {
                    worksheet.Cells["AE7"].Value = "VOIDED BY";
                    worksheet.Cells["AF7"].Value = "VOIDED DATE";
                }

                // Apply styling to the header row
                string headerEndColumn = showVoidCancelColumns ? "AF7" : "AD7";
                using (var range = worksheet.Cells[$"A7:{headerEndColumn}"])
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
                var totalEwtBalance = 0m;
                var totalCwVatAmount = 0m;
                var totalCwVatAmountPaid = 0m;
                var totalCwVatBalance = 0m;
                var totalDebitAmount = 0m;
                var totalCreditAmount = 0m;
                var repoCalculator = _unitOfWork.FilprideDeliveryReceipt;

                foreach (var groupByCustomer in salesInvoice.GroupBy(x => x.Customer))
                {
                    foreach (var si in groupByCustomer)
                    {
                        var isVatable = (si.CustomerOrderSlip?.VatType ?? SD.VatType_Vatable) == SD.VatType_Vatable;
                        var isTaxable = si.CustomerOrderSlip?.HasEWT ?? true;
                        var hasCwVat = si.CustomerOrderSlip?.HasWVAT ?? true;
                        var freight = si.DeliveryReceipt?.FreightAmount;
                        var grossAmount = si.Amount;
                        var siAmountIncludingDmCmAmount = si.Amount + si.DebitAmount - si.CreditAmount;
                        var netOfVat = isVatable
                            ? NetOfVatOrZero(siAmountIncludingDmCmAmount)
                            : siAmountIncludingDmCmAmount;
                        var vatAmount = isVatable ? VatAmountOrZero(netOfVat) : 0m;
                        var vatPerLiter = DivideOrZero(vatAmount, si.Quantity);
                        var ewtAmount = isTaxable ? EwtAmountOrZero(netOfVat, si.DeliveryReceipt?.CwtPercent ?? 0.0100m) : 0m;
                        var isEwtAmountPaid = si.IsTaxAndVatPaid ? ewtAmount : 0m;
                        var ewtBalance = RoundToFour(ewtAmount - isEwtAmountPaid);
                        var cwvatAmount = hasCwVat ? EwtAmountOrZero(netOfVat, si.DeliveryReceipt?.CwvPercent ?? 0.0500m) : 0m;
                        var isCwvatAmountPaid = si.IsTaxAndVatPaid ? cwvatAmount : 0m;
                        var cwvatBalance = RoundToFour(cwvatAmount - isCwvatAmountPaid);

                        worksheet.Cells[row, 1].Value = si.Customer?.CustomerCode;
                        worksheet.Cells[row, 2].Value = si.CustomerOrderSlip?.CustomerName ?? si.Customer?.CustomerName;
                        worksheet.Cells[row, 3].Value = si.CustomerOrderSlip?.CustomerType ?? si.Customer?.CustomerType;
                        worksheet.Cells[row, 4].Value = si.Terms;
                        worksheet.Cells[row, 5].Value = si.TransactionDate;
                        worksheet.Cells[row, 6].Value = si.DueDate;
                        worksheet.Cells[row, 7].Value = si.SalesInvoiceNo;
                        worksheet.Cells[row, 8].Value = si.DeliveryReceipt?.DeliveryReceiptNo;
                        worksheet.Cells[row, 9].Value = si.CustomerOrderSlip?.CustomerPoNo;
                        worksheet.Cells[row, 10].Value = si.CustomerOrderSlip?.CustomerOrderSlipNo;
                        worksheet.Cells[row, 11].Value = si.Remarks;
                        worksheet.Cells[row, 12].Value = si.CustomerOrderSlip?.ProductName;
                        worksheet.Cells[row, 13].Value = si.Quantity;
                        worksheet.Cells[row, 14].Value = si.Product?.ProductUnit;
                        worksheet.Cells[row, 15].Value = si.UnitPrice;
                        worksheet.Cells[row, 16].Value = freight;
                        worksheet.Cells[row, 17].Value = si.DeliveryReceipt?.Freight;
                        worksheet.Cells[row, 18].Value = vatPerLiter;
                        worksheet.Cells[row, 19].Value = vatAmount;
                        worksheet.Cells[row, 20].Value = grossAmount;
                        worksheet.Cells[row, 21].Value = si.DebitAmount;
                        worksheet.Cells[row, 22].Value = si.CreditAmount;
                        worksheet.Cells[row, 23].Value = si.AmountPaid;
                        worksheet.Cells[row, 24].Value = si.Balance;
                        worksheet.Cells[row, 25].Value = ewtAmount;
                        worksheet.Cells[row, 26].Value = isEwtAmountPaid;
                        worksheet.Cells[row, 27].Value = ewtBalance;
                        worksheet.Cells[row, 28].Value = cwvatAmount;
                        worksheet.Cells[row, 29].Value = isCwvatAmountPaid;
                        worksheet.Cells[row, 30].Value = cwvatBalance;

                        // Add void/cancel data — only for All or InvalidOnly
                        if (showVoidCancelColumns)
                        {
                            worksheet.Cells[row, 31].Value = si.VoidedBy;
                            worksheet.Cells[row, 32].Value = si.VoidedDate;
                            if (si.VoidedDate.HasValue)
                            {
                                worksheet.Cells[row, 32].Style.Numberformat.Format = "MMM/dd/yyyy";
                            }
                        }

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
                        worksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[row, 28].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[row, 29].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        worksheet.Cells[row, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;

                        row++;

                        totalQuantity += si.Quantity;
                        totalFreight += freight ?? 0m;
                        totalFreightPerLiter += si.DeliveryReceipt?.Freight ?? 0m;
                        totalVatAmount += vatAmount;
                        totalGrossAmount += grossAmount;
                        totalAmountPaid += si.AmountPaid;
                        totalBalance += si.Balance;
                        totalEwtAmount += ewtAmount;
                        totalEwtAmountPaid += isEwtAmountPaid;
                        totalEwtBalance += ewtBalance;
                        totalCwVatAmount += cwvatAmount;
                        totalCwVatAmountPaid += isCwvatAmountPaid;
                        totalCwVatBalance += cwvatBalance;
                        totalDebitAmount += si.DebitAmount;
                        totalCreditAmount += si.CreditAmount;
                    }
                    var subTotalQuantity = groupByCustomer.Sum(x => x.Quantity);

                    var isVatableSub = groupByCustomer.Select(x => x.CustomerOrderSlip?.VatType).FirstOrDefault();
                    var isTaxableSub = groupByCustomer.Select(x => x.CustomerOrderSlip?.HasEWT).FirstOrDefault();
                    var hasCwVatSub = groupByCustomer.Select(x => x.CustomerOrderSlip?.HasWVAT).FirstOrDefault();
                    var subTotalFreight = groupByCustomer.Sum(x => x.DeliveryReceipt?.FreightAmount) ?? 0m;
                    var subTotalFreightPerLiter = subTotalFreight != 0m && subTotalQuantity != 0m ? DivideOrZero(subTotalFreight, subTotalQuantity) : 0m;
                    var subTotalGrossAmount = groupByCustomer.Sum(x => x.Amount);
                    var subTotalBalanceIncludingDmCmAmount = groupByCustomer.Sum(x => x.Balance);
                    var subTotalNetOfVat = isVatableSub == SD.VatType_Vatable
                        ? NetOfVatOrZero(subTotalBalanceIncludingDmCmAmount)
                        : subTotalBalanceIncludingDmCmAmount;
                    var subTotalVatAmount = isVatableSub == SD.VatType_Vatable
                        ? VatAmountOrZero(subTotalNetOfVat)
                        : 0m;
                    var subTotalAmountPaid = groupByCustomer.Sum(x => x.AmountPaid);
                    var subTotalVatPerLiter = DivideOrZero(subTotalVatAmount, subTotalQuantity);
                    var subTotalEwtAmount = isTaxableSub == true
                        ? EwtAmountOrZero(subTotalNetOfVat, groupByCustomer.Select(x => x.DeliveryReceipt != null ? x.DeliveryReceipt.CwtPercent : 0.0100m).FirstOrDefault())
                        : 0m;
                    var isEwtAmountPaidSub = groupByCustomer.Select(x => x.IsTaxAndVatPaid).FirstOrDefault() ? subTotalEwtAmount : 0m;
                    var subTotalEwtBalance = RoundToFour(subTotalEwtAmount - isEwtAmountPaidSub);
                    var subTotalUnitPrice = DivideOrZero(subTotalBalanceIncludingDmCmAmount, subTotalQuantity);
                    var subTotalBalance = groupByCustomer.Sum(x => x.Balance);
                    var subTotalEwtAmountPaid = isEwtAmountPaidSub;
                    var subTotalCwVatAmount = hasCwVatSub == true
                        ? EwtAmountOrZero(subTotalNetOfVat, groupByCustomer.Select(x => x.DeliveryReceipt != null ? x.DeliveryReceipt.CwvPercent : 0.0500m).FirstOrDefault())
                        : 0m;
                    var isCwVatAmountPaidSub = groupByCustomer.Select(x => x.IsTaxAndVatPaid).FirstOrDefault() ? subTotalCwVatAmount : 0m;
                    var subTotalCwVatBalance = RoundToFour(subTotalCwVatAmount - isCwVatAmountPaidSub);

                    var subTotalDebitAmount = groupByCustomer.Sum(x => x.DebitAmount);
                    var subTotalCreditAmount = groupByCustomer.Sum(x => x.CreditAmount);

                    worksheet.Cells[row, 12].Value = "SUB TOTAL ";

                    worksheet.Cells[row, 13].Value = subTotalQuantity;
                    worksheet.Cells[row, 15].Value = subTotalUnitPrice;
                    worksheet.Cells[row, 16].Value = subTotalFreight;
                    worksheet.Cells[row, 17].Value = subTotalFreightPerLiter;
                    worksheet.Cells[row, 18].Value = subTotalVatPerLiter;
                    worksheet.Cells[row, 19].Value = subTotalVatAmount;
                    worksheet.Cells[row, 20].Value = subTotalGrossAmount;
                    worksheet.Cells[row, 21].Value = subTotalDebitAmount;
                    worksheet.Cells[row, 22].Value = subTotalCreditAmount;
                    worksheet.Cells[row, 23].Value = subTotalAmountPaid;
                    worksheet.Cells[row, 24].Value = subTotalBalance;
                    worksheet.Cells[row, 25].Value = subTotalEwtAmount;
                    worksheet.Cells[row, 26].Value = subTotalEwtAmountPaid;
                    worksheet.Cells[row, 27].Value = subTotalEwtBalance;
                    worksheet.Cells[row, 28].Value = subTotalCwVatAmount;
                    worksheet.Cells[row, 29].Value = isCwVatAmountPaidSub;
                    worksheet.Cells[row, 30].Value = subTotalCwVatBalance;

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
                    worksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 28].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 29].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    // Apply style to sub-total row
                    int lastColumn = showVoidCancelColumns ? 32 : 30;
                    using (var range = worksheet.Cells[row, 1, row, lastColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    }

                    row++;
                }
                totalFreightPerLiter = totalFreight != 0 && totalQuantity != 0 ? DivideOrZero(totalFreight, totalQuantity) : 0m;
                totalVatPerLiter = DivideOrZero(totalVatAmount, totalQuantity);

                worksheet.Cells[row, 12].Value = "GRAND TOTAL ";

                worksheet.Cells[row, 13].Value = totalQuantity;
                worksheet.Cells[row, 15].Value = DivideOrZero(totalGrossAmount, totalQuantity);
                worksheet.Cells[row, 16].Value = totalFreight;
                worksheet.Cells[row, 17].Value = totalFreightPerLiter;
                worksheet.Cells[row, 18].Value = totalVatPerLiter;
                worksheet.Cells[row, 19].Value = totalVatAmount;
                worksheet.Cells[row, 20].Value = totalGrossAmount;
                worksheet.Cells[row, 21].Value = totalDebitAmount;
                worksheet.Cells[row, 22].Value = totalCreditAmount;
                worksheet.Cells[row, 23].Value = totalAmountPaid;
                worksheet.Cells[row, 24].Value = totalBalance;
                worksheet.Cells[row, 25].Value = totalEwtAmount;
                worksheet.Cells[row, 26].Value = totalEwtAmountPaid;
                worksheet.Cells[row, 27].Value = totalEwtBalance;
                worksheet.Cells[row, 28].Value = totalCwVatAmount;
                worksheet.Cells[row, 29].Value = totalCwVatAmountPaid;
                worksheet.Cells[row, 30].Value = totalCwVatBalance;

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
                worksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 28].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 29].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[row, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;

                // Apply style to grand total row
                int grandTotalLastColumn = showVoidCancelColumns ? 32 : 30;
                using (var range = worksheet.Cells[row, 1, row, grandTotalLastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 12, row, grandTotalLastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate ar per customer report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var fileName = $"AR_Per_Customer_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate ar per customer report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
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


            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ServiceInvoiceReport));
            }

            var statusFilter = NormalizeStatusFilter(model.StatusFilter);

            try
            {
                var serviceInvoice = await _unitOfWork.FilprideReport
                    .GetServiceInvoiceReport(model.DateFrom, model.DateTo, statusFilter, cancellationToken);

                if (!serviceInvoice.Any())
                {
                    TempData["info"] = "No records found!";
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
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerAddress);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.CustomerTin);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ServiceInvoiceNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ServiceName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Period.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DueDate.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Total != 0 ? record.Total < 0 ? $"({Math.Abs(record.Total).ToString(SD.Two_Decimal_Format)})" : record.Total.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Total < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.AmountPaid != 0 ? record.AmountPaid < 0 ? $"({Math.Abs(record.AmountPaid).ToString(SD.Two_Decimal_Format)})" : record.AmountPaid.ToString(SD.Two_Decimal_Format) : null).FontColor(record.AmountPaid < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PaymentStatus);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Instructions);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Type);

                                    totalAmount += record.Total;
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate service invoice report quest pdf", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate service invoice report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(ServiceInvoiceReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var serviceReport = await _unitOfWork.FilprideReport.GetServiceInvoiceReport(model.DateFrom, model.DateTo, statusFilter, cancellationToken);

                if (serviceReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(ServiceInvoiceReport));
                }
                // Create the Excel package
                using var package = new ExcelPackage();

                // Audit info columns — only for All or InvalidOnly
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ServiceReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SERVICE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

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

                // Add void/cancel columns — only for All or InvalidOnly
                if (showVoidCancelColumns)
                {
                    worksheet.Cells["N7"].Value = "VOIDED BY";
                    worksheet.Cells["O7"].Value = "VOIDED DATE";
                }

                // Apply styling to the header row
                string headerEndColumn = showVoidCancelColumns ? "O7" : "M7";
                using (var range = worksheet.Cells[$"A7:{headerEndColumn}"])
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
                    worksheet.Cells[row, 2].Value = sv.CustomerName;
                    worksheet.Cells[row, 3].Value = sv.CustomerAddress;
                    worksheet.Cells[row, 4].Value = sv.CustomerTin;
                    worksheet.Cells[row, 5].Value = sv.ServiceInvoiceNo;
                    worksheet.Cells[row, 6].Value = sv.ServiceName;
                    worksheet.Cells[row, 7].Value = sv.Period;
                    worksheet.Cells[row, 8].Value = sv.DueDate;
                    worksheet.Cells[row, 9].Value = sv.Total;
                    worksheet.Cells[row, 10].Value = sv.AmountPaid;
                    worksheet.Cells[row, 11].Value = sv.PaymentStatus;
                    worksheet.Cells[row, 12].Value = sv.Instructions;
                    worksheet.Cells[row, 13].Value = sv.Type;

                    // Add void/cancel data — only for All or InvalidOnly
                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, 14].Value = sv.VoidedBy;
                        worksheet.Cells[row, 15].Value = sv.VoidedDate;
                        if (sv.VoidedDate.HasValue)
                        {
                            worksheet.Cells[row, 15].Style.Numberformat.Format = "MMM/dd/yyyy";
                        }
                    }

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
                int lastColumn = showVoidCancelColumns ? 15 : 13;
                using (var range = worksheet.Cells[row, 1, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 8, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 3);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate service invoice report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var fileName = $"Service_Invoice_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate dispatch report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ServiceInvoiceReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult SalesInvoiceReport()
        {
            return View();
        }

        #region -- Generated Sales Invoice Report as Excel File (Legacy - Pre Oct 2024) --

        public async Task<IActionResult> GenerateSalesInvoiceReportExcelFile(
            DateOnly dateFrom,
            DateOnly dateTo,
            string? statusFilter,
            CancellationToken cancellationToken)
        {
            try
            {
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var normalizedStatusFilter = NormalizeStatusFilter(statusFilter);

                var salesReport = await _unitOfWork.FilprideReport
                    .GetSalesInvoiceReport(dateFrom, dateTo, normalizedStatusFilter, cancellationToken);

                if (salesReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(SalesReport));
                }

                var totalQuantity = salesReport.Sum(s => s.Quantity);
                var totalAmount = salesReport.Sum(s => s.Amount);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("SalesInvoiceReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SALES INVOICE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(normalizedStatusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

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

                var showVoidCancelColumns = normalizedStatusFilter != "ValidOnly";
                if (showVoidCancelColumns)
                {
                    worksheet.Cells["W7"].Value = "VOIDED BY";
                    worksheet.Cells["X7"].Value = "VOIDED DATE";
                }

                string headerEndColumn = showVoidCancelColumns ? "X7" : "V7";
                using (var range = worksheet.Cells[$"A7:{headerEndColumn}"])
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
                    var poNumbers = string.Join(", ", dr.DeliveryReceipt?.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .Select(detail => detail.PurchaseOrder!.PurchaseOrderNo)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase) ?? []);
                    var oldPoNumbers = string.Join(", ", dr.DeliveryReceipt?.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .Select(detail => detail.PurchaseOrder!.OldPoNo)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase) ?? []);
                    var quantity = dr.Quantity;
                    var freightAmount = dr.DeliveryReceipt?.FreightAmount ?? 0m;
                    var segment = dr.Amount;
                    var salesNetOfVat = NetOfVatOrZero(segment);
                    var vat = VatAmountOrZero(salesNetOfVat);
                    var freightNetOfVat = NetOfVatOrZero(freightAmount);

                    worksheet.Cells[row, 1].Value = dr.TransactionDate;
                    worksheet.Cells[row, 2].Value = dr.Customer?.CustomerName;
                    worksheet.Cells[row, 3].Value = dr.Customer?.CustomerType;
                    worksheet.Cells[row, 4].Value = dr.CustomerOrderSlip?.AccountSpecialist;
                    worksheet.Cells[row, 5].Value = dr.SalesInvoiceNo;
                    worksheet.Cells[row, 6].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo;
                    worksheet.Cells[row, 7].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo;
                    worksheet.Cells[row, 8].Value = dr.DeliveryReceipt?.DeliveryReceiptNo;
                    worksheet.Cells[row, 9].Value = dr.DeliveryReceipt?.ManualDrNo;
                    worksheet.Cells[row, 10].Value = !string.IsNullOrWhiteSpace(poNumbers) ? poNumbers : dr.PurchaseOrder?.PurchaseOrderNo;
                    worksheet.Cells[row, 11].Value = !string.IsNullOrWhiteSpace(oldPoNumbers) ? oldPoNumbers : dr.PurchaseOrder?.OldPoNo;
                    worksheet.Cells[row, 12].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.DeliveryOption;
                    worksheet.Cells[row, 13].Value = dr.Product?.ProductName;
                    worksheet.Cells[row, 14].Value = quantity;
                    worksheet.Cells[row, 15].Value = freightAmount;
                    worksheet.Cells[row, 16].Value = segment;
                    worksheet.Cells[row, 17].Value = vat;
                    worksheet.Cells[row, 18].Value = salesNetOfVat;
                    worksheet.Cells[row, 19].Value = freightNetOfVat;
                    worksheet.Cells[row, 20].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate;
                    worksheet.Cells[row, 21].Value = dr.DeliveryReceipt?.CustomerOrderSlip?.CommissioneeName;
                    worksheet.Cells[row, 22].Value = dr.Remarks;

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, 23].Value = dr.VoidedBy;
                        worksheet.Cells[row, 24].Value = dr.VoidedDate;
                        if (dr.VoidedDate.HasValue)
                        {
                            worksheet.Cells[row, 24].Style.Numberformat.Format = "MMM/dd/yyyy";
                        }
                    }

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

                int lastColumn = showVoidCancelColumns ? 24 : 22;
                using (var range = worksheet.Cells[row, 1, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 13, row, lastColumn])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                worksheet.View.FreezePanes(8, 3);
                worksheet.Cells.AutoFitColumns();

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate sales invoice report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SalesInvoiceReport_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate sales invoice report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(SalesReport));
            }
        }

        #endregion

        public IActionResult OtcFuelSalesReport()
        {
            return View();
        }

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
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // fetch sales report
                var salesReport = await _unitOfWork.FilprideReport
                    .GetSalesReport(model.DateFrom, model.DateTo, cancellationToken: cancellationToken);

                // check if there is no record
                if (salesReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(OtcFuelSalesReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();

                #region == Product worksheets ==

                var groupedByProductReport = salesReport
                    .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip?.ProductName)
                    .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip?.ProductName);

                foreach (var productReport in groupedByProductReport)
                {
                    var productName = productReport.First().DeliveryReceipt.CustomerOrderSlip?.ProductName;

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
                    worksheet.Cells["A6"].Value = "Date and Time Generated: ";

                    worksheet.Cells["B4"].Value = $"{dateFrom}";
                    worksheet.Cells["B5"].Value = $"{dateTo}";
                    worksheet.Cells["B6"].Value = DateTimeHelper.GetCurrentPhilippineTime();

                    worksheet.Cells["B6"].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                    worksheet.Cells["B6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
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
                    var grandTotalVolume = 0m;
                    var grandTotalAmount = 0m;
                    #endregion

                    var groupedByCustomer = productReport
                        .OrderBy(pr => pr.DeliveryReceipt.CustomerOrderSlip!.CustomerName)
                        .GroupBy(pr => pr.DeliveryReceipt.CustomerOrderSlip!.CustomerName);

                    foreach (var customer in groupedByCustomer)
                    {
                        var sortedByDateCustomer = customer
                            .OrderBy(c => c.DeliveryReceipt.DeliveredDate)
                            .ToList();

                        decimal totalVolume = 0m;
                        decimal totalAmount = 0m;

                        foreach (var transaction in sortedByDateCustomer)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.DeliveryReceipt.DeliveredDate; // Date
                            worksheet.Cells[row, 2].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.ProductName; // Items
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

                        // put total at the bottom of customer list
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

                    // put total at the bottom of customer list
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
                    var grandTotalVolume = 0m;
                    var grandTotalAmount = 0m;
                    #endregion

                    groupedByProductReport = salesReport
                        .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.ProductName)
                        .ThenBy(sr => sr.DeliveryReceipt.Customer!.CustomerName)
                        .ThenBy(sr => sr.DeliveryReceipt.DeliveredDate)
                        .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.ProductName);

                    // shows by product
                    foreach (var product in groupedByProductReport)
                    {
                        decimal totalVolume = 0m;
                        decimal totalAmount = 0m;

                        foreach (var transaction in product)
                        {
                            #region -- Assign Values to Cells --

                            worksheet.Cells[row, 1].Value = transaction.DeliveryReceipt.DeliveredDate; // Date
                            worksheet.Cells[row, 2].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.CustomerName; // Account Name
                            worksheet.Cells[row, 3].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.CustomerType; // Account Type
                            worksheet.Cells[row, 4].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.CustomerOrderSlipNo; // New COS #
                            worksheet.Cells[row, 5].Value = transaction.DeliveryReceipt.CustomerOrderSlip?.OldCosNo; // Old COS #
                            worksheet.Cells[row, 6].Value = transaction.DeliveryReceipt.DeliveryReceiptNo; // New DR #
                            worksheet.Cells[row, 7].Value = transaction.DeliveryReceipt.ManualDrNo; // Old DR #
                            worksheet.Cells[row, 8].Value = transaction.DeliveryReceipt.CustomerOrderSlip!.ProductName; // Items
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

                        // put total at the bottom of customer list
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

                        // incrementing for grand total
                        grandTotalVolume += totalVolume;
                        grandTotalAmount += totalAmount;

                        row++;
                    }

                    row++;

                    #region == Grandtotal ==
                    // showing grand total
                    worksheet.Cells[row, 8].Value = "Grand Total:";
                    worksheet.Cells[row, 9].Value = grandTotalVolume;
                    worksheet.Cells[row, 10].Value = grandTotalAmount;

                    //format gran total
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;

                    // additional formatting for the grand total
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
                    var productList = GetOrderedProductNames(
                        salesReport,
                        sr => sr.DeliveryReceipt.CustomerOrderSlip!.ProductName);

                    var groupByCustomerType = salesReport
                        .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.CustomerType)
                        .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.CustomerType)
                        .OrderBy(g => g.Key != "Retail")
                        .ThenBy(g => g.Key);

                    #region == Contents ==

                    foreach (var ct in groupByCustomerType)
                    {
                        worksheet.Cells[row, 1].Value = ct.First().DeliveryReceipt.CustomerOrderSlip!.CustomerType;
                        worksheet.Cells[row, 1].Style.Font.Bold = true;
                        worksheet.Cells[row, 1].Style.Font.Italic = true;
                        worksheet.Cells[row, 1].Style.Font.Size = 18;

                        row++;
                        worksheet.Cells[row, 1].Value = isStation ? "STATION" : "ACCOUNTS";

                        var detailStartColumn = 2;
                        foreach (var productName in productList)
                        {
                            worksheet.Cells[row, detailStartColumn].Value = productName;
                            worksheet.Cells[row, detailStartColumn + 1].Value = "AMOUNT";
                            detailStartColumn += 2;
                        }

                        var detailTotalQuantityColumn = detailStartColumn;
                        var detailTotalAmountColumn = detailStartColumn + 1;
                        worksheet.Cells[row, detailTotalQuantityColumn].Value = "TOTAL";
                        worksheet.Cells[row, detailTotalAmountColumn].Value = "AMOUNT";

                        using (var range = worksheet.Cells[row, 1, row, detailTotalAmountColumn])
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
                            .OrderBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.CustomerName)
                            .GroupBy(sr => sr.DeliveryReceipt.CustomerOrderSlip!.CustomerName);

                        foreach (var customerGroup in groupByCustomerName)
                        {
                            worksheet.Cells[row, 1].Value = customerGroup.First().DeliveryReceipt.CustomerOrderSlip!.CustomerName;
                            worksheet.Cells[row, 1].Style.Font.Bold = true;

                            var detailColumn = 2;
                            foreach (var productName in productList)
                            {
                                worksheet.Cells[row, detailColumn].Value = SumQuantityByProduct(
                                    customerGroup,
                                    productName,
                                    cg => cg.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                    cg => cg.DeliveryReceipt.Quantity);
                                worksheet.Cells[row, detailColumn + 1].Value = SumAmountByProduct(
                                    customerGroup,
                                    productName,
                                    cg => cg.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                    cg => cg.DeliveryReceipt.TotalAmount);
                                detailColumn += 2;
                            }

                            worksheet.Cells[row, detailTotalQuantityColumn].Value = customerGroup
                                .Sum(cg => cg.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, detailTotalAmountColumn].Value = customerGroup
                                .Sum(cg => cg.DeliveryReceipt.TotalAmount);

                            worksheet.Cells[row, 2, row, detailTotalAmountColumn].Style.Numberformat.Format = "#,##0.00";

                            row++;
                        }

                        worksheet.Cells[row, 1].Value = "Total";
                        var totalDetailColumn = 2;
                        foreach (var productName in productList)
                        {
                            worksheet.Cells[row, totalDetailColumn].Value = SumQuantityByProduct(
                                ct,
                                productName,
                                si => si.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                si => si.DeliveryReceipt.Quantity);
                            worksheet.Cells[row, totalDetailColumn + 1].Value = SumAmountByProduct(
                                ct,
                                productName,
                                si => si.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                si => si.DeliveryReceipt.TotalAmount);
                            totalDetailColumn += 2;
                        }

                        worksheet.Cells[row, detailTotalQuantityColumn].Value = ct
                            .Sum(si => si.DeliveryReceipt.Quantity);
                        worksheet.Cells[row, detailTotalAmountColumn].Value = ct
                            .Sum(si => si.DeliveryReceipt.TotalAmount);

                        var tillRowToResize = row;
                        worksheet.Cells[rowToResize, 1, tillRowToResize, detailTotalAmountColumn].Style.Font.Size = 10;

                        using (var range = worksheet.Cells[row, 1, row, detailTotalAmountColumn])
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
                    var grandTotalDetailColumn = 2;
                    foreach (var productName in productList)
                    {
                        worksheet.Cells[row, grandTotalDetailColumn].Value = SumQuantityByProduct(
                            salesReport,
                            productName,
                            si => si.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                            si => si.DeliveryReceipt.Quantity);
                        worksheet.Cells[row, grandTotalDetailColumn + 1].Value = SumAmountByProduct(
                            salesReport,
                            productName,
                            si => si.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                            si => si.DeliveryReceipt.TotalAmount);
                        grandTotalDetailColumn += 2;
                    }

                    worksheet.Cells[row, grandTotalDetailColumn].Value = salesReport
                        .Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, grandTotalDetailColumn + 1].Value = salesReport
                        .Sum(si => si.DeliveryReceipt.TotalAmount);

                    using (var range = worksheet.Cells[row, 1, row, grandTotalDetailColumn + 1])
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
                    var summaryProductColumn = 2;
                    foreach (var productName in productList)
                    {
                        worksheet.Cells[row, summaryProductColumn].Value = productName;
                        summaryProductColumn++;
                    }

                    var summaryTotalColumn = summaryProductColumn;
                    worksheet.Cells[row, summaryTotalColumn].Value = "TOTAL";

                    // summary columns names styling
                    using (var range = worksheet.Cells[row, 2, row, summaryTotalColumn])
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
                        worksheet.Cells[row, 1].Value = typeGroup.First().DeliveryReceipt.CustomerOrderSlip!.CustomerType;
                        worksheet.Cells[row, 1].Style.Font.Italic = true;
                        worksheet.Cells[row, 1].Style.Font.Bold = true;

                        var summaryValueColumn = 2;
                        foreach (var productName in productList)
                        {
                            worksheet.Cells[row, summaryValueColumn].Value = SumQuantityByProduct(
                                typeGroup,
                                productName,
                                tg => tg.DeliveryReceipt.CustomerOrderSlip!.ProductName,
                                tg => tg.DeliveryReceipt.Quantity);
                            summaryValueColumn++;
                        }

                        worksheet.Cells[row, summaryTotalColumn].Value = typeGroup
                            .Sum(tg => tg.DeliveryReceipt.Quantity);
                        row++;
                    }

                    // merge cells of "total" label
                    using (var range = worksheet.Cells[row, 1, row, summaryTotalColumn - 1])
                    {
                        range.Merge = true;
                        range.Value = "Total:";
                        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    // styling total value
                    worksheet.Cells[row, summaryTotalColumn].Value = salesReport.Sum(si => si.DeliveryReceipt.Quantity);
                    worksheet.Cells[row, summaryTotalColumn].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    worksheet.Cells[row, summaryTotalColumn].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, summaryTotalColumn].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 156, 252));
                    worksheet.Cells[row, summaryTotalColumn].Style.Font.Bold = true;

                    var summaryRowEnd = row;

                    // range for the summary
                    using (var range = worksheet.Cells[summaryRowStart, 1, summaryRowEnd, summaryTotalColumn])
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

                    for (int col = 2; col <= summaryTotalColumn; col++)
                    {
                        worksheet.Column(col).Width = 20;
                    }

                }

                #endregion == Month to Date Sales ==

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

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

        public IActionResult CosSummaryReport()
        {
            return View();
        }

        #region -- Generate Cos Summary Report Excel File --

        public async Task<IActionResult> GenerateCosSummaryReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(CosSummaryReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var cosSummaryReport = await _unitOfWork.FilprideReport.GetCustomerOrderSlipReport(model.DateFrom, model.DateTo, statusFilter, cancellationToken);

                if (cosSummaryReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(CosSummaryReport));
                }
                // Create the Excel package
                using var package = new ExcelPackage();

                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("CosSummaryReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "COS SUMMARY REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "COS DATE CREATED";col++;
                worksheet.Cells[row, col].Value = "CUSTOMER";col++;
                worksheet.Cells[row, col].Value = "BRANCH";col++;
                worksheet.Cells[row, col].Value = "PRODUCT";col++;
                worksheet.Cells[row, col].Value = "P.O. No.";col++;
                worksheet.Cells[row, col].Value = "COS No.";col++;
                worksheet.Cells[row, col].Value = "PRICE";col++;
                worksheet.Cells[row, col].Value = "VOLUME";col++;
                worksheet.Cells[row, col].Value = "AMOUNT";col++;
                worksheet.Cells[row, col].Value = "FREIGHT";col++;
                worksheet.Cells[row, col].Value = "COS STATUS";col++;
                worksheet.Cells[row, col].Value = "EXP OF COS";col++;
                worksheet.Cells[row, col].Value = "COMMISSIONEE";col++;
                worksheet.Cells[row, col].Value = "COMMISSION RATE";



                // Apply styling to the header row
                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(153, 102, 255));
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                row++;
                string currencyFormatTwoDecimal = "#,##0.00";
                string currencyFormatFourDecimal = "#,##0.0000";

                foreach (var record in cosSummaryReport)
                {
                    col = 1;
                    worksheet.Cells[row, col].Value = record.Date;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";col++;
                    worksheet.Cells[row, col].Value = record.CustomerName;col++;
                    worksheet.Cells[row, col].Value = record.Branch;col++;
                    worksheet.Cells[row, col].Value = record.ProductName;col++;
                    worksheet.Cells[row, col].Value = record.CustomerPoNo;col++;
                    worksheet.Cells[row, col].Value = record.CustomerOrderSlipNo;col++;
                    worksheet.Cells[row, col].Value = record.DeliveredPrice;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormatFourDecimal;col++;
                    worksheet.Cells[row, col].Value = record.Quantity;col++;
                    worksheet.Cells[row, col].Value = record.TotalAmount;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormatTwoDecimal;col++;
                    worksheet.Cells[row, col].Value = record.Freight;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormatFourDecimal;col++;
                    worksheet.Cells[row, col].Value = record.Status;col++;
                    worksheet.Cells[row, col].Value = record.ExpirationDate;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";col++;
                    worksheet.Cells[row, col].Value = record.CommissioneeName;col++;
                    worksheet.Cells[row, col].Value = record.CommissionRate;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate cos summary report excel file", "Accounts Receivable Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var fileName = $"COS_Summary_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cos summary report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(CosSummaryReport));
            }
        }

        #endregion
    }
}

