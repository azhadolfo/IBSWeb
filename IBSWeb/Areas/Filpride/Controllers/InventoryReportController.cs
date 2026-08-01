using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class InventoryReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<InventoryReportController> _logger;

        public InventoryReportController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<InventoryReportController> logger)
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

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
        }

        [HttpGet]
        public async Task<IActionResult> InventoryReport(CancellationToken cancellationToken)
        {
            InventoryReportViewModel viewModel = new InventoryReportViewModel();

            var companyClaims = await GetCompanyClaimAsync();

            viewModel.Products = await _unitOfWork.GetProductListAsyncById(cancellationToken);

            viewModel.PO = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DisplayInventoryReport(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(InventoryReport));
            }

            try
            {
                var inventoryRecords = await _dbContext.FilprideInventories
                    .AsNoTracking()
                    .Include(i => i.Product)
                    .Include(i => i.PurchaseOrder)
                    .Where(i => i.Date >= viewModel.DateTo
                                && i.Date <= viewModel.DateTo.AddMonths(1).AddDays(-1)
                                && i.Company == companyClaims
                                && (viewModel.ProductId == null || i.ProductId == viewModel.ProductId)
                                && (viewModel.POId == null || i.POId == viewModel.POId))
                    .OrderBy(i => i.Product.ProductName)
                    .ThenBy(i => i.POId)
                    .ThenBy(i => i.Date)
                    .ToListAsync(cancellationToken);

                if (inventoryRecords.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(InventoryReport));
                }

                var inventories = inventoryRecords
                    .GroupBy(x => new { x.ProductId, x.POId })
                    .OrderBy(g => g.First().Product.ProductName)
                    .ThenBy(g => g.Key.POId)
                    .ToList();

                var showProductSections = viewModel.ProductId == null;
                var productName = viewModel.ProductId == null
                    ? "ALL PRODUCTS"
                    : inventoryRecords.First().Product.ProductName;

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

                        page.Header().Height(76).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("INVENTORY REPORT")
                                    .FontSize(20).SemiBold();

                                column.Item().Text(text =>
                                {
                                    text.Span("As of ").SemiBold();
                                    text.Span(viewModel.DateTo.ToString("MMMM yyyy"));
                                });

                                column.Item().PaddingTop(10).Text(text =>
                                {
                                    text.Span("Product Name: ").FontSize(16).SemiBold();
                                    text.Span(productName).FontSize(16);
                                });
                            });

                            row.ConstantItem(size: 100)
                                .Height(50)
                                .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                        });

                        #endregion

                        #region -- Content

                        page.Content().Table(table =>
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
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Particular").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Reference").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Cost").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Inventory Balance").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Unit Cost Average").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total Balance").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

                                var grandTotalInventoryBalance = 0m;
                                var grandTotalTotalBalance = 0m;
                                string? previousProductName = null;
                                var productTotalInventoryBalance = 0m;
                                var productTotalTotalBalance = 0m;

                                for (var groupIndex = 0; groupIndex < inventories.Count; groupIndex++)
                                {
                                    var group = inventories[groupIndex];
                                    var currentProductName = group.First().Product.ProductName;

                                    if (showProductSections && !string.Equals(previousProductName, currentProductName, StringComparison.Ordinal))
                                    {
                                        table.Cell().ColumnSpan(10)
                                            .Background(Colors.Grey.Lighten2)
                                            .Border(0.5f)
                                            .PaddingVertical(5)
                                            .PaddingHorizontal(3)
                                            .Text($"PRODUCT: {currentProductName}")
                                            .FontColor(Colors.Black)
                                            .SemiBold();
                                        previousProductName = currentProductName;
                                    }

                                    var subTotalInventoryBalance = 0m;
                                    var subTotalAverageCost = 0m;
                                    var subTotalTotalBalance = 0m;

                                    foreach (var record in group.OrderBy(e => e.Date)
                                                 .ThenBy(x => x.Particular == "Purchases" ? 0 : 1)
                                                 .ThenBy(x => x.InventoryId))
                                    {
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Particular);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Reference);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Cost != 0 ? record.Cost < 0 ? $"({Math.Abs(record.Cost).ToString(SD.Four_Decimal_Format)})" : record.Cost.ToString(SD.Four_Decimal_Format) : null).FontColor(record.Cost < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Total != 0 ? record.Total < 0 ? $"({Math.Abs(record.Total).ToString(SD.Two_Decimal_Format)})" : record.Total.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Total < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.InventoryBalance != 0 ? record.InventoryBalance < 0 ? $"({Math.Abs(record.InventoryBalance).ToString(SD.Two_Decimal_Format)})" : record.InventoryBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(record.InventoryBalance < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.AverageCost != 0 ? record.AverageCost < 0 ? $"({Math.Abs(record.AverageCost).ToString(SD.Four_Decimal_Format)})" : record.AverageCost.ToString(SD.Four_Decimal_Format) : null).FontColor(record.AverageCost < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.TotalBalance != 0 ? record.TotalBalance < 0 ? $"({Math.Abs(record.TotalBalance).ToString(SD.Two_Decimal_Format)})" : record.TotalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(record.TotalBalance < 0 ? Colors.Red.Medium : Colors.Black);

                                        subTotalInventoryBalance = record.InventoryBalance;
                                        subTotalAverageCost = record.AverageCost;
                                        subTotalTotalBalance = record.TotalBalance;

                                    }

                                    table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten1).Border(0.5f);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Sub Total").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalInventoryBalance != 0 ? subTotalInventoryBalance < 0 ? $"({Math.Abs(subTotalInventoryBalance).ToString(SD.Two_Decimal_Format)})" : subTotalInventoryBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalInventoryBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalAverageCost != 0 ? subTotalAverageCost < 0 ? $"({Math.Abs(subTotalAverageCost).ToString(SD.Four_Decimal_Format)})" : subTotalAverageCost.ToString(SD.Four_Decimal_Format) : null).FontColor(subTotalAverageCost < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalTotalBalance != 0 ? subTotalTotalBalance < 0 ? $"({Math.Abs(subTotalTotalBalance).ToString(SD.Two_Decimal_Format)})" : subTotalTotalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotalTotalBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                    grandTotalInventoryBalance += subTotalInventoryBalance;
                                    grandTotalTotalBalance += subTotalTotalBalance;
                                    productTotalInventoryBalance += subTotalInventoryBalance;
                                    productTotalTotalBalance += subTotalTotalBalance;

                                    var isLastGroupForProduct = groupIndex == inventories.Count - 1 ||
                                        !string.Equals(
                                            inventories[groupIndex + 1].First().Product.ProductName,
                                            currentProductName,
                                            StringComparison.Ordinal);

                                    if (showProductSections && isLastGroupForProduct)
                                    {
                                        var productAverageCost = productTotalInventoryBalance != 0
                                            ? productTotalTotalBalance / productTotalInventoryBalance
                                            : 0m;

                                        table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten2).Border(0.5f);
                                        table.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(3).Text($"Product Total - {currentProductName}").SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(3).AlignRight().Text(productTotalInventoryBalance != 0 ? productTotalInventoryBalance < 0 ? $"({Math.Abs(productTotalInventoryBalance).ToString(SD.Two_Decimal_Format)})" : productTotalInventoryBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(productTotalInventoryBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(3).AlignRight().Text(productAverageCost != 0 ? productAverageCost < 0 ? $"({Math.Abs(productAverageCost).ToString(SD.Four_Decimal_Format)})" : productAverageCost.ToString(SD.Four_Decimal_Format) : null).FontColor(productAverageCost < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten2).Border(0.5f).Padding(3).AlignRight().Text(productTotalTotalBalance != 0 ? productTotalTotalBalance < 0 ? $"({Math.Abs(productTotalTotalBalance).ToString(SD.Two_Decimal_Format)})" : productTotalTotalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(productTotalTotalBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                        productTotalInventoryBalance = 0m;
                                        productTotalTotalBalance = 0m;
                                    }
                                }

                            var grandTotalAverageCost = grandTotalTotalBalance != 0
                                ? grandTotalTotalBalance / grandTotalInventoryBalance
                                : 0m;
                            table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten1).Border(0.5f);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Grand Total").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalInventoryBalance != 0 ? grandTotalInventoryBalance < 0 ? $"({Math.Abs(grandTotalInventoryBalance).ToString(SD.Two_Decimal_Format)})" : grandTotalInventoryBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(grandTotalInventoryBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalAverageCost != 0 ? grandTotalAverageCost < 0 ? $"({Math.Abs(grandTotalAverageCost).ToString(SD.Four_Decimal_Format)})" : grandTotalAverageCost.ToString(SD.Four_Decimal_Format) : null).FontColor(grandTotalAverageCost < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalTotalBalance != 0 ? grandTotalTotalBalance < 0 ? $"({Math.Abs(grandTotalTotalBalance).ToString(SD.Two_Decimal_Format)})" : grandTotalTotalBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(grandTotalTotalBalance < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate inventory report quest pdf", "Inventory Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate inventory report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(InventoryReport));
            }
        }

        [HttpPost]
        public async Task<IActionResult> DisplayInventoryReportExcel(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(InventoryReport));
            }

            try
            {
                var inventoryRecords = await _dbContext.FilprideInventories
                    .AsNoTracking()
                    .Include(i => i.Product)
                    .Include(i => i.PurchaseOrder)
                    .Where(i =>
                        i.Date >= viewModel.DateTo &&
                        i.Date <= viewModel.DateTo.AddMonths(1).AddDays(-1) &&
                        i.Company == companyClaims &&
                        (viewModel.ProductId == null || i.ProductId == viewModel.ProductId) &&
                        (viewModel.POId == null || i.POId == viewModel.POId))
                    .OrderBy(i => i.Product.ProductName)
                    .ThenBy(i => i.POId)
                    .ThenBy(i => i.Date)
                    .ToListAsync(cancellationToken);

                if (inventoryRecords.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(InventoryReport));
                }

                var inventories = inventoryRecords
                    .GroupBy(x => new { x.ProductId, x.POId })
                    .OrderBy(g => g.First().Product.ProductName)
                    .ThenBy(g => g.Key.POId)
                    .ToList();

                var showProductSections = viewModel.ProductId == null;
                var productName = viewModel.ProductId == null
                    ? "ALL PRODUCTS"
                    : inventoryRecords.First().Product.ProductName;

                // Create Excel package
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Inventory Report");

                // Set up the header section
                worksheet.Cells["A1:P1"].Merge = true;
                worksheet.Cells["A1"].Value = "INVENTORY REPORT";
                worksheet.Cells["A1"].Style.Font.Size = 20;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A2:P2"].Merge = true;
                worksheet.Cells["A2"].Value = $"As of {viewModel.DateTo:MMMM yyyy}";
                worksheet.Cells["A2"].Style.Font.Size = 12;
                worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A3:P3"].Merge = true;
                worksheet.Cells["A3"].Value = $"Product Name: {productName}";
                worksheet.Cells["A3"].Style.Font.Size = 14;
                worksheet.Cells["A3"].Style.Font.Bold = true;
                worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A4:P4"].Merge = true;
                worksheet.Cells["A4"].Value = $"Date and Time Generated: {DateTimeHelper.GetCurrentPhilippineTime()}";
                worksheet.Cells["A4"].Style.Font.Size = 12;
                worksheet.Cells["A4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Add some spacing
                int currentRow = 6;
                string currencyTwoDecimalFormat = "#,##0.00_);[Red](#,##0.00)";
                string currencyFourDecimalFormat = "#,##0.0000_);[Red](#,##0.0000)";

                var headerGroups = new (string Range, string Title)[]
                {
                    ("E5:G5", "Beginning Balance"),
                    ("H5:J5", "Purchases"),
                    ("K5:M5", "Sales"),
                    ("N5:P5", "Inventory Balance"),
                };

                foreach (var (rangeAddress, title) in headerGroups)
                {
                    using var range = worksheet.Cells[rangeAddress];
                    range.Merge = true;
                    range.Value = title;
                    range.Style.Font.Size = 14;
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Set up table headers
                var headers = new[]
                {
                    "Date",
                    "Particular",
                    "PO No.",
                    "Reference",
                    "Quantity",
                    "Cost",
                    "Total",
                    "Quantity",
                    "Cost",
                    "Total",
                    "Quantity",
                    "Cost",
                    "Total",
                    "Inventory Balance",
                    "Unit Cost Average",
                    "Total Balance"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[currentRow, i + 1].Value = headers[i];
                    worksheet.Cells[currentRow, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[currentRow, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[currentRow, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[currentRow, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                currentRow++;

                var grandTotalPurchasesQty = 0m;
                var grandTotalPurchasesAmt = 0m;
                var grandTotalSalesQty = 0m;
                var grandTotalSalesAmt = 0m;
                var grandTotalBegbalQty = 0m;
                var grandTotalBegbalAmt = 0m;
                var grandTotalInventoryBalance = 0m;
                var grandTotalTotalBalance = 0m;
                string? previousProductName = null;
                var productTotalPurchasesQty = 0m;
                var productTotalPurchasesAmt = 0m;
                var productTotalSalesQty = 0m;
                var productTotalSalesAmt = 0m;
                var productTotalBegBalQty = 0m;
                var productTotalBegBalAmt = 0m;
                var productTotalInventoryBalance = 0m;
                var productTotalTotalBalance = 0m;

                // Loop through inventory groups
                for (var groupIndex = 0; groupIndex < inventories.Count; groupIndex++)
                {
                    var group = inventories[groupIndex];
                    var currentProductName = group.First().Product.ProductName;

                    if (showProductSections && !string.Equals(previousProductName, currentProductName, StringComparison.Ordinal))
                    {
                        worksheet.Cells[currentRow, 1, currentRow, 16].Merge = true;
                        worksheet.Cells[currentRow, 1].Value = $"PRODUCT: {currentProductName}";
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Font.Bold = true;
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gainsboro);
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        currentRow++;
                        previousProductName = currentProductName;
                    }

                    var subTotalPurchasesQty = 0m;
                    var subTotalPurchasesAmt = 0m;
                    var subTotalSalesQty = 0m;
                    var subTotalSalesAmt = 0m;
                    var subTotalBegBalQty = 0m;
                    var subTotalBegBalAmt = 0m;
                    var subTotalInventoryBalance = 0m;
                    var subTotalTotalBalance = 0m;
                    var orderedGroup = group
                        .OrderBy(e => e.Date)
                        .ThenBy(x => x.Particular == "Purchases" ? 0 : 1)
                        .ThenBy(x => x.InventoryId)
                        .ToList();
                    var firstEntry = orderedGroup.FirstOrDefault();

                    if (firstEntry != null)
                    {
                        var isSales = firstEntry.Particular.Contains("sales", StringComparison.InvariantCultureIgnoreCase);
                        subTotalBegBalQty += !isSales
                            ? firstEntry.InventoryBalance - firstEntry.Quantity
                            : firstEntry.InventoryBalance + firstEntry.Quantity;
                        subTotalBegBalAmt += !isSales
                            ? firstEntry.TotalBalance - firstEntry.Total
                            : firstEntry.TotalBalance + firstEntry.Total;
                        var beginningAverageCost = firstEntry.AverageCost;

                        worksheet.Cells[currentRow, 1].Value = "BEGINNING BALANCE";
                        worksheet.Cells[currentRow, 5].Value = subTotalBegBalQty;
                        worksheet.Cells[currentRow, 5].Style.Numberformat.Format = currencyTwoDecimalFormat;
                        worksheet.Cells[currentRow, 6].Value = beginningAverageCost;
                        worksheet.Cells[currentRow, 6].Style.Numberformat.Format = currencyFourDecimalFormat;
                        worksheet.Cells[currentRow, 7].Value = subTotalBegBalAmt;
                        worksheet.Cells[currentRow, 7].Style.Numberformat.Format = currencyTwoDecimalFormat;
                        worksheet.Cells[currentRow, 5, currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Font.Bold = true;
                        worksheet.Cells[currentRow, 1, currentRow, 16].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        subTotalInventoryBalance += subTotalBegBalQty;
                        subTotalTotalBalance += subTotalBegBalAmt;

                        currentRow++;
                    }

                    foreach (var record in orderedGroup)
                    {
                        // Date
                        worksheet.Cells[currentRow, 1].Value = record.Date.ToString(SD.Date_Format);
                        worksheet.Cells[currentRow, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Particular
                        worksheet.Cells[currentRow, 2].Value = record.Particular;
                        worksheet.Cells[currentRow, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // PO No.
                        worksheet.Cells[currentRow, 3].Value = record.PurchaseOrder?.PurchaseOrderNo;
                        worksheet.Cells[currentRow, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Reference
                        worksheet.Cells[currentRow, 4].Value = record.Reference;
                        worksheet.Cells[currentRow, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        for (var col = 5; col <= 16; col++)
                        {
                            worksheet.Cells[currentRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        if (record.Particular == "Purchases")
                        {
                            // Purchases Qty
                            worksheet.Cells[currentRow, 8].Value = record.Quantity != 0 ? record.Quantity : 0;
                            worksheet.Cells[currentRow, 8].Style.Numberformat.Format = currencyTwoDecimalFormat;
                            subTotalPurchasesQty += record.Quantity;

                            // Purchases Cost
                            worksheet.Cells[currentRow, 9].Value = record.Cost != 0 ? record.Cost : 0;
                            worksheet.Cells[currentRow, 9].Style.Numberformat.Format = currencyFourDecimalFormat;

                            // Purchases Amt
                            worksheet.Cells[currentRow, 10].Value = record.Total != 0 ? record.Total : 0;
                            worksheet.Cells[currentRow, 10].Style.Numberformat.Format = currencyTwoDecimalFormat;
                            subTotalPurchasesAmt += record.Total;
                        }

                        worksheet.Cells[currentRow, 8, currentRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        if (record.Particular == "Sales")
                        {
                            // Sales Qty
                            worksheet.Cells[currentRow, 11].Value = record.Quantity != 0 ? record.Quantity : 0;
                            worksheet.Cells[currentRow, 11].Style.Numberformat.Format = currencyTwoDecimalFormat;
                            subTotalSalesQty += record.Quantity;

                            // Sales Cost
                            worksheet.Cells[currentRow, 12].Value = record.Cost != 0 ? record.Cost : 0;
                            worksheet.Cells[currentRow, 12].Style.Numberformat.Format = currencyFourDecimalFormat;

                            // Sales Amt
                            worksheet.Cells[currentRow, 13].Value = record.Total != 0 ? record.Total : 0;
                            worksheet.Cells[currentRow, 13].Style.Numberformat.Format = currencyTwoDecimalFormat;
                            subTotalSalesAmt += record.Total;

                        }

                        worksheet.Cells[currentRow, 11, currentRow, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        // Inventory Balance
                        worksheet.Cells[currentRow, 14].Value = record.InventoryBalance;
                        worksheet.Cells[currentRow, 14].Style.Numberformat.Format = currencyTwoDecimalFormat;

                        // Unit Cost Average
                        worksheet.Cells[currentRow, 15].Value = record.AverageCost;
                        worksheet.Cells[currentRow, 15].Style.Numberformat.Format = currencyFourDecimalFormat;

                        // Total Balance
                        worksheet.Cells[currentRow, 16].Value = record.TotalBalance;
                        worksheet.Cells[currentRow, 16].Style.Numberformat.Format = currencyTwoDecimalFormat;

                        worksheet.Cells[currentRow, 14, currentRow, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        subTotalInventoryBalance = record.InventoryBalance;
                        subTotalTotalBalance = record.TotalBalance;

                        currentRow++;
                    }

                    // Add subtotal row
                    worksheet.Cells[currentRow, 1, currentRow, 3].Merge = true;
                    worksheet.Cells[currentRow, 4].Value = "Sub Total";
                    worksheet.Cells[currentRow, 4].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Subtotal Columns
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 5], subTotalBegBalQty, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 6], subTotalBegBalQty != 0
                        ? subTotalBegBalAmt / subTotalBegBalQty
                        : 0, currencyFourDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 7], subTotalBegBalAmt, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 8], subTotalPurchasesQty, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 9], subTotalPurchasesQty != 0
                        ? subTotalPurchasesAmt / subTotalPurchasesQty
                        : 0, currencyFourDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 10], subTotalPurchasesAmt, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 11], subTotalSalesQty, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 12], subTotalSalesQty != 0
                        ? subTotalSalesAmt / subTotalSalesQty
                        : 0, currencyFourDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 13], subTotalSalesAmt, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 14], subTotalInventoryBalance, currencyTwoDecimalFormat);
                    ApplySubtotalStyle(
                        worksheet.Cells[currentRow, 15],
                        orderedGroup.LastOrDefault()?.AverageCost ?? 0m,
                        currencyFourDecimalFormat);
                    ApplySubtotalStyle(worksheet.Cells[currentRow, 16], subTotalTotalBalance, currencyTwoDecimalFormat);

                    // Apply borders to subtotal row
                    for (int i = 1; i <= 16; i++)
                    {
                        worksheet.Cells[currentRow, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    // Update grand totals
                    grandTotalPurchasesAmt += subTotalPurchasesAmt;
                    grandTotalPurchasesQty += subTotalPurchasesQty;
                    grandTotalSalesAmt += subTotalSalesAmt;
                    grandTotalSalesQty += subTotalSalesQty;
                    grandTotalBegbalAmt += subTotalBegBalAmt;
                    grandTotalBegbalQty += subTotalBegBalQty;
                    grandTotalInventoryBalance += subTotalInventoryBalance;
                    grandTotalTotalBalance += subTotalTotalBalance;
                    productTotalPurchasesAmt += subTotalPurchasesAmt;
                    productTotalPurchasesQty += subTotalPurchasesQty;
                    productTotalSalesAmt += subTotalSalesAmt;
                    productTotalSalesQty += subTotalSalesQty;
                    productTotalBegBalAmt += subTotalBegBalAmt;
                    productTotalBegBalQty += subTotalBegBalQty;
                    productTotalInventoryBalance += subTotalInventoryBalance;
                    productTotalTotalBalance += subTotalTotalBalance;

                    currentRow++;

                    var isLastGroupForProduct = groupIndex == inventories.Count - 1 ||
                        !string.Equals(
                            inventories[groupIndex + 1].First().Product.ProductName,
                            currentProductName,
                            StringComparison.Ordinal);

                    if (showProductSections && isLastGroupForProduct)
                    {
                        var productTotalAverageCost = productTotalInventoryBalance != 0
                            ? productTotalTotalBalance / productTotalInventoryBalance
                            : 0m;
                        var productTotalPurchasesAverageCost = productTotalPurchasesQty != 0
                            ? productTotalPurchasesAmt / productTotalPurchasesQty
                            : 0m;
                        var productTotalSalesAverageCost = productTotalSalesQty != 0
                            ? productTotalSalesAmt / productTotalSalesQty
                            : 0m;
                        var productTotalBegBalAverageCost = productTotalBegBalQty != 0
                            ? productTotalBegBalAmt / productTotalBegBalQty
                            : 0m;

                        worksheet.Cells[currentRow, 1, currentRow, 3].Merge = true;
                        worksheet.Cells[currentRow, 4].Value = $"Product Total - {currentProductName}";
                        worksheet.Cells[currentRow, 4].Style.Font.Bold = true;
                        worksheet.Cells[currentRow, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[currentRow, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gainsboro);

                        ApplySubtotalStyle(worksheet.Cells[currentRow, 5], productTotalBegBalQty, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 6], productTotalBegBalAverageCost, currencyFourDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 7], productTotalBegBalAmt, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 8], productTotalPurchasesQty, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 9], productTotalPurchasesAverageCost, currencyFourDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 10], productTotalPurchasesAmt, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 11], productTotalSalesQty, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 12], productTotalSalesAverageCost, currencyFourDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 13], productTotalSalesAmt, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 14], productTotalInventoryBalance, currencyTwoDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 15], productTotalAverageCost, currencyFourDecimalFormat);
                        ApplySubtotalStyle(worksheet.Cells[currentRow, 16], productTotalTotalBalance, currencyTwoDecimalFormat);

                        for (int i = 1; i <= 16; i++)
                        {
                            worksheet.Cells[currentRow, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            worksheet.Cells[currentRow, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[currentRow, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Gainsboro);
                        }

                        currentRow += 2;
                        productTotalPurchasesQty = 0m;
                        productTotalPurchasesAmt = 0m;
                        productTotalSalesQty = 0m;
                        productTotalSalesAmt = 0m;
                        productTotalBegBalQty = 0m;
                        productTotalBegBalAmt = 0m;
                        productTotalInventoryBalance = 0m;
                        productTotalTotalBalance = 0m;
                    }
                }

                // Calculate averages
                var grandTotalAverageCost = grandTotalTotalBalance != 0
                    ? grandTotalTotalBalance / grandTotalInventoryBalance
                    : 0m;
                var grandTotalPurchasesAverageCost = grandTotalPurchasesQty != 0
                    ? grandTotalPurchasesAmt / grandTotalPurchasesQty
                    : 0m;
                var grandTotalSalesAverageCost = grandTotalSalesQty != 0
                    ? grandTotalSalesAmt / grandTotalSalesQty
                    : 0m;
                var grandTotalBegbalAverageCost = grandTotalBegbalQty != 0
                    ? grandTotalBegbalAmt / grandTotalBegbalQty
                    : 0m;

                // Title cell
                worksheet.Cells[currentRow, 1, currentRow, 3].Merge = true;
                worksheet.Cells[currentRow, 4].Value = "Grand Total";
                worksheet.Cells[currentRow, 4].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Columns (no loop, just direct calls)
                ApplySubtotalStyle(worksheet.Cells[currentRow, 5], grandTotalBegbalQty, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 6], grandTotalBegbalAverageCost, currencyFourDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 7], grandTotalBegbalAmt, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 8], grandTotalPurchasesQty, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 9], grandTotalPurchasesAverageCost, currencyFourDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 10], grandTotalPurchasesAmt, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 11], grandTotalSalesQty, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 12], grandTotalSalesAverageCost, currencyFourDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 13], grandTotalSalesAmt, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 14], grandTotalInventoryBalance, currencyTwoDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 15], grandTotalAverageCost, currencyFourDecimalFormat);
                ApplySubtotalStyle(worksheet.Cells[currentRow, 16], grandTotalTotalBalance, currencyTwoDecimalFormat);

                // Apply borders to grand total row
                for (int i = 1; i <= 16; i++)
                {
                    worksheet.Cells[currentRow, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.View.FreezePanes(7, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate inventory report excel", "Inventory Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                // Generate Excel file
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                var sanitizedProductName = productName.Replace(" ", "_");
                var fileName = $"Inventory_Report_{viewModel.DateTo:yyyyMM}_{sanitizedProductName}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate inventory report excel. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(InventoryReport));
            }
        }

        private void ApplySubtotalStyle(ExcelRange cell, object? value, string? numberFormat = null)
        {
            if (value != null)
            {
                cell.Value = value;
            }

            if (!string.IsNullOrEmpty(numberFormat))
            {
                cell.Style.Numberformat.Format = numberFormat;
            }

            cell.Style.Font.Bold = true;
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        [HttpGet]
        public async Task<JsonResult> GetPOsByProduct(int? productId, CancellationToken cancellationToken)
        {
            if (productId == null)
            {
                return Json(Array.Empty<SelectListItem>());
            }

            var companyClaims = await GetCompanyClaimAsync();
            var purchaseOrders = await _dbContext.FilpridePurchaseOrders
                .OrderBy(p => p.PurchaseOrderNo)
                .Where(p => p.Company == companyClaims && p.ProductId == productId)
                .Select(p => new SelectListItem
                {
                    Value = p.PurchaseOrderId.ToString(),
                    Text = p.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return Json(purchaseOrders);
        }
    }
}
