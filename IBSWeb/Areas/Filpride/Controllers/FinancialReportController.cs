using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Helpers;
using QuestPDF.Fluent;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class FinancialReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<FinancialReportController> _logger;

        public FinancialReportController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            ILogger<FinancialReportController> logger)
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
        public IActionResult ProfitAndLossReport()
        {
            return View();
        }

        #region -- Generated Profit and Loss Report as Quest PDF

        public async Task<IActionResult> GenerateProfitAndLossReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ProfitAndLossReport));
            }

            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= firstDayOfMonth &&
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Include(coa => coa.Children)
                    .OrderBy(coa => coa.AccountNumber)
                    .Where(coa => coa.FinancialStatementType == nameof(FinancialStatementType.PnL))
                    .ToListAsync(cancellationToken);

                var nibitForThePeriod = await _dbContext.FilprideMonthlyNibits
                    .FirstOrDefaultAsync(m => m.Year == monthDate.Year &&
                                              m.Month == monthDate.Month &&
                                              m.Company == companyClaims, cancellationToken);

                if (nibitForThePeriod == null)
                {
                    TempData["error"] = "NIBIT For The Period not found. Contact MIS-Enterprise.";
                    return RedirectToAction(nameof(ProfitAndLossReport));
                }

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ProfitAndLossReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Letter.Portrait());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(140).Column(column =>
                            {
                                column.Item().Text("FILPRIDE RESOURCES INC.").FontSize(16).SemiBold().AlignCenter();

                                column.Item().AlignCenter().Row(row =>
                                {
                                    row.Spacing(10);
                                    row.ConstantItem(150).Height(45)
                                        .Image(QuestPDF.Infrastructure.Image.FromFile(imgFilprideLogoPath)).FitHeight().FitWidth();
                                    row.Spacing(10);
                                });

                                column.Item().Text("PNL REPORT").FontSize(14).SemiBold().AlignCenter();
                                column.Item().Text($"As Of {monthDate.ToString(SD.Date_Format)}").SemiBold().AlignCenter();
                            });



                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L1").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L2").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L3").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L4").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).BorderTop(0.5f).BorderLeft(0.5f).BorderBottom(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L5").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).BorderTop(0.5f).BorderRight(0.5f);
                                });

                            #endregion

                             #region -- Loop to Show Records

                                foreach (var record in chartOfAccounts.Where(a => a.IsMain))
                                {
                                    decimal grandTotal = 0;

                                    table.Cell().ColumnSpan(6).Border(0.5f).Padding(3).Text(record.AccountName);

                                    foreach (var levelTwo in record.Children)
                                    {
                                        decimal subTotal = 0;
                                        table.Cell().BorderLeft(0.5f).BorderBottom(0.5f);
                                        table.Cell().ColumnSpan(5).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelTwo.AccountName);

                                        foreach (var levelThree in levelTwo.Children)
                                        {
                                            table.Cell().ColumnSpan(2).BorderLeft(0.5f).BorderBottom(0.5f);
                                            table.Cell().ColumnSpan(4).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelThree.AccountName);

                                            foreach (var levelFour in levelThree.Children)
                                            {
                                                table.Cell().ColumnSpan(3).BorderLeft(0.5f).BorderBottom(0.5f);
                                                table.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelFour.AccountName);
                                                var levelFourBalance = generalLedgers
                                                    .Where(gl =>
                                                        gl.AccountNo == levelFour.AccountNumber)
                                                    .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                                        gl.Debit - gl.Credit :
                                                        gl.Credit - gl.Debit);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(levelFourBalance != 0 ? levelFourBalance < 0 ? $"({Math.Abs(levelFourBalance).ToString(SD.Two_Decimal_Format)})" : levelFourBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(levelFourBalance < 0 ? Colors.Red.Medium : Colors.Black);
                                                subTotal += levelFourBalance;

                                                foreach (var levelFive in levelFour.Children)
                                                {
                                                    table.Cell().ColumnSpan(4).BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelFive.AccountName);
                                                    var levelFiveBalance = generalLedgers
                                                        .Where(gl =>
                                                            gl.AccountNo == levelFour.AccountNumber)
                                                        .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                                            gl.Debit - gl.Credit :
                                                            gl.Credit - gl.Debit);
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(levelFiveBalance != 0 ? levelFiveBalance < 0 ? $"({Math.Abs(levelFiveBalance).ToString(SD.Two_Decimal_Format)})" : levelFiveBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(levelFiveBalance < 0 ? Colors.Red.Medium : Colors.Black);
                                                }
                                            }
                                        }

                                        table.Cell().BorderLeft(0.5f).BorderBottom(0.5f);
                                        table.Cell().ColumnSpan(4).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text($"TOTAL {levelTwo.AccountName.ToUpper()}").SemiBold();

                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(subTotal != 0 ? subTotal < 0 ? $"({Math.Abs(subTotal).ToString(SD.Two_Decimal_Format)})" : subTotal.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotal < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        grandTotal += subTotal;

                                    }

                                    table.Cell().ColumnSpan(5).Border(0.5f).Padding(3).Text($"TOTAL {record.AccountName.ToUpper()}").Bold();

                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(grandTotal != 0 ? grandTotal < 0 ? $"({Math.Abs(grandTotal).ToString(SD.Two_Decimal_Format)})" : grandTotal.ToString(SD.Two_Decimal_Format) : null).Bold();

                                }

                                table.Cell().ColumnSpan(6).Border(0.5f).Text("");
                                table.Cell().ColumnSpan(5).Border(0.5f).Padding(3).Text("NIBIT").Bold();
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(nibitForThePeriod.NetIncome != 0 ? nibitForThePeriod.NetIncome < 0 ? $"({Math.Abs(nibitForThePeriod.NetIncome).ToString(SD.Two_Decimal_Format)})" : nibitForThePeriod.NetIncome.ToString(SD.Two_Decimal_Format) : null).FontColor(nibitForThePeriod.NetIncome < 0 ? Colors.Red.Medium : Colors.Black).Bold();

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
                _logger.LogError(ex, "Failed to generate profit and loss report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ProfitAndLossReport));
            }
        }

        #endregion

        #region -- Generate PNL Report as Excel File --
        [HttpPost]

        public async Task<IActionResult> ProfitAndLossReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var companyClaims = await GetCompanyClaimAsync();
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= firstDayOfMonth &&
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Include(coa => coa.Children)
                    .OrderBy(coa => coa.AccountNumber)
                    .Where(coa => coa.FinancialStatementType == nameof(FinancialStatementType.PnL))
                    .ToListAsync(cancellationToken);

                var nibitForThePeriod = await _dbContext.FilprideMonthlyNibits
                    .FirstOrDefaultAsync(m => m.Year == monthDate.Year &&
                                              m.Month == monthDate.Month &&
                                              m.Company == companyClaims, cancellationToken);

                if (nibitForThePeriod == null)
                {
                    TempData["error"] = "NIBIT For The Period not found. Contact MIS-Enterprise.";
                    return RedirectToAction(nameof(ProfitAndLossReport));
                }

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ProfitAndLossReport));
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("PNL Report");
                string currencyFormat = "#,##0.00_);[Red](#,##0.00)";
                worksheet.View.FreezePanes(5, 1);
                int row = 1;

                #region == Column Header ==

                using (var range = worksheet.Cells[row, 1, row, 8])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES INC.";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;


                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    var picture = await worksheet.Drawings.AddPictureAsync(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 4, 10);
                    picture.SetSize(330, 75);
                }

                worksheet.Row(row).Height = 80;

                using (var range = worksheet.Cells[row, 1, row, 7])
                {
                    range.Merge = true;
                }
                row++;


                using (var range = worksheet.Cells[row, 1, row, 7])
                {
                    range.Merge = true;
                    range.Value = "PNL REPORT";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 7])
                {
                    range.Merge = true;
                    range.Value = "As of " + monthDate.ToString("MMM dd, yyyy");
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 7])
                {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row += 2;
                worksheet.Cells[row, 1].Value = "L1";
                worksheet.Cells[row, 2].Value = "L2";
                worksheet.Cells[row, 3].Value = "L3";
                worksheet.Cells[row, 4].Value = "L4";
                worksheet.Cells[row, 5].Value = "L5";

                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

                row++;

                #endregion

                foreach (var account in chartOfAccounts.Where(a => a.IsMain))
                {
                    decimal grandTotal = 0;

                    worksheet.Cells[row, 1].Value = account.AccountName;
                    row++;

                    foreach (var levelTwo in account.Children)
                    {
                        decimal subTotal = 0;
                        worksheet.Cells[row, 2].Value = levelTwo.AccountName;
                        row++;

                        foreach (var levelThree in levelTwo.Children)
                        {
                            worksheet.Cells[row, 3].Value = levelThree.AccountName;
                            row++;

                            foreach (var levelFour in levelThree.Children)
                            {
                                worksheet.Cells[row, 4].Value = levelFour.AccountName;
                                var levelFourBalance = generalLedgers
                                    .Where(gl =>
                                        gl.AccountNo == levelFour.AccountNumber)
                                    .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                        gl.Debit - gl.Credit :
                                        gl.Credit - gl.Debit);
                                worksheet.Cells[row, 6].Value = levelFourBalance != 0 ? levelFourBalance : null;
                                subTotal += levelFourBalance;
                                row++;

                                foreach (var levelFive in levelFour.Children)
                                {
                                    worksheet.Cells[row, 5].Value = levelFive.AccountName;
                                    var levelFiveBalance = generalLedgers
                                        .Where(gl =>
                                            gl.AccountNo == levelFour.AccountNumber)
                                        .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                            gl.Debit - gl.Credit :
                                            gl.Credit - gl.Debit);
                                    worksheet.Cells[row, 6].Value = levelFiveBalance != 0 ? levelFiveBalance : null;
                                    row++;
                                }
                            }
                        }

                        worksheet.Cells[row, 2].Value = $"TOTAL {levelTwo.AccountName.ToUpper()}";
                        worksheet.Cells[row, 2].Style.Font.Bold = true;
                        worksheet.Cells[row, 6].Value = subTotal != 0 ? subTotal : null;
                        worksheet.Cells[row, 6].Style.Font.Bold = true;
                        worksheet.Cells[row, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[row, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        grandTotal += subTotal;
                        row++;
                    }

                    worksheet.Cells[row, 1].Value = $"TOTAL {account.AccountName.ToUpper()}";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 6].Value = grandTotal != 0 ? grandTotal : null;
                    worksheet.Cells[row, 6].Style.Font.Bold = true;
                    worksheet.Cells[row, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells[row, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    row++;

                }

                worksheet.Cells[row + 1, 1].Value = "NIBIT";
                worksheet.Cells[row + 1, 6].Value = nibitForThePeriod.NetIncome;
                worksheet.Cells[row + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[row + 1, 6].Style.Font.Bold = true;
                worksheet.Cells[row + 1, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row + 1, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells["F"].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells.AutoFitColumns();
                for (int i = 1; i <= 4; i++)
                {
                    worksheet.Column(i).Width = 4.5;
                }
                worksheet.Column(5).Width = 50;

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PNL Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate profit and loss report.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(ProfitAndLossReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult LevelOneReport()
        {
            return View();
        }

        #region -- Generated Level One Report as Quest PDF

        public async Task<IActionResult> GenerateLevelOneReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(LevelOneReport));
            }

            try
            {
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= firstDayOfMonth &&
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(LevelOneReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Letter.Portrait());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(140).Column(column =>
                            {
                                column.Item().Text("FILPRIDE RESOURCES INC.").FontSize(16).SemiBold().AlignCenter();

                                column.Item().AlignCenter().Row(row =>
                                {
                                    row.Spacing(10);
                                    row.ConstantItem(150).Height(45)
                                        .Image(QuestPDF.Infrastructure.Image.FromFile(imgFilprideLogoPath)).FitHeight().FitWidth();
                                    row.Spacing(10);
                                });

                                column.Item().Text("Level 1").FontSize(14).SemiBold().AlignCenter();
                                column.Item().Text($"As Of: {monthDate:dd MMM yyyy}").SemiBold().AlignCenter();
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
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Number").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Debit").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Credit").SemiBold();
                                });

                            #endregion

                             #region -- Loop to Show Records


                                 var groupByLevelOne = generalLedgers
                                     .OrderBy(gl => gl.Account.AccountNumber)
                                     .GroupBy(gl =>
                                     {
                                         // Traverse the account hierarchy to find the top-level parent account
                                         var currentAccount = gl.Account;
                                         while (currentAccount.ParentAccount != null)
                                         {
                                             currentAccount = currentAccount.ParentAccount;
                                         }
                                         // Return the top-level parent account (mother account)
                                         return new { currentAccount.AccountNumber, currentAccount.AccountName };
                                     });

                                 decimal nibit = 0;

                                foreach (var record in groupByLevelOne)
                                {
                                    var creditPosition = record
                                        .Sum(g => g.Account.NormalBalance == nameof(NormalBalance.Debit)
                                            ? g.Debit - g.Credit
                                            : g.Credit - g.Debit);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Key.AccountNumber);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.Key.AccountName);
                                    if (record.First().Account.FinancialStatementType == nameof(FinancialStatementType.BalanceSheet))
                                    {
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Sum(g => g.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                            g.Debit - g.Credit :
                                            g.Credit - g.Debit).ToString(SD.Two_Decimal_Format));
                                        table.Cell().Border(0.5f);
                                    }
                                    else
                                    {
                                        table.Cell().Border(0.5f);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(creditPosition.ToString(SD.Two_Decimal_Format));

                                        if (nibit == 0)
                                        {
                                            nibit += creditPosition;
                                            continue;
                                        }

                                        nibit -= creditPosition;
                                    }
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("NIBIT").SemiBold();
                               table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(nibit.ToString(SD.Two_Decimal_Format)).SemiBold();

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
                _logger.LogError(ex, "Failed to generate level one report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(LevelOneReport));
            }
        }

        #endregion

        #region -- Generate Level One Report as Excel File --

        [HttpPost]
        public async Task<IActionResult> LevelOneReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var companyClaims = await GetCompanyClaimAsync();
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= firstDayOfMonth &&
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(LevelOneReport));
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Level One");
                string currencyFormat = "#,##0.00_);[Red](#,##0.00)";
                worksheet.View.FreezePanes(5, 1);
                int row = 1;

                #region == Top of Header ==


                using (var range = worksheet.Cells[row, 1, row, 4])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES INC.";
                    range.Style.Font.Bold = true;
                }
                row++;

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    var picture = await worksheet.Drawings.AddPictureAsync(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 1, -85);
                    picture.SetSize(330, 75);
                }

                worksheet.Row(row).Height = 80;

                using (var range = worksheet.Cells[row, 1, row, 4])
                {
                    range.Merge = true;
                }
                row++;
                using (var range = worksheet.Cells[row, 1, row, 4])
                {
                    range.Merge = true;
                    range.Value = "Level 1";
                    range.Style.Font.Bold = true;
                }
                row++;
                using (var range = worksheet.Cells[row, 1, row, 4])
                {
                    range.Merge = true;
                    range.Value =  "As of " + monthDate.ToString("dd MMMM yyyy");
                }
                row++;
                using (var range = worksheet.Cells[1, 1, row, 4])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                #endregion

                row++;

                worksheet.Cells[row, 1].Value = "ACCOUNT NUMBER";
                worksheet.Cells[row, 2].Value = "ACCOUNT NAME";
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                row++;

                var groupByLevelOne = generalLedgers
                    .OrderBy(gl => gl.Account.AccountNumber)
                    .GroupBy(gl =>
                    {
                        // Traverse the account hierarchy to find the top-level parent account
                        var currentAccount = gl.Account;
                        while (currentAccount.ParentAccount != null)
                        {
                            currentAccount = currentAccount.ParentAccount;
                        }
                        // Return the top-level parent account (mother account)
                        return new { currentAccount.AccountNumber, currentAccount.AccountName };
                    });

                decimal nibit = 0;
                foreach (var gl in groupByLevelOne)
                {
                    worksheet.Cells[row, 1].Value = gl.Key.AccountNumber;
                    worksheet.Cells[row, 2].Value = gl.Key.AccountName;
                    if (gl.First().Account.FinancialStatementType == nameof(FinancialStatementType.BalanceSheet))
                    {
                        worksheet.Cells[row, 3].Value = gl.Sum(g => g.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                            g.Debit - g.Credit :
                            g.Credit - g.Debit);
                        worksheet.Cells[row, 3].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                    else
                    {
                        worksheet.Cells[row, 4].Value = gl.Sum(g => g.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                            g.Debit - g.Credit :
                            g.Credit - g.Debit);
                        worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                        if (nibit == 0)
                        {
                            nibit += (decimal)worksheet.Cells[row, 4].Value;
                            row++;
                            continue;
                        }

                        nibit -= (decimal)worksheet.Cells[row, 4].Value;
                    }

                    row++;
                }

                worksheet.Cells[row, 2].Value = "NIBIT";
                worksheet.Cells[row, 4].Value = nibit;
                worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(1).Width = 17;
                worksheet.Column(2).Width = 23;
                worksheet.Column(3).Width = 17;

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Level One Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate level one report.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(LevelOneReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult TrialBalanceReport()
        {
            return View();
        }

        #region -- Generated Trial Balance Report as Quest PDF

        public async Task<IActionResult> GenerateTrialBalanceReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(TrialBalanceReport));
            }

            try
            {
                var currentLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= model.DateFrom &&
                        gl.Date <= model.DateTo &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var priorLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date < model.DateFrom &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .OrderBy(coa => coa.AccountNumber)
                    .ToListAsync(cancellationToken);

                if (!currentLedgers.Any() || !priorLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(TrialBalanceReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Letter.Portrait());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(140).Column(column =>
                            {
                                column.Item().Text("FILPRIDE RESOURCES INC.").FontSize(16).SemiBold().AlignCenter();

                                column.Item().AlignCenter().Row(row =>
                                {
                                    row.Spacing(10);
                                    row.ConstantItem(150).Height(45)
                                        .Image(QuestPDF.Infrastructure.Image.FromFile(imgFilprideLogoPath)).FitHeight().FitWidth();
                                    row.Spacing(10);
                                });

                                column.Item().Text("TRIAL BALANCE").FontSize(14).SemiBold().AlignCenter();
                                column.Item().Text($"For the Perdiod {model.DateFrom.ToString("MMM dd")} to {model.DateTo.ToString(SD.Date_Format)}").SemiBold().AlignCenter();
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
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f);
                                    header.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("BEG BALANCES").SemiBold();
                                    header.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("TRANSACTIONS FOR THE PERIOD").SemiBold();
                                    header.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ENDING BALANCES").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Number").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Normal Balance").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Level").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CR").SemiBold();
                                });

                            #endregion

                             #region -- Loop to Show Records

                                 decimal totalBeginningDr = 0;
                                 decimal totalBeginningCr = 0;
                                 decimal totalcurrentDr = 0;
                                 decimal totalcurrentCr = 0;
                                 decimal totalEndingDr = 0;
                                 decimal totalEndingCr = 0;

                                foreach (var record in chartOfAccounts)
                                {
                                    decimal beginningDr = priorLedgers.Where(p => p.AccountNo == record.AccountNumber).Sum(p => p.Debit);
                                    decimal beginningCr = priorLedgers.Where(p => p.AccountNo == record.AccountNumber).Sum(p => p.Credit);
                                    decimal currentDr = currentLedgers.Where(p => p.AccountNo == record.AccountNumber).Sum(p => p.Debit);
                                    decimal currentCr = currentLedgers.Where(p => p.AccountNo == record.AccountNumber).Sum(p => p.Credit);

                                    table.Cell().Border(0.5f).Padding(3).Text(record.AccountNumber);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.AccountName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.NormalBalance);
                                    table.Cell().Border(0.5f).Padding(3).AlignCenter().Text(record.Level.ToString());
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningDr != 0 ? beginningDr.ToString(SD.Two_Decimal_Format) : null);
                                    totalBeginningDr += beginningDr;
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningCr != 0 ? beginningCr.ToString(SD.Two_Decimal_Format) : null);
                                    totalBeginningCr += beginningCr;
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentDr != 0 ? currentDr.ToString(SD.Two_Decimal_Format) : null);
                                    totalcurrentDr += currentDr;
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(currentCr != 0 ? currentCr.ToString(SD.Two_Decimal_Format) : null);
                                    totalcurrentCr += currentCr;

                                    decimal endingDr = beginningDr + currentDr - beginningCr - currentCr;
                                    decimal endingCr = beginningCr + currentCr - beginningDr - currentDr;

                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingDr != 0 ? endingDr.ToString(SD.Two_Decimal_Format) : null);
                                    totalEndingDr += endingDr;
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingCr != 0 ? endingCr.ToString(SD.Two_Decimal_Format) : null);
                                    totalEndingCr += endingCr;
                                }

                            #endregion

                            #region -- Create Table Cell for Totals

                               table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTALS").SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalBeginningDr.ToString(SD.Two_Decimal_Format)).SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalBeginningCr.ToString(SD.Two_Decimal_Format)).SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalcurrentDr.ToString(SD.Two_Decimal_Format)).SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalcurrentCr.ToString(SD.Two_Decimal_Format)).SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalEndingDr.ToString(SD.Two_Decimal_Format)).SemiBold();
                               table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalEndingCr.ToString(SD.Two_Decimal_Format)).SemiBold();

                               decimal beginningGrandTotal = totalBeginningDr - totalBeginningCr;
                               table.Cell().ColumnSpan(6).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(beginningGrandTotal != 0 ? beginningGrandTotal.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                               decimal currentGrandTotal = totalcurrentDr - totalcurrentCr;
                               table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(currentGrandTotal != 0 ? currentGrandTotal.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                               decimal endingGrandTotal = totalEndingDr - totalEndingCr;
                               table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(endingGrandTotal != 0 ? endingGrandTotal.ToString(SD.Two_Decimal_Format) : null).SemiBold();

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
                _logger.LogError(ex, "Failed to generate trial balance report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TrialBalanceReport));
            }
        }

        #endregion

        #region -- Trial Balance Report Excel File --

        public async Task<IActionResult> TrialBalanceReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var companyClaims = await GetCompanyClaimAsync();

                var currentLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= dateFrom &&
                        gl.Date <= dateTo &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var priorLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date < dateFrom &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .OrderBy(coa => coa.AccountNumber)
                    .ToListAsync(cancellationToken);

                if (!currentLedgers.Any() || !priorLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(TrialBalanceReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TrialBalance");
                var alignmentCenter = ExcelHorizontalAlignment.Center;


                // Set the column headers
                worksheet.Cells["B1"].Value = "FILPRIDE RESOURCES INC.";
                worksheet.Cells["B1"].Style.HorizontalAlignment = alignmentCenter;

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    worksheet.Column(2).Width = 80;
                    worksheet.Row(2).Height = 80;
                    var picture = await worksheet.Drawings.AddPictureAsync(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 1, 15);
                    picture.SetSize(330, 75);
                }

                worksheet.Cells["B3"].Value = "TRIAL BALANCE";
                worksheet.Cells["B3"].Style.HorizontalAlignment = alignmentCenter;
                worksheet.Cells["B4"].Value =
                    $"For the Period {dateFrom.ToString("MMM dd")} to {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B4"].Style.HorizontalAlignment = alignmentCenter;

                worksheet.Cells["A7"].Value = "ACCOUNT NUMBER";
                worksheet.Cells["B7"].Value = "ACCOUNT NAME";
                worksheet.Cells["C7"].Value = "NORMAL BALANCE";
                worksheet.Cells["D7"].Value = "LEVEL";

                var mergedCellBegBal = worksheet.Cells["E6:F6"];
                mergedCellBegBal.Merge = true;
                mergedCellBegBal.Value = "BEG BALANCES";
                mergedCellBegBal.Style.HorizontalAlignment = alignmentCenter;

                var mergedCellTransForThePeriod = worksheet.Cells["G6:H6"];
                mergedCellTransForThePeriod.Merge = true;
                mergedCellTransForThePeriod.Value = "TRANSACTIONS FOR THE PERIOD";
                mergedCellTransForThePeriod.Style.HorizontalAlignment = alignmentCenter;

                var mergedCellEndBal = worksheet.Cells["I6:J6"];
                mergedCellEndBal.Merge = true;
                mergedCellEndBal.Value = "ENDING BALANCES";
                mergedCellEndBal.Style.HorizontalAlignment = alignmentCenter;

                worksheet.Cells["E7"].Value = "DR";
                worksheet.Cells["F7"].Value = "CR";
                worksheet.Cells["G7"].Value = "DR";
                worksheet.Cells["H7"].Value = "CR";
                worksheet.Cells["I7"].Value = "DR";
                worksheet.Cells["J7"].Value = "CR";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:J7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Cells["E:J"].Style.Numberformat.Format = "#,##0.00_);[Red](#,##0.00)";

                int row = 8;
                decimal totalBeginningDr = 0;
                decimal totalBeginningCr = 0;
                decimal totalcurrentDr = 0;
                decimal totalcurrentCr = 0;
                decimal totalEndingDr = 0;
                decimal totalEndingCr = 0;


                foreach (var account in chartOfAccounts)
                {
                    decimal beginningDr = priorLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Debit);
                    decimal beginningCr = priorLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Credit);
                    decimal currentDr = currentLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Debit);
                    decimal currentCr = currentLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Credit);

                    worksheet.Cells[row, 1].Value = account.AccountNumber;
                    worksheet.Cells[row, 2].Value = account.AccountName;
                    worksheet.Cells[row, 3].Value = account.NormalBalance;
                    worksheet.Cells[row, 4].Value = account.Level;
                    worksheet.Cells[row, 5].Value = beginningDr != 0 ? beginningDr : null;
                    totalBeginningDr += beginningDr;
                    worksheet.Cells[row, 6].Value = beginningCr != 0 ? beginningCr : null;
                    totalBeginningCr += beginningCr;
                    worksheet.Cells[row, 7].Value = currentDr != 0 ? currentDr : null;
                    totalcurrentDr += currentDr;
                    worksheet.Cells[row, 8].Value = currentCr != 0 ? currentCr : null;
                    totalcurrentCr += currentCr;

                    decimal endingDr = beginningDr + currentDr - beginningCr - currentCr;
                    decimal endingCr = beginningCr + currentCr - beginningDr - currentDr;

                    worksheet.Cells[row, 9].Value = endingDr != 0 ? endingDr : null;
                    totalEndingDr += endingDr;
                    worksheet.Cells[row, 10].Value = endingCr != 0 ? endingCr : null;
                    totalEndingCr += endingCr;


                    row++;
                }

                row += 3;
                worksheet.Cells[row, 2].Value = "TOTALS";
                worksheet.Cells[row, 5].Value = totalBeginningDr;
                worksheet.Cells[row, 6].Value = totalBeginningCr;
                decimal beginningGrandTotal = totalBeginningDr - totalBeginningCr;
                worksheet.Cells[row + 1, 6].Value = beginningGrandTotal != 0 ? beginningGrandTotal : null;


                worksheet.Cells[row, 7].Value = totalcurrentDr;
                worksheet.Cells[row, 8].Value = totalcurrentCr;
                decimal currentGrandTotal = totalcurrentDr - totalcurrentCr;
                worksheet.Cells[row + 1, 8].Value = currentGrandTotal != 0 ? currentGrandTotal : null;


                worksheet.Cells[row, 9].Value = totalEndingDr;
                worksheet.Cells[row, 10].Value = totalEndingCr;
                decimal endingGrandTotal = totalEndingDr - totalEndingCr;
                worksheet.Cells[row + 1, 10].Value = endingGrandTotal != 0 ? endingGrandTotal : null;

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                using (var range = worksheet.Cells[row, 5, row, 10])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Trial Balance Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate trial balance report.");
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(TrialBalanceReport));
            }
        }

        #endregion -- Trial Balance Report Excel File --


        [HttpGet]
        public IActionResult BalanceSheetReport()
        {
            return View();
        }

        #region -- Generated Balance Sheet Report as Quest PDF

        public async Task<IActionResult> GenerateBalanceSheetReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(BalanceSheetReport));
            }

            try
            {
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Include(coa => coa.Children)
                    .OrderBy(coa => coa.AccountNumber)
                    .Where(coa => coa.FinancialStatementType == nameof(FinancialStatementType.BalanceSheet))
                    .ToListAsync(cancellationToken);

                var nibitForThePeriod = await _dbContext.FilprideMonthlyNibits
                    .FirstOrDefaultAsync(m => m.Year == monthDate.Year &&
                                              m.Month == monthDate.Month &&
                                              m.Company == companyClaims, cancellationToken);

                if (nibitForThePeriod == null)
                {
                    if (nibitForThePeriod == null)
                    {
                        TempData["error"] = "NIBIT For The Period not found. Contact MIS-Enterprise.";
                        return RedirectToAction(nameof(BalanceSheetReport));
                    }
                }

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(BalanceSheetReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                            page.Size(PageSizes.Letter.Portrait());
                            page.Margin(20);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion

                        #region -- Header

                            var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                            page.Header().Height(140).Column(column =>
                            {
                                column.Item().Text("FILPRIDE RESOURCES INC.").FontSize(16).SemiBold().AlignCenter();

                                column.Item().AlignCenter().Row(row =>
                                {
                                    row.Spacing(10);
                                    row.ConstantItem(150).Height(45)
                                        .Image(QuestPDF.Infrastructure.Image.FromFile(imgFilprideLogoPath)).FitHeight().FitWidth();
                                    row.Spacing(10);
                                });

                                column.Item().Text("BALANCE SHEET").FontSize(14).SemiBold().AlignCenter();
                                column.Item().Text($"As Of {monthDate.ToString(SD.Date_Format)}").SemiBold().AlignCenter();
                            });



                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            #region -- Columns Definition

                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.ConstantColumn(20);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                });

                            #endregion

                            #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L1").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L2").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L3").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L4").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).BorderTop(0.5f).BorderLeft(0.5f).BorderBottom(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("L5").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).BorderTop(0.5f).BorderRight(0.5f);
                                });

                            #endregion

                             #region -- Loop to Show Records

                                 decimal totalAsset = 0;
                                 decimal totalLiabilitiesAndEquity = 0;

                                foreach (var record in chartOfAccounts.Where(a => a.IsMain))
                                {
                                    decimal grandTotal = 0;

                                    table.Cell().ColumnSpan(6).Border(0.5f).Padding(3).Text(record.AccountName);

                                    foreach (var levelTwo in record.Children.OrderBy(l => l.AccountNumber))
                                    {
                                        decimal subTotal = 0;
                                        table.Cell().BorderLeft(0.5f).BorderBottom(0.5f);
                                        table.Cell().ColumnSpan(5).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelTwo.AccountName);

                                        foreach (var levelThree in levelTwo.Children)
                                        {
                                            table.Cell().ColumnSpan(2).BorderLeft(0.5f).BorderBottom(0.5f);
                                            table.Cell().ColumnSpan(4).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelThree.AccountName);

                                            foreach (var levelFour in levelThree.Children)
                                            {
                                                if (levelFour.AccountName.Contains("Retained Earnings"))
                                                {
                                                    table.Cell().ColumnSpan(3).BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text("Retained Earnings Beg");
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(nibitForThePeriod.BeginningBalance != 0 ? nibitForThePeriod.BeginningBalance < 0 ? $"({Math.Abs(nibitForThePeriod.BeginningBalance).ToString(SD.Two_Decimal_Format)})" : nibitForThePeriod.BeginningBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(nibitForThePeriod.BeginningBalance < 0 ? Colors.Red.Medium : Colors.Black);

                                                    table.Cell().ColumnSpan(3).BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text("Net Income for the Period");
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(nibitForThePeriod.NetIncome != 0 ? nibitForThePeriod.NetIncome < 0 ? $"({Math.Abs(nibitForThePeriod.NetIncome).ToString(SD.Two_Decimal_Format)})" : nibitForThePeriod.NetIncome.ToString(SD.Two_Decimal_Format) : null).FontColor(nibitForThePeriod.NetIncome < 0 ? Colors.Red.Medium : Colors.Black);
                                                    continue;

                                                }

                                                if (levelFour.AccountName.Contains("Prior Period"))
                                                {
                                                    table.Cell().ColumnSpan(3).BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelFour.AccountName);
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(nibitForThePeriod.PriorPeriodAdjustment != 0 ? nibitForThePeriod.PriorPeriodAdjustment < 0 ? $"({Math.Abs(nibitForThePeriod.PriorPeriodAdjustment).ToString(SD.Two_Decimal_Format)})" : nibitForThePeriod.PriorPeriodAdjustment.ToString(SD.Two_Decimal_Format) : null).FontColor(nibitForThePeriod.PriorPeriodAdjustment < 0 ? Colors.Red.Medium : Colors.Black);

                                                    table.Cell().BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().ColumnSpan(4).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text("Retained Earnings End");
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(nibitForThePeriod.EndingBalance != 0 ? nibitForThePeriod.EndingBalance < 0 ? $"({Math.Abs(nibitForThePeriod.EndingBalance).ToString(SD.Two_Decimal_Format)})" : nibitForThePeriod.EndingBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(nibitForThePeriod.EndingBalance < 0 ? Colors.Red.Medium : Colors.Black);

                                                    subTotal += nibitForThePeriod.EndingBalance;

                                                    continue;
                                                }

                                                table.Cell().ColumnSpan(3).BorderLeft(0.5f).BorderBottom(0.5f);
                                                table.Cell().ColumnSpan(2).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelFour.AccountName);
                                                var levelFourBalance = generalLedgers
                                                    .Where(gl =>
                                                        gl.AccountNo == levelFour.AccountNumber)
                                                    .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                                        gl.Debit - gl.Credit :
                                                        gl.Credit - gl.Debit);
                                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(levelFourBalance != 0 ? levelFourBalance < 0 ? $"({Math.Abs(levelFourBalance).ToString(SD.Two_Decimal_Format)})" : levelFourBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(levelFourBalance < 0 ? Colors.Red.Medium : Colors.Black);
                                                subTotal += levelFourBalance;

                                                foreach (var levelFive in levelFour.Children)
                                                {
                                                    table.Cell().ColumnSpan(4).BorderLeft(0.5f).BorderBottom(0.5f);
                                                    table.Cell().BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text(levelFive.AccountName);
                                                    var levelFiveBalance = generalLedgers
                                                        .Where(gl =>
                                                            gl.AccountNo == levelFive.AccountNumber)
                                                        .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                                            gl.Debit - gl.Credit :
                                                            gl.Credit - gl.Debit);
                                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(levelFiveBalance != 0 ? levelFiveBalance < 0 ? $"({Math.Abs(levelFiveBalance).ToString(SD.Two_Decimal_Format)})" : levelFiveBalance.ToString(SD.Two_Decimal_Format) : null).FontColor(levelFiveBalance < 0 ? Colors.Red.Medium : Colors.Black);
                                                }
                                            }
                                        }

                                        if (levelTwo.AccountName.Contains("Retained Earnings") ||
                                            levelTwo.AccountName.Contains("Prior Period"))
                                        {
                                            grandTotal += subTotal;
                                            continue;
                                        }

                                        table.Cell().BorderLeft(0.5f).BorderBottom(0.5f);
                                        table.Cell().ColumnSpan(4).BorderTop(0.5f).BorderRight(0.5f).BorderBottom(0.5f).Padding(3).Text($"TOTAL {levelTwo.AccountName.ToUpper()}").SemiBold();

                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(subTotal != 0 ? subTotal < 0 ? $"({Math.Abs(subTotal).ToString(SD.Two_Decimal_Format)})" : subTotal.ToString(SD.Two_Decimal_Format) : null).FontColor(subTotal < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        grandTotal += subTotal;
                                    }

                                    table.Cell().ColumnSpan(5).Border(0.5f).Padding(3).Text($"TOTAL {record.AccountName.ToUpper()}").Bold();

                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(grandTotal != 0 ? grandTotal < 0 ? $"({Math.Abs(grandTotal).ToString(SD.Two_Decimal_Format)})" : grandTotal.ToString(SD.Two_Decimal_Format) : null).Bold();

                                    if (record.AccountType == "Asset")
                                    {
                                        totalAsset += grandTotal;
                                    }
                                    else
                                    {
                                        totalLiabilitiesAndEquity += grandTotal;
                                    }

                                }

                                table.Cell().ColumnSpan(6).Border(0.5f).Text("");
                                table.Cell().ColumnSpan(5).Border(0.5f).Padding(3).Text("TOTAL LIABILITIES AND EQUITY").Bold();
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(totalLiabilitiesAndEquity.ToString(SD.Two_Decimal_Format)).Bold();
                                var difference = totalLiabilitiesAndEquity - totalAsset;
                                table.Cell().ColumnSpan(6).Border(0.5f).Padding(3).AlignRight().Text(difference != 0 ? difference < 0 ? $"({Math.Abs(difference).ToString(SD.Two_Decimal_Format)})" : difference.ToString(SD.Two_Decimal_Format) : null).FontColor(difference < 0 ? Colors.Red.Medium : Colors.Black).Bold();

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
                _logger.LogError(ex, "Failed to generate balance sheet report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(BalanceSheetReport));
            }
        }

        #endregion

        #region -- Generate Balance Sheet --

        public async Task<IActionResult> BalanceSheetReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var companyClaims = await GetCompanyClaimAsync();
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac!.ParentAccount) // Level 2
                    .ThenInclude(ac => ac!.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fixed
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .Include(coa => coa.Children)
                    .OrderBy(coa => coa.AccountNumber)
                    .Where(coa => coa.FinancialStatementType == nameof(FinancialStatementType.BalanceSheet))
                    .ToListAsync(cancellationToken);

                var nibitForThePeriod = await _dbContext.FilprideMonthlyNibits
                    .FirstOrDefaultAsync(m => m.Year == monthDate.Year &&
                                              m.Month == monthDate.Month &&
                                              m.Company == companyClaims, cancellationToken);

                if (nibitForThePeriod == null)
                {
                    if (nibitForThePeriod == null)
                    {
                        TempData["error"] = "NIBIT For The Period not found. Contact MIS-Enterprise.";
                        return RedirectToAction(nameof(BalanceSheetReport));
                    }
                }

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(BalanceSheetReport));
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Balance Sheet Report");
                string currencyFormat = "#,##0.00_);[Red](#,##0.00)";
                worksheet.View.FreezePanes(5, 1);
                int row = 1;

                #region == Column Header ==

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES INC.";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;


                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    var picture = await worksheet.Drawings.AddPictureAsync(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 4, 10);
                    picture.SetSize(330, 75);
                }

                worksheet.Row(row).Height = 80;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                }
                row++;


                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "BALANCE SHEET";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "As of " + monthDate.ToString("MMM dd, yyyy");
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row += 2;
                worksheet.Cells[row, 1].Value = "L1";
                worksheet.Cells[row, 2].Value = "L2";
                worksheet.Cells[row, 3].Value = "L3";
                worksheet.Cells[row, 4].Value = "L4";
                worksheet.Cells[row, 5].Value = "L5";

                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

                row++;

                #endregion

                decimal totalAsset = 0;
                decimal totalLiabilitiesAndEquity = 0;

                foreach (var account in chartOfAccounts.Where(a => a.IsMain))
                {
                    decimal grandTotal = 0;

                    worksheet.Cells[row, 1].Value = account.AccountName;
                    row++;

                    foreach (var levelTwo in account.Children.OrderBy(l => l.AccountNumber))
                    {
                        decimal subTotal = 0;
                        worksheet.Cells[row, 2].Value = levelTwo.AccountName;
                        row++;

                        foreach (var levelThree in levelTwo.Children)
                        {
                            worksheet.Cells[row, 3].Value = levelThree.AccountName;
                            row++;

                            foreach (var levelFour in levelThree.Children)
                            {
                                if (levelFour.AccountName.Contains("Retained Earnings"))
                                {
                                    worksheet.Cells[row, 4].Value = "Retained Earnings Beg";
                                    worksheet.Cells[row, 6].Value = nibitForThePeriod.BeginningBalance != 0 ? nibitForThePeriod.BeginningBalance : null;
                                    row++;

                                    worksheet.Cells[row, 4].Value = "Net Income for the Period";
                                    worksheet.Cells[row, 6].Value = nibitForThePeriod.NetIncome != 0 ? nibitForThePeriod.NetIncome : null;

                                    row++;
                                    continue;

                                }

                                if (levelFour.AccountName.Contains("Prior Period"))
                                {
                                    worksheet.Cells[row, 4].Value = levelFour.AccountName;
                                    worksheet.Cells[row, 6].Value = nibitForThePeriod.PriorPeriodAdjustment != 0 ? nibitForThePeriod.PriorPeriodAdjustment : null;
                                    row++;

                                    worksheet.Cells[row, 2].Value = "Retained Earnings End";
                                    worksheet.Cells[row, 6].Value = nibitForThePeriod.EndingBalance != 0 ? nibitForThePeriod.EndingBalance : null;

                                    subTotal += nibitForThePeriod.EndingBalance;
                                    row++;

                                    continue;
                                }

                                worksheet.Cells[row, 4].Value = levelFour.AccountName;
                                var levelFourBalance = generalLedgers
                                    .Where(gl =>
                                        gl.AccountNo == levelFour.AccountNumber)
                                    .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                        gl.Debit - gl.Credit :
                                        gl.Credit - gl.Debit);
                                worksheet.Cells[row, 6].Value = levelFourBalance != 0 ? levelFourBalance : null;
                                subTotal += levelFourBalance;
                                row++;

                                foreach (var levelFive in levelFour.Children)
                                {
                                    worksheet.Cells[row, 5].Value = levelFive.AccountName;
                                    var levelFiveBalance = generalLedgers
                                        .Where(gl =>
                                            gl.AccountNo == levelFive.AccountNumber)
                                        .Sum(gl => gl.Account.NormalBalance == nameof(NormalBalance.Debit) ?
                                            gl.Debit - gl.Credit :
                                            gl.Credit - gl.Debit);
                                    worksheet.Cells[row, 6].Value = levelFiveBalance != 0 ? levelFiveBalance : null;
                                    row++;
                                }
                            }
                        }

                        if (levelTwo.AccountName.Contains("Retained Earnings") ||
                            levelTwo.AccountName.Contains("Prior Period"))
                        {
                            grandTotal += subTotal;
                            continue;
                        }

                        worksheet.Cells[row, 2].Value = $"TOTAL {levelTwo.AccountName.ToUpper()}";
                        worksheet.Cells[row, 2].Style.Font.Bold = true;

                        worksheet.Cells[row, 6].Value = subTotal != 0 ? subTotal : null;
                        worksheet.Cells[row, 6].Style.Font.Bold = true;
                        worksheet.Cells[row, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[row, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        grandTotal += subTotal;
                        row++;
                    }

                    worksheet.Cells[row, 1].Value = $"TOTAL {account.AccountName.ToUpper()}";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;

                    worksheet.Cells[row, 6].Value = grandTotal != 0 ? grandTotal : null;
                    worksheet.Cells[row, 6].Style.Font.Bold = true;
                    worksheet.Cells[row, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells[row, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    row++;

                    if (account.AccountType == "Asset")
                    {
                        totalAsset += grandTotal;
                    }
                    else
                    {
                        totalLiabilitiesAndEquity += grandTotal;
                    }


                }

                worksheet.Cells[row + 1, 1].Value = "TOTAL LIABILITIES AND EQUITY";
                worksheet.Cells[row + 1, 6].Value = totalLiabilitiesAndEquity;
                var difference = totalLiabilitiesAndEquity - totalAsset;
                worksheet.Cells[row + 3, 6].Value = difference != 0 ? difference : null;

                worksheet.Cells[row + 1, 1].Style.Font.Bold = true;
                worksheet.Cells[row + 1, 6].Style.Font.Bold = true;
                worksheet.Cells[row + 1, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[row + 1, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;



                worksheet.Cells["F"].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells.AutoFitColumns();
                for (int i = 1; i <= 4; i++)
                {
                    worksheet.Column(i).Width = 4.5;
                }
                worksheet.Column(5).Width = 50;

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Balance Sheet Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate balance sheet report.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(BalanceSheetReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult StatementOfRetainedEarningsReport()
        {
            return View();
        }


        #region -- Generate SRE Report --

        public async Task<IActionResult> StatementOfRetainedEarningsReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var companyClaims = await GetCompanyClaimAsync();

                var nibitForThePeriod = await _dbContext.FilprideMonthlyNibits
                    .FirstOrDefaultAsync(m => m.Year == monthDate.Year &&
                                              m.Month == monthDate.Month &&
                                              m.Company == companyClaims, cancellationToken);

                if (nibitForThePeriod == null)
                {
                    TempData["error"] = "NIBIT For The Period not found. Contact MIS-Enterprise.";
                    return RedirectToAction(nameof(StatementOfRetainedEarningsReport));
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("SRE Report");
                string currencyFormat = "#,##0.00_);[Red](#,##0.00)";
                worksheet.View.FreezePanes(5, 1);
                int row = 1;

                #region == Column Header ==

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES INC.";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;


                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    var picture = await worksheet.Drawings.AddPictureAsync(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 4, 10);
                    picture.SetSize(330, 75);
                }

                worksheet.Row(row).Height = 80;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                }
                row++;


                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "STATEMENT OF RETAINED EARNINGS";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Value = "As of " + monthDate.ToString("MMM dd, yyyy");
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row++;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Merge = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                row += 2;
                worksheet.Cells[row, 1].Value = "L1";
                worksheet.Cells[row, 2].Value = "L2";
                worksheet.Cells[row, 3].Value = "L3";
                worksheet.Cells[row, 4].Value = "L4";
                worksheet.Cells[row, 5].Value = "L5";

                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;

                row++;

                #endregion

                worksheet.Cells[8, 2].Value = "Retained Earnings";
                worksheet.Cells[8, 2].Style.Font.Bold = true;

                worksheet.Cells[9, 3].Value = "Retained Earnings";

                worksheet.Cells[10, 4].Value = "Retained Earnings Beg";
                worksheet.Cells[10, 6].Value = nibitForThePeriod.BeginningBalance != 0 ? nibitForThePeriod.BeginningBalance : null;
                worksheet.Cells[10, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[10, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells[11, 4].Value = "Net Income for the Period";
                worksheet.Cells[11, 6].Value = nibitForThePeriod.NetIncome != 0 ? nibitForThePeriod.NetIncome : null;
                worksheet.Cells[11, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[11, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells[12, 2].Value = "Prior Period Adjustment";
                worksheet.Cells[12, 2].Style.Font.Bold = true;

                worksheet.Cells[13, 3].Value = "Prior Period Adjustment";

                worksheet.Cells[14, 4].Value = "Prior Period Adjustment";
                worksheet.Cells[14, 6].Value = nibitForThePeriod.PriorPeriodAdjustment != 0 ? nibitForThePeriod.PriorPeriodAdjustment : null;
                worksheet.Cells[14, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[14, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells[15, 2].Value = "Retained Earnings End";
                worksheet.Cells[15, 2].Style.Font.Bold = true;
                worksheet.Cells[15, 6].Value = nibitForThePeriod.EndingBalance != 0 ? nibitForThePeriod.EndingBalance : null;
                worksheet.Cells[15, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[15, 6].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells["F"].Style.Font.Bold = true;
                worksheet.Cells["F"].Style.Numberformat.Format = currencyFormat;

                worksheet.Cells.AutoFitColumns();
                for (int i = 1; i <= 4; i++)
                {
                    worksheet.Column(i).Width = 4.5;
                }
                worksheet.Column(5).Width = 50;

                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SRE Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate statement of retained Earnings report.");
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(StatementOfRetainedEarningsReport));
            }
        }

        #endregion

    }
}
