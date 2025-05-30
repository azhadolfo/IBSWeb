using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class GeneralLedgerReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public GeneralLedgerReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
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

        public IActionResult GeneralLedgerBook()
        {
            return View();
        }

        public async Task<IActionResult> GeneralLedgerBookReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(GeneralLedgerBook));
            }

            try
            {
                var generalLedgerBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims!);

                if (!generalLedgerBooks.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }

                var totalDebit = generalLedgerBooks.Sum(gb => gb.Debit);
                var totalCredit = generalLedgerBooks.Sum(gb => gb.Credit);

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
                                    .Text($"GENERAL LEDGER BY TRANSACTION")
                                    .FontSize(20).SemiBold();

                                column.Item().Text(text =>
                                {
                                    text.Span("Date From: ").SemiBold();
                                    text.Span($"{model.DateFrom}");
                                });

                                column.Item().Text(text =>
                                {
                                    text.Span("Date To: ").SemiBold();
                                    text.Span($"{model.DateTo}");
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
                                columns.ConstantColumn(90);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Date").SemiBold();
                                header.Cell().Text("Reference").SemiBold();
                                header.Cell().Text("Description").SemiBold();
                                header.Cell().Text("Account Title").SemiBold();
                                header.Cell().AlignRight().Text("Debit").SemiBold();
                                header.Cell().AlignRight().Text("Credit").SemiBold();
                            });

                            foreach (var record in generalLedgerBooks)
                            {
                                table.Cell().Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Text(record.Reference);
                                table.Cell().Text(record.Description);
                                table.Cell().Text($"{record.AccountNo} {record.AccountTitle}");
                                table.Cell().AlignRight().Text($"{record.Debit.ToString(SD.Two_Decimal_Format)}");
                                table.Cell().AlignRight().Text($"{record.Credit.ToString(SD.Two_Decimal_Format)}");

                            }

                            table.Cell().ColumnSpan(4).AlignRight().Text("TOTAL:").SemiBold();
                            table.Cell().AlignRight().Text($"{totalDebit.ToString(SD.Two_Decimal_Format)}").SemiBold();
                            table.Cell().AlignRight().Text($"{totalCredit.ToString(SD.Two_Decimal_Format)}").SemiBold();
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
                _logger.LogError(ex, "Failed to terminate placement. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(GeneralLedgerBook));
            }
        }
    }
}
