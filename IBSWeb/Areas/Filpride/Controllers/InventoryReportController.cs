using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

                var product = await _dbContext.Products
                    .FindAsync(viewModel.ProductId, cancellationToken);

                if (!inventories.Any())
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
                                var grandTotalAverageCost = 0m;
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

                                grandTotalAverageCost = grandTotalInventoryBalance != 0 && grandTotalTotalBalance != 0 ? grandTotalTotalBalance / grandTotalInventoryBalance : 0m;

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

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate inventory report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
