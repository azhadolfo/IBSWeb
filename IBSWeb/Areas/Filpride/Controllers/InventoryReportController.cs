using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
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

        private readonly ILogger<InventoryController> _logger;

        public InventoryReportController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<InventoryController> logger)
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
                var inventories = await _dbContext.FilprideInventories
                    .OrderBy(i => i.POId)
                    .Where(i => i.Date >= viewModel.DateTo
                                && i.Date <= viewModel.DateTo.AddMonths(1).AddDays(-1)
                                && i.Company == companyClaims
                                && i.ProductId == viewModel.ProductId
                                && (viewModel.POId == null || i.POId == viewModel.POId))
                    .GroupBy(x => x.POId)
                    .ToListAsync(cancellationToken);

                var product = await _unitOfWork.Product
                    .GetAsync(x => x.ProductId == viewModel.ProductId, cancellationToken);

                if (inventories.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(InventoryReport));
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
                                    text.Span(product!.ProductName).FontSize(16);
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

                                foreach (var group in inventories)
                                {
                                    var subTotalInventoryBalance = 0m;
                                    var subTotalAverageCost = 0m;
                                    var subTotalTotalBalance = 0m;

                                    foreach (var record in group.OrderBy(e => e.Date)
                                                 .ThenBy(x => x.Particular))
                                    {
                                        var getPurchaseOrder  =
                                        _unitOfWork.FilpridePurchaseOrder.GetAsync(x =>
                                            x.PurchaseOrderId == record.POId, cancellationToken);

                                        table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(record.Particular);
                                        table.Cell().Border(0.5f).Padding(3).Text(getPurchaseOrder.Result?.PurchaseOrderNo);
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
                                }

                                var grandTotalAverageCost = grandTotalInventoryBalance != 0 && grandTotalTotalBalance != 0 ? grandTotalTotalBalance / grandTotalInventoryBalance : 0m;

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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate inventory report quest pdf", "Inventory Report", companyClaims);
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
                var inventories = await _dbContext.FilprideInventories
                    .OrderBy(i => i.POId)
                    .Where(i => i.Date >= viewModel.DateTo
                                && i.Date <= viewModel.DateTo.AddMonths(1).AddDays(-1)
                                && i.Company == companyClaims
                                && i.ProductId == viewModel.ProductId
                                && (viewModel.POId == null || i.POId == viewModel.POId))
                    .GroupBy(x => x.POId)
                    .ToListAsync(cancellationToken);

                var product = await _unitOfWork.Product
                    .GetAsync(x => x.ProductId == viewModel.ProductId, cancellationToken);

                if (inventories.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(InventoryReport));
                }

                // Create Excel package
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Inventory Report");

                // Set up the header section
                worksheet.Cells["A1:M1"].Merge = true;
                worksheet.Cells["A1"].Value = "INVENTORY REPORT";
                worksheet.Cells["A1"].Style.Font.Size = 20;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A2:M2"].Merge = true;
                worksheet.Cells["A2"].Value = $"As of {viewModel.DateTo:MMMM yyyy}";
                worksheet.Cells["A2"].Style.Font.Size = 12;
                worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A3:M3"].Merge = true;
                worksheet.Cells["A3"].Value = $"Product Name: {product!.ProductName}";
                worksheet.Cells["A3"].Style.Font.Size = 14;
                worksheet.Cells["A3"].Style.Font.Bold = true;
                worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Add some spacing
                int currentRow = 6;
                string currencyTwoDecimalFormat = "#,##0.00_);[Red](#,##0.00)";
                string currencyFourDecimalFormat = "#,##0.0000_);[Red](#,##0.0000)";

                using (var range = worksheet.Cells["E5:G5"])
                {
                    range.Merge = true;
                    range.Value = "Purchases";
                    range.Style.Font.Size = 14;
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                using (var range = worksheet.Cells["H5:J5"])
                {
                    range.Merge = true;
                    range.Value = "Sales";
                    range.Style.Font.Size = 14;
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                using (var range = worksheet.Cells["K5:M5"])
                {
                    range.Merge = true;
                    range.Value = "Inventory Balance";
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
                    "Date", "Particular", "PO No.", "Reference", "Quantity", "Cost", "Total", "Quantity",
                    "Cost", "Total", "Inventory Balance", "Unit Cost Average", "Total Balance"
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

                currentRow += 1;

                var grandTotalPurchasesQty = 0m;
                var grandTotalPurchasesAmt = 0m;
                var grandTotalSalesQty = 0m;
                var grandTotalSalesAmt = 0m;
                var grandTotalInventoryBalance = 0m;
                var grandTotalTotalBalance = 0m;

                // Loop through inventory groups
                foreach (var group in inventories)
                {
                    var subTotalPurchasesQty = 0m;
                    var subTotalPurchasesAmt = 0m;
                    var subTotalSalesQty = 0m;
                    var subTotalSalesAmt = 0m;
                    var subTotalInventoryBalance = 0m;
                    var subTotalAverageCost = 0m;
                    var subTotalTotalBalance = 0m;

                    foreach (var record in group.OrderBy(e => e.Date).ThenBy(x => x.Particular))
                    {
                        var getPurchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                            .GetAsync(x => x.PurchaseOrderId == record.POId, cancellationToken);

                        // Date
                        worksheet.Cells[currentRow, 1].Value = record.Date.ToString(SD.Date_Format);
                        worksheet.Cells[currentRow, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Particular
                        worksheet.Cells[currentRow, 2].Value = record.Particular;
                        worksheet.Cells[currentRow, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // PO No.
                        worksheet.Cells[currentRow, 3].Value = getPurchaseOrder?.PurchaseOrderNo;
                        worksheet.Cells[currentRow, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Reference
                        worksheet.Cells[currentRow, 4].Value = record.Reference;
                        worksheet.Cells[currentRow, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        if (record.Particular == "Purchases")
                        {
                            // Purchases Qty
                            if (record.Quantity != 0)
                            {
                                worksheet.Cells[currentRow, 5].Value = record.Quantity;
                                worksheet.Cells[currentRow, 5].Style.Numberformat.Format = currencyTwoDecimalFormat;
                                subTotalPurchasesQty += record.Quantity;
                            }

                            // Purchases Cost
                            if (record.Cost != 0)
                            {
                                worksheet.Cells[currentRow, 6].Value = record.Cost;
                                worksheet.Cells[currentRow, 6].Style.Numberformat.Format = currencyFourDecimalFormat;
                            }

                            // Purchases Amt
                            if (record.Total != 0)
                            {
                                worksheet.Cells[currentRow, 7].Value = record.Total;
                                worksheet.Cells[currentRow, 7].Style.Numberformat.Format = currencyTwoDecimalFormat;
                                subTotalPurchasesAmt += record.Total;
                            }
                        }
                        worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        if (record.Particular == "Sales")
                        {
                            // Sales Qty
                            if (record.Quantity != 0)
                            {
                                worksheet.Cells[currentRow, 8].Value = record.Quantity;
                                worksheet.Cells[currentRow, 8].Style.Numberformat.Format = currencyTwoDecimalFormat;
                                subTotalSalesQty += record.Quantity;
                            }

                            // Sales Cost
                            if (record.Cost != 0)
                            {
                                worksheet.Cells[currentRow, 9].Value = record.Cost;
                                worksheet.Cells[currentRow, 9].Style.Numberformat.Format = currencyFourDecimalFormat;
                            }

                            // Sales Amt
                            if (record.Total != 0)
                            {
                                worksheet.Cells[currentRow, 10].Value = record.Total;
                                worksheet.Cells[currentRow, 10].Style.Numberformat.Format = currencyTwoDecimalFormat;
                                subTotalSalesAmt += record.Total;
                            }
                        }
                        worksheet.Cells[currentRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[currentRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[currentRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Inventory Balance
                        if (record.InventoryBalance != 0)
                        {
                            worksheet.Cells[currentRow, 11].Value = record.InventoryBalance;
                            worksheet.Cells[currentRow, 11].Style.Numberformat.Format = currencyTwoDecimalFormat;
                        }
                        worksheet.Cells[currentRow, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Unit Cost Average
                        if (record.AverageCost != 0)
                        {
                            worksheet.Cells[currentRow, 12].Value = record.AverageCost;
                            worksheet.Cells[currentRow, 12].Style.Numberformat.Format = currencyFourDecimalFormat;
                        }
                        worksheet.Cells[currentRow, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        // Total Balance
                        if (record.TotalBalance != 0)
                        {
                            worksheet.Cells[currentRow, 13].Value = record.TotalBalance;
                            worksheet.Cells[currentRow, 13].Style.Numberformat.Format = currencyTwoDecimalFormat;
                        }

                        worksheet.Cells[currentRow, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[currentRow, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                        subTotalInventoryBalance += record.InventoryBalance;
                        subTotalAverageCost += record.AverageCost;
                        subTotalTotalBalance += record.TotalBalance;

                        currentRow++;
                    }

                    // Add subtotal row
                    worksheet.Cells[currentRow, 1, currentRow, 3].Merge = true;
                    worksheet.Cells[currentRow, 4].Value = "Sub Total";
                    worksheet.Cells[currentRow, 4].Style.Font.Bold = true;
                                        worksheet.Cells[currentRow, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Purchases Qty
                    if (subTotalPurchasesQty != 0)
                    {
                        worksheet.Cells[currentRow, 5].Value = subTotalPurchasesQty;
                        worksheet.Cells[currentRow, 5].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 5].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Purchases Cost
                    if (subTotalPurchasesQty != 0)
                    {
                        worksheet.Cells[currentRow, 6].Value = (subTotalPurchasesAmt/subTotalPurchasesQty);
                        worksheet.Cells[currentRow, 6].Style.Numberformat.Format = currencyFourDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 6].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Purchases Amt
                    if (subTotalPurchasesAmt != 0)
                    {
                        worksheet.Cells[currentRow, 7].Value = subTotalPurchasesAmt;
                        worksheet.Cells[currentRow, 7].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 7].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Sales Qty
                    if (subTotalSalesQty != 0)
                    {
                        worksheet.Cells[currentRow, 8].Value = subTotalSalesQty;
                        worksheet.Cells[currentRow, 8].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 8].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 8].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Sales Cost
                    if (subTotalSalesQty != 0)
                    {
                        worksheet.Cells[currentRow, 9].Value = (subTotalPurchasesAmt/subTotalSalesQty);
                        worksheet.Cells[currentRow, 9].Style.Numberformat.Format = currencyFourDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 9].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Sales Amt
                    if (subTotalSalesAmt != 0)
                    {
                        worksheet.Cells[currentRow, 10].Value = subTotalSalesAmt;
                        worksheet.Cells[currentRow, 10].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 10].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 10].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Inventory Balance
                    if (subTotalInventoryBalance != 0)
                    {
                        worksheet.Cells[currentRow, 11].Value = subTotalInventoryBalance;
                        worksheet.Cells[currentRow, 11].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 11].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 11].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Average Cost
                    if (subTotalAverageCost != 0)
                    {
                        worksheet.Cells[currentRow, 12].Value = subTotalAverageCost;
                        worksheet.Cells[currentRow, 12].Style.Numberformat.Format = currencyFourDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 12].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 12].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Sub Total Total Balance
                    if (subTotalTotalBalance != 0)
                    {
                        worksheet.Cells[currentRow, 13].Value = subTotalTotalBalance;
                        worksheet.Cells[currentRow, 13].Style.Numberformat.Format = currencyTwoDecimalFormat;
                    }
                    worksheet.Cells[currentRow, 13].Style.Font.Bold = true;
                    worksheet.Cells[currentRow, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                        worksheet.Cells[currentRow, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[currentRow, 13].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                    // Apply borders to subtotal row
                    for (int i = 1; i <= 13; i++)
                    {
                        worksheet.Cells[currentRow, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    grandTotalInventoryBalance += subTotalInventoryBalance; // Qty
                    grandTotalTotalBalance += subTotalTotalBalance; // Amt
                    grandTotalPurchasesAmt += subTotalPurchasesAmt;
                    grandTotalPurchasesQty += subTotalPurchasesQty;
                    grandTotalSalesAmt += subTotalSalesAmt;
                    grandTotalSalesQty += subTotalSalesQty;

                    currentRow++;
                }

                // Add grand total row
                var grandTotalAverageCost = grandTotalInventoryBalance != 0 && grandTotalTotalBalance != 0
                    ? grandTotalTotalBalance / grandTotalInventoryBalance : 0m;
                var grandTotalPurchasesAverageCost = grandTotalPurchasesQty != 0 && grandTotalPurchasesAmt != 0
                    ? grandTotalPurchasesAmt / grandTotalPurchasesQty : 0m;
                var grandTotalSalesAverageCost = grandTotalSalesQty != 0 && grandTotalSalesAmt != 0
                    ? grandTotalSalesAmt / grandTotalSalesQty : 0m;

                worksheet.Cells[currentRow, 1, currentRow, 3].Merge = true;
                worksheet.Cells[currentRow, 4].Value = "Grand Total";
                worksheet.Cells[currentRow, 4].Style.Font.Bold = true;
                                worksheet.Cells[currentRow, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Purchases Qty
                if (grandTotalPurchasesQty != 0)
                {
                    worksheet.Cells[currentRow, 5].Value = grandTotalPurchasesQty;
                    worksheet.Cells[currentRow, 5].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 5].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Purchases Average Cost
                if (grandTotalPurchasesAverageCost != 0)
                {
                    worksheet.Cells[currentRow, 6].Value = grandTotalPurchasesAverageCost;
                    worksheet.Cells[currentRow, 6].Style.Numberformat.Format = currencyFourDecimalFormat;
                }
                worksheet.Cells[currentRow, 6].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Purchases Amt
                if (grandTotalPurchasesAmt != 0)
                {
                    worksheet.Cells[currentRow, 7].Value = grandTotalPurchasesAmt;
                    worksheet.Cells[currentRow, 7].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 7].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);


                // Grand Total Sales Qty
                if (grandTotalPurchasesQty != 0)
                {
                    worksheet.Cells[currentRow, 8].Value = grandTotalSalesQty;
                    worksheet.Cells[currentRow, 8].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 8].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 8].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Sales Average Cost
                if (grandTotalSalesAverageCost != 0)
                {
                    worksheet.Cells[currentRow, 9].Value = grandTotalSalesAverageCost;
                    worksheet.Cells[currentRow, 9].Style.Numberformat.Format = currencyFourDecimalFormat;
                }
                worksheet.Cells[currentRow, 9].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Sales Amt
                if (grandTotalSalesAmt != 0)
                {
                    worksheet.Cells[currentRow, 10].Value = grandTotalSalesAmt;
                    worksheet.Cells[currentRow, 10].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 10].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 10].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Inventory Balance
                if (grandTotalInventoryBalance != 0)
                {
                    worksheet.Cells[currentRow, 11].Value = grandTotalInventoryBalance;
                    worksheet.Cells[currentRow, 11].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 11].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 11].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 11].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Average Cost
                if (grandTotalAverageCost != 0)
                {
                    worksheet.Cells[currentRow, 12].Value = grandTotalAverageCost;
                    worksheet.Cells[currentRow, 12].Style.Numberformat.Format = currencyFourDecimalFormat;
                }
                worksheet.Cells[currentRow, 12].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 12].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Grand Total Total Balance
                if (grandTotalTotalBalance != 0)
                {
                    worksheet.Cells[currentRow, 13].Value = grandTotalTotalBalance;
                    worksheet.Cells[currentRow, 13].Style.Numberformat.Format = currencyTwoDecimalFormat;
                }
                worksheet.Cells[currentRow, 13].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[currentRow, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[currentRow, 13].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                // Apply borders to grand total row
                for (int i = 1; i <= 13; i++)
                {
                    worksheet.Cells[currentRow, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                worksheet.View.FreezePanes(7, 1);

                // Set minimum column widths
                for (int i = 1; i <= 13; i++)
                {
                    if (worksheet.Column(i).Width < 12)
                        worksheet.Column(i).Width = 12;
                }

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate inventory report excel", "Inventory Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

                // Generate Excel file
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                var fileName = $"Inventory_Report_{viewModel.DateTo:yyyyMM}_{product.ProductName.Replace(" ", "_")}.xlsx";

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

        [HttpGet]
        public async Task<JsonResult> GetPOsByProduct(int? productId, CancellationToken cancellationToken)
        {
            if (productId == null)
            {
                return Json(null);
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
