using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;
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
    public class ComparativeReportController : Controller
    {
        private readonly ILogger<ComparativeReportController> _logger;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<ApplicationUser> _userManager;

        public ComparativeReportController(ILogger<ComparativeReportController> logger,
            ApplicationDbContext dbContext, IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
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
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateOnly monthDate, string category, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            try
            {
                var dateFrom = monthDate.ToDateTime(TimeOnly.MinValue);
                var dateTo = monthDate.AddMonths(1).ToDateTime(TimeOnly.MinValue);
                var isCombinedReport = string.Equals(category, "All", StringComparison.OrdinalIgnoreCase);

                var adjustmentsQuery = _dbContext.LockedPeriodAdjustments
                    .AsNoTracking()
                    .Where(a => a.CreatedDate >= dateFrom
                                && a.CreatedDate < dateTo);

                if (!isCombinedReport)
                {
                    adjustmentsQuery = category switch
                    {
                        "Sales" => adjustmentsQuery.Where(a =>
                            a.AdjustmentType == LockedPeriodAdjustmentType.SellingPrice ||
                            a.AdjustmentType == LockedPeriodAdjustmentType.DebitMemo ||
                            a.AdjustmentType == LockedPeriodAdjustmentType.CreditMemo),
                        "Purchases" => adjustmentsQuery.Where(a => a.AdjustmentType == LockedPeriodAdjustmentType.UnitCost),
                        "Commission" => adjustmentsQuery.Where(a => a.AdjustmentType == LockedPeriodAdjustmentType.Commission),
                        "Freight" => adjustmentsQuery.Where(a => a.AdjustmentType == LockedPeriodAdjustmentType.Freight),
                        _ => throw new ArgumentException("Invalid comparative report category.")
                    };
                }

                var adjustments = await adjustmentsQuery
                    .OrderBy(a => a.CreatedDate)
                    .ThenBy(a => a.AdjustmentType)
                    .ThenBy(a => a.EntityTypeNo)
                    .ToListAsync(cancellationToken);

                if (adjustments.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(Index));
                }

                var document = GenerateAdjustmentReport(adjustments, GetReportTitle(category), monthDate, isCombinedReport);

                FilprideAuditTrail auditTrailBook = new(
                    GetUserFullName(),
                    $"Generate {category.ToLower()} comparative adjustment report quest pdf",
                    "Comparative Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate comparative adjustment report. Category: {Category}, Month: {MonthDate}, Generated by: {UserName}",
                    category, monthDate, _userManager.GetUserName(User));
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private static string GetReportTitle(string category)
        {
            return category switch
            {
                "All" => "COMPARATIVE ADJUSTMENT REPORT",
                "Sales" => "COMPARATIVE SALES ADJUSTMENT REPORT",
                "Purchases" => "COMPARATIVE PURCHASE ADJUSTMENT REPORT",
                "Commission" => "COMPARATIVE COMMISSION ADJUSTMENT REPORT",
                "Freight" => "COMPARATIVE FREIGHT ADJUSTMENT REPORT",
                _ => "COMPARATIVE ADJUSTMENT REPORT"
            };
        }

        private Document GenerateAdjustmentReport(
            IReadOnlyCollection<IBS.Models.Filpride.LockedPeriodAdjustment> adjustments,
            string reportTitle,
            DateOnly monthDate,
            bool isCombinedReport)
        {
            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Legal.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Times New Roman"));

                    page.Header().Height(50).Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(reportTitle).FontSize(20).SemiBold();
                            column.Item().Text(text =>
                            {
                                text.Span("Period Created: ").SemiBold();
                                text.Span(monthDate.ToString("MMMM yyyy"));
                            });
                        });

                        row.ConstantItem(100)
                            .Height(50)
                            .Image(Image.FromFile(imgFilprideLogoPath))
                            .FitWidth();
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
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
                            if (isCombinedReport)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            AddHeaderCell(header, "Created Date");
                            AddHeaderCell(header, "Period");
                            if (isCombinedReport)
                            {
                                AddHeaderCell(header, "Category");
                            }
                            AddHeaderCell(header, "Module");
                            AddHeaderCell(header, "Reference");
                            AddHeaderCell(header, "Customer");
                            AddHeaderCell(header, "Supplier");
                            AddHeaderCell(header, "Affected Qty");
                            AddHeaderCell(header, "Old Value");
                            AddHeaderCell(header, "New Value");
                            AddHeaderCell(header, "Value Diff");
                            AddHeaderCell(header, "Adjustment");
                            AddHeaderCell(header, "Reason");
                            AddHeaderCell(header, "Created By");
                        });

                        foreach (var adjustment in adjustments)
                        {
                            AddTextCell(table, adjustment.CreatedDate.ToString(SD.Date_Format));
                            AddTextCell(table, adjustment.Period.ToString("MMM yyyy"));
                            if (isCombinedReport)
                            {
                                AddTextCell(table, GetCategoryLabel(adjustment.AdjustmentType));
                            }
                            AddTextCell(table, adjustment.EntityType.ToString());
                            AddTextCell(table, adjustment.EntityTypeNo);
                            AddTextCell(table, adjustment.CustomerName);
                            AddTextCell(table, adjustment.SupplierName);
                            AddNumberCell(table, adjustment.AffectedQuantity, SD.Two_Decimal_Format);
                            AddNumberCell(table, adjustment.OldValue, SD.Four_Decimal_Format);
                            AddNumberCell(table, adjustment.NewValue, SD.Four_Decimal_Format);
                            AddNumberCell(table, adjustment.NewValue - adjustment.OldValue, SD.Four_Decimal_Format, true);
                            AddNumberCell(table, adjustment.AdjustmentValue, SD.Two_Decimal_Format, true);
                            AddTextCell(table, adjustment.Reason);
                            AddTextCell(table, adjustment.CreatedBy);
                        }

                        AddTotalRow(table,
                            adjustments.Sum(a => a.AffectedQuantity),
                            adjustments.Sum(a => a.NewValue - a.OldValue),
                            adjustments.Sum(a => a.AdjustmentValue),
                            isCombinedReport);
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        }

        private static void AddHeaderCell(TableCellDescriptor table, string text)
        {
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text(text).SemiBold();
        }

        private static void AddTextCell(TableDescriptor table, string? text)
        {
            table.Cell().Border(0.5f).Padding(3).Text(text ?? string.Empty);
        }

        private static void AddNumberCell(TableDescriptor table, decimal value, string format, bool emphasize = false)
        {
            var descriptor = table.Cell().Border(0.5f).Padding(3).AlignRight().Text(FormatNumber(value, format));

            if (emphasize)
            {
                descriptor.SemiBold().FontColor(value < 0 ? Colors.Red.Medium : Colors.Black);
            }
        }

        private static void AddTotalRow(
            TableDescriptor table,
            decimal totalAffectedQuantity,
            decimal totalValueDifference,
            decimal totalAdjustment,
            bool isCombinedReport)
        {
            table.Cell().ColumnSpan((uint)(isCombinedReport ? 7 : 6))
                .Background(Colors.Grey.Lighten1)
                .Border(0.5f)
                .Padding(3)
                .AlignRight()
                .Text("TOTAL")
                .SemiBold();
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight()
                .Text(FormatNumber(totalAffectedQuantity, SD.Two_Decimal_Format))
                .SemiBold();
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3);
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3);
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight()
                .Text(FormatNumber(totalValueDifference, SD.Four_Decimal_Format))
                .SemiBold()
                .FontColor(totalValueDifference < 0 ? Colors.Red.Medium : Colors.Black);
            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight()
                .Text(FormatNumber(totalAdjustment, SD.Two_Decimal_Format))
                .SemiBold()
                .FontColor(totalAdjustment < 0 ? Colors.Red.Medium : Colors.Black);
            table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3);
        }

        private static string FormatNumber(decimal value, string format)
        {
            return value < 0
                ? $"({Math.Abs(value).ToString(format)})"
                : value.ToString(format);
        }

        private static string GetCategoryLabel(LockedPeriodAdjustmentType adjustmentType)
        {
            return adjustmentType switch
            {
                LockedPeriodAdjustmentType.SellingPrice => "Sales",
                LockedPeriodAdjustmentType.DebitMemo => "Sales",
                LockedPeriodAdjustmentType.CreditMemo => "Sales",
                LockedPeriodAdjustmentType.UnitCost => "Purchases",
                LockedPeriodAdjustmentType.Commission => "Commission",
                LockedPeriodAdjustmentType.Freight => "Freight",
                _ => adjustmentType.ToString()
            };
        }
    }
}
