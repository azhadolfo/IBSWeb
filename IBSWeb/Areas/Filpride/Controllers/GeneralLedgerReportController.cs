using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace IBSWeb.Areas.User.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class GeneralLedgerReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public GeneralLedgerReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public IActionResult GeneralLedgerBook()
        {
            return View();
        }

        public async Task<IActionResult> GeneralLedgerBookReport(ViewModelBook model)
        {
            //var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    var generalLedgerBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, nameof(Filpride));

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
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                            page.Content().Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(90);
                                    columns.ConstantColumn(110);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(100);
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
                                    table.Cell().Text(record.Date.ToString("MM/dd/yyyy"));
                                    table.Cell().Text(record.Reference);
                                    table.Cell().Text(record.Description);
                                    table.Cell().Text($"{record.AccountNo} {record.AccountTitle}");
                                    table.Cell().AlignRight().Text($"{record.Debit:N2}");
                                    table.Cell().AlignRight().Text($"{record.Credit:N2}");
                                }

                                table.Cell().ColumnSpan(4).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().AlignRight().Text($"{totalDebit:N2}").SemiBold();
                                table.Cell().AlignRight().Text($"{totalCredit:N2}").SemiBold();
                            });
                        });
                    });

                    var pdfBytes = document.GeneratePdf();

                    Response.Headers.Add("Content-Disposition", "inline; filename=AuditTrailReport.pdf");
                    return File(pdfBytes, "application/pdf");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(GeneralLedgerBook));
        }
    }
}
