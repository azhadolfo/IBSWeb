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
    public class AccountsPayableReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public AccountsPayableReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ILogger<GeneralLedgerReportController> logger)
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

                        #endregion

                        #region -- Content

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
                _logger.LogError(ex, "Failed to generate cleared disbursement report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(ClearedDisbursementReport));
            }
        }

        #endregion

        #region -- Generate Cleared Disbursement as Report Excel File --

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

        public IActionResult PurchaseOrderReport()
        {
            return View();
        }

        #region -- Generated Purchase Order Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseOrderReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
            try
            {
                var purchaseOrder = await _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo, companyClaims!);

                if (!purchaseOrder.Any())
                {
                    TempData["error"] = "No records found!";
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

                        #endregion

                        #region -- Content

                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(125);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(100);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(100);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("PO#").SemiBold();
                                header.Cell().Text("IS PO#").SemiBold();
                                header.Cell().Text("Transaction Date").SemiBold();
                                header.Cell().Text("Supplier").SemiBold();
                                header.Cell().Text("Product").SemiBold();
                                header.Cell().AlignRight().Text("Quantity").SemiBold();
                                header.Cell().AlignCenter().Text("Unit").SemiBold();
                                header.Cell().AlignRight().Text("Price").SemiBold();
                                header.Cell().AlignRight().Text("Amount").SemiBold();
                                header.Cell().PaddingLeft(10).Text("Remarks").SemiBold();
                            });

                            foreach (var record in purchaseOrder)
                            {
                                table.Cell().Text(record.PurchaseOrderNo);
                                table.Cell().Text(record.OldPoNo);
                                table.Cell().Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Text(record.Supplier?.SupplierName);
                                table.Cell().Text(record.Product?.ProductName);
                                table.Cell().AlignRight().Text(record.Quantity.ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignCenter().Text(record.Product?.ProductUnit);
                                table.Cell().AlignRight().Text(record.Price.ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text(record.Amount.ToString(SD.Two_Decimal_Format));
                                table.Cell().PaddingLeft(10).Text(record.Remarks);

                            }
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
                _logger.LogError(ex, "Failed to generate purchase report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
        }

        #endregion

        #region -- Generate Purchase Order Report Excel File --

        public async Task<IActionResult> GeneratePurchaseOrderReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please input date range";
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

                var purchaseOrderReport =
                    await _unitOfWork.FilprideReport.GetPurchaseOrderReport(model.DateFrom, model.DateTo,
                        companyClaims);

                if (purchaseOrderReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
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
                int row = 8;
                string currencyFormat = "#,##0.00";

                foreach (var po in purchaseOrderReport)
                {
                    worksheet.Cells[row, 1].Value = po.PurchaseOrderNo;
                    worksheet.Cells[row, 2].Value = po.OldPoNo;
                    worksheet.Cells[row, 3].Value = po.Date;
                    worksheet.Cells[row, 4].Value = po.Supplier?.SupplierName;
                    worksheet.Cells[row, 5].Value = po.Product?.ProductName;
                    worksheet.Cells[row, 6].Value = po.Quantity;
                    worksheet.Cells[row, 7].Value = po.Product?.ProductUnit;
                    worksheet.Cells[row, 8].Value = po.Price;
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

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PurchaseOrderReport_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(PurchaseOrderReport));
            }
        }

        #endregion
    }
}
