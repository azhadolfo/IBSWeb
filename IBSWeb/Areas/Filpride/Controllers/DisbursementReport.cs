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

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class DisbursementReport : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public DisbursementReport(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
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
        public IActionResult ClearedDisbursementReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedClearedDisbursementReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }

            try
            {
                var checkVoucherHeader = await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo, companyClaims!);

                if (!checkVoucherHeader.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(ClearedDisbursementReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("CLEARED DISBURSEMENT REPORT")
                                    .FontSize(20).SemiBold();

                                column.Item().Text(text =>
                                {
                                    text.Span("Date From: ").SemiBold();
                                    text.Span(model.DateFrom.ToString());
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Date To: ").SemiBold();
                                    text.Span(model.DateTo.ToString());
                                });
                            });

                            row.ConstantItem(size: 100)
                                .Height(50)
                                .Image(Image.FromFile(imgFilprideLogoPath)).FitWidth();

                        });

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(125);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(125);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Category").SemiBold();
                                header.Cell().Text("Subcategory").SemiBold();
                                header.Cell().Text("Payee").SemiBold();
                                header.Cell().Text("Date").SemiBold();
                                header.Cell().Text("Voucher#").SemiBold();
                                header.Cell().Text("Bank Name").SemiBold();
                                header.Cell().Text("Check").SemiBold();
                                header.Cell().Text("Particulars").SemiBold();
                                header.Cell().AlignRight().Text("Total").SemiBold();
                            });

                            var totalAmt = 0m;
                            foreach (var record in checkVoucherHeader)
                            {
                                table.Cell().Text("Empty");
                                table.Cell().Text("Empty");
                                table.Cell().Text(record.Payee);
                                table.Cell().Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Text(record.CheckVoucherHeaderNo);
                                table.Cell().Text(record.BankAccount!.AccountName);
                                table.Cell().Text(record.CheckNo);
                                table.Cell().Text(record.Particulars);
                                table.Cell().AlignRight().Text(record.Total.ToString(SD.Two_Decimal_Format));
                                totalAmt += record.Total;
                            }

                            table.Cell().ColumnSpan(9).Element(cell =>
                            {
                                cell.Column(column =>
                                {
                                    column.Item().Height(2); // Top spacing or content
                                    column.Item().Height(1).Background(Colors.Black); // Horizontal line
                                    column.Item().Height(3); // Bottom spacing or content
                                });
                            });

                            table.Cell().ColumnSpan(8).AlignRight().Text("TOTAL:").SemiBold();
                            table.Cell().AlignRight().Text(totalAmt.ToString(SD.Two_Decimal_Format)).SemiBold();

                            table.Cell().ColumnSpan(9).Element(cell =>
                            {
                                cell.Column(column =>
                                {
                                    column.Item().Height(2); // Top spacing or content
                                    column.Item().Height(1).Background(Colors.Black); // Horizontal line
                                    column.Item().Height(2); // Horizontal line
                                    column.Item().Height(1).Background(Colors.Black); // Horizontal line
                                    column.Item().Height(3); // Bottom spacing or content
                                });
                            });
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

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cleared disbursement report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }
    }
}
