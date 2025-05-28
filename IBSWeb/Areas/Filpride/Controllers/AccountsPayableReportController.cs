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

        #region -- Generate Cleared Disbursement Report as Excel File --

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

        #region -- Generate Purchase Order Report as Excel File --

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

        public IActionResult PurchaseReport()
        {
            return View();
        }

        #region -- Generated Purchase Report as Quest PDF

        [HttpPost]
        public async Task<IActionResult> GeneratedPurchaseReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();

            if (!ModelState.IsValid)
            {
                TempData["error"] = "The submitted information is invalid.";
                return RedirectToAction(nameof(PurchaseReport));
            }
            try
            {
                var purchaseReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims!, dateSelectionType:model.DateSelectionType);

                if (!purchaseReport.Any())
                {
                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(PurchaseReport));
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
                                    .Text("PURCHASE REPORT")
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
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(35);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(25);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(30);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Date").SemiBold();
                                header.Cell().Text("Supplier Name").SemiBold();
                                header.Cell().Text("Supplier Tin").SemiBold();
                                header.Cell().Text("Supplier Address").SemiBold();
                                header.Cell().Text("PO No.").SemiBold();
                                header.Cell().Text("Filpride RR").SemiBold();
                                header.Cell().Text("Filpride DR").SemiBold();
                                header.Cell().Text("ATL No.").SemiBold();
                                header.Cell().Text("Supplier SI").SemiBold();
                                header.Cell().Text("SI/Lifting Date").SemiBold();
                                header.Cell().Text("Supplier DR").SemiBold();
                                header.Cell().Text("Supplier WC").SemiBold();
                                header.Cell().Text("Customer Name").SemiBold();
                                header.Cell().Text("Product").SemiBold();
                                header.Cell().AlignRight().Text("Volume").SemiBold();
                                header.Cell().AlignRight().Text("Cost Per Liter").SemiBold();
                                header.Cell().AlignRight().Text("Cost Amount").SemiBold();
                                header.Cell().AlignRight().Text("Vat Amount").SemiBold();
                                header.Cell().AlignRight().Text("WHT Amount").SemiBold();
                                header.Cell().AlignRight().Text("Net Purchases").SemiBold();
                                header.Cell().PaddingLeft(10).Text("Hauler Name").SemiBold();
                            });

                            var totalVolume = 0m;
                            decimal totalCostAmount = 0m;
                            decimal totalVatAmount = 0m;
                            decimal totalWHTAmount = 0m;
                            decimal totalNetPurchases = 0m;
                            decimal totalCOSAmount = 0m;
                            decimal totalGMAmount = 0m;
                            decimal totalFCAmount = 0m;
                            decimal totalCommissionAmount = 0m;
                            decimal totalNetMarginAmount = 0m;

                            foreach (var record in purchaseReport)
                            {
                                var volume = record.QuantityReceived;
                                var costAmountGross = record.Amount;
                                var costPerLiter = costAmountGross / volume;
                                var cosPricePerLiter = (record.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m);
                                var freightChargePerLiter = (record.DeliveryReceipt?.Freight ?? 0m);
                                var commissionPerLiter = (record.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m);
                                var costAmountNet = costAmountGross / 1.12m;
                                var cosAmountGross = volume * cosPricePerLiter;
                                var gmPerLiter = (cosPricePerLiter - costPerLiter);

                                table.Cell().Text(record.Date.ToString(SD.Date_Format));
                                table.Cell().Text(record.PurchaseOrder?.Supplier?.SupplierName);
                                table.Cell().Text(record.PurchaseOrder?.Supplier?.SupplierTin);
                                table.Cell().Text(record.PurchaseOrder?.Supplier?.SupplierAddress);
                                table.Cell().Text(record.PurchaseOrder?.PurchaseOrderNo);
                                table.Cell().Text(record.ReceivingReportNo);
                                table.Cell().Text(record.DeliveryReceipt?.DeliveryReceiptNo);
                                table.Cell().Text(record.AuthorityToLoadNo);
                                table.Cell().Text(record.SupplierInvoiceNumber);
                                table.Cell().Text(record.SupplierInvoiceDate?.ToString(SD.Date_Format));
                                table.Cell().Text(record.SupplierDrNo);
                                table.Cell().Text(record.WithdrawalCertificate);
                                table.Cell().Text(record.DeliveryReceipt?.Customer?.CustomerName);
                                table.Cell().Text(record.PurchaseOrder?.Product?.ProductName);
                                table.Cell().AlignRight().Text(volume.ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text(costPerLiter.ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text(costAmountGross.ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text((costAmountNet * 0.12m).ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text((costAmountNet * 0.01m).ToString(SD.Two_Decimal_Format));
                                table.Cell().AlignRight().Text(costAmountNet.ToString(SD.Two_Decimal_Format));
                                table.Cell().PaddingLeft(10).Text(record.DeliveryReceipt?.Hauler?.SupplierName);

                                totalVolume += volume;
                                totalCostAmount += costAmountGross;
                                totalVatAmount += costAmountNet * 0.12m;
                                totalWHTAmount += costAmountNet * 0.01m;
                                totalNetPurchases += costAmountNet;
                                totalCOSAmount += cosPricePerLiter * volume;
                                totalGMAmount += cosAmountGross - costAmountGross;
                                totalFCAmount += freightChargePerLiter * volume;
                                totalCommissionAmount += volume * commissionPerLiter;
                                totalNetMarginAmount += (gmPerLiter - freightChargePerLiter) * volume;
                            }

                            var totalCostPerLiter = totalCostAmount / totalVolume;

                            table.Cell().ColumnSpan(21).Element(cell =>
                            {
                                cell.Column(column =>
                                {
                                    column.Item().Height(2); // Top spacing or content
                                    column.Item().Height(1).Background(Colors.Black); // Horizontal line
                                    column.Item().Height(3); // Bottom spacing or content
                                });
                            });

                            table.Cell().ColumnSpan(14).AlignRight().Text("TOTAL:").SemiBold();
                            table.Cell().AlignRight().Text(totalVolume.ToString(SD.Two_Decimal_Format)).SemiBold();
                            table.Cell().AlignRight().Text(totalCostPerLiter.ToString(SD.Two_Decimal_Format)).SemiBold();
                            table.Cell().AlignRight().Text(totalCostAmount.ToString(SD.Two_Decimal_Format)).SemiBold();
                            table.Cell().AlignRight().Text(totalVatAmount.ToString(SD.Two_Decimal_Format)).SemiBold();
                            table.Cell().AlignRight().Text(totalWHTAmount.ToString(SD.Two_Decimal_Format)).SemiBold();
                            table.Cell().AlignRight().Text(totalNetPurchases.ToString(SD.Two_Decimal_Format)).SemiBold();

                            table.Cell().ColumnSpan(21).Element(cell =>
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
                _logger.LogError(ex, "Failed to generate purchase report. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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
                TempData["error"] = "Please input date range";
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
                var purchaseReport = await _unitOfWork.FilprideReport.GetPurchaseReport(model.DateFrom, model.DateTo, companyClaims);

                // check if there is no record
                if (purchaseReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(PurchaseReport));
                }

                #region -- Initialize "total" Variables for operations --

                decimal totalVolume = purchaseReport.Sum(pr => pr.QuantityReceived);
                decimal totalCostPerLiter = 0m;
                decimal totalCostAmount = 0m;
                decimal totalVatAmount = 0m;
                decimal totalWHTAmount = 0m;
                decimal totalNetPurchases = 0m;
                decimal totalCOSPrice = purchaseReport.Sum(pr => (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m));
                decimal totalCOSAmount = 0m;
                decimal totalGMPerLiter = 0m;
                decimal totalGMAmount = 0m;
                decimal totalFreightCharge = purchaseReport.Sum(pr => (pr.DeliveryReceipt?.Freight ?? 0m));
                decimal totalFCAmount = 0m;
                decimal totalCommissionPerLiter =  purchaseReport.Sum(pr => (pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m));
                decimal totalCommissionAmount = 0m;
                decimal totalNetMarginPerLiter = 0m;
                decimal totalNetMarginAmount = 0m;
                decimal totalNetFreight = 0m;
                decimal totalCommission = 0m;

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

                    purchaseReportWorksheet.Cells["A7"].Value = "TRANSACTION DATE";
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
                    purchaseReportWorksheet.Cells["L7"].Value = "SUPPLIER'S SI";
                    purchaseReportWorksheet.Cells["M7"].Value = "SI/LIFTING DATE";
                    purchaseReportWorksheet.Cells["N7"].Value = "SUPPLIER'S DR";
                    purchaseReportWorksheet.Cells["O7"].Value = "SUPPLIER'S WC";
                    purchaseReportWorksheet.Cells["P7"].Value = "CUSTOMER NAME";
                    purchaseReportWorksheet.Cells["Q7"].Value = "PRODUCT";
                    purchaseReportWorksheet.Cells["R7"].Value = "VOLUME";
                    purchaseReportWorksheet.Cells["S7"].Value = "CPL G.VAT";
                    purchaseReportWorksheet.Cells["T7"].Value = "PURCHASES G.VAT";
                    purchaseReportWorksheet.Cells["U7"].Value = "VAT AMOUNT";
                    purchaseReportWorksheet.Cells["V7"].Value = "WHT AMOUNT";
                    purchaseReportWorksheet.Cells["W7"].Value = "HAULER'S NAME";
                    purchaseReportWorksheet.Cells["X7"].Value = "PURCHASES N.VAT";
                    purchaseReportWorksheet.Cells["Y7"].Value = "FREIGHT N.VAT";
                    purchaseReportWorksheet.Cells["Z7"].Value = "COMMISSION";
                    purchaseReportWorksheet.Cells["AA7"].Value = "OTC COS#.";
                    purchaseReportWorksheet.Cells["AB7"].Value = "OTC DR#.";
                    purchaseReportWorksheet.Cells["AC7"].Value = "IS PO#";
                    purchaseReportWorksheet.Cells["AD7"].Value = "IS RR#";

                    #endregion

                    #region -- Apply styling to the header row --

                    using (var range = purchaseReportWorksheet.Cells["A7:AD7"])
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
                    int row = 8; // starting row
                    string currencyFormat = "#,##0.0000"; // numbers format
                    string currencyFormat2 = "#,##0.00"; // numbers format

                    #region -- Populate data rows --

                    foreach (var pr in purchaseReport)
                    {
                        #region -- Variables and Formulas --

                        // calculate values, put in variables to be displayed per cell
                        var volume = pr.QuantityReceived; // volume
                        var costAmount = pr.Amount; // purchase total gross
                        var netPurchases = costAmount / 1.12m; // purchase total net
                        var netFreight = pr.DeliveryReceipt?.Freight / 1.12m ?? 0m; // purchase total net
                        var vatAmount = costAmount * 0.12m; // vat total
                        var whtAmount = netPurchases * 0.01m; // wht total
                        var cosAmount = (pr.QuantityReceived * (pr.DeliveryReceipt?.CustomerOrderSlip?.DeliveredPrice ?? 0m)); // sale total gross
                        var costPerLiter = costAmount / volume; // sale price per liter
                        var commission = ((pr.DeliveryReceipt?.CustomerOrderSlip?.CommissionRate ?? 0m) * volume);

                        #endregion

                        #region -- Assign Values to Cells --

                        purchaseReportWorksheet.Cells[row, 1].Value = pr.Date; // Date
                        purchaseReportWorksheet.Cells[row, 2].Value = pr.DeliveryReceipt?.DeliveredDate; // DeliveredDate
                        purchaseReportWorksheet.Cells[row, 3].Value = pr.PurchaseOrder?.Supplier?.SupplierName; // Supplier Name
                        purchaseReportWorksheet.Cells[row, 4].Value = pr.PurchaseOrder?.Supplier?.SupplierTin; // Supplier Tin
                        purchaseReportWorksheet.Cells[row, 5].Value = pr.PurchaseOrder?.Supplier?.SupplierAddress; // Supplier Address
                        purchaseReportWorksheet.Cells[row, 6].Value = pr.PurchaseOrder?.PurchaseOrderNo; // PO No.
                        purchaseReportWorksheet.Cells[row, 7].Value = pr.ReceivingReportNo ?? pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride RR
                        purchaseReportWorksheet.Cells[row, 8].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.CustomerOrderSlipNo; // COS
                        purchaseReportWorksheet.Cells[row, 9].Value = pr.DeliveryReceipt?.DeliveryReceiptNo; // Filpride DR
                        purchaseReportWorksheet.Cells[row, 10].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.PickUpPoint?.Depot; // Filpride DR
                        purchaseReportWorksheet.Cells[row, 11].Value = pr.DeliveryReceipt?.AuthorityToLoadNo; // ATL #
                        purchaseReportWorksheet.Cells[row, 12].Value = pr.SupplierInvoiceNumber; // Supplier's Sales Invoice
                        purchaseReportWorksheet.Cells[row, 13].Value = pr.SupplierInvoiceDate; // Supplier's Sales Invoice
                        purchaseReportWorksheet.Cells[row, 14].Value = pr.SupplierDrNo; // Supplier's DR
                        purchaseReportWorksheet.Cells[row, 15].Value = pr.WithdrawalCertificate; // Supplier's WC
                        purchaseReportWorksheet.Cells[row, 16].Value = pr.DeliveryReceipt?.Customer?.CustomerName; // Customer Name
                        purchaseReportWorksheet.Cells[row, 17].Value = pr.PurchaseOrder?.Product?.ProductName; // Product
                        purchaseReportWorksheet.Cells[row, 18].Value = volume; // Volume
                        purchaseReportWorksheet.Cells[row, 19].Value = costPerLiter; // Purchase price per liter
                        purchaseReportWorksheet.Cells[row, 20].Value = costAmount; // Purchase total gross
                        purchaseReportWorksheet.Cells[row, 21].Value = vatAmount; // Vat total
                        purchaseReportWorksheet.Cells[row, 22].Value = whtAmount; // WHT total
                        purchaseReportWorksheet.Cells[row, 23].Value = pr.DeliveryReceipt?.Hauler?.SupplierName; // Hauler's Name
                        purchaseReportWorksheet.Cells[row, 24].Value = netPurchases; // Purchase total net ======== move to third last
                        purchaseReportWorksheet.Cells[row, 25].Value = netFreight; // freight n vat ============
                        purchaseReportWorksheet.Cells[row, 26].Value = commission; // commission =========
                        purchaseReportWorksheet.Cells[row, 27].Value = pr.DeliveryReceipt?.CustomerOrderSlip?.OldCosNo; // OTC COS =========
                        purchaseReportWorksheet.Cells[row, 28].Value = pr.DeliveryReceipt?.ManualDrNo; // OTC DR =========
                        purchaseReportWorksheet.Cells[row, 29].Value = pr.PurchaseOrder?.OldPoNo; // IS PO =========
                        purchaseReportWorksheet.Cells[row, 30].Value = pr.OldRRNo; // IS RR =========

                        #endregion -- Assign Values to Cells --

                        #region -- Add the values to total --

                        totalCostAmount += costAmount;
                        totalVatAmount += vatAmount;
                        totalWHTAmount += whtAmount;
                        totalNetPurchases += netPurchases;
                        totalCOSAmount += cosAmount;
                        totalCommission += commission;

                        #endregion -- Add the values to total and format number cells --

                        #region -- Add format number cells from Assign Values to Cells --

                        purchaseReportWorksheet.Cells[row, 1, row, 2].Style.Numberformat.Format = "MMM/dd/yyyy";
                        purchaseReportWorksheet.Cells[row, 13].Style.Numberformat.Format = "MMM/dd/yyyy";
                        purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                        purchaseReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat2;
                        purchaseReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                        purchaseReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat2;

                        #endregion -- Add format number cells from Assign Values to Cells --

                        row++;
                    }

                    #endregion -- Populate data rows --

                    #region -- Assign values of other totals and formatting of total cells --

                    totalCostPerLiter = totalCostAmount / totalVolume;
                    totalCOSPrice = totalCOSAmount / totalVolume;
                    totalGMPerLiter = totalGMAmount / totalVolume;
                    totalFreightCharge = totalFCAmount / totalVolume;
                    totalCommissionPerLiter = totalCommissionAmount / totalVolume;
                    totalNetMarginPerLiter = totalNetMarginAmount / totalVolume;

                    purchaseReportWorksheet.Cells[row, 17].Value = "Total: ";
                    purchaseReportWorksheet.Cells[row, 18].Value = totalVolume;
                    purchaseReportWorksheet.Cells[row, 19].Value = totalCostPerLiter;
                    purchaseReportWorksheet.Cells[row, 20].Value = totalCostAmount;
                    purchaseReportWorksheet.Cells[row, 21].Value = totalVatAmount;
                    purchaseReportWorksheet.Cells[row, 22].Value = totalWHTAmount;
                    purchaseReportWorksheet.Cells[row, 24].Value = totalNetPurchases;
                    purchaseReportWorksheet.Cells[row, 25].Value = totalNetFreight;
                    purchaseReportWorksheet.Cells[row, 26].Value = totalCommission;

                    purchaseReportWorksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 21].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 22].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 24].Style.Numberformat.Format = currencyFormat2;
                    purchaseReportWorksheet.Cells[row, 25].Style.Numberformat.Format = currencyFormat;
                    purchaseReportWorksheet.Cells[row, 26].Style.Numberformat.Format = currencyFormat2;

                    #endregion -- Assign values of other totals and formatting of total cells --

                    // Apply style to subtotal rows
                    // color to whole row
                    using (var range = purchaseReportWorksheet.Cells[row, 1, row, 26])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(172, 185, 202));
                    }
                    // line to subtotal values
                    using (var range = purchaseReportWorksheet.Cells[row, 14, row, 26])
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

                    string[] productList = { "DIESEL", "ECONO", "ENVIRO" };

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

                        using (var range = purchaseReportWorksheet.Cells[row, i+1, row, i+3])
                        {
                            range.Style.Font.Bold = true;
                            range.Style.Font.Italic = true;
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                        }
                    }

                    row += 2;

                    #endregion -- Summary Header --

                    #region == Summary Contents ==

                    // query a group by supplier
                    var supplierByRr = purchaseReport
                        .OrderBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName)
                        .GroupBy(rr => rr.PurchaseOrder!.Supplier!.SupplierName);

                    // for each supplier
                    foreach (var rrSupplier in supplierByRr)
                    {
                        int startingColumn = 2;

                        // get name of group supplier
                        purchaseReportWorksheet.Cells[row, 2].Value = rrSupplier.First().PurchaseOrder!.Supplier!.SupplierName;
                        purchaseReportWorksheet.Cells[row, 2].Style.Font.Bold = true;
                        purchaseReportWorksheet.Cells[row, 2].Style.Font.Italic = true;

                        // group each product of supplier
                        var productBySupplier = rrSupplier
                            .OrderBy(p => p.PurchaseOrder!.Product!.ProductName)
                            .GroupBy(rr => rr.PurchaseOrder!.Product!.ProductName);

                        // get volume, net purchases, and average cost per liter
                        foreach (var product in productBySupplier)
                        {
                            if (product.Any())
                            {
                                var grandTotalVolume = product
                                    .Sum(pr => pr.QuantityReceived); // volume
                                var grandTotalPurchaseNet = product
                                    .Sum(pr => (pr.QuantityReceived * pr.PurchaseOrder?.Price ?? 0m) / 1.12m); // Purchase Net Total

                                purchaseReportWorksheet.Cells[row, startingColumn + 1].Value = grandTotalVolume;
                                purchaseReportWorksheet.Cells[row, startingColumn + 2].Value = grandTotalPurchaseNet;
                                purchaseReportWorksheet.Cells[row, startingColumn + 3].Value = (grandTotalVolume != 0m ? grandTotalPurchaseNet / grandTotalVolume : 0m); // Gross Margin Per Liter
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

                    for (int i = 2; i != 11; i += 3)
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

                    var Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 2, lastColumnForThickBorder, 2];
                    Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 3, lastColumnForThickBorder, 5];
                    Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 6, lastColumnForThickBorder, 8];
                    Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    Enclosure = purchaseReportWorksheet.Cells[firstColumnForThickBorder, 9, lastColumnForThickBorder, 11];
                    Enclosure.Style.Border.BorderAround(ExcelBorderStyle.Medium);

                    #endregion == Summary Contents ==

                    #endregion -- Summary Rows --

                    // Auto-fit columns for better readability
                    purchaseReportWorksheet.Cells.AutoFitColumns();
                    purchaseReportWorksheet.View.FreezePanes(8, 1);

                    for (int col = 1; col <= 22; col++)
                    {
                        double currentWidth = purchaseReportWorksheet.Column(col).Width;
                        if (currentWidth > 25)
                        {
                            purchaseReportWorksheet.Column(col).Width = 24;
                        }
                    }

                #endregion -- Purchase Report Worksheet --

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Purchase Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(PurchaseReport));
            }
        }

        #endregion
    }
}
