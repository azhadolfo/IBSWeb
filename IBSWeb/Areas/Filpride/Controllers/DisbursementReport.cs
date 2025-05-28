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
using OfficeOpenXml;
using OfficeOpenXml.Style;

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

        #region -- Generate Cleared Disbursement Report Excel File --

        public async Task<IActionResult> GenerateClearedDisbursementReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var clearedDisbursementReport =
                    await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo,
                        companyClaims);


                if (clearedDisbursementReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(clearedDisbursementReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("ClearedDisbursementReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "CLEARED DISBURSEMENT REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Category";
                worksheet.Cells["B7"].Value = "Subcategory";
                worksheet.Cells["C7"].Value = "Payee";
                worksheet.Cells["D7"].Value = "Date";
                worksheet.Cells["E7"].Value = "Voucher #";
                worksheet.Cells["F7"].Value = "Bank Name";
                worksheet.Cells["G7"].Value = "Check #";
                worksheet.Cells["H7"].Value = "Particulars";
                worksheet.Cells["I7"].Value = "Amount";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:I7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = 8;
                string currencyFormat = "#,##0.00";

                foreach (var cd in clearedDisbursementReport)
                {

                    var details = await _dbContext.FilprideCheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == cd.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken);

                    var invoiceDebit = details
                        .FirstOrDefault(d => d.Debit > 0);

                    var getCategoryInChartOfAccount = await _dbContext.FilprideChartOfAccounts
                        .Include(coa => coa.ParentAccount) // Level 3
                        .ThenInclude(a => a!.ParentAccount) // Level 2
                        .ThenInclude(a => a!.ParentAccount) // Level 1
                        .Where(coa => coa.AccountNumber == invoiceDebit!.AccountNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    var levelOneAccount = getCategoryInChartOfAccount?.ParentAccount?.ParentAccount;

                    worksheet.Cells[row, 1].Value = $"{levelOneAccount?.AccountNumber} " +
                                                    $"{levelOneAccount?.AccountName}";
                    worksheet.Cells[row, 2].Value = $"{invoiceDebit!.AccountNo} {invoiceDebit.AccountName}";
                    worksheet.Cells[row, 3].Value = cd.Payee;
                    worksheet.Cells[row, 4].Value = cd.Date;
                    worksheet.Cells[row, 5].Value = cd.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 6].Value = cd.BankAccount!.AccountName;
                    worksheet.Cells[row, 7].Value = cd.CheckNo;
                    worksheet.Cells[row, 8].Value = cd.Particulars;
                    worksheet.Cells[row, 9].Value = cd.Total;

                    worksheet.Cells[row, 4].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                worksheet.Cells[row, 8].Value = "Total: ";
                worksheet.Cells[row, 9].Value = clearedDisbursementReport.Sum(cv => cv.Total);
                using (var range = worksheet.Cells[row, 1, row, 9])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thick; // Apply thick border at the top of the row
                }

                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ClearedDisbursementReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }


        #endregion
    }
}
