using System.Globalization;
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
using Microsoft.EntityFrameworkCore;
using IBS.Models;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Books;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class AccountsPayableReportController : Controller
    {
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
                var checkVoucherHeader = await _unitOfWork.FilprideReport.GetClearedDisbursementReport(model.DateFrom, model.DateTo, companyClaims);

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

                        #endregion

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
                                });

                            #endregion

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

                            #endregion

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

                            #endregion

                            #region Create Table Cell for Totals

                                table.Cell().ColumnSpan(8).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalAmt != 0 ? totalAmt < 0 ? $"({Math.Abs(totalAmt).ToString(SD.Two_Decimal_Format)})" : totalAmt.ToString(SD.Two_Decimal_Format) : null).SemiBold().FontColor(totalAmt < 0 ? Colors.Red.Medium : Colors.Black);

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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate cleared disbursement report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

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
                var extractedBy = _userManager.GetUserName(User)!;
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
                var row = 8;
                var currencyFormat = "#,##0.00";

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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate cleared disbursement report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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


        #endregion

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
                var purchaseOrder = await _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims);

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

                        #endregion

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

                            #endregion

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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate purchase order report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

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
                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();
                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var purchaseOrderReport = await _unitOfWork.FilprideReport
                    .GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken);

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
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";

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

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:J7"])
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

                    row++;
                }

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate purchase order report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

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
                var purchaseReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, dateSelectionType:model.DateSelectionType);

                if (purchaseReport.Count == 0)
                {
                    TempData["info"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                var document =  Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        #region -- Page setup

                        page.Size(PageSizes.Legal.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Times New Roman"));

                        #endregion

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

                        #endregion

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

                                #endregion

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

                                #endregion

                                #region -- Initialize Variable for Computation

                                    var totalVolume = 0m;
                                    var totalCostAmount = 0m;
                                    var totalVatAmount = 0m;
                                    var totalWhtAmount = 0m;
                                    var totalNetPurchases = 0m;

                                    #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in purchaseReport)
                                    {
                                        var volume = record.QuantityReceived;
                                        var costAmountGross = record.Amount;
                                        var costPerLiter = costAmountGross / volume;
                                        var costAmountNet = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                            ? costAmountGross / 1.12m
                                            : costAmountGross;
                                        var vatAmount = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                            ? costAmountNet * 0.12m
                                            : 0m;
                                        var taxAmount = record.PurchaseOrder!.VatType == SD.VatType_Vatable
                                            ? costAmountNet * 0.12m
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
                                        table.Cell().Border(0.5f).Padding(3).Text(record.PurchaseOrder?.ProductName);
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

                                #endregion

                                #region -- Initialize Variable for Computation of Totals

                                var totalCostPerLiter = totalCostAmount / totalVolume;

                                #endregion

                                #region -- Create Table Cell for Totals

                                    table.Cell().ColumnSpan(14).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVolume != 0 ? totalVolume < 0 ? $"({Math.Abs(totalVolume).ToString(SD.Two_Decimal_Format)})" : totalVolume.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCostPerLiter != 0 ? totalCostPerLiter < 0 ? $"({Math.Abs(totalCostPerLiter).ToString(SD.Four_Decimal_Format)})" : totalCostPerLiter.ToString(SD.Four_Decimal_Format) : null).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalCostAmount != 0 ? totalCostAmount < 0 ? $"({Math.Abs(totalCostAmount).ToString(SD.Two_Decimal_Format)})" : totalCostAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalVatAmount != 0 ? totalVatAmount < 0 ? $"({Math.Abs(totalVatAmount).ToString(SD.Two_Decimal_Format)})" : totalVatAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalWhtAmount != 0 ? totalWhtAmount < 0 ? $"({Math.Abs(totalWhtAmount).ToString(SD.Two_Decimal_Format)})" : totalWhtAmount.ToString(SD.Two_Decimal_Format) : null).SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalNetPurchases != 0 ? totalNetPurchases < 0 ? $"({Math.Abs(totalNetPurchases).ToString(SD.Two_Decimal_Format)})" : totalNetPurchases.ToString(SD.Two_Decimal_Format) : null).SemiBold();

                                #endregion

                                //Summary Table
                                col.Item().PaddingTop(50).Text("SUMMARY").Bold().FontSize(14);

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
                                                columns.ConstantColumn(5);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.ConstantColumn(5);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });

                                        #endregion

                                        #region -- Table Header

                                            content.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Biodiesel").AlignCenter().SemiBold();
                                                header.Cell();
                                                header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Econogas").AlignCenter().SemiBold();
                                                header.Cell();
                                                header.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Envirogas").AlignCenter().SemiBold();

                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Suppliers").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. CPL").SemiBold();
                                                header.Cell();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. CPL").SemiBold();
                                                header.Cell();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Volume").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Purchases N. VAT").SemiBold();
                                                header.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignCenter().Text("Ave. CPL").SemiBold();

                                            });

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                        var totalQuantityForBiodiesel = 0m;
                                        var totalPurchaseNetOfVatForBiodiesel = 0m;
                                        var totalQuantityForEconogas = 0m;
                                        var totalPurchaseNetOfVatForEconogas = 0m;
                                        var totalQuantityForEnvirogas = 0m;
                                        var totalPurchaseNetOfVatForEnvirogas  = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            var groupBySupplier = purchaseReport
                                                    .OrderBy(rr => rr.PurchaseOrder!.SupplierName)
                                                    .GroupBy(rr => rr.PurchaseOrder!.SupplierName);

                                            // for each supplier
                                            foreach (var record in groupBySupplier)
                                            {
                                                var list = purchaseReport.Where(s => s.PurchaseOrder!.SupplierName == record.Key).ToList();

                                                var isVatable = list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;

                                                #region Computation for Biodiesel

                                                var listForBiodiesel = list.Where(s => s.PurchaseOrder!.ProductName == "BIODIESEL").ToList();

                                                var biodieselQuantitySum = listForBiodiesel.Sum(s => s.QuantityReceived);
                                                var biodieselPurchaseNetOfVatSum = isVatable
                                                    ? listForBiodiesel.Sum(pr => pr.Amount / 1.12m)
                                                    : listForBiodiesel.Sum(pr => pr.Amount);
                                                var biodieselAverageSellingPrice = biodieselPurchaseNetOfVatSum != 0m || biodieselQuantitySum != 0m
                                                    ? biodieselPurchaseNetOfVatSum / biodieselQuantitySum
                                                    : 0m;

                                                #endregion

                                                #region Computation for Econogas

                                                var listForEconogas = list.Where(s => s.PurchaseOrder!.ProductName == "ECONOGAS").ToList();

                                                var econogasQuantitySum = listForEconogas.Sum(s => s.QuantityReceived);
                                                var econogasPurchaseNetOfVatSum = isVatable
                                                    ? listForEconogas.Sum(pr => pr.Amount / 1.12m)
                                                    :  listForEconogas.Sum(pr => pr.Amount);
                                                var econogasAverageSellingPrice = econogasPurchaseNetOfVatSum != 0m && econogasQuantitySum != 0m
                                                    ? econogasPurchaseNetOfVatSum / econogasQuantitySum
                                                    : 0m;

                                                #endregion

                                                #region Computation for Envirogas

                                                var listForEnvirogas = list.Where(s => s.PurchaseOrder!.ProductName == "ENVIROGAS").ToList();

                                                var envirogasQuantitySum = listForEnvirogas.Sum(s => s.QuantityReceived);
                                                var envirogasPurchaseNetOfVatSum = isVatable
                                                    ? listForEnvirogas.Sum(pr => pr.Amount / 1.12m)
                                                    : listForEnvirogas.Sum(pr => pr.Amount);
                                                var envirogasAverageSellingPrice = envirogasPurchaseNetOfVatSum != 0m && envirogasQuantitySum != 0m ? envirogasPurchaseNetOfVatSum / envirogasQuantitySum : 0m;

                                                #endregion

                                                content.Cell().Border(0.5f).Padding(3).Text(record.Key);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselQuantitySum != 0 ? biodieselQuantitySum < 0 ? $"({Math.Abs(biodieselQuantitySum).ToString(SD.Two_Decimal_Format)})" : biodieselQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselPurchaseNetOfVatSum != 0 ? biodieselPurchaseNetOfVatSum < 0 ? $"({Math.Abs(biodieselPurchaseNetOfVatSum).ToString(SD.Two_Decimal_Format)})" : biodieselPurchaseNetOfVatSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselPurchaseNetOfVatSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselAverageSellingPrice != 0 ? biodieselAverageSellingPrice < 0 ? $"({Math.Abs(biodieselAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : biodieselAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(biodieselAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell();
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasQuantitySum != 0 ? econogasQuantitySum < 0 ? $"({Math.Abs(econogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : econogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasPurchaseNetOfVatSum != 0 ? econogasPurchaseNetOfVatSum < 0 ? $"({Math.Abs(econogasPurchaseNetOfVatSum).ToString(SD.Two_Decimal_Format)})" : econogasPurchaseNetOfVatSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasPurchaseNetOfVatSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasAverageSellingPrice != 0 ? econogasAverageSellingPrice < 0 ? $"({Math.Abs(econogasAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : econogasAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(econogasAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell();
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasQuantitySum != 0 ? envirogasQuantitySum < 0 ? $"({Math.Abs(envirogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : envirogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasPurchaseNetOfVatSum != 0 ? envirogasPurchaseNetOfVatSum < 0 ? $"({Math.Abs(envirogasPurchaseNetOfVatSum).ToString(SD.Two_Decimal_Format)})" : envirogasPurchaseNetOfVatSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasPurchaseNetOfVatSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasAverageSellingPrice != 0 ? envirogasAverageSellingPrice < 0 ? $"({Math.Abs(envirogasAverageSellingPrice).ToString(SD.Four_Decimal_Format)})" : envirogasAverageSellingPrice.ToString(SD.Four_Decimal_Format) : null).FontColor(envirogasAverageSellingPrice < 0 ? Colors.Red.Medium : Colors.Black);

                                                totalQuantityForBiodiesel += biodieselQuantitySum;
                                                totalPurchaseNetOfVatForBiodiesel += biodieselPurchaseNetOfVatSum;
                                                totalQuantityForEconogas += econogasQuantitySum;
                                                totalPurchaseNetOfVatForEconogas += econogasPurchaseNetOfVatSum;
                                                totalQuantityForEnvirogas += envirogasQuantitySum;
                                                totalPurchaseNetOfVatForEnvirogas += envirogasPurchaseNetOfVatSum;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            var averageSellingPriceForBiodiesel = totalPurchaseNetOfVatForBiodiesel != 0 && totalQuantityForBiodiesel != 0 ? totalPurchaseNetOfVatForBiodiesel / totalQuantityForBiodiesel : 0m;
                                            var averageSellingPriceForEconogas = totalPurchaseNetOfVatForEconogas != 0 && totalQuantityForEconogas != 0 ? totalPurchaseNetOfVatForEconogas / totalQuantityForEconogas : 0m;
                                            var averageSellingPriceForEnvirogas = totalPurchaseNetOfVatForEnvirogas != 0 && totalQuantityForEnvirogas != 0 ? totalPurchaseNetOfVatForEnvirogas / totalQuantityForEnvirogas : 0m;

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:").SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForBiodiesel != 0 ? totalQuantityForBiodiesel < 0 ? $"({Math.Abs(totalQuantityForBiodiesel).ToString(SD.Two_Decimal_Format)})" : totalQuantityForBiodiesel.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalPurchaseNetOfVatForBiodiesel != 0 ? totalPurchaseNetOfVatForBiodiesel < 0 ? $"({Math.Abs(totalPurchaseNetOfVatForBiodiesel).ToString(SD.Two_Decimal_Format)})" : totalPurchaseNetOfVatForBiodiesel.ToString(SD.Two_Decimal_Format) : null).FontColor(totalPurchaseNetOfVatForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForBiodiesel != 0 ? averageSellingPriceForBiodiesel < 0 ? $"({Math.Abs(averageSellingPriceForBiodiesel).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForBiodiesel.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForBiodiesel < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEconogas != 0 ? totalQuantityForEconogas < 0 ? $"({Math.Abs(totalQuantityForEconogas).ToString(SD.Two_Decimal_Format)})" : totalQuantityForEconogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalPurchaseNetOfVatForEconogas != 0 ? totalPurchaseNetOfVatForEconogas < 0 ? $"({Math.Abs(totalPurchaseNetOfVatForEconogas).ToString(SD.Two_Decimal_Format)})" : totalPurchaseNetOfVatForEconogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalPurchaseNetOfVatForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForEconogas != 0 ? averageSellingPriceForEconogas < 0 ? $"({Math.Abs(averageSellingPriceForEconogas).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForEconogas.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForEconogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalQuantityForEnvirogas != 0 ? totalQuantityForEnvirogas < 0 ? $"({Math.Abs(totalQuantityForEnvirogas).ToString(SD.Two_Decimal_Format)})" : totalQuantityForEnvirogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalQuantityForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(totalPurchaseNetOfVatForEnvirogas != 0 ? totalPurchaseNetOfVatForEnvirogas < 0 ? $"({Math.Abs(totalPurchaseNetOfVatForEnvirogas).ToString(SD.Two_Decimal_Format)})" : totalPurchaseNetOfVatForEnvirogas.ToString(SD.Two_Decimal_Format) : null).FontColor(totalPurchaseNetOfVatForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(averageSellingPriceForEnvirogas != 0 ? averageSellingPriceForEnvirogas < 0 ? $"({Math.Abs(averageSellingPriceForEnvirogas).ToString(SD.Four_Decimal_Format)})" : averageSellingPriceForEnvirogas.ToString(SD.Four_Decimal_Format) : null).FontColor(averageSellingPriceForEnvirogas < 0 ? Colors.Red.Medium : Colors.Black).SemiBold();

                                        #endregion
                                    });

                                #endregion
                            });

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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate purchase report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

        #region -- Generate Purchase Report as Excel File --

        public async Task<IActionResult> GeneratePurchaseReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(PurchaseReport));
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

                // get rr data from chosen date
                var purchaseReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken: cancellationToken);

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

                #endregion

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
                    purchaseReportWorksheet.Cells["A3"].Value = "Extracted By:";
                    purchaseReportWorksheet.Cells["A4"].Value = "Company:";

                    purchaseReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                    purchaseReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                    purchaseReportWorksheet.Cells["B4"].Value = $"{companyClaims}";

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
                    purchaseReportWorksheet.Cells["V7"].Value = "VAT AMOUNT";
                    purchaseReportWorksheet.Cells["W7"].Value = "FREIGHT G.VAT";
                    purchaseReportWorksheet.Cells["X7"].Value = "WHT AMOUNT";
                    purchaseReportWorksheet.Cells["Y7"].Value = "HAULER'S NAME";
                    purchaseReportWorksheet.Cells["Z7"].Value = "PURCHASES N.VAT";
                    purchaseReportWorksheet.Cells["AA7"].Value = "FREIGHT N.VAT";
                    purchaseReportWorksheet.Cells["AB7"].Value = "FREIGHT AMT G.VAT";
                    purchaseReportWorksheet.Cells["AC7"].Value = "FREIGHT AMT N.VAT";
                    purchaseReportWorksheet.Cells["AD7"].Value = "COMMISSION";
                    purchaseReportWorksheet.Cells["AE7"].Value = "OTC COS#.";
                    purchaseReportWorksheet.Cells["AF7"].Value = "OTC DR#.";
                    purchaseReportWorksheet.Cells["AG7"].Value = "IS PO#";
                    purchaseReportWorksheet.Cells["AH7"].Value = "IS RR#";

                    #endregion

                    #region -- Apply styling to the header row --

                    using (var range = purchaseReportWorksheet.Cells["A7:AH7"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                    #endregion

                    // Populate the data rows
                    var row = 8; // starting row
                    var currencyFormat = "#,##0.0000"; // numbers format
                    var currencyFormat2 = "#,##0.00"; // numbers format

                    var atlNos = purchaseReport.Select(pr => pr.AuthorityToLoadNo).Distinct().ToList();
                    var atls = await _dbContext.FilprideAuthorityToLoads
                        .Where(x => atlNos.Contains(x.AuthorityToLoadNo))
                        .ToListAsync(cancellationToken);
                    var atlLookup = atls.ToDictionary(x => x.AuthorityToLoadNo);

                    #region -- Populate data rows --

                    foreach (var pr in purchaseReport)
                    {
                        #region -- Variables and Formulas --

                        var isSupplierVatable = pr.PurchaseOrder!.VatType == SD.VatType_Vatable;
                        var isSupplierTaxable = pr.PurchaseOrder!.TaxType == SD.TaxType_WithTax;
                        var isHaulerVatable = pr.DeliveryReceipt!.HaulerVatType == SD.VatType_Vatable;

                        // calculate values, put in variables to be displayed per cell
                        var volume = pr.QuantityReceived; // volume
                        var costAmount = pr.Amount; // purchase total gross
                        var netPurchases = isSupplierVatable
                            ? _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(costAmount)
                            : costAmount; // purchase total net
                        var freight = pr.DeliveryReceipt?.Freight ?? 0m; // freight g vat
                        var netFreight = isHaulerVatable && freight != 0m
                            ? _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(freight)
                            : freight; // freight n vat
                        var freightAmount = freight * volume; // purchase total net
                        var freightAmountNet = netFreight * volume; // purchase total net
                        var vatAmount = isSupplierVatable
                            ? _unitOfWork.FilpridePurchaseOrder.ComputeVatAmount(netPurchases)
                            : 0m; // vat total
                        var whtAmount = isSupplierTaxable
                            ? _unitOfWork.FilpridePurchaseOrder.ComputeEwtAmount(netPurchases, 0.01m)
                            : 0m; // wht total
                        var costPerLiter = costAmount / volume; // sale price per liter
                        var commission = ((pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m) * volume);

                        atlLookup.TryGetValue(pr.AuthorityToLoadNo!, out var atl);

                        #endregion

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
                        purchaseReportWorksheet.Cells[row, 12].Value = atl?.UppiAtlNo; // Supplier ATL #
                        purchaseReportWorksheet.Cells[row, 13].Value = pr.SupplierInvoiceNumber; // Supplier's Sales Invoice
                        purchaseReportWorksheet.Cells[row, 14].Value = pr.SupplierInvoiceDate; // Supplier's Sales Invoice
                        purchaseReportWorksheet.Cells[row, 15].Value = pr.SupplierDrNo; // Supplier's DR
                        purchaseReportWorksheet.Cells[row, 16].Value = pr.WithdrawalCertificate; // Supplier's WC
                        purchaseReportWorksheet.Cells[row, 17].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerName; // Customer Name
                        purchaseReportWorksheet.Cells[row, 18].Value = pr.PurchaseOrder?.ProductName; // Product
                        purchaseReportWorksheet.Cells[row, 19].Value = volume; // Volume
                        purchaseReportWorksheet.Cells[row, 20].Value = costPerLiter; // Purchase price per liter
                        purchaseReportWorksheet.Cells[row, 21].Value = costAmount; // Purchase total gross
                        purchaseReportWorksheet.Cells[row, 22].Value = vatAmount; // Vat total
                        purchaseReportWorksheet.Cells[row, 23].Value = freight; // WHT total
                        purchaseReportWorksheet.Cells[row, 24].Value = whtAmount; // freight g vat
                        purchaseReportWorksheet.Cells[row, 25].Value = pr.DeliveryReceipt?.HaulerName; // Hauler's Name
                        purchaseReportWorksheet.Cells[row, 26].Value = netPurchases; // Purchase total net ======== move to third last
                        purchaseReportWorksheet.Cells[row, 27].Value = netFreight; // freight n vat ============
                        purchaseReportWorksheet.Cells[row, 28].Value = freightAmount; // freight amount n vat ============
                        purchaseReportWorksheet.Cells[row, 29].Value = freightAmountNet; // freight amount n vat ============
                        purchaseReportWorksheet.Cells[row, 30].Value = commission; // commission =========
                        purchaseReportWorksheet.Cells[row, 31].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo; // OTC COS =========
                        purchaseReportWorksheet.Cells[row, 32].Value = pr.DeliveryReceipt?.ManualDrNo; // OTC DR =========
                        purchaseReportWorksheet.Cells[row, 33].Value = pr.PurchaseOrder?.OldPoNo; // IS PO =========
                        purchaseReportWorksheet.Cells[row, 34].Value = pr.OldRRNo; // IS RR =========

                        #endregion -- Assign Values to Cells --

                        #region -- Add the values to total --

                        totalCostAmount += costAmount;
                        totalVatAmount += vatAmount;
                        totalWhtAmount += whtAmount;
                        totalNetPurchases += netPurchases;
                        totalCommission += commission;
                        totalFreight += freightAmount;
                        totalNetFreight += freightAmountNet;

                        #endregion -- Add the values to total and format number cells --

                        #region -- Add format number cells from Assign Values to Cells --

                        purchaseReportWorksheet.Cells[row, 1, row, 2].Style.Numberformat.Format = "MMM/dd/yyyy";
                        purchaseReportWorksheet.Cells[row, 14].Style.Numberformat.Format = "MMM/dd/yyyy";

                        #endregion -- Add format number cells from Assign Values to Cells --

                        row++;
                    }

                    #endregion -- Populate data rows --

                    #region -- Assign values of other totals and formatting of total cells --

                    var totalCostPerLiter = totalCostAmount / totalVolume;

                    purchaseReportWorksheet.Cells[row, 17].Value = "Total: ";
                    purchaseReportWorksheet.Cells[row, 19].Value = totalVolume;
                    purchaseReportWorksheet.Cells[row, 20].Value = totalCostPerLiter;
                    purchaseReportWorksheet.Cells[row, 21].Value = totalCostAmount;
                    purchaseReportWorksheet.Cells[row, 22].Value = totalVatAmount;
                    purchaseReportWorksheet.Cells[row, 23].Value = "";
                    purchaseReportWorksheet.Cells[row, 24].Value = totalWhtAmount;
                    purchaseReportWorksheet.Cells[row, 26].Value = totalNetPurchases;
                    purchaseReportWorksheet.Cells[row, 27].Value = "";
                    purchaseReportWorksheet.Cells[row, 28].Value = totalFreight;
                    purchaseReportWorksheet.Cells[row, 29].Value = totalNetFreight;
                    purchaseReportWorksheet.Cells[row, 30].Value = totalCommission;

                    purchaseReportWorksheet.Column(19).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(20).Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Column(21).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(22).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(23).Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Column(24).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(26).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(27).Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Column(28).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(29).Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Column(30).Style.Numberformat.Format = currencyFormat2;

                    #endregion -- Assign values of other totals and formatting of total cells --

                    // Apply style to subtotal rows
                    // color to whole row
                    using (var range = purchaseReportWorksheet.Cells[row, 1, row, 30])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                    }
                    // line to subtotal values
                    using (var range = purchaseReportWorksheet.Cells[row, 17, row, 30])
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

                    string[] productList = ["DIESEL", "ECONO", "ENVIRO"];

                    for (int i = 3, index = 0; i != 12; i += 3, index++)
                    {
                        mergedCells = purchaseReportWorksheet.Cells[row, i, row, i + 2];
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
                    purchaseReportWorksheet.Cells[row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                    purchaseReportWorksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    for (int i = 2; i != 11; i += 3)
                    {
                        purchaseReportWorksheet.Cells[row, i+1].Value = "VOLUME";
                        purchaseReportWorksheet.Cells[row, i+2].Value = "PURCHASES N.VAT";
                        purchaseReportWorksheet.Cells[row, i+3].Value = "AVE. CPL";
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        purchaseReportWorksheet.Cells[row, i+1, row, i+3].Style.Border.Top.Style = ExcelBorderStyle.Thin;

                        using var range = purchaseReportWorksheet.Cells[row, i + 1, row, i + 3];
                        range.Style.Font.Bold = true;
                        range.Style.Font.Italic = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
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
                        var startingColumn = 2;
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
                        foreach (var product in productBySupplier)
                        {
                            if (product.Any())
                            {
                                var grandTotalVolume = product
                                    .Sum(pr => pr.QuantityReceived); // volume
                                var grandTotalPurchaseNet = isVatable
                                    ? product.Sum(pr => pr.Amount  / 1.12m)
                                    : product.Sum(pr => pr.Amount); // Purchase Net Total

                                purchaseReportWorksheet.Cells[row, startingColumn + 1].Value = grandTotalVolume;
                                purchaseReportWorksheet.Cells[row, startingColumn + 2].Value = grandTotalPurchaseNet;
                                purchaseReportWorksheet.Cells[row, startingColumn + 3].Value = grandTotalVolume != 0m ? grandTotalPurchaseNet / grandTotalVolume : 0m; // Gross Margin Per Liter
                                purchaseReportWorksheet.Cells[row, startingColumn+1, row, startingColumn + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                purchaseReportWorksheet.Cells[row, startingColumn + 1].Style.Numberformat.Format = currencyFormat2;
                                purchaseReportWorksheet.Cells[row, startingColumn + 2].Style.Numberformat.Format = currencyFormat2;
                                purchaseReportWorksheet.Cells[row, startingColumn + 3].Style.Numberformat.Format = currencyFormat;
                            }

                            startingColumn += 3;
                        }

                        row++;
                    }

                    var endingSummaryTableRow = row - 1;

                    row++;

                    for (var i = 2; i != 11; i += 3)
                    {
                        purchaseReportWorksheet.Cells[row, i + 1].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, i + 1].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, i + 1].Address})";
                        purchaseReportWorksheet.Cells[row, i + 2].Formula = $"=SUM({purchaseReportWorksheet.Cells[startingSummaryTableRow, i + 2].Address}:{purchaseReportWorksheet.Cells[endingSummaryTableRow, i + 2].Address})";
                        purchaseReportWorksheet.Cells[row, i + 3].Formula = $"={purchaseReportWorksheet.Cells[row, i + 2].Address}/{purchaseReportWorksheet.Cells[row, i + 1].Address}";


                        purchaseReportWorksheet.Cells[row, i+1].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, i+2].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, i+3].Style.Numberformat.Format = currencyFormat;

                        mergedCells = purchaseReportWorksheet.Cells[row, i + 1, row, i + 3];
                        mergedCells.Style.Font.Bold = true;
                        mergedCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        mergedCells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                        mergedCells.Style.Font.Size = 11;
                        mergedCells.Style.Border.Top.Style = ExcelBorderStyle.Thin ;
                        mergedCells.Style.Border.Bottom.Style = ExcelBorderStyle.Double ;
                        mergedCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    var lastColumnForThickBorder = row;

                    var enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 2, lastColumnForThickBorder, 2];
                    enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 3, lastColumnForThickBorder, 5];
                    enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 6, lastColumnForThickBorder, 8];
                    enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 9, lastColumnForThickBorder, 11];
                    enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    #endregion == Summary Contents ==

                    #endregion -- Summary Rows --

                    // Auto-fit columns for better readability
                    purchaseReportWorksheet.Cells.AutoFitColumns();
                    purchaseReportWorksheet.View.FreezePanes(8, 1);
                    purchaseReportWorksheet.Column(5).Width = 24;



                #endregion -- Purchase Report Worksheet --

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate purchase report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

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
                var grossMarginReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, model.Customers, model.Commissionee);

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

                        #endregion

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

                        #endregion

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

                                #endregion

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

                                #endregion

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

                                #endregion

                                #region -- Loop to Show Records

                                    foreach (var record in grossMarginReport)
                                    {
                                        var isVatable = record.PurchaseOrder!.VatType == SD.VatType_Vatable;
                                        var volume = record.QuantityReceived;
                                        var costAmountGross = record.Amount;
                                        var purchasePerLiter = costAmountGross / volume;
                                        var salePricePerLiter = record.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m;
                                        var costAmountNet = isVatable
                                            ? _unitOfWork.FilpridePurchaseOrder.ComputeNetOfVat(costAmountGross)
                                            : costAmountGross;
                                        var costVatAmount = isVatable
                                            ? _unitOfWork.FilpridePurchaseOrder.ComputeVatAmount(costAmountNet)
                                            : 0m;
                                        var saleAmountGross = volume * salePricePerLiter;
                                        var gmPerLiter = salePricePerLiter - purchasePerLiter;
                                        var gmAmount = volume * gmPerLiter;
                                        var freightChargePerLiter = record.DeliveryReceipt!.Freight + (record.DeliveryReceipt?.ECC ?? 0m);
                                        var commissionPerLiter = record.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m;
                                        var commissionAmount = commissionPerLiter * volume;
                                        var netMarginPerLiter = gmPerLiter - freightChargePerLiter;
                                        var freightChargeAmount = volume * freightChargePerLiter;
                                        var netMarginAmount = volume * netMarginPerLiter;

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

                                #endregion

                                #region -- Initialize Variable for Computation of Totals

                                    var averagePurchasePrice = totalPurchaseAmountGross / totalVolume;
                                    var averageSalePrice = totalSaleAmount / totalVolume;
                                    var totalGmPerLiter = totalGmAmount / totalVolume;
                                    var totalFreightCharge = totalFcAmount / totalVolume;
                                    var totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                                    var totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                                #endregion

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

                                #endregion

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

                                        #endregion

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

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                            var overallTotalQuantity = 0m;
                                            var overallTotalSales = 0m;
                                            var overallTotalPurchases = 0m;
                                            var overallTotalGrossMargin = 0m;
                                            var overallTotalFreight = 0m;
                                            var overallTotalCommission = 0m;
                                            var overallTotalNetMargin = 0m;
                                            var overallTotalNetMarginPerLiter = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                var list = grossMarginReport.Where(s => s.DeliveryReceipt!.CustomerOrderSlip?.CustomerType == customerType.ToString()).ToList();
                                                var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                                var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                                var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                                                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                                                // Computation for Overall
                                                var overallQuantitySum = list.Sum(s => s.DeliveryReceipt!.Quantity);
                                                var overallSalesSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                                                var overallNetOfSalesSum = isCustomerVatable && overallSalesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(overallSalesSum)
                                                    : overallSalesSum;
                                                var overallPurchasesSum = list.Sum(s => s.Amount);
                                                var overallNetOfPurchasesSum = isSupplierVatable && overallPurchasesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(overallPurchasesSum)
                                                    : overallPurchasesSum;
                                                var overallGrossMarginSum = overallNetOfSalesSum - overallNetOfPurchasesSum;
                                                var overallFreightSum = list.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                                                var overallNetOfFreightSum = isHaulerVatable && overallFreightSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(overallFreightSum)
                                                    : overallFreightSum;
                                                var overallCommissionSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                                                var overallNetMarginSum = overallGrossMarginSum - (overallFreightSum + overallCommissionSum);
                                                var overallNetMarginPerLiterSum = overallNetMarginSum != 0 && overallQuantitySum != 0 ? overallNetMarginSum / overallQuantitySum : 0;

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

                                        #endregion

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

                                        #endregion
                                    });

                                #endregion

                                #region -- Biodiesel Summary

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

                                        #endregion

                                        #region -- Table Header

                                            content.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Biodiesel").AlignCenter().SemiBold();

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

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                            var biodieselTotalQuantity = 0m;
                                            var biodieselTotalSales = 0m;
                                            var biodieselTotalPurchases = 0m;
                                            var biodieselTotalGrossMargin = 0m;
                                            var biodieselTotalFreight = 0m;
                                            var biodieselTotalCommission = 0m;
                                            var biodieselTotalNetMargin = 0m;
                                            var biodieselTotalNetMarginPerLiter = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                var list = grossMarginReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                                                var listForBiodiesel = list.Where(s => s.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL").ToList();
                                                var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                                var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                                var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                                                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;


                                                // Computation for Biodiesel
                                                var biodieselQuantitySum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity);
                                                var biodieselSalesSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                                                var biodieselNetOfSalesSum = isCustomerVatable && biodieselSalesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(biodieselSalesSum)
                                                    : biodieselSalesSum;
                                                var biodieselPurchasesSum = listForBiodiesel.Sum(s => s.Amount);
                                                var biodieselNetOfPurchasesSum = isSupplierVatable && biodieselPurchasesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(biodieselPurchasesSum)
                                                    : biodieselPurchasesSum;
                                                var biodieselGrossMarginSum = biodieselNetOfSalesSum - biodieselNetOfPurchasesSum;
                                                var biodieselFreightSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                                                var biodieselNetOfFreightSum = isHaulerVatable && biodieselFreightSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(biodieselFreightSum)
                                                    : biodieselFreightSum;
                                                var biodieselCommissionSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                                                var biodieselNetMarginSum = biodieselGrossMarginSum - (biodieselFreightSum + biodieselCommissionSum);
                                                var biodieselNetMarginPerLiterSum = biodieselNetMarginSum != 0 && biodieselQuantitySum != 0 ? biodieselNetMarginSum / biodieselQuantitySum : 0;

                                                content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselQuantitySum != 0 ? biodieselQuantitySum < 0 ? $"({Math.Abs(biodieselQuantitySum).ToString(SD.Two_Decimal_Format)})" : biodieselQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetOfSalesSum != 0 ? biodieselNetOfSalesSum < 0 ? $"({Math.Abs(biodieselNetOfSalesSum).ToString(SD.Two_Decimal_Format)})" : biodieselNetOfSalesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselNetOfSalesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetOfPurchasesSum != 0 ? biodieselNetOfPurchasesSum < 0 ? $"({Math.Abs(biodieselNetOfPurchasesSum).ToString(SD.Two_Decimal_Format)})" : biodieselNetOfPurchasesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselNetOfPurchasesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselGrossMarginSum != 0 ? biodieselGrossMarginSum < 0 ? $"({Math.Abs(biodieselGrossMarginSum).ToString(SD.Two_Decimal_Format)})" : biodieselGrossMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselGrossMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetOfFreightSum != 0 ? biodieselNetOfFreightSum < 0 ? $"({Math.Abs(biodieselNetOfFreightSum).ToString(SD.Two_Decimal_Format)})" : biodieselNetOfFreightSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselNetOfFreightSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselCommissionSum != 0 ? biodieselCommissionSum < 0 ? $"({Math.Abs(biodieselCommissionSum).ToString(SD.Two_Decimal_Format)})" : biodieselCommissionSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselCommissionSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetMarginSum != 0 ? biodieselNetMarginSum < 0 ? $"({Math.Abs(biodieselNetMarginSum).ToString(SD.Two_Decimal_Format)})" : biodieselNetMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselNetMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(biodieselNetMarginPerLiterSum != 0 ? biodieselNetMarginPerLiterSum < 0 ? $"({Math.Abs(biodieselNetMarginPerLiterSum).ToString(SD.Four_Decimal_Format)})" : biodieselNetMarginPerLiterSum.ToString(SD.Four_Decimal_Format) : null).FontColor(biodieselNetMarginPerLiterSum < 0 ? Colors.Red.Medium : Colors.Black);

                                                biodieselTotalQuantity += biodieselQuantitySum;
                                                biodieselTotalSales += biodieselNetOfSalesSum;
                                                biodieselTotalPurchases += biodieselNetOfPurchasesSum;
                                                biodieselTotalGrossMargin += biodieselGrossMarginSum;
                                                biodieselTotalFreight += biodieselNetOfFreightSum;
                                                biodieselTotalCommission += biodieselCommissionSum;
                                                biodieselTotalNetMargin += biodieselNetMarginSum;
                                                biodieselTotalNetMarginPerLiter = biodieselTotalNetMargin != 0 && biodieselTotalQuantity != 0 ? biodieselTotalNetMargin / biodieselTotalQuantity : 0;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalQuantity != 0 ? biodieselTotalQuantity < 0 ? $"({Math.Abs(biodieselTotalQuantity).ToString(SD.Two_Decimal_Format)})" : biodieselTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalSales != 0 ? biodieselTotalSales < 0 ? $"({Math.Abs(biodieselTotalSales).ToString(SD.Two_Decimal_Format)})" : biodieselTotalSales.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalSales < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalPurchases != 0 ? biodieselTotalPurchases < 0 ? $"({Math.Abs(biodieselTotalPurchases).ToString(SD.Two_Decimal_Format)})" : biodieselTotalPurchases.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalPurchases < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalGrossMargin != 0 ? biodieselTotalGrossMargin < 0 ? $"({Math.Abs(biodieselTotalGrossMargin).ToString(SD.Two_Decimal_Format)})" : biodieselTotalGrossMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalGrossMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalFreight != 0 ? biodieselTotalFreight < 0 ? $"({Math.Abs(biodieselTotalFreight).ToString(SD.Two_Decimal_Format)})" : biodieselTotalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalCommission != 0 ? biodieselTotalCommission < 0 ? $"({Math.Abs(biodieselTotalCommission).ToString(SD.Two_Decimal_Format)})" : biodieselTotalCommission.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalCommission < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalNetMargin != 0 ? biodieselTotalNetMargin < 0 ? $"({Math.Abs(biodieselTotalNetMargin).ToString(SD.Two_Decimal_Format)})" : biodieselTotalNetMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(biodieselTotalNetMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(biodieselTotalNetMarginPerLiter != 0 ? biodieselTotalNetMarginPerLiter < 0 ? $"({Math.Abs(biodieselTotalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : biodieselTotalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(biodieselTotalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);

                                        #endregion
                                    });

                                #endregion

                                #region -- Econogas Summary

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

                                        #endregion

                                        #region -- Table Header

                                            content.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Econogas").AlignCenter().SemiBold();

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

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                            var econogasTotalQuantity = 0m;
                                            var econogasTotalSales = 0m;
                                            var econogasTotalPurchases = 0m;
                                            var econogasTotalGrossMargin = 0m;
                                            var econogasTotalFreight = 0m;
                                            var econogasTotalCommission = 0m;
                                            var econogasTotalNetMargin = 0m;
                                            var econogasTotalNetMarginPerLiter = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                var list = grossMarginReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                                                var listForEconogas = list.Where(s => s.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName == "ECONOGAS").ToList();
                                                var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                                var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                                var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                                                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                                                // Computation for Econogas
                                                var econogasQuantitySum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity);
                                                var econogasSalesSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                                                var econogasNetOfSalesSum = isCustomerVatable && econogasSalesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(econogasSalesSum)
                                                    : econogasSalesSum;
                                                var econogasPurchasesSum = listForEconogas.Sum(s => s.Amount);
                                                var econogasNetOfPurchasesSum = isSupplierVatable && econogasPurchasesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(econogasPurchasesSum)
                                                    : econogasPurchasesSum;
                                                var econogasGrossMarginSum = econogasNetOfSalesSum - econogasNetOfPurchasesSum;
                                                var econogasFreightSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                                                var econogasNetOfFreightSum = isHaulerVatable && econogasFreightSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(econogasFreightSum)
                                                    : econogasFreightSum;
                                                var econogasCommissionSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                                                var econogasNetMarginSum = econogasGrossMarginSum - (econogasFreightSum + econogasCommissionSum);
                                                var econogasNetMarginPerLiterSum = econogasNetMarginSum != 0 && econogasQuantitySum != 0 ? econogasNetMarginSum / econogasQuantitySum : 0;

                                                content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasQuantitySum != 0 ? econogasQuantitySum < 0 ? $"({Math.Abs(econogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : econogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetOfSalesSum != 0 ? econogasNetOfSalesSum < 0 ? $"({Math.Abs(econogasNetOfSalesSum).ToString(SD.Two_Decimal_Format)})" : econogasNetOfSalesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasNetOfSalesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetOfPurchasesSum != 0 ? econogasNetOfPurchasesSum < 0 ? $"({Math.Abs(econogasNetOfPurchasesSum).ToString(SD.Two_Decimal_Format)})" : econogasNetOfPurchasesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasNetOfPurchasesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasGrossMarginSum != 0 ? econogasGrossMarginSum < 0 ? $"({Math.Abs(econogasGrossMarginSum).ToString(SD.Two_Decimal_Format)})" : econogasGrossMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasGrossMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetOfFreightSum != 0 ? econogasNetOfFreightSum < 0 ? $"({Math.Abs(econogasNetOfFreightSum).ToString(SD.Two_Decimal_Format)})" : econogasNetOfFreightSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasNetOfFreightSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasCommissionSum != 0 ? econogasCommissionSum < 0 ? $"({Math.Abs(econogasCommissionSum).ToString(SD.Two_Decimal_Format)})" : econogasCommissionSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasCommissionSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetMarginSum != 0 ? econogasNetMarginSum < 0 ? $"({Math.Abs(econogasNetMarginSum).ToString(SD.Two_Decimal_Format)})" : econogasNetMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasNetMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(econogasNetMarginPerLiterSum != 0 ? econogasNetMarginPerLiterSum < 0 ? $"({Math.Abs(econogasNetMarginPerLiterSum).ToString(SD.Four_Decimal_Format)})" : econogasNetMarginPerLiterSum.ToString(SD.Four_Decimal_Format) : null).FontColor(econogasNetMarginPerLiterSum < 0 ? Colors.Red.Medium : Colors.Black);

                                                econogasTotalQuantity += econogasQuantitySum;
                                                econogasTotalSales += econogasNetOfSalesSum;
                                                econogasTotalPurchases += econogasNetOfPurchasesSum;
                                                econogasTotalGrossMargin += econogasGrossMarginSum;
                                                econogasTotalFreight += econogasNetOfFreightSum;
                                                econogasTotalCommission += econogasCommissionSum;
                                                econogasTotalNetMargin += econogasNetMarginSum;
                                                econogasTotalNetMarginPerLiter = econogasTotalNetMargin != 0 && econogasTotalQuantity != 0 ? econogasTotalNetMargin / econogasTotalQuantity : 0;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalQuantity != 0 ? econogasTotalQuantity < 0 ? $"({Math.Abs(econogasTotalQuantity).ToString(SD.Two_Decimal_Format)})" : econogasTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalSales != 0 ? econogasTotalSales < 0 ? $"({Math.Abs(econogasTotalSales).ToString(SD.Two_Decimal_Format)})" : econogasTotalSales.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalSales < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalPurchases != 0 ? econogasTotalPurchases < 0 ? $"({Math.Abs(econogasTotalPurchases).ToString(SD.Two_Decimal_Format)})" : econogasTotalPurchases.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalPurchases < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalGrossMargin != 0 ? econogasTotalGrossMargin < 0 ? $"({Math.Abs(econogasTotalGrossMargin).ToString(SD.Two_Decimal_Format)})" : econogasTotalGrossMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalGrossMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalFreight != 0 ? econogasTotalFreight < 0 ? $"({Math.Abs(econogasTotalFreight).ToString(SD.Two_Decimal_Format)})" : econogasTotalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalCommission != 0 ? econogasTotalCommission < 0 ? $"({Math.Abs(econogasTotalCommission).ToString(SD.Two_Decimal_Format)})" : econogasTotalCommission.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalCommission < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalNetMargin != 0 ? econogasTotalNetMargin < 0 ? $"({Math.Abs(econogasTotalNetMargin).ToString(SD.Two_Decimal_Format)})" : econogasTotalNetMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(econogasTotalNetMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(econogasTotalNetMarginPerLiter != 0 ? econogasTotalNetMarginPerLiter < 0 ? $"({Math.Abs(econogasTotalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : econogasTotalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(econogasTotalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);

                                        #endregion
                                    });

                                #endregion

                                #region -- Envirogas Summary

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

                                        #endregion

                                        #region -- Table Header

                                            content.Header(header =>
                                            {
                                                header.Cell().ColumnSpan(9).Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).Text("Envirogas").AlignCenter().SemiBold();

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

                                        #endregion

                                        #region -- Initialize Variable for Computation

                                            var envirogasTotalQuantity = 0m;
                                            var envirogasTotalSales = 0m;
                                            var envirogasTotalPurchases = 0m;
                                            var envirogasTotalGrossMargin = 0m;
                                            var envirogasTotalFreight = 0m;
                                            var envirogasTotalCommission = 0m;
                                            var envirogasTotalNetMargin = 0m;
                                            var envirogasTotalNetMarginPerLiter = 0m;

                                        #endregion

                                        #region -- Loop to Show Records

                                            foreach (var customerType in Enum.GetValues<CustomerType>())
                                            {
                                                var list = grossMarginReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                                                var listForEnvirogas = list.Where(s => s.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName == "ENVIROGAS").ToList();
                                                var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                                                var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                                                var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                                                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                                                // Computation for Envirogas
                                                var envirogasQuantitySum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity);
                                                var envirogasSalesSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                                                var envirogasNetOfSalesSum = isCustomerVatable && envirogasSalesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(envirogasSalesSum)
                                                    : envirogasSalesSum;
                                                var envirogasPurchasesSum = listForEnvirogas.Sum(s => s.Amount);
                                                var envirogasNetOfPurchasesSum = isSupplierVatable && envirogasPurchasesSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(envirogasPurchasesSum)
                                                    : envirogasPurchasesSum;
                                                var envirogasGrossMarginSum = envirogasNetOfSalesSum - envirogasNetOfPurchasesSum;
                                                var envirogasFreightSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * (s.DeliveryReceipt.Freight + s.DeliveryReceipt.ECC));
                                                var envirogasNetOfFreightSum = isHaulerVatable && envirogasFreightSum != 0m
                                                    ? repoCalculator.ComputeNetOfVat(envirogasFreightSum)
                                                    : envirogasFreightSum;
                                                var envirogasCommissionSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                                                var envirogasNetMarginSum = envirogasGrossMarginSum - (envirogasFreightSum + envirogasCommissionSum);
                                                var envirogasNetMarginPerLiterSum = envirogasNetMarginSum != 0 && envirogasQuantitySum != 0 ? envirogasNetMarginSum / envirogasQuantitySum : 0;

                                                content.Cell().Border(0.5f).Padding(3).Text(customerType.ToString());
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasQuantitySum != 0 ? envirogasQuantitySum < 0 ? $"({Math.Abs(envirogasQuantitySum).ToString(SD.Two_Decimal_Format)})" : envirogasQuantitySum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasQuantitySum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetOfSalesSum != 0 ? envirogasNetOfSalesSum < 0 ? $"({Math.Abs(envirogasNetOfSalesSum).ToString(SD.Two_Decimal_Format)})" : envirogasNetOfSalesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasNetOfSalesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetOfPurchasesSum != 0 ? envirogasNetOfPurchasesSum < 0 ? $"({Math.Abs(envirogasNetOfPurchasesSum).ToString(SD.Two_Decimal_Format)})" : envirogasNetOfPurchasesSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasNetOfPurchasesSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasGrossMarginSum != 0 ? envirogasGrossMarginSum < 0 ? $"({Math.Abs(envirogasGrossMarginSum).ToString(SD.Two_Decimal_Format)})" : envirogasGrossMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasGrossMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetOfFreightSum != 0 ? envirogasNetOfFreightSum < 0 ? $"({Math.Abs(envirogasNetOfFreightSum).ToString(SD.Two_Decimal_Format)})" : envirogasNetOfFreightSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasNetOfFreightSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasCommissionSum != 0 ? envirogasCommissionSum < 0 ? $"({Math.Abs(envirogasCommissionSum).ToString(SD.Two_Decimal_Format)})" : envirogasCommissionSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasCommissionSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetMarginSum != 0 ? envirogasNetMarginSum < 0 ? $"({Math.Abs(envirogasNetMarginSum).ToString(SD.Two_Decimal_Format)})" : envirogasNetMarginSum.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasNetMarginSum < 0 ? Colors.Red.Medium : Colors.Black);
                                                content.Cell().Border(0.5f).Padding(3).AlignRight().Text(envirogasNetMarginPerLiterSum != 0 ? envirogasNetMarginPerLiterSum < 0 ? $"({Math.Abs(envirogasNetMarginPerLiterSum).ToString(SD.Four_Decimal_Format)})" : envirogasNetMarginPerLiterSum.ToString(SD.Four_Decimal_Format) : null).FontColor(envirogasNetMarginPerLiterSum < 0 ? Colors.Red.Medium : Colors.Black);

                                                envirogasTotalQuantity += envirogasQuantitySum;
                                                envirogasTotalSales += envirogasNetOfSalesSum;
                                                envirogasTotalPurchases += envirogasNetOfPurchasesSum;
                                                envirogasTotalGrossMargin += envirogasGrossMarginSum;
                                                envirogasTotalFreight += envirogasNetOfFreightSum;
                                                envirogasTotalCommission += envirogasCommissionSum;
                                                envirogasTotalNetMargin += envirogasNetMarginSum;
                                                envirogasTotalNetMarginPerLiter = envirogasTotalNetMargin != 0 && envirogasTotalQuantity != 0 ? envirogasTotalNetMargin / envirogasTotalQuantity : 0;
                                            }

                                        #endregion

                                        #region -- Create Table Cell for Totals

                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text("TOTAL:");
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalQuantity != 0 ? envirogasTotalQuantity < 0 ? $"({Math.Abs(envirogasTotalQuantity).ToString(SD.Two_Decimal_Format)})" : envirogasTotalQuantity.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalQuantity < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalSales != 0 ? envirogasTotalSales < 0 ? $"({Math.Abs(envirogasTotalSales).ToString(SD.Two_Decimal_Format)})" : envirogasTotalSales.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalSales < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalPurchases != 0 ? envirogasTotalPurchases < 0 ? $"({Math.Abs(envirogasTotalPurchases).ToString(SD.Two_Decimal_Format)})" : envirogasTotalPurchases.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalPurchases < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalGrossMargin != 0 ? envirogasTotalGrossMargin < 0 ? $"({Math.Abs(envirogasTotalGrossMargin).ToString(SD.Two_Decimal_Format)})" : envirogasTotalGrossMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalGrossMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalFreight != 0 ? envirogasTotalFreight < 0 ? $"({Math.Abs(envirogasTotalFreight).ToString(SD.Two_Decimal_Format)})" : envirogasTotalFreight.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalFreight < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalCommission != 0 ? envirogasTotalCommission < 0 ? $"({Math.Abs(envirogasTotalCommission).ToString(SD.Two_Decimal_Format)})" : envirogasTotalCommission.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalCommission < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalNetMargin != 0 ? envirogasTotalNetMargin < 0 ? $"({Math.Abs(envirogasTotalNetMargin).ToString(SD.Two_Decimal_Format)})" : envirogasTotalNetMargin.ToString(SD.Two_Decimal_Format) : null).FontColor(envirogasTotalNetMargin < 0 ? Colors.Red.Medium : Colors.Black);
                                            content.Cell().Background(Colors.Grey.Lighten1).Border(0.5f).Padding(3).AlignRight().Text(envirogasTotalNetMarginPerLiter != 0 ? envirogasTotalNetMarginPerLiter < 0 ? $"({Math.Abs(envirogasTotalNetMarginPerLiter).ToString(SD.Four_Decimal_Format)})" : envirogasTotalNetMarginPerLiter.ToString(SD.Four_Decimal_Format) : null).FontColor(envirogasTotalNetMarginPerLiter < 0 ? Colors.Red.Medium : Colors.Black);

                                        #endregion
                                    });

                                #endregion
                            });
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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate gross margin report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

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

                var extractedBy = _userManager.GetUserName(User)!;
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                using var package = new ExcelPackage();
                var gmReportWorksheet = package.Workbook.Worksheets.Add("GMReport");

                var purchaseReport = await _unitOfWork.FilprideReport
                    .GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims, model.Customers, model.Commissionee, cancellationToken:cancellationToken);

                if (purchaseReport.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(GrossMarginReport));
                }

                #region -- Initialize "total" Variables for operations --

                var totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
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

                #endregion

                #region -- Column Names --

                var mergedCells = gmReportWorksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "GM REPORT";
                mergedCells.Style.Font.Size = 13;

                gmReportWorksheet.Cells["A2"].Value = "Date Range:";
                gmReportWorksheet.Cells["A3"].Value = "Extracted By:";
                gmReportWorksheet.Cells["A4"].Value = "Company:";

                gmReportWorksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                gmReportWorksheet.Cells["B3"].Value = $"{extractedBy}";
                gmReportWorksheet.Cells["B4"].Value = $"{companyClaims}";

                gmReportWorksheet.Cells["A7"].Value = "RR DATE";
                gmReportWorksheet.Cells["B7"].Value = "SUPPLIER NAME";
                gmReportWorksheet.Cells["C7"].Value = "PO NO.";
                gmReportWorksheet.Cells["D7"].Value = "FILPRIDE RR";
                gmReportWorksheet.Cells["E7"].Value = "FILPRIDE DR";
                gmReportWorksheet.Cells["F7"].Value = "CUSTOMER NAME";
                gmReportWorksheet.Cells["G7"].Value = "PRODUCT NAME";
                gmReportWorksheet.Cells["H7"].Value = "ACCOUNT SPECIALIST";
                gmReportWorksheet.Cells["I7"].Value = "HAULER NAME";
                gmReportWorksheet.Cells["J7"].Value = "COMMISSIONEE";
                gmReportWorksheet.Cells["K7"].Value = "VOLUME";
                gmReportWorksheet.Cells["L7"].Value = "COS PRICE";
                gmReportWorksheet.Cells["M7"].Value = "SALES G. VAT";
                gmReportWorksheet.Cells["N7"].Value = "SALES N. VAT";
                gmReportWorksheet.Cells["O7"].Value = "CPL G. VAT";
                gmReportWorksheet.Cells["P7"].Value = "PURCHASES G. VAT";
                gmReportWorksheet.Cells["Q7"].Value = "PURCHASES N.VAT";
                gmReportWorksheet.Cells["R7"].Value = "GM/LITER";
                gmReportWorksheet.Cells["S7"].Value = "GM AMOUNT";
                gmReportWorksheet.Cells["T7"].Value = "FREIGHT CHARGE";
                gmReportWorksheet.Cells["U7"].Value = "FC AMOUNT";
                gmReportWorksheet.Cells["V7"].Value = "FC N.VAT";
                gmReportWorksheet.Cells["W7"].Value = "COMMISSION/LITER";
                gmReportWorksheet.Cells["X7"].Value = "COMMISSION AMOUNT";
                gmReportWorksheet.Cells["Y7"].Value = "NET MARGIN/LIT";
                gmReportWorksheet.Cells["Z7"].Value = "NET MARGIN AMOUNT";

                #endregion

                #region -- Apply styling to the header row --

                    using (var range = gmReportWorksheet.Cells["A7:Z7"])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }

                #endregion

                // Populate the data row
                var row = 8; // starting row
                var currencyFormat = "#,##0.0000"; // numbers format
                var currencyFormatTwoDecimal = "#,##0.00"; // numbers format

                #region -- Populate data rows --

                foreach (var pr in purchaseReport)
                {
                    #region -- Variables and Formulas --

                    // calculate values, put in variables to be displayed per cell
                    var isSupplierVatable = pr.PurchaseOrder!.VatType == SD.VatType_Vatable;
                    var isHaulerVatable = pr.DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                    var isCustomerVatable = pr.DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;
                    var volume = pr.QuantityReceived;
                    var cosPricePerLiter = pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m; // sales per liter
                    var salesAmount = volume * cosPricePerLiter; // sales total
                    var netSales = isCustomerVatable
                        ? repoCalculator.ComputeNetOfVat(salesAmount)
                        : salesAmount;
                    var costAmount = pr.Amount; // purchase total
                    var costPerLiter = costAmount / volume; // purchase per liter
                    var netPurchases = isSupplierVatable
                        ? repoCalculator.ComputeNetOfVat(costAmount)
                        : costAmount; // purchase total net
                    var gmAmount = netSales - netPurchases; // gross margin total
                    var gmPerLiter = gmAmount/volume; // gross margin per liter
                    var freightCharge = (pr.DeliveryReceipt?.Freight ?? 0m) + (pr.DeliveryReceipt?.ECC ?? 0m); // freight charge per liter
                    var freightChargeAmount = pr.DeliveryReceipt?.FreightAmount ?? 0m; // freight charge total
                    var freightChargeNet = isHaulerVatable && freightChargeAmount != 0m
                        ? repoCalculator.ComputeNetOfVat(freightChargeAmount)
                        : freightChargeAmount;
                    var commissionPerLiter = pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m; // commission rate
                    var commissionAmount = volume * commissionPerLiter; // commission total
                    var netMarginAmount = gmAmount - freightChargeNet - commissionAmount;
                    var netMarginPerLiter = netMarginAmount / volume; // net margin per liter

                    #endregion

                    #region -- Assign Values to Cells --

                    gmReportWorksheet.Cells[row, 1].Value = pr.Date;
                    gmReportWorksheet.Cells[row, 2].Value = pr.PurchaseOrder?.SupplierName;
                    gmReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.PurchaseOrderNo;
                    gmReportWorksheet.Cells[row, 4].Value = pr.ReceivingReportNo;
                    gmReportWorksheet.Cells[row, 5].Value = pr.DeliveryReceipt?.DeliveryReceiptNo;
                    gmReportWorksheet.Cells[row, 6].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerName;
                    gmReportWorksheet.Cells[row, 7].Value = pr.PurchaseOrder?.ProductName;
                    gmReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.AccountSpecialist;
                    gmReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.HaulerName;
                    gmReportWorksheet.Cells[row, 10].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CommissioneeName;
                    gmReportWorksheet.Cells[row, 11].Value = volume;
                    gmReportWorksheet.Cells[row, 12].Value = cosPricePerLiter;
                    gmReportWorksheet.Cells[row, 13].Value = salesAmount;
                    gmReportWorksheet.Cells[row, 14].Value = netSales;
                    gmReportWorksheet.Cells[row, 15].Value = costPerLiter;
                    gmReportWorksheet.Cells[row, 16].Value = costAmount;
                    gmReportWorksheet.Cells[row, 17].Value = netPurchases;
                    gmReportWorksheet.Cells[row, 18].Value = gmPerLiter;
                    gmReportWorksheet.Cells[row, 19].Value = gmAmount;
                    gmReportWorksheet.Cells[row, 20].Value = freightCharge;
                    gmReportWorksheet.Cells[row, 21].Value = freightChargeAmount;
                    gmReportWorksheet.Cells[row, 22].Value = freightChargeNet;
                    gmReportWorksheet.Cells[row, 23].Value = commissionPerLiter;
                    gmReportWorksheet.Cells[row, 24].Value = commissionAmount;
                    gmReportWorksheet.Cells[row, 25].Value = netMarginPerLiter;
                    gmReportWorksheet.Cells[row, 26].Value = netMarginAmount;

                    #endregion -- Assign Values to Cells --

                    #region -- Add the values to total and format number cells --

                    totalCostAmount += costAmount;
                    totalNetPurchases += netPurchases;
                    totalSalesAmount += salesAmount;
                    totalGmPerLiter += gmPerLiter;
                    totalGmAmount += gmAmount;
                    totalFcAmount += freightChargeAmount;
                    totalCommissionAmount += commissionAmount;
                    totalNetMarginPerLiter += netMarginPerLiter;
                    totalNetMarginAmount += netMarginAmount;
                    totalNetSales += netSales;
                    totalFcNet += freightChargeNet;

                    gmReportWorksheet.Cells[row, 1].Style.Numberformat.Format = "MMM/dd/yyyy";

                    #endregion -- Add the values to total and format number cells --

                    row++;
                }

                #endregion -- Populate data rows --

                #region -- Other subtotal values and formatting of subtotal cells --

                var totalCostPerLiter = totalCostAmount / totalVolume;
                var totalCosPrice = totalSalesAmount / totalVolume;
                totalGmPerLiter = totalGmAmount / totalVolume;
                var totalFreightCharge = totalFcAmount / totalVolume;
                var totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                gmReportWorksheet.Cells[row, 10].Value = "Total: ";
                gmReportWorksheet.Cells[row, 11].Value = totalVolume;
                gmReportWorksheet.Cells[row, 12].Value = totalCosPrice;
                gmReportWorksheet.Cells[row, 13].Value = totalSalesAmount;
                gmReportWorksheet.Cells[row, 14].Value = totalNetSales;
                gmReportWorksheet.Cells[row, 15].Value = totalCostPerLiter;
                gmReportWorksheet.Cells[row, 16].Value = totalCostAmount;
                gmReportWorksheet.Cells[row, 17].Value = totalNetPurchases;
                gmReportWorksheet.Cells[row, 18].Value = totalGmPerLiter;
                gmReportWorksheet.Cells[row, 19].Value = totalGmAmount;
                gmReportWorksheet.Cells[row, 20].Value = totalFreightCharge;
                gmReportWorksheet.Cells[row, 21].Value = totalFcAmount;
                gmReportWorksheet.Cells[row, 22].Value = totalFcNet;
                gmReportWorksheet.Cells[row, 23].Value = totalCommissionPerLiter;
                gmReportWorksheet.Cells[row, 24].Value = totalCommissionAmount;
                gmReportWorksheet.Cells[row, 25].Value = totalNetMarginPerLiter;
                gmReportWorksheet.Cells[row, 26].Value = totalNetMarginAmount;

                gmReportWorksheet.Column(11).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(12).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(13).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(14).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(15).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(16).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(17).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(18).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(19).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(20).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(21).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(22).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(23).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(24).Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Column(25).Style.Numberformat.Format = currencyFormat;
                gmReportWorksheet.Column(26).Style.Numberformat.Format = currencyFormatTwoDecimal;

                #endregion -- Assign values of other totals and formatting of total cells --

                // Apply style to subtotal rows
                // color to whole row
                using (var range = gmReportWorksheet.Cells[row, 1, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                }
                // line to subtotal values
                using (var range = gmReportWorksheet.Cells[row, 10, row, 26])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                #region -- Summary Row --

                var rowForSummary = row + 8;

                // Set the column headers
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

                // Apply styling to the header row for Overall
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 2, rowForSummary - 1, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Overall
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 2, rowForSummary + 4, 10])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForBiodiesel = gmReportWorksheet.Cells[rowForSummary - 2, 12, rowForSummary - 2, 19];
                mergedCellForBiodiesel.Merge = true;
                mergedCellForBiodiesel.Value = "Biodiesel";
                mergedCellForBiodiesel.Style.Font.Size = 13;
                mergedCellForBiodiesel.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 12, rowForSummary - 2, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                gmReportWorksheet.Cells[rowForSummary - 1, 12].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 13].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 14].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 15].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 16].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 17].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 18].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 19].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 12, rowForSummary - 1, 19].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Biodiesel
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 12, rowForSummary - 1, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Biodiesel
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 12, rowForSummary + 4, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 12, rowForSummary + 4, 19])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEconogas = gmReportWorksheet.Cells[rowForSummary - 2, 21, rowForSummary - 2, 28];
                mergedCellForEconogas.Merge = true;
                mergedCellForEconogas.Value = "Econogas";
                mergedCellForEconogas.Style.Font.Size = 13;
                mergedCellForEconogas.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 21, rowForSummary - 2, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                gmReportWorksheet.Cells[rowForSummary - 1, 21].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 22].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 23].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 24].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 25].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 26].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 27].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 28].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 21, rowForSummary - 1, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Econogas
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 21, rowForSummary - 1, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Econogas
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 21, rowForSummary + 4, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 21, rowForSummary + 4, 28])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                // Set the column headers
                var mergedCellForEnvirogas = gmReportWorksheet.Cells[rowForSummary - 2, 30, rowForSummary - 2, 37];
                mergedCellForEnvirogas.Merge = true;
                mergedCellForEnvirogas.Value = "Envirogas";
                mergedCellForEnvirogas.Style.Font.Size = 13;
                mergedCellForEnvirogas.Style.Font.Bold = true;
                gmReportWorksheet.Cells[rowForSummary - 2, 30, rowForSummary - 2, 37].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                //inset data/value in excel
                gmReportWorksheet.Cells[rowForSummary - 1, 30].Value = "Volume";
                gmReportWorksheet.Cells[rowForSummary - 1, 31].Value = "Sales N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 32].Value = "Purchases N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 33].Value = "Gross Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 34].Value = "Freight N. VAT";
                gmReportWorksheet.Cells[rowForSummary - 1, 35].Value = "Commission";
                gmReportWorksheet.Cells[rowForSummary - 1, 36].Value = "Net Margin";
                gmReportWorksheet.Cells[rowForSummary - 1, 37].Value = "Net GM/LIT";

                gmReportWorksheet.Cells[rowForSummary - 1, 30, rowForSummary - 1, 37].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Apply styling to the header row for Envirogas
                using (var range = gmReportWorksheet.Cells[rowForSummary - 1, 30, rowForSummary - 1, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Apply style to subtotal row for Envirogas
                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 30, rowForSummary + 4, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                }

                using (var range = gmReportWorksheet.Cells[rowForSummary + 4, 30, rowForSummary + 4, 37])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin; // Single top border
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double; // Double bottom border
                }

                var totalOverallQuantity = 0m;
                var totalOverallNetOfSales = 0m;
                var totalOverallNetOfPurchases = 0m;
                var totalOverallGrossMargin = 0m;
                var totalOverallNetOfFreight = 0m;
                var totalOverallCommission = 0m;
                var totalOverallNetMargin = 0m;
                var totalOverallNetMarginPerLiter = 0m;

                var totalQuantityForBiodiesel = 0m;
                var totalNetOfSalesForBiodiesel = 0m;
                var totalNetOfPurchasesForBiodiesel = 0m;
                var totalGrossMarginForBiodiesel = 0m;
                var totalNetOfFreightForBiodiesel = 0m;
                var totalCommissionForBiodiesel = 0m;
                var totalNetMarginForBiodiesel = 0m;
                var totalNetMarginPerLiterForBiodiesel = 0m;

                var totalQuantityForEconogas = 0m;
                var totalNetOfSalesForEconogas = 0m;
                var totalNetOfPurchasesForEconogas = 0m;
                var totalGrossMarginForEconogas = 0m;
                var totalNetOfFreightForEconogas = 0m;
                var totalCommissionForEconogas = 0m;
                var totalNetMarginForEconogas = 0m;
                var totalNetMarginPerLiterForEconogas = 0m;

                var totalQuantityForEnvirogas = 0m;
                var totalNetOfSalesForEnvirogas= 0m;
                var totalNetOfPurchasesForEnvirogas = 0m;
                var totalGrossMarginForEnvirogas = 0m;
                var totalNetOfFreightForEnvirogas = 0m;
                var totalCommissionForEnvirogas = 0m;
                var totalNetMarginForEnvirogas = 0m;
                var totalNetMarginPerLiterForEnvirogas = 0m;


                foreach (var customerType in Enum.GetValues<CustomerType>())
                {
                    var list = purchaseReport.Where(s => s.DeliveryReceipt!.Customer?.CustomerType == customerType.ToString()).ToList();
                    var listForBiodiesel = list.Where(s => s.DeliveryReceipt!.CustomerOrderSlip!.Product?.ProductName == "BIODIESEL").ToList();
                    var listForEconogas = list.Where(s => s.DeliveryReceipt!.PurchaseOrder!.Product?.ProductName == "ECONOGAS").ToList();
                    var listForEnvirogas = list.Where(s => s.DeliveryReceipt!.PurchaseOrder!.Product?.ProductName == "ENVIROGAS").ToList();
                    var isSupplierVatable = list.Count > 0 && list.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                    var isHaulerVatable = list.Count > 0 && list.First().DeliveryReceipt?.HaulerVatType == SD.VatType_Vatable;
                    var isCustomerVatable = list.Count > 0 && list.First().DeliveryReceipt?.CustomerOrderSlip!.VatType == SD.VatType_Vatable;

                    // Computation for Overall
                    var overallQuantitySum = list.Sum(s => s.DeliveryReceipt!.Quantity);
                    var overallSalesSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var overallNetOfSalesSum = isCustomerVatable && overallSalesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(overallSalesSum)
                        : overallSalesSum;
                    var overallPurchasesSum = list.Sum(s => s.Amount);
                    var overallNetOfPurchasesSum = isSupplierVatable && overallPurchasesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(overallPurchasesSum)
                        : overallPurchasesSum;
                    var overallGrossMarginSum = overallNetOfSalesSum - overallNetOfPurchasesSum;
                    var overallFreightSum = list.Sum(s => s.DeliveryReceipt!.FreightAmount);
                    var overallNetOfFreightSum = isHaulerVatable && overallFreightSum != 0m
                        ? repoCalculator.ComputeNetOfVat(overallFreightSum)
                        : overallFreightSum;
                    var overallCommissionSum = list.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var overallNetMarginSum = overallGrossMarginSum - (overallFreightSum + overallCommissionSum);
                    var overallNetMarginPerLiterSum = overallNetMarginSum != 0 && overallQuantitySum != 0 ? overallNetMarginSum / overallQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 2].Value = customerType.ToString();
                    gmReportWorksheet.Cells[rowForSummary, 3].Value = overallQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 4].Value = overallNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 5].Value = overallNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 6].Value = overallGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 7].Value = overallNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 8].Value = overallCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 9].Value = overallNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 10].Value = overallNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                    // Computation for Biodiesel
                    var biodieselQuantitySum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity);
                    var biodieselSalesSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var biodieselNetOfSalesSum = isCustomerVatable && biodieselSalesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(biodieselSalesSum)
                        : biodieselSalesSum;
                    var biodieselPurchasesSum = listForBiodiesel.Sum(s => s.Amount);
                    var biodieselNetOfPurchasesSum = isSupplierVatable && biodieselPurchasesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(biodieselPurchasesSum)
                        : biodieselPurchasesSum;
                    var biodieselGrossMarginSum = biodieselNetOfSalesSum - biodieselNetOfPurchasesSum;
                    var biodieselFreightSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.FreightAmount);
                    var biodieselNetOfFreightSum = isHaulerVatable && biodieselFreightSum != 0m
                        ? repoCalculator.ComputeNetOfVat(biodieselFreightSum)
                        : biodieselFreightSum;
                    var biodieselCommissionSum = listForBiodiesel.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt.CustomerOrderSlip!.CommissionRate);
                    var biodieselNetMarginSum = biodieselGrossMarginSum - (biodieselFreightSum + biodieselCommissionSum);
                    var biodieselNetMarginPerLiterSum = biodieselNetMarginSum != 0 && biodieselQuantitySum != 0 ? biodieselNetMarginSum / biodieselQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 12].Value = biodieselQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 13].Value = biodieselNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 14].Value = biodieselNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 15].Value = biodieselGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 16].Value = biodieselNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 17].Value = biodieselCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 18].Value = biodieselNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 19].Value = biodieselNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 19].Style.Numberformat.Format = currencyFormat;

                    // Computation for Econogas
                    var econogasQuantitySum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity);
                    var econogasSalesSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt.CustomerOrderSlip!.DeliveredPrice);
                    var econogasNetOfSalesSum = isCustomerVatable && econogasSalesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(econogasSalesSum)
                        : econogasSalesSum;
                    var econogasPurchasesSum = listForEconogas.Sum(s => s.Amount);
                    var econogasNetOfPurchasesSum = isSupplierVatable && econogasPurchasesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(econogasPurchasesSum)
                        : econogasPurchasesSum;
                    var econogasGrossMarginSum = econogasNetOfSalesSum - econogasNetOfPurchasesSum;
                    var econogasFreightSum = listForEconogas.Sum(s => s.DeliveryReceipt!.FreightAmount);
                    var econogasNetOfFreightSum = isHaulerVatable && econogasFreightSum != 0m
                        ? repoCalculator.ComputeNetOfVat(econogasFreightSum)
                        : econogasFreightSum;
                    var econogasCommissionSum = listForEconogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var econogasNetMarginSum = econogasGrossMarginSum - (econogasFreightSum + econogasCommissionSum);
                    var econogasNetMarginPerLiterSum = econogasNetMarginSum != 0 && econogasQuantitySum != 0 ? econogasNetMarginSum / econogasQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 21].Value = econogasQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 22].Value = econogasNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 23].Value = econogasNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 24].Value = econogasGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 25].Value = econogasNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 26].Value = econogasCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 27].Value = econogasNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 28].Value = econogasNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 28].Style.Numberformat.Format = currencyFormat;

                    // Computation for Envirogas
                    var envirogasQuantitySum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity);
                    var envirogasSalesSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.DeliveredPrice);
                    var envirogasNetOfSalesSum = isCustomerVatable && envirogasSalesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(envirogasSalesSum)
                        : envirogasSalesSum;
                    var envirogasPurchasesSum = listForEnvirogas.Sum(s => s.Amount);
                    var envirogasNetOfPurchasesSum = isSupplierVatable && envirogasPurchasesSum != 0m
                        ? repoCalculator.ComputeNetOfVat(envirogasPurchasesSum)
                        : envirogasPurchasesSum;
                    var envirogasGrossMarginSum = envirogasNetOfSalesSum - envirogasNetOfPurchasesSum;
                    var envirogasFreightSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.FreightAmount);
                    var envirogasNetOfFreightSum = isHaulerVatable && envirogasFreightSum != 0m
                        ? repoCalculator.ComputeNetOfVat(envirogasFreightSum)
                        : envirogasFreightSum;
                    var envirogasCommissionSum = listForEnvirogas.Sum(s => s.DeliveryReceipt!.Quantity * s.DeliveryReceipt!.CustomerOrderSlip!.CommissionRate);
                    var envirogasNetMarginSum = envirogasGrossMarginSum - (envirogasFreightSum + envirogasCommissionSum);
                    var envirogasNetMarginPerLiterSum = envirogasNetMarginSum != 0 && envirogasQuantitySum != 0 ? envirogasNetMarginSum / envirogasQuantitySum : 0;

                    gmReportWorksheet.Cells[rowForSummary, 30].Value = envirogasQuantitySum;
                    gmReportWorksheet.Cells[rowForSummary, 31].Value = envirogasNetOfSalesSum;
                    gmReportWorksheet.Cells[rowForSummary, 32].Value = envirogasNetOfPurchasesSum;
                    gmReportWorksheet.Cells[rowForSummary, 33].Value = envirogasGrossMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 34].Value = envirogasNetOfFreightSum;
                    gmReportWorksheet.Cells[rowForSummary, 35].Value = envirogasCommissionSum;
                    gmReportWorksheet.Cells[rowForSummary, 36].Value = envirogasNetMarginSum;
                    gmReportWorksheet.Cells[rowForSummary, 37].Value = envirogasNetMarginPerLiterSum;

                    gmReportWorksheet.Cells[rowForSummary, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 31].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 32].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 33].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 34].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 35].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 36].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    gmReportWorksheet.Cells[rowForSummary, 37].Style.Numberformat.Format = currencyFormat;

                    rowForSummary++;

                    // Computation of total for Overall
                    totalOverallQuantity += overallQuantitySum;
                    totalOverallNetOfSales += overallNetOfSalesSum;
                    totalOverallNetOfPurchases += overallNetOfPurchasesSum;
                    totalOverallGrossMargin += overallGrossMarginSum;
                    totalOverallNetOfFreight += overallNetOfFreightSum;
                    totalOverallCommission += overallCommissionSum;
                    totalOverallNetMargin += overallNetMarginSum;
                    totalOverallNetMarginPerLiter += totalOverallNetMargin != 0 && totalOverallQuantity != 0 ? totalOverallNetMargin / totalOverallQuantity : 0;

                    // Computation of total for Biodiesel
                    totalQuantityForBiodiesel += biodieselQuantitySum;
                    totalNetOfSalesForBiodiesel += biodieselNetOfSalesSum;
                    totalNetOfPurchasesForBiodiesel += biodieselNetOfPurchasesSum;
                    totalGrossMarginForBiodiesel += biodieselGrossMarginSum;
                    totalNetOfFreightForBiodiesel += biodieselNetOfFreightSum;
                    totalCommissionForBiodiesel += biodieselCommissionSum;
                    totalNetMarginForBiodiesel += biodieselNetMarginSum;
                    totalNetMarginPerLiterForBiodiesel += totalNetMarginForBiodiesel != 0 && totalQuantityForBiodiesel != 0 ? totalNetMarginForBiodiesel / totalQuantityForBiodiesel : 0;

                    // Computation of total for Econogas
                    totalQuantityForEconogas += econogasQuantitySum;
                    totalNetOfSalesForEconogas += econogasNetOfSalesSum;
                    totalNetOfPurchasesForEconogas += econogasNetOfPurchasesSum;
                    totalGrossMarginForEconogas += econogasGrossMarginSum;
                    totalNetOfFreightForEconogas += econogasNetOfFreightSum;
                    totalCommissionForEconogas += econogasCommissionSum;
                    totalNetMarginForEconogas += econogasNetMarginSum;
                    totalNetMarginPerLiterForEconogas += totalNetMarginForEconogas != 0 && totalQuantityForEconogas != 0 ? totalNetMarginForEconogas / totalQuantityForEconogas : 0;

                    // Computation of total for Envirogas
                    totalQuantityForEnvirogas += envirogasQuantitySum;
                    totalNetOfSalesForEnvirogas += envirogasNetOfSalesSum;
                    totalNetOfPurchasesForEnvirogas += envirogasNetOfPurchasesSum;
                    totalGrossMarginForEnvirogas += envirogasGrossMarginSum;
                    totalNetOfFreightForEnvirogas += envirogasNetOfFreightSum;
                    totalCommissionForEnvirogas += envirogasCommissionSum;
                    totalNetMarginForEnvirogas += envirogasNetMarginSum;
                    totalNetMarginPerLiterForEnvirogas += totalNetMarginForEnvirogas != 0 && totalQuantityForEnvirogas != 0 ? totalNetMarginForEnvirogas / totalQuantityForEnvirogas : 0;

                }

                var styleOfTotal = gmReportWorksheet.Cells[rowForSummary, 2];
                styleOfTotal.Value = "Total";

                gmReportWorksheet.Cells[rowForSummary, 3].Value = totalOverallQuantity;
                gmReportWorksheet.Cells[rowForSummary, 4].Value = totalOverallNetOfSales;
                gmReportWorksheet.Cells[rowForSummary, 5].Value = totalOverallNetOfPurchases;
                gmReportWorksheet.Cells[rowForSummary, 6].Value = totalOverallGrossMargin;
                gmReportWorksheet.Cells[rowForSummary, 7].Value = totalOverallNetOfFreight;
                gmReportWorksheet.Cells[rowForSummary, 8].Value = totalOverallCommission;
                gmReportWorksheet.Cells[rowForSummary, 9].Value = totalOverallNetMargin;
                gmReportWorksheet.Cells[rowForSummary, 10].Value = totalOverallNetMarginPerLiter;

                gmReportWorksheet.Cells[rowForSummary, 3].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 4].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 5].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 10].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 12].Value = totalQuantityForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 13].Value = totalNetOfSalesForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 14].Value = totalNetOfPurchasesForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 15].Value = totalGrossMarginForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 16].Value = totalNetOfFreightForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 17].Value = totalCommissionForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 18].Value = totalNetMarginForBiodiesel;
                gmReportWorksheet.Cells[rowForSummary, 19].Value = totalNetMarginPerLiterForBiodiesel;

                gmReportWorksheet.Cells[rowForSummary, 12].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 13].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 14].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 15].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 16].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 17].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 18].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 19].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 21].Value = totalQuantityForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 22].Value = totalNetOfSalesForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 23].Value = totalNetOfPurchasesForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 24].Value = totalGrossMarginForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 25].Value = totalNetOfFreightForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 26].Value = totalCommissionForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 27].Value = totalNetMarginForEconogas;
                gmReportWorksheet.Cells[rowForSummary, 28].Value = totalNetMarginPerLiterForEconogas;

                gmReportWorksheet.Cells[rowForSummary, 21].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 22].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 23].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 24].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 25].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 26].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 28].Style.Numberformat.Format = currencyFormat;

                gmReportWorksheet.Cells[rowForSummary, 30].Value = totalQuantityForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 31].Value = totalNetOfSalesForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 32].Value = totalNetOfPurchasesForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 33].Value = totalGrossMarginForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 34].Value = totalNetOfFreightForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 35].Value = totalCommissionForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 36].Value = totalNetMarginForEnvirogas;
                gmReportWorksheet.Cells[rowForSummary, 37].Value = totalNetMarginPerLiterForEnvirogas;

                gmReportWorksheet.Cells[rowForSummary, 30].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 31].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 32].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 33].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 34].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 35].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 36].Style.Numberformat.Format = currencyFormatTwoDecimal;
                gmReportWorksheet.Cells[rowForSummary, 37].Style.Numberformat.Format = currencyFormat;

                #endregion == Summary Contents ==

                // Auto-fit columns for better readability
                gmReportWorksheet.Cells.AutoFitColumns();
                gmReportWorksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate gross margin report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion -- Generate GM Report Excel File --

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
                    .ToListAsync();

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

                        #endregion

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

                        #endregion

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

                                #endregion

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

                                #endregion

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

                                #endregion

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
                                        ? repoCalculator.ComputeNetOfVat(subTotalBeginningGross)
                                        : subTotalBeginningGross;
                                    var subTotalBeginningEwt = isSupplierTaxable
                                        ? repoCalculator.ComputeEwtAmount(subTotalBeginningNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalBeginningNetAmount = subTotalBeginningGross - subTotalBeginningEwt;
                                    //PURCHASES
                                    var subTotalPurchaseVolume = rr.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.QuantityReceived);
                                    var subTotalPurchaseGross = rr.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.Amount);
                                    var subTotalPurchaseNetOfVat = isSupplierVatable && subTotalPurchaseGross != 0
                                        ? repoCalculator.ComputeNetOfVat(subTotalPurchaseGross)
                                        : subTotalPurchaseGross;
                                    var subTotalPurchaseEwt = isSupplierTaxable && subTotalPurchaseNetOfVat != 0
                                        ? repoCalculator.ComputeEwtAmount(subTotalPurchaseNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalPurchaseNetAmount = subTotalPurchaseGross - subTotalPurchaseEwt;
                                    //PAYMENT
                                    var subTotalPaymentVolume = rr.Where(x => x.IsPaid).Sum(x => x.QuantityReceived);
                                    var subTotalPaymentGross = rr.Where(x => x.IsPaid).Sum(x => x.Amount);
                                    var subTotalPaymentNetOfVat = isSupplierVatable && subTotalPaymentGross != 0
                                        ? repoCalculator.ComputeNetOfVat(subTotalPaymentGross)
                                        : subTotalPaymentGross;
                                    var subTotalPaymentEwt = isSupplierTaxable && subTotalPaymentNetOfVat != 0
                                        ? repoCalculator.ComputeEwtAmount(subTotalPaymentNetOfVat, 0.01m)
                                        : 0m;
                                    var subTotalPaymentNetAmount = subTotalPaymentGross - subTotalPaymentEwt;
                                    //ENDING BALANCE
                                    var subTotalEndingVolume = (subTotalBeginningVolume + subTotalPurchaseVolume) - subTotalPaymentVolume;
                                    var subTotalEndingGross = (subTotalBeginningGross + subTotalPurchaseGross) - subTotalPaymentGross;
                                    var subTotalEndingEwt = (subTotalBeginningEwt + subTotalPurchaseEwt) - subTotalPaymentEwt;
                                    var subTotalEndingNetAmount = (subTotalBeginningNetAmount + subTotalPurchaseNetAmount) - subTotalPaymentNetAmount;

                                    var groupBySupplier = rr.GroupBy(x => new { x.PurchaseOrder?.Supplier?.SupplierName }).ToList();

                                    foreach (var item in groupBySupplier)
                                    {
                                        //BEGINNING
                                        var beginningVolume = item.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.QuantityReceived);
                                        var beginningGross = item.Where(x => !x.IsPaid && x.Date < model.DateTo).Sum(x => x.Amount);
                                        var beginningNetOfVat = isSupplierVatable && beginningGross != 0
                                            ? repoCalculator.ComputeNetOfVat(beginningGross)
                                            : beginningGross;
                                        var beginningEwt = isSupplierTaxable && beginningNetOfVat != 0
                                            ? repoCalculator.ComputeEwtAmount(beginningNetOfVat, 0.01m)
                                            : 0m;
                                        var beginningNetAmount = beginningGross - beginningEwt;
                                        //PURCHASES
                                        var purchaseVolume = item.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.QuantityReceived);
                                        var purchaseGross = item.Where(x => !x.IsPaid && x.Date == model.DateTo).Sum(x => x.Amount);
                                        var purchaseNetOfVat = isSupplierVatable && purchaseGross != 0
                                            ? repoCalculator.ComputeNetOfVat(purchaseGross)
                                            : purchaseGross;
                                        var purchaseEwt = isSupplierTaxable && purchaseNetOfVat != 0
                                            ? repoCalculator.ComputeEwtAmount(purchaseNetOfVat, 0.01m)
                                            : 0m;
                                        var purchaseNetAmount = purchaseGross - purchaseEwt;
                                        //PAYMENT
                                        var paymentVolume = item.Where(x => x.IsPaid).Sum(x => x.QuantityReceived);
                                        var paymentGross = item.Where(x => x.IsPaid).Sum(x => x.Amount);
                                        var paymentNetOfVat = isSupplierVatable && paymentGross != 0
                                            ? repoCalculator.ComputeNetOfVat(paymentGross)
                                            : paymentGross;
                                        var paymentEwt = isSupplierTaxable && paymentNetOfVat != 0
                                            ? repoCalculator.ComputeEwtAmount(paymentNetOfVat, 0.01m)
                                            : 0m;
                                        var paymentNetAmount = paymentGross - paymentEwt;
                                        //ENDING BALANCE
                                        var endingVolume = (beginningVolume + purchaseVolume) - paymentVolume;
                                        var endingGross = (beginningGross + purchaseGross) - paymentGross;
                                        var endingEwt = (beginningEwt + purchaseEwt) - paymentEwt;
                                        var endingNetAmount = (beginningNetAmount + purchaseNetAmount) - paymentNetAmount;

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

                                #endregion

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

                                #endregion
                            });
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

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate trade payable report quest pdf", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion

        #region -- Generate Trade Payable Report as Excel File --

        public async Task<IActionResult> GenerateTradePayableReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradePayableReport));
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

                var currencyFormat = "#,##0.00";

                var tradePayableReport = await _unitOfWork.FilprideReport
                    .GetTradePayableReport(model.DateFrom, model.DateTo, companyClaims, cancellationToken);

                var allReportMonthYear = tradePayableReport.GroupBy(rr => new { rr.Date.Year, rr.Date.Month });

                var beginning = tradePayableReport
                    .Where(rr => rr.Date < dateFrom)
                    .Where(rr => rr.IsPaid == false)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month })
                    .ToList();

                var beginningPaid = tradePayableReport
                    .Where(rr => rr.Date < dateFrom)
                    .Where(rr => rr.IsPaid)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month })
                    .ToList();

                var purchases = tradePayableReport
                    .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month })
                    .ToList();

                var purchasesPaid = tradePayableReport
                    .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo)
                    .Where(rr => rr.IsPaid)
                    .GroupBy(rr => new { rr.Date.Year, rr.Date.Month })
                    .ToList();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Trade Payable");

                #region == Title ==
                var titleCells = worksheet.Cells["A1:B1"];
                titleCells.Merge = true;
                titleCells.Value = "TRADE PAYABLE REPORT";
                titleCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Extracted By:";
                worksheet.Cells["A4"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{dateFrom} - {dateTo}";
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                #endregion

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
                titleCells.Value = "APTRADE";
                titleCells.Style.Font.Size = 13;
                titleCells.Style.Font.Bold = true;
                titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                titleCells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Salmon);
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

                    titleCells = worksheet.Cells[6, col-4, 6, col-1];
                    titleCells.Merge = true;
                    titleCells.Value = header;
                    titleCells.Style.Font.Size = 13;
                    titleCells.Style.Font.Bold = true;
                    titleCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    titleCells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Salmon);
                    titleCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    titleCells.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    col = col + 1;
                }
                #endregion

                var row = 8;
                IEnumerable<IGrouping<object, FilprideReceivingReport>> categorized = null!;

                #region == Initialize Variables ==

                // initialize subtotals for month/year
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

                #endregion

                // loop for each month
                foreach (var monthYear in allReportMonthYear)
                {
                    // reset placing per category

                    // get the month-year then group by supplier
                    var allSupplier = monthYear.GroupBy(rr => rr.PurchaseOrder?.Supplier?.SupplierName)
                        .ToList();

                    // enter the month-year to the Excel
                    worksheet.Cells[row, 1].Value = (CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(allSupplier.FirstOrDefault()?.FirstOrDefault()?.Date.Month ?? 0))
                                                    + " " +
                                                    (allSupplier.FirstOrDefault()?.FirstOrDefault()?.Date.Year.ToString() ?? " ");
                    worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                    row++;

                    // get the int of the current month-year to be used in condition
                    var monthInt = allSupplier.FirstOrDefault()?.FirstOrDefault()?.Date.Month ?? 0;
                    var yearInt = allSupplier.FirstOrDefault()?.FirstOrDefault()?.Date.Year ?? 0;


                    // in the month-year loop by supplier
                    foreach (var supplier in allSupplier)
                    {
                        // write the name of supplier
                        var supplierName = supplier.FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName ?? "";
                        var isSupplierVatable = supplier.First().PurchaseOrder!.VatType == SD.VatType_Vatable;
                        var isSupplierTaxable = supplier.First().PurchaseOrder!.TaxType == SD.TaxType_WithTax;
                        worksheet.Cells[row, 2].Value = supplierName;
                        var whichPayment = string.Empty;
                        var forPayment = false;
                        var forEnding = false;

                        // make loop for the two categories
                        for (var i = 1; i != 5; i++)
                        {
                            // decide category to use either beginning or purchase
                            switch (i)
                            {
                                case 1:
                                    categorized = beginning;
                                    whichPayment = "beginning";
                                    break;
                                case 2:
                                    categorized = purchases;
                                    whichPayment = "purchases";
                                    break;
                                case 3:
                                    forPayment = true;
                                    break;
                                case 4:
                                    forEnding = true;
                                    break;
                            }

                            if (forPayment)
                            {
                                switch (whichPayment)
                                {
                                    case "beginning":
                                        categorized = beginningPaid;
                                        break;
                                    case "purchases":
                                        categorized = purchasesPaid;
                                        break;
                                }
                            }

                            if (categorized != null)
                            {
                                // iterate through the date range, find which month-year
                                foreach (var monthYearChoice in categorized)
                                {
                                    // if the month-year was found in iteration, write
                                    if (monthYearChoice.FirstOrDefault()?.Date.Month !=
                                        monthYear.FirstOrDefault()?.Date.Month ||
                                        monthYearChoice.FirstOrDefault()?.Date.Year !=
                                        monthYear.FirstOrDefault()?.Date.Year)
                                    {
                                        continue;
                                    }

                                    // write the data per category
                                    var gross = monthYearChoice
                                        .Where(rr => rr.Date.Month == monthInt && rr.Date.Year == yearInt)
                                        .Where(rr => rr.PurchaseOrder?.Supplier?.SupplierName == supplier
                                            .FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName)
                                        .Sum(rr => rr.Amount);
                                    var volume = monthYearChoice
                                        .Where(rr => rr.Date.Month == monthInt && rr.Date.Year == yearInt)
                                        .Where(rr => rr.PurchaseOrder?.Supplier?.SupplierName == supplier
                                            .FirstOrDefault()?.PurchaseOrder?.Supplier?.SupplierName)
                                        .Sum(rr => rr.QuantityReceived);
                                    var netOfVat = isSupplierVatable
                                        ? repoCalculator.ComputeNetOfVat(gross)
                                        : gross;
                                    var ewt = isSupplierTaxable
                                        ? repoCalculator.ComputeEwtAmount(netOfVat, 0.01m)
                                        : 0m;
                                    var net = gross - ewt;

                                    worksheet.Cells[row, i * 5-1].Value = volume;
                                    worksheet.Cells[row, i*5].Value = gross;
                                    worksheet.Cells[row, i*5+1].Value = ewt;
                                    worksheet.Cells[row, i*5+2].Value = net;
                                    worksheet.Cells[row, i*5-1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, i*5].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, i*5+1].Style.Numberformat.Format = currencyFormat;
                                    worksheet.Cells[row, i*5+2].Style.Numberformat.Format = currencyFormat;

                                    switch (i)
                                    {
                                        // add to subtotals
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

                            if (forEnding)
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

                            forPayment = false;
                        }
                        // after the four categories, next supplier
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
                    #endregion

                    #region == Ending Subtotal and Grand Total Processes ==
                    // input subtotal of ending
                    var subtotalVolumeEnding = subtotalVolumeBeginning + subtotalVolumePurchases - subtotalVolumePayments;
                    var subtotalGrossEnding = subtotalGrossBeginning + subtotalGrossPurchases - subtotalGrossPayments;
                    var subtotalEwtEnding = subtotalEwtBeginning + subtotalEwtPurchases - subtotalEwtPayments;
                    var subtotalNetEnding = subtotalNetBeginning + subtotalNetPurchases - subtotalNetPayments;

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

                    #endregion

                }

                row++;

                #region == Grand Total Inputting ==
                worksheet.Cells[row, 2].Value = "GRAND TOTALS:";
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
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                }
                #endregion

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(3).Width = 1;
                worksheet.Column(8).Width = 1;
                worksheet.Column(13).Width = 1;
                worksheet.Column(18).Width = 1;

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate trade payable report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion -- Generate AP Trade Report --

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
                worksheet.Cells[1, 2, 3, 2].Style.Font.Size = 14;

                worksheet.Cells[5, 2].Value = "SUPPLIER";
                worksheet.Cells[5, 3].Value = "BUYER";
                worksheet.Cells[5, 4].Value = "PRODUCT";
                worksheet.Cells[5, 5].Value = "PAYMENT TERMS";
                worksheet.Cells[5, 6].Value = "ORIGINAL PO VOLUME";
                worksheet.Cells[5, 7].Value = "UNLIFTED LAST MONTH";
                worksheet.Cells[5, 8].Value = "LIFTED THIS MONTH";
                worksheet.Cells[5, 9].Value = "UNLIFTED THIS MONTH";
                worksheet.Cells[5, 10].Value = "PRICE(VAT-EX)";
                worksheet.Cells[5, 11].Value = "PRICE (VAT-INC)";
                worksheet.Cells[5, 12].Value = "GROSS AMOUNT";
                worksheet.Cells[5, 13].Value = "EWT";
                worksheet.Cells[5, 14].Value = "NET OF EWT";

                using (var range = worksheet.Cells[5, 2, 5, 14])
                {
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255,204,172));
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Row(5).Height = 36;

                var groupBySupplier = apReport
                    .OrderBy(po => po.Date)
                    .ThenBy(po => po.PurchaseOrderNo)
                    .GroupBy(po => po.Supplier)
                    .ToList();

                int row = 5;
                decimal originalPoGrandTotalBiodiesel = 0m;
                decimal originalPoGrandTotalEconogas = 0m;
                decimal originalPoGrandTotalEnvirogas = 0m;
                decimal unliftedLastMonthGrandTotalBiodiesel = 0m;
                decimal unliftedLastMonthGrandTotalEconogas = 0m;
                decimal unliftedLastMonthGrandTotalEnvirogas = 0m;
                decimal liftedThisMonthGrandTotalBiodiesel = 0m;
                decimal liftedThisMonthGrandTotalEconogas = 0m;
                decimal liftedThisMonthGrandTotalEnvirogas = 0m;
                decimal unliftedThisMonthGrandTotalBiodiesel = 0m;
                decimal unliftedThisMonthGrandTotalEconogas = 0m;
                decimal unliftedThisMonthGrandTotalEnvirogas = 0m;
                decimal grossAmountGrandTotalBiodiesel = 0m;
                decimal grossAmountGrandTotalEconogas = 0m;
                decimal grossAmountGrandTotalEnvirogas = 0m;
                decimal ewtGrandTotalBiodiesel = 0m;
                decimal ewtGrandTotalEconogas = 0m;
                decimal ewtGrandTotalEnvirogas = 0m;
                var repoCalculator = _unitOfWork.FilpridePurchaseOrder;

                string[] productList = ["BIODIESEL", "ECONOGAS", "ENVIROGAS"];

                foreach (var sameSupplierGroup in groupBySupplier)
                {
                    var isVatable = sameSupplierGroup.First().VatType == SD.VatType_Vatable;
                    var isTaxable = sameSupplierGroup.First().TaxType == SD.TaxType_WithTax;
                    row += 2;
                    worksheet.Cells[row, 2].Value = sameSupplierGroup.First().Supplier!.SupplierName;
                    worksheet.Cells[row, 2].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Value = sameSupplierGroup.First().Company;
                    var groupByProduct = sameSupplierGroup
                        .GroupBy(po => po.Product)
                        .OrderBy(po => po.Key?.ProductName)
                        .ToList();
                    decimal poSubtotal = 0m;
                    decimal unliftedLastMonthSubtotal = 0m;
                    decimal liftedThisMonthSubtotal = 0m;
                    decimal unliftedThisMonthSubtotal = 0m;
                    decimal grossAmountSubtotal = 0m;
                    decimal ewtAmountSubtotal = 0m;
                    decimal tempForGrandTotal = 0m;

                    foreach (var product in productList)
                    {
                        // declare per product
                        var aGroupByProduct = groupByProduct
                            .FirstOrDefault(g => g.Key?.ProductName == product);
                        worksheet.Cells[row, 4].Value = product;
                        worksheet.Cells[row, 5].Value = groupByProduct.FirstOrDefault()?.FirstOrDefault()?.Supplier?.SupplierTerms;

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

                                foreach (var po in aGroupByProduct)
                                {
                                    decimal rrQtyForUnliftedLastMonth = 0m;
                                    decimal rrQtyForLiftedThisMonth = 0m;
                                    decimal currentPoQuantity = po.Quantity;
                                    allPoTotal += currentPoQuantity;

                                    if (po.ReceivingReports!.Count != 0)
                                    {
                                        foreach (var rr in po.ReceivingReports)
                                        {
                                            if (rr.Date < monthYear)
                                            {
                                                rrQtyForUnliftedLastMonth += rr.QuantityReceived;
                                            }
                                            else if (rr.Date.Month == monthYear.Month && rr.Date.Year == monthYear.Year)
                                            {
                                                rrQtyForLiftedThisMonth += rr.QuantityReceived;
                                                grossOfLiftedThisMonth += rr.Amount;
                                            }
                                        }
                                    }

                                    unliftedLastMonth += currentPoQuantity - rrQtyForUnliftedLastMonth;
                                    liftedThisMonth += rrQtyForLiftedThisMonth;
                                    unliftedThisMonth += currentPoQuantity - rrQtyForUnliftedLastMonth - rrQtyForLiftedThisMonth;
                                }

                                if (allPoTotal != 0m)
                                {
                                    poSubtotal += allPoTotal;
                                    tempForGrandTotal += allPoTotal;
                                }

                                // operations per product
                                var netOfVat = isVatable
                                    ? repoCalculator.ComputeNetOfVat(grossOfLiftedThisMonth)
                                    : grossOfLiftedThisMonth;
                                var ewt = isTaxable
                                    ? repoCalculator.ComputeEwtAmount(netOfVat, 0.01m)
                                    : 0m;

                                // WRITE ORIGINAL PO VOLUME
                                worksheet.Cells[row, 6].Value = allPoTotal;
                                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormatTwoDecimal;

                                // WRITE UNLIFTED LAST MONTH
                                if (unliftedLastMonth != 0m)
                                {
                                    worksheet.Cells[row, 7].Value = unliftedLastMonth;
                                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 7].Value = 0m;
                                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE LIFTED THIS MONTH
                                if (liftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 8].Value = liftedThisMonth;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 8].Value = 0m;
                                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // WRITE UNLIFTED THIS MONTH
                                if (unliftedThisMonth != 0m)
                                {
                                    worksheet.Cells[row, 9].Value = unliftedThisMonth;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }
                                else
                                {
                                    worksheet.Cells[row, 9].Value = 0m;
                                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                }

                                // operations for grandtotals
                                switch (product)
                                {
                                    case "BIODIESEL":
                                        unliftedLastMonthGrandTotalBiodiesel += unliftedLastMonth;
                                        liftedThisMonthGrandTotalBiodiesel += liftedThisMonth;
                                        unliftedThisMonthGrandTotalBiodiesel += unliftedThisMonth;
                                        grossAmountGrandTotalBiodiesel += grossOfLiftedThisMonth;
                                        ewtGrandTotalBiodiesel += ewt;
                                        break;
                                    case "ECONOGAS":
                                        unliftedLastMonthGrandTotalEconogas += unliftedLastMonth;
                                        liftedThisMonthGrandTotalEconogas += liftedThisMonth;
                                        unliftedThisMonthGrandTotalEconogas += unliftedThisMonth;
                                        grossAmountGrandTotalEconogas += grossOfLiftedThisMonth;
                                        ewtGrandTotalEconogas += ewt;
                                        break;
                                    case "ENVIROGAS":
                                        unliftedLastMonthGrandTotalEnvirogas += unliftedLastMonth;
                                        liftedThisMonthGrandTotalEnvirogas += liftedThisMonth;
                                        unliftedThisMonthGrandTotalEnvirogas += unliftedThisMonth;
                                        grossAmountGrandTotalEnvirogas += grossOfLiftedThisMonth;
                                        ewtGrandTotalEnvirogas += ewt;
                                        break;
                                }

                                // operations for subtotals
                                unliftedLastMonthSubtotal += unliftedLastMonth;
                                liftedThisMonthSubtotal += liftedThisMonth;
                                unliftedThisMonthSubtotal += unliftedThisMonth;
                                grossAmountSubtotal += grossOfLiftedThisMonth;
                                ewtAmountSubtotal += ewt;

                                // write per product: price, gross, ewt, net
                                var price = grossOfLiftedThisMonth / liftedThisMonth;
                                var priceNetOfVat = isVatable
                                    ? repoCalculator.ComputeNetOfVat(price)
                                    : price;

                                worksheet.Cells[row, 10].Value = priceNetOfVat;
                                worksheet.Cells[row, 11].Value = price;
                                worksheet.Cells[row, 12].Value = grossOfLiftedThisMonth;
                                worksheet.Cells[row, 13].Value = ewt;
                                worksheet.Cells[row, 14].Value = grossOfLiftedThisMonth - ewt;
                                using var range = worksheet.Cells[row, 10, row, 14];
                                range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }
                        }

                        switch (product)
                        {
                            case "BIODIESEL":
                                originalPoGrandTotalBiodiesel += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;
                            case "ECONOGAS":
                                originalPoGrandTotalEconogas += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;
                            case "ENVIROGAS":
                                originalPoGrandTotalEnvirogas += tempForGrandTotal;
                                tempForGrandTotal = 0m;
                                break;

                        }

                        row++;
                    }

                    worksheet.Cells[row, 3].Value = "SUB-TOTAL";
                    worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                    worksheet.Cells[row, 6].Value = poSubtotal;
                    worksheet.Cells[row, 7].Value = unliftedLastMonthSubtotal;
                    worksheet.Cells[row, 8].Value = liftedThisMonthSubtotal;
                    worksheet.Cells[row, 9].Value = unliftedThisMonthSubtotal;
                    if (liftedThisMonthSubtotal != 0)
                    {
                        var price = grossAmountSubtotal / liftedThisMonthSubtotal;
                        var priceNetOfVat = isVatable
                            ? repoCalculator.ComputeNetOfVat(price)
                            : price;
                        worksheet.Cells[row, 10].Value = priceNetOfVat;
                        worksheet.Cells[row, 11].Value = price;
                        worksheet.Cells[row, 12].Value = grossAmountSubtotal;
                        worksheet.Cells[row, 13].Value = ewtAmountSubtotal;
                        worksheet.Cells[row, 14].Value = grossAmountSubtotal - ewtAmountSubtotal;
                    }
                    else
                    {
                        worksheet.Cells[row, 10].Value = 0m;
                        worksheet.Cells[row, 11].Value = 0m;
                        worksheet.Cells[row, 12].Value = 0m;
                        worksheet.Cells[row, 13].Value = 0m;
                        worksheet.Cells[row, 14].Value = 0m;
                    }

                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    using (var range = worksheet.Cells[row, 3, row, 14])
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

                decimal finalPo = originalPoGrandTotalBiodiesel + originalPoGrandTotalEconogas + originalPoGrandTotalEnvirogas;
                decimal finalUnliftedLastMonth = unliftedLastMonthGrandTotalBiodiesel + unliftedLastMonthGrandTotalEconogas + originalPoGrandTotalEnvirogas;
                decimal finalLiftedThisMonth = liftedThisMonthGrandTotalBiodiesel + liftedThisMonthGrandTotalEconogas + liftedThisMonthGrandTotalEnvirogas;
                decimal finalUnliftedThisMonth = unliftedThisMonthGrandTotalBiodiesel + unliftedThisMonthGrandTotalEconogas + unliftedThisMonthGrandTotalEnvirogas;
                decimal finalGross = grossAmountGrandTotalBiodiesel + grossAmountGrandTotalEconogas + grossAmountGrandTotalEnvirogas;
                decimal finalEwt = ewtGrandTotalBiodiesel + ewtGrandTotalEconogas + ewtGrandTotalEnvirogas;

                foreach (var product in productList)
                {
                    worksheet.Cells[row, 4].Value = product;
                    worksheet.Cells[row, 5].Value = "ALL TERMS";

                    switch (product)
                    {
                        case "BIODIESEL":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalBiodiesel;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalBiodiesel;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalBiodiesel;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalBiodiesel;
                            if (liftedThisMonthGrandTotalBiodiesel != 0)
                            {
                                worksheet.Cells[row, 10].Value = grossAmountGrandTotalBiodiesel / liftedThisMonthGrandTotalBiodiesel / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalBiodiesel / liftedThisMonthGrandTotalBiodiesel;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalBiodiesel;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalBiodiesel;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalBiodiesel - ewtGrandTotalBiodiesel;
                            break;
                        case "ECONOGAS":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalEconogas;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalEconogas;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalEconogas;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalEconogas;
                            if (liftedThisMonthGrandTotalEconogas != 0)
                            {
                                worksheet.Cells[row, 10].Value = grossAmountGrandTotalEconogas / liftedThisMonthGrandTotalEconogas / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalEconogas / liftedThisMonthGrandTotalEconogas;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalEconogas;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalEconogas;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalEconogas - ewtGrandTotalEconogas;
                            break;
                        case "ENVIROGAS":
                            worksheet.Cells[row, 6].Value = originalPoGrandTotalEnvirogas;
                            worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotalEnvirogas;
                            worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotalEnvirogas;
                            worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotalEnvirogas;
                            if (liftedThisMonthGrandTotalEnvirogas != 0)
                            {
                                worksheet.Cells[row, 10].Value = grossAmountGrandTotalEnvirogas / liftedThisMonthGrandTotalEnvirogas / 1.12m;
                                worksheet.Cells[row, 11].Value = grossAmountGrandTotalEnvirogas / liftedThisMonthGrandTotalEnvirogas;
                            }
                            else
                            {
                                worksheet.Cells[row, 10].Value = 0m;
                                worksheet.Cells[row, 11].Value = 0m;
                            }
                            worksheet.Cells[row, 12].Value = grossAmountGrandTotalEnvirogas;
                            worksheet.Cells[row, 13].Value = ewtGrandTotalEnvirogas;
                            worksheet.Cells[row, 14].Value = grossAmountGrandTotalEnvirogas - ewtGrandTotalEnvirogas;
                            break;
                    }

                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    row++;
                }

                // final total
                worksheet.Cells[row, 3].Value = "GRAND-TOTAL";
                worksheet.Cells[row, 4].Value = "ALL PRODUCTS";
                worksheet.Cells[row, 6].Value = finalPo;
                worksheet.Cells[row, 7].Value = finalUnliftedLastMonth;
                worksheet.Cells[row, 8].Value = finalLiftedThisMonth;
                worksheet.Cells[row, 9].Value = finalUnliftedThisMonth;
                if (finalLiftedThisMonth != 0)
                {
                    worksheet.Cells[row, 10].Value = finalGross / finalLiftedThisMonth / 1.12m;
                    worksheet.Cells[row, 11].Value = finalGross / finalLiftedThisMonth;
                    worksheet.Cells[row, 12].Value = finalGross;
                    worksheet.Cells[row, 13].Value = finalEwt;
                    worksheet.Cells[row, 14].Value = finalGross - finalEwt;
                }
                else
                {
                    worksheet.Cells[row, 10].Value = 0m;
                    worksheet.Cells[row, 11].Value = 0m;
                    worksheet.Cells[row, 12].Value = 0m;
                    worksheet.Cells[row, 13].Value = 0m;
                    worksheet.Cells[row, 14].Value = 0m;
                }

                using (var range = worksheet.Cells[row, 6, row, 14])
                {
                    range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                }

                using (var range = worksheet.Cells[row, 3, row, 14])
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

                worksheet.Columns.AutoFit();
                worksheet.Column(1).Width = 8;
                worksheet.Column(2).Width = 30;

                #endregion == TOPSHEET ==

                #region == BY SUPPLIER ==

                foreach (var aGroupBySupplier in groupBySupplier)
                {
                    var firstRecord = aGroupBySupplier.FirstOrDefault();
                    var isVatable = firstRecord!.VatType == SD.VatType_Vatable;
                    var isTaxable = firstRecord.TaxType == SD.TaxType_WithTax;
                    DateOnly monthYearTemp = new DateOnly(monthYear.Year, monthYear.Month, 1);
                    DateOnly lastDayOfMonth = monthYearTemp.AddDays(-1);
                    var poGrandTotal = 0m;
                    var unliftedLastMonthGrandTotal = 0m;
                    var liftedThisMonthGrandTotal = 0m;
                    var unliftedThisMonthGrandTotal = 0m;
                    var grossAmountGrandTotal = 0m;
                    var ewtGrandTotal = 0m;

                    worksheet = package.Workbook.Worksheets.Add(firstRecord.Supplier!.SupplierName);
                    worksheet.Cells.Style.Font.Name = "Calibri";
                    worksheet.Cells[1, 1].Value = $"SUPPLIER: {firstRecord.Supplier!.SupplierName}";
                    worksheet.Cells[2, 1].Value = "AP MONITORING REPORT (TRADE & SUPPLY GENERATED: PER PO #)";
                    worksheet.Cells[3, 1].Value = "REF: PURCHASE ORDER REPORT-per INTEGRATED BUSINESS SYSTEM";
                    worksheet.Cells[4, 1].Value = $"FOR THE MONTH OF {monthYear.ToString("MMMM")} {monthYear.Year.ToString()}";
                    worksheet.Cells[5, 1].Value = $"DUE DATE: {lastDayOfMonth.ToString("MMMM dd, yyyy")}";
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
                        worksheet.Cells[row, 6].Value = "ORIGINAL PO VOLUME";
                        worksheet.Cells[row, 7].Value = "UNLIFTED LAST MONTH";
                        worksheet.Cells[row, 8].Value = "LIFTED THIS MONTH";
                        worksheet.Cells[row, 9].Value = "UNLIFTED THIS MONTH";
                        worksheet.Cells[row, 10].Value = "PRICE(VAT-EX)";
                        worksheet.Cells[row, 11].Value = "PRICE(VAT-INC)";
                        worksheet.Cells[row, 12].Value = "GROSS AMOUNT(VAT-INC)";
                        worksheet.Cells[row, 13].Value = "EWT";
                        worksheet.Cells[row, 14].Value = "NET OF EWT";

                        using (var range = worksheet.Cells[row, 1, row, 14])
                        {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255,204,172));
                            range.Style.Font.Bold = true;
                        }

                        worksheet.Row(row).Height = 36;
                        row++;

                        foreach(var po in aGroupByProduct)
                        {
                            // computing the cells variables
                            var poTotal = po.Quantity;
                            var grossAmount = 0m;
                            var unliftedLastMonth = 0m;
                            var liftedThisMonthRrQty = 0m;
                            var unliftedThisMonth = 0m;

                            if (po.ReceivingReports!.Count != 0)
                            {
                                var liftedLastMonthRrQty = po.ReceivingReports
                                    .Where(rr => rr.Date < monthYear)
                                    .Sum(rr => rr.QuantityReceived);

                                unliftedLastMonth = poTotal - liftedLastMonthRrQty;

                                var liftedThisMonth = po.ReceivingReports
                                    .Where(rr => rr.Date.Month == monthYear.Month && rr.Date.Year == monthYear.Year)
                                    .ToList();

                                liftedThisMonthRrQty = liftedThisMonth.Sum(x => x.QuantityReceived);

                                unliftedThisMonth = unliftedLastMonth - liftedThisMonthRrQty;
                                grossAmount += liftedThisMonth.Sum(x => x.Amount);
                            }

                            var netOfVat = isVatable
                                ? repoCalculator.ComputeNetOfVat(grossAmount)
                                : grossAmount;
                            var ewt = isTaxable
                                ? repoCalculator.ComputeEwtAmount(netOfVat, 0.01m)
                                : 0m;

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
                            worksheet.Cells[row, 6].Value = poTotal;
                            worksheet.Cells[row, 7].Value = unliftedLastMonth;
                            worksheet.Cells[row, 8].Value = liftedThisMonthRrQty;
                            worksheet.Cells[row, 9].Value = unliftedThisMonth;
                            worksheet.Cells[row, 10].Value = isVatable
                                ? repoCalculator.ComputeNetOfVat(po.FinalPrice)
                                : po.FinalPrice;
                            worksheet.Cells[row, 11].Value = po.FinalPrice;
                            worksheet.Cells[row, 12].Value = grossAmount;
                            worksheet.Cells[row, 13].Value = ewt;
                            worksheet.Cells[row, 14].Value = grossAmount - ewt;

                            using (var range = worksheet.Cells[row, 6, row, 14])
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
                        worksheet.Cells[row, 6].Value = poSubtotal;
                        worksheet.Cells[row, 7].Value = unliftedLastMonthSubtotal;
                        worksheet.Cells[row, 8].Value = liftedThisMonthSubtotal;
                        worksheet.Cells[row, 9].Value = unliftedThisMonthSubtotal;
                        if (liftedThisMonthSubtotal != 0)
                        {
                            var price = grossAmountSubtotal / liftedThisMonthSubtotal;
                            var priceNetOfVat = isVatable
                                ? repoCalculator.ComputeNetOfVat(price)
                                : price;
                            worksheet.Cells[row, 10].Value = priceNetOfVat;
                            worksheet.Cells[row, 11].Value = price;
                        }
                        worksheet.Cells[row, 12].Value = grossAmountSubtotal;
                        worksheet.Cells[row, 13].Value = ewtSubtotal;
                        worksheet.Cells[row, 14].Value = grossAmountSubtotal - ewtSubtotal;

                        using (var range = worksheet.Cells[row, 3, row, 5])
                        {
                            range.Merge = true;
                            range.Value = product;
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        using (var range = worksheet.Cells[row, 6, row, 14])
                        {
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                        using (var range = worksheet.Cells[row, 1, row, 14])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        }

                        row += 2;
                    }

                    worksheet.Cells[row, 2].Value = "GRAND-TOTAL";
                    worksheet.Cells[row, 6].Value = poGrandTotal;
                    worksheet.Cells[row, 7].Value = unliftedLastMonthGrandTotal;
                    worksheet.Cells[row, 8].Value = liftedThisMonthGrandTotal;
                    worksheet.Cells[row, 9].Value = unliftedThisMonthGrandTotal;
                    if (liftedThisMonthGrandTotal != 0)
                    {
                        var price = grossAmountGrandTotal / liftedThisMonthGrandTotal;
                        var priceNetOfVat = isVatable
                            ? repoCalculator.ComputeNetOfVat(price)
                            : price;
                        worksheet.Cells[row, 10].Value = priceNetOfVat;
                        worksheet.Cells[row, 11].Value = price;
                    }
                    worksheet.Cells[row, 12].Value = grossAmountGrandTotal;
                    worksheet.Cells[row, 13].Value = ewtGrandTotal;
                    worksheet.Cells[row, 14].Value = grossAmountGrandTotal - ewtGrandTotal;

                    using (var range = worksheet.Cells[row, 3, row, 5])
                    {
                        range.Merge = true;
                        range.Value = "ALL PRODUCTS";
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    using (var range = worksheet.Cells[row, 6, row, 14])
                    {
                        range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                    using (var range = worksheet.Cells[row, 1, row, 14])
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
                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    worksheet.Columns.AutoFit();
                    worksheet.Column(1).Width = 14;
                }

                #endregion == BY SUPPLIER ==

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(User.Identity!.Name!, "Generate accounts payable report excel file", "Accounts Payable Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion

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

        #endregion
    }
}
