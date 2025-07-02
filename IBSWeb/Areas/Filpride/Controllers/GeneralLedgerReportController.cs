using System.Text;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
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

        #region -- Generated General Ledger by Transaction as Quest PDF

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
                                        .Text("GENERAL LEDGER BY TRANSACTION")
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
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Reference").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Description").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Title").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Debit").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Credit").SemiBold();
                                });

                            #endregion

                            #region -- Loop to Show Records

                                foreach (var record in generalLedgerBooks)
                                {
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Reference);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Description);
                                    table.Cell().Border(0.5f).Padding(3).Text($"{record.AccountNo} {record.AccountTitle}");
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Debit != 0 ? record.Debit < 0 ? $"({Math.Abs(record.Debit).ToString(SD.Two_Decimal_Format)})" : record.Debit.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Debit < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Credit != 0 ? record.Credit < 0 ? $"({Math.Abs(record.Credit).ToString(SD.Two_Decimal_Format)})" : record.Credit.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Credit < 0 ? Colors.Red.Medium : Colors.Black);

                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalDebit != 0 ? totalDebit < 0 ? $"({Math.Abs(totalDebit).ToString(SD.Two_Decimal_Format)})" : totalDebit.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalDebit < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCredit != 0 ? totalCredit < 0 ? $"({Math.Abs(totalCredit).ToString(SD.Two_Decimal_Format)})" : totalCredit.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalCredit < 0 ? Colors.Red.Medium : Colors.Black);

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
                _logger.LogError(ex, "Failed to generate general ledger by transaction. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(GeneralLedgerBook));
            }
        }

        #endregion

        #region -- Generate GeneralLedgerBook by Transaction as Excel File
            public async Task<IActionResult> GenerateGeneralLedgerBookExcelFile(ViewModelBook model, CancellationToken cancellationToken)
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var generalBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
                if (generalBooks.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }
                var totalDebit = generalBooks.Sum(gb => gb.Debit);
                var totalCredit = generalBooks.Sum(gb => gb.Credit);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("GeneralLedgerBook");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "GENERAL LEDGER BOOK";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

                worksheet.Cells["A7"].Value = "Date";
                worksheet.Cells["B7"].Value = "Reference";
                worksheet.Cells["C7"].Value = "Description";
                worksheet.Cells["D7"].Value = "Account Title";
                worksheet.Cells["E7"].Value = "Debit";
                worksheet.Cells["F7"].Value = "Credit";
                worksheet.Cells["G7"].Value = "Company";
                worksheet.Cells["H7"].Value = "Bank";
                worksheet.Cells["I7"].Value = "Customer";
                worksheet.Cells["J7"].Value = "Supplier";
                worksheet.Cells["K7"].Value = "Employee";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:K7"])
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

                foreach (var gl in generalBooks)
                {
                    worksheet.Cells[row, 1].Value = gl.Date;
                    worksheet.Cells[row, 2].Value = gl.Reference;
                    worksheet.Cells[row, 3].Value = gl.Description;
                    worksheet.Cells[row, 4].Value = $"{gl.AccountNo} {gl.AccountTitle}";

                    worksheet.Cells[row, 5].Value = gl.Debit;
                    worksheet.Cells[row, 6].Value = gl.Credit;
                    worksheet.Cells[row, 7].Value = gl.CompanyName;
                    worksheet.Cells[row, 8].Value = gl.BankAccountName;
                    worksheet.Cells[row, 9].Value = gl.CustomerName;
                    worksheet.Cells[row, 10].Value = gl.SupplierName;
                    worksheet.Cells[row, 11].Value = gl.EmployeeName;

                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                worksheet.Cells[row, 4].Value = "Total ";
                worksheet.Cells[row, 5].Value = totalDebit;
                worksheet.Cells[row, 6].Value = totalCredit;

                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;

                // Apply style to subtotal row
                using (var range = worksheet.Cells[row, 1, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                }

                using (var range = worksheet.Cells[row, 4, row, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GeneralLedgerBook_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
        #endregion

        public async Task<IActionResult> GeneralLedgerReportByAccountNumber()
        {

            var viewModel = new GeneralLedgerReportViewModel
            {
                ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountNumber)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber + " " + s.AccountName,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(),
            };

            return View(viewModel);
        }

        #region -- Generated Ganeral Ledger by Account number as Quest PDF

        public async Task<IActionResult> GenerateGeneralLedgerReportByAccountNumber(GeneralLedgerReportViewModel model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(GeneralLedgerReportByAccountNumber));
            }

            try
            {
                var generalLedgerByAccountNo = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(g => g.Supplier)
                    .Include(g => g.Customer)
                    .Where(g =>
                        g.Date >= model.DateFrom && g.Date <= model.DateTo &&
                        (model.AccountNo == null || g.AccountNo == model.AccountNo) &&
                        g.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                if (!generalLedgerByAccountNo.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(GeneralLedgerReportByAccountNumber));
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
                                    .Text("GENERAL LEDGER BY ACCOUNT NUMBER")
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
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Particular").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Title").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Code").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Code").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Debit").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Credit").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Balance").SemiBold();
                                });

                            #endregion

                            #region -- Initialize Variable for Computation

                                int row = 8;
                                decimal balance = 0;
                                decimal debit = 0;
                                decimal credit = 0;

                            #endregion

                            #region -- Loop to Show Records

                                foreach (var grouped in generalLedgerByAccountNo.OrderBy(g => g.AccountNo).GroupBy(g => g.AccountTitle))
                                {
                                    balance = 0;

                                    foreach (var journal in grouped.OrderBy(g => g.Date))
                                    {
                                        var account =
                                            _dbContext.FilprideChartOfAccounts.FirstOrDefault(a =>
                                                a.AccountNumber == journal.AccountNo);

                                        if (balance != 0)
                                        {
                                            if (account?.NormalBalance == nameof(NormalBalance.Debit))
                                            {
                                                balance += journal.Debit - journal.Credit;
                                            }
                                            else
                                            {
                                                balance -= journal.Debit - journal.Credit;
                                            }
                                        }
                                        else
                                        {
                                            balance = journal.Debit > 0 ? journal.Debit : journal.Credit;
                                        }

                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Date.ToString(SD.Date_Format));
                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Description);
                                        table.Cell().Border(0.5f).Padding(3).Text($"{journal.AccountNo} {journal.AccountTitle}");
                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Customer?.CustomerCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Customer?.CustomerName);
                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Supplier?.SupplierCode);
                                        table.Cell().Border(0.5f).Padding(3).Text(journal.Supplier?.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(journal.Debit != 0 ? journal.Debit < 0 ? $"({Math.Abs(journal.Debit).ToString(SD.Two_Decimal_Format)})" : journal.Debit.ToString(SD.Two_Decimal_Format) : null).FontColor(journal.Debit < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(journal.Credit != 0 ? journal.Credit < 0 ? $"({Math.Abs(journal.Credit).ToString(SD.Two_Decimal_Format)})" : journal.Credit.ToString(SD.Two_Decimal_Format) : null).FontColor(journal.Credit < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(balance != 0 ? balance < 0 ? $"({Math.Abs(balance).ToString(SD.Two_Decimal_Format)})" : balance.ToString(SD.Two_Decimal_Format) : null).FontColor(balance < 0 ? Colors.Red.Medium : Colors.Black);

                                        row++;
                                    }

                                    debit = grouped.Sum(j => j.Debit);
                                    credit = grouped.Sum(j => j.Credit);
                                    balance = debit - credit;

                                    #region -- Sub Total

                                        table.Cell().ColumnSpan(7).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text($"Total {grouped.Key}").SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(debit.ToString(SD.Two_Decimal_Format)).SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(credit.ToString(SD.Two_Decimal_Format)).SemiBold();
                                        table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(balance.ToString(SD.Two_Decimal_Format)).SemiBold();

                                    #endregion

                                    row++;
                                }

                            #endregion

                            #region -- Initialize Variable for Computation of Totals

                            debit = generalLedgerByAccountNo.Sum(j => j.Debit);
                            credit = generalLedgerByAccountNo.Sum(j => j.Credit);
                            balance = debit - credit;

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(7).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("GRAND TOTAL:").Bold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(debit != 0 ? debit < 0 ? $"({Math.Abs(debit).ToString(SD.Two_Decimal_Format)})" : debit.ToString(SD.Two_Decimal_Format) : null).Bold().FontColor(debit < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(credit != 0 ? credit < 0 ? $"({Math.Abs(credit).ToString(SD.Two_Decimal_Format)})" : credit.ToString(SD.Two_Decimal_Format) : null).Bold().FontColor(credit < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(balance != 0 ? balance < 0 ? $"({Math.Abs(balance).ToString(SD.Two_Decimal_Format)})" : balance.ToString(SD.Two_Decimal_Format) : null).Bold().FontColor(balance < 0 ? Colors.Red.Medium : Colors.Black);

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
                _logger.LogError(ex, "Failed to generate general ledger by account number. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(GeneralLedgerReportByAccountNumber));
            }
        }

        #endregion

        #region -- Generate General Ledger by Account Number as Excel File

        public async Task<IActionResult> GenerateGeneralLedgerReportByAccountNumberExcelFile(GeneralLedgerReportViewModel model, CancellationToken cancellationToken)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);
            var companyClaims = await GetCompanyClaimAsync();

            var chartOfAccount = await _dbContext.FilprideChartOfAccounts
                .FirstOrDefaultAsync(coa => coa.AccountNumber == model.AccountNo);

            var generalLedgerByAccountNo = await _dbContext.FilprideGeneralLedgerBooks
                .Include(g => g.Supplier)
                .Include(g => g.Customer)
                .Where(g =>
                    g.Date >= dateFrom && g.Date <= dateTo &&
                    (model.AccountNo == null || g.AccountNo == model.AccountNo) &&
                    g.Company == companyClaims)
                .ToListAsync(cancellationToken);

            if (generalLedgerByAccountNo.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(GeneralLedgerReportByAccountNumber));
            }

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("GeneralLedger");

            // Set the column headers
            var mergedCells = worksheet.Cells["A1:C1"];
            mergedCells.Merge = true;
            mergedCells.Value = "GENERAL LEDGER BY ACCOUNT NUMBER";
            mergedCells.Style.Font.Size = 13;

            worksheet.Cells["A2"].Value = "Date Range:";
            worksheet.Cells["A3"].Value = "Account No:";
            worksheet.Cells["A4"].Value = "Account Title:";

            worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
            worksheet.Cells["B3"].Value = $"{chartOfAccount}";
            worksheet.Cells["B4"].Value = $"{chartOfAccount?.AccountNumber}";

            worksheet.Cells["A7"].Value = "Date";
            worksheet.Cells["B7"].Value = "Particular";
            worksheet.Cells["C7"].Value = "Account No";
            worksheet.Cells["D7"].Value = "Account Title";
            worksheet.Cells["E7"].Value = "Customer Code";
            worksheet.Cells["F7"].Value = "Customer Name";
            worksheet.Cells["G7"].Value = "Supplier Code";
            worksheet.Cells["H7"].Value = "Supplier Name";
            worksheet.Cells["I7"].Value = "Debit";
            worksheet.Cells["J7"].Value = "Credit";
            worksheet.Cells["K7"].Value = "Balance";

            using (var range = worksheet.Cells["A7:K7"])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            int row = 8;
            decimal balance = 0;
            string currencyFormat = "#,##0.00";
            decimal debit = 0;
            decimal credit = 0;
            foreach (var grouped in generalLedgerByAccountNo.OrderBy(g => g.AccountNo).GroupBy(g => g.AccountTitle))
            {
                balance = 0;

                foreach (var journal in grouped.OrderBy(g => g.Date))
                {
                    var account = await _dbContext.FilprideChartOfAccounts
                        .FirstOrDefaultAsync(a => a.AccountNumber == journal.AccountNo);

                    if (balance != 0)
                    {
                        if (account?.NormalBalance == nameof(NormalBalance.Debit))
                        {
                            balance += journal.Debit - journal.Credit;
                        }
                        else
                        {
                            balance -= journal.Debit - journal.Credit;
                        }
                    }
                    else
                    {
                        balance = journal.Debit > 0 ? journal.Debit : journal.Credit;
                    }

                    worksheet.Cells[row, 1].Value = journal.Date.ToString("dd-MMM-yyyy");
                    worksheet.Cells[row, 2].Value = journal.Description;
                    worksheet.Cells[row, 3].Value = journal.AccountNo;
                    worksheet.Cells[row, 4].Value = journal.AccountTitle;
                    worksheet.Cells[row, 5].Value = journal.Customer?.CustomerCode;
                    worksheet.Cells[row, 6].Value = journal.Customer?.CustomerName;
                    worksheet.Cells[row, 7].Value = journal.Supplier?.SupplierCode;
                    worksheet.Cells[row, 8].Value = journal.Supplier?.SupplierName;

                    worksheet.Cells[row, 9].Value = journal.Debit;
                    worksheet.Cells[row, 10].Value = journal.Credit;
                    worksheet.Cells[row, 11].Value = balance;

                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                    row++;
                }

                debit = grouped.Sum(j => j.Debit);
                credit = grouped.Sum(j => j.Credit);
                balance = debit - credit;

                worksheet.Cells[row, 8].Value = "Total " + grouped.Key;
                worksheet.Cells[row, 9].Value = debit;
                worksheet.Cells[row, 10].Value = credit;
                worksheet.Cells[row, 11].Value = balance;

                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                using (var range = worksheet.Cells[row, 1, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                }

                row++;
            }

            using (var range = worksheet.Cells[row, 9, row, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
            }

            debit = generalLedgerByAccountNo.Sum(j => j.Debit);
            credit = generalLedgerByAccountNo.Sum(j => j.Credit);
            balance = debit - credit;

            worksheet.Cells[row, 8].Value = "Total";
            worksheet.Cells[row, 8].Style.Font.Bold = true;
            worksheet.Cells[row, 9].Value = debit;
            worksheet.Cells[row, 10].Value = credit;
            worksheet.Cells[row, 11].Value = balance;

            worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
            worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

            // Auto-fit columns for better readability
            worksheet.Cells.AutoFitColumns();
            worksheet.View.FreezePanes(8, 1);

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"GeneralLedgerByAccountNo_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
        }

        #endregion

        #region -- Generate General Ledger as .Txt file
            public async Task<IActionResult> GenerateGeneralLedgerBookTxtFile(ViewModelBook model)
            {
                if (ModelState.IsValid)
                {
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

                        var generalBooks = await _unitOfWork.FilprideReport.GetGeneralLedgerBooks(model.DateFrom, model.DateTo, companyClaims);
                        if (generalBooks.Count == 0)
                        {
                            TempData["error"] = "No Record Found";
                            return RedirectToAction(nameof(GeneralLedgerBook));
                        }
                        var totalDebit = generalBooks.Sum(gb => gb.Debit);
                        var totalCredit = generalBooks.Sum(gb => gb.Credit);
                        var lastRecord = generalBooks.LastOrDefault();
                        var firstRecord = generalBooks.FirstOrDefault();
                        if (lastRecord != null)
                        {
                            ViewBag.LastRecord = lastRecord.CreatedDate;
                        }

                        var fileContent = new StringBuilder();

                        fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
                        fileContent.AppendLine($"TIN: 000-216-589-00000");
                        fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
                        fileContent.AppendLine();
                        fileContent.AppendLine($"Accounting System: Accounting Administration System");
                        fileContent.AppendLine($"Acknowledgement Certificate Control No.:");
                        fileContent.AppendLine($"Date Issued:");
                        fileContent.AppendLine();
                        fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
                        fileContent.AppendLine("File Name: General Ledger Book Report");
                        fileContent.AppendLine("File Type: Text File");
                        fileContent.AppendLine($"{"Number of Records: ",-35}{generalBooks.Count}");
                        fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
                        fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
                        fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
                        fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
                        fileContent.AppendLine();
                        fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
                        fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord!.Date}");
                        fileContent.AppendLine($"{"Reference"}\t{"Reference"}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
                        fileContent.AppendLine($"{"Description"}\t{"Description"}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
                        fileContent.AppendLine($"{"AccountTitle"}\t{"Account Title"}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountNo + " " + firstRecord.AccountTitle}");
                        fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
                        fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
                        fileContent.AppendLine();
                        fileContent.AppendLine("GENERAL LEDGER BOOK");
                        fileContent.AppendLine();
                        fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",18}\t{"Credit",18}");

                        // Generate the records
                        foreach (var record in generalBooks)
                        {
                            fileContent.AppendLine($"{record.Date.ToString("MM/dd/yyyy"),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountNo + " " + record.AccountTitle,-50}\t{record.Debit,18}\t{record.Credit,18}");
                        }
                        fileContent.AppendLine(new string('-', 187));
                        fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-50}\t{"TOTAL:",50}\t{totalDebit,18}\t{totalCredit,18}");

                        fileContent.AppendLine();
                        fileContent.AppendLine($"Software Name: {CS.AAS}");
                        fileContent.AppendLine($"Version: {CS.Version}");
                        fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
                        fileContent.AppendLine($"Date & Time Extracted: {DateTimeHelper.GetCurrentPhilippineTimeFormatted()}");

                        // Convert the content to a byte array
                        var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

                        // Return the file to the user
                        return File(bytes, "text/plain", "GeneralLedgerBookReport.txt");
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(GeneralLedgerBook));
                    }
                }
                return View(model);
            }
        #endregion
    }
}
