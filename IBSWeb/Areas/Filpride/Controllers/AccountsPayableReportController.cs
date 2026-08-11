using System.Globalization;
using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.Integrated;
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
using Color = System.Drawing.Color;
using DateTime = System.DateTime;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class AccountsPayableReportController : Controller
    {
        private sealed class PurchaseSummaryMetric
        {
            public decimal Quantity { get; set; }
            public decimal NetOfAmount { get; set; }
        }

        private sealed class PurchaseOrderSummaryMetric
        {
            public decimal OriginalPo { get; set; }
            public decimal UnliftedLastMonth { get; set; }
            public decimal LiftedThisMonth { get; set; }
            public decimal UnliftedThisMonth { get; set; }
            public decimal GrossAmount { get; set; }
            public decimal EwtAmount { get; set; }
        }

        private sealed class GrossMarginSummaryMetric
        {
            public decimal Quantity { get; set; }
            public decimal NetOfSales { get; set; }
            public decimal NetOfPurchases { get; set; }
            public decimal GrossMargin { get; set; }
            public decimal NetOfFreight { get; set; }
            public decimal Commission { get; set; }
            public decimal NetMargin { get; set; }
        }

        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public AccountsPayableReportController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private string GetUserFullName()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                   ?? User.Identity?.Name!;
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

        private static string NormalizeStatusFilter(string? statusFilter) => statusFilter switch
        {
            "All" => "All",
            "InvalidOnly" => "InvalidOnly",
            _ => "ValidOnly"
        };

        private static string GetStatusFilterLabel(string statusFilter) => statusFilter switch
        {
            "All" => "All (Include Voided)",
            "InvalidOnly" => "Voided Only",
            _ => "Valid Only (Exclude Voided)"
        };

        private static decimal RoundToFour(decimal value) => DecimalRoundingHelper.RoundToFour(value);

        private static decimal DivideOrZero(decimal dividend, decimal divisor) => DecimalRoundingHelper.DivideOrZero(dividend, divisor);

        private static decimal NetOfVatOrZero(decimal grossAmount) => DecimalRoundingHelper.ComputeNetOfVat(grossAmount);

        private static decimal VatAmountOrZero(decimal netOfVatAmount) => DecimalRoundingHelper.ComputeVatAmount(netOfVatAmount);

        private static decimal EwtAmountOrZero(decimal netOfVatAmount, decimal percent) => DecimalRoundingHelper.ComputeEwtAmount(netOfVatAmount, percent);

        private static decimal NetUnitValueOrZero(decimal grossAmount, decimal quantity) => DecimalRoundingHelper.ComputeNetUnitValue(grossAmount, quantity);

        private static List<string> GetOrderedProductNames<T>(IEnumerable<T> records, Func<T, string?> selector)
        {
            return records
                .Select(selector)
                .Where(productName => !string.IsNullOrWhiteSpace(productName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(productName => productName, StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
        }

        private static decimal ComputeAverage(decimal amount, decimal quantity)
        {
            return amount != 0m && quantity != 0m
                ? DivideOrZero(amount, quantity)
                : 0m;
        }

        private static Dictionary<string, PurchaseSummaryMetric> CreatePurchaseSummaryMetricMap(IEnumerable<string> productNames)
        {
            return productNames.ToDictionary(
                productName => productName,
                _ => new PurchaseSummaryMetric(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, PurchaseOrderSummaryMetric> CreatePurchaseOrderSummaryMetricMap(IEnumerable<string> productNames)
        {
            return productNames.ToDictionary(
                productName => productName,
                _ => new PurchaseOrderSummaryMetric(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, GrossMarginSummaryMetric> CreateGrossMarginSummaryMetricMap(IEnumerable<string> productNames)
        {
            return productNames.ToDictionary(
                productName => productName,
                _ => new GrossMarginSummaryMetric(),
                StringComparer.OrdinalIgnoreCase);
        }

        [HttpGet]
        public IActionResult ClearedDisbursementReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult NonTradeInvoiceReport()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CvDisbursementReport()
        {
            return View();
        }

        #region -- Generated Cleared Disbursement Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedClearedDisbursementReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }

            try
            {
                var checkVoucherHeader = await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken);

                if (checkVoucherHeader.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(ClearedDisbursementReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page Setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion -- Page Setup

                        #region -- Header

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

                        #endregion -- Header

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
                            });

                            #endregion -- Columns Definition

                            #region -- Table Header

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Category").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Subcategory").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Payee").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Voucher#").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Bank Name").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Check").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Particulars").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Total").SemiBold();
                            });

                            #endregion -- Table Header

                            #region -- Loop to Show Records

                            var totalAmt = 0m;
                            foreach (var record in checkVoucherHeader)
                            {
                                table.Cell().Border(0.5f).Padding(3).Text("Empty");
                                table.Cell().Border(0.5f).Padding(3).Text("Empty");
                                table.Cell().Border(0.5f).Padding(3).Text(record.Payee);
                                table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Border(0.5f).Padding(3).Text(record.CheckVoucherHeaderNo);
                                table.Cell().Border(0.5f).Padding(3).Text(record.BankAccountName);
                                table.Cell().Border(0.5f).Padding(3).Text(record.CheckNo);
                                table.Cell().Border(0.5f).Padding(3).Text(record.Particulars);
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Total != 0 ? record.Total < 0 ? $"({Math.Abs(record.Total).ToString(SD.Two_Decimal_Format)})" : record.Total.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Total < 0 ? Colors.Red.Medium : Colors.Black);
                                totalAmt += record.Total;
                            }

                            #endregion -- Loop to Show Records

                            #region Create Table Cell for Totals

                            table.Cell().ColumnSpan(8).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                            table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmt != 0 ? totalAmt < 0 ? $"({Math.Abs(totalAmt).ToString(SD.Two_Decimal_Format)})" : totalAmt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalAmt < 0 ? Colors.Red.Medium : Colors.Black);

                            #endregion Create Table Cell for Totals
                        });

                        #endregion -- Content

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion -- Footer
                    });
                });

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate cleared disbursement report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cleared disbursement report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }

        #endregion -- Generated Cleared Disbursement Report as Quest PDF

        #region -- Generate Cleared Disbursement Report as Excel File --

        public async Task<IActionResult> GenerateClearedDisbursementReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var clearedDisbursementReport =
                    await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo,
                        companyClaims, cancellationToken);

                if (clearedDisbursementReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
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
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

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
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                var row = 8;
                var currencyFormat = "#,##0.00";

                var coaLookup = await _dbContext.FilprideChartOfAccounts
                    .AsNoTracking()
                    .Include(coa => coa.ParentAccount)
                        .ThenInclude(a => a!.ParentAccount)
                        .ThenInclude(a => a!.ParentAccount)
                    .ToDictionaryAsync(c => c.AccountNumber!, cancellationToken);

                foreach (var cd in clearedDisbursementReport)
                {
                    var invoiceDebit = cd.Details!
                        .Where(d => !d.IsDisplayEntry)
                        .OrderByDescending(d => d.Debit)
                        .FirstOrDefault();

                    if (invoiceDebit == null)
                    {
                        return BadRequest();
                    }

                    if (!coaLookup.TryGetValue(invoiceDebit.AccountNo, out var coa))
                    {
                        continue;
                    }

                    var levelOneAccount = coa?.ParentAccount?.ParentAccount?.ParentAccount;

                    worksheet.Cells[row, 1].Value = $"{levelOneAccount?.AccountNumber} " +
                                                    $"{levelOneAccount?.AccountName}";
                    worksheet.Cells[row, 2].Value = $"{invoiceDebit.AccountNo} {invoiceDebit.AccountName}";
                    worksheet.Cells[row, 3].Value = cd.Payee;
                    worksheet.Cells[row, 4].Value = cd.Date;
                    worksheet.Cells[row, 5].Value = cd.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 6].Value = cd.BankAccountName;
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

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate cleared disbursement report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Cleared_Disbursement_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cleared disbursement report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }

        #endregion -- Generate Cleared Disbursement Report as Excel File --

        #region -- Generate NonTrade Invoice Report as Excel File --

        public async Task<IActionResult> GenerateNonTradeInvoiceReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(NonTradeInvoiceReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }
                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var nonTradeInvoiceReport =
                    await _dbContext.FilprideCheckVoucherDetails
                        .AsNoTracking()
                        .Where(cvd => cvd.CheckVoucherHeader!.Company == companyClaims
                                      && cvd.CheckVoucherHeader.CvType == nameof(CVType.Invoicing)
                                      && cvd.CheckVoucherHeader.Date >= dateFrom &&
                                      cvd.CheckVoucherHeader.Date <= dateTo
                                      && (statusFilter == "ValidOnly"
                                          ? cvd.CheckVoucherHeader.PostedBy != null
                                          : statusFilter != "InvalidOnly" || cvd.CheckVoucherHeader.VoidedBy != null))
                        .Include(cvd => cvd.CheckVoucherHeader)
                        .ThenInclude(cvh => cvh!.Supplier)
                        .OrderBy(cvd => cvd.CheckVoucherHeader!.Date)
                        .ThenBy(cvd => cvd.CheckVoucherHeader!.CheckVoucherHeaderNo)
                        .ThenByDescending(cvd => cvd.Debit)
                        .ToListAsync(cancellationToken);

                var nonTradeNos = nonTradeInvoiceReport
                    .Select(x => x.TransactionNo)
                    .ToList();

                var payments = await _dbContext.FilprideCheckVoucherHeaders
                    .AsNoTracking()
                    .Where(x =>
                        x.PostedBy != null &&
                        x.Reference != null &&
                        nonTradeNos.Contains(x.Reference) &&
                        x.Company == companyClaims)
                    .Select(x => new
                    {
                        x.Reference,
                        x.CheckVoucherHeaderNo,
                        x.DcrDate,
                        x.BankAccount,
                        x.BankAccountNumber,
                        x.BankAccountName
                    })
                    .ToListAsync(cancellationToken);

                var paymentDict = payments
                    .GroupBy(x => x.Reference)
                    .ToDictionary(g => g.Key!, g => g.First());

                if (nonTradeInvoiceReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(NonTradeInvoiceReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("APNonTradeInvoiceReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "AP NON-TRADE INVOICE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value =
                    $"{dateFrom.ToString("MMM dd, yyyy")} - {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                const int dateColumn = 1;
                const int dcrDateColumn = 4;
                const int subAccountNameColumn = 13;
                const int debitColumn = 14;
                const int creditColumn = 15;
                const int statusColumn = 16;

                var row = 7;
                var col = 1;

                worksheet.Cells[row, col].Value = "DATE"; col++;
                worksheet.Cells[row, col].Value = "INV NO."; col++;
                worksheet.Cells[row, col].Value = "CV PAYMENT"; col++;
                worksheet.Cells[row, col].Value = "DCR"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "BANK"; col++;
                worksheet.Cells[row, col].Value = "BANK ACCOUNT #"; col++;
                worksheet.Cells[row, col].Value = "BANK ACCOUNT NAME"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;
                worksheet.Cells[row, col].Value = "ACCOUNT NUMBER"; col++;
                worksheet.Cells[row, col].Value = "ACCOUNT NAME"; col++;
                worksheet.Cells[row, col].Value = "SUB ACCOUNT NAME"; col++;
                worksheet.Cells[row, col].Value = "DEBIT"; col++;
                worksheet.Cells[row, col].Value = "CREDIT"; col++;
                worksheet.Cells[row, col].Value = "STATUS";

                if (showVoidCancelColumns)
                {
                    col++;
                    worksheet.Cells[row, col].Value = "VOIDED BY"; col++;
                    worksheet.Cells[row, col].Value = "VOIDED DATE";
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                }

                int lastHeaderCol = showVoidCancelColumns ? statusColumn + 2 : statusColumn;

                using (var range = worksheet.Cells[row, 1, row, lastHeaderCol])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var currencyFormat = "#,##0.00";
                var totalCredit = 0m;
                var totalDebit = 0m;

                foreach (var inv in nonTradeInvoiceReport)
                {
                    var paymentInfo = paymentDict.GetValueOrDefault(inv.TransactionNo);
                    col = 1;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader!.Date.ToDateTime(TimeOnly.MinValue);
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy"; col++;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.CheckVoucherHeaderNo; col++;
                    worksheet.Cells[row, col].Value = paymentInfo?.CheckVoucherHeaderNo ?? ""; col++;
                    if (paymentInfo?.DcrDate.HasValue == true)
                    {
                        worksheet.Cells[row, col].Value = paymentInfo.DcrDate.Value.ToDateTime(TimeOnly.MinValue);
                        worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }
                    col++;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.Payee; col++;
                    worksheet.Cells[row, col].Value = paymentInfo?.BankAccount!.Bank; col++;
                    worksheet.Cells[row, col].Value = paymentInfo?.BankAccountNumber; col++;
                    worksheet.Cells[row, col].Value = paymentInfo?.BankAccountName; col++;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.Particulars;col++;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.Type; col++;
                    worksheet.Cells[row, col].Value = inv.AccountNo; col++;
                    worksheet.Cells[row, col].Value = inv.AccountName; col++;
                    worksheet.Cells[row, col].Value = inv.SubAccountName; col++;
                    worksheet.Cells[row, col].Value = inv.Debit;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat; col++;
                    worksheet.Cells[row, col].Value = inv.Credit;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat; col++;
                    worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.Status;

                    if (showVoidCancelColumns)
                    {
                        col++;
                        worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.VoidedBy; col++;
                        worksheet.Cells[row, col].Value = inv.CheckVoucherHeader.VoidedDate;
                        worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    worksheet.Cells[row, dateColumn].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, dcrDateColumn].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, debitColumn].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, creditColumn].Style.Numberformat.Format = currencyFormat;

                    totalCredit += inv.Credit;
                    totalDebit += inv.Debit;

                    row++;
                }

                int totalRow = row;
                int lastDataCol = showVoidCancelColumns ? statusColumn + 2 : statusColumn;
                worksheet.Cells[totalRow, subAccountNameColumn].Value = "TOTAL: ";
                worksheet.Cells[totalRow, debitColumn].Value = totalDebit;
                worksheet.Cells[totalRow, creditColumn].Value = totalCredit;

                using (var range = worksheet.Cells[totalRow, 1, totalRow, lastDataCol])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[totalRow, debitColumn, totalRow, creditColumn])
                {
                    range.Style.Numberformat.Format = currencyFormat;
                }

                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Non-Trade Invoice report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"NonTrade_Invoice_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate non trade invoice report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(NonTradeInvoiceReport));
            }
        }

        #endregion -- Generate NonTrade Invoice Report as Excel File --

        #region -- Generate CV Disbursement Report as Excel File --

        public async Task<IActionResult> GenerateCvDisbursementReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(CvDisbursementReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var cvTradeHeaderReport = await _dbContext.FilprideCheckVoucherHeaders
                        .AsNoTracking()
                        .Where(cvh =>
                            cvh.Company == companyClaims &&
                            cvh.CvType != nameof(CVType.Invoicing) &&
                            cvh.Date >= dateFrom &&
                            cvh.Date <= dateTo
                            && (statusFilter == "ValidOnly"
                                ? cvh.PostedBy != null
                                : statusFilter != "InvalidOnly" || cvh.VoidedBy != null))
                        .Include(cvh => cvh.Details!)
                        .Include(cvh => cvh.Supplier)
                        .Include(cvh => cvh.BankAccount)
                        .OrderBy(cvh => cvh.Date)
                        .ThenBy(cvh => cvh.CheckVoucherHeaderNo)
                        .ToListAsync(cancellationToken);

                var cvTradeHeaderIds = cvTradeHeaderReport.Select(cvh => cvh.CheckVoucherHeaderId).ToList();
                var cvTradePayments = await _dbContext.FilprideCVTradePayments.Where(cvp => cvTradeHeaderIds.Contains(cvp.CheckVoucherId)).ToListAsync(cancellationToken);

                var supplierIds = cvTradeHeaderReport.Where(cvh => cvh.Category == "Trade" && cvh.CvType == "Supplier").Select(cvh => cvh.CheckVoucherHeaderId).ToList();
                var receivingReportIds = cvTradePayments.Where(cvp => supplierIds.Contains(cvp.CheckVoucherId)).Select(cvp => cvp.DocumentId).ToList();
                var receivingReports = await _unitOfWork.FilprideReceivingReport.GetAllAsync(dr => receivingReportIds.Contains(dr.ReceivingReportId), cancellationToken);

                var notSupplierIds = cvTradeHeaderReport.Where(cvh => cvh.Category == "Trade" && cvh.CvType != "Supplier").Select(cvh => cvh.CheckVoucherHeaderId).ToList();
                var deliveryReceiptIds = cvTradePayments.Where(cvp => notSupplierIds.Contains(cvp.CheckVoucherId)).Select(cvp => cvp.DocumentId).ToList();
                var deliveryReceipts = await _unitOfWork.FilprideDeliveryReceipt.GetAllAsync(dr => deliveryReceiptIds.Contains(dr.DeliveryReceiptId), cancellationToken);

                if (cvTradeHeaderReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(CvDisbursementReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("CvDisbursementReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "CV DISBURSEMENT REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom.ToString("MMM dd, yyyy")} - {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                const int dateColumn = 1;
                const int dcrDateColumn = 3;
                const int subAccountNameColumn = 14;
                const int debitColumn = 15;
                const int creditColumn = 16;
                const int statusColumn = 17;

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "DATE"; col++;
                worksheet.Cells[row, col].Value = "CV No."; col++;
                worksheet.Cells[row, col].Value = "DCR DATE"; col++;
                worksheet.Cells[row, col].Value = "CHECK #"; col++;
                worksheet.Cells[row, col].Value = "BANK"; col++;
                worksheet.Cells[row, col].Value = "BANK ACCOUNT #"; col++;
                worksheet.Cells[row, col].Value = "BANK NAME"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;
                worksheet.Cells[row, col].Value = "INVOICE No."; col++;
                worksheet.Cells[row, col].Value = "ACCOUNT NUMBER"; col++;
                worksheet.Cells[row, col].Value = "ACCOUNT NAME"; col++;
                worksheet.Cells[row, col].Value = "SUB ACCOUNT NAME"; col++;
                worksheet.Cells[row, col].Value = "DEBIT"; col++;
                worksheet.Cells[row, col].Value = "CREDIT"; col++;
                worksheet.Cells[row, col].Value = "STATUS";

                if (showVoidCancelColumns)
                {
                    col++;
                    worksheet.Cells[row, col].Value = "VOIDED BY"; col++;
                    worksheet.Cells[row, col].Value = "VOIDED DATE";
                }

                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var currencyFormat = "#,##0.00";
                var totalCredit = 0m;
                var totalDebit = 0m;

                foreach (var header in cvTradeHeaderReport)
                {
                    foreach (var details in header.Details!
                                 .Where(x => !x.IsDisplayEntry)
                                 .OrderByDescending(d => d.Debit))
                    {
                        col = 1;
                        worksheet.Cells[row, col].Value = header.Date.ToDateTime(TimeOnly.MinValue); col++;
                        worksheet.Cells[row, col].Value = header.CheckVoucherHeaderNo; col++;
                        worksheet.Cells[row, col].Value = header.DcrDate?.ToDateTime(TimeOnly.MinValue); col++;
                        worksheet.Cells[row, col].Value = header.CheckNo; col++;
                        worksheet.Cells[row, col].Value = header.BankAccount!.Bank; col++;
                        worksheet.Cells[row, col].Value = header.BankAccountNumber; col++;
                        worksheet.Cells[row, col].Value = header.BankAccountName; col++;
                        worksheet.Cells[row, col].Value = header.Payee; col++;
                        worksheet.Cells[row, col].Value = header.Particulars; col++;
                        worksheet.Cells[row, col].Value = header.Type; col++;

                        if (header.Category == "Trade")
                        {
                            var rrListOfString = new List<string>();

                            if (header.CvType == "Supplier")
                            {
                                var cvTradeRrs = cvTradePayments
                                    .Where(ctp => ctp.CheckVoucherId == header.CheckVoucherHeaderId)
                                    .ToList();

                                if (cvTradeRrs.Count > 0)
                                {
                                    foreach (var cvTradeRr in cvTradeRrs)
                                    {
                                        var rr = receivingReports.FirstOrDefault(r => r.ReceivingReportId == cvTradeRr.DocumentId);
                                        if (rr != null)
                                        {
                                            rrListOfString.Add(rr.ReceivingReportNo!);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var cvTradeDrs = cvTradePayments
                                    .Where(ctp => ctp.CheckVoucherId == header.CheckVoucherHeaderId)
                                    .ToList();

                                if (cvTradeDrs.Count > 0)
                                {
                                    foreach (var cvTradeRr in cvTradeDrs)
                                    {
                                        var rr = deliveryReceipts.FirstOrDefault(r => r.DeliveryReceiptId == cvTradeRr.DocumentId);
                                        if (rr != null)
                                        {
                                            rrListOfString.Add(rr.DeliveryReceiptNo);
                                        }
                                    }
                                }
                            }

                            if (rrListOfString.Count > 0)
                            {
                                worksheet.Cells[row, col].Value = $"{string.Join(", ", rrListOfString)}";
                            }
                        }
                        else
                        {
                            worksheet.Cells[row, col].Value = header.Reference;
                        }
                        col++;

                        worksheet.Cells[row, col].Value = details.AccountNo; col++;
                        worksheet.Cells[row, col].Value = details.AccountName; col++;
                        worksheet.Cells[row, col].Value = details.SubAccountName; col++;
                        worksheet.Cells[row, col].Value = details.Debit; col++;
                        worksheet.Cells[row, col].Value = details.Credit; col++;
                        worksheet.Cells[row, col].Value = header.Status;

                        if (showVoidCancelColumns)
                        {
                            col++;
                            worksheet.Cells[row, col].Value = header.VoidedBy; col++;
                            worksheet.Cells[row, col].Value = header.VoidedDate;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                        }

                        worksheet.Cells[row, dateColumn].Style.Numberformat.Format = "MMM/dd/yyyy";
                        worksheet.Cells[row, dcrDateColumn].Style.Numberformat.Format = "MMM/dd/yyyy";
                        worksheet.Cells[row, debitColumn].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, creditColumn].Style.Numberformat.Format = currencyFormat;

                        totalCredit += details.Credit;
                        totalDebit += details.Debit;

                        row++;
                    }
                }

                int totalRow = row;
                int lastDataCol = showVoidCancelColumns ? statusColumn + 2 : statusColumn;
                worksheet.Cells[totalRow, subAccountNameColumn].Value = "TOTAL: ";
                worksheet.Cells[totalRow, debitColumn].Value = totalDebit;
                worksheet.Cells[totalRow, creditColumn].Value = totalCredit;

                using (var range = worksheet.Cells[totalRow, 1, totalRow, lastDataCol])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[totalRow, debitColumn, totalRow, creditColumn])
                {
                    range.Style.Numberformat.Format = currencyFormat;
                }

                worksheet.Cells.AutoFitColumns();

                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Cv Disbursement report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"CV_Disbursement_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate cleared disbursement report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(CvDisbursementReport));
            }
        }

        #endregion -- Generate CV Disbursement Report as Excel File --

        public IActionResult PurchaseOrderReport()
        {
            return View();
        }

        #region -- Generated Purchase Order Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseOrderReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
            try
            {
                var statusFilter = NormalizeStatusFilter(model.StatusFilter);
                var purchaseOrder = await _unitOfWork.FilprideReport
                    .GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims, statusFilter, cancellationToken);

                if (purchaseOrder.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseOrderReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Times New Roman"));

                        #endregion -- Page setup

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("PURCHASE ORDER REPORT")
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

                        #endregion -- Header

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

                            #endregion -- Columns Definition

                            #region -- Table Header

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO#").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("IS PO#").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Transaction Date").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Quantity").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Unit").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Price").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Amount").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Remarks").SemiBold();
                            });

                            #endregion -- Table Header

                            #region -- Loop to Show Records

                            foreach (var record in purchaseOrder)
                            {
                                table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrderNo);
                                table.Cell().Border(0.5f).Padding(3).Text(record.OldPoNo);
                                table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Border(0.5f).Padding(3).Text(record.SupplierName);
                                table.Cell().Border(0.5f).Padding(3).Text(record.ProductName);
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Quantity != 0 ? record.Quantity < 0 ? $"({Math.Abs(record.Quantity).ToString(SD.Two_Decimal_Format)})" : record.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(record.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Border(0.5f).Padding(3).Text(record.Product?.ProductUnit);
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text((record.ActualPrices?.Count != 0 ? record.ActualPrices?.First(x => x.IsApproved).TriggeredPrice : record.Price) != 0
                                    ? record.Price < 0 ? $"({Math.Abs(record.Price).ToString(SD.Four_Decimal_Format)})" : record.Price.ToString(SD.Four_Decimal_Format) : null);
                                table.Cell().Border(0.5f).Padding(3).AlignRight().Text(record.Amount != 0 ? record.Amount < 0 ? $"({Math.Abs(record.Amount).ToString(SD.Two_Decimal_Format)})" : record.Amount.ToString(SD.Two_Decimal_Format) : null);
                                table.Cell().Border(0.5f).Padding(3).Text(record.Remarks);
                            }

                            #endregion -- Loop to Show Records
                        });

                        #endregion -- Content

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion -- Footer
                    });
                });

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate purchase order report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate purchase report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
        }

        #endregion -- Generated Purchase Order Report as Quest PDF

        #region -- Generate Purchase Order Report as Excel File --

        public async Task<IActionResult> GeneratePurchaseOrderReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(PurchaseOrderReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var purchaseOrderReport = await _unitOfWork.FilprideReport
                    .GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims, statusFilter, cancellationToken);

                if (purchaseOrderReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseOrderReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("PurchaseOrderReport");

                // Set the column headers
                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "PURCHASE ORDER REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                worksheet.Cells["A7"].Value = "PO #";
                worksheet.Cells["B7"].Value = "IS PO #";
                worksheet.Cells["C7"].Value = "Date";
                worksheet.Cells["D7"].Value = "Supplier";
                worksheet.Cells["E7"].Value = "Product";
                worksheet.Cells["F7"].Value = "Quantity";
                worksheet.Cells["G7"].Value = "Unit";
                worksheet.Cells["H7"].Value = "Price";
                worksheet.Cells["I7"].Value = "Amount";
                worksheet.Cells["J7"].Value = "Remarks";

                if (showVoidCancelColumns)
                {
                    worksheet.Cells[7, 11].Value = "Status";
                    worksheet.Cells[7, 12].Value = "Voided By";
                    worksheet.Cells[7, 13].Value = "Voided Date";
                }

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:" + (showVoidCancelColumns ? "M7" : "J7")])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                var row = 8;
                var currencyFormat = "#,##0.00";

                foreach (var po in purchaseOrderReport)
                {
                    worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                    worksheet.Cells[row, 2].Value = po.OldPoNo;
                    worksheet.Cells[row, 3].Value = po.Date;
                    worksheet.Cells[row, 4].Value = po.SupplierName;
                    worksheet.Cells[row, 5].Value = po.ProductName;
                    worksheet.Cells[row, 6].Value = po.Quantity;
                    worksheet.Cells[row, 7].Value = po.Product?.ProductUnit;
                    worksheet.Cells[row, 8].Value = po.ActualPrices!.Count != 0 ? po.ActualPrices!.First(x => x.IsApproved).TriggeredPrice : po.Price;
                    worksheet.Cells[row, 9].Value = po.Amount;
                    worksheet.Cells[row, 10].Value = po.Remarks;

                    worksheet.Cells[row, 3].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, 11].Value = po.Status;
                        worksheet.Cells[row, 12].Value = po.VoidedBy;
                        worksheet.Cells[row, 13].Value = po.VoidedDate;
                        worksheet.Cells[row, 13].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate purchase order report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Purchase_Order_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate purchase order report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
        }

        #endregion -- Generate Purchase Order Report as Excel File --

        public IActionResult PurchaseReport()
        {
            return View();
        }

        #region -- Generated Purchase Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PurchaseReport));
            }
            try
            {
                var purchaseReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, dateSelectionType: model.DateSelectionType, cancellationToken: cancellationToken);

                if (purchaseReport.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Times New Roman"));

                        #endregion -- Page setup

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("PURCHASE REPORT")
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

                        #endregion -- Header

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
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

                                #endregion -- Columns Definition

                                #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Lifting Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Tin").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Address").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Filpride RR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Filpride DR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ATL No.").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier SI").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("SI/Lifting Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier DR").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier WC").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CPL G. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Purchases G. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Vat Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("WHT Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Purchases N. VAT").SemiBold();
                                });

                                #endregion -- Table Header

                                #region -- Initialize Variable for Computation

                                var totalVolume = 0m;
                                var totalCostAmount = 0m;
                                var totalVatAmount = 0m;
                                var totalWhtAmount = 0m;
                                var totalNetPurchases = 0m;

                                #endregion -- Initialize Variable for Computation

                                #region -- Loop to Show Records

                                foreach (var record in purchaseReport)
                                {
                                    var volume = record.QuantityReceived;
                                    var costAmountGross = record.Amount;
                                    var costPerLiter = DivideOrZero(costAmountGross, volume);
                                    var costAmountNet = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                        ? NetOfVatOrZero(costAmountGross)
                                        : costAmountGross;
                                    var vatAmount = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                        ? VatAmountOrZero(costAmountNet)
                                        : 0m;
                                    var taxAmount = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                        ? VatAmountOrZero(costAmountNet)
                                        : 0m;

                                    table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.SupplierName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.SupplierTin);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.SupplierAddress);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ReceivingReportNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.AuthorityToLoadNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SupplierInvoiceNumber);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SupplierInvoiceDate?.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.SupplierDrNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.WithdrawalCertificate);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.ProductName);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(volume != 0 ? volume < 0 ? $"({Math.Abs(volume).ToString(SD.Two_Decimal_Format)})" : volume.ToString(SD.Two_Decimal_Format) : null).FontColor(volume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costPerLiter != 0 ? costPerLiter < 0 ? $"({Math.Abs(costPerLiter).ToString(SD.Four_Decimal_Format)})" : costPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(costPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costAmountGross != 0 ? costAmountGross < 0 ? $"({Math.Abs(costAmountGross).ToString(SD.Two_Decimal_Format)})" : costAmountGross.ToString(SD.Two_Decimal_Format) : null).FontColor(costAmountGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(vatAmount != 0 ? vatAmount < 0 ? $"({Math.Abs(vatAmount).ToString(SD.Two_Decimal_Format)})" : vatAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(vatAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(taxAmount != 0 ? taxAmount < 0 ? $"({Math.Abs(taxAmount).ToString(SD.Two_Decimal_Format)})" : taxAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(taxAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costAmountNet != 0 ? costAmountNet < 0 ? $"({Math.Abs(costAmountNet).ToString(SD.Two_Decimal_Format)})" : costAmountNet.ToString(SD.Two_Decimal_Format) : null).FontColor(costAmountNet < 0 ? Colors.Red.Medium : Colors.Black);

                                    totalVolume += volume;
                                    totalCostAmount += costAmountGross;
                                    totalVatAmount += vatAmount;
                                    totalWhtAmount += taxAmount;
                                    totalNetPurchases += costAmountNet;
                                }

                                #endregion -- Loop to Show Records

                                #region -- Initialize Variable for Computation of Totals

                                var totalCostPerLiter = DivideOrZero(totalCostAmount, totalVolume);

                                #endregion -- Initialize Variable for Computation of Totals

                                #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(14).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVolume != 0 ? totalVolume < 0 ? $"({Math.Abs(totalVolume).ToString(SD.Two_Decimal_Format)})" : totalVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCostPerLiter != 0 ? totalCostPerLiter < 0 ? $"({Math.Abs(totalCostPerLiter).ToString(SD.Four_Decimal_Format)})" : totalCostPerLiter.ToString(SD.Four_Decimal_Format) : null).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCostAmount != 0 ? totalCostAmount < 0 ? $"({Math.Abs(totalCostAmount).ToString(SD.Two_Decimal_Format)})" : totalCostAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVatAmount != 0 ? totalVatAmount < 0 ? $"({Math.Abs(totalVatAmount).ToString(SD.Two_Decimal_Format)})" : totalVatAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalWhtAmount != 0 ? totalWhtAmount < 0 ? $"({Math.Abs(totalWhtAmount).ToString(SD.Two_Decimal_Format)})" : totalWhtAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetPurchases != 0 ? totalNetPurchases < 0 ? $"({Math.Abs(totalNetPurchases).ToString(SD.Two_Decimal_Format)})" : totalNetPurchases.ToString(SD.Two_Decimal_Format) : null).SemiBold();

                                #endregion -- Create Table Cell for Totals

                                //Summary Table
                                col.Item().PaddingTop(50).Text("SUMMARY").Bold().FontSize(14);
                                var summaryProductList = GetOrderedProductNames(
                                    purchaseReport,
                                    pr => pr.PurchaseOrder!.ProductName);

                                #region -- Overall Summary

                                col.Item().PaddingTop(10).Table(content =>
                                {
                                    #region -- Columns Definition

                                    content.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn();

                                            foreach (var _ in summaryProductList)
                                            {
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();

                                                if (_ != summaryProductList.Last())
                                                {
                                                    columns.ConstantColumn(5);
                                                }
                                            }
                                        });

                                    #endregion -- Columns Definition

                                    #region -- Table Header

                                    content.Header(header =>
                                        {
                                            for (var index = 0; index < summaryProductList.Count; index++)
                                            {
                                                var productName = summaryProductList[index];
                                                header.Cell().ColumnSpan((uint)(index == 0 ? 4 : 3)).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text(productName).AlignCenter().SemiBold();

                                                if (index < summaryProductList.Count - 1)
                                                {
                                                    header.Cell();
                                                }
                                            }

                                            for (var index = 0; index < summaryProductList.Count; index++)
                                            {
                                                if (index == 0)
                                                {
                                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Suppliers").SemiBold();
                                                }

                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. CPL").SemiBold();

                                                if (index < summaryProductList.Count - 1)
                                                {
                                                    header.Cell();
                                                }
                                            }
                                        });

                                    #endregion -- Table Header

                                    #region -- Initialize Variable for Computation

                                    var totalMetricsByProduct = CreatePurchaseSummaryMetricMap(summaryProductList);

                                    #endregion -- Initialize Variable for Computation

                                    #region -- Loop to Show Records

                                    var groupBySupplier = purchaseReport
                                                .OrderBy(rr => rr.PurchaseOrder!.SupplierName)
                                                .GroupBy(rr => rr.PurchaseOrder!.SupplierName);

                                    // for each supplier
                                    foreach (var record in groupBySupplier)
                                    {
                                        var list = purchaseReport.Where(s => s.PurchaseOrder!.SupplierName == record.Key).ToList();

                                        var isVatable = list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                        var supplierMetricsByProduct = CreatePurchaseSummaryMetricMap(summaryProductList);
                                        foreach (var productName in summaryProductList)
                                        {
                                            var productPurchases = list.Where(s => string.Equals(s.PurchaseOrder!.ProductName, productName, StringComparison.OrdinalIgnoreCase)).ToList();
                                            supplierMetricsByProduct[productName].Quantity = productPurchases.Sum(s => s.QuantityReceived);
                                            supplierMetricsByProduct[productName].NetOfAmount = isVatable
                                                ? NetOfVatOrZero(productPurchases.Sum(pr => pr.Amount))
                                                : productPurchases.Sum(pr => pr.Amount);
                                        }

                                        content.Cell().Border(0.5f).Padding(3).Text(record.Key);
                                        for (var index = 0; index < summaryProductList.Count; index++)
                                        {
                                            var productName = summaryProductList[index];
                                            var productMetric = supplierMetricsByProduct[productName];
                                            var averagePurchase = ComputeAverage(productMetric.NetOfAmount, productMetric.Quantity);

                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(productMetric.Quantity != 0 ? productMetric.Quantity < 0 ? $"({Math.Abs(productMetric.Quantity).ToString(SD.Two_Decimal_Format)})" : productMetric.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(productMetric.NetOfAmount != 0 ? productMetric.NetOfAmount < 0 ? $"({Math.Abs(productMetric.NetOfAmount).ToString(SD.Two_Decimal_Format)})" : productMetric.NetOfAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(productMetric.NetOfAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(averagePurchase != 0 ? averagePurchase < 0 ? $"({Math.Abs(averagePurchase).ToString(SD.Four_Decimal_Format)})" : averagePurchase.ToString(SD.Four_Decimal_Format) : null).FontColor(averagePurchase < 0 ? Colors.Red.Medium : Colors.Black);

                                            if (index < summaryProductList.Count - 1)
                                            {
                                                content.Cell();
                                            }

                                            totalMetricsByProduct[productName].Quantity += productMetric.Quantity;
                                            totalMetricsByProduct[productName].NetOfAmount += productMetric.NetOfAmount;
                                        }
                                    }

                                    #endregion -- Loop to Show Records

                                    #region -- Create Table Cell for Totals

                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    for (var index = 0; index < summaryProductList.Count; index++)
                                    {
                                        var productName = summaryProductList[index];
                                        var totalMetric = totalMetricsByProduct[productName];
                                        var averagePurchase = ComputeAverage(totalMetric.NetOfAmount, totalMetric.Quantity);

                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.Quantity != 0 ? totalMetric.Quantity < 0 ? $"({Math.Abs(totalMetric.Quantity).ToString(SD.Two_Decimal_Format)})" : totalMetric.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.Quantity < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.NetOfAmount != 0 ? totalMetric.NetOfAmount < 0 ? $"({Math.Abs(totalMetric.NetOfAmount).ToString(SD.Two_Decimal_Format)})" : totalMetric.NetOfAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.NetOfAmount < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averagePurchase != 0 ? averagePurchase < 0 ? $"({Math.Abs(averagePurchase).ToString(SD.Four_Decimal_Format)})" : averagePurchase.ToString(SD.Four_Decimal_Format) : null).FontColor(averagePurchase < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                        if (index < summaryProductList.Count - 1)
                                        {
                                            content.Cell();
                                        }
                                    }

                                    #endregion -- Create Table Cell for Totals
                                });

                                #endregion -- Overall Summary
                            });
                        });

                        #endregion -- Content

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion -- Footer
                    });
                });

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate purchase report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate purchase report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseReport));
            }
        }

        #endregion -- Generated Purchase Report as Quest PDF

        #region -- Generate Purchase Report as Excel File --

        public async Task<IActionResult> GeneratePurchaseReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(PurchaseReport));
            }

            var statusFilter = NormalizeStatusFilter(model.StatusFilter);

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // get rr data from chosen date
                var purchaseReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom,
                        model.DateTo,
                        companyClaims,
                        dateSelectionType: model.DateSelectionType,
                        statusFilter: statusFilter,
                        cancellationToken: cancellationToken);

                // check if there is no record
                if (purchaseReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                #region -- Initialize "total" Variables for operations --

                var totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
                var totalCostAmount = 0m;
                var totalVatAmount = 0m;
                var totalWhtAmount = 0m;
                var totalNetPurchases = 0m;
                var totalFreight = 0m;
                var totalNetFreight = 0m;
                var totalCommission = 0m;
                var totalPurchaseNetOfWht = 0m;
                var totalFreightWhtAmount = 0m;
                var totalFreightNetOfWht = 0m;

                #endregion -- Initialize "total" Variables for operations --

                // Create the Excel package
                using var package = new ExcelPackage();

                // Add a new worksheet to the Excel package
                var purchaseReportWorksheet = package.Workbook.Worksheets.Add("PurchaseReport");

                #region -- Purchase Report Worksheet --

                #region -- Set the column header  --

                var mergedCells = purchaseReportWorksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "PURCHASE REPORT";
                mergedCells.Style.Font.Size = 13;

                purchaseReportWorksheet.Cells["A2"].Value = "Date Range:";
                purchaseReportWorksheet.Cells["A3"].Value = "Generated By:";
                purchaseReportWorksheet.Cells["A4"].Value = "Company:";
                purchaseReportWorksheet.Cells["A5"].Value = "Status Filter:";
                purchaseReportWorksheet.Cells["A6"].Value = "Date and Time Generated:";

                purchaseReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                purchaseReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                purchaseReportWorksheet.Cells["B4"].Value = $"{companyClaims}";
                purchaseReportWorksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                purchaseReportWorksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                purchaseReportWorksheet.Cells["A7"].Value = "LIFTING DATE";
                purchaseReportWorksheet.Cells["B7"].Value = "CUSTOMER RECEIVED DATE";
                purchaseReportWorksheet.Cells["C7"].Value = "SUPPLIER NAME";
                purchaseReportWorksheet.Cells["D7"].Value = "SUPPLIER TIN";
                purchaseReportWorksheet.Cells["E7"].Value = "SUPPLIER ADDRESS";
                purchaseReportWorksheet.Cells["F7"].Value = "PO#.";
                purchaseReportWorksheet.Cells["G7"].Value = "FILPRIDE RR";
                purchaseReportWorksheet.Cells["H7"].Value = "COS#";
                purchaseReportWorksheet.Cells["I7"].Value = "FILPRIDE DR";
                purchaseReportWorksheet.Cells["J7"].Value = "DEPOT";
                purchaseReportWorksheet.Cells["K7"].Value = "ATL #";
                purchaseReportWorksheet.Cells["L7"].Value = "SUPPLIER ATL #";
                purchaseReportWorksheet.Cells["M7"].Value = "SUPPLIER'S SI";
                purchaseReportWorksheet.Cells["N7"].Value = "SI/LIFTING DATE";
                purchaseReportWorksheet.Cells["O7"].Value = "SUPPLIER'S DR";
                purchaseReportWorksheet.Cells["P7"].Value = "SUPPLIER'S WC";
                purchaseReportWorksheet.Cells["Q7"].Value = "CUSTOMER NAME";
                purchaseReportWorksheet.Cells["R7"].Value = "PRODUCT";
                purchaseReportWorksheet.Cells["S7"].Value = "VOLUME";
                purchaseReportWorksheet.Cells["T7"].Value = "CPL G.VAT";
                purchaseReportWorksheet.Cells["U7"].Value = "PURCHASES G.VAT";
                purchaseReportWorksheet.Cells["V7"].Value = "PURCHASES N.VAT";
                purchaseReportWorksheet.Cells["W7"].Value = "VAT AMOUNT";
                purchaseReportWorksheet.Cells["X7"].Value = "WHT AMOUNT";
                purchaseReportWorksheet.Cells["Y7"].Value = "PURC.NET OF WHT";
                purchaseReportWorksheet.Cells["Z7"].Value = "HAULER'S NAME";
                purchaseReportWorksheet.Cells["AA7"].Value = "FREIGHT G.VAT";
                purchaseReportWorksheet.Cells["AB7"].Value = "FREIGHT N.VAT";
                purchaseReportWorksheet.Cells["AC7"].Value = "FREIGHT AMT G.VAT";
                purchaseReportWorksheet.Cells["AD7"].Value = "FREIGHT AMT N.VAT";
                purchaseReportWorksheet.Cells["AE7"].Value = "FREIGHT WHT AMT";
                purchaseReportWorksheet.Cells["AF7"].Value = "FREIGHT NET OF WHT";
                purchaseReportWorksheet.Cells["AG7"].Value = "COMMISSION";
                purchaseReportWorksheet.Cells["AH7"].Value = "OTC COS#.";
                purchaseReportWorksheet.Cells["AI7"].Value = "OTC DR#.";
                purchaseReportWorksheet.Cells["AJ7"].Value = "IS PO#";
                purchaseReportWorksheet.Cells["AK7"].Value = "IS RR#";
                purchaseReportWorksheet.Cells["AL7"].Value = "TERMS";

                int lastColIndex = 38; // AL = 38
                if (showVoidCancelColumns)
                {
                    purchaseReportWorksheet.Cells[7, 39].Value = "STATUS";
                    purchaseReportWorksheet.Cells[7, 40].Value = "VOIDED BY";
                    purchaseReportWorksheet.Cells[7, 41].Value = "VOIDED DATE";
                    lastColIndex = 41;
                }

                #endregion -- Set the column header  --

                #region -- Apply styling to the header row --

                using (var range = purchaseReportWorksheet.Cells["A7:" + (showVoidCancelColumns ? "AO7" : "AL7")])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                #endregion -- Apply styling to the header row --

                // Populate the data rows
                var row = 8; // starting row
                var currencyFormat = "#,##0.0000"; // numbers format
                var currencyFormat2 = "#,##0.00"; // numbers format



                var atlNos = purchaseReport
                    .Select(pr => pr.AuthorityToLoadNo)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                var atls = await _dbContext.FilprideAuthorityToLoads
                    .Where(x => atlNos.Contains(x.AuthorityToLoadNo))
                    .ToListAsync(cancellationToken);

                var atlLookup = atls
                    .GroupBy(x => x.AuthorityToLoadNo)
                    .ToDictionary(g => g.Key, g => g.First());
                var purchaseReportDeliveryReceiptIds = purchaseReport
                    .Where(pr => pr.DeliveryReceiptId.HasValue)
                    .Select(pr => pr.DeliveryReceiptId!.Value)
                    .Distinct()
                    .ToList();
                var purchaseReportPoIds = purchaseReport
                    .Select(pr => pr.POId)
                    .Distinct()
                    .ToList();
                var supplierAtlEntries = await _dbContext.FilprideDeliveryReceiptDetails
                    .Where(d => purchaseReportDeliveryReceiptIds.Contains(d.DeliveryReceiptId)
                                && purchaseReportPoIds.Contains(d.PurchaseOrderId)
                                && atlNos.Contains(d.AuthorityToLoadNo!))
                    .Join(_dbContext.FilprideBookAtlDetails,
                        drDetail => new
                        {
                            drDetail.AuthorityToLoadId,
                            drDetail.CustomerOrderSlipId,
                            drDetail.PurchaseOrderId
                        },
                        atlDetail => new
                        {
                            atlDetail.AuthorityToLoadId,
                            atlDetail.CustomerOrderSlipId,
                            PurchaseOrderId = atlDetail.AppointedSupplier!.PurchaseOrderId
                        },
                        (drDetail, atlDetail) => new
                        {
                            drDetail.DeliveryReceiptId,
                            drDetail.PurchaseOrderId,
                            drDetail.AuthorityToLoadNo,
                            atlDetail.SupplierAtlNo
                        })
                    .ToListAsync(cancellationToken);

                #region -- Populate data rows --

                foreach (var pr in purchaseReport)
                {
                    #region -- Variables and Formulas --

                    var isSupplierVatable = pr.PurchaseOrder!.VatType == SD.VatType_Vatable;
                    var isSupplierTaxable = pr.PurchaseOrder!.TaxType == SD.TaxType_WithTax;
                    var isHaulerVatable = pr.DeliveryReceipt!.HaulerVatType == SD.VatType_Vatable;
                    var isHaulerTaxable = pr.DeliveryReceipt!.Hauler?.TaxType == SD.TaxType_WithTax;

                    // calculate values, put in variables to be displayed per cell
                    var volume = pr.QuantityReceived; // volume
                    var costAmount = pr.Amount; // purchase total gross
                    var netPurchases = isSupplierVatable
                        ? NetOfVatOrZero(costAmount)
                        : costAmount; // purchase total net
                    var freight = pr.DeliveryReceipt?.Freight ?? 0m; // freight g vat
                    var netFreight = isHaulerVatable && freight != 0m
                        ? NetOfVatOrZero(freight)
                        : freight; // freight n vat
                    var freightAmount = RoundToFour(freight * volume); // freight total gross
                    var freightAmountNet =
                        isHaulerVatable && freightAmount != 0m
                            ? NetOfVatOrZero(freightAmount)
                            : freightAmount; // freight total net
                    var vatAmount = isSupplierVatable
                        ? VatAmountOrZero(netPurchases)
                        : 0m; // vat total
                    var whtAmount = isSupplierTaxable
                        ? EwtAmountOrZero(netPurchases, pr.TaxPercentage)
                        : 0m; // wht total
                    var costPerLiter = DivideOrZero(costAmount, volume); // sale price per liter
                    var commission = RoundToFour((pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m) * volume);
                    var purchaseNetOfWht = RoundToFour(costAmount - whtAmount);
                    var freightNetOfVatAmount = isHaulerVatable
                        ? NetOfVatOrZero(freightAmountNet)
                        : freightAmountNet;
                    var freightWhtAmount = isHaulerTaxable
                        ? EwtAmountOrZero(freightNetOfVatAmount, pr.DeliveryReceipt?.Hauler?.WithholdingTaxPercent ?? 0)
                        : 0m;
                    var freightNetOfWht = RoundToFour(freightAmount - freightWhtAmount);

                    FilprideAuthorityToLoad? atl;
                    if (pr.AuthorityToLoadNo != null)
                    {
                        atlLookup.TryGetValue(pr.AuthorityToLoadNo, out atl);
                    }
                    else
                    {
                        atl = null;
                    }

                    var supplierAtlNo = pr.DeliveryReceiptId.HasValue && !string.IsNullOrWhiteSpace(pr.AuthorityToLoadNo)
                        ? supplierAtlEntries
                            .Where(x => x.DeliveryReceiptId == pr.DeliveryReceiptId.Value
                                        && x.PurchaseOrderId == pr.POId
                                        && x.AuthorityToLoadNo == pr.AuthorityToLoadNo)
                            .Select(x => x.SupplierAtlNo)
                            .FirstOrDefault()
                        : null;

                    #endregion -- Variables and Formulas --

                    #region -- Assign Values to Cells --

                    purchaseReportWorksheet.Cells[row, 1].Value = pr.Date; // Date
                    purchaseReportWorksheet.Cells[row, 2].Value = pr.DeliveryReceipt?.DeliveredDate; // DeliveredDate
                    purchaseReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.SupplierName; // Supplier Name
                    purchaseReportWorksheet.Cells[row, 4].Value = pr.PurchaseOrder?.SupplierTin; // Supplier Tin
                    purchaseReportWorksheet.Cells[row, 5].Value = pr.PurchaseOrder?.SupplierAddress; // Supplier Address
                    purchaseReportWorksheet.Cells[row, 6].Value = pr.PurchaseOrder?.PurchaseOrderNo; // PO No.
                    purchaseReportWorksheet.Cells[row, 7].Value = pr.ReceivingReportNo ?? pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride RR
                    purchaseReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo; // COS
                    purchaseReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride DR
                    purchaseReportWorksheet.Cells[row, 10].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.Depot; // Filpride DR
                    purchaseReportWorksheet.Cells[row, 11].Value = atl?.AuthorityToLoadNo; // ATL #
                    purchaseReportWorksheet.Cells[row, 12].Value = supplierAtlNo; // Supplier ATL #
                    purchaseReportWorksheet.Cells[row, 13].Value = pr.SupplierInvoiceNumber; // Supplier's Sales Invoice
                    purchaseReportWorksheet.Cells[row, 14].Value = pr.SupplierInvoiceDate; // Supplier's Sales Invoice
                    purchaseReportWorksheet.Cells[row, 15].Value = pr.SupplierDrNo; // Supplier's DR
                    purchaseReportWorksheet.Cells[row, 16].Value = pr.WithdrawalCertificate; // Supplier's WC
                    purchaseReportWorksheet.Cells[row, 17].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerName; // Customer Name
                    purchaseReportWorksheet.Cells[row, 18].Value = pr.PurchaseOrder?.ProductName; // Product
                    purchaseReportWorksheet.Cells[row, 19].Value = volume; // Volume
                    purchaseReportWorksheet.Cells[row, 20].Value = costPerLiter; // Purchase price per liter
                    purchaseReportWorksheet.Cells[row, 21].Value = costAmount; // Purchase total gross
                    purchaseReportWorksheet.Cells[row, 22].Value = netPurchases; // Purchase total net ======== move to third last
                    purchaseReportWorksheet.Cells[row, 23].Value = vatAmount; // Vat total
                    purchaseReportWorksheet.Cells[row, 24].Value = whtAmount; // freight g vat
                    purchaseReportWorksheet.Cells[row, 25].Value = purchaseNetOfWht; // Purchase Net of WHT
                    purchaseReportWorksheet.Cells[row, 26].Value = pr.DeliveryReceipt?.HaulerName; // Hauler's Name
                    purchaseReportWorksheet.Cells[row, 27].Value = freight; // WHT total
                    purchaseReportWorksheet.Cells[row, 28].Value = netFreight; // freight n vat ============
                    purchaseReportWorksheet.Cells[row, 29].Value = freightAmount; // freight amount n vat ============
                    purchaseReportWorksheet.Cells[row, 30].Value = freightAmountNet; // freight amount n vat ============
                    purchaseReportWorksheet.Cells[row, 31].Value = freightWhtAmount; // Freight WHT amount
                    purchaseReportWorksheet.Cells[row, 32].Value = freightNetOfWht; // Freight Net of WHT
                    purchaseReportWorksheet.Cells[row, 33].Value = commission; // commission =========
                    purchaseReportWorksheet.Cells[row, 34].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo; // OTC COS =========
                    purchaseReportWorksheet.Cells[row, 35].Value = pr.DeliveryReceipt?.ManualDrNo; // OTC DR =========
                    purchaseReportWorksheet.Cells[row, 36].Value = pr.PurchaseOrder?.OldPoNo; // IS PO =========
                    purchaseReportWorksheet.Cells[row, 37].Value = pr.OldRRNo; // IS RR =========
                    purchaseReportWorksheet.Cells[row, 38].Value = pr.PurchaseOrder?.Terms;

                    if (showVoidCancelColumns)
                    {
                        purchaseReportWorksheet.Cells[row, 39].Value = pr.Status;
                        purchaseReportWorksheet.Cells[row, 40].Value = pr.VoidedBy;
                        purchaseReportWorksheet.Cells[row, 41].Value = pr.VoidedDate;
                        purchaseReportWorksheet.Cells[row, 41].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    #endregion -- Assign Values to Cells --

                    #region -- Add the values to total --

                    totalCostAmount += costAmount;
                    totalVatAmount += vatAmount;
                    totalWhtAmount += whtAmount;
                    totalNetPurchases += netPurchases;
                    totalCommission += commission;
                    totalFreight += freightAmount;
                    totalNetFreight += freightAmountNet;
                    totalPurchaseNetOfWht += purchaseNetOfWht;
                    totalFreightWhtAmount += freightWhtAmount;
                    totalFreightNetOfWht += freightNetOfWht;

                    #endregion -- Add the values to total --

                    #region -- Add format number cells from Assign Values to Cells --

                    purchaseReportWorksheet.Cells[row, 1, row, 2].Style.Numberformat.Format = "MMM/dd/yyyy";
                    purchaseReportWorksheet.Cells[row, 14].Style.Numberformat.Format = "MMM/dd/yyyy";

                    #endregion -- Add format number cells from Assign Values to Cells --

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Assign values of other totals and formatting of total cells --

                var totalCostPerLiter = DivideOrZero(totalCostAmount, totalVolume);

                purchaseReportWorksheet.Cells[row, 17].Value = "Total: ";
                purchaseReportWorksheet.Cells[row, 19].Value = totalVolume;
                purchaseReportWorksheet.Cells[row, 20].Value = totalCostPerLiter;
                purchaseReportWorksheet.Cells[row, 21].Value = totalCostAmount;
                purchaseReportWorksheet.Cells[row, 22].Value = totalNetPurchases;
                purchaseReportWorksheet.Cells[row, 23].Value = totalVatAmount;
                purchaseReportWorksheet.Cells[row, 24].Value = totalWhtAmount;
                purchaseReportWorksheet.Cells[row, 25].Value = totalPurchaseNetOfWht;

                purchaseReportWorksheet.Cells[row, 27].Value = "";
                purchaseReportWorksheet.Cells[row, 29].Value = totalFreight;
                purchaseReportWorksheet.Cells[row, 30].Value = totalNetFreight;
                purchaseReportWorksheet.Cells[row, 31].Value = totalFreightWhtAmount;
                purchaseReportWorksheet.Cells[row, 32].Value = totalFreightNetOfWht;
                purchaseReportWorksheet.Cells[row, 33].Value = totalCommission;

                purchaseReportWorksheet.Column(19).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(20).Style.Numberformat.Format = currencyFormat;
                purchaseReportWorksheet.Column(21).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(22).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(23).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(24).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(25).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(27).Style.Numberformat.Format = currencyFormat;
                purchaseReportWorksheet.Column(28).Style.Numberformat.Format = currencyFormat;
                purchaseReportWorksheet.Column(29).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(30).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(31).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(32).Style.Numberformat.Format = currencyFormat2;
                purchaseReportWorksheet.Column(33).Style.Numberformat.Format = currencyFormat2;

                #endregion -- Assign values of other totals and formatting of total cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = purchaseReportWorksheet.Cells[row, 1, row, lastColIndex])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = purchaseReportWorksheet.Cells[row, 17, row, 33])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                row += 2;

                #region -- Summary Header --

                purchaseReportWorksheet.Cells[row, 2].Value = "SUMMARY: ";
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Size = 16;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.UnderLine = true;

                row++;

                var firstColumnForThickBorder = row;

                var startingSummaryTableRow = row;

                var productList = GetOrderedProductNames(
                    purchaseReport,
                    pr => pr.PurchaseOrder!.ProductName);
                var summaryFirstMetricColumn = 3;

                for (int index = 0; index < productList.Count; index++)
                {
                    var summaryStartColumn = summaryFirstMetricColumn + (index * 3);
                    mergedCells = purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Font.Size = 16;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    mergedCells.Merge = true;
                    mergedCells.Value = productList[index];
                }

                row++;

                purchaseReportWorksheet.Cells[row, 2].Value = "SUPPLIERS";
                purchaseReportWorksheet.Cells[row, 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                purchaseReportWorksheet.Cells[row, 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;
                purchaseReportWorksheet.Cells[row, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                purchaseReportWorksheet.Cells[row, 2].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                purchaseReportWorksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                for (int index = 0; index < productList.Count; index++)
                {
                    var summaryStartColumn = summaryFirstMetricColumn + (index * 3);
                    purchaseReportWorksheet.Cells[row, summaryStartColumn].Value = "VOLUME";
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 1].Value = "PURCHASES N.VAT";
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 2].Value = "AVE. CPL";
                    purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                    using var range = purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2];
                    range.Style.Font.Bold = true;
                    range.Style.Font.Italic = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                row += 2;

                #endregion -- Summary Header --

                #region == Summary Contents ==

                // query a group by supplier
                var supplierByRr = purchaseReport
                    .OrderBy(rr => rr.PurchaseOrder!.SupplierName)
                    .GroupBy(rr => rr.PurchaseOrder!.SupplierName);

                // for each supplier
                foreach (var rrSupplier in supplierByRr)
                {
                    var startingColumn = summaryFirstMetricColumn;
                    var isVatable = rrSupplier.First().PurchaseOrder!.VatType == SD.VatType_Vatable;

                    // get name of group supplier
                    purchaseReportWorksheet.Cells[row, 2].Value = rrSupplier.First().PurchaseOrder!.SupplierName;
                    purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                    purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;

                    // group each product of supplier
                    var productBySupplier = rrSupplier
                        .OrderBy(p => p.PurchaseOrder!.ProductName)
                        .GroupBy(rr => rr.PurchaseOrder!.ProductName);

                    // get volume, net purchases, and average cost per liter
                    foreach (var productName in productList)
                    {
                        var product = productBySupplier.FirstOrDefault(p => string.Equals(p.Key, productName, StringComparison.OrdinalIgnoreCase));
                        if (product != null && product.Any())
                        {
                            var grandTotalVolume = product
                                .Sum(pr => pr.QuantityReceived); // volume
                            var grandTotalPurchaseNet = isVatable
                                ? NetOfVatOrZero(product.Sum(pr => pr.Amount))
                                : product.Sum(pr => pr.Amount); // Purchase Net Total

                            purchaseReportWorksheet.Cells[row, startingColumn].Value = grandTotalVolume;
                            purchaseReportWorksheet.Cells[row, startingColumn + 1].Value = grandTotalPurchaseNet;
                            purchaseReportWorksheet.Cells[row, startingColumn + 2].Value = DivideOrZero(grandTotalPurchaseNet, grandTotalVolume); // Gross Margin Per Liter
                            purchaseReportWorksheet.Cells[row, startingColumn, row, startingColumn + 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            purchaseReportWorksheet.Cells[row, startingColumn].Style.Numberformat.Format = currencyFormat2;
                            purchaseReportWorksheet.Cells[row, startingColumn + 1].Style.Numberformat.Format = currencyFormat2;
                            purchaseReportWorksheet.Cells[row, startingColumn + 2].Style.Numberformat.Format = currencyFormat;
                        }

                        startingColumn += 3;
                    }

                    row++;
                }

                var endingSummaryTableRow = row - 1;

                row++;

                for (var index = 0; index < productList.Count; index++)
                {
                    var summaryStartColumn = summaryFirstMetricColumn + (index * 3);
                    purchaseReportWorksheet.Cells[row, summaryStartColumn].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, summaryStartColumn].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, summaryStartColumn].Address})";
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 1].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, summaryStartColumn + 1].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, summaryStartColumn + 1].Address})";
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 2].Formula = $"={purchaseReportWorksheet.Cells[row, summaryStartColumn + 1].Address}/{purchaseReportWorksheet.Cells[row, summaryStartColumn].Address}";

                    purchaseReportWorksheet.Cells[row, summaryStartColumn].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 1].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, summaryStartColumn + 2].Style.Numberformat.Format = currencyFormat;

                    mergedCells = purchaseReportWorksheet.Cells[row, summaryStartColumn, row, summaryStartColumn + 2];
                    mergedCells.Style.Font.Bold = true;
                    mergedCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    mergedCells.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    mergedCells.Style.Font.Size = 11;
                    mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var lastColumnForThickBorder = row;

                var enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 2, lastColumnForThickBorder, 2];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 3, lastColumnForThickBorder, 5];
                enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                for (var index = 1; index < productList.Count; index++)
                {
                    var summaryStartColumn = summaryFirstMetricColumn + (index * 3);
                    enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, summaryStartColumn, lastColumnForThickBorder, summaryStartColumn + 2];
                    enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                #endregion == Summary Contents ==

                #endregion -- Summary Row --

                // Auto-fit columns for better readability
                purchaseReportWorksheet.Cells.AutoFitColumns();
                purchaseReportWorksheet.View.FreezePanes(8, 1);
                purchaseReportWorksheet.Column(5).Width = 24;

                #endregion -- Purchase Report Worksheet --

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate purchase report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Purchase_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate purchase report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseReport));
            }
        }

        #endregion -- Generate Purchase Report as Excel File --

        public async Task<IActionResult> GrossMarginReport()
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims == null)
            {
                return BadRequest();
            }

            ViewModelBook viewmodel = new()
            {
                CustomerList = await _unitOfWork.GetFilprideCustomerListAsyncById(companyClaims),
                CommissioneeList = await _unitOfWork.GetFilprideCommissioneeListAsyncById(companyClaims)
            };

            return View(viewmodel);
        }

        #region -- Generated Gross Margin Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedGmReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(GrossMarginReport));
            }

            try
            {
                var grossMarginReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, model.Customers, model.Commissionee, cancellationToken: cancellationToken);

                if (!grossMarginReport.Any())
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(GrossMarginReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Times New Roman"));

                        #endregion -- Page setup

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("GROSS MARGIN REPORT")
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

                        #endregion -- Header

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
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
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                #endregion -- Columns Definition

                                #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Date").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PO#").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("RR#").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("DR#").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Customer Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Product").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Account Specialist").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Hauler Name").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Commissionee").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("COS Price").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Sales G. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("CPL G. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Purchase G. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Vat Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Purchase N. VAT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("GM/Liter").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("GM Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Freight Charge").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("FC Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Commission/Liter").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Commission Amount").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Margin/Liter").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Margin Amount").SemiBold();
                                });

                                #endregion -- Table Header

                                #region -- Initialize Variable for Computation

                                var totalVolume = 0m;
                                var totalPurchaseAmountGross = 0m;
                                var totalVatAmount = 0m;
                                var totalPurchaseAmountNet = 0m;
                                var totalSaleAmount = 0m;
                                var totalGmAmount = 0m;
                                var totalFcAmount = 0m;
                                var totalCommissionAmount = 0m;
                                var totalNetMarginAmount = 0m;

                                #endregion -- Initialize Variable for Computation

                                #region -- Loop to Show Records

                                foreach (var record in grossMarginReport)
                                {
                                    var isVatable = record.PurchaseOrder!.VatType == SD.VatType_Vatable;
                                    var volume = record.QuantityReceived;
                                    var costAmountGross = record.Amount;
                                    var purchasePerLiter = DivideOrZero(costAmountGross, volume);
                                    var salePricePerLiter = record.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m;
                                    var costAmountNet = isVatable
                                        ? NetOfVatOrZero(costAmountGross)
                                        : costAmountGross;
                                    var costVatAmount = isVatable
                                        ? VatAmountOrZero(costAmountNet)
                                        : 0m;
                                    var saleAmountGross = RoundToFour(volume * salePricePerLiter);
                                    var gmPerLiter = RoundToFour(salePricePerLiter - purchasePerLiter);
                                    var gmAmount = RoundToFour(volume * gmPerLiter);
                                    var freightChargePerLiter = RoundToFour(record.DeliveryReceipt!.Freight + (record.DeliveryReceipt?.ECC ?? 0m));
                                    var commissionPerLiter = record.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m;
                                    var commissionAmount = RoundToFour(commissionPerLiter * volume);
                                    var netMarginPerLiter = RoundToFour(gmPerLiter - freightChargePerLiter);
                                    var freightChargeAmount = RoundToFour(volume * freightChargePerLiter);
                                    var netMarginAmount = RoundToFour(volume * netMarginPerLiter);

                                    table.Cell().Border(0.5f).Padding(3).Text(record.Date.ToString(SD.Date_Format));
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.SupplierName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.PurchaseOrderNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.ReceivingReportNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.CustomerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.ProductName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.AccountSpecialist);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.HaulerName);
                                    table.Cell().Border(0.5f).Padding(3).Text(record.DeliveryReceipt?.CustomerOrderSlip?.CommissioneeName);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(volume != 0 ? volume < 0 ? $"({Math.Abs(volume).ToString(SD.Two_Decimal_Format)})" : volume.ToString(SD.Two_Decimal_Format) : null).FontColor(volume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(salePricePerLiter != 0 ? salePricePerLiter < 0 ? $"({Math.Abs(salePricePerLiter).ToString(SD.Four_Decimal_Format)})" : salePricePerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(salePricePerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(saleAmountGross != 0 ? saleAmountGross < 0 ? $"({Math.Abs(saleAmountGross).ToString(SD.Two_Decimal_Format)})" : saleAmountGross.ToString(SD.Two_Decimal_Format) : null).FontColor(saleAmountGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(purchasePerLiter != 0 ? purchasePerLiter < 0 ? $"({Math.Abs(purchasePerLiter).ToString(SD.Four_Decimal_Format)})" : purchasePerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(purchasePerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costAmountGross != 0 ? costAmountGross < 0 ? $"({Math.Abs(costAmountGross).ToString(SD.Two_Decimal_Format)})" : costAmountGross.ToString(SD.Two_Decimal_Format) : null).FontColor(costAmountGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costVatAmount != 0 ? costVatAmount < 0 ? $"({Math.Abs(costVatAmount).ToString(SD.Two_Decimal_Format)})" : costVatAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(costVatAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(costAmountNet != 0 ? costAmountNet < 0 ? $"({Math.Abs(costAmountNet).ToString(SD.Two_Decimal_Format)})" : costAmountNet.ToString(SD.Two_Decimal_Format) : null).FontColor(costAmountNet < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(gmPerLiter != 0 ? gmPerLiter < 0 ? $"({Math.Abs(gmPerLiter).ToString(SD.Four_Decimal_Format)})" : gmPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(gmPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(gmAmount != 0 ? gmAmount < 0 ? $"({Math.Abs(gmAmount).ToString(SD.Two_Decimal_Format)})" : gmAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(gmAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightChargePerLiter != 0 ? freightChargePerLiter < 0 ? $"({Math.Abs(freightChargePerLiter).ToString(SD.Four_Decimal_Format)})" : freightChargePerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(freightChargePerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(freightChargeAmount != 0 ? freightChargeAmount < 0 ? $"({Math.Abs(freightChargeAmount).ToString(SD.Two_Decimal_Format)})" : freightChargeAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(freightChargeAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(commissionPerLiter != 0 ? commissionPerLiter < 0 ? $"({Math.Abs(commissionPerLiter).ToString(SD.Four_Decimal_Format)})" : commissionPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(commissionPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(commissionAmount != 0 ? commissionAmount < 0 ? $"({Math.Abs(commissionAmount).ToString(SD.Two_Decimal_Format)})" : commissionAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(commissionAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(netMarginPerLiter != 0 ? netMarginPerLiter < 0 ? $"({Math.Abs(netMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : netMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(netMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Border(0.5f).Padding(3).AlignRight().Text(netMarginAmount != 0 ? netMarginAmount < 0 ? $"({Math.Abs(netMarginAmount).ToString(SD.Two_Decimal_Format)})" : netMarginAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(netMarginAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                    totalVolume += volume;
                                    totalPurchaseAmountGross += costAmountGross;
                                    totalVatAmount += costVatAmount;
                                    totalPurchaseAmountNet += costAmountNet;
                                    totalSaleAmount += saleAmountGross;
                                    totalGmAmount += saleAmountGross - costAmountGross;
                                    totalFcAmount += freightChargePerLiter * volume;
                                    totalCommissionAmount += volume * commissionPerLiter;
                                    totalNetMarginAmount += (gmPerLiter - freightChargePerLiter) * volume;
                                }

                                #endregion -- Loop to Show Records

                                #region -- Initialize Variable for Computation of Totals

                                var averagePurchasePrice = DivideOrZero(totalPurchaseAmountGross, totalVolume);
                                var averageSalePrice = DivideOrZero(totalSaleAmount, totalVolume);
                                var totalGmPerLiter = DivideOrZero(totalGmAmount, totalVolume);
                                var totalFreightCharge = DivideOrZero(totalFcAmount, totalVolume);
                                var totalCommissionPerLiter = DivideOrZero(totalCommissionAmount, totalVolume);
                                var totalNetMarginPerLiter = DivideOrZero(totalNetMarginAmount, totalVolume);

                                #endregion -- Initialize Variable for Computation of Totals

                                #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(10).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVolume != 0 ? totalVolume < 0 ? $"({Math.Abs(totalVolume).ToString(SD.Two_Decimal_Format)})" : totalVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSalePrice != 0 ? averageSalePrice < 0 ? $"({Math.Abs(averageSalePrice).ToString(SD.Four_Decimal_Format)})" : averageSalePrice.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(averageSalePrice < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalSaleAmount != 0 ? totalSaleAmount < 0 ? $"({Math.Abs(totalSaleAmount).ToString(SD.Two_Decimal_Format)})" : totalSaleAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalSaleAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averagePurchasePrice != 0 ? averagePurchasePrice < 0 ? $"({Math.Abs(averagePurchasePrice).ToString(SD.Four_Decimal_Format)})" : averagePurchasePrice.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(averagePurchasePrice < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalPurchaseAmountGross != 0 ? totalPurchaseAmountGross < 0 ? $"({Math.Abs(totalPurchaseAmountGross).ToString(SD.Two_Decimal_Format)})" : totalPurchaseAmountGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalPurchaseAmountGross < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVatAmount != 0 ? totalVatAmount < 0 ? $"({Math.Abs(totalVatAmount).ToString(SD.Two_Decimal_Format)})" : totalVatAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalVatAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalPurchaseAmountNet != 0 ? totalPurchaseAmountNet < 0 ? $"({Math.Abs(totalPurchaseAmountNet).ToString(SD.Two_Decimal_Format)})" : totalPurchaseAmountNet.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalPurchaseAmountNet < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalGmPerLiter != 0 ? totalGmPerLiter < 0 ? $"({Math.Abs(totalGmPerLiter).ToString(SD.Four_Decimal_Format)})" : totalGmPerLiter.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(totalGmPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalGmAmount != 0 ? totalGmAmount < 0 ? $"({Math.Abs(totalGmAmount).ToString(SD.Two_Decimal_Format)})" : totalGmAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalGmAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFreightCharge != 0 ? totalFreightCharge < 0 ? $"({Math.Abs(totalFreightCharge).ToString(SD.Four_Decimal_Format)})" : totalFreightCharge.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(totalFreightCharge < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalFcAmount != 0 ? totalFcAmount < 0 ? $"({Math.Abs(totalFcAmount).ToString(SD.Two_Decimal_Format)})" : totalFcAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalFcAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCommissionPerLiter != 0 ? totalCommissionPerLiter < 0 ? $"({Math.Abs(totalCommissionPerLiter).ToString(SD.Four_Decimal_Format)})" : totalCommissionPerLiter.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(totalCommissionPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCommissionAmount != 0 ? totalCommissionAmount < 0 ? $"({Math.Abs(totalCommissionAmount).ToString(SD.Two_Decimal_Format)})" : totalCommissionAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalCommissionAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetMarginPerLiter != 0 ? totalNetMarginPerLiter < 0 ? $"({Math.Abs(totalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : totalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).SemiBold().FontColor(totalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetMarginAmount != 0 ? totalNetMarginAmount < 0 ? $"({Math.Abs(totalNetMarginAmount).ToString(SD.Two_Decimal_Format)})" : totalNetMarginAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalNetMarginAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                #endregion -- Create Table Cell for Totals

                                //Summary Table
                                col.Item().PageBreak();
                                col.Item().Text("SUMMARY").Bold().FontSize(14);

                                #region -- Overall Summary

                                col.Item().PaddingTop(10).Table(content =>
                                {
                                    #region -- Columns Definition

                                    content.ColumnsDefinition(columns =>
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
                                        });

                                    #endregion -- Columns Definition

                                    #region -- Table Header

                                    content.Header(header =>
                                        {
                                            header.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Overall").AlignCenter().SemiBold();

                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Segment").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Gross Margin").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Freight N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Commission").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Net Margin").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Net GM/LIT").SemiBold();
                                        });

                                    #endregion -- Table Header

                                    #region -- Initialize Variable for Computation

                                    var overallTotalQuantity = 0m;
                                    var overallTotalSales = 0m;
                                    var overallTotalPurchases = 0m;
                                    var overallTotalGrossMargin = 0m;
                                    var overallTotalFreight = 0m;
                                    var overallTotalCommission = 0m;
                                    var overallTotalNetMargin = 0m;
                                    var overallTotalNetMarginPerLiter = 0m;

                                    #endregion -- Initialize Variable for Computation

                                    #region -- Loop to Show Records

                                    foreach (var customerType in Enum.GetValues<CustomerType>())
                                    {
                                        var list = grossMarginReport.Where(s => s.DeliveryReceipt!.CustomerOrderSlip?.CustomerType == customerType.ToString()).ToList();
                                        var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                        var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                        var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;

                                        var overallQuantitySum = list.Sum(s => s.QuantityReceived);
                                        var overallSalesSum = RoundToFour(list.Sum(s => s.QuantityReceived * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice));
                                        var overallNetOfSalesSum = isCustomerVatable && overallSalesSum != 0m
                                                ? NetOfVatOrZero(overallSalesSum)
                                                : overallSalesSum;
                                        var overallPurchasesSum = list.Sum(s => s.Amount);
                                        var overallNetOfPurchasesSum = isSupplierVatable && overallPurchasesSum != 0m
                                                ? NetOfVatOrZero(overallPurchasesSum)
                                                : overallPurchasesSum;
                                        var overallGrossMarginSum = RoundToFour(overallNetOfSalesSum - overallNetOfPurchasesSum);
                                        var overallFreightSum = RoundToFour(list.Sum(s => s.QuantityReceived * (s.DeliveryReceipt!.Freight + s.DeliveryReceipt.ECC)));
                                        var overallNetOfFreightSum = isHaulerVatable && overallFreightSum != 0m
                                                ? NetOfVatOrZero(overallFreightSum)
                                                : overallFreightSum;
                                        var overallCommissionSum = RoundToFour(list.Sum(s => s.QuantityReceived * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate));
                                        var overallNetMarginSum = RoundToFour(overallGrossMarginSum - (overallFreightSum + overallCommissionSum));
                                        var overallNetMarginPerLiterSum = overallNetMarginSum != 0 && overallQuantitySum != 0 ? DivideOrZero(overallNetMarginSum, overallQuantitySum) : 0m;

                                        content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallQuantitySum != 0 ? overallQuantitySum < 0 ? $"({Math.Abs(overallQuantitySum).ToString(SD.Two_Decimal_Format)})" : overallQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfSalesSum != 0 ? overallNetOfSalesSum < 0 ? $"({Math.Abs(overallNetOfSalesSum).ToString(SD.Two_Decimal_Format)})" : overallNetOfSalesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetOfSalesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfPurchasesSum != 0 ? overallNetOfPurchasesSum < 0 ? $"({Math.Abs(overallNetOfPurchasesSum).ToString(SD.Two_Decimal_Format)})" : overallNetOfPurchasesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetOfPurchasesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallGrossMarginSum != 0 ? overallGrossMarginSum < 0 ? $"({Math.Abs(overallGrossMarginSum).ToString(SD.Two_Decimal_Format)})" : overallGrossMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallGrossMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetOfFreightSum != 0 ? overallNetOfFreightSum < 0 ? $"({Math.Abs(overallNetOfFreightSum).ToString(SD.Two_Decimal_Format)})" : overallNetOfFreightSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetOfFreightSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallCommissionSum != 0 ? overallCommissionSum < 0 ? $"({Math.Abs(overallCommissionSum).ToString(SD.Two_Decimal_Format)})" : overallCommissionSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallCommissionSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetMarginSum != 0 ? overallNetMarginSum < 0 ? $"({Math.Abs(overallNetMarginSum).ToString(SD.Two_Decimal_Format)})" : overallNetMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(overallNetMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Border(0.5f).Padding(3).AlignRight().Text(overallNetMarginPerLiterSum != 0 ? overallNetMarginPerLiterSum < 0 ? $"({Math.Abs(overallNetMarginPerLiterSum).ToString(SD.Four_Decimal_Format)})" : overallNetMarginPerLiterSum.ToString(SD.Four_Decimal_Format) : null).FontColor(overallNetMarginPerLiterSum < 0 ? Colors.Red.Medium : Colors.Black);

                                        overallTotalQuantity += overallQuantitySum;
                                        overallTotalSales += overallNetOfSalesSum;
                                        overallTotalPurchases += overallNetOfPurchasesSum;
                                        overallTotalGrossMargin += overallGrossMarginSum;
                                        overallTotalFreight += overallNetOfFreightSum;
                                        overallTotalCommission += overallCommissionSum;
                                        overallTotalNetMargin += overallNetMarginSum;
                                        overallTotalNetMarginPerLiter = overallTotalNetMargin != 0 && overallTotalQuantity != 0 ? overallTotalNetMargin / overallTotalQuantity : 0;
                                    }

                                    #endregion -- Loop to Show Records

                                    #region -- Create Table Cell for Totals

                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalQuantity != 0 ? overallTotalQuantity < 0 ? $"({Math.Abs(overallTotalQuantity).ToString(SD.Two_Decimal_Format)})" : overallTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalSales != 0 ? overallTotalSales < 0 ? $"({Math.Abs(overallTotalSales).ToString(SD.Two_Decimal_Format)})" : overallTotalSales.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalSales < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalPurchases != 0 ? overallTotalPurchases < 0 ? $"({Math.Abs(overallTotalPurchases).ToString(SD.Two_Decimal_Format)})" : overallTotalPurchases.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalPurchases < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalGrossMargin != 0 ? overallTotalGrossMargin < 0 ? $"({Math.Abs(overallTotalGrossMargin).ToString(SD.Two_Decimal_Format)})" : overallTotalGrossMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalGrossMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalFreight != 0 ? overallTotalFreight < 0 ? $"({Math.Abs(overallTotalFreight).ToString(SD.Two_Decimal_Format)})" : overallTotalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalCommission != 0 ? overallTotalCommission < 0 ? $"({Math.Abs(overallTotalCommission).ToString(SD.Two_Decimal_Format)})" : overallTotalCommission.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalCommission < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalNetMargin != 0 ? overallTotalNetMargin < 0 ? $"({Math.Abs(overallTotalNetMargin).ToString(SD.Two_Decimal_Format)})" : overallTotalNetMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(overallTotalNetMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                    content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(overallTotalNetMarginPerLiter != 0 ? overallTotalNetMarginPerLiter < 0 ? $"({Math.Abs(overallTotalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : overallTotalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(overallTotalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);

                                    #endregion -- Create Table Cell for Totals
                                });

                                #endregion -- Overall Summary

                                var grossMarginProductList = GetOrderedProductNames(
                                    grossMarginReport,
                                    report => report.DeliveryReceipt!.CustomerOrderSlip!.ProductName);

                                foreach (var productName in grossMarginProductList)
                                {
                                    col.Item().PaddingTop(10).Table(content =>
                                    {
                                        content.ColumnsDefinition(columns =>
                                        {
                                            for (var index = 0; index < 9; index++)
                                            {
                                                columns.RelativeColumn();
                                            }
                                        });

                                        content.Header(header =>
                                        {
                                            header.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text(productName).AlignCenter().SemiBold();

                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Segment").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Sales N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Gross Margin").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Freight N. VAT").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Commission").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Net Margin").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Net GM/LIT").SemiBold();
                                        });

                                        var totalMetric = new GrossMarginSummaryMetric();

                                        foreach (var customerType in Enum.GetValues<CustomerType>())
                                        {
                                            var list = grossMarginReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                                            var productItems = list.Where(s => string.Equals(s.DeliveryReceipt!.CustomerOrderSlip!.ProductName, productName, StringComparison.OrdinalIgnoreCase)).ToList();
                                            var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                            var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                            var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;

                                            var quantitySum = productItems.Sum(s => s.QuantityReceived);
                                            var salesSum = RoundToFour(productItems.Sum(s => s.QuantityReceived * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice));
                                            var netOfSalesSum = isCustomerVatable && salesSum != 0m
                                                ? NetOfVatOrZero(salesSum)
                                                : salesSum;
                                            var purchasesSum = productItems.Sum(s => s.Amount);
                                            var netOfPurchasesSum = isSupplierVatable && purchasesSum != 0m
                                                ? NetOfVatOrZero(purchasesSum)
                                                : purchasesSum;
                                            var grossMarginSum = RoundToFour(netOfSalesSum - netOfPurchasesSum);
                                            var freightSum = RoundToFour(productItems.Sum(s => s.QuantityReceived * (s.DeliveryReceipt!.Freight + s.DeliveryReceipt.ECC)));
                                            var netOfFreightSum = isHaulerVatable && freightSum != 0m
                                                ? NetOfVatOrZero(freightSum)
                                                : freightSum;
                                            var commissionSum = RoundToFour(productItems.Sum(s => s.QuantityReceived * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate));
                                            var netMarginSum = RoundToFour(grossMarginSum - (freightSum + commissionSum));
                                            var netMarginPerLiterSum = quantitySum != 0m ? DivideOrZero(netMarginSum, quantitySum) : 0m;

                                            content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(quantitySum != 0 ? quantitySum < 0 ? $"({Math.Abs(quantitySum).ToString(SD.Two_Decimal_Format)})" : quantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(quantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(netOfSalesSum != 0 ? netOfSalesSum < 0 ? $"({Math.Abs(netOfSalesSum).ToString(SD.Two_Decimal_Format)})" : netOfSalesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(netOfSalesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(netOfPurchasesSum != 0 ? netOfPurchasesSum < 0 ? $"({Math.Abs(netOfPurchasesSum).ToString(SD.Two_Decimal_Format)})" : netOfPurchasesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(netOfPurchasesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(grossMarginSum != 0 ? grossMarginSum < 0 ? $"({Math.Abs(grossMarginSum).ToString(SD.Two_Decimal_Format)})" : grossMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(grossMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(netOfFreightSum != 0 ? netOfFreightSum < 0 ? $"({Math.Abs(netOfFreightSum).ToString(SD.Two_Decimal_Format)})" : netOfFreightSum.ToString(SD.Two_Decimal_Format) : null).FontColor(netOfFreightSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(commissionSum != 0 ? commissionSum < 0 ? $"({Math.Abs(commissionSum).ToString(SD.Two_Decimal_Format)})" : commissionSum.ToString(SD.Two_Decimal_Format) : null).FontColor(commissionSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(netMarginSum != 0 ? netMarginSum < 0 ? $"({Math.Abs(netMarginSum).ToString(SD.Two_Decimal_Format)})" : netMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(netMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Border(0.5f).Padding(3).AlignRight().Text(netMarginPerLiterSum != 0 ? netMarginPerLiterSum < 0 ? $"({Math.Abs(netMarginPerLiterSum).ToString(SD.Four_Decimal_Format)})" : netMarginPerLiterSum.ToString(SD.Four_Decimal_Format) : null).FontColor(netMarginPerLiterSum < 0 ? Colors.Red.Medium : Colors.Black);

                                            totalMetric.Quantity += quantitySum;
                                            totalMetric.NetOfSales += netOfSalesSum;
                                            totalMetric.NetOfPurchases += netOfPurchasesSum;
                                            totalMetric.GrossMargin += grossMarginSum;
                                            totalMetric.NetOfFreight += netOfFreightSum;
                                            totalMetric.Commission += commissionSum;
                                            totalMetric.NetMargin += netMarginSum;
                                        }

                                        var totalNetMarginPerLiter = totalMetric.Quantity != 0m ? DivideOrZero(totalMetric.NetMargin, totalMetric.Quantity) : 0m;

                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.Quantity != 0 ? totalMetric.Quantity < 0 ? $"({Math.Abs(totalMetric.Quantity).ToString(SD.Two_Decimal_Format)})" : totalMetric.Quantity.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.Quantity < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.NetOfSales != 0 ? totalMetric.NetOfSales < 0 ? $"({Math.Abs(totalMetric.NetOfSales).ToString(SD.Two_Decimal_Format)})" : totalMetric.NetOfSales.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.NetOfSales < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.NetOfPurchases != 0 ? totalMetric.NetOfPurchases < 0 ? $"({Math.Abs(totalMetric.NetOfPurchases).ToString(SD.Two_Decimal_Format)})" : totalMetric.NetOfPurchases.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.NetOfPurchases < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.GrossMargin != 0 ? totalMetric.GrossMargin < 0 ? $"({Math.Abs(totalMetric.GrossMargin).ToString(SD.Two_Decimal_Format)})" : totalMetric.GrossMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.GrossMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.NetOfFreight != 0 ? totalMetric.NetOfFreight < 0 ? $"({Math.Abs(totalMetric.NetOfFreight).ToString(SD.Two_Decimal_Format)})" : totalMetric.NetOfFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.NetOfFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.Commission != 0 ? totalMetric.Commission < 0 ? $"({Math.Abs(totalMetric.Commission).ToString(SD.Two_Decimal_Format)})" : totalMetric.Commission.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.Commission < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalMetric.NetMargin != 0 ? totalMetric.NetMargin < 0 ? $"({Math.Abs(totalMetric.NetMargin).ToString(SD.Two_Decimal_Format)})" : totalMetric.NetMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(totalMetric.NetMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                        content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetMarginPerLiter != 0 ? totalNetMarginPerLiter < 0 ? $"({Math.Abs(totalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : totalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(totalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);
                                    });
                                }
                            });
                        });

                        #endregion -- Content

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion -- Footer
                    });
                });

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate gross margin report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate gross margin report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(GrossMarginReport));
            }
        }

        #endregion -- Generated Gross Margin Report as Quest PDF

        #region -- Generate Gross Margin Report as Excel File --

        public async Task<IActionResult> GenerateGmReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(GrossMarginReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;

                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                using var package = new ExcelPackage();
                var gmReportWorksheet = package.Workbook.Worksheets.Add("GMReport");

                var grossMarginReport = await _unitOfWork.FilprideReport
                    .GetGrossMarginReport(model.DateFrom, model.DateTo, companyClaims, model.Customers, model.Commissionee, cancellationToken: cancellationToken);

                if (grossMarginReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(GrossMarginReport));
                }

                var drIds = grossMarginReport
                    .Where(dr => dr.HasReceivingReport)
                    .Select(dr => dr.DeliveryReceiptId)
                    .ToList();
                var receivingReports = await _unitOfWork.FilprideReceivingReport
                    .GetAllAsync(rr => rr.DeliveryReceiptId.HasValue
                                       && rr.PostedBy != null
                                       && drIds.Contains(rr.DeliveryReceiptId.Value), cancellationToken);
                var rrLookup = receivingReports
                    .Where(rr => rr.DeliveryReceiptId.HasValue)
                    .ToLookup(rr => rr.DeliveryReceiptId!.Value);

                #region -- Initialize "total" Variables for operations --

                var totalVolume = grossMarginReport.Sum(pr => pr.Quantity);
                var totalCostAmount = 0m;
                var totalNetPurchases = 0m;
                var totalSalesAmount = 0m;
                var totalNetSales = 0m;
                var totalGmPerLiter = 0m;
                var totalGmAmount = 0m;
                var totalFcAmount = 0m;
                var totalFcNet = 0m;
                var totalCommissionAmount = 0m;
                var totalNetMarginPerLiter = 0m;
                var totalNetMarginAmount = 0m;
                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                #endregion -- Initialize "total" Variables for operations --

                #region -- Initialize "Summary" variables

                    var grossMarginProductList = GetOrderedProductNames(
                        grossMarginReport,
                        report => report.CustomerOrderSlip!.ProductName);
                    var customerTypeNames = Enum.GetValues<CustomerType>()
                        .Select(customerType => customerType.ToString())
                        .ToList();
                    var overallMetricsByCustomerType = customerTypeNames.ToDictionary(
                        customerType => customerType,
                        _ => new GrossMarginSummaryMetric(),
                        StringComparer.OrdinalIgnoreCase);
                    var productMetricsByCustomerType = customerTypeNames.ToDictionary(
                        customerType => customerType,
                        _ => CreateGrossMarginSummaryMetricMap(grossMarginProductList),
                        StringComparer.OrdinalIgnoreCase);
                    var totalOverallMetric = new GrossMarginSummaryMetric();
                    var totalProductMetrics = CreateGrossMarginSummaryMetricMap(grossMarginProductList);

                #endregion

                #region -- Column Names --

                var mergedCells = gmReportWorksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "GM REPORT";
                mergedCells.Style.Font.Size = 13;

                gmReportWorksheet.Cells["A2"].Value = "Date Range:";
                gmReportWorksheet.Cells["A3"].Value = "Generated By:";
                gmReportWorksheet.Cells["A4"].Value = "Company:";
                gmReportWorksheet.Cells["A5"].Value = "Date and Time Generated:";

                gmReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                gmReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                gmReportWorksheet.Cells["B4"].Value = $"{companyClaims}";
                gmReportWorksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                gmReportWorksheet.Cells["A7"].Value = "RR DATE";
                gmReportWorksheet.Cells["B7"].Value = "SUPPLIER NAME";
                gmReportWorksheet.Cells["C7"].Value = "SUPPLIER TERMS";
                gmReportWorksheet.Cells["D7"].Value = "PO NO.";
                gmReportWorksheet.Cells["E7"].Value = "FILPRIDE RR";
                gmReportWorksheet.Cells["F7"].Value = "FILPRIDE DR";
                gmReportWorksheet.Cells["G7"].Value = "CUSTOMER NAME";
                gmReportWorksheet.Cells["H7"].Value = "PRODUCT NAME";
                gmReportWorksheet.Cells["I7"].Value = "ACCOUNT SPECIALIST";
                gmReportWorksheet.Cells["J7"].Value = "HAULER NAME";
                gmReportWorksheet.Cells["K7"].Value = "COMMISSIONEE";
                gmReportWorksheet.Cells["L7"].Value = "VOLUME";
                gmReportWorksheet.Cells["M7"].Value = "COS PRICE";
                gmReportWorksheet.Cells["N7"].Value = "SALES G. VAT";
                gmReportWorksheet.Cells["O7"].Value = "SALES N. VAT";
                gmReportWorksheet.Cells["P7"].Value = "CPL G. VAT";
                gmReportWorksheet.Cells["Q7"].Value = "PURCHASES G. VAT";
                gmReportWorksheet.Cells["R7"].Value = "PURCHASES N.VAT";
                gmReportWorksheet.Cells["S7"].Value = "GM/LITER";
                gmReportWorksheet.Cells["T7"].Value = "GM AMOUNT";
                gmReportWorksheet.Cells["U7"].Value = "FREIGHT CHARGE";
                gmReportWorksheet.Cells["V7"].Value = "FC AMOUNT";
                gmReportWorksheet.Cells["W7"].Value = "FC N.VAT";
                gmReportWorksheet.Cells["X7"].Value = "COMMISSION/LITER";
                gmReportWorksheet.Cells["Y7"].Value = "COMMISSION AMOUNT";
                gmReportWorksheet.Cells["Z7"].Value = "NET MARGIN/LIT";
                gmReportWorksheet.Cells["AA7"].Value = "NET MARGIN AMOUNT";

                #endregion -- Column Names --

                #region -- Apply styling to the header row --

                using (var range = gmReportWorksheet.Cells["A7:AA7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                #endregion -- Apply styling to the header row --

                // Populate the data row
                var row = 8; // starting row
                var currencyFormat = "#,##0.0000"; // numbers format
                var currencyFormatTwoDecimal = "#,##0.00"; // numbers format

                #region -- Populate data rows --

                foreach (var dr in grossMarginReport)
                {
                    #region -- Variables and Formulas --

                    // calculate values, put in variables to be displayed per cell
                    var isHaulerVatable = dr.HaulerVatType == SD.VatType_Vatable;
                    var isCustomerVatable = dr.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                    var relatedReceivingReports = dr.HasReceivingReport
                        ? rrLookup[dr.DeliveryReceiptId].OrderBy(rr => rr.Date).ToList()
                        : [];
                    var purchaseOrders = dr.Details
                        .Where(detail => detail.PurchaseOrder != null)
                        .GroupBy(detail => detail.PurchaseOrderId)
                        .Select(group => group.First().PurchaseOrder!)
                        .ToList();

                    if (purchaseOrders.Count == 0 && dr.PurchaseOrder != null)
                    {
                        purchaseOrders.Add(dr.PurchaseOrder);
                    }

                    var supplierNames = string.Join(", ", purchaseOrders
                        .Select(po => po.SupplierName)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase));

                    var terms = string.Join(", ", purchaseOrders
                        .Select(po => po.Terms)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase));

                    var poNumbers = string.Join(", ", purchaseOrders
                        .Select(po => po.PurchaseOrderNo)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                    var rrNumbers = relatedReceivingReports
                        .Select(rr => rr.ReceivingReportNo)
                        .Where(rrNo => !string.IsNullOrWhiteSpace(rrNo));
                    var rrDateDisplay = relatedReceivingReports.Count switch
                    {
                        0 => dr.DeliveredDate?.ToString("MMM/dd/yyyy"),
                        1 => relatedReceivingReports[0].Date.ToString("MMM/dd/yyyy"),
                        _ => $"{relatedReceivingReports.First().Date:MMM/dd/yyyy} - {relatedReceivingReports.Last().Date:MMM/dd/yyyy}"
                    };
                    var volume = dr.Quantity;
                    var cosPricePerLiter = dr.CustomerOrderSlip.DeliveredPrice; // sales per liter
                    var salesAmount = dr.TotalAmount; // sales total
                    var netSales = isCustomerVatable
                        ? NetOfVatOrZero(salesAmount)
                        : salesAmount;
                    var costAmount = relatedReceivingReports.Count > 0
                        ? relatedReceivingReports.Sum(rr => rr.Amount)
                        : purchaseOrders.Count > 0
                            ? dr.Details
                                .Where(detail => detail.PurchaseOrder != null)
                                .Sum(detail => detail.Quantity * detail.PurchaseOrder!.FinalPrice)
                            : dr.PurchaseOrder!.FinalPrice * volume; // purchase total
                    var costPerLiter = DivideOrZero(costAmount, volume); // purchase per liter
                    var netPurchases = relatedReceivingReports.Count > 0
                        ? relatedReceivingReports.Sum(rr => rr.PurchaseOrder?.VatType == SD.VatType_Vatable && rr.Amount != 0m
                            ? NetOfVatOrZero(rr.Amount)
                            : rr.Amount)
                        : purchaseOrders.Count > 0
                            ? dr.Details
                                .Where(detail => detail.PurchaseOrder != null)
                                .Sum(detail =>
                                {
                                    var detailCostAmount = detail.Quantity * detail.PurchaseOrder!.FinalPrice;
                                    return detail.PurchaseOrder.VatType == SD.VatType_Vatable
                                        ? NetOfVatOrZero(detailCostAmount)
                                        : detailCostAmount;
                                })
                            : dr.PurchaseOrder!.VatType == SD.VatType_Vatable
                                ? NetOfVatOrZero(costAmount)
                                : costAmount; // purchase total net
                    var gmAmount = RoundToFour(netSales - netPurchases); // gross margin total
                    var gmPerLiter = DivideOrZero(gmAmount, volume); // gross margin per liter
                    var freightCharge = RoundToFour(dr.Freight + dr.ECC); // freight charge per liter
                    var freightChargeAmount = dr.FreightAmount; // freight charge total
                    var freightChargeNet = isHaulerVatable && freightChargeAmount != 0m
                        ? NetOfVatOrZero(freightChargeAmount)
                        : freightChargeAmount;
                    var commissionPerLiter = dr.CommissionRate; // commission rate
                    var commissionAmount = dr.CommissionAmount; // commission total
                    var netMarginAmount = RoundToFour(gmAmount - freightChargeNet - commissionAmount);
                    var netMarginPerLiter = DivideOrZero(netMarginAmount, volume); // net margin per liter

                    #endregion -- Variables and Formulas --

                    #region -- Variables and Formulas for summary --

                    var customerType = dr.CustomerOrderSlip!.CustomerType;
                    var productName = dr.CustomerOrderSlip!.ProductName;

                    if (!overallMetricsByCustomerType.TryGetValue(customerType, out var overallMetric))
                    {
                        throw new ArgumentException("No customer type");
                    }

                    overallMetric.Quantity += volume;
                    overallMetric.NetOfSales += netSales;
                    overallMetric.NetOfPurchases += netPurchases;
                    overallMetric.GrossMargin += gmAmount;
                    overallMetric.NetOfFreight += freightChargeNet;
                    overallMetric.Commission += commissionAmount;
                    overallMetric.NetMargin += netMarginAmount;

                    totalOverallMetric.Quantity += volume;
                    totalOverallMetric.NetOfSales += netSales;
                    totalOverallMetric.NetOfPurchases += netPurchases;
                    totalOverallMetric.GrossMargin += gmAmount;
                    totalOverallMetric.NetOfFreight += freightChargeNet;
                    totalOverallMetric.Commission += commissionAmount;
                    totalOverallMetric.NetMargin += netMarginAmount;

                    if (productMetricsByCustomerType.TryGetValue(customerType, out var productMetrics)
                        && productMetrics.TryGetValue(productName, out var productMetric))
                    {
                        productMetric.Quantity += volume;
                        productMetric.NetOfSales += netSales;
                        productMetric.NetOfPurchases += netPurchases;
                        productMetric.GrossMargin += gmAmount;
                        productMetric.NetOfFreight += freightChargeNet;
                        productMetric.Commission += commissionAmount;
                        productMetric.NetMargin += netMarginAmount;
                    }

                    if (totalProductMetrics.TryGetValue(productName, out var totalProductMetric))
                    {
                        totalProductMetric.Quantity += volume;
                        totalProductMetric.NetOfSales += netSales;
                        totalProductMetric.NetOfPurchases += netPurchases;
                        totalProductMetric.GrossMargin += gmAmount;
                        totalProductMetric.NetOfFreight += freightChargeNet;
                        totalProductMetric.Commission += commissionAmount;
                        totalProductMetric.NetMargin += netMarginAmount;
                    }

                    #endregion

                    #region -- Assign Values to Cells --

                    gmReportWorksheet.Cells[row, 1].Value = rrDateDisplay;
                    gmReportWorksheet.Cells[row, 2].Value = supplierNames;
                    gmReportWorksheet.Cells[row, 3].Value = terms;
                    gmReportWorksheet.Cells[row, 4].Value = poNumbers;
                    gmReportWorksheet.Cells[row, 5].Value = string.Join(", ", rrNumbers);
                    gmReportWorksheet.Cells[row, 6].Value = dr.DeliveryReceiptNo;
                    gmReportWorksheet.Cells[row, 7].Value = dr.CustomerOrderSlip.CustomerName;
                    gmReportWorksheet.Cells[row, 8].Value = productName;
                    gmReportWorksheet.Cells[row, 9].Value = dr.CustomerOrderSlip.AccountSpecialist;
                    gmReportWorksheet.Cells[row, 10].Value = dr.HaulerName;
                    gmReportWorksheet.Cells[row, 11].Value = dr.CustomerOrderSlip.CommissioneeName;
                    gmReportWorksheet.Cells[row, 12].Value = volume;
                    gmReportWorksheet.Cells[row, 13].Value = cosPricePerLiter;
                    gmReportWorksheet.Cells[row, 14].Value = salesAmount;
                    gmReportWorksheet.Cells[row, 15].Value = netSales;
                    gmReportWorksheet.Cells[row, 16].Value = costPerLiter;
                    gmReportWorksheet.Cells[row, 17].Value = costAmount;
                    gmReportWorksheet.Cells[row, 18].Value = netPurchases;
                    gmReportWorksheet.Cells[row, 19].Value = gmPerLiter;
                    gmReportWorksheet.Cells[row, 20].Value = gmAmount;
                    gmReportWorksheet.Cells[row, 21].Value = freightCharge;
                    gmReportWorksheet.Cells[row, 22].Value = freightChargeAmount;
                    gmReportWorksheet.Cells[row, 23].Value = freightChargeNet;
                    gmReportWorksheet.Cells[row, 24].Value = commissionPerLiter;
                    gmReportWorksheet.Cells[row, 25].Value = commissionAmount;
                    gmReportWorksheet.Cells[row, 26].Value = netMarginPerLiter;
                    gmReportWorksheet.Cells[row, 27].Value = netMarginAmount;

                    #endregion -- Assign Values to Cells --

                    #region -- Computation of totals --

                    totalSalesAmount += salesAmount;
                    totalNetSales += netSales;
                    totalCostAmount += costAmount;
                    totalNetPurchases += netPurchases;
                    totalGmPerLiter += gmPerLiter;
                    totalGmAmount += gmAmount;
                    totalFcAmount += freightChargeAmount;
                    totalFcNet += freightChargeNet;
                    totalCommissionAmount += commissionAmount;
                    totalNetMarginPerLiter += netMarginPerLiter;
                    totalNetMarginAmount += netMarginAmount;

                    #endregion

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Other subtotal values and formatting of subtotal cells --

                var totalCostPerLiter = DivideOrZero(totalCostAmount, totalVolume);
                var totalCosPrice = DivideOrZero(totalSalesAmount, totalVolume);
                totalGmPerLiter = DivideOrZero(totalGmAmount, totalVolume);
                var totalFreightCharge = DivideOrZero(totalFcAmount, totalVolume);
                var totalCommissionPerLiter = DivideOrZero(totalCommissionAmount, totalVolume);
                totalNetMarginPerLiter = DivideOrZero(totalNetMarginAmount, totalVolume);

                gmReportWorksheet.Cells[row, 10].Value = "Total: ";
                gmReportWorksheet.Cells[row, 12].Value = totalVolume;
                gmReportWorksheet.Cells[row, 13].Value = totalCosPrice;
                gmReportWorksheet.Cells[row, 14].Value = totalSalesAmount;
                gmReportWorksheet.Cells[row, 15].Value = totalNetSales;
                gmReportWorksheet.Cells[row, 16].Value = totalCostPerLiter;
                gmReportWorksheet.Cells[row, 17].Value = totalCostAmount;
                gmReportWorksheet.Cells[row, 18].Value = totalNetPurchases;
                gmReportWorksheet.Cells[row, 19].Value = totalGmPerLiter;
                gmReportWorksheet.Cells[row, 20].Value = totalGmAmount;
                gmReportWorksheet.Cells[row, 21].Value = totalFreightCharge;
                gmReportWorksheet.Cells[row, 22].Value = totalFcAmount;
                gmReportWorksheet.Cells[row, 23].Value = totalFcNet;
                gmReportWorksheet.Cells[row, 24].Value = totalCommissionPerLiter;
                gmReportWorksheet.Cells[row, 25].Value = totalCommissionAmount;
                gmReportWorksheet.Cells[row, 26].Value = totalNetMarginPerLiter;
                gmReportWorksheet.Cells[row, 27].Value = totalNetMarginAmount;

                gmReportWorksheet.Column(12).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(13).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(14).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(15).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(16).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(17).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(18).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(19).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(20).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(21).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(22).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(23).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(24).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(25).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(26).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(27).Style.Numberformat.Format = currencyFormatTwoDecimal;

                #endregion -- Other subtotal values and formatting of subtotal cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = gmReportWorksheet.Cells[row, 1, row, 27])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = gmReportWorksheet.Cells[row, 10, row, 27])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                var rowForSummary = row + 8;
                var summaryTotalRow = rowForSummary + customerTypeNames.Count;
                const int productSummaryStartColumn = 12;
                const int productSummaryWidth = 8;
                const int productSummarySpacing = 1;

                #region -- Summary header for "Overall"

                var mergedCellForOverall = gmReportWorksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 10];
                mergedCellForOverall.Merge = true;
                mergedCellForOverall.Value = "Overall";
                mergedCellForOverall.Style.Font.Size = 13;
                mergedCellForOverall.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 3, rowForSummary - 2, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var textStyleForSummary = gmReportWorksheet.Cells[rowForSummary - 3, 2];
                textStyleForSummary.Style.Font.Size = 16;
                textStyleForSummary.Style.Font.Bold = true;

                gmReportWorksheet.Cells[rowForSummary - 3, 2].Value = "Summary";
                gmReportWorksheet.Cells[rowForSummary - 1, 2].Value = "Segment";
                gmReportWorksheet.Cells[rowForSummary - 1, 3].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 4].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 5].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 6].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 7].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 8].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 9].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 10].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                using (var range = gmReportWorksheet.Cells[summaryTotalRow, 2, summaryTotalRow, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                #endregion

                for (var index = 0; index < grossMarginProductList.Count; index++)
                {
                    var productName = grossMarginProductList[index];
                    var sectionStartColumn = productSummaryStartColumn + index * (productSummaryWidth + productSummarySpacing);
                    var sectionEndColumn = sectionStartColumn + productSummaryWidth - 1;
                    var mergedCell = gmReportWorksheet.Cells[rowForSummary - 2, sectionStartColumn, rowForSummary - 2, sectionEndColumn];
                    mergedCell.Merge = true;
                    mergedCell.Value = productName;
                    mergedCell.Style.Font.Size = 13;
                    mergedCell.Style.Font.Bold = true;
                    gmReportWorksheet.Cells[rowForSummary - 2, sectionStartColumn, rowForSummary - 2, sectionEndColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn].Value = "Volume";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 1].Value = "Sales N. VAT";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 2].Value = "Purchases N. VAT";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 3].Value = "Gross Margin";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 4].Value = "Freight N. VAT";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 5].Value = "Commission";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 6].Value = "Net Margin";
                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn + 7].Value = "Net GM/LIT";

                    gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn, rowForSummary - 1, sectionEndColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    using (var range = gmReportWorksheet.Cells[rowForSummary - 1, sectionStartColumn, rowForSummary - 1, sectionEndColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    using (var range = gmReportWorksheet.Cells[summaryTotalRow, sectionStartColumn, summaryTotalRow, sectionEndColumn])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                }

                foreach (var customerType in customerTypeNames)
                {
                    if (!overallMetricsByCustomerType.TryGetValue(customerType, out var overallMetric))
                    {
                        throw new ArgumentException("No customer type");
                    }

                    gmReportWorksheet.Cells[rowForSummary, 2].Value = customerType;
                    gmReportWorksheet.Cells[rowForSummary, 3].Value = overallMetric.Quantity;
                    gmReportWorksheet.Cells[rowForSummary, 4].Value = overallMetric.NetOfSales;
                    gmReportWorksheet.Cells[rowForSummary, 5].Value = overallMetric.NetOfPurchases;
                    gmReportWorksheet.Cells[rowForSummary, 6].Value = overallMetric.GrossMargin;
                    gmReportWorksheet.Cells[rowForSummary, 7].Value = overallMetric.NetOfFreight;
                    gmReportWorksheet.Cells[rowForSummary, 8].Value = overallMetric.Commission;
                    gmReportWorksheet.Cells[rowForSummary, 9].Value = overallMetric.NetMargin;
                    gmReportWorksheet.Cells[rowForSummary, 10].Value = ComputeAverage(overallMetric.NetMargin, overallMetric.Quantity);

                    gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                    if (productMetricsByCustomerType.TryGetValue(customerType, out var productMetrics))
                    {
                        for (var index = 0; index < grossMarginProductList.Count; index++)
                        {
                            var productName = grossMarginProductList[index];
                            var sectionStartColumn = productSummaryStartColumn + index * (productSummaryWidth + productSummarySpacing);

                            if (!productMetrics.TryGetValue(productName, out var productMetric))
                            {
                                continue;
                            }

                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn].Value = productMetric.Quantity;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 1].Value = productMetric.NetOfSales;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 2].Value = productMetric.NetOfPurchases;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 3].Value = productMetric.GrossMargin;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 4].Value = productMetric.NetOfFreight;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 5].Value = productMetric.Commission;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 6].Value = productMetric.NetMargin;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 7].Value = ComputeAverage(productMetric.NetMargin, productMetric.Quantity);

                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 1].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 2].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 7].Style.Numberformat.Format = currencyFormat;
                        }
                    }

                    rowForSummary++;
                }

                gmReportWorksheet.Cells[rowForSummary, 2].Value = "Total";
                gmReportWorksheet.Cells[rowForSummary, 3].Value = totalOverallMetric.Quantity;
                gmReportWorksheet.Cells[rowForSummary, 4].Value = totalOverallMetric.NetOfSales;
                gmReportWorksheet.Cells[rowForSummary, 5].Value = totalOverallMetric.NetOfPurchases;
                gmReportWorksheet.Cells[rowForSummary, 6].Value = totalOverallMetric.GrossMargin;
                gmReportWorksheet.Cells[rowForSummary, 7].Value = totalOverallMetric.NetOfFreight;
                gmReportWorksheet.Cells[rowForSummary, 8].Value = totalOverallMetric.Commission;
                gmReportWorksheet.Cells[rowForSummary, 9].Value = totalOverallMetric.NetMargin;
                gmReportWorksheet.Cells[rowForSummary, 10].Value = ComputeAverage(totalOverallMetric.NetMargin, totalOverallMetric.Quantity);

                gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                for (var index = 0; index < grossMarginProductList.Count; index++)
                {
                    var productName = grossMarginProductList[index];
                    var sectionStartColumn = productSummaryStartColumn + index * (productSummaryWidth + productSummarySpacing);

                    if (!totalProductMetrics.TryGetValue(productName, out var totalProductMetric))
                    {
                        continue;
                    }

                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn].Value = totalProductMetric.Quantity;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 1].Value = totalProductMetric.NetOfSales;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 2].Value = totalProductMetric.NetOfPurchases;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 3].Value = totalProductMetric.GrossMargin;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 4].Value = totalProductMetric.NetOfFreight;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 5].Value = totalProductMetric.Commission;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 6].Value = totalProductMetric.NetMargin;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 7].Value = ComputeAverage(totalProductMetric.NetMargin, totalProductMetric.Quantity);

                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 1].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 2].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, sectionStartColumn + 7].Style.Numberformat.Format = currencyFormat;
                }

                #endregion -- Summary Row --

                // Auto-fit columns for better readability
                gmReportWorksheet.Cells.AutoFitColumns();
                gmReportWorksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate gross margin report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"GM_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate gross margin report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(GrossMarginReport));
            }
        }

        #endregion -- Generate Gross Margin Report as Excel File --

        [HttpGet]
        public IActionResult TradePayableReport()
        {
            return View();
        }

        #region -- Generated Trade Payable Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GenerateTradePayableReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (companyClaims == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(TradePayableReport));
            }

            try
            {
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Include(rr => rr.PurchaseOrder).ThenInclude(po => po!.Supplier)
                    .Where(rr => rr.Company == companyClaims && rr.Date <= model.DateTo)
                    .OrderBy(rr => rr.Date.Year)
                    .ThenBy(rr => rr.Date.Month)
                    .ThenBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName)
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .ToListAsync(cancellationToken: cancellationToken);

                if (!receivingReports.Any())
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(TradePayableReport));
                }

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Times New Roman"));

                        #endregion -- Page setup

                        #region -- Header

                        var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");

                        page.Header().Height(50).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item()
                                    .Text("TRADE PAYABLE REPORT")
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

                        #endregion -- Header

                        #region -- Content

                        page.Content().PaddingTop(10).Column(col =>
                        {
                            col.Item().Table(table =>
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
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                #endregion -- Columns Definition

                                #region -- Table Header

                                table.Header(header =>
                                {
                                    header.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("AP TRADE").SemiBold();
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("BEGINNING").SemiBold();
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PURCHASES").SemiBold();
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("PAYMENTS").SemiBold();
                                    header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("ENDING").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Month").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Supplier").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Amount").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Amount").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Amount").SemiBold();

                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Volume").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Gross").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("EWT").SemiBold();
                                    header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text("Net Amount").SemiBold();
                                });

                                #endregion -- Table Header

                                #region -- Initialize Variable for Computation

                                var grandTotalBeginningVolume = 0m;
                                var grandTotalBeginningGross = 0m;
                                var grandTotalBeginningEwt = 0m;
                                var grandTotalBeginningNetAmount = 0m;
                                var grandTotalPurchaseVolume = 0m;
                                var grandTotalPurchaseGross = 0m;
                                var grandTotalPurchaseEwt = 0m;
                                var grandTotalPurchaseNetAmount = 0m;
                                var grandTotalPaymentVolume = 0m;
                                var grandTotalPaymentGross = 0m;
                                var grandTotalPaymentEwt = 0m;
                                var grandTotalPaymentNetAmount = 0m;
                                var grandTotalEndingVolume = 0m;
                                var grandTotalEndingGross = 0m;
                                var grandTotalEndingEwt = 0m;
                                var grandTotalEndingNetAmount = 0m;
                                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                                #endregion -- Initialize Variable for Computation

                                #region -- Loop to Show Records

                                foreach (var rr in receivingReports)
                                {
                                    bool isStart = true;
                                    var isSupplierTaxable = rr.First().PurchaseOrder!.TaxType == SD.TaxType_WithTax;
                                    var isSupplierVatable = rr.First().PurchaseOrder!.VatType == SD.VatType_Vatable;

                                    //BEGINNING
                                    var subTotalBeginningVolume = rr.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.QuantityReceived);
                                    var subTotalBeginningGross = rr.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.Amount);
                                    var subTotalBeginningNetOfVat = isSupplierVatable
                                        ? NetOfVatOrZero(subTotalBeginningGross)
                                        : subTotalBeginningGross;
                                    var subTotalBeginningEwt = isSupplierTaxable
                                        ? EwtAmountOrZero(subTotalBeginningNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalBeginningNetAmount = RoundToFour(subTotalBeginningGross - subTotalBeginningEwt);
                                    //PURCHASES
                                    var subTotalPurchaseVolume = rr.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.QuantityReceived);
                                    var subTotalPurchaseGross = rr.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.Amount);
                                    var subTotalPurchaseNetOfVat = isSupplierVatable && subTotalPurchaseGross != 0
                                        ? NetOfVatOrZero(subTotalPurchaseGross)
                                        : subTotalPurchaseGross;
                                    var subTotalPurchaseEwt = isSupplierTaxable && subTotalPurchaseNetOfVat != 0
                                        ? EwtAmountOrZero(subTotalPurchaseNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalPurchaseNetAmount = RoundToFour(subTotalPurchaseGross - subTotalPurchaseEwt);
                                    //PAYMENT
                                    var subTotalPaymentVolume = rr.Where(x => x.IsPaid).Sum(x => x.QuantityReceived);
                                    var subTotalPaymentGross = rr.Where(x => x.IsPaid).Sum(x => x.Amount);
                                    var subTotalPaymentNetOfVat = isSupplierVatable && subTotalPaymentGross != 0
                                        ? NetOfVatOrZero(subTotalPaymentGross)
                                        : subTotalPaymentGross;
                                    var subTotalPaymentEwt = isSupplierTaxable && subTotalPaymentNetOfVat != 0
                                        ? EwtAmountOrZero(subTotalPaymentNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalPaymentNetAmount = RoundToFour(subTotalPaymentGross - subTotalPaymentEwt);
                                    //ENDING BALANCE
                                    var subTotalEndingVolume = RoundToFour((subTotalBeginningVolume + subTotalPurchaseVolume) - subTotalPaymentVolume);
                                    var subTotalEndingGross = RoundToFour((subTotalBeginningGross + subTotalPurchaseGross) - subTotalPaymentGross);
                                    var subTotalEndingEwt = RoundToFour((subTotalBeginningEwt + subTotalPurchaseEwt) - subTotalPaymentEwt);
                                    var subTotalEndingNetAmount = RoundToFour((subTotalBeginningNetAmount + subTotalPurchaseNetAmount) - subTotalPaymentNetAmount);

                                    var groupBySupplier = rr.GroupBy(x => new { x.PurchaseOrder?.Supplier?.SupplierName }).ToList();

                                    foreach (var item in groupBySupplier)
                                    {
                                        //BEGINNING
                                        var beginningVolume = item.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.QuantityReceived);
                                        var beginningGross = item.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.Amount);
                                        var beginningNetOfVat = isSupplierVatable && beginningGross != 0
                                            ? NetOfVatOrZero(beginningGross)
                                            : beginningGross;
                                        var beginningEwt = isSupplierTaxable && beginningNetOfVat != 0
                                            ? EwtAmountOrZero(beginningNetOfVat, 0.01m)
                                            : 0m;
                                        var beginningNetAmount = RoundToFour(beginningGross - beginningEwt);
                                        //PURCHASES
                                        var purchaseVolume = item.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.QuantityReceived);
                                        var purchaseGross = item.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.Amount);
                                        var purchaseNetOfVat = isSupplierVatable && purchaseGross != 0
                                            ? NetOfVatOrZero(purchaseGross)
                                            : purchaseGross;
                                        var purchaseEwt = isSupplierTaxable && purchaseNetOfVat != 0
                                            ? EwtAmountOrZero(purchaseNetOfVat, 0.01m)
                                            : 0m;
                                        var purchaseNetAmount = RoundToFour(purchaseGross - purchaseEwt);
                                        //PAYMENT
                                        var paymentVolume = item.Where(x => x.IsPaid).Sum(x => x.QuantityReceived);
                                        var paymentGross = item.Where(x => x.IsPaid).Sum(x => x.Amount);
                                        var paymentNetOfVat = isSupplierVatable && paymentGross != 0
                                            ? NetOfVatOrZero(paymentGross)
                                            : paymentGross;
                                        var paymentEwt = isSupplierTaxable && paymentNetOfVat != 0
                                            ? EwtAmountOrZero(paymentNetOfVat, 0.01m)
                                            : 0m;
                                        var paymentNetAmount = RoundToFour(paymentGross - paymentEwt);
                                        //ENDING BALANCE
                                        var endingVolume = RoundToFour((beginningVolume + purchaseVolume) - paymentVolume);
                                        var endingGross = RoundToFour((beginningGross + purchaseGross) - paymentGross);
                                        var endingEwt = RoundToFour((beginningEwt + purchaseEwt) - paymentEwt);
                                        var endingNetAmount = RoundToFour((beginningNetAmount + purchaseNetAmount) - paymentNetAmount);

                                        if (isStart)
                                        {
                                            table.Cell().RowSpan((uint)groupBySupplier.Count).Border(0.5f).Padding(3).AlignCenter().AlignMiddle().Text($"{new DateTime(rr.Key.Year, rr.Key.Month, 1):MMM yyyy}");
                                        }
                                        table.Cell().Border(0.5f).Padding(3).Text(item.Key.SupplierName);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningVolume != 0 ? beginningVolume < 0 ? $"({Math.Abs(beginningVolume).ToString(SD.Two_Decimal_Format)})" : beginningVolume.ToString(SD.Two_Decimal_Format) : null).FontColor(beginningVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningGross != 0 ? beginningGross < 0 ? $"({Math.Abs(beginningGross).ToString(SD.Two_Decimal_Format)})" : beginningGross.ToString(SD.Two_Decimal_Format) : null).FontColor(beginningGross < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningEwt != 0 ? beginningEwt < 0 ? $"({Math.Abs(beginningEwt).ToString(SD.Two_Decimal_Format)})" : beginningEwt.ToString(SD.Two_Decimal_Format) : null).FontColor(beginningEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(beginningNetAmount != 0 ? beginningNetAmount < 0 ? $"({Math.Abs(beginningNetAmount).ToString(SD.Two_Decimal_Format)})" : beginningNetAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(beginningNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(purchaseVolume != 0 ? purchaseVolume < 0 ? $"({Math.Abs(purchaseVolume).ToString(SD.Two_Decimal_Format)})" : purchaseVolume.ToString(SD.Two_Decimal_Format) : null).FontColor(purchaseVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(purchaseGross != 0 ? purchaseGross < 0 ? $"({Math.Abs(purchaseGross).ToString(SD.Two_Decimal_Format)})" : purchaseGross.ToString(SD.Two_Decimal_Format) : null).FontColor(purchaseGross < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(purchaseEwt != 0 ? purchaseEwt < 0 ? $"({Math.Abs(purchaseEwt).ToString(SD.Two_Decimal_Format)})" : purchaseEwt.ToString(SD.Two_Decimal_Format) : null).FontColor(purchaseEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(purchaseNetAmount != 0 ? purchaseNetAmount < 0 ? $"({Math.Abs(purchaseNetAmount).ToString(SD.Two_Decimal_Format)})" : purchaseNetAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(purchaseNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(paymentVolume != 0 ? paymentVolume < 0 ? $"({Math.Abs(paymentVolume).ToString(SD.Two_Decimal_Format)})" : paymentVolume.ToString(SD.Two_Decimal_Format) : null).FontColor(paymentVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(paymentGross != 0 ? paymentGross < 0 ? $"({Math.Abs(paymentGross).ToString(SD.Two_Decimal_Format)})" : paymentGross.ToString(SD.Two_Decimal_Format) : null).FontColor(paymentGross < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(paymentEwt != 0 ? paymentEwt < 0 ? $"({Math.Abs(paymentEwt).ToString(SD.Two_Decimal_Format)})" : paymentEwt.ToString(SD.Two_Decimal_Format) : null).FontColor(paymentEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(paymentNetAmount != 0 ? paymentNetAmount < 0 ? $"({Math.Abs(paymentNetAmount).ToString(SD.Two_Decimal_Format)})" : paymentNetAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(paymentNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingVolume != 0 ? endingVolume < 0 ? $"({Math.Abs(endingVolume).ToString(SD.Two_Decimal_Format)})" : endingVolume.ToString(SD.Two_Decimal_Format) : null).FontColor(endingVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingGross != 0 ? endingGross < 0 ? $"({Math.Abs(endingGross).ToString(SD.Two_Decimal_Format)})" : endingGross.ToString(SD.Two_Decimal_Format) : null).FontColor(endingGross < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingEwt != 0 ? endingEwt < 0 ? $"({Math.Abs(endingEwt).ToString(SD.Two_Decimal_Format)})" : endingEwt.ToString(SD.Two_Decimal_Format) : null).FontColor(endingEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                        table.Cell().Border(0.5f).Padding(3).AlignRight().Text(endingNetAmount != 0 ? endingNetAmount < 0 ? $"({Math.Abs(endingNetAmount).ToString(SD.Two_Decimal_Format)})" : endingNetAmount.ToString(SD.Two_Decimal_Format) : null).FontColor(endingNetAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                        isStart = false;
                                    }

                                    //Compute sub total
                                    table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("Sub Total:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalBeginningVolume != 0 ? subTotalBeginningVolume < 0 ? $"({Math.Abs(subTotalBeginningVolume).ToString(SD.Two_Decimal_Format)})" : subTotalBeginningVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalBeginningVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalBeginningGross != 0 ? subTotalBeginningGross < 0 ? $"({Math.Abs(subTotalBeginningGross).ToString(SD.Two_Decimal_Format)})" : subTotalBeginningGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalBeginningGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalBeginningEwt != 0 ? subTotalBeginningEwt < 0 ? $"({Math.Abs(subTotalBeginningEwt).ToString(SD.Two_Decimal_Format)})" : subTotalBeginningEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalBeginningEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalBeginningNetAmount != 0 ? subTotalBeginningNetAmount < 0 ? $"({Math.Abs(subTotalBeginningNetAmount).ToString(SD.Two_Decimal_Format)})" : subTotalBeginningNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalBeginningNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPurchaseVolume != 0 ? subTotalPurchaseVolume < 0 ? $"({Math.Abs(subTotalPurchaseVolume).ToString(SD.Two_Decimal_Format)})" : subTotalPurchaseVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPurchaseVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPurchaseGross != 0 ? subTotalPurchaseGross < 0 ? $"({Math.Abs(subTotalPurchaseGross).ToString(SD.Two_Decimal_Format)})" : subTotalPurchaseGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPurchaseGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPurchaseEwt != 0 ? subTotalPurchaseEwt < 0 ? $"({Math.Abs(subTotalPurchaseEwt).ToString(SD.Two_Decimal_Format)})" : subTotalPurchaseEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPurchaseEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPurchaseNetAmount != 0 ? subTotalPurchaseNetAmount < 0 ? $"({Math.Abs(subTotalPurchaseNetAmount).ToString(SD.Two_Decimal_Format)})" : subTotalPurchaseNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPurchaseNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPaymentVolume != 0 ? subTotalPaymentVolume < 0 ? $"({Math.Abs(subTotalPaymentVolume).ToString(SD.Two_Decimal_Format)})" : subTotalPaymentVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPaymentVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPaymentGross != 0 ? subTotalPaymentGross < 0 ? $"({Math.Abs(subTotalPaymentGross).ToString(SD.Two_Decimal_Format)})" : subTotalPaymentGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPaymentGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPaymentEwt != 0 ? subTotalPaymentEwt < 0 ? $"({Math.Abs(subTotalPaymentEwt).ToString(SD.Two_Decimal_Format)})" : subTotalPaymentEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPaymentEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalPaymentNetAmount != 0 ? subTotalPaymentNetAmount < 0 ? $"({Math.Abs(subTotalPaymentNetAmount).ToString(SD.Two_Decimal_Format)})" : subTotalPaymentNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalPaymentNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEndingVolume != 0 ? subTotalEndingVolume < 0 ? $"({Math.Abs(subTotalEndingVolume).ToString(SD.Two_Decimal_Format)})" : subTotalEndingVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalEndingVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEndingGross != 0 ? subTotalEndingGross < 0 ? $"({Math.Abs(subTotalEndingGross).ToString(SD.Two_Decimal_Format)})" : subTotalEndingGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalEndingGross < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEndingEwt != 0 ? subTotalEndingEwt < 0 ? $"({Math.Abs(subTotalEndingEwt).ToString(SD.Two_Decimal_Format)})" : subTotalEndingEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalEndingEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(subTotalEndingNetAmount != 0 ? subTotalEndingNetAmount < 0 ? $"({Math.Abs(subTotalEndingNetAmount).ToString(SD.Two_Decimal_Format)})" : subTotalEndingNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(subTotalEndingNetAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                    grandTotalBeginningVolume += subTotalBeginningVolume;
                                    grandTotalBeginningGross += subTotalBeginningGross;
                                    grandTotalBeginningEwt += subTotalBeginningEwt;
                                    grandTotalBeginningNetAmount += subTotalBeginningNetAmount;
                                    grandTotalPurchaseVolume += subTotalPurchaseVolume;
                                    grandTotalPurchaseGross += subTotalPurchaseGross;
                                    grandTotalPurchaseEwt += subTotalPurchaseEwt;
                                    grandTotalPurchaseNetAmount += subTotalPurchaseNetAmount;
                                    grandTotalPaymentVolume += subTotalPaymentVolume;
                                    grandTotalPaymentGross += subTotalPaymentGross;
                                    grandTotalPaymentEwt += subTotalPaymentEwt;
                                    grandTotalPaymentNetAmount += subTotalPaymentNetAmount;
                                    grandTotalEndingVolume += subTotalEndingVolume;
                                    grandTotalEndingGross += subTotalEndingGross;
                                    grandTotalEndingEwt += subTotalEndingEwt;
                                    grandTotalEndingNetAmount += subTotalEndingNetAmount;
                                }

                                #endregion -- Loop to Show Records

                                #region -- Create Table Cell for Totals

                                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("GRAND TOTALS:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalBeginningVolume != 0 ? grandTotalBeginningVolume < 0 ? $"({Math.Abs(grandTotalBeginningVolume).ToString(SD.Two_Decimal_Format)})" : grandTotalBeginningVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalBeginningVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalBeginningGross != 0 ? grandTotalBeginningGross < 0 ? $"({Math.Abs(grandTotalBeginningGross).ToString(SD.Two_Decimal_Format)})" : grandTotalBeginningGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalBeginningGross < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalBeginningEwt != 0 ? grandTotalBeginningEwt < 0 ? $"({Math.Abs(grandTotalBeginningEwt).ToString(SD.Two_Decimal_Format)})" : grandTotalBeginningEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalBeginningEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalBeginningNetAmount != 0 ? grandTotalBeginningNetAmount < 0 ? $"({Math.Abs(grandTotalBeginningNetAmount).ToString(SD.Two_Decimal_Format)})" : grandTotalBeginningNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalBeginningNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPurchaseVolume != 0 ? grandTotalPurchaseVolume < 0 ? $"({Math.Abs(grandTotalPurchaseVolume).ToString(SD.Two_Decimal_Format)})" : grandTotalPurchaseVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPurchaseVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPurchaseGross != 0 ? grandTotalPurchaseGross < 0 ? $"({Math.Abs(grandTotalPurchaseGross).ToString(SD.Two_Decimal_Format)})" : grandTotalPurchaseGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPurchaseGross < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPurchaseEwt != 0 ? grandTotalPurchaseEwt < 0 ? $"({Math.Abs(grandTotalPurchaseEwt).ToString(SD.Two_Decimal_Format)})" : grandTotalPurchaseEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPurchaseEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPurchaseNetAmount != 0 ? grandTotalPurchaseNetAmount < 0 ? $"({Math.Abs(grandTotalPurchaseNetAmount).ToString(SD.Two_Decimal_Format)})" : grandTotalPurchaseNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPurchaseNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPaymentVolume != 0 ? grandTotalPaymentVolume < 0 ? $"({Math.Abs(grandTotalPaymentVolume).ToString(SD.Two_Decimal_Format)})" : grandTotalPaymentVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPaymentVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPaymentGross != 0 ? grandTotalPaymentGross < 0 ? $"({Math.Abs(grandTotalPaymentGross).ToString(SD.Two_Decimal_Format)})" : grandTotalPaymentGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPaymentGross < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPaymentEwt != 0 ? grandTotalPaymentEwt < 0 ? $"({Math.Abs(grandTotalPaymentEwt).ToString(SD.Two_Decimal_Format)})" : grandTotalPaymentEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPaymentEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalPaymentNetAmount != 0 ? grandTotalPaymentNetAmount < 0 ? $"({Math.Abs(grandTotalPaymentNetAmount).ToString(SD.Two_Decimal_Format)})" : grandTotalPaymentNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalPaymentNetAmount < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalEndingVolume != 0 ? grandTotalEndingVolume < 0 ? $"({Math.Abs(grandTotalEndingVolume).ToString(SD.Two_Decimal_Format)})" : grandTotalEndingVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalEndingVolume < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalEndingGross != 0 ? grandTotalEndingGross < 0 ? $"({Math.Abs(grandTotalEndingGross).ToString(SD.Two_Decimal_Format)})" : grandTotalEndingGross.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalEndingGross < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalEndingEwt != 0 ? grandTotalEndingEwt < 0 ? $"({Math.Abs(grandTotalEndingEwt).ToString(SD.Two_Decimal_Format)})" : grandTotalEndingEwt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalEndingEwt < 0 ? Colors.Red.Medium : Colors.Black);
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(grandTotalEndingNetAmount != 0 ? grandTotalEndingNetAmount < 0 ? $"({Math.Abs(grandTotalEndingNetAmount).ToString(SD.Two_Decimal_Format)})" : grandTotalEndingNetAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(grandTotalEndingNetAmount < 0 ? Colors.Red.Medium : Colors.Black);

                                #endregion -- Create Table Cell for Totals
                            });
                        });

                        #endregion -- Content

                        #region -- Footer

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });

                        #endregion -- Footer
                    });
                });

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate trade payable report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var pdfBytes = document.GeneratePdf();
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade payable report quest pdf. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradePayableReport));
            }
        }

        #endregion -- Generated Trade Payable Report as Quest PDF

        #region -- Generate Trade Payable Report as Excel File --

        public async Task<IActionResult> GenerateTradePayableReportExcelFile(ViewModelBook viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradePayableReport));
            }

            try
            {
                var dateFrom = viewModel.DateFrom;
                var dateTo = viewModel.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var currencyFormat = "#,##0.00";

                var allCv = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv => cv.Category == "Trade" && cv.CvType == "Supplier" && cv.Date <= dateTo)
                    .Include(cv => cv.Supplier)
                    .ToListAsync(cancellationToken);

                var cvIdOfSelected = allCv
                    .Where(cv => cv.Date >= dateFrom)
                    .Select(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                var cvIdOfPrevious = allCv
                    .Where(cv => cv.Date < dateFrom)
                    .Select(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                var cvPaymentsOfSelected = await _dbContext.FilprideCVTradePayments
                    .Where(ctp => cvIdOfSelected.Contains(ctp.DocumentId) && ctp.DocumentType == "RR")
                    .Include(ctp => ctp.CV)
                    .ToListAsync(cancellationToken);

                var cvPaymentsOfPrevious = await _dbContext.FilprideCVTradePayments
                    .Where(ctp => cvIdOfPrevious.Contains(ctp.DocumentId) && ctp.DocumentType == "RR")
                    .Include(ctp => ctp.CV)
                    .ToListAsync(cancellationToken);

                var idsOfRrsOfSelectedPeriodFromCv = cvPaymentsOfSelected
                    .Select(ctp => new
                    {
                        ReceivingReportId = ctp.DocumentId,
                        ctp.AmountPaid
                    })
                    .ToList();

                var idsOfRrsOfPreviousPeriodsFromCv = cvPaymentsOfPrevious
                    .Select(ctp => new
                    {
                        ReceivingReportId = ctp.DocumentId,
                        ctp.AmountPaid
                    })
                    .ToList();

                var allRr = await _unitOfWork.FilprideReport
                    .GetTradePayableReport(viewModel.DateFrom, viewModel.DateTo, companyClaims, cancellationToken);

                var rrAndAmountPaidForSelectedPeriodFromCv = allRr
                    .Where(rr => idsOfRrsOfSelectedPeriodFromCv.Select(rrSet => rrSet.ReceivingReportId).ToList().Contains(rr.ReceivingReportId) &&
                    rr.Amount != 0m)
                    .Select(rrSet => new RrWithAmountPaidViewModel
                    {
                        ReceivingReport = rrSet,
                        AmountPaid = idsOfRrsOfSelectedPeriodFromCv.FirstOrDefault(rr => rr.ReceivingReportId == rrSet.ReceivingReportId) == null ? 0m :
                            idsOfRrsOfSelectedPeriodFromCv.FirstOrDefault(rr => rr.ReceivingReportId == rrSet.ReceivingReportId)!.AmountPaid
                    })
                    .GroupBy(rr => new MonthYear(
                        rr.ReceivingReport.Date!.Year,
                        rr.ReceivingReport.Date!.Month
                    ));

                var rrAndAmountPaidForPreviousPeriodFromCv = allRr
                    .Where(rr => idsOfRrsOfPreviousPeriodsFromCv.Select(rrSet => rrSet.ReceivingReportId).ToList().Contains(rr.ReceivingReportId) &&
                    rr.Amount != 0m)
                    .Select(rrSet => new RrWithAmountPaidViewModel
                    {
                        ReceivingReport = rrSet,
                        AmountPaid = idsOfRrsOfPreviousPeriodsFromCv.FirstOrDefault(rr => rr.ReceivingReportId == rrSet.ReceivingReportId) == null ? 0m :
                            idsOfRrsOfPreviousPeriodsFromCv.FirstOrDefault(rr => rr.ReceivingReportId == rrSet.ReceivingReportId)!.AmountPaid
                    })
                    .GroupBy(rr => new MonthYear(
                        rr.ReceivingReport.Date!.Year,
                        rr.ReceivingReport.Date!.Month
                    ));

                var allRrGroupedByMonthYear = allRr
                    .GroupBy(rr => new MonthYear(
                        rr.Date!.Year,
                        rr.Date!.Month
                    ));

                var allPreviousRrGroupedByMonthYear = allRr
                    .Where(rr => rr.Date! < dateFrom)
                    .GroupBy(rr => new MonthYear(
                        rr.Date!.Year,
                        rr.Date!.Month
                    ))
                    .ToList();

                var allSelectedRrGroupedByMonthYear = allRr
                    .Where(rr => rr.Date! >= dateFrom)
                    .GroupBy(rr => new MonthYear(
                        rr.Date!.Year,
                        rr.Date!.Month
                    ))
                    .ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Trade Payable");

                #region == Title ==

                var titleCells = worksheet.Cells["A1:B1"];
                titleCells.Merge = true;
                titleCells.Value = "TRADE PAYABLE REPORT";
                titleCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                #endregion == Title ==

                #region == Header Row ==

                titleCells = worksheet.Cells["A7:B7"];
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A7"].Value = "MONTH";
                worksheet.Cells["A7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["B7"].Value = "SUPPLIER";
                worksheet.Cells["B7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                titleCells = worksheet.Cells["A6:B6"];
                titleCells.Merge = true;
                titleCells.Value = "AP TRADE";
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                string[] headers = ["BEGINNING", "PURCHASES", "PAYMENTS", "ENDING"];
                string[] subHeaders = ["VOLUME", "GROSS", "EWT", "NET AMOUNT"];
                var col = 4;

                foreach (var header in headers)
                {
                    foreach (var subheader in subHeaders)
                    {
                        worksheet.Cells[7, col].Value = subheader;
                        worksheet.Cells[7, col].Style.Font.Bold = true;
                        worksheet.Cells[7, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[7, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = col + 1;
                    }

                    titleCells = worksheet.Cells[6, col - 4, 6, col - 1];
                    titleCells.Merge = true;
                    titleCells.Value = header;
                    titleCells.Style.Font.Size = 13;
                    titleCells.Style.Font.Bold = true;
                    titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                    titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    col = col + 1;
                }

                #endregion == Header Row ==

                var row = 8;

                IEnumerable<IGrouping<MonthYear, FilprideReceivingReport>> loopingMainRrGroupedByMonthYear = null!;
                IEnumerable<IGrouping<MonthYear, RrWithAmountPaidViewModel>> loopingSecondRrGroupedByMonthYear = null!;

                #region == Initialize Variables ==

                // subtotals per month/year
                var subtotalVolumeBeginning = 0m;
                var subtotalGrossBeginning = 0m;
                var subtotalEwtBeginning = 0m;
                var subtotalNetBeginning = 0m;

                var subtotalVolumePurchases = 0m;
                var subtotalGrossPurchases = 0m;
                var subtotalEwtPurchases = 0m;
                var subtotalNetPurchases = 0m;

                var subtotalVolumePayments = 0m;
                var subtotalGrossPayments = 0m;
                var subtotalEwtPayments = 0m;
                var subtotalNetPayments = 0m;

                var currentVolumeEnding = 0m;
                var currentGrossEnding = 0m;
                var currentEwtEnding = 0m;
                var currentNetEnding = 0m;

                var grandTotalVolumeBeginning = 0m;
                var grandTotalGrossBeginning = 0m;
                var grandTotalEwtBeginning = 0m;
                var grandTotalNetBeginning = 0m;

                var grandTotalVolumePurchases = 0m;
                var grandTotalGrossPurchases = 0m;
                var grandTotalEwtPurchases = 0m;
                var grandTotalNetPurchases = 0m;

                var grandTotalVolumePayments = 0m;
                var grandTotalGrossPayments = 0m;
                var grandTotalEwtPayments = 0m;
                var grandTotalNetPayments = 0m;

                var grandTotalVolumeEnding = 0m;
                var grandTotalGrossEnding = 0m;
                var grandTotalEwtEnding = 0m;
                var grandTotalNetEnding = 0m;

                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                #endregion == Initialize Variables ==

                foreach (var allRrsSameMonthYear in allRrGroupedByMonthYear)
                {
                    // reset placing per category

                    // get current group of month-year rr
                    // group the rr by supplier
                    var sameMonthYearGroupedBySupplier = allRrsSameMonthYear.GroupBy(rr => rr.PurchaseOrder?.Supplier?.SupplierName)
                        .ToList();

                    // MONTH YEAR LABEL
                    worksheet.Cells[row, 1].Value = (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(sameMonthYearGroupedBySupplier.FirstOrDefault()?.FirstOrDefault()?.Date!.Month ?? 0))
                                                    + " " +
                                                    (sameMonthYearGroupedBySupplier.FirstOrDefault()?.FirstOrDefault()?.Date!.Year.ToString() ?? " ");
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    row++;

                    // LOOP BY SUPPLIER
                    foreach (var sameMonthYearSameSupplier in sameMonthYearGroupedBySupplier)
                    {
                        // NAME OF SUPPLIER
                        var supplierName = sameMonthYearSameSupplier.FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName ?? "";
                        var isSupplierVatable = sameMonthYearSameSupplier.First().PurchaseOrder?.Supplier!.VatType == SD.VatType_Vatable;
                        var isSupplierTaxable = sameMonthYearSameSupplier.First().PurchaseOrder?.Supplier!.TaxType == SD.TaxType_WithTax;
                        worksheet.Cells[row, 2].Value = supplierName;
                        var columnName = string.Empty;
                        var isPayment = false;
                        var isEnding = false;

                        // loop by month-year and supplier
                        for (var i = 1; i != 5; i++)
                        {
                            // determines if the loop is beginning, current, payment, or ending
                            switch (i)
                            {
                                // beginning
                                case 1:
                                    loopingMainRrGroupedByMonthYear = allPreviousRrGroupedByMonthYear;
                                    loopingSecondRrGroupedByMonthYear = rrAndAmountPaidForPreviousPeriodFromCv;
                                    columnName = "beginning";
                                    break;
                                // current
                                case 2:
                                    loopingMainRrGroupedByMonthYear = allSelectedRrGroupedByMonthYear;
                                    loopingSecondRrGroupedByMonthYear = null!;
                                    columnName = "purchases";
                                    break;
                                // payment
                                case 3:
                                    loopingMainRrGroupedByMonthYear = allRrGroupedByMonthYear;
                                    loopingSecondRrGroupedByMonthYear = rrAndAmountPaidForSelectedPeriodFromCv;
                                    columnName = "payments";
                                    break;
                                // ending
                                case 4:
                                    isEnding = true;
                                    break;
                            }

                            if (isPayment)
                            {
                                switch (columnName)
                                {
                                    case "beginning":
                                        loopingSecondRrGroupedByMonthYear = rrAndAmountPaidForPreviousPeriodFromCv;
                                        break;

                                    case "purchases":
                                        loopingSecondRrGroupedByMonthYear = null!;
                                        break;
                                }
                            }

                            if (loopingMainRrGroupedByMonthYear != null)
                            {
                                foreach (var sameMonthYear in loopingMainRrGroupedByMonthYear)
                                {
                                    // this process finds the rr that has the same month/year for current month/year section
                                    if (sameMonthYear.FirstOrDefault()?.Date!.Month != allRrsSameMonthYear.FirstOrDefault()?.Date!.Month ||
                                        sameMonthYear.FirstOrDefault()?.Date!.Year != allRrsSameMonthYear.FirstOrDefault()?.Date!.Year)
                                    {
                                        continue;
                                    }

                                    IEnumerable<RrWithAmountPaidViewModel>? secondLoopSameMonthYearSameSupplier = null;
                                    IGrouping<MonthYear, RrWithAmountPaidViewModel>? secondLoopSameMonthYear = null!;

                                    // GET DR SET WITH SAME MONTH YEAR + SUPPLIER
                                    var sameSupplierSameMonthYear = sameMonthYear
                                        .Where(rr => rr.PurchaseOrder!.Supplier!.SupplierName == sameMonthYearSameSupplier.FirstOrDefault()?.PurchaseOrder!.Supplier!.SupplierName);

                                    var volume = 0m;
                                    var gross = 0m;
                                    var netOfVat = 0m;
                                    var ewtPercentage = 0m;
                                    var ewt = 0m;
                                    var net = 0m;
                                    var totalAmount = 0m;
                                    var totalVolume = 0m;
                                    decimal sumOfAmountPaid = 0m;
                                    decimal sumOfVolumePaid = 0m;

                                    // PROCESS DEPENDING ON CATEGORY
                                    switch (i)
                                    {
                                        // BEGINNING
                                        case 1:
                                            // CONTAINS PREVIOUS PAID
                                            secondLoopSameMonthYear = loopingSecondRrGroupedByMonthYear
                                                .FirstOrDefault(secondLoop => secondLoop.Key == sameMonthYear.Key);

                                            // GET PREVIOUS PAID WITH SAME SUPPLIER
                                            if (secondLoopSameMonthYear != null)
                                            {
                                                secondLoopSameMonthYearSameSupplier = secondLoopSameMonthYear
                                                    .Where(rr => rr.ReceivingReport!.PurchaseOrder!.Supplier!.SupplierName == sameMonthYearSameSupplier
                                                    .FirstOrDefault()?.PurchaseOrder!.Supplier!.SupplierName)
                                                    .ToList();

                                                if (secondLoopSameMonthYearSameSupplier.Count() != 0)
                                                {
                                                    sumOfAmountPaid =
                                                        secondLoopSameMonthYearSameSupplier.Sum(rr => rr.AmountPaid);

                                                    sumOfVolumePaid =
                                                        secondLoopSameMonthYearSameSupplier.Sum(rr => rr.ReceivingReport.QuantityReceived);
                                                }
                                            }

                                            totalAmount = sameSupplierSameMonthYear
                                                .Sum(rr => rr.Amount);

                                            totalVolume = sameSupplierSameMonthYear
                                                  .Sum(rr => rr.QuantityReceived);

                                            gross = totalAmount - sumOfAmountPaid;

                                            volume = totalVolume - sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewtPercentage = sameMonthYear.Average(rr => rr.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? 0m);

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;

                                        // CURRENT
                                        case 2:

                                            totalAmount = sameSupplierSameMonthYear
                                                .Sum(rr => rr.Amount);

                                            totalVolume = sameSupplierSameMonthYear
                                                .Sum(rr => rr.QuantityReceived);

                                            gross = totalAmount - sumOfAmountPaid;

                                            volume = totalVolume - sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewtPercentage = sameMonthYear.Average(dr => dr.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? 0m);

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;

                                        // PAYMENT
                                        case 3:
                                            // CONTAINS SELECTED PAID
                                            secondLoopSameMonthYear = loopingSecondRrGroupedByMonthYear
                                                .FirstOrDefault(secondLoop => secondLoop.Key == sameMonthYear.Key);

                                            // GET PAID WITH SAME SUPPLIER
                                            if (secondLoopSameMonthYear != null)
                                            {
                                                secondLoopSameMonthYearSameSupplier = secondLoopSameMonthYear
                                                    .Where(rr => rr.ReceivingReport.PurchaseOrder!.Supplier!.SupplierName == sameMonthYearSameSupplier.FirstOrDefault()?.PurchaseOrder!.Supplier!.SupplierName);

                                                sumOfAmountPaid =
                                                    secondLoopSameMonthYearSameSupplier.Sum(rr => rr.AmountPaid);

                                                sumOfVolumePaid =
                                                    secondLoopSameMonthYearSameSupplier.Sum(rr => rr.ReceivingReport.QuantityReceived);

                                                ewtPercentage = secondLoopSameMonthYear.Average(rr => rr.ReceivingReport.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? 0m);
                                            }

                                            if (secondLoopSameMonthYearSameSupplier == null)
                                            {
                                                continue;
                                            }

                                            gross = sumOfAmountPaid;

                                            volume = sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;
                                    }

                                    // write in the category
                                    worksheet.Cells[row, (i * 5) - 1].Value = volume;
                                    worksheet.Cells[row, i * 5].Value = gross;
                                    worksheet.Cells[row, (i * 5) + 1].Value = ewt;
                                    worksheet.Cells[row, (i * 5) + 2].Value = net;
                                    worksheet.Cells[row, (i * 5) - 1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, i * 5].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, (i * 5) + 1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, (i * 5) + 2].Style.Numberformat.Format = currencyFormat;

                                    // decide what to do to subtotals depending on category (beg, current, payment)
                                    switch (i)
                                    {
                                        // beginning
                                        case 1:
                                            subtotalVolumeBeginning += volume;
                                            subtotalGrossBeginning += gross;
                                            subtotalEwtBeginning += ewt;
                                            subtotalNetBeginning += net;
                                            currentVolumeEnding += volume;
                                            currentGrossEnding += gross;
                                            currentEwtEnding += ewt;
                                            currentNetEnding += net;
                                            break;
                                        // current
                                        case 2:
                                            subtotalVolumePurchases += volume;
                                            subtotalGrossPurchases += gross;
                                            subtotalEwtPurchases += ewt;
                                            subtotalNetPurchases += net;
                                            currentVolumeEnding += volume;
                                            currentGrossEnding += gross;
                                            currentEwtEnding += ewt;
                                            currentNetEnding += net;
                                            break;
                                        // payment
                                        case 3:
                                            subtotalVolumePayments += volume;
                                            subtotalGrossPayments += gross;
                                            subtotalEwtPayments += ewt;
                                            subtotalNetPayments += net;
                                            currentVolumeEnding -= volume;
                                            currentGrossEnding -= gross;
                                            currentEwtEnding -= ewt;
                                            currentNetEnding -= net;
                                            break;
                                    }
                                }
                            }

                            if (isEnding)
                            {
                                worksheet.Cells[row, 19].Value = currentVolumeEnding;
                                worksheet.Cells[row, 20].Value = currentGrossEnding;
                                worksheet.Cells[row, 21].Value = currentEwtEnding;
                                worksheet.Cells[row, 22].Value = currentNetEnding;
                                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                                currentVolumeEnding = 0m;
                                currentGrossEnding = 0m;
                                currentEwtEnding = 0m;
                                currentNetEnding = 0m;
                            }

                            isPayment = false;
                        }

                        // after the four columns(beginning, current, payment, ending), next is supplier
                        row++;
                    }

                    #region == Subtotal Inputting ==

                    // after all supplier, input subtotals if not zero
                    if (subtotalGrossBeginning != 0m)
                    {
                        worksheet.Cells[row, 4].Value = subtotalVolumeBeginning;
                        worksheet.Cells[row, 5].Value = subtotalGrossBeginning;
                        worksheet.Cells[row, 6].Value = subtotalEwtBeginning;
                        worksheet.Cells[row, 7].Value = subtotalNetBeginning;

                        using var range = worksheet.Cells[row, 4, row, 7];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }
                    if (subtotalGrossPurchases != 0m)
                    {
                        worksheet.Cells[row, 9].Value = subtotalVolumePurchases;
                        worksheet.Cells[row, 10].Value = subtotalGrossPurchases;
                        worksheet.Cells[row, 11].Value = subtotalEwtPurchases;
                        worksheet.Cells[row, 12].Value = subtotalNetPurchases;

                        using var range = worksheet.Cells[row, 9, row, 12];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }
                    if (subtotalGrossPayments != 0m)
                    {
                        worksheet.Cells[row, 14].Value = subtotalVolumePayments;
                        worksheet.Cells[row, 15].Value = subtotalGrossPayments;
                        worksheet.Cells[row, 16].Value = subtotalEwtPayments;
                        worksheet.Cells[row, 17].Value = subtotalNetPayments;

                        using var range = worksheet.Cells[row, 14, row, 17];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }

                    #endregion == Subtotal Inputting ==

                    #region == Ending Subtotal and Grand Total Processes ==

                    // input subtotal of ending
                    var subtotalVolumeEnding = RoundToFour(subtotalVolumeBeginning + subtotalVolumePurchases - subtotalVolumePayments);
                    var subtotalGrossEnding = RoundToFour(subtotalGrossBeginning + subtotalGrossPurchases - subtotalGrossPayments);
                    var subtotalEwtEnding = RoundToFour(subtotalEwtBeginning + subtotalEwtPurchases - subtotalEwtPayments);
                    var subtotalNetEnding = RoundToFour(subtotalNetBeginning + subtotalNetPurchases - subtotalNetPayments);

                    worksheet.Cells[row, 19].Value = subtotalVolumeEnding;
                    worksheet.Cells[row, 20].Value = subtotalGrossEnding;
                    worksheet.Cells[row, 21].Value = subtotalEwtEnding;
                    worksheet.Cells[row, 22].Value = subtotalNetEnding;

                    using (var range = worksheet.Cells[row, 19, row, 22])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }

                    // after inputting all subtotals, next row
                    row++;

                    // after inputting all subtotals, add subtotals to grand total
                    grandTotalVolumeBeginning += subtotalVolumeBeginning;
                    grandTotalGrossBeginning += subtotalGrossBeginning;
                    grandTotalEwtBeginning += subtotalEwtBeginning;
                    grandTotalNetBeginning += subtotalNetBeginning;

                    grandTotalVolumePurchases += subtotalVolumePurchases;
                    grandTotalGrossPurchases += subtotalGrossPurchases;
                    grandTotalEwtPurchases += subtotalEwtPurchases;
                    grandTotalNetPurchases += subtotalNetPurchases;

                    grandTotalVolumePayments += subtotalVolumePayments;
                    grandTotalGrossPayments += subtotalGrossPayments;
                    grandTotalEwtPayments += subtotalEwtPayments;
                    grandTotalNetPayments += subtotalNetPayments;

                    grandTotalVolumeEnding += subtotalVolumeEnding;
                    grandTotalGrossEnding += subtotalGrossEnding;
                    grandTotalEwtEnding += subtotalEwtEnding;
                    grandTotalNetEnding += subtotalNetEnding;

                    // reset subtotals
                    subtotalVolumePurchases = 0m;
                    subtotalGrossPurchases = 0m;
                    subtotalEwtPurchases = 0m;
                    subtotalNetPurchases = 0m;
                    subtotalVolumeBeginning = 0m;
                    subtotalGrossBeginning = 0m;
                    subtotalEwtBeginning = 0m;
                    subtotalNetBeginning = 0m;
                    currentVolumeEnding = 0m;
                    currentGrossEnding = 0m;
                    currentEwtEnding = 0m;
                    currentNetEnding = 0m;
                    subtotalVolumePayments = 0m;
                    subtotalGrossPayments = 0m;
                    subtotalEwtPayments = 0m;
                    subtotalNetPayments = 0m;

                    #endregion == Ending Subtotal and Grand Total Processes ==
                }

                row++;

                #region == Grand Total Inputting ==

                worksheet.Cells[row, 2].Value = "GRAND TOTAL:";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 4].Value = grandTotalVolumeBeginning;
                worksheet.Cells[row, 5].Value = grandTotalGrossBeginning;
                worksheet.Cells[row, 6].Value = grandTotalEwtBeginning;
                worksheet.Cells[row, 7].Value = grandTotalNetBeginning;
                worksheet.Cells[row, 9].Value = grandTotalVolumePurchases;
                worksheet.Cells[row, 10].Value = grandTotalGrossPurchases;
                worksheet.Cells[row, 11].Value = grandTotalEwtPurchases;
                worksheet.Cells[row, 12].Value = grandTotalNetPurchases;
                worksheet.Cells[row, 14].Value = grandTotalVolumePayments;
                worksheet.Cells[row, 15].Value = grandTotalGrossPayments;
                worksheet.Cells[row, 16].Value = grandTotalEwtPayments;
                worksheet.Cells[row, 17].Value = grandTotalNetPayments;
                worksheet.Cells[row, 19].Value = grandTotalVolumeEnding;
                worksheet.Cells[row, 20].Value = grandTotalGrossEnding;
                worksheet.Cells[row, 21].Value = grandTotalEwtEnding;
                worksheet.Cells[row, 22].Value = grandTotalNetEnding;

                using (var range = worksheet.Cells[row, 4, row, 22])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Numberformat.Format = currencyFormat;
                }

                using (var range = worksheet.Cells[row, 1, row, 22])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                #endregion == Grand Total Inputting ==

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(3).Width = 1;
                worksheet.Column(8).Width = 1;
                worksheet.Column(13).Width = 1;
                worksheet.Column(18).Width = 1;
                worksheet.View.FreezePanes(8, 2);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate trade payable report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Payable_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade payable report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradePayableReport));
            }
        }

        #endregion -- Generate Trade Payable Report as Excel File --

        [HttpGet]
        public IActionResult ApReport()
        {
            return View();
        }

        #region -- Generate Ap Report Excel File --

        [HttpPost]
        public async Task<IActionResult> ApReportExcelFile(DateOnly monthYear, CancellationToken cancellationToken)
        {
            try
            {
                if (monthYear == default)
                {
                    TempData["error"] = "Please enter a valid month";
                    return RedirectToAction(nameof(ApReport));
                }

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                // string currencyFormat = "#,##0.0000";
                string currencyFormatTwoDecimal = "#,##0.00";
                var periodStart = new DateOnly(monthYear.Year, monthYear.Month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                // fetch for this month and back
                var apReport = await _unitOfWork.FilprideReport.GetApReport(monthYear, companyClaims, cancellationToken);

                if (apReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(ApReport));
                }

                #region == TOPSHEET ==

                // Create the Excel package
                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add("TOPSHEET");
                worksheet.Cells.Style.Font.Name = "Calibri";

                worksheet.Cells[1, 2].Value = "Summary of Purchases";
                worksheet.Cells[1, 2].Style.Font.Bold = true;
                worksheet.Cells[2, 2].Value = $"AP Monitoring Report for the month of {monthYear.ToString("MMMM")} {monthYear.Year}";
                worksheet.Cells[3, 2].Value = "Filpride Resources, Inc.";
                worksheet.Cells[4, 2].Value = $"Date and Time Generated: {DateTimeHelper.GetCurrentPhilippineTime()}";
                worksheet.Cells[1, 2, 3, 2].Style.Font.Size = 14;

                worksheet.Cells[5, 2].Value = "SUPPLIER";
                worksheet.Cells[5, 3].Value = "BUYER";
                worksheet.Cells[5, 4].Value = "PRODUCT";
                worksheet.Cells[5, 5].Value = "PAYMENT TERMS";
                worksheet.Cells[5, 6].Value = "TYPE OF PURCHASE";
                worksheet.Cells[5, 7].Value = "ORIGINAL PO VOLUME";
                worksheet.Cells[5, 8].Value = "UNLIFTED LAST MONTH";
                worksheet.Cells[5, 9].Value = "LIFTED THIS MONTH";
                worksheet.Cells[5, 10].Value = "UNLIFTED THIS MONTH";
                worksheet.Cells[5, 11].Value = "PRICE(VAT-EX)";
                worksheet.Cells[5, 12].Value = "PRICE (VAT-INC)";
                worksheet.Cells[5, 13].Value = "GROSS AMOUNT";
                worksheet.Cells[5, 14].Value = "EWT";
                worksheet.Cells[5, 15].Value = "NET OF EWT";

                using (var range = worksheet.Cells[5, 2, 5, 15])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 172));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Row(5).Height = 36;

                var groupBySupplier = apReport
                    .OrderBy(po => po.Date)
                    .ThenBy(po => po.PurchaseOrderNo)
                    .GroupBy(po => new
                    {
                        po.SupplierId,
                        po.SupplierName
                    })
                    .ToList();

                var groupBySupplierTermsAndType = apReport
                    .GroupBy(po => new
                    {
                        po.SupplierId,
                        po.SupplierName,
                        po.Company,
                        po.Terms
                    })
                    .OrderBy(po => po.Key.SupplierName)
                    .ThenBy(po => po.Key.Terms)
                    .ToList();

                int row = 5;
                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;
                var productList = GetOrderedProductNames(
                    groupBySupplierTermsAndType.SelectMany(group => group),
                    po => po.ProductName);
                var grandTotalsByProduct = CreatePurchaseOrderSummaryMetricMap(productList);

                foreach (var sameSupplierGroup in groupBySupplierTermsAndType)
                {
                    row += 2;
                    worksheet.Cells[row, 2].Value = sameSupplierGroup.Key.SupplierName;
                    worksheet.Cells[row, 2].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Value = sameSupplierGroup.Key.Company;
                    var groupByProduct = sameSupplierGroup
                        .GroupBy(po => new
                        {
                            po.ProductId,
                            po.ProductName
                        })
                        .OrderBy(po => po.Key.ProductName)
                        .ToList();
                    var distinctTypesOfPurchase = sameSupplierGroup
                        .Select(po => po.TypeOfPurchase)
                        .Where(type => !string.IsNullOrWhiteSpace(type))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var typeOfPurchaseLabel = distinctTypesOfPurchase.Count switch
                    {
                        0 => string.Empty,
                        1 => distinctTypesOfPurchase[0],
                        _ => "MIXED"
                    };
                    decimal poSubtotal = 0m;
                    decimal unliftedLastMonthSubtotal = 0m;
                    decimal liftedThisMonthSubtotal = 0m;
                    decimal unliftedThisMonthSubtotal = 0m;
                    decimal grossAmountSubtotal = 0m;
                    decimal ewtAmountSubtotal = 0m;
                    foreach (var product in productList)
                    {
                        // declare per product
                        var aGroupByProduct = groupByProduct
                            .FirstOrDefault(g => g.Key.ProductName == product);
                        worksheet.Cells[row, 4].Value = product;
                        worksheet.Cells[row, 5].Value = sameSupplierGroup.Key.Terms;
                        worksheet.Cells[row, 6].Value = typeOfPurchaseLabel;

                        // get the necessary values from po, separate it by variable
                        if (aGroupByProduct != null)
                        {
                            if (aGroupByProduct.Sum(po => po.Quantity) != 0m)
                            {
                                // original po volume
                                decimal allPoTotal = 0m;
                                decimal unliftedLastMonth = 0m;
                                decimal liftedThisMonth = 0m;
                                decimal unliftedThisMonth = 0m;
                                decimal grossOfLiftedThisMonth = 0m;
                                decimal totalEwt = 0m;

                                foreach (var po in aGroupByProduct)
                                {
                                    decimal currentPoQuantity = po.Quantity;
                                    allPoTotal += currentPoQuantity;
                                    var isTaxable = po.TaxType == SD.TaxType_WithTax;
                                    var isVatable = po.VatType == SD.VatType_Vatable;
                                    var rrQtyBeforeSelectedPeriod = po.Date < periodStart
                                        ? po.ReceivingReports!
                                            .Where(rr => rr.Date < periodStart)
                                            .Sum(rr => rr.QuantityReceived)
                                        : 0m;
                                    var liftedThisMonthReports = po.ReceivingReports!
                                        .Where(rr => rr.Date >= periodStart && rr.Date <= periodEnd)
                                        .ToList();
                                    var rrQtyForLiftedThisMonth = liftedThisMonthReports.Sum(rr => rr.QuantityReceived);
                                    var rrQtyThroughMonthEnd = rrQtyBeforeSelectedPeriod + rrQtyForLiftedThisMonth;

                                    if (liftedThisMonthReports.Count != 0)
                                    {
                                        foreach (var rr in liftedThisMonthReports)
                                        {
                                            grossOfLiftedThisMonth += rr.Amount;

                                            var netOfVat = isVatable
                                                ? NetOfVatOrZero(rr.Amount)
                                                : rr.Amount;
                                            var ewt = isTaxable
                                                ? EwtAmountOrZero(netOfVat, rr.TaxPercentage)
                                                : 0m;

                                            totalEwt += ewt;
                                        }
                                    }

                                    unliftedLastMonth += po.Date < periodStart
                                        ? Math.Max(0m, currentPoQuantity - rrQtyBeforeSelectedPeriod)
                                        : 0m;
                                    liftedThisMonth += rrQtyForLiftedThisMonth;
                                    unliftedThisMonth += Math.Max(0m, currentPoQuantity - rrQtyThroughMonthEnd);
                                }

                                if (allPoTotal != 0m)
                                {
                                    poSubtotal += allPoTotal;
                                }

                                // WRITE ORIGINAL PO VOLUME
                                worksheet.Cells[row, 7].Value = allPoTotal;
                                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;

                                // WRITE UNLIFTED LAST MONTH
                                if (unliftedLastMonth != 0m)
                                {
                                    worksheet.Cells[row, 8].Value = unliftedLastMonth;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 8].Value = 0m;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE LIFTED THIS MONTH
                                if (liftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 9].Value = liftedThisMonth;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 9].Value = 0m;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE UNLIFTED THIS MONTH
                                if (unliftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 10].Value = unliftedThisMonth;
                                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 10].Value = 0m;
                                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                grandTotalsByProduct[product].OriginalPo += allPoTotal;
                                grandTotalsByProduct[product].UnliftedLastMonth += unliftedLastMonth;
                                grandTotalsByProduct[product].LiftedThisMonth += liftedThisMonth;
                                grandTotalsByProduct[product].UnliftedThisMonth += unliftedThisMonth;
                                grandTotalsByProduct[product].GrossAmount += grossOfLiftedThisMonth;
                                grandTotalsByProduct[product].EwtAmount += totalEwt;

                                // operations for subtotals
                                unliftedLastMonthSubtotal += unliftedLastMonth;
                                liftedThisMonthSubtotal += liftedThisMonth;
                                unliftedThisMonthSubtotal += unliftedThisMonth;
                                grossAmountSubtotal += grossOfLiftedThisMonth;
                                ewtAmountSubtotal += totalEwt;

                                // write per product: price, gross, ewt, net
                                var price = liftedThisMonth > 0
                                    ? grossOfLiftedThisMonth / liftedThisMonth
                                    : 0m;
                                var priceNetOfVat = NetOfVatOrZero(price);

                                worksheet.Cells[row, 11].Value = priceNetOfVat;
                                worksheet.Cells[row, 12].Value = price;
                                worksheet.Cells[row, 13].Value = grossOfLiftedThisMonth;
                                worksheet.Cells[row, 14].Value = totalEwt;
                                worksheet.Cells[row, 15].Value = RoundToFour(grossOfLiftedThisMonth - totalEwt);
                                using var range = worksheet.Cells[row, 11, row, 15];
                                range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }
                        }

                        row++;
                    }

                    worksheet.Cells[row, 3].Value = "SUB-TOTAL";
                    worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                    worksheet.Cells[row, 7].Value = poSubtotal;
                    worksheet.Cells[row, 8].Value = unliftedLastMonthSubtotal;
                    worksheet.Cells[row, 9].Value = liftedThisMonthSubtotal;
                    worksheet.Cells[row, 10].Value = unliftedThisMonthSubtotal;
                    if (liftedThisMonthSubtotal != 0)
                    {
                        var price = DivideOrZero(grossAmountSubtotal, liftedThisMonthSubtotal);
                        var priceNetOfVat = NetOfVatOrZero(price);
                        worksheet.Cells[row, 11].Value = priceNetOfVat;
                        worksheet.Cells[row, 12].Value = price;
                        worksheet.Cells[row, 13].Value = grossAmountSubtotal;
                        worksheet.Cells[row, 14].Value = ewtAmountSubtotal;
                        worksheet.Cells[row, 15].Value = RoundToFour(grossAmountSubtotal - ewtAmountSubtotal);
                    }
                    else
                    {
                        worksheet.Cells[row, 11].Value = 0m;
                        worksheet.Cells[row, 12].Value = 0m;
                        worksheet.Cells[row, 13].Value = 0m;
                        worksheet.Cells[row, 14].Value = 0m;
                        worksheet.Cells[row, 15].Value = 0m;
                    }

                    using (var range = worksheet.Cells[row, 7, row, 15])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    using (var range = worksheet.Cells[row, 3, row, 15])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Font.Bold = true;
                    }
                }

                row += 2;
                worksheet.Cells[row, 2].Value = "ALL SUPPLIERS";
                worksheet.Cells[row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Value = "FILPRIDE";

                decimal finalPo = grandTotalsByProduct.Values.Sum(metric => metric.OriginalPo);
                decimal finalUnliftedLastMonth = grandTotalsByProduct.Values.Sum(metric => metric.UnliftedLastMonth);
                decimal finalLiftedThisMonth = grandTotalsByProduct.Values.Sum(metric => metric.LiftedThisMonth);
                decimal finalUnliftedThisMonth = grandTotalsByProduct.Values.Sum(metric => metric.UnliftedThisMonth);
                decimal finalGross = grandTotalsByProduct.Values.Sum(metric => metric.GrossAmount);
                decimal finalEwt = grandTotalsByProduct.Values.Sum(metric => metric.EwtAmount);

                foreach (var product in productList)
                {
                    worksheet.Cells[row, 4].Value = product;
                    worksheet.Cells[row, 5].Value = "ALL TERMS";
                    var grandTotalMetric = grandTotalsByProduct[product];
                    worksheet.Cells[row, 7].Value = grandTotalMetric.OriginalPo;
                    worksheet.Cells[row, 8].Value = grandTotalMetric.UnliftedLastMonth;
                    worksheet.Cells[row, 9].Value = grandTotalMetric.LiftedThisMonth;
                    worksheet.Cells[row, 10].Value = grandTotalMetric.UnliftedThisMonth;
                    if (grandTotalMetric.LiftedThisMonth != 0)
                    {
                        worksheet.Cells[row, 11].Value = NetUnitValueOrZero(grandTotalMetric.GrossAmount, grandTotalMetric.LiftedThisMonth);
                        worksheet.Cells[row, 12].Value = DivideOrZero(grandTotalMetric.GrossAmount, grandTotalMetric.LiftedThisMonth);
                    }
                    else
                    {
                        worksheet.Cells[row, 11].Value = 0m;
                        worksheet.Cells[row, 12].Value = 0m;
                    }
                    worksheet.Cells[row, 13].Value = grandTotalMetric.GrossAmount;
                    worksheet.Cells[row, 14].Value = grandTotalMetric.EwtAmount;
                    worksheet.Cells[row, 15].Value = RoundToFour(grandTotalMetric.GrossAmount - grandTotalMetric.EwtAmount);

                    using (var range = worksheet.Cells[row, 6, row, 15])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    row++;
                }

                // final total
                worksheet.Cells[row, 3].Value = "GRAND-TOTAL";
                worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                worksheet.Cells[row, 7].Value = finalPo;
                worksheet.Cells[row, 8].Value = finalUnliftedLastMonth;
                worksheet.Cells[row, 9].Value = finalLiftedThisMonth;
                worksheet.Cells[row, 10].Value = finalUnliftedThisMonth;
                if (finalLiftedThisMonth != 0)
                {
                    worksheet.Cells[row, 11].Value = NetUnitValueOrZero(finalGross, finalLiftedThisMonth);
                    worksheet.Cells[row, 12].Value = DivideOrZero(finalGross, finalLiftedThisMonth);
                    worksheet.Cells[row, 13].Value = finalGross;
                    worksheet.Cells[row, 14].Value = finalEwt;
                    worksheet.Cells[row, 15].Value = RoundToFour(finalGross - finalEwt);
                }
                else
                {
                    worksheet.Cells[row, 11].Value = 0m;
                    worksheet.Cells[row, 12].Value = 0m;
                    worksheet.Cells[row, 12].Value = 0m;
                    worksheet.Cells[row, 14].Value = 0m;
                    worksheet.Cells[row, 15].Value = 0m;
                }

                using (var range = worksheet.Cells[row, 6, row, 15])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                using (var range = worksheet.Cells[row, 3, row, 15])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Font.Bold = true;
                }

                row += 6;
                worksheet.Cells[row, 2].Value = "Prepared by:";
                worksheet.Cells[row, 5].Value = "Approved by:";
                worksheet.Cells[row, 8].Value = "Acknowledged by:";
                worksheet.Cells[row, 11].Value = "Received by:";
                row += 3;
                worksheet.Cells[row, 2].Value = "";
                worksheet.Cells[row, 5].Value = "";
                worksheet.Cells[row, 8].Value = "";
                worksheet.Cells[row, 11].Value = "";
                using (var range = worksheet.Cells[row, 1, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.UnderLine = true;
                }
                row++;
                worksheet.Cells[row, 2].Value = "Pricing Specialist";
                worksheet.Cells[row, 5].Value = "Operations Manager";
                worksheet.Cells[row, 8].Value = "Chief Operating Officer";
                worksheet.Cells[row, 11].Value = "Finance Manager";

                worksheet.Column(10).Style.Numberformat.Format = "#,##0.0000";
                worksheet.Column(11).Style.Numberformat.Format = "#,##0.0000";

                worksheet.Columns.AutoFit();
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 30;

                #endregion == TOPSHEET ==

                #region == BY SUPPLIER ==

                foreach (var aGroupBySupplier in groupBySupplier)
                {
                    var firstRecord = aGroupBySupplier.FirstOrDefault();
                    var isVatable = firstRecord!.VatType == SD.VatType_Vatable;
                    DateOnly monthYearTemp = new DateOnly(monthYear.Year, monthYear.Month, 1);
                    DateOnly followingMonth = monthYearTemp.AddMonths(1);
                    var poGrandTotal = 0m;
                    var unliftedLastMonthGrandTotal = 0m;
                    var liftedThisMonthGrandTotal = 0m;
                    var unliftedThisMonthGrandTotal = 0m;
                    var grossAmountGrandTotal = 0m;
                    var ewtGrandTotal = 0m;

                    worksheet = package.Workbook.Worksheets.Add(aGroupBySupplier.Key.SupplierName);
                    worksheet.Cells.Style.Font.Name = "Calibri";
                    worksheet.Cells[1, 1].Value = $"SUPPLIER: {aGroupBySupplier.Key.SupplierName}";
                    worksheet.Cells[2, 1].Value = "AP MONITORING REPORT (TRADE & SUPPLY GENERATED: PER PO #)";
                    worksheet.Cells[3, 1].Value = "REF: PURCHASE ORDER REPORT-per INTEGRATED BUSINESS SYSTEM";
                    worksheet.Cells[4, 1].Value = $"FOR THE MONTH OF {monthYear.ToString("MMMM yyyy")}";
                    worksheet.Cells[5, 1].Value = $"DUE DATE: {followingMonth.ToString("MMMM yyyy")}";
                    worksheet.Cells[1, 1, 5, 1].Style.Font.Bold = true;
                    row = 8;
                    var groupByProduct = aGroupBySupplier.GroupBy(po => po.Product!.ProductName).ToList();

                    foreach (var product in productList)
                    {
                        var aGroupByProduct = groupByProduct
                            .FirstOrDefault(g => g.FirstOrDefault()!.Product!.ProductName == product);

                        if (aGroupByProduct == null)
                        {
                            continue;
                        }

                        var poSubtotal = 0m;
                        var unliftedLastMonthSubtotal = 0m;
                        var liftedThisMonthSubtotal = 0m;
                        var unliftedThisMonthSubtotal = 0m;
                        var grossAmountSubtotal = 0m;
                        var ewtSubtotal = 0m;

                        worksheet.Cells[row, 1].Value = "PO#";
                        worksheet.Cells[row, 2].Value = "DATE";
                        worksheet.Cells[row, 3].Value = "PRODUCT";
                        worksheet.Cells[row, 4].Value = "PORT";
                        worksheet.Cells[row, 5].Value = "REFERENCE MOPS";
                        worksheet.Cells[row, 6].Value = "PAYMENT TERMS";
                        worksheet.Cells[row, 7].Value = "TYPE OF PURCHASE";
                        worksheet.Cells[row, 8].Value = "ORIGINAL PO VOLUME";
                        worksheet.Cells[row, 9].Value = "UNLIFTED LAST MONTH";
                        worksheet.Cells[row, 10].Value = "LIFTED THIS MONTH";
                        worksheet.Cells[row, 11].Value = "UNLIFTED THIS MONTH";
                        worksheet.Cells[row, 12].Value = "PRICE(VAT-EX)";
                        worksheet.Cells[row, 13].Value = "PRICE(VAT-INC)";
                        worksheet.Cells[row, 14].Value = "GROSS AMOUNT(VAT-INC)";
                        worksheet.Cells[row, 15].Value = "EWT";
                        worksheet.Cells[row, 16].Value = "NET OF EWT";

                        using (var range = worksheet.Cells[row, 1, row, 16])
                        {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 172));
                            range.Style.Font.Bold = true;
                        }

                        worksheet.Row(row).Height = 36;
                        row++;

                        foreach (var po in aGroupByProduct)
                        {
                            // computing the cells variables
                            var poTotal = po.Quantity;
                            var grossAmount = 0m;
                            var unliftedLastMonth = 0m;
                            var liftedThisMonthRrQty = 0m;
                            var unliftedThisMonth = 0m;
                            var isTaxable = po.TaxType == SD.TaxType_WithTax;
                            var totalEwt = 0m;
                            var rrQtyBeforeSelectedPeriod = po.Date < periodStart
                                ? po.ReceivingReports!
                                    .Where(rr => rr.Date < periodStart)
                                    .Sum(rr => rr.QuantityReceived)
                                : 0m;

                            unliftedLastMonth = po.Date < periodStart
                                ? Math.Max(0m, poTotal - rrQtyBeforeSelectedPeriod)
                                : 0m;

                            var liftedThisMonth = po.ReceivingReports!
                                .Where(rr => rr.Date >= periodStart && rr.Date <= periodEnd)
                                .ToList();

                            liftedThisMonthRrQty = liftedThisMonth.Sum(x => x.QuantityReceived);
                            unliftedThisMonth = Math.Max(0m, poTotal - rrQtyBeforeSelectedPeriod - liftedThisMonthRrQty);
                            grossAmount += liftedThisMonth.Sum(x => x.Amount);

                            totalEwt = isTaxable
                                ? liftedThisMonth.Sum(rr =>
                                {
                                    var netOfVat = isVatable
                                        ? NetOfVatOrZero(rr.Amount)
                                        : rr.Amount;

                                    return EwtAmountOrZero(netOfVat, rr.TaxPercentage);
                                })
                                : 0m;

                            var ewt = totalEwt;

                            // incrementing subtotals
                            poSubtotal += poTotal;
                            unliftedLastMonthSubtotal += unliftedLastMonth;
                            liftedThisMonthSubtotal += liftedThisMonthRrQty;
                            unliftedThisMonthSubtotal += unliftedThisMonth;
                            grossAmountSubtotal += grossAmount;
                            ewtSubtotal += ewt;

                            // writing the values to cells
                            worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                            worksheet.Cells[row, 2].Value = po.Date.ToString("MM/dd/yyyy");
                            worksheet.Cells[row, 3].Value = po.Product!.ProductName;
                            worksheet.Cells[row, 4].Value = po.PickUpPoint!.Depot;
                            worksheet.Cells[row, 5].Value = po.TriggerDate != default ? $"TRIGGER {po.TriggerDate.ToString("MM.dd.yyyy")}" : "UNDETERMINED";
                            worksheet.Cells[row, 6].Value = po.Terms;
                            worksheet.Cells[row, 7].Value = po.TypeOfPurchase.ToUpper();
                            worksheet.Cells[row, 8].Value = poTotal;
                            worksheet.Cells[row, 9].Value = unliftedLastMonth;
                            worksheet.Cells[row, 10].Value = liftedThisMonthRrQty;
                            worksheet.Cells[row, 11].Value = unliftedThisMonth;
                            var cost = liftedThisMonthRrQty > 0
                                ? grossAmount / liftedThisMonthRrQty
                                : 0;
                            worksheet.Cells[row, 12].Value = isVatable
                                ? NetOfVatOrZero(cost)
                                : cost;
                            worksheet.Cells[row, 13].Value = cost;
                            worksheet.Cells[row, 14].Value = grossAmount;
                            worksheet.Cells[row, 15].Value = ewt;
                            worksheet.Cells[row, 16].Value = RoundToFour(grossAmount - ewt);

                            using (var range = worksheet.Cells[row, 6, row, 16])
                            {
                                range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }

                            row++;
                        }

                        // incrementing grandtotals
                        poGrandTotal += poSubtotal;
                        unliftedLastMonthGrandTotal += unliftedLastMonthSubtotal;
                        liftedThisMonthGrandTotal += liftedThisMonthSubtotal;
                        unliftedThisMonthGrandTotal += unliftedThisMonthSubtotal;
                        grossAmountGrandTotal += grossAmountSubtotal;
                        ewtGrandTotal += ewtSubtotal;

                        worksheet.Cells[row, 2].Value = "SUB-TOTAL";
                        worksheet.Cells[row, 8].Value = poSubtotal;
                        worksheet.Cells[row, 9].Value = unliftedLastMonthSubtotal;
                        worksheet.Cells[row, 10].Value = liftedThisMonthSubtotal;
                        worksheet.Cells[row, 11].Value = unliftedThisMonthSubtotal;
                        if (liftedThisMonthSubtotal != 0)
                        {
                            var price = DivideOrZero(grossAmountSubtotal, liftedThisMonthSubtotal);
                            var priceNetOfVat = isVatable
                                ? NetOfVatOrZero(price)
                                : price;
                            worksheet.Cells[row, 12].Value = priceNetOfVat;
                            worksheet.Cells[row, 13].Value = price;
                        }
                        worksheet.Cells[row, 14].Value = grossAmountSubtotal;
                        worksheet.Cells[row, 15].Value = ewtSubtotal;
                        worksheet.Cells[row, 16].Value = RoundToFour(grossAmountSubtotal - ewtSubtotal);

                        using (var range = worksheet.Cells[row, 3, row, 5])
                        {
                            range.Merge = true;
                            range.Value = product;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        using (var range = worksheet.Cells[row, 6, row, 16])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        using (var range = worksheet.Cells[row, 1, row, 16])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        }

                        row += 2;
                    }

                    worksheet.Cells[row, 2].Value = "GRAND-TOTAL";
                    worksheet.Cells[row, 8].Value = poGrandTotal;
                    worksheet.Cells[row, 9].Value = unliftedLastMonthGrandTotal;
                    worksheet.Cells[row, 10].Value = liftedThisMonthGrandTotal;
                    worksheet.Cells[row, 11].Value = unliftedThisMonthGrandTotal;
                    if (liftedThisMonthGrandTotal != 0)
                    {
                        var price = DivideOrZero(grossAmountGrandTotal, liftedThisMonthGrandTotal);
                        var priceNetOfVat = isVatable
                            ? NetOfVatOrZero(price)
                            : price;
                        worksheet.Cells[row, 12].Value = priceNetOfVat;
                        worksheet.Cells[row, 13].Value = price;
                    }
                    worksheet.Cells[row, 14].Value = grossAmountGrandTotal;
                    worksheet.Cells[row, 15].Value = ewtGrandTotal;
                    worksheet.Cells[row, 16].Value = RoundToFour(grossAmountGrandTotal - ewtGrandTotal);

                    using (var range = worksheet.Cells[row, 3, row, 5])
                    {
                        range.Merge = true;
                        range.Value = "ALL PRODUCTS";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    using (var range = worksheet.Cells[row, 6, row, 16])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    using (var range = worksheet.Cells[row, 1, row, 16])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }

                    row += 6;
                    worksheet.Cells[row, 1].Value = "Note:   Volume paid is the volume recorded in the Purchase Journal Report.";
                    row += 3;
                    worksheet.Cells[row, 1].Value = "Prepared by:";
                    worksheet.Cells[row, 5].Value = "Approved by:";
                    worksheet.Cells[row, 8].Value = "Acknowledged by:";
                    row += 2;
                    worksheet.Cells[row, 1].Value = "";
                    worksheet.Cells[row, 5].Value = "";
                    worksheet.Cells[row, 8].Value = "";
                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Font.UnderLine = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    row++;
                    worksheet.Cells[row, 1].Value = "Pricing Specialist";
                    worksheet.Cells[row, 5].Value = "Operations Manager";
                    worksheet.Cells[row, 8].Value = "Chief Operating Officer";
                    worksheet.Column(10).Style.Numberformat.Format = "#,##0.0000";
                    worksheet.Column(11).Style.Numberformat.Format = "#,##0.0000";

                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    worksheet.Columns.AutoFit();
                    worksheet.Column(1).Width = 16;
                }

                #endregion == BY SUPPLIER ==

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate accounts payable report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"AP_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate accounts payable report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ApReport));
            }
        }

        #endregion -- Generate Ap Report Excel File --

        [HttpGet]
        public async Task<IActionResult> LiquidationReport(CancellationToken cancellationToken)
        {
            var viewModelBook = new ViewModelBook();

            var distinctSupplierIds = await _dbContext.FilpridePurchaseOrders
                .Select(po => po.SupplierId).Distinct().ToListAsync(cancellationToken);

            var suppliers = await _dbContext.FilprideSuppliers
                .Where(s => distinctSupplierIds.Contains(s.SupplierId))
                .ToListAsync(cancellationToken);

            viewModelBook.SupplierList = suppliers.Select(s => new SelectListItem
            {
                Value = s.SupplierId.ToString(),
                Text = s.SupplierName
            }).ToList();

            return View(viewModelBook);
        }

        #region -- Generate Liquidation Report Excel File --

        [HttpPost]
        public async Task<IActionResult> GenerateLiquidationReportExcelFile(ViewModelBook viewModel, CancellationToken cancellationToken)
        {
            try
            {
                #region == Initializations ==

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                if (viewModel.Period == null)
                {
                    TempData["error"] = "Period/Month cannot be null.";
                    return RedirectToAction(nameof(LiquidationReport));
                }

                if (viewModel.PurchaseOrderId == null)
                {
                    TempData["error"] = "Purchase Report cannot be null.";
                    return RedirectToAction(nameof(LiquidationReport));
                }

                var purchaseOrder = await _unitOfWork.FilpridePurchaseOrder
                    .GetAsync(po => po.PurchaseOrderId == viewModel.PurchaseOrderId, cancellationToken);

                if (purchaseOrder == null)
                {
                    TempData["error"] = "Purchase Order not found.";
                    return RedirectToAction(nameof(LiquidationReport));
                }

                string currencyFormatTwoDecimal = "#,##0.00";
                string currencyFormatFourDecimal = "#,##0.0000";

                var receivingReports = (await _unitOfWork.FilprideReceivingReport
                        .GetAllAsync(rr => rr.POId == viewModel.PurchaseOrderId
                                           && rr.WithdrawalCertificate != null
                                           && rr.Date.Month == viewModel.Period.Value.Month
                                           && rr.Date.Year == viewModel.Period.Value.Year
                                           && rr.Status == nameof(Status.Posted),
                            cancellationToken))
                    .OrderBy(rr => rr.ReceivingReportNo)
                    .ToList();

                if (receivingReports.Count == 0)
                {
                    TempData["error"] = "No Receiving Reports found.";
                    return RedirectToAction(nameof(LiquidationReport));
                }

                #endregion == Initializations ==

                #region == TOPSHEET ==

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("TOPSHEET");
                worksheet.Cells[1, 1].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells.Style.Font.Name = "Calibri";

                // inserting filpride image
                var imgFilprideLogoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "Filpride-logo.png");
                var pic = await worksheet.Drawings.AddPictureAsync("Landscape", new FileInfo(imgFilprideLogoPath));
                pic.SetSize(120, 50);
                pic.SetPosition(2, 0, 2, 0);

                // title area
                using (var range = worksheet.Cells[3, 3, 3, 9])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES, INC.";
                    range.Style.Font.Size = 14;
                    range.Style.Font.Bold = true;
                    range.Style.Font.UnderLine = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                using (var range = worksheet.Cells[4, 3, 4, 9])
                {
                    range.Merge = true;
                    range.Value = "ACTIVITY REPORT";
                    range.Style.Font.Size = 14;
                    range.Style.Font.Bold = true;
                    range.Style.Font.UnderLine = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var row = 17;

                worksheet.Cells[7, 3].Value = "Created Date";
                worksheet.Cells[7, 4].Value = DateTimeHelper.GetCurrentPhilippineTime().ToString("MMM dd, yyyy");
                worksheet.Cells[7, 7].Value = "Attachments:";
                worksheet.Cells[7, 7].Style.Font.Bold = true;
                worksheet.Cells[7, 7].Style.Font.UnderLine = true;
                worksheet.Cells[8, 3].Value = "Time Created";
                worksheet.Cells[8, 4].Value = DateTimeHelper.GetCurrentPhilippineTime().ToString("h:mm:ss tt");
                worksheet.Cells[9, 3].Value = "To:";
                worksheet.Cells[9, 4].Value = "Operations Accounting";
                worksheet.Cells[10, 3].Value = "From: ";
                worksheet.Cells[10, 4].Value = "TNS-Operations";
                worksheet.Cells[11, 3].Value = "CC: ";
                worksheet.Cells[11, 4].Value = "Chief Operation Officer";
                worksheet.Cells[13, 3].Value = "Date Needed: ";
                worksheet.Cells[13, 4].Value = "ASAP";
                worksheet.Cells[14, 3].Value = "Supplier: ";
                worksheet.Cells[14, 4].Value = purchaseOrder!.Supplier!.SupplierName;
                worksheet.Cells[15, 3].Value = "IBS PO #: ";
                worksheet.Cells[15, 4].Value = purchaseOrder!.PurchaseOrderNo;
                worksheet.Cells[16, 3].Value = "PO Date Created: ";
                worksheet.Cells[16, 4].Value = receivingReports.FirstOrDefault()!.PurchaseOrder!.CreatedDate.ToString("MMM dd, yyyy");
                worksheet.Cells[17, 3].Value = "Product: ";
                worksheet.Cells[17, 4].Value = receivingReports.FirstOrDefault()!.PurchaseOrder!.Product!.ProductName;

                if (purchaseOrder.SupplierId == 19)
                {
                    worksheet.Cells[8, 7].Value = "1";
                    worksheet.Cells[8, 8].Value = "Filpride PO";
                    worksheet.Cells[9, 7].Value = "2";
                    worksheet.Cells[9, 8].Value = "PO Liquidation vs UPPI Billing";
                    worksheet.Cells[10, 7].Value = "3";
                    worksheet.Cells[10, 8].Value = "PO Summary from the IBS System";
                    worksheet.Cells[11, 7].Value = "4";
                    worksheet.Cells[11, 8].Value = "WC Distribution Summary";
                    worksheet.Cells[12, 7].Value = "5";
                    worksheet.Cells[12, 8].Value = "Filpride Computation-MOPS Price";
                    worksheet.Cells[13, 7].Value = "6";
                    worksheet.Cells[13, 8].Value = "UPPI Email Confirmation";
                    worksheet.Cells[14, 7].Value = "7";
                    worksheet.Cells[14, 8].Value = "UPPI Price Computation";
                    worksheet.Cells[15, 7].Value = "8";
                    worksheet.Cells[15, 8].Value = "Filpride DR";
                    worksheet.Cells[16, 7].Value = "9";
                    worksheet.Cells[16, 8].Value = "Filpride RR";
                    worksheet.Cells[17, 7].Value = "10";
                    worksheet.Cells[17, 8].Value = "Supplier Docs (SI, DR, WC)";
                }
                else
                {
                    worksheet.Cells[8, 7].Value = "1";
                    worksheet.Cells[8, 8].Value = "Filpride PO";
                    worksheet.Cells[9, 7].Value = "2";
                    worksheet.Cells[9, 8].Value = "PO Summary from the IBS System";
                    worksheet.Cells[10, 7].Value = "3";
                    worksheet.Cells[10, 8].Value = "WC Distribution Summary";
                    worksheet.Cells[11, 7].Value = "4";
                    worksheet.Cells[11, 8].Value = "Filpride DR";
                    worksheet.Cells[12, 7].Value = "5";
                    worksheet.Cells[12, 8].Value = "Filpride RR";
                    worksheet.Cells[13, 7].Value = "6";
                    worksheet.Cells[13, 8].Value = "Supplier Docs (SI, DR, WC)";
                }

                worksheet.Cells[19, 3].Value = "Payment Terms: ";
                worksheet.Cells[19, 4].Value = purchaseOrder.Terms;
                worksheet.Cells[20, 3].Value = "Due Date: ";

                var dueDate = await _unitOfWork.FilpridePurchaseOrder.ComputeDueDateAsync(purchaseOrder.Terms, purchaseOrder.Date, cancellationToken);

                worksheet.Cells[20, 4].Value = dueDate.ToString("MMM dd, yyyy");

                using (var range = worksheet.Cells[7, 4, 20, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.UnderLine = true;
                }

                using (var range = worksheet.Cells[8, 7, 17, 8])
                {
                    range.Style.Font.Bold = true;
                }

                using (var range = worksheet.Cells[8, 7, 17, 7])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                worksheet.Cells[22, 3].Value = "Subject: ";
                worksheet.Cells[22, 4].Value = "Requesting to process the payment on or before the due date as stated above.";
                worksheet.Cells[22, 4].Style.Font.Bold = true;

                using (var range = worksheet.Cells[22, 4, 22, 9])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Cells[25, 3].Value = "Summary: ";
                worksheet.Cells[25, 3].Style.Font.Bold = true;
                worksheet.Cells[27, 3].Value = "Classifications";
                worksheet.Cells[27, 4].Value = "AP to Supplier";
                worksheet.Cells[27, 5].Value = "AP to Hauler";
                worksheet.Cells[27, 6].Value = "Total AP";

                using (var range = worksheet.Cells[27, 3, 27, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                var sumOfFreightAmountWithFreight = receivingReports.Where(rr => rr.DeliveryReceipt!.Freight > 0)
                    .Sum(rr => rr.DeliveryReceipt!.Freight * rr.QuantityReceived);

                var sumOfQuantityWithFreight = receivingReports.Where(rr => rr.DeliveryReceipt!.Freight > 0)
                    .Sum(rr => rr.QuantityReceived);

                var sumOfQuantity = receivingReports.Sum(rr => rr.QuantityReceived);
                var sumOfAmount = receivingReports.Sum(rr => rr.Amount);
                var averageCostPerLiter = DivideOrZero(sumOfAmount, sumOfQuantity);

                var sumOfFreightAmount = receivingReports.Sum(rr => rr.DeliveryReceipt!.Freight * rr.QuantityReceived);
                var averageFreightPerLiterWithFreight = DivideOrZero(sumOfFreightAmountWithFreight, sumOfQuantityWithFreight);
                var averageFreightPerLiter = DivideOrZero(sumOfFreightAmount, sumOfQuantity);

                var sumOfAmountBasedOnSoa = (receivingReports.Sum(rr => rr.CostBasedOnSoa * rr.QuantityReceived));
                var averageCostBasedOnSoa = DivideOrZero(sumOfAmountBasedOnSoa, sumOfQuantity);

                worksheet.Cells[28, 3].Value = "Volume Lifted: ";
                worksheet.Cells[28, 4].Value = sumOfQuantity;
                worksheet.Cells[28, 5].Value = sumOfQuantityWithFreight;
                worksheet.Cells[28, 6].Value = sumOfQuantity;
                worksheet.Cells[29, 3].Value = "Cost/ltr: ";
                worksheet.Cells[29, 4].Value = averageCostPerLiter;
                worksheet.Cells[29, 5].Value = averageFreightPerLiterWithFreight;
                worksheet.Cells[30, 3].Value = "Total Amount";
                worksheet.Cells[30, 4].Value = sumOfAmount;
                worksheet.Cells[30, 5].Value = sumOfFreightAmount;

                using (var range = worksheet.Cells[30, 4, 30, 6])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Font.Bold = true;
                }

                row = 32;

                worksheet.Cells[32, 3].Value = "Form Check:";
                worksheet.Cells[32, 4].Value = sumOfAmount;
                worksheet.Cells[30, 5].Value = sumOfFreightAmount;
                worksheet.Cells[32, 6].Value = RoundToFour(sumOfAmount + sumOfFreightAmount);

                using (var range = worksheet.Cells[row, 4, row, 6])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Cells[34, 3].Value = "Variance";
                worksheet.Cells[34, 4].Value = 0; //zero for now
                worksheet.Cells[34, 5].Value = 0; //zero for now
                worksheet.Cells[34, 6].Value = 0; //zero for now

                using (var range = worksheet.Cells[34, 4, 34, 6])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                var groupedByHauler = receivingReports
                    .GroupBy(rr => rr.DeliveryReceipt!.HaulerName)
                    .OrderBy(rr => rr.Key);

                var col = 4;
                var totalFreightAmount = 0m;

                foreach (var rrByHauler in groupedByHauler)
                {
                    worksheet.Cells[36, col].Value = rrByHauler.Key;
                    worksheet.Cells[36, col].Style.Font.Bold = true;
                    worksheet.Cells[36, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    col++;
                }

                worksheet.Cells[36, col].Value = "Total";
                worksheet.Cells[36, col].Style.Font.Bold = true;
                worksheet.Cells[36, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[36, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                col = 4;

                worksheet.Cells[37, 3].Value = "Volume Lifted: ";

                foreach (var rrByHauler in groupedByHauler)
                {
                    worksheet.Cells[37, col].Value = rrByHauler.Where(rr => rr.DeliveryReceipt!.Freight > 0).Sum(rr => rr.QuantityReceived);
                    worksheet.Cells[37, col].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    col++;
                }

                worksheet.Cells[37, col].Value = sumOfQuantityWithFreight;
                worksheet.Cells[37, col].Style.Numberformat.Format = currencyFormatTwoDecimal;

                col = 4;

                worksheet.Cells[38, 3].Value = "Cost/ltr: ";

                foreach (var rrByHauler in groupedByHauler)
                {
                    var freightAmountPerSupplier = rrByHauler.Where(rr => rr.DeliveryReceipt!.Freight > 0)
                        .Sum(rr => rr.DeliveryReceipt!.FreightAmount);

                    var freightQuantityPerSupplier = rrByHauler.Where(rr => rr.DeliveryReceipt!.Freight > 0)
                        .Sum(rr => rr.QuantityReceived);

                    worksheet.Cells[38, col].Value = freightQuantityPerSupplier > 0
                        ? freightAmountPerSupplier / freightQuantityPerSupplier
                        : 0;
                    worksheet.Cells[38, col].Style.Numberformat.Format = currencyFormatFourDecimal;
                    col++;
                }

                col = 4;

                worksheet.Cells[39, 3].Value = "Total Amount: ";

                foreach (var rrByHauler in groupedByHauler)
                {
                    worksheet.Cells[39, col].Value = rrByHauler
                        .Where(rr => rr.DeliveryReceipt!.Freight > 0)
                        .Sum(rr => rr.DeliveryReceipt!.FreightAmount);
                    worksheet.Cells[39, col].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    col++;

                    totalFreightAmount += rrByHauler.Sum(rr => rr.DeliveryReceipt!.FreightAmount); // get the total of all freight
                }

                worksheet.Cells[39, col].Value = totalFreightAmount; // total of freight
                worksheet.Cells[39, col].Style.Numberformat.Format = currencyFormatTwoDecimal;

                // use if-else to determine value of freight if multiple or single hauler
                worksheet.Cells[38, col].Value = DivideOrZero(totalFreightAmount, sumOfQuantityWithFreight); // new cost/ltr of freight
                worksheet.Cells[38, col].Style.Numberformat.Format = currencyFormatFourDecimal;
                worksheet.Cells[29, 6].Value = RoundToFour(averageFreightPerLiterWithFreight + averageCostPerLiter); // cost/ltr + freight/ltr if multiple hauler
                worksheet.Cells[30, 6].Value = RoundToFour(sumOfAmount + sumOfFreightAmountWithFreight); // total amount with total freight amount

                worksheet.Cells[32, 4].Value = sumOfAmount;
                worksheet.Cells[32, 5].Value = sumOfFreightAmount;
                worksheet.Cells[32, 6].Value = RoundToFour(sumOfAmount + sumOfFreightAmountWithFreight); // total amount with freight amount

                worksheet.Cells[39 - 1, col].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[29, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                worksheet.Cells[30, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;

                using (var range = worksheet.Cells[28, 4, 34, 6])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                using (var range = worksheet.Cells[29, 4, 29, 6])
                {
                    range.Style.Numberformat.Format = currencyFormatFourDecimal;
                }

                using (var range = worksheet.Cells[38, 4, 38, col])
                {
                    range.Style.Numberformat.Format = currencyFormatFourDecimal;
                }

                using (var range = worksheet.Cells[28, 3, 39, 3])
                {
                    range.Style.Font.Bold = true;
                }

                worksheet.Cells[43, 3].Value = "Prepared by: ";
                worksheet.Cells[43, 6].Value = "Approved by: ";
                worksheet.Cells[43, 9].Value = "Received by: ";
                worksheet.Cells[45, 3].Value = "TNS Staff";
                worksheet.Cells[45, 3].Style.Font.Bold = true;
                worksheet.Cells[45, 3].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[45, 6].Value = "Operations Manager: ";
                worksheet.Cells[45, 6].Style.Font.Bold = true;
                worksheet.Cells[45, 6].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                worksheet.Cells[45, 9].Value = "Accounting Staff: ";
                worksheet.Cells[45, 9].Style.Font.Bold = true;
                worksheet.Cells[45, 9].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                worksheet.Column(1).Width = 8.5;
                worksheet.Column(2).Width = 4.5;
                worksheet.Column(3).Width = 17.2;
                worksheet.Column(4).Width = 18.3;
                worksheet.Column(5).Width = 14;
                worksheet.Column(6).Width = 14;
                worksheet.Column(7).Width = 13;
                worksheet.Column(8).Width = 13.5;

                #endregion == TOPSHEET ==

                #region == ANNEX A-2 ==

                worksheet = package.Workbook.Worksheets.Add("ANNEX A-2");

                worksheet.Cells[3, 3].Value = "FILPRIDE RESOURCES, INC.";
                worksheet.Cells[3, 16].Value = "ANNEX A-2";
                worksheet.Cells[3, 16].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[4, 3].Value = "PO Liquidation Vs Supplier's Billing";
                worksheet.Cells[4, 3].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[5, 3].Value = "Purchases to Supplier";
                worksheet.Cells[6, 3].Value = "Month:";
                worksheet.Cells[6, 4].Value = viewModel.Period?.ToString("MMM yyyy");
                worksheet.Cells[8, 3].Value = "Breakdown of purchases";

                using (var range = worksheet.Cells[10, 8, 10, 10])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RECORD BASED ON SYSTEM ";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 192, 0));
                }
                using (var range = worksheet.Cells[10, 11, 10, 13])
                {
                    range.Merge = true;
                    range.Value = "PER SUPPLIER'S INVOICE";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 0));
                }
                using (var range = worksheet.Cells[10, 14, 10, 16])
                {
                    range.Merge = true;
                    range.Value = "VARIANCE";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(146, 208, 80));
                }

                using (var range = worksheet.Cells[10, 8, 10, 16])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                using (var range = worksheet.Cells[3, 3, 11, 17])
                {
                    range.Style.Font.Bold = true;
                }

                row = 11;
                col = 3;

                var arrayOfColumnNames = new[]
                {
                    "Lifting Date", "IBS PO #", "RR Number", "DR Number", "Product", "Qty", "Cost/ltr", "Cost Amount",
                    "Qty", "Cost/ltr", "Cost Amount", "Qty", "Cost/ltr", "Cost Amount", "Remarks"
                };

                foreach (var columnName in arrayOfColumnNames)
                {
                    worksheet.Cells[row, col].Value = columnName;
                    worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells[row, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    col++;
                }

                row++;

                foreach (var rr in receivingReports)
                {
                    var costPerLiter = DivideOrZero(rr.Amount, rr.QuantityReceived);
                    var amountBasedOnSoa = (rr.CostBasedOnSoa * rr.QuantityReceived);

                    worksheet.Cells[row, 3].Value = rr.Date;
                    worksheet.Cells[row, 4].Value = rr.PurchaseOrder!.PurchaseOrderNo;
                    worksheet.Cells[row, 5].Value = rr.ReceivingReportNo;
                    worksheet.Cells[row, 6].Value = rr.DeliveryReceipt!.DeliveryReceiptNo;
                    worksheet.Cells[row, 7].Value = rr.PurchaseOrder!.ProductName;
                    worksheet.Cells[row, 8].Value = rr.QuantityReceived;
                    worksheet.Cells[row, 9].Value = costPerLiter;
                    worksheet.Cells[row, 10].Value = rr.Amount;
                    worksheet.Cells[row, 11].Value = rr.QuantityReceived;
                    worksheet.Cells[row, 12].Value = rr.CostBasedOnSoa;
                    worksheet.Cells[row, 13].Value = amountBasedOnSoa;
                    worksheet.Cells[row, 14].Value = 0;
                    worksheet.Cells[row, 15].Value = RoundToFour(costPerLiter - rr.CostBasedOnSoa);
                    worksheet.Cells[row, 16].Value = RoundToFour(rr.Amount - amountBasedOnSoa);

                    worksheet.Cells[row, 3].Style.Numberformat.Format = "MM/dd/yyyy";

                    using (var range = worksheet.Cells[row, 8, row, 16])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatFourDecimal;
                    worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormatFourDecimal;
                    worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatFourDecimal;

                    row++;
                }

                worksheet.Cells[row, 7].Value = "Total";
                worksheet.Cells[row, 8].Value = sumOfQuantity;
                worksheet.Cells[row, 9].Value = averageCostPerLiter;
                worksheet.Cells[row, 10].Value = sumOfAmount;
                worksheet.Cells[row, 11].Value = sumOfQuantity;
                worksheet.Cells[row, 12].Value = averageCostBasedOnSoa;
                worksheet.Cells[row, 13].Value = sumOfAmountBasedOnSoa;
                worksheet.Cells[row, 14].Value = 0;
                worksheet.Cells[row, 15].Value = RoundToFour(averageCostPerLiter - averageCostBasedOnSoa);
                worksheet.Cells[row, 16].Value = RoundToFour(sumOfAmount - sumOfAmountBasedOnSoa);

                using (var range = worksheet.Cells[row, 7, row, 16])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[row, 8, row, 16])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatFourDecimal;
                worksheet.Cells[row, 12].Style.Numberformat.Format = currencyFormatFourDecimal;
                worksheet.Cells[row, 15].Style.Numberformat.Format = currencyFormatFourDecimal;

                worksheet.Column(1).Width = 3;
                worksheet.Column(2).Width = 3;
                worksheet.Column(3).Width = 13;

                for (int i = 4; i != 17; i++)
                {
                    worksheet.Column(i).AutoFit(); // max 18 min 9
                    if (worksheet.Column(i).Width < 15)
                    {
                        worksheet.Column(i).Width = 15;
                    }
                    if (worksheet.Column(i).Width > 20)
                    {
                        worksheet.Column(i).Width = 20;
                    }
                }

                #endregion == ANNEX A-2 ==

                #region == ANNEX A-3 ==

                worksheet = package.Workbook.Worksheets.Add("ANNEX A-3");

                worksheet.Cells[3, 3].Value = "FILPRIDE RESOURCES, INC.";
                worksheet.Cells[3, 16].Value = "ANNEX A-3";
                worksheet.Cells[3, 16].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[4, 3].Value = "PO Summary";
                worksheet.Cells[4, 3].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[5, 3].Value = "Purchases to Supplier";
                worksheet.Cells[6, 3].Value = "Month:";
                worksheet.Cells[6, 4].Value = viewModel.Period?.ToString("MMM yyyy");
                worksheet.Cells[8, 3].Value = "Breakdown of purchases";

                using (var range = worksheet.Cells[10, 3, 10, 19])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                using (var range = worksheet.Cells[3, 3, 10, 19])
                {
                    range.Style.Font.Bold = true;
                }

                row = 10;
                col = 3;

                arrayOfColumnNames = new[]
                {
                    "Lifting Date", "IBS PO #", "FRI RR Number", "FRI DR Number", "Supplier's DR", "Supplier's Invoice",
                    "Supplier's WC", "Client Name", "Hauler's Name", "Product", "Qty", "Cost/ltr", "Cost Amount",
                    "Freight/ltr", "Freight Amount", "P/ltr", "Total AP/Amount"
                };

                foreach (var columnName in arrayOfColumnNames)
                {
                    worksheet.Cells[row, col].Value = columnName;
                    worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells[row, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    col++;
                }

                row++;

                foreach (var rr in receivingReports)
                {
                    var costPerLiter = DivideOrZero(rr.Amount, rr.QuantityReceived);
                    var amountWithFreight = (rr.Amount + rr.DeliveryReceipt!.FreightAmount);

                    worksheet.Cells[row, 3].Value = rr.Date;
                    worksheet.Cells[row, 4].Value = rr.PurchaseOrder!.PurchaseOrderNo;
                    worksheet.Cells[row, 5].Value = rr.ReceivingReportNo;
                    worksheet.Cells[row, 6].Value = rr.DeliveryReceipt!.DeliveryReceiptNo;
                    worksheet.Cells[row, 7].Value = rr.SupplierDrNo;
                    worksheet.Cells[row, 8].Value = rr.SupplierInvoiceNumber;
                    worksheet.Cells[row, 9].Value = rr.WithdrawalCertificate;
                    worksheet.Cells[row, 10].Value = rr.DeliveryReceipt!.Customer!.CustomerName;
                    worksheet.Cells[row, 11].Value = rr.DeliveryReceipt.HaulerName;
                    worksheet.Cells[row, 12].Value = rr.PurchaseOrder.ProductName;
                    worksheet.Cells[row, 13].Value = rr.QuantityReceived;
                    worksheet.Cells[row, 14].Value = costPerLiter;
                    worksheet.Cells[row, 15].Value = rr.Amount;
                    worksheet.Cells[row, 16].Value = rr.DeliveryReceipt.Freight;
                    worksheet.Cells[row, 17].Value = rr.DeliveryReceipt.FreightAmount;
                    worksheet.Cells[row, 18].Value = RoundToFour(costPerLiter + rr.DeliveryReceipt.Freight);
                    worksheet.Cells[row, 19].Value = amountWithFreight;

                    worksheet.Cells[row, 3].Style.Numberformat.Format = "MM/dd/yyyy";

                    using (var range = worksheet.Cells[row, 13, row, 19])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatFourDecimal;
                    worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatFourDecimal;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatFourDecimal;

                    row++;
                }

                worksheet.Cells[row, 12].Value = "Total";
                worksheet.Cells[row, 12].Style.Font.Bold = true;
                worksheet.Cells[row, 13].Value = sumOfQuantity;
                worksheet.Cells[row, 14].Value = averageCostPerLiter;
                worksheet.Cells[row, 15].Value = sumOfAmount;
                worksheet.Cells[row, 16].Value = averageFreightPerLiter;
                worksheet.Cells[row, 17].Value = sumOfFreightAmount;
                worksheet.Cells[row, 18].Value = RoundToFour(averageCostPerLiter + averageFreightPerLiter);
                worksheet.Cells[row, 19].Value = RoundToFour(receivingReports.Sum(rr => rr.Amount + rr.DeliveryReceipt!.FreightAmount));

                using (var range = worksheet.Cells[row, 13, row, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[row, 13, row, 19])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                worksheet.Cells[row, 14].Style.Numberformat.Format = currencyFormatFourDecimal;
                worksheet.Cells[row, 16].Style.Numberformat.Format = currencyFormatFourDecimal;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormatFourDecimal;

                worksheet.Column(1).Width = 3;
                worksheet.Column(2).Width = 3;
                worksheet.Column(3).Width = 13;

                for (int i = 4; i != 20; i++)
                {
                    worksheet.Column(i).AutoFit(); // max 18 min 9
                    if (worksheet.Column(i).Width < 15)
                    {
                        worksheet.Column(i).Width = 15;
                    }
                    if (worksheet.Column(i).Width > 20)
                    {
                        worksheet.Column(i).Width = 20;
                    }
                }

                #endregion == ANNEX A-3 ==

                #region == ANNEX A-4 ==

                worksheet = package.Workbook.Worksheets.Add("ANNEX A-4");

                worksheet.Cells[3, 3].Value = "FILPRIDE RESOURCES, INC.";
                worksheet.Cells[3, 16].Value = "ANNEX A-4";
                worksheet.Cells[3, 16].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[4, 3].Value = "Withdrawal Certificate (WC) Distribution Summary";
                worksheet.Cells[4, 3].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[5, 3].Value = "Purchases to Supplier";
                worksheet.Cells[6, 3].Value = "Month:";
                worksheet.Cells[6, 4].Value = viewModel.Period?.ToString("MMM yyyy");
                worksheet.Cells[8, 3].Value = "Breakdown of purchases";

                using (var range = worksheet.Cells[11, 3, 11, 10])
                {
                    range.Merge = true;
                    range.Value = "PO & RR LIQUIDATION";
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 0));
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                using (var range = worksheet.Cells[10, 8, 10, 16])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                using (var range = worksheet.Cells[3, 3, 8, 16])
                {
                    range.Style.Font.Bold = true;
                }

                row = 12;
                col = 3;

                arrayOfColumnNames = new[]
                {
                    "Lifting Date", "IBS PO #", "FRI RR Number", "FRI DR Number", "FRI ATL Number", "Supplier's ATL Number",
                    "Delivered to", "Product", "DR Volume", "WC Number", "WC Volume"
                };

                foreach (var columnName in arrayOfColumnNames)
                {
                    worksheet.Cells[row, col].Style.Font.Bold = true;
                    worksheet.Cells[row, col].Value = columnName;
                    worksheet.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    worksheet.Cells[row, col].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    col++;
                }

                using (var range = worksheet.Cells[12, (col - 2), 12, (col - 1)])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 224, 180));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var wcTotal = 0m;
                int mostNumberOfCoLoads = 0;
                var listOfCoLoadTotal = new List<decimal>();
                var receivingReportDeliveryReceiptIds = receivingReports
                    .Where(rr => rr.DeliveryReceiptId.HasValue)
                    .Select(rr => rr.DeliveryReceiptId!.Value)
                    .Distinct()
                    .ToList();
                var receivingReportPoIds = receivingReports
                    .Select(rr => rr.POId)
                    .Distinct()
                    .ToList();
                var receivingReportAtlNos = receivingReports
                    .Select(rr => rr.AuthorityToLoadNo)
                    .Where(rr => !string.IsNullOrWhiteSpace(rr))
                    .Distinct()
                    .ToList();
                var supplierAtlEntries = await _dbContext.FilprideDeliveryReceiptDetails
                    .Where(d => receivingReportDeliveryReceiptIds.Contains(d.DeliveryReceiptId)
                                && receivingReportPoIds.Contains(d.PurchaseOrderId)
                                && receivingReportAtlNos.Contains(d.AuthorityToLoadNo!))
                    .Join(_dbContext.FilprideBookAtlDetails,
                        drDetail => new
                        {
                            drDetail.AuthorityToLoadId,
                            drDetail.CustomerOrderSlipId,
                            drDetail.PurchaseOrderId
                        },
                        atlDetail => new
                        {
                            atlDetail.AuthorityToLoadId,
                            atlDetail.CustomerOrderSlipId,
                            PurchaseOrderId = atlDetail.AppointedSupplier!.PurchaseOrderId
                        },
                        (drDetail, atlDetail) => new
                        {
                            drDetail.DeliveryReceiptId,
                            drDetail.PurchaseOrderId,
                            drDetail.AuthorityToLoadNo,
                            atlDetail.SupplierAtlNo
                        })
                    .ToListAsync(cancellationToken);

                foreach (var rr in receivingReports)
                {
                    var rrWithSameWC = (await _unitOfWork.FilprideReceivingReport
                        .GetAllAsync(wcs => wcs.WithdrawalCertificate == rr.WithdrawalCertificate && wcs.ReceivingReportId != rr.ReceivingReportId, cancellationToken))
                        .ToList();

                    if (mostNumberOfCoLoads < rrWithSameWC.Count)
                    {
                        mostNumberOfCoLoads = rrWithSameWC.Count;
                    }

                    for (int i = 0; i < rrWithSameWC.Count; i++)
                    {
                        if (listOfCoLoadTotal.Count < (i + 1))
                        {
                            listOfCoLoadTotal.Add(0m);
                        }

                        if (rrWithSameWC[i].ReceivingReportId == rr.ReceivingReportId)
                        {
                            continue;
                        }

                        listOfCoLoadTotal[i] += rrWithSameWC[i].QuantityReceived;
                    }

                    worksheet.Cells[row, 3].Value = rr.Date;
                    worksheet.Cells[row, 4].Value = rr.PurchaseOrder!.PurchaseOrderNo;
                    worksheet.Cells[row, 5].Value = rr.ReceivingReportNo;
                    worksheet.Cells[row, 6].Value = rr.DeliveryReceipt!.DeliveryReceiptNo;
                    worksheet.Cells[row, 7].Value = rr.AuthorityToLoadNo;
                    var supplierAtlNo = rr.DeliveryReceiptId.HasValue && !string.IsNullOrWhiteSpace(rr.AuthorityToLoadNo)
                        ? supplierAtlEntries
                            .Where(x => x.DeliveryReceiptId == rr.DeliveryReceiptId.Value
                                        && x.PurchaseOrderId == rr.POId
                                        && x.AuthorityToLoadNo == rr.AuthorityToLoadNo)
                            .Select(x => x.SupplierAtlNo)
                            .FirstOrDefault()
                        : null;
                    if (!string.IsNullOrWhiteSpace(supplierAtlNo))
                    {
                        worksheet.Cells[row, 8].Value = supplierAtlNo;
                    }
                    worksheet.Cells[row, 9].Value = rr.DeliveryReceipt!.Customer!.CustomerName;
                    worksheet.Cells[row, 10].Value = rr.PurchaseOrder!.ProductName;
                    worksheet.Cells[row, 11].Value = rr.QuantityReceived;
                    worksheet.Cells[row, 12].Value = rr.WithdrawalCertificate;
                    worksheet.Cells[row, 13].Value = RoundToFour(rrWithSameWC.Sum(wcs => wcs.QuantityReceived) + rr.QuantityReceived); // added the current rr's quantity because it is excluded in rrWithSameWC

                    worksheet.Cells[row, 3].Style.Numberformat.Format = "MM/dd/yyyy";
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    worksheet.Cells[row, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;

                    wcTotal += RoundToFour(rrWithSameWC.Sum(wcs => wcs.QuantityReceived) + rr.QuantityReceived); // added the current rr's quantity because it is excluded in rrWithSameWC
                    col = 14;
                    int ctr = 1;

                    foreach (var coload in rrWithSameWC)
                    {
                        if (coload.ReceivingReportId == rr.ReceivingReportId)
                        {
                            continue;
                        }

                        var currentLetter = ((char)('a' + ctr - 1)).ToString().ToUpper();

                        using (var range = worksheet.Cells[11, col, 11, (col + 2)])
                        {
                            range.Merge = true;
                            range.Value = $"CO-LOAD/SHARED WITH ({currentLetter})";
                            range.Style.Font.Bold = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 0));
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }

                        worksheet.Cells[12, col].Value = "IBS PO #";
                        worksheet.Cells[12, col].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[12, col].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[12, (col + 1)].Value = "FRI DR Number";
                        worksheet.Cells[12, (col + 1)].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[12, (col + 1)].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[12, (col + 2)].Value = "DR Volume";
                        worksheet.Cells[12, (col + 2)].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        worksheet.Cells[12, (col + 2)].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        using (var range = worksheet.Cells[12, col, 12, (col + 2)])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 224, 180));
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        }

                        worksheet.Cells[row, col].Value = coload.PurchaseOrder!.PurchaseOrderNo;
                        worksheet.Cells[row, (col + 1)].Value = coload.DeliveryReceipt!.DeliveryReceiptNo;
                        worksheet.Cells[row, (col + 2)].Value = coload.QuantityReceived;
                        worksheet.Cells[row, (col + 2)].Style.Numberformat.Format = currencyFormatTwoDecimal;

                        ctr++;
                        col += 3;
                    }

                    row++;
                }

                worksheet.Cells[row, 10].Value = "Total";
                worksheet.Cells[row, 11].Value = sumOfQuantity;
                worksheet.Cells[row, 13].Value = wcTotal;

                col = 16;

                foreach (var total in listOfCoLoadTotal)
                {
                    worksheet.Cells[row, col].Value = total;
                    col += 3;
                }

                col -= 3;

                using (var range = worksheet.Cells[row, 10, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[row, 9, row, col])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                worksheet.Column(1).Width = 3;
                worksheet.Column(2).Width = 3;
                worksheet.Column(3).Width = 13;

                for (int i = 4; i <= col; i++)
                {
                    worksheet.Column(i).AutoFit(); // max 18 min 9
                    if (worksheet.Column(i).Width < 15)
                    {
                        worksheet.Column(i).Width = 15;
                    }
                    if (worksheet.Column(i).Width > 20)
                    {
                        worksheet.Column(i).Width = 20;
                    }
                }

                using (var range = worksheet.Cells[10, 11, 10, (col)])
                {
                    range.Merge = true;
                    range.Value = "WC SUMMARY AND MATCHING";
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(198, 224, 180));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                #endregion == ANNEX A-4 ==

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate liquidation report excel file", "Liquidation Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"{purchaseOrder.Supplier!.SupplierName}_{purchaseOrder.PurchaseOrderNo}_{viewModel.Period!.Value.ToString("MMMM_yyyy")}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate liquidation report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(LiquidationReport));
            }
        }

        #endregion -- Generate Liquidation Report Excel File --

        [HttpGet]
        public IActionResult PurchaseJournalReport()
        {
            return View();
        }

        #region -- Generate Purchase Journal Report Excel File --

        [HttpPost]
        public async Task<IActionResult> GeneratePurchaseJournalReportExcelFile(ViewModelBook viewModel, CancellationToken cancellationToken)
        {
            try
            {
                #region == Initializations ==

                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                if (viewModel.Period == null)
                {
                    TempData["error"] = "Period/Month cannot be null.";
                    return RedirectToAction(nameof(LiquidationReport));
                }

                string currencyFormatTwoDecimal = "#,##0.00_);(#,##0.00)";
                string currencyFormatFourDecimal = "#,##0.0000_);(#,##0.0000)";
                var basePeriod = new DateOnly(viewModel.Period.Value.Year, viewModel.Period.Value.Month, 1);
                var nextMonth = basePeriod.AddMonths(1);

                var breakdownColumnNames = new[]
                {
                    "Lifting Date",
                    "Delivery Date",
                    "Segment",
                    "Supplier Name",
                    "PO Number",
                    "RR Number",
                    "DR Number",
                    "Client Name",
                    "Product",
                    "Quantity Served",
                    "Sales Amount(Vat Inc)",
                    "Sales Amount(Vat Ex)",
                    "Sales/ltr (Vat Ex)",
                    "Cost Amount (Vat Inc)",
                    "Cost Amount (Vat Ex)",
                    "Cost/ltr (Vat Ex)",
                    "Freight Amount (Vat Inc)",
                    "Freight Amount (Vat Ex)",
                    "Freight/ltr (Vat Ex)",
                    "Commission Amount",
                    "Commission/ltr",
                    "GM Amount",
                    "GM/ltr",
                };

                var receivingReportsThisMonth = (await _unitOfWork.FilprideReceivingReport
                        .GetAllAsync(rr =>
                                rr.Status == "Posted" &&
                                rr.Date.Month == viewModel.Period.Value.Month &&
                                rr.Date.Year == viewModel.Period.Value.Year,
                            cancellationToken))
                    .OrderBy(rr => rr.Date)
                    .ToList();

                if (receivingReportsThisMonth.Count == 0)
                {
                    TempData["error"] = "No Receiving Reports found.";
                    return RedirectToAction(nameof(PurchaseJournalReport));
                }

                var rrsByProduct = receivingReportsThisMonth.OrderBy(rr => rr.PurchaseOrder!.ProductName)
                    .GroupBy(rr => rr.PurchaseOrder!.ProductName)
                    .ToList();
                var listOfProducts = rrsByProduct.Select(rr => rr.Key).ToList();

                var receivingReportsLastMonth = (await _unitOfWork.FilprideReceivingReport
                        .GetAllAsync(rr =>
                                rr.Status == "Posted" &&
                                rr.Date < basePeriod,
                            cancellationToken))
                    .OrderBy(rr => rr.Date)
                    .ToList();

                var inTransitPrevToThisMonth = receivingReportsLastMonth
                    .Where(rr =>
                        rr.DeliveryReceipt!.DeliveredDate == null
                        || (rr.DeliveryReceipt!.DeliveredDate.Value.Month == viewModel.Period.Value.Month
                            && rr.DeliveryReceipt!.DeliveredDate.Value.Year == viewModel.Period.Value.Year))
                    .OrderBy(rr => rr.Date)
                    .ToList();

                var inTransitNowToNextMonth = receivingReportsThisMonth
                    .Where(rr =>
                        rr.DeliveryReceipt!.DeliveredDate == null
                        || rr.DeliveryReceipt!.DeliveredDate >= nextMonth)
                    .OrderBy(rr => rr.Date)
                    .ToList();

                var rrWithIOCForAccountOfMMSI = receivingReportsThisMonth
                    .Where(rr =>
                        rr.PurchaseOrder!.SupplierId == 182)
                    .OrderBy(rr => rr.Date)
                    .ToList();

                #endregion == Initializations ==

                #region == Contents ==

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("PURCHASE JOURNAL REPORT");

                #region == Main Header ==

                // values
                worksheet.Cells[3, 2].Value = "Company Name:";
                worksheet.Cells[3, 3].Value = "Filpride Resources, Inc.";
                worksheet.Cells[4, 2].Value = "Department:";
                worksheet.Cells[4, 3].Value = "Operations-TNS";
                worksheet.Cells[5, 2].Value = "Subject:";
                worksheet.Cells[5, 3].Value = "Purchase Journal Report";
                worksheet.Cells[6, 2].Value = "Period Covered:";
                worksheet.Cells[6, 3].Value = $"{viewModel.Period.Value.ToString("MMMM yyyy")}";
                worksheet.Cells[7, 2].Value = "Date and Time Generated:";
                worksheet.Cells[7, 3].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";
                // styling
                using (var range = worksheet.Cells[3, 2, 7, 3])
                {
                    range.Style.Font.Bold = true;
                }

                #endregion == Main Header ==

                #region == Section A: Summary Per Segment ==

                worksheet.Cells[9, 2].Value = "A. Summary Per Segment:";
                worksheet.Cells[9, 2].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[9, 2].Style.Font.Bold = true;

                worksheet.Cells[11, 2].Value = "All Segment:";
                worksheet.Cells[11, 2].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[11, 2].Style.Font.Bold = true;

                var summaryPerSegmentColumnNames = new[]
                {
                    "Segment",
                    "Product",
                    "Quantity Served",
                    "Sales Amount (Vat Inc)",
                    "Sales Amount (Vat Ex)",
                    "Sales/ltr (Vat Ex)",
                    "Cost Amount (Vat Inc)",
                    "Cost Amount (Vat Ex)",
                    "Cost/ltr (Vat Ex)",
                    "Freight Amount (Vat Inc)",
                    "Freight Amount (Vat Ex)",
                    "Freight/ltr (Vat Ex)",
                    "Commission Amount",
                    "Commission/ltr",
                    "GM Amount",
                    "GM/ltr",
                };

                int col = 2;

                // ALL SEGMENT COLUMNS
                foreach (var columnName in summaryPerSegmentColumnNames)
                {
                    worksheet.Cells[12, col].Value = columnName;
                    worksheet.Cells[12, col].Style.WrapText = true;
                    col++;
                }
                // styling
                worksheet.Row(12).Height = 30;
                using (var range = worksheet.Cells[12, 2, 12, col - 1])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Font.Bold = true;
                }

                int row = 13;
                var totalQuantityServed = 0m;
                var totalSalesAmount = 0m;
                var totalSalesAmountVatEx = 0m;
                var totalCostAmount = 0m;
                var totalCostAmountVatEx = 0m;
                var totalFreightAmount = 0m;
                var totalFreightAmountEx = 0m;
                var totalCommissionAmount = 0m;
                var totalGmAmount = 0m;

                // ALL SEGMENT CONTENTS
                foreach (var product in listOfProducts)
                {
                    // LIST BY PRODUCT
                    var groupByProduct = rrsByProduct.FirstOrDefault(rr => rr.Key == product);
                    if (groupByProduct == null)
                    {
                        continue;
                    }

                    var quantityServed = groupByProduct.Sum(rr => rr.QuantityReceived);
                    var salesAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.TotalAmount);
                    var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                    var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                    var costAmount = groupByProduct.Sum(rr => rr.Amount);
                    var costAmountVatEx = NetOfVatOrZero(costAmount);
                    var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                    var freightAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.FreightAmount);
                    var freightAmountEx = NetOfVatOrZero(freightAmount);
                    var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                    var commissionAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.CommissionAmount);
                    var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                    var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                    var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                    // CONTENTS ENCODING
                    worksheet.Cells[row, 2].Value = "All Segment:";
                    worksheet.Cells[row, 3].Value = product;
                    worksheet.Cells[row, 4].Value = quantityServed;
                    worksheet.Cells[row, 5].Value = salesAmount;
                    worksheet.Cells[row, 6].Value = salesAmountVatEx;
                    worksheet.Cells[row, 7].Value = salesPerLiterVatEx;
                    worksheet.Cells[row, 8].Value = costAmount;
                    worksheet.Cells[row, 9].Value = costAmountVatEx;
                    worksheet.Cells[row, 10].Value = costPerLiterVatEx;
                    worksheet.Cells[row, 11].Value = freightAmount;
                    worksheet.Cells[row, 12].Value = freightAmountEx;
                    worksheet.Cells[row, 13].Value = freightPerLiterEx;
                    worksheet.Cells[row, 14].Value = commissionAmount;
                    worksheet.Cells[row, 15].Value = commissionPerLiter;
                    worksheet.Cells[row, 16].Value = gmAmount;
                    worksheet.Cells[row, 17].Value = gmPerLiter;
                    // styling
                    using (var range = worksheet.Cells[row, 4, row, 16])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    int[] fourDecimalColumns = [7, 10, 13, 15, 17];
                    foreach (var column in fourDecimalColumns)
                    {
                        worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                    }

                    row++;
                    totalQuantityServed += quantityServed;
                    totalSalesAmount += salesAmount;
                    totalSalesAmountVatEx += salesAmountVatEx;
                    totalCostAmount += costAmount;
                    totalCostAmountVatEx += costAmountVatEx;
                    totalFreightAmount += freightAmount;
                    totalFreightAmountEx += freightAmountEx;
                    totalCommissionAmount += commissionAmount;
                    totalGmAmount += gmAmount;
                }

                // ALL SEGMENT GRAND TOTAL
                worksheet.Cells[row, 3].Value = "Grand Total";
                worksheet.Cells[row, 4].Value = totalQuantityServed;
                worksheet.Cells[row, 5].Value = totalSalesAmount;
                worksheet.Cells[row, 6].Value = totalSalesAmountVatEx;
                worksheet.Cells[row, 7].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                worksheet.Cells[row, 8].Value = totalCostAmount;
                worksheet.Cells[row, 9].Value = totalCostAmountVatEx;
                worksheet.Cells[row, 10].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                worksheet.Cells[row, 11].Value = totalFreightAmount;
                worksheet.Cells[row, 12].Value = totalFreightAmountEx;
                worksheet.Cells[row, 13].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                worksheet.Cells[row, 14].Value = totalCommissionAmount;
                worksheet.Cells[row, 15].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                worksheet.Cells[row, 16].Value = totalGmAmount;
                worksheet.Cells[row, 17].Value = DivideOrZero(totalGmAmount, totalQuantityServed);
                // styling
                using (var range = worksheet.Cells[row, 4, row, 16])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }
                int[] fourDecimalColumnsGrandTotal = [7, 10, 13, 15, 17];
                foreach (var column in fourDecimalColumnsGrandTotal)
                {
                    worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                }
                using (var range = worksheet.Cells[row, 4, row, 17])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }
                using (var range = worksheet.Cells[row, 3, row, 17])
                {
                    range.Style.Font.Bold = true;
                }

                #region == Per Segment ==

                var groupedBySegment = receivingReportsThisMonth.GroupBy(rr => rr.DeliveryReceipt!.Customer!.CustomerType).ToList();

                foreach (var segment in Enum.GetValues<CustomerType>())
                {
                    var rrsBySegment = groupedBySegment.FirstOrDefault(rrs => rrs.Key == segment.ToString());
                    if (rrsBySegment == null)
                    {
                        continue;
                    }

                    row += 2;
                    totalQuantityServed = 0m;
                    totalSalesAmount = 0m;
                    totalSalesAmountVatEx = 0m;
                    totalCostAmount = 0m;
                    totalCostAmountVatEx = 0m;
                    totalFreightAmount = 0m;
                    totalFreightAmountEx = 0m;
                    totalCommissionAmount = 0m;
                    totalGmAmount = 0m;

                    // SEGMENT TITLE
                    worksheet.Cells[row, 2].Value = segment;
                    worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 2].Style.Font.Bold = true;

                    row++;
                    col = 2;

                    // SEGMENT COLUMN NAMES
                    foreach (var columnName in summaryPerSegmentColumnNames)
                    {
                        worksheet.Cells[row, col].Value = columnName;
                        worksheet.Cells[row, col].Style.WrapText = true;
                        col++;
                    }
                    // styling
                    worksheet.Row(row).Height = 30;
                    using (var range = worksheet.Cells[row, 2, row, 17])
                    {
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Bold = true;
                    }

                    row++;

                    // ALREADY LISTED BY SEGMENT, NOW LIST BY PRODUCT
                    var rrsBySegmentThenByProduct = rrsBySegment.GroupBy(rr => rr.PurchaseOrder!.Product!.ProductName);

                    // ENCODE PER PRODUCT
                    foreach (var product in listOfProducts)
                    {
                        // LIST BY PRODUCT
                        var groupByProduct = (rrsBySegmentThenByProduct.FirstOrDefault(rr => rr.Key == product))?.ToList();
                        if (groupByProduct == null)
                        {
                            continue;
                        }

                        var quantityServed = groupByProduct.Sum(rr => rr.QuantityReceived);
                        var salesAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.TotalAmount);
                        var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                        var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                        var costAmount = groupByProduct.Sum(rr => rr.Amount);
                        var costAmountVatEx = NetOfVatOrZero(costAmount);
                        var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                        var freightAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.FreightAmount);
                        var freightAmountEx = NetOfVatOrZero(freightAmount);
                        var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                        var commissionAmount = groupByProduct.Sum(rr => rr.DeliveryReceipt!.CommissionAmount);
                        var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                        var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                        var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                        // ENCODE, THIS SET IS BY PRODUCT
                        worksheet.Cells[row, 2].Value = segment;
                        worksheet.Cells[row, 3].Value = product;
                        worksheet.Cells[row, 4].Value = quantityServed;
                        worksheet.Cells[row, 5].Value = salesAmount;
                        worksheet.Cells[row, 6].Value = salesAmountVatEx;
                        worksheet.Cells[row, 7].Value = salesPerLiterVatEx;
                        worksheet.Cells[row, 8].Value = costAmount;
                        worksheet.Cells[row, 9].Value = costAmountVatEx;
                        worksheet.Cells[row, 10].Value = costPerLiterVatEx;
                        worksheet.Cells[row, 11].Value = freightAmount;
                        worksheet.Cells[row, 12].Value = freightAmountEx;
                        worksheet.Cells[row, 13].Value = freightPerLiterEx;
                        worksheet.Cells[row, 14].Value = commissionAmount;
                        worksheet.Cells[row, 15].Value = commissionPerLiter;
                        worksheet.Cells[row, 16].Value = gmAmount;
                        worksheet.Cells[row, 17].Value = gmPerLiter;
                        // styling
                        using (var range = worksheet.Cells[row, 4, row, 17])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        int[] fourDecimalColumns = [7, 10, 13, 15, 17];
                        foreach (var column in fourDecimalColumns)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                        }

                        row++;
                        totalQuantityServed += quantityServed;
                        totalSalesAmount += salesAmount;
                        totalSalesAmountVatEx += salesAmountVatEx;
                        totalCostAmount += costAmount;
                        totalCostAmountVatEx += costAmountVatEx;
                        totalFreightAmount += freightAmount;
                        totalFreightAmountEx += freightAmountEx;
                        totalCommissionAmount += commissionAmount;
                        totalGmAmount += gmAmount;
                    }

                    // SUBTOTAL BY SEGMENT
                    worksheet.Cells[row, 3].Value = "Sub Total";
                    worksheet.Cells[row, 4].Value = totalQuantityServed;
                    worksheet.Cells[row, 5].Value = totalSalesAmount;
                    worksheet.Cells[row, 6].Value = totalSalesAmountVatEx;
                    worksheet.Cells[row, 7].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 8].Value = totalCostAmount;
                    worksheet.Cells[row, 9].Value = totalCostAmountVatEx;
                    worksheet.Cells[row, 10].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 11].Value = totalFreightAmount;
                    worksheet.Cells[row, 12].Value = totalFreightAmountEx;
                    worksheet.Cells[row, 13].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                    worksheet.Cells[row, 14].Value = totalCommissionAmount;
                    worksheet.Cells[row, 15].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                    worksheet.Cells[row, 16].Value = totalGmAmount;
                    worksheet.Cells[row, 17].Value = DivideOrZero(totalGmAmount, totalQuantityServed);
                    // styling
                    using (var range = worksheet.Cells[row, 4, row, 17])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    foreach (var column in fourDecimalColumnsGrandTotal)
                    {
                        worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                    }
                    using (var range = worksheet.Cells[row, 4, row, 17])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                    using (var range = worksheet.Cells[row, 3, row, 17])
                    {
                        range.Style.Font.Bold = true;
                    }
                }

                row += 3;

                #endregion == Per Segment ==

                #endregion == Section A: Summary Per Segment ==

                #region == Section B: Breakdown of Intransit and Other Income ==

                worksheet.Cells[row, 2].Value = "B. Breakdown of Intransit and Other Income:";
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[row, 2].Style.Font.Bold = true;

                if (inTransitPrevToThisMonth.Count != 0)
                {
                    row += 2;

                    // SEGMENT TITLE
                    worksheet.Cells[row, 2].Value = "I. Purchased/Lifted last month, Sold/Delivered this month:";
                    worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 2].Style.Font.Bold = true;

                    row++;
                    col = 2;

                    // SEGMENT COLUMN NAMES
                    foreach (var columnName in breakdownColumnNames)
                    {
                        worksheet.Cells[row, col].Value = columnName;
                        worksheet.Cells[row, col].Style.WrapText = true;
                        col++;
                    }
                    // styling
                    worksheet.Row(row).Height = 30;
                    using (var range = worksheet.Cells[row, 2, row, 24])
                    {
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Bold = true;
                    }

                    row++;
                    totalQuantityServed = 0m;
                    totalSalesAmount = 0m;
                    totalSalesAmountVatEx = 0m;
                    totalCostAmount = 0m;
                    totalCostAmountVatEx = 0m;
                    totalFreightAmount = 0m;
                    totalFreightAmountEx = 0m;
                    totalCommissionAmount = 0m;
                    totalGmAmount = 0m;

                    foreach (var receivingReport in inTransitPrevToThisMonth)
                    {
                        var quantityServed = receivingReport.QuantityReceived;
                        var salesAmount = receivingReport.DeliveryReceipt!.TotalAmount;
                        var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                        var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                        var costAmount = receivingReport.Amount;
                        var costAmountVatEx = NetOfVatOrZero(costAmount);
                        var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                        var freightAmount = receivingReport.DeliveryReceipt!.FreightAmount;
                        var freightAmountEx = NetOfVatOrZero(freightAmount);
                        var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                        var commissionAmount = receivingReport.DeliveryReceipt!.CommissionAmount;
                        var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                        var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                        var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                        // SUBTOTAL BY SEGMENT
                        worksheet.Cells[row, 2].Value = receivingReport.Date.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 3].Value = receivingReport.DeliveryReceipt!.DeliveredDate?.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 4].Value = receivingReport.DeliveryReceipt.Customer!.CustomerType;
                        worksheet.Cells[row, 5].Value = receivingReport.PurchaseOrder!.Supplier!.SupplierName;
                        worksheet.Cells[row, 6].Value = receivingReport.PurchaseOrder!.PurchaseOrderNo;
                        worksheet.Cells[row, 7].Value = receivingReport.ReceivingReportNo;
                        worksheet.Cells[row, 8].Value = receivingReport.DeliveryReceipt.DeliveryReceiptNo;
                        worksheet.Cells[row, 9].Value = receivingReport.DeliveryReceipt.Customer.CustomerName;
                        worksheet.Cells[row, 10].Value = receivingReport.PurchaseOrder.ProductName;
                        worksheet.Cells[row, 11].Value = quantityServed;
                        worksheet.Cells[row, 12].Value = salesAmount;
                        worksheet.Cells[row, 13].Value = salesAmountVatEx;
                        worksheet.Cells[row, 14].Value = salesPerLiterVatEx;
                        worksheet.Cells[row, 15].Value = costAmount;
                        worksheet.Cells[row, 16].Value = costAmountVatEx;
                        worksheet.Cells[row, 17].Value = commissionPerLiter;
                        worksheet.Cells[row, 18].Value = freightAmount;
                        worksheet.Cells[row, 19].Value = freightAmountEx;
                        worksheet.Cells[row, 20].Value = freightPerLiterEx;
                        worksheet.Cells[row, 21].Value = commissionAmount;
                        worksheet.Cells[row, 22].Value = commissionPerLiter;
                        worksheet.Cells[row, 23].Value = gmAmount;
                        worksheet.Cells[row, 24].Value = gmPerLiter;

                        // styling
                        using (var range = worksheet.Cells[row, 11, row, 23])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                        foreach (var column in fourDecimalColumnsGrandTotal)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                        }

                        row++;
                        totalQuantityServed += quantityServed;
                        totalSalesAmount += salesAmount;
                        totalSalesAmountVatEx += salesAmountVatEx;
                        totalCostAmount += costAmount;
                        totalCostAmountVatEx += costAmountVatEx;
                        totalFreightAmount += freightAmount;
                        totalFreightAmountEx += freightAmountEx;
                        totalCommissionAmount += commissionAmount;
                        totalGmAmount += gmAmount;
                    }

                    row++;

                    worksheet.Cells[row, 10].Value = "Sub-total";
                    worksheet.Cells[row, 11].Value = totalQuantityServed;
                    worksheet.Cells[row, 12].Value = totalSalesAmount;
                    worksheet.Cells[row, 13].Value = totalSalesAmountVatEx;
                    worksheet.Cells[row, 14].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 15].Value = totalCostAmount;
                    worksheet.Cells[row, 16].Value = totalCostAmountVatEx;
                    worksheet.Cells[row, 17].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 18].Value = totalFreightAmount;
                    worksheet.Cells[row, 19].Value = totalFreightAmountEx;
                    worksheet.Cells[row, 20].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                    worksheet.Cells[row, 21].Value = totalCommissionAmount;
                    worksheet.Cells[row, 22].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                    worksheet.Cells[row, 23].Value = totalGmAmount;
                    worksheet.Cells[row, 24].Value = DivideOrZero(totalGmAmount, totalQuantityServed);

                    // styling
                    using (var range = worksheet.Cells[row, 11, row, 23])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                    foreach (var column in fourDecimalColumnsGrandTotal)
                    {
                        worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                    }
                    using (var range = worksheet.Cells[row, 11, row, 24])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                    using (var range = worksheet.Cells[row, 10, row, 24])
                    {
                        range.Style.Font.Bold = true;
                    }
                }

                if (inTransitNowToNextMonth.Count != 0)
                {
                    row += 2;

                    // SEGMENT TITLE
                    worksheet.Cells[row, 2].Value = "II. Purchased/Lifted this month, Sold/Delivered next month:";
                    worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 2].Style.Font.Bold = true;

                    row++;
                    col = 2;

                    // SEGMENT COLUMN NAMES
                    foreach (var columnName in breakdownColumnNames)
                    {
                        worksheet.Cells[row, col].Value = columnName;
                        worksheet.Cells[row, col].Style.WrapText = true;
                        col++;
                    }
                    // styling
                    worksheet.Row(row).Height = 30;
                    using (var range = worksheet.Cells[row, 2, row, 24])
                    {
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Bold = true;
                    }

                    row++;
                    totalQuantityServed = 0m;
                    totalSalesAmount = 0m;
                    totalSalesAmountVatEx = 0m;
                    totalCostAmount = 0m;
                    totalCostAmountVatEx = 0m;
                    totalFreightAmount = 0m;
                    totalFreightAmountEx = 0m;
                    totalCommissionAmount = 0m;
                    totalGmAmount = 0m;

                    foreach (var receivingReport in inTransitNowToNextMonth)
                    {
                        var quantityServed = receivingReport.QuantityReceived;
                        var salesAmount = receivingReport.DeliveryReceipt!.TotalAmount;
                        var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                        var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                        var costAmount = receivingReport.Amount;
                        var costAmountVatEx = NetOfVatOrZero(costAmount);
                        var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                        var freightAmount = receivingReport.DeliveryReceipt!.FreightAmount;
                        var freightAmountEx = NetOfVatOrZero(freightAmount);
                        var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                        var commissionAmount = receivingReport.DeliveryReceipt!.CommissionAmount;
                        var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                        var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                        var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                        // SUBTOTAL BY SEGMENT
                        worksheet.Cells[row, 2].Value = receivingReport.Date.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 3].Value = receivingReport.DeliveryReceipt!.DeliveredDate?.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 4].Value = receivingReport.DeliveryReceipt.Customer!.CustomerType;
                        worksheet.Cells[row, 5].Value = receivingReport.PurchaseOrder!.Supplier!.SupplierName;
                        worksheet.Cells[row, 6].Value = receivingReport.PurchaseOrder!.PurchaseOrderNo;
                        worksheet.Cells[row, 7].Value = receivingReport.ReceivingReportNo;
                        worksheet.Cells[row, 8].Value = receivingReport.DeliveryReceipt.DeliveryReceiptNo;
                        worksheet.Cells[row, 9].Value = receivingReport.DeliveryReceipt.Customer.CustomerName;
                        worksheet.Cells[row, 10].Value = receivingReport.PurchaseOrder.ProductName;
                        worksheet.Cells[row, 11].Value = quantityServed;
                        worksheet.Cells[row, 12].Value = salesAmount;
                        worksheet.Cells[row, 13].Value = salesAmountVatEx;
                        worksheet.Cells[row, 14].Value = salesPerLiterVatEx;
                        worksheet.Cells[row, 15].Value = costAmount;
                        worksheet.Cells[row, 16].Value = costAmountVatEx;
                        worksheet.Cells[row, 17].Value = commissionPerLiter;
                        worksheet.Cells[row, 18].Value = freightAmount;
                        worksheet.Cells[row, 19].Value = freightAmountEx;
                        worksheet.Cells[row, 20].Value = freightPerLiterEx;
                        worksheet.Cells[row, 21].Value = commissionAmount;
                        worksheet.Cells[row, 22].Value = commissionPerLiter;
                        worksheet.Cells[row, 23].Value = gmAmount;
                        worksheet.Cells[row, 24].Value = gmPerLiter;

                        // styling
                        using (var range = worksheet.Cells[row, 11, row, 23])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                        foreach (var column in fourDecimalColumnsGrandTotal)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                        }

                        row++;
                        totalQuantityServed += quantityServed;
                        totalSalesAmount += salesAmount;
                        totalSalesAmountVatEx += salesAmountVatEx;
                        totalCostAmount += costAmount;
                        totalCostAmountVatEx += costAmountVatEx;
                        totalFreightAmount += freightAmount;
                        totalFreightAmountEx += freightAmountEx;
                        totalCommissionAmount += commissionAmount;
                        totalGmAmount += gmAmount;
                    }

                    row++;

                    worksheet.Cells[row, 10].Value = "Sub-total";
                    worksheet.Cells[row, 11].Value = totalQuantityServed;
                    worksheet.Cells[row, 12].Value = totalSalesAmount;
                    worksheet.Cells[row, 13].Value = totalSalesAmountVatEx;
                    worksheet.Cells[row, 14].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 15].Value = totalCostAmount;
                    worksheet.Cells[row, 16].Value = totalCostAmountVatEx;
                    worksheet.Cells[row, 17].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 18].Value = totalFreightAmount;
                    worksheet.Cells[row, 19].Value = totalFreightAmountEx;
                    worksheet.Cells[row, 20].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                    worksheet.Cells[row, 21].Value = totalCommissionAmount;
                    worksheet.Cells[row, 22].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                    worksheet.Cells[row, 23].Value = totalGmAmount;
                    worksheet.Cells[row, 24].Value = DivideOrZero(totalGmAmount, totalQuantityServed);

                    // styling
                    using (var range = worksheet.Cells[row, 11, row, 23])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                    foreach (var column in fourDecimalColumnsGrandTotal)
                    {
                        worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                    }
                    using (var range = worksheet.Cells[row, 11, row, 24])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                    using (var range = worksheet.Cells[row, 10, row, 24])
                    {
                        range.Style.Font.Bold = true;
                    }
                }

                if (rrWithIOCForAccountOfMMSI.Count != 0)
                {
                    row += 2;

                    // SEGMENT TITLE
                    worksheet.Cells[row, 2].Value = "III. Breakdown of Trading Fee to MMSI";
                    worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                    worksheet.Cells[row, 2].Style.Font.Bold = true;

                    row++;
                    col = 2;

                    // SEGMENT COLUMN NAMES
                    foreach (var columnName in breakdownColumnNames)
                    {
                        worksheet.Cells[row, col].Value = columnName;
                        worksheet.Cells[row, col].Style.WrapText = true;
                        col++;
                    }
                    // styling
                    worksheet.Row(row).Height = 30;
                    using (var range = worksheet.Cells[row, 2, row, 24])
                    {
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        range.Style.Font.Bold = true;
                    }

                    row++;
                    totalQuantityServed = 0m;
                    totalSalesAmount = 0m;
                    totalSalesAmountVatEx = 0m;
                    totalCostAmount = 0m;
                    totalCostAmountVatEx = 0m;
                    totalFreightAmount = 0m;
                    totalFreightAmountEx = 0m;
                    totalCommissionAmount = 0m;
                    totalGmAmount = 0m;

                    foreach (var receivingReport in rrWithIOCForAccountOfMMSI)
                    {
                        var quantityServed = receivingReport.QuantityReceived;
                        var salesAmount = receivingReport.DeliveryReceipt!.TotalAmount;
                        var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                        var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                        var costAmount = receivingReport.Amount;
                        var costAmountVatEx = NetOfVatOrZero(costAmount);
                        var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                        var freightAmount = receivingReport.DeliveryReceipt!.FreightAmount;
                        var freightAmountEx = NetOfVatOrZero(freightAmount);
                        var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                        var commissionAmount = receivingReport.DeliveryReceipt!.CommissionAmount;
                        var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                        var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                        var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                        // SUBTOTAL BY SEGMENT
                        worksheet.Cells[row, 2].Value = receivingReport.Date.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 3].Value = receivingReport.DeliveryReceipt!.DeliveredDate?.ToString("MM/dd/yyyy");
                        worksheet.Cells[row, 4].Value = receivingReport.DeliveryReceipt.Customer!.CustomerType;
                        worksheet.Cells[row, 5].Value = receivingReport.PurchaseOrder!.Supplier!.SupplierName;
                        worksheet.Cells[row, 6].Value = receivingReport.PurchaseOrder!.PurchaseOrderNo;
                        worksheet.Cells[row, 7].Value = receivingReport.ReceivingReportNo;
                        worksheet.Cells[row, 8].Value = receivingReport.DeliveryReceipt.DeliveryReceiptNo;
                        worksheet.Cells[row, 9].Value = receivingReport.DeliveryReceipt.Customer.CustomerName;
                        worksheet.Cells[row, 10].Value = receivingReport.PurchaseOrder.ProductName;
                        worksheet.Cells[row, 11].Value = quantityServed;
                        worksheet.Cells[row, 12].Value = salesAmount;
                        worksheet.Cells[row, 13].Value = salesAmountVatEx;
                        worksheet.Cells[row, 14].Value = salesPerLiterVatEx;
                        worksheet.Cells[row, 15].Value = costAmount;
                        worksheet.Cells[row, 16].Value = costAmountVatEx;
                        worksheet.Cells[row, 17].Value = commissionPerLiter;
                        worksheet.Cells[row, 18].Value = freightAmount;
                        worksheet.Cells[row, 19].Value = freightAmountEx;
                        worksheet.Cells[row, 20].Value = freightPerLiterEx;
                        worksheet.Cells[row, 21].Value = commissionAmount;
                        worksheet.Cells[row, 22].Value = commissionPerLiter;
                        worksheet.Cells[row, 23].Value = gmAmount;
                        worksheet.Cells[row, 24].Value = gmPerLiter;

                        // styling
                        using (var range = worksheet.Cells[row, 11, row, 23])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                        foreach (var column in fourDecimalColumnsGrandTotal)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                        }

                        row++;
                        totalQuantityServed += quantityServed;
                        totalSalesAmount += salesAmount;
                        totalSalesAmountVatEx += salesAmountVatEx;
                        totalCostAmount += costAmount;
                        totalCostAmountVatEx += costAmountVatEx;
                        totalFreightAmount += freightAmount;
                        totalFreightAmountEx += freightAmountEx;
                        totalCommissionAmount += commissionAmount;
                        totalGmAmount += gmAmount;
                    }

                    row++;

                    worksheet.Cells[row, 10].Value = "Sub-total";
                    worksheet.Cells[row, 11].Value = totalQuantityServed;
                    worksheet.Cells[row, 12].Value = totalSalesAmount;
                    worksheet.Cells[row, 13].Value = totalSalesAmountVatEx;
                    worksheet.Cells[row, 14].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 15].Value = totalCostAmount;
                    worksheet.Cells[row, 16].Value = totalCostAmountVatEx;
                    worksheet.Cells[row, 17].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                    worksheet.Cells[row, 18].Value = totalFreightAmount;
                    worksheet.Cells[row, 19].Value = totalFreightAmountEx;
                    worksheet.Cells[row, 20].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                    worksheet.Cells[row, 21].Value = totalCommissionAmount;
                    worksheet.Cells[row, 22].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                    worksheet.Cells[row, 23].Value = totalGmAmount;
                    worksheet.Cells[row, 24].Value = DivideOrZero(totalGmAmount, totalQuantityServed);

                    // styling
                    using (var range = worksheet.Cells[row, 11, row, 23])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                    foreach (var column in fourDecimalColumnsGrandTotal)
                    {
                        worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                    }
                    using (var range = worksheet.Cells[row, 11, row, 24])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    }
                    using (var range = worksheet.Cells[row, 10, row, 24])
                    {
                        range.Style.Font.Bold = true;
                    }
                }

                row += 3;

                #endregion == Section B: Breakdown of Intransit and Other Income ==

                #region == Section C: Breakdown of Purchases Per Segment: ==

                worksheet.Cells[row, 2].Value = "C. Breakdown of Purchases Per Segment:";
                worksheet.Cells[row, 2].Style.Font.Color.SetColor(Color.Red);
                worksheet.Cells[row, 2].Style.Font.Bold = true;

                var grandTotalQuantityServed = 0m;
                var grandTotalSalesAmount = 0m;
                var grandTotalSalesAmountVatEx = 0m;
                var grandTotalCostAmount = 0m;
                var grandTotalCostAmountVatEx = 0m;
                var grandTotalFreightAmount = 0m;
                var grandTotalFreightAmountEx = 0m;
                var grandTotalCommissionAmount = 0m;
                var grandTotalGmAmount = 0m;

                foreach (var segment in Enum.GetValues<CustomerType>())
                {
                    foreach (var product in listOfProducts)
                    {
                        var rrSetBySegmentAndProduct = receivingReportsThisMonth
                            .Where(rr =>
                                rr.DeliveryReceipt!.Customer!.CustomerType == segment.ToString() &&
                                rr.PurchaseOrder!.ProductName == product)
                            .OrderBy(rr => rr.Date)
                            .ToList();

                        if (rrSetBySegmentAndProduct.Count != 0)
                        {
                            row += 2;

                            // SEGMENT TITLE
                            worksheet.Cells[row, 2].Value = segment.ToString() == "Retail" ? $"{segment}/MOBILITY: {product}" : $"{segment}: {product}";
                            worksheet.Cells[row, 2].Style.Font.Bold = true;

                            row++;
                            col = 2;

                            // SEGMENT COLUMN NAMES
                            foreach (var columnName in breakdownColumnNames)
                            {
                                worksheet.Cells[row, col].Value = columnName;
                                worksheet.Cells[row, col].Style.WrapText = true;
                                col++;
                            }
                            // styling
                            worksheet.Row(row).Height = 30;
                            using (var range = worksheet.Cells[row, 2, row, 24])
                            {
                                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                                range.Style.Font.Bold = true;
                            }

                            row++;
                            totalQuantityServed = 0m;
                            totalSalesAmount = 0m;
                            totalSalesAmountVatEx = 0m;
                            totalCostAmount = 0m;
                            totalCostAmountVatEx = 0m;
                            totalFreightAmount = 0m;
                            totalFreightAmountEx = 0m;
                            totalCommissionAmount = 0m;
                            totalGmAmount = 0m;

                            foreach (var receivingReport in rrSetBySegmentAndProduct)
                            {
                                var quantityServed = receivingReport.QuantityReceived;
                                var salesAmount = receivingReport.DeliveryReceipt!.TotalAmount;
                                var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                                var salesPerLiterVatEx = quantityServed > 0
                                    ? DivideOrZero(salesAmountVatEx, quantityServed)
                                    : 0m;
                                var costAmount = receivingReport.Amount;
                                var costAmountVatEx = NetOfVatOrZero(costAmount);
                                var costPerLiterVatEx = quantityServed > 0
                                    ? DivideOrZero(costAmountVatEx, quantityServed)
                                    : 0m;
                                var freightAmount = receivingReport.DeliveryReceipt!.FreightAmount;
                                var freightAmountEx = NetOfVatOrZero(freightAmount);
                                var freightPerLiterEx = quantityServed > 0
                                    ? DivideOrZero(freightAmountEx, quantityServed)
                                    : 0m;
                                var commissionAmount = receivingReport.DeliveryReceipt!.CommissionAmount;
                                var commissionPerLiter = quantityServed > 0
                                    ? DivideOrZero(commissionAmount, quantityServed)
                                    : 0m;
                                var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                                var gmPerLiter = quantityServed > 0
                                    ? DivideOrZero(gmAmount, quantityServed)
                                    : 0m;

                                // SUBTOTAL BY SEGMENT
                                worksheet.Cells[row, 2].Value = receivingReport.Date.ToString("MM/dd/yyyy");
                                worksheet.Cells[row, 3].Value = receivingReport.DeliveryReceipt!.DeliveredDate?.ToString("MM/dd/yyyy");
                                worksheet.Cells[row, 4].Value = receivingReport.DeliveryReceipt.Customer!.CustomerType;
                                worksheet.Cells[row, 5].Value = receivingReport.PurchaseOrder!.Supplier!.SupplierName;
                                worksheet.Cells[row, 6].Value = receivingReport.PurchaseOrder!.PurchaseOrderNo;
                                worksheet.Cells[row, 7].Value = receivingReport.ReceivingReportNo;
                                worksheet.Cells[row, 8].Value = receivingReport.DeliveryReceipt.DeliveryReceiptNo;
                                worksheet.Cells[row, 9].Value = receivingReport.DeliveryReceipt.Customer.CustomerName;
                                worksheet.Cells[row, 10].Value = receivingReport.PurchaseOrder.Product!.ProductName;
                                worksheet.Cells[row, 11].Value = quantityServed;
                                worksheet.Cells[row, 12].Value = salesAmount;
                                worksheet.Cells[row, 13].Value = salesAmountVatEx;
                                worksheet.Cells[row, 14].Value = salesPerLiterVatEx;
                                worksheet.Cells[row, 15].Value = costAmount;
                                worksheet.Cells[row, 16].Value = costAmountVatEx;
                                worksheet.Cells[row, 17].Value = commissionPerLiter;
                                worksheet.Cells[row, 18].Value = freightAmount;
                                worksheet.Cells[row, 19].Value = freightAmountEx;
                                worksheet.Cells[row, 20].Value = freightPerLiterEx;
                                worksheet.Cells[row, 21].Value = commissionAmount;
                                worksheet.Cells[row, 22].Value = commissionPerLiter;
                                worksheet.Cells[row, 23].Value = gmAmount;
                                worksheet.Cells[row, 24].Value = gmPerLiter;

                                // styling
                                using (var range = worksheet.Cells[row, 11, row, 23])
                                {
                                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                                foreach (var column in fourDecimalColumnsGrandTotal)
                                {
                                    worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                                }

                                row++;
                                totalQuantityServed += quantityServed;
                                totalSalesAmount += salesAmount;
                                totalSalesAmountVatEx += salesAmountVatEx;
                                totalCostAmount += costAmount;
                                totalCostAmountVatEx += costAmountVatEx;
                                totalFreightAmount += freightAmount;
                                totalFreightAmountEx += freightAmountEx;
                                totalCommissionAmount += commissionAmount;
                                totalGmAmount += gmAmount;
                            }

                            row++;

                            worksheet.Cells[row, 10].Value = $"Sub-total ({product})";
                            worksheet.Cells[row, 11].Value = totalQuantityServed;
                            worksheet.Cells[row, 12].Value = totalSalesAmount;
                            worksheet.Cells[row, 13].Value = totalSalesAmountVatEx;
                            worksheet.Cells[row, 14].Value = DivideOrZero(totalSalesAmountVatEx, totalQuantityServed);
                            worksheet.Cells[row, 15].Value = totalCostAmount;
                            worksheet.Cells[row, 16].Value = totalCostAmountVatEx;
                            worksheet.Cells[row, 17].Value = DivideOrZero(totalCostAmountVatEx, totalQuantityServed);
                            worksheet.Cells[row, 18].Value = totalFreightAmount;
                            worksheet.Cells[row, 19].Value = totalFreightAmountEx;
                            worksheet.Cells[row, 20].Value = DivideOrZero(totalFreightAmountEx, totalQuantityServed);
                            worksheet.Cells[row, 21].Value = totalCommissionAmount;
                            worksheet.Cells[row, 22].Value = DivideOrZero(totalCommissionAmount, totalQuantityServed);
                            worksheet.Cells[row, 23].Value = totalGmAmount;
                            worksheet.Cells[row, 24].Value = DivideOrZero(totalGmAmount, totalQuantityServed);

                            // styling
                            using (var range = worksheet.Cells[row, 11, row, 23])
                            {
                                range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }
                            fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                            foreach (var column in fourDecimalColumnsGrandTotal)
                            {
                                worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                            }
                            using (var range = worksheet.Cells[row, 11, row, 24])
                            {
                                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            }
                            using (var range = worksheet.Cells[row, 10, row, 24])
                            {
                                range.Style.Font.Bold = true;
                            }
                        }
                    }

                    var rrSetBySegment = receivingReportsThisMonth
                        .Where(rr =>
                            rr.DeliveryReceipt!.Customer!.CustomerType == segment.ToString())
                        .OrderBy(rr => rr.Date)
                        .ToList();

                    if (rrSetBySegment.Count != 0)
                    {
                        row += 2;
                        var quantityServed = rrSetBySegment.Sum(rr => rr.QuantityReceived);
                        var salesAmount = rrSetBySegment.Sum(rr => rr.DeliveryReceipt!.TotalAmount);
                        var salesAmountVatEx = NetOfVatOrZero(salesAmount);
                        var salesPerLiterVatEx = DivideOrZero(salesAmountVatEx, quantityServed);
                        var costAmount = rrSetBySegment.Sum(rr => rr.Amount);
                        var costAmountVatEx = NetOfVatOrZero(costAmount);
                        var costPerLiterVatEx = DivideOrZero(costAmountVatEx, quantityServed);
                        var freightAmount = rrSetBySegment.Sum(rr => rr.DeliveryReceipt!.FreightAmount);
                        var freightAmountEx = NetOfVatOrZero(freightAmount);
                        var freightPerLiterEx = DivideOrZero(freightAmountEx, quantityServed);
                        var commissionAmount = rrSetBySegment.Sum(rr => rr.DeliveryReceipt!.CommissionAmount);
                        var commissionPerLiter = DivideOrZero(commissionAmount, quantityServed);
                        var gmAmount = RoundToFour(salesAmountVatEx - costAmountVatEx - freightAmountEx - commissionAmount);
                        var gmPerLiter = DivideOrZero(gmAmount, quantityServed);

                        worksheet.Cells[row, 10].Value = "Sub-total (All Products)";
                        worksheet.Cells[row, 11].Value = quantityServed;
                        worksheet.Cells[row, 12].Value = salesAmount;
                        worksheet.Cells[row, 13].Value = salesAmountVatEx;
                        worksheet.Cells[row, 14].Value = salesPerLiterVatEx;
                        worksheet.Cells[row, 15].Value = costAmount;
                        worksheet.Cells[row, 16].Value = costAmountVatEx;
                        worksheet.Cells[row, 17].Value = commissionPerLiter;
                        worksheet.Cells[row, 18].Value = freightAmount;
                        worksheet.Cells[row, 19].Value = freightAmountEx;
                        worksheet.Cells[row, 20].Value = freightPerLiterEx;
                        worksheet.Cells[row, 21].Value = commissionAmount;
                        worksheet.Cells[row, 22].Value = commissionPerLiter;
                        worksheet.Cells[row, 23].Value = gmAmount;
                        worksheet.Cells[row, 24].Value = gmPerLiter;

                        // styling
                        using (var range = worksheet.Cells[row, 11, row, 23])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                        foreach (var column in fourDecimalColumnsGrandTotal)
                        {
                            worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                        }
                        using (var range = worksheet.Cells[row, 11, row, 24])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        }
                        using (var range = worksheet.Cells[row, 10, row, 24])
                        {
                            range.Style.Font.Bold = true;
                        }

                        grandTotalQuantityServed += quantityServed;
                        grandTotalSalesAmount += salesAmount;
                        grandTotalSalesAmountVatEx += salesAmountVatEx;
                        grandTotalCostAmount += costAmount;
                        grandTotalCostAmountVatEx += costAmountVatEx;
                        grandTotalFreightAmount += freightAmount;
                        grandTotalFreightAmountEx += freightAmountEx;
                        grandTotalCommissionAmount += commissionAmount;
                        grandTotalGmAmount += gmAmount;
                    }
                }

                row += 2;

                worksheet.Cells[row, 10].Value = "Grand-total (All Segments)";
                worksheet.Cells[row, 11].Value = grandTotalQuantityServed;
                worksheet.Cells[row, 12].Value = grandTotalSalesAmount;
                worksheet.Cells[row, 13].Value = grandTotalSalesAmountVatEx;
                worksheet.Cells[row, 14].Value = DivideOrZero(grandTotalSalesAmountVatEx, grandTotalQuantityServed);
                worksheet.Cells[row, 15].Value = grandTotalCostAmount;
                worksheet.Cells[row, 16].Value = grandTotalCostAmountVatEx;
                worksheet.Cells[row, 17].Value = DivideOrZero(grandTotalCostAmountVatEx, grandTotalQuantityServed);
                worksheet.Cells[row, 18].Value = grandTotalFreightAmount;
                worksheet.Cells[row, 19].Value = grandTotalFreightAmountEx;
                worksheet.Cells[row, 20].Value = DivideOrZero(grandTotalFreightAmountEx, grandTotalQuantityServed);
                worksheet.Cells[row, 21].Value = grandTotalCommissionAmount;
                worksheet.Cells[row, 22].Value = DivideOrZero(grandTotalCommissionAmount, grandTotalQuantityServed);
                worksheet.Cells[row, 23].Value = grandTotalGmAmount;
                worksheet.Cells[row, 24].Value = DivideOrZero(grandTotalGmAmount, grandTotalQuantityServed);

                // styling
                using (var range = worksheet.Cells[row, 11, row, 23])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }
                fourDecimalColumnsGrandTotal = [14, 17, 20, 22, 24];
                foreach (var column in fourDecimalColumnsGrandTotal)
                {
                    worksheet.Cells[row, column].Style.Numberformat.Format = currencyFormatFourDecimal;
                }
                using (var range = worksheet.Cells[row, 11, row, 24])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }
                using (var range = worksheet.Cells[row, 10, row, 24])
                {
                    range.Style.Font.Bold = true;
                }

                #endregion == Section C: Breakdown of Purchases Per Segment: ==

                worksheet.Columns.AutoFit();
                worksheet.Column(1).Width = 2;
                worksheet.Column(2).Width = 17;
                worksheet.Column(3).Width = 17;
                worksheet.Column(4).Width = 19;
                worksheet.Column(5).Width = 29;
                worksheet.Column(6).Width = 22;
                worksheet.Column(7).Width = 15;
                worksheet.Column(8).Width = 19;
                worksheet.Column(9).Width = 27;
                worksheet.Column(10).Width = 22;
                worksheet.Column(11).Width = 19;
                worksheet.Column(12).Width = 16;
                worksheet.Column(13).Width = 13;
                worksheet.Column(14).Width = 14;
                worksheet.Column(15).Width = 15;
                worksheet.Column(16).Width = 19;
                worksheet.Column(17).Width = 11;
                worksheet.Column(18).Width = 14;
                worksheet.Column(19).Width = 13;
                worksheet.Column(20).Width = 10;
                worksheet.Column(21).Width = 11;
                worksheet.Column(22).Width = 13;
                worksheet.Column(23).Width = 16;
                worksheet.Column(24).Width = 9;
                worksheet.Cells.Style.Font.Name = "Calibri";

                #endregion == Contents ==

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate purchase journal report excel file", "Liquidation Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Purchase_Journal_Report_{viewModel.Period!.Value.ToString("MMMM_yyyy")}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate purchase journal report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseJournalReport));
            }
        }

        #endregion -- Generate Purchase Journal Report Excel File --

        [HttpGet]
        public IActionResult HaulerPayableReport()
        {
            return View();
        }

        #region -- Generate Hauler Payable Report Excel File --

        [HttpPost]
        public async Task<IActionResult> GenerateHaulerPayableReportExcelFile(ViewModelBook viewModel, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(HaulerPayableReport));
            }

            try
            {
                var dateFrom = viewModel.DateFrom;
                var dateTo = viewModel.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var currencyFormat = "#,##0.00";

                var allCv = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv => cv.Category == "Trade" && cv.CvType == "Hauler" && cv.Date <= dateTo)
                    .Include(cv => cv.Supplier)
                    .ToListAsync(cancellationToken);

                var cvIdOfSelected = allCv
                    .Where(cv => cv.Date >= dateFrom)
                    .Select(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                var cvIdOfPrevious = allCv
                    .Where(cv => cv.Date < dateFrom)
                    .Select(cv => cv.CheckVoucherHeaderId)
                    .ToList();

                var cvPaymentsOfSelected = await _dbContext.FilprideCVTradePayments
                    .Where(ctp => cvIdOfSelected.Contains(ctp.DocumentId) && ctp.DocumentType == "DR")
                    .Include(ctp => ctp.CV)
                    .ToListAsync(cancellationToken);

                var cvPaymentsOfPrevious = await _dbContext.FilprideCVTradePayments
                    .Where(ctp => cvIdOfPrevious.Contains(ctp.DocumentId) && ctp.DocumentType == "DR")
                    .Include(ctp => ctp.CV)
                    .ToListAsync(cancellationToken);

                var idsOfDrsOfSelectedPeriodFromCv = cvPaymentsOfSelected
                    .Select(ctp => new
                    {
                        DeliveryReceiptId = ctp.DocumentId,
                        ctp.AmountPaid
                    })
                    .ToList();

                var idsOfDrsOfPreviousPeriodsFromCv = cvPaymentsOfPrevious
                    .Select(ctp => new
                    {
                        DeliveryReceiptId = ctp.DocumentId,
                        ctp.AmountPaid
                    })
                    .ToList();

                var allDr = await _unitOfWork.FilprideReport
                    .GetHaulerPayableReport(viewModel.DateFrom, viewModel.DateTo, companyClaims, cancellationToken);

                var drAndAmountPaidForSelectedPeriodFromCv = allDr
                    .Where(dr => idsOfDrsOfSelectedPeriodFromCv.Select(drSet => drSet.DeliveryReceiptId).ToList().Contains(dr.DeliveryReceiptId) &&
                    dr.FreightAmount != 0m)
                    .Select(drSet => new DrWithAmountPaidViewModel
                    {
                        DeliveryReceipt = drSet,
                        AmountPaid = idsOfDrsOfSelectedPeriodFromCv.FirstOrDefault(dr => dr.DeliveryReceiptId == drSet.DeliveryReceiptId) == null ? 0m :
                            idsOfDrsOfSelectedPeriodFromCv.FirstOrDefault(dr => dr.DeliveryReceiptId == drSet.DeliveryReceiptId)!.AmountPaid
                    })
                    .GroupBy(dr => new MonthYear(
                        dr.DeliveryReceipt.DeliveredDate!.Value.Year,
                        dr.DeliveryReceipt.DeliveredDate!.Value.Month
                    ));

                var drAndAmountPaidForPreviousPeriodFromCv = allDr
                    .Where(dr => idsOfDrsOfPreviousPeriodsFromCv.Select(drSet => drSet.DeliveryReceiptId).ToList().Contains(dr.DeliveryReceiptId) &&
                    dr.FreightAmount != 0m)
                    .Select(drSet => new DrWithAmountPaidViewModel
                    {
                        DeliveryReceipt = drSet,
                        AmountPaid = idsOfDrsOfPreviousPeriodsFromCv.FirstOrDefault(dr => dr.DeliveryReceiptId == drSet.DeliveryReceiptId) == null ? 0m :
                            idsOfDrsOfPreviousPeriodsFromCv.FirstOrDefault(dr => dr.DeliveryReceiptId == drSet.DeliveryReceiptId)!.AmountPaid
                    })
                    .GroupBy(dr => new MonthYear(
                        dr.DeliveryReceipt.DeliveredDate!.Value.Year,
                        dr.DeliveryReceipt.DeliveredDate!.Value.Month
                    ));

                var allDrGroupedByMonthYear = allDr
                    .GroupBy(dr => new MonthYear(
                        dr.DeliveredDate!.Value.Year,
                        dr.DeliveredDate!.Value.Month
                    ));

                var allPreviousDrGroupedByMonthYear = allDr
                    .Where(dr => dr.DeliveredDate!.Value < dateFrom)
                    .GroupBy(dr => new MonthYear(
                        dr.DeliveredDate!.Value.Year,
                        dr.DeliveredDate!.Value.Month
                    ))
                    .ToList();

                var allSelectedDrGroupedByMonthYear = allDr
                    .Where(dr => dr.DeliveredDate!.Value >= dateFrom)
                    .GroupBy(dr => new MonthYear(
                        dr.DeliveredDate!.Value.Year,
                        dr.DeliveredDate!.Value.Month
                    ))
                    .ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Hauler Payable");

                #region == Title ==

                var titleCells = worksheet.Cells["A1:B1"];
                titleCells.Merge = true;
                titleCells.Value = "HAULER PAYABLE REPORT";
                titleCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                #endregion == Title ==

                #region == Header Row ==

                titleCells = worksheet.Cells["A7:B7"];
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A7"].Value = "MONTH";
                worksheet.Cells["A7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["B7"].Value = "HAULER";
                worksheet.Cells["B7"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                titleCells = worksheet.Cells["A6:B6"];
                titleCells.Merge = true;
                titleCells.Value = "AP HAULING";
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                string[] headers = ["BEGINNING", "FREIGHT AMOUNTS", "PAYMENTS", "ENDING"];
                string[] subHeaders = ["VOLUME", "GROSS", "EWT", "NET AMOUNT"];
                var col = 4;

                foreach (var header in headers)
                {
                    foreach (var subheader in subHeaders)
                    {
                        worksheet.Cells[7, col].Value = subheader;
                        worksheet.Cells[7, col].Style.Font.Bold = true;
                        worksheet.Cells[7, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        worksheet.Cells[7, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col = col + 1;
                    }

                    titleCells = worksheet.Cells[6, col - 4, 6, col - 1];
                    titleCells.Merge = true;
                    titleCells.Value = header;
                    titleCells.Style.Font.Size = 13;
                    titleCells.Style.Font.Bold = true;
                    titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleCells.Style.Fill.BackgroundColor.SetColor(Color.Salmon);
                    titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    col = col + 1;
                }

                #endregion == Header Row ==

                var row = 8;
                IEnumerable<IGrouping<MonthYear, FilprideDeliveryReceipt>> loopingMainDrGroupedByMonthYear = null!;
                IEnumerable<IGrouping<MonthYear, DrWithAmountPaidViewModel>> loopingSecondDrGroupedByMonthYear = null!;
                IEnumerable<IGrouping<MonthYear, DrWithAmountPaidViewModel>> loopingThirdDrGroupedByMonthYear = null!;

                #region == Initialize Variables ==

                // subtotals per month/year
                var subtotalVolumeBeginning = 0m;
                var subtotalGrossBeginning = 0m;
                var subtotalEwtBeginning = 0m;
                var subtotalNetBeginning = 0m;

                var subtotalVolumePurchases = 0m;
                var subtotalGrossPurchases = 0m;
                var subtotalEwtPurchases = 0m;
                var subtotalNetPurchases = 0m;

                var subtotalVolumePayments = 0m;
                var subtotalGrossPayments = 0m;
                var subtotalEwtPayments = 0m;
                var subtotalNetPayments = 0m;

                var currentVolumeEnding = 0m;
                var currentGrossEnding = 0m;
                var currentEwtEnding = 0m;
                var currentNetEnding = 0m;

                var grandTotalVolumeBeginning = 0m;
                var grandTotalGrossBeginning = 0m;
                var grandTotalEwtBeginning = 0m;
                var grandTotalNetBeginning = 0m;

                var grandTotalVolumePurchases = 0m;
                var grandTotalGrossPurchases = 0m;
                var grandTotalEwtPurchases = 0m;
                var grandTotalNetPurchases = 0m;

                var grandTotalVolumePayments = 0m;
                var grandTotalGrossPayments = 0m;
                var grandTotalEwtPayments = 0m;
                var grandTotalNetPayments = 0m;

                var grandTotalVolumeEnding = 0m;
                var grandTotalGrossEnding = 0m;
                var grandTotalEwtEnding = 0m;
                var grandTotalNetEnding = 0m;

                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                #endregion == Initialize Variables ==

                // DO NOT CHANGE loop for month year
                foreach (var allDrsSameMonthYear in allDrGroupedByMonthYear)
                {
                    // reset placing per category

                    // get current group of month-year drs
                    // group the drs by hauler
                    var sameMonthYearGroupedByHauler = allDrsSameMonthYear.GroupBy(rr => rr.Hauler!.SupplierName)
                        .ToList();

                    // MONTH YEAR LABEL
                    worksheet.Cells[row, 1].Value = (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(sameMonthYearGroupedByHauler.FirstOrDefault()?.FirstOrDefault()?.DeliveredDate!.Value.Month ?? 0))
                                                    + " " +
                                                    (sameMonthYearGroupedByHauler.FirstOrDefault()?.FirstOrDefault()?.DeliveredDate!.Value.Year.ToString() ?? " ");
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    row++;

                    // LOOP BY HAULER
                    foreach (var sameMonthYearSameHauler in sameMonthYearGroupedByHauler)
                    {
                        // NAME OF HAULER
                        var supplierName = sameMonthYearSameHauler.FirstOrDefault()?.Hauler!.SupplierName ?? "";
                        var isSupplierVatable = sameMonthYearSameHauler.First().Hauler!.VatType == SD.VatType_Vatable;
                        var isSupplierTaxable = sameMonthYearSameHauler.First().Hauler!.TaxType == SD.TaxType_WithTax;
                        worksheet.Cells[row, 2].Value = supplierName;
                        var columnName = string.Empty;
                        var isPayment = false;
                        var isEnding = false;

                        // loop by month-year and hauler
                        for (var i = 1; i != 5; i++)
                        {
                            // determines if the loop is beginning, current, payment, or ending
                            switch (i)
                            {
                                // beginning
                                case 1:
                                    loopingMainDrGroupedByMonthYear = allPreviousDrGroupedByMonthYear;
                                    loopingSecondDrGroupedByMonthYear = drAndAmountPaidForPreviousPeriodFromCv;
                                    loopingThirdDrGroupedByMonthYear = drAndAmountPaidForSelectedPeriodFromCv;
                                    columnName = "beginning";
                                    break;
                                // current
                                case 2:
                                    loopingMainDrGroupedByMonthYear = allSelectedDrGroupedByMonthYear;
                                    loopingSecondDrGroupedByMonthYear = null!;
                                    loopingThirdDrGroupedByMonthYear = null!;
                                    columnName = "purchases";
                                    break;
                                // payment
                                case 3:
                                    loopingMainDrGroupedByMonthYear = allDrGroupedByMonthYear;
                                    loopingSecondDrGroupedByMonthYear = drAndAmountPaidForSelectedPeriodFromCv;
                                    loopingThirdDrGroupedByMonthYear = null!;
                                    columnName = "payments";
                                    break;
                                // ending
                                case 4:
                                    isEnding = true;
                                    break;
                            }

                            if (isPayment)
                            {
                                switch (columnName)
                                {
                                    case "beginning":
                                        loopingSecondDrGroupedByMonthYear = drAndAmountPaidForPreviousPeriodFromCv;
                                        break;

                                    case "purchases":
                                        loopingSecondDrGroupedByMonthYear = null!;
                                        break;
                                }
                            }

                            if (loopingMainDrGroupedByMonthYear != null)
                            {
                                foreach (var sameMonthYear in loopingMainDrGroupedByMonthYear)
                                {
                                    // this process finds the dr that has the same month/year for current month/year section
                                    if (sameMonthYear.FirstOrDefault()?.DeliveredDate!.Value.Month != allDrsSameMonthYear.FirstOrDefault()?.DeliveredDate!.Value.Month ||
                                        sameMonthYear.FirstOrDefault()?.DeliveredDate!.Value.Year != allDrsSameMonthYear.FirstOrDefault()?.DeliveredDate!.Value.Year)
                                    {
                                        continue;
                                    }

                                    IEnumerable<DrWithAmountPaidViewModel>? secondLoopSameMonthYearSameHauler = null;
                                    IGrouping<MonthYear, DrWithAmountPaidViewModel>? secondLoopSameMonthYear = null!;

                                    // GET DR SET WITH SAME MONTH YEAR + HAULER
                                    var sameHaulerSameMonthYear = sameMonthYear
                                        .Where(rr => rr.Hauler!.SupplierName == sameMonthYearSameHauler.FirstOrDefault()?.Hauler!.SupplierName);

                                    var volume = 0m;
                                    var gross = 0m;
                                    var netOfVat = 0m;
                                    var ewtPercentage = 0m;
                                    var ewt = 0m;
                                    var net = 0m;
                                    var totalAmount = 0m;
                                    var totalVolume = 0m;
                                    decimal sumOfAmountPaid = 0m;
                                    decimal sumOfVolumePaid = 0m;

                                    // PROCESS DEPENDING ON CATEGORY
                                    switch (i)
                                    {
                                        // BEGINNING
                                        case 1:
                                            // CONTAINS PREVIOUS PAID
                                            secondLoopSameMonthYear = loopingSecondDrGroupedByMonthYear
                                                .FirstOrDefault(secondLoop => secondLoop.Key == sameMonthYear.Key);

                                            // GET PREVIOUS PAID WITH SAME HAULER
                                            if (secondLoopSameMonthYear != null)
                                            {
                                                secondLoopSameMonthYearSameHauler = secondLoopSameMonthYear
                                                    .Where(rr => rr.DeliveryReceipt.Hauler!.SupplierName == sameMonthYearSameHauler
                                                    .FirstOrDefault()?.Hauler!.SupplierName)
                                                    .ToList();

                                                if (secondLoopSameMonthYearSameHauler.Count() != 0)
                                                {
                                                    sumOfAmountPaid =
                                                        secondLoopSameMonthYearSameHauler.Sum(dr => dr.AmountPaid);

                                                    sumOfVolumePaid =
                                                        secondLoopSameMonthYearSameHauler.Sum(dr => dr.DeliveryReceipt.Quantity);
                                                }
                                            }

                                            totalAmount = sameHaulerSameMonthYear
                                                .Sum(dr => dr.FreightAmount);

                                            totalVolume = sameHaulerSameMonthYear
                                                  .Sum(dr => dr.Quantity);

                                            gross = totalAmount - sumOfAmountPaid;

                                            volume = totalVolume - sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewtPercentage = sameMonthYear.Average(dr => dr.Hauler!.WithholdingTaxPercent ?? 0m);

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;

                                        // CURRENT
                                        case 2:

                                            totalAmount = sameHaulerSameMonthYear
                                                .Sum(dr => dr.FreightAmount);

                                            totalVolume = sameHaulerSameMonthYear
                                                .Sum(dr => dr.Quantity);

                                            gross = totalAmount - sumOfAmountPaid;

                                            volume = totalVolume - sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewtPercentage = sameMonthYear.Average(dr => dr.Hauler!.WithholdingTaxPercent ?? 0m);

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;

                                        // PAYMENT
                                        case 3:
                                            // CONTAINS SELECTED PAID
                                            secondLoopSameMonthYear = loopingSecondDrGroupedByMonthYear
                                                .FirstOrDefault(secondLoop => secondLoop.Key == sameMonthYear.Key);

                                            // GET PAID WITH SAME SUPPLIER
                                            if (secondLoopSameMonthYear != null)
                                            {
                                                secondLoopSameMonthYearSameHauler = secondLoopSameMonthYear
                                                    .Where(rr => rr.DeliveryReceipt.Hauler!.SupplierName == sameMonthYearSameHauler.FirstOrDefault()?.Hauler!.SupplierName);

                                                sumOfAmountPaid =
                                                    secondLoopSameMonthYearSameHauler.Sum(dr => dr.AmountPaid);

                                                sumOfVolumePaid =
                                                    secondLoopSameMonthYearSameHauler.Sum(dr => dr.DeliveryReceipt.Quantity);

                                                ewtPercentage = secondLoopSameMonthYear.Average(dr => dr.DeliveryReceipt.Hauler!.WithholdingTaxPercent ?? 0m);
                                            }

                                            if (secondLoopSameMonthYearSameHauler == null)
                                            {
                                                continue;
                                            }

                                            gross = sumOfAmountPaid;

                                            volume = sumOfVolumePaid;

                                            netOfVat = isSupplierVatable ? NetOfVatOrZero(gross) : gross;

                                            ewt = isSupplierTaxable ? EwtAmountOrZero(netOfVat, ewtPercentage) : 0m;

                                            net = gross - ewt;

                                            break;
                                    }

                                    // write in the category
                                    worksheet.Cells[row, (i * 5) - 1].Value = volume;
                                    worksheet.Cells[row, i * 5].Value = gross;
                                    worksheet.Cells[row, (i * 5) + 1].Value = ewt;
                                    worksheet.Cells[row, (i * 5) + 2].Value = net;
                                    worksheet.Cells[row, (i * 5) - 1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, i * 5].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, (i * 5) + 1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, (i * 5) + 2].Style.Numberformat.Format = currencyFormat;

                                    // decide what to do to subtotals depending on category (beg, current, payment)
                                    switch (i)
                                    {
                                        // beginning
                                        case 1:
                                            subtotalVolumeBeginning += volume;
                                            subtotalGrossBeginning += gross;
                                            subtotalEwtBeginning += ewt;
                                            subtotalNetBeginning += net;
                                            currentVolumeEnding += volume;
                                            currentGrossEnding += gross;
                                            currentEwtEnding += ewt;
                                            currentNetEnding += net;
                                            break;
                                        // current
                                        case 2:
                                            subtotalVolumePurchases += volume;
                                            subtotalGrossPurchases += gross;
                                            subtotalEwtPurchases += ewt;
                                            subtotalNetPurchases += net;
                                            currentVolumeEnding += volume;
                                            currentGrossEnding += gross;
                                            currentEwtEnding += ewt;
                                            currentNetEnding += net;
                                            break;
                                        // payment
                                        case 3:
                                            subtotalVolumePayments += volume;
                                            subtotalGrossPayments += gross;
                                            subtotalEwtPayments += ewt;
                                            subtotalNetPayments += net;
                                            currentVolumeEnding -= volume;
                                            currentGrossEnding -= gross;
                                            currentEwtEnding -= ewt;
                                            currentNetEnding -= net;
                                            break;
                                    }
                                }
                            }

                            if (isEnding)
                            {
                                worksheet.Cells[row, 19].Value = currentVolumeEnding;
                                worksheet.Cells[row, 20].Value = currentGrossEnding;
                                worksheet.Cells[row, 21].Value = currentEwtEnding;
                                worksheet.Cells[row, 22].Value = currentNetEnding;
                                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat;
                                worksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat;
                                currentVolumeEnding = 0m;
                                currentGrossEnding = 0m;
                                currentEwtEnding = 0m;
                                currentNetEnding = 0m;
                            }

                            isPayment = false;
                        }
                        // after the four columns(beginning, current, payment, ending), next hauler
                        row++;
                    }

                    #region == Subtotal Inputting ==

                    // after all hauler, input subtotals if not zero
                    if (subtotalGrossBeginning != 0m)
                    {
                        worksheet.Cells[row, 4].Value = subtotalVolumeBeginning;
                        worksheet.Cells[row, 5].Value = subtotalGrossBeginning;
                        worksheet.Cells[row, 6].Value = subtotalEwtBeginning;
                        worksheet.Cells[row, 7].Value = subtotalNetBeginning;

                        using var range = worksheet.Cells[row, 4, row, 7];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }
                    if (subtotalGrossPurchases != 0m)
                    {
                        worksheet.Cells[row, 9].Value = subtotalVolumePurchases;
                        worksheet.Cells[row, 10].Value = subtotalGrossPurchases;
                        worksheet.Cells[row, 11].Value = subtotalEwtPurchases;
                        worksheet.Cells[row, 12].Value = subtotalNetPurchases;

                        using var range = worksheet.Cells[row, 9, row, 12];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }
                    if (subtotalGrossPayments != 0m)
                    {
                        worksheet.Cells[row, 14].Value = subtotalVolumePayments;
                        worksheet.Cells[row, 15].Value = subtotalGrossPayments;
                        worksheet.Cells[row, 16].Value = subtotalEwtPayments;
                        worksheet.Cells[row, 17].Value = subtotalNetPayments;

                        using var range = worksheet.Cells[row, 14, row, 17];
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }

                    #endregion == Subtotal Inputting ==

                    #region == Ending Subtotal and Grand Total Processes ==

                    // input subtotal of ending
                    var subtotalVolumeEnding = RoundToFour(subtotalVolumeBeginning + subtotalVolumePurchases - subtotalVolumePayments);
                    var subtotalGrossEnding = RoundToFour(subtotalGrossBeginning + subtotalGrossPurchases - subtotalGrossPayments);
                    var subtotalEwtEnding = RoundToFour(subtotalEwtBeginning + subtotalEwtPurchases - subtotalEwtPayments);
                    var subtotalNetEnding = RoundToFour(subtotalNetBeginning + subtotalNetPurchases - subtotalNetPayments);

                    worksheet.Cells[row, 19].Value = subtotalVolumeEnding;
                    worksheet.Cells[row, 20].Value = subtotalGrossEnding;
                    worksheet.Cells[row, 21].Value = subtotalEwtEnding;
                    worksheet.Cells[row, 22].Value = subtotalNetEnding;

                    using (var range = worksheet.Cells[row, 19, row, 22])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Numberformat.Format = currencyFormat;
                    }

                    // after inputting all subtotals, next row
                    row++;

                    // after inputting all subtotals, add subtotals to grand total
                    grandTotalVolumeBeginning += subtotalVolumeBeginning;
                    grandTotalGrossBeginning += subtotalGrossBeginning;
                    grandTotalEwtBeginning += subtotalEwtBeginning;
                    grandTotalNetBeginning += subtotalNetBeginning;

                    grandTotalVolumePurchases += subtotalVolumePurchases;
                    grandTotalGrossPurchases += subtotalGrossPurchases;
                    grandTotalEwtPurchases += subtotalEwtPurchases;
                    grandTotalNetPurchases += subtotalNetPurchases;

                    grandTotalVolumePayments += subtotalVolumePayments;
                    grandTotalGrossPayments += subtotalGrossPayments;
                    grandTotalEwtPayments += subtotalEwtPayments;
                    grandTotalNetPayments += subtotalNetPayments;

                    grandTotalVolumeEnding += subtotalVolumeEnding;
                    grandTotalGrossEnding += subtotalGrossEnding;
                    grandTotalEwtEnding += subtotalEwtEnding;
                    grandTotalNetEnding += subtotalNetEnding;

                    // reset subtotals
                    subtotalVolumePurchases = 0m;
                    subtotalGrossPurchases = 0m;
                    subtotalEwtPurchases = 0m;
                    subtotalNetPurchases = 0m;
                    subtotalVolumeBeginning = 0m;
                    subtotalGrossBeginning = 0m;
                    subtotalEwtBeginning = 0m;
                    subtotalNetBeginning = 0m;
                    currentVolumeEnding = 0m;
                    currentGrossEnding = 0m;
                    currentEwtEnding = 0m;
                    currentNetEnding = 0m;
                    subtotalVolumePayments = 0m;
                    subtotalGrossPayments = 0m;
                    subtotalEwtPayments = 0m;
                    subtotalNetPayments = 0m;

                    #endregion == Ending Subtotal and Grand Total Processes ==
                }

                row++;

                #region == Grand Total Inputting ==

                worksheet.Cells[row, 2].Value = "GRAND TOTAL:";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 4].Value = grandTotalVolumeBeginning;
                worksheet.Cells[row, 5].Value = grandTotalGrossBeginning;
                worksheet.Cells[row, 6].Value = grandTotalEwtBeginning;
                worksheet.Cells[row, 7].Value = grandTotalNetBeginning;
                worksheet.Cells[row, 9].Value = grandTotalVolumePurchases;
                worksheet.Cells[row, 10].Value = grandTotalGrossPurchases;
                worksheet.Cells[row, 11].Value = grandTotalEwtPurchases;
                worksheet.Cells[row, 12].Value = grandTotalNetPurchases;
                worksheet.Cells[row, 14].Value = grandTotalVolumePayments;
                worksheet.Cells[row, 15].Value = grandTotalGrossPayments;
                worksheet.Cells[row, 16].Value = grandTotalEwtPayments;
                worksheet.Cells[row, 17].Value = grandTotalNetPayments;
                worksheet.Cells[row, 19].Value = grandTotalVolumeEnding;
                worksheet.Cells[row, 20].Value = grandTotalGrossEnding;
                worksheet.Cells[row, 21].Value = grandTotalEwtEnding;
                worksheet.Cells[row, 22].Value = grandTotalNetEnding;

                using (var range = worksheet.Cells[row, 4, row, 22])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Numberformat.Format = currencyFormat;
                }

                using (var range = worksheet.Cells[row, 1, row, 22])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                #endregion == Grand Total Inputting ==

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(3).Width = 1;
                worksheet.Column(8).Width = 1;
                worksheet.Column(13).Width = 1;
                worksheet.Column(18).Width = 1;
                worksheet.View.FreezePanes(8, 2);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate hauler payable report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Hauler_Payable_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate hauler payable report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(HaulerPayableReport));
            }
        }

        #endregion -- Generate Hauler Payable Report Excel File --

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderListBySupplier(int supplierId, CancellationToken cancellationToken)
        {
            var purchaseOrderList = await _dbContext.FilpridePurchaseOrders
                .Where(po => po.SupplierId == supplierId)
                .OrderBy(po => po.PurchaseOrderNo)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return Json(purchaseOrderList);
        }

        [HttpGet]
        public IActionResult JournalVoucherReport()
        {
            return View();
        }

        #region -- Generated Journal Voucher Report as Excel File --

        [HttpPost]
        public async Task<IActionResult> GenerateJournalVoucherExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(JournalVoucherReport));
            }

            try
            {
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                // Fetch journal voucher report data
                var journalVoucherReport = await _unitOfWork.FilprideReport
                    .GetJournalVoucherReport(model.DateFrom, model.DateTo, companyClaims, statusFilter, cancellationToken);

                if (journalVoucherReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(JournalVoucherReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("JournalVoucherReport");

                // Set report title
                var reportTitle = worksheet.Cells["A1:B1"];
                reportTitle.Merge = true;
                reportTitle.Value = "JOURNAL VOUCHER REPORT";
                reportTitle.Style.Font.Size = 13;

                // Set filter information
                worksheet.Cells["A2"].Value = "Date Range: ";
                worksheet.Cells["B2"].Value = $"{model.DateFrom} - {model.DateTo}";
                worksheet.Cells["A3"].Value = "Generated By: ";
                worksheet.Cells["B3"].Value = GetUserFullName();
                worksheet.Cells["A4"].Value = "Company: ";
                worksheet.Cells["B4"].Value = await GetCompanyClaimAsync();
                worksheet.Cells["A5"].Value = "Status Filter: ";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["A6"].Value = "Date and Time Generated:";
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                // Set column headers (Row 7)
                int headerRow = 7;

                worksheet.Cells[headerRow, 1].Value = "DATE";
                worksheet.Cells[headerRow, 2].Value = "JV #";
                worksheet.Cells[headerRow, 3].Value = "PARTICULARS";
                worksheet.Cells[headerRow, 4].Value = "DEBIT";
                worksheet.Cells[headerRow, 5].Value = "CREDIT";
                worksheet.Cells[headerRow, 6].Value = "ACCOUNT NUMBER";
                worksheet.Cells[headerRow, 7].Value = "ACCOUNT NAME";
                worksheet.Cells[headerRow, 8].Value = "JV STATUS";
                worksheet.Cells[headerRow, 9].Value = "JV REASON";
                worksheet.Cells[headerRow, 10].Value = "CHECK NO";
                worksheet.Cells[headerRow, 11].Value = "CV #";
                worksheet.Cells[headerRow, 12].Value = "PAYEE";
                worksheet.Cells[headerRow, 13].Value = "PREPARED BY";

                int lastColIndex = 13;
                if (showVoidCancelColumns)
                {
                    worksheet.Cells[headerRow, 14].Value = "VOIDED BY";
                    worksheet.Cells[headerRow, 15].Value = "VOIDED DATE";
                    lastColIndex = 15;
                }

                // Align all cells left
                worksheet.Cells[worksheet.Dimension.Address].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                // Apply border to left, right of header
                using (var range = worksheet.Cells[headerRow, 1, headerRow, lastColIndex])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Populate the data rows
                int row = headerRow + 1;
                string currencyFormat = "#,##0.00";

                foreach (var detail in journalVoucherReport)
                {
                    worksheet.Cells[row, 1].Value = detail.JournalVoucherHeader!.Date;
                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";
                    worksheet.Cells[row, 2].Value = detail.JournalVoucherHeader.JournalVoucherHeaderNo;
                    worksheet.Cells[row, 3].Value = detail.JournalVoucherHeader.Particulars;
                    worksheet.Cells[row, 4].Value = detail.Debit;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 5].Value = detail.Credit;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 6].Value = detail.AccountNo;
                    worksheet.Cells[row, 7].Value = detail.AccountName;
                    worksheet.Cells[row, 8].Value = detail.JournalVoucherHeader.Status;
                    worksheet.Cells[row, 9].Value = detail.JournalVoucherHeader.JVReason;
                    worksheet.Cells[row, 10].Value = detail.JournalVoucherHeader.CheckVoucherHeader?.CheckNo;
                    worksheet.Cells[row, 11].Value = detail.JournalVoucherHeader.CheckVoucherHeader?.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 12].Value = detail.JournalVoucherHeader.CheckVoucherHeader?.Payee;
                    worksheet.Cells[row, 13].Value = detail.JournalVoucherHeader.CreatedBy;

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, 14].Value = detail.JournalVoucherHeader.VoidedBy;
                        worksheet.Cells[row, 15].Value = detail.JournalVoucherHeader.VoidedDate;
                        worksheet.Cells[row, 15].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    row++;
                }

                // Append the total of credit and debit
                worksheet.Cells[row, 3].Value = "TOTAL:";
                worksheet.Cells[row, 4].Value = journalVoucherReport.Sum(jv => jv.Debit);
                worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 5].Value = journalVoucherReport.Sum(jv => jv.Credit);
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;

                // Apply the specified styling to the total row
                using (var range = worksheet.Cells[row, 1, row, lastColIndex])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.Column(3).Width = 60;
                worksheet.Column(9).Width = 30;

                // Freeze panes at particulars and
                worksheet.View.FreezePanes(headerRow + 1, 3);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(
                    GetUserFullName(),
                    "Generate journal voucher report excel file",
                    "Journal Voucher Report",
                    companyClaims
                );
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"JournalVoucher_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyMMddHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate journal voucher report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(JournalVoucherReport));
            }
        }

        #endregion -- Generated Journal Voucher Report as Excel File --

        [HttpGet]
        public IActionResult TradeSupplierFuelReport()
        {
            return View();
        }

        #region -- Generate Trade Supplier Fuel Report as Excel File --

        public async Task<IActionResult> GenerateTradeSupplierFuelReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeSupplierFuelReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var cvTradePayments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "RR" &&
                                x.CV.Company == companyClaims &&
                                x.CV.Date >= dateFrom &&
                                x.CV.Date <= dateTo &&
                                (statusFilter == "ValidOnly"
                                    ? x.CV.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.CV.VoidedBy != null))
                    .Include(x => x.CV)
                    .ToListAsync(cancellationToken);

                var receivingReportId = cvTradePayments.Select(x => x.DocumentId).Distinct().ToList();
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Where(x => receivingReportId.Contains(x.ReceivingReportId) &&
                                (statusFilter == "ValidOnly"
                                    ? x.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.VoidedBy != null))
                    .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                    .ToDictionaryAsync(x => x.ReceivingReportId, cancellationToken);

                if (cvTradePayments.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeSupplierFuelReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TradeSupplierFuelReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE SUPPLIER FUEL REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom.ToString("MMM dd, yyyy")} - {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "SI No."; col++;
                worksheet.Cells[row, col].Value = "Suppliers PO No."; col++;
                worksheet.Cells[row, col].Value = "RR No."; col++;
                worksheet.Cells[row, col].Value = "RR Date"; col++;
                worksheet.Cells[row, col].Value = "Gross of Vat"; col++;
                worksheet.Cells[row, col].Value = "Net of Vat"; col++;
                worksheet.Cells[row, col].Value = "EWT"; col++;
                worksheet.Cells[row, col].Value = "Amount Paid"; col++;
                worksheet.Cells[row, col].Value = "CV No."; col++;
                worksheet.Cells[row, col].Value = "CV DATE"; col++;
                worksheet.Cells[row, col].Value = "CHECK #"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;

                if (showVoidCancelColumns)
                {
                    worksheet.Cells[row, col].Value = "VOIDED BY"; col++;
                    worksheet.Cells[row, col].Value = "VOIDED DATE";
                }

                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var currencyFormat = "#,##0.00";

                foreach (var record in cvTradePayments)
                {
                    receivingReports.TryGetValue(record.DocumentId, out var rr);
                    if (rr == null)
                    {
                        continue;
                    }
                    col = 1;

                    var netOfVatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                        ? NetOfVatOrZero(rr.Amount)
                        : rr.Amount;

                    var vatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                        ? VatAmountOrZero(netOfVatAmount)
                        : 0m;

                    var taxPercent = rr.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? rr.TaxPercentage;

                    var withHoldingTaxAmount = rr.PurchaseOrder!.Supplier!.TaxType == SD.TaxType_WithTax
                        ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, taxPercent)
                        : 0m;

                    worksheet.Cells[row, col].Value = rr.SupplierInvoiceNumber; col++;
                    worksheet.Cells[row, col].Value = rr.PONo; col++;
                    worksheet.Cells[row, col].Value = rr.ReceivingReportNo; col++;
                    worksheet.Cells[row, col].Value = rr.Date;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                    col++;
                    worksheet.Cells[row, col].Value = rr.Amount;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                    col++;
                    worksheet.Cells[row, col].Value = netOfVatAmount;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                    col++;
                    worksheet.Cells[row, col].Value = withHoldingTaxAmount;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                    col++;
                    worksheet.Cells[row, col].Value = rr.AmountPaid;
                    worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                    col++;
                    worksheet.Cells[row, col].Value = record.CV.CheckVoucherHeaderNo; col++;
                    worksheet.Cells[row, col].Value = record.CV.Date;
                    worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                    col++;
                    worksheet.Cells[row, col].Value = record.CV.CheckNo; col++;
                    worksheet.Cells[row, col].Value = record.CV.Payee; col++;
                    worksheet.Cells[row, col].Value = record.CV.Particulars; col++;
                    worksheet.Cells[row, col].Value = record.CV.Type; col++;

                    if (showVoidCancelColumns)
                    {
                        worksheet.Cells[row, col].Value = rr.VoidedBy; col++;
                        worksheet.Cells[row, col].Value = rr.VoidedDate;
                        worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                    }

                    row++;
                }

                // int totalRow = row;
                // int lastDataCol = showVoidCancelColumns ? 16 : 14;
                // worksheet.Cells[totalRow, 5].Value = "TOTAL: ";
                // worksheet.Cells[totalRow, 6].Value = 1;
                // worksheet.Cells[totalRow, 7].Value = 2;
                // worksheet.Cells[totalRow, 8].Value = 3;
                // worksheet.Cells[totalRow, 9].Value = 4;
                //
                // using (var range = worksheet.Cells[totalRow, 1, totalRow, lastDataCol])
                // {
                //     range.Style.Font.Bold = true;
                //     range.Style.Border.Top.Style = ExcelBorderStyle.Double;
                //     range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                // }
                // using (var range = worksheet.Cells[totalRow, 11, totalRow, lastDataCol])
                // {
                //     range.Style.Numberformat.Format = currencyFormat;
                // }

                worksheet.Cells.AutoFitColumns();

                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Supplier Fuel report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Supplier_Fuel_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade supplier fuel report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradeSupplierFuelReport));
            }
        }

        #endregion


        [HttpGet]
        public IActionResult TradeSupplierCommissioneeReport()
        {
            return View();
        }

        #region -- Generate Trade Supplier Commissionee Report as Excel File --

        public async Task<IActionResult> GenerateTradeSupplierCommissioneeReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeSupplierCommissioneeReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var cvTradePayments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "DR" &&
                                x.CV.CvType == "Commission" &&
                                x.CV.Company == companyClaims &&
                                x.CV.Date >= dateFrom &&
                                x.CV.Date <= dateTo &&
                                (statusFilter == "ValidOnly"
                                    ? x.CV.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.CV.VoidedBy != null))
                    .Include(x => x.CV)
                    .ToListAsync(cancellationToken);

                var deliveryReceiptId = cvTradePayments.Select(x => x.DocumentId).Distinct().ToList();
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Where(x => x.DeliveryReceiptId.HasValue &&
                                deliveryReceiptId.Contains(x.DeliveryReceiptId.Value) &&
                                (statusFilter == "ValidOnly"
                                    ? x.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.VoidedBy != null))
                    .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                    .ToListAsync(cancellationToken);

                var receivingReportLookup = receivingReports
                    .GroupBy(x => x.DeliveryReceiptId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                if (cvTradePayments.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeSupplierCommissioneeReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TradeSupplierCommissioneeReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE SUPPLIER COMMISSIONEE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom.ToString("MMM dd, yyyy")} - {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "SI No."; col++;
                worksheet.Cells[row, col].Value = "Suppliers PO No."; col++;
                worksheet.Cells[row, col].Value = "RR No."; col++;
                worksheet.Cells[row, col].Value = "RR Date"; col++;
                worksheet.Cells[row, col].Value = "Gross of Vat"; col++;
                worksheet.Cells[row, col].Value = "Net of Vat"; col++;
                worksheet.Cells[row, col].Value = "EWT"; col++;
                worksheet.Cells[row, col].Value = "Amount Paid"; col++;
                worksheet.Cells[row, col].Value = "CV No."; col++;
                worksheet.Cells[row, col].Value = "CV DATE"; col++;
                worksheet.Cells[row, col].Value = "CHECK #"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;

                if (showVoidCancelColumns)
                {
                    worksheet.Cells[row, col].Value = "VOIDED BY"; col++;
                    worksheet.Cells[row, col].Value = "VOIDED DATE";
                }

                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var currencyFormat = "#,##0.00";

                foreach (var record in cvTradePayments)
                {

                    if (receivingReportLookup.TryGetValue(record.DocumentId, out var receivingReport))
                    {
                        foreach (var rr in receivingReport)
                        {
                            col = 1;

                            var netOfVatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                                ? NetOfVatOrZero(rr.Amount)
                                : rr.Amount;

                            var vatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                                ? VatAmountOrZero(netOfVatAmount)
                                : 0m;

                            var taxPercent = rr.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? rr.TaxPercentage;

                            var withHoldingTaxAmount = rr.PurchaseOrder!.Supplier!.TaxType == SD.TaxType_WithTax
                                ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, taxPercent)
                                : 0m;

                            worksheet.Cells[row, col].Value = rr.SupplierInvoiceNumber; col++;
                            worksheet.Cells[row, col].Value = rr.PONo; col++;
                            worksheet.Cells[row, col].Value = rr.ReceivingReportNo; col++;
                            worksheet.Cells[row, col].Value = rr.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = rr.Amount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = netOfVatAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = withHoldingTaxAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = rr.AmountPaid;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = record.CV.CheckVoucherHeaderNo; col++;
                            worksheet.Cells[row, col].Value = record.CV.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = record.CV.CheckNo; col++;
                            worksheet.Cells[row, col].Value = record.CV.Payee; col++;
                            worksheet.Cells[row, col].Value = record.CV.Particulars; col++;
                            worksheet.Cells[row, col].Value = record.CV.Type; col++;

                            if (showVoidCancelColumns)
                            {
                                worksheet.Cells[row, col].Value = rr.VoidedBy; col++;
                                worksheet.Cells[row, col].Value = rr.VoidedDate;
                                worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            }

                            row++;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();

                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Supplier Commissionee report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Supplier_Commissionee_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade supplier commissionee report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradeSupplierCommissioneeReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult TradeSupplierHaulerOrFreightReport()
        {
            return View();
        }

        #region -- Generate Trade Supplier Hauler or Freight Report as Excel File --

        public async Task<IActionResult> GenerateTradeSupplierHaulerOrFreightReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeSupplierHaulerOrFreightReport));
            }

            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var statusFilter = NormalizeStatusFilter(model.StatusFilter);

                var cvTradePayments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "DR" &&
                                x.CV.CvType == "Hauler" &&
                                x.CV.Company == companyClaims &&
                                x.CV.Date >= dateFrom &&
                                x.CV.Date <= dateTo &&
                                (statusFilter == "ValidOnly"
                                    ? x.CV.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.CV.VoidedBy != null))
                    .Include(x => x.CV)
                    .ToListAsync(cancellationToken);

                var deliveryReceiptId = cvTradePayments.Select(x => x.DocumentId).Distinct().ToList();
                var receivingReports = await _dbContext.FilprideReceivingReports
                    .Where(x => x.DeliveryReceiptId.HasValue &&
                                deliveryReceiptId.Contains(x.DeliveryReceiptId.Value) &&
                                (statusFilter == "ValidOnly"
                                    ? x.PostedBy != null
                                    : statusFilter != "InvalidOnly" || x.VoidedBy != null))
                    .Include(x => x.PurchaseOrder)
                    .ThenInclude(x => x!.Supplier)
                    .ToListAsync(cancellationToken);

                var receivingReportLookup = receivingReports
                    .GroupBy(x => x.DeliveryReceiptId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                if (cvTradePayments.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeSupplierHaulerOrFreightReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TradeSupplierHaulerOrFreightReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE SUPPLIER HAULER/FREIGHT REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Status Filter:";
                worksheet.Cells["A6"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom.ToString("MMM dd, yyyy")} - {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = GetStatusFilterLabel(statusFilter);
                worksheet.Cells["B6"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                // Determine if we need to show void/cancel columns
                bool showVoidCancelColumns = statusFilter != "ValidOnly";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "SI No."; col++;
                worksheet.Cells[row, col].Value = "Suppliers PO No."; col++;
                worksheet.Cells[row, col].Value = "RR No."; col++;
                worksheet.Cells[row, col].Value = "RR Date"; col++;
                worksheet.Cells[row, col].Value = "Gross of Vat"; col++;
                worksheet.Cells[row, col].Value = "Net of Vat"; col++;
                worksheet.Cells[row, col].Value = "EWT"; col++;
                worksheet.Cells[row, col].Value = "Amount Paid"; col++;
                worksheet.Cells[row, col].Value = "CV No."; col++;
                worksheet.Cells[row, col].Value = "CV DATE"; col++;
                worksheet.Cells[row, col].Value = "CHECK #"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;

                if (showVoidCancelColumns)
                {
                    worksheet.Cells[row, col].Value = "VOIDED BY"; col++;
                    worksheet.Cells[row, col].Value = "VOIDED DATE";
                }

                using (var range = worksheet.Cells[row, 1, row, col])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                row++;
                var currencyFormat = "#,##0.00";

                foreach (var record in cvTradePayments)
                {

                    if (receivingReportLookup.TryGetValue(record.DocumentId, out var receivingReport))
                    {
                        foreach (var rr in receivingReport)
                        {
                            col = 1;

                            var netOfVatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                                ? NetOfVatOrZero(rr.Amount)
                                : rr.Amount;

                            var vatAmount = rr.PurchaseOrder!.Supplier!.VatType == SD.VatType_Vatable
                                ? VatAmountOrZero(netOfVatAmount)
                                : 0m;

                            var taxPercent = rr.PurchaseOrder!.Supplier!.WithholdingTaxPercent ?? rr.TaxPercentage;

                            var withHoldingTaxAmount = rr.PurchaseOrder!.Supplier!.TaxType == SD.TaxType_WithTax
                                ? _unitOfWork.FilprideReceivingReport.ComputeEwtAmount(netOfVatAmount, taxPercent)
                                : 0m;

                            worksheet.Cells[row, col].Value = rr.SupplierInvoiceNumber; col++;
                            worksheet.Cells[row, col].Value = rr.PONo; col++;
                            worksheet.Cells[row, col].Value = rr.ReceivingReportNo; col++;
                            worksheet.Cells[row, col].Value = rr.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = rr.Amount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = netOfVatAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = withHoldingTaxAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = rr.AmountPaid;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = record.CV.CheckVoucherHeaderNo; col++;
                            worksheet.Cells[row, col].Value = record.CV.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = record.CV.CheckNo; col++;
                            worksheet.Cells[row, col].Value = record.CV.Payee; col++;
                            worksheet.Cells[row, col].Value = record.CV.Particulars; col++;
                            worksheet.Cells[row, col].Value = record.CV.Type; col++;

                            if (showVoidCancelColumns)
                            {
                                worksheet.Cells[row, col].Value = rr.VoidedBy; col++;
                                worksheet.Cells[row, col].Value = rr.VoidedDate;
                                worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            }

                            row++;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();

                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Supplier Hauler/Freight report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Supplier_Hauler_or_Freight_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade supplier hauler/freight report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradeSupplierHaulerOrFreightReport));
            }
        }

        #endregion
    }
}
