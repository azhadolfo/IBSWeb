using IBS.DataAccess.Data;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    [DepartmentAuthorize(SD.Department_ManagementAccounting, SD.Department_RCD)]
    public class ComparativeReportController : Controller
    {
        private readonly ILogger<ComparativeReportController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ComparativeReportController(ILogger<ComparativeReportController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
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

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DateOnly monthDate, string category, CancellationToken cancellation)
        {
            try
            {
                if (category == "Sales")
                {
                    var lockedSales = await _dbContext.FilprideSalesLockedRecordsQueues
                        .Include(x => x.DeliveryReceipt)
                        .ThenInclude(x => x.CustomerOrderSlip)
                        .Where(x => x.LockedDate.Month == monthDate.Month
                                    && x.LockedDate.Year == monthDate.Year)
                        .OrderBy(x => x.DeliveryReceiptId)
                        .ToListAsync(cancellation);

                    if (lockedSales.Count == 0)
                    {
                        TempData["info"] = "No records found!";
                        return RedirectToAction(nameof(Index));
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
                                        .Text("COMPARATIVE SALES REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("As Of: ").SemiBold();
                                        text.Span(monthDate.ToString("MMMM yyyy"));
                                    });

                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                            #endregion

                            #region Content

                            page.Content().PaddingTop(10).Table(table =>
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
                                });

                                #endregion

                                #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CURRENT").SemiBold();
                                    header.Cell();
                                    header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PREVIOUS").SemiBold();
                                    header.Cell();
                                    header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("INCREASE/(DECREASE)").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Reference").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                    header.Cell();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                    header.Cell();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                });

                                #endregion

                                #region -- Loop to Show Records

                                var sumCurrentQuantity = 0m;
                                var sumCurrentPrice = 0m;
                                var sumCurrentTotal = 0m;
                                var sumPreviousQuantity = 0m;
                                var sumPreviousPrice = 0m;
                                var sumPreviousTotal = 0m;
                                var sumQuantityDifference = 0m;
                                var sumPriceDifference = 0m;
                                var sumTotalDifference = 0m;

                                foreach (var record in lockedSales)
                                {
                                    var currentQuantity = record.DeliveryReceipt.Quantity;
                                    var currentPrice = record.DeliveryReceipt.CustomerOrderSlip!.DeliveredPrice;
                                    var currentTotal = record.DeliveryReceipt.TotalAmount;
                                    var previousQuantity = record.Quantity;
                                    var previousPrice = record.Price;
                                    var previousTotal = previousQuantity * previousPrice;
                                    var quantityDifference = currentQuantity - previousQuantity;
                                    var priceDifference = currentPrice - previousPrice;
                                    var totalDifference = currentTotal - previousTotal;


                                    sumCurrentQuantity += currentQuantity;

                                    sumCurrentTotal += currentTotal;
                                    sumPreviousQuantity += previousQuantity;

                                    sumPreviousTotal += previousTotal;
                                    sumQuantityDifference += quantityDifference;
                                    sumPriceDifference += priceDifference;
                                    sumTotalDifference += totalDifference;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt.DeliveryReceiptNo);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentQuantity.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentPrice.ToString(SD.Four_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentTotal.ToString(SD.Two_Decimal_Format));
                                    table.Cell();
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousQuantity.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousPrice.ToString(SD.Four_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousTotal.ToString(SD.Two_Decimal_Format));
                                    table.Cell();
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantityDifference != 0 ? quantityDifference < 0 ? $"({Math.Abs(quantityDifference).ToString(SD.Two_Decimal_Format)})" : quantityDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(quantityDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(priceDifference != 0 ? priceDifference < 0 ? $"({Math.Abs(priceDifference).ToString(SD.Four_Decimal_Format)})" : priceDifference.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(priceDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalDifference != 0 ? totalDifference < 0 ? $"({Math.Abs(totalDifference).ToString(SD.Two_Decimal_Format)})" : totalDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalDifference < 0 ? Colors.Red.Medium : Colors.Black);

                                }

                                sumCurrentPrice = sumCurrentTotal != 0 && sumCurrentQuantity != 0 ? sumCurrentTotal / sumCurrentQuantity : 0;
                                sumPreviousPrice = sumPreviousTotal != 0 && sumPreviousQuantity != 0 ? sumPreviousTotal / sumPreviousQuantity : 0;

                                #endregion

                                #region -- Create Table Cell for Totals

                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTALS").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentQuantity.ToString(SD.Two_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentPrice.ToString(SD.Four_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentTotal.ToString(SD.Two_Decimal_Format));
                                table.Cell();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousQuantity.ToString(SD.Two_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousPrice.ToString(SD.Four_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousTotal.ToString(SD.Two_Decimal_Format));
                                table.Cell();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumQuantityDifference != 0 ? sumQuantityDifference < 0 ? $"({Math.Abs(sumQuantityDifference).ToString(SD.Two_Decimal_Format)})" : sumQuantityDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(sumQuantityDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPriceDifference != 0 ? sumPriceDifference < 0 ? $"({Math.Abs(sumPriceDifference).ToString(SD.Four_Decimal_Format)})" : sumPriceDifference.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(sumPriceDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumTotalDifference != 0 ? sumTotalDifference < 0 ? $"({Math.Abs(sumTotalDifference).ToString(SD.Two_Decimal_Format)})" : sumTotalDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(sumTotalDifference < 0 ? Colors.Red.Medium : Colors.Black);

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
                else
                {
                    var lockedPurchases = await _dbContext.FilpridePurchaseLockedRecordsQueues
                        .Include(x => x.ReceivingReport)
                        .ThenInclude(x => x.PurchaseOrder)
                        .ThenInclude(x => x!.ActualPrices)
                        .Where(x => x.LockedDate.Month == monthDate.Month
                                    && x.LockedDate.Year == monthDate.Year)
                        .OrderBy(x => x.ReceivingReportId)
                        .ToListAsync(cancellation);

                    if (lockedPurchases.Count == 0)
                    {
                        TempData["info"] = "No records found!";
                        return RedirectToAction(nameof(Index));
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
                                        .Text("COMPARATIVE PURCHASE REPORT")
                                        .FontSize(20).SemiBold();

                                    column.Item().Text(text =>
                                    {
                                        text.Span("As Of: ").SemiBold();
                                        text.Span(monthDate.ToString("MMMM yyyy"));
                                    });

                                });

                                row.ConstantItem(size: 100)
                                    .Height(50)
                                    .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                            });

                            #endregion

                            #region Content

                            page.Content().PaddingTop(10).Table(table =>
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
                                });

                                #endregion

                                #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CURRENT").SemiBold();
                                    header.Cell();
                                    header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PREVIOUS").SemiBold();
                                    header.Cell();
                                    header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("INCREASE/(DECREASE)").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Reference").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                    header.Cell();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                    header.Cell();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                });

                                #endregion

                                #region -- Loop to Show Records

                                var sumCurrentQuantity = 0m;
                                var sumCurrentPrice = 0m;
                                var sumCurrentTotal = 0m;
                                var sumPreviousQuantity = 0m;
                                var sumPreviousPrice = 0m;
                                var sumPreviousTotal = 0m;
                                var sumQuantityDifference = 0m;
                                var sumPriceDifference = 0m;
                                var sumTotalDifference = 0m;

                                foreach (var record in lockedPurchases)
                                {
                                    var currentQuantity = record.ReceivingReport.QuantityReceived;
                                    var currentTotal = record.ReceivingReport.Amount;
                                    var currentPrice = currentTotal / currentQuantity;
                                    var previousQuantity = record.Quantity;
                                    var previousPrice = record.Price;
                                    var previousTotal = previousQuantity * previousPrice;
                                    var quantityDifference = currentQuantity - previousQuantity;
                                    var priceDifference = currentPrice - previousPrice;
                                    var totalDifference = currentTotal - previousTotal;


                                    sumCurrentQuantity += currentQuantity;

                                    sumCurrentTotal += currentTotal;
                                    sumPreviousQuantity += previousQuantity;

                                    sumPreviousTotal += previousTotal;
                                    sumQuantityDifference += quantityDifference;
                                    sumPriceDifference += priceDifference;
                                    sumTotalDifference += totalDifference;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.ReceivingReport.ReceivingReportNo);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentQuantity.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentPrice.ToString(SD.Four_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentTotal.ToString(SD.Two_Decimal_Format));
                                    table.Cell();
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousQuantity.ToString(SD.Two_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousPrice.ToString(SD.Four_Decimal_Format));
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(previousTotal.ToString(SD.Two_Decimal_Format));
                                    table.Cell();
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantityDifference != 0 ? quantityDifference < 0 ? $"({Math.Abs(quantityDifference).ToString(SD.Two_Decimal_Format)})" : quantityDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(quantityDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(priceDifference != 0 ? priceDifference < 0 ? $"({Math.Abs(priceDifference).ToString(SD.Four_Decimal_Format)})" : priceDifference.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(priceDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalDifference != 0 ? totalDifference < 0 ? $"({Math.Abs(totalDifference).ToString(SD.Two_Decimal_Format)})" : totalDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalDifference < 0 ? Colors.Red.Medium : Colors.Black);

                                }

                                sumCurrentPrice = sumCurrentTotal != 0 && sumCurrentQuantity != 0 ? sumCurrentTotal / sumCurrentQuantity : 0;
                                sumPreviousPrice = sumPreviousTotal != 0 && sumPreviousQuantity != 0 ? sumPreviousTotal / sumPreviousQuantity : 0;

                                #endregion

                                #region -- Create Table Cell for Totals

                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTALS").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentQuantity.ToString(SD.Two_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentPrice.ToString(SD.Four_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumCurrentTotal.ToString(SD.Two_Decimal_Format));
                                table.Cell();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousQuantity.ToString(SD.Two_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousPrice.ToString(SD.Four_Decimal_Format));
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPreviousTotal.ToString(SD.Two_Decimal_Format));
                                table.Cell();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumQuantityDifference != 0 ? sumQuantityDifference < 0 ? $"({Math.Abs(sumQuantityDifference).ToString(SD.Two_Decimal_Format)})" : sumQuantityDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(sumQuantityDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumPriceDifference != 0 ? sumPriceDifference < 0 ? $"({Math.Abs(sumPriceDifference).ToString(SD.Four_Decimal_Format)})" : sumPriceDifference.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(sumPriceDifference < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(sumTotalDifference != 0 ? sumTotalDifference < 0 ? $"({Math.Abs(sumTotalDifference).ToString(SD.Two_Decimal_Format)})" : sumTotalDifference.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(sumTotalDifference < 0 ? Colors.Red.Medium : Colors.Black);

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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
