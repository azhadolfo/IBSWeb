using System.Security.Claims;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.Books;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Color = System.Drawing.Color;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SubsidiaryLedgerReportController: Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<GeneralLedgerReportController> _logger;

        public SubsidiaryLedgerReportController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<GeneralLedgerReportController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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

        [HttpGet]
        public IActionResult TradeFuelReport()
        {
            return View();
        }

        #region -- Generate Trade Fuel Report as Excel File --

        public async Task<IActionResult> GenerateTradeFuelReportExcelFile(DateOnly monthDate, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeFuelReport));
            }

            try
            {
                monthDate = monthDate.AddMonths(1).AddDays(-1);
                var extractedBy = GetUserFullName();
                var companyClaims = await GetCompanyClaimAsync();

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var receivingReportsGroupBySupplier = await _dbContext.FilprideReceivingReports
                    .Where(x => x.Status == nameof(Status.Posted) &&
                                x.Date <= monthDate)
                    .Include(x => x.PurchaseOrder)
                    .GroupBy(x => x.PurchaseOrder!.SupplierName)
                    .ToListAsync(cancellationToken);
                var payments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "RR" &&
                                x.CV.Status == nameof(Status.Posted) &&
                                x.CV.Date <= monthDate)
                    .Include(x => x.CV)
                    .ToListAsync(cancellationToken);

                var cvTradePayments = payments
                    .GroupBy(x => x.DocumentId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            DocumentId = g.Key,
                            g.First().DocumentType,
                            CheckVouchers = g
                                .Select(x => new
                                {
                                    x.CV,
                                    x.AmountPaid
                                })
                                .ToList()
                        });

                if (receivingReportsGroupBySupplier.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeFuelReport));
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
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"As of " + monthDate.ToString("MMM yyyy");
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "SUPPLIER NAME"; col++;
                worksheet.Cells[row, col].Value = "SI NO."; col++;
                worksheet.Cells[row, col].Value = "SUPPLIERS PO NO."; col++;
                worksheet.Cells[row, col].Value = "RR NO."; col++;
                worksheet.Cells[row, col].Value = "RR DATE"; col++;
                worksheet.Cells[row, col].Value = "GROSS OF VAT"; col++;
                worksheet.Cells[row, col].Value = "NET OF VAT"; col++;
                worksheet.Cells[row, col].Value = "EWT"; col++;
                worksheet.Cells[row, col].Value = "NET OF TAX"; col++;
                worksheet.Cells[row, col].Value = ""; col++;
                worksheet.Cells[row, col].Value = "CV NO."; col++;
                worksheet.Cells[row, col].Value = "CV DATE"; col++;
                worksheet.Cells[row, col].Value = "CHECK #"; col++;
                worksheet.Cells[row, col].Value = "CLEARED DATE"; col++;
                worksheet.Cells[row, col].Value = "PAYEE"; col++;
                worksheet.Cells[row, col].Value = "PARTICULARS"; col++;
                worksheet.Cells[row, col].Value = "DOCUMENT TYPE"; col++;
                worksheet.Cells[row, col].Value = "AMOUNT PAID";col++;
                worksheet.Cells[row, col].Value = "BALANCE";

                foreach (var range in new[]
                         {
                             worksheet.Cells[row, 1, row, 9],
                             worksheet.Cells[row, 11, row, col]
                         })
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
                var grandTotalGrossOfVat = 0m;
                var grandTotalNetOfTax = 0m;
                var grandTotalNetOfVat = 0m;
                var grandTotalEwt = 0m;
                var grandTotalAmountPaid = 0m;
                var grandTotalBalance = 0m;

                foreach (var receivingReports in receivingReportsGroupBySupplier)
                {
                    var subtotalGrossOfVat = 0m;
                    var subtotalNetOfTax = 0m;
                    var subtotalNetOfVat = 0m;
                    var subtotalEwt = 0m;
                    var subtotalAmountPaid = 0m;
                    var subtotalBalance = 0m;

                    foreach (var item in receivingReports)
                    {
                        cvTradePayments.TryGetValue(item.ReceivingReportId, out var cvTradePayment);

                        var netOfVatAmount = item.PurchaseOrder!.VatType == SD.VatType_Vatable
                            ? NetOfVatOrZero(item.Amount)
                            : item.Amount;

                        var taxPercent = item.TaxPercentage;

                        var withHoldingTaxAmount = item.PurchaseOrder.TaxType == SD.TaxType_WithTax
                            ? EwtAmountOrZero(netOfVatAmount, taxPercent)
                            : 0m;

                        var netOfTax = item.Amount - withHoldingTaxAmount;

                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;

                            var balance = netOfTax - amountPaid;

                            worksheet.Cells[row, col].Value = item.PurchaseOrder.SupplierName; col++;
                            worksheet.Cells[row, col].Value = item.SupplierInvoiceNumber; col++;
                            worksheet.Cells[row, col].Value = item.PONo; col++;
                            worksheet.Cells[row, col].Value = item.ReceivingReportNo; col++;
                            worksheet.Cells[row, col].Value = item.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = item.Amount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = netOfVatAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = withHoldingTaxAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = netOfTax;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = "";
                            col++;

                            worksheet.Cells[row, col].Value = checkVoucher?.CV.CheckVoucherHeaderNo; col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.CheckNo; col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.DcrDate;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.Payee; col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.Particulars; col++;
                            worksheet.Cells[row, col].Value = checkVoucher?.CV.Type; col++;

                            worksheet.Cells[row, col].Value = checkVoucher != null ? amountPaid : "";
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = balance;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;

                            subtotalGrossOfVat += item.Amount;
                            subtotalNetOfVat += netOfVatAmount;
                            subtotalEwt += withHoldingTaxAmount;
                            subtotalNetOfTax += netOfTax;
                            subtotalAmountPaid += amountPaid;
                            subtotalBalance += balance;

                            row++;
                        }
                    }

                    worksheet.Cells[row, 1].Value = $"SUBTOTAL: {receivingReports.Key}";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 6].Value = subtotalGrossOfVat;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 7].Value = subtotalNetOfVat;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Value = subtotalEwt;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Value = subtotalNetOfTax;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 18].Value = subtotalAmountPaid;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 19].Value = subtotalBalance;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;

                    foreach (var range in new[]
                             {
                                 worksheet.Cells[row, 1, row, 9],
                                 worksheet.Cells[row, 11, row, col]
                             })
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 228, 214));
                    }

                    grandTotalGrossOfVat += subtotalGrossOfVat;
                    grandTotalNetOfTax += subtotalNetOfTax;
                    grandTotalNetOfVat += subtotalNetOfVat;
                    grandTotalEwt += subtotalEwt;
                    grandTotalAmountPaid += subtotalAmountPaid;
                    grandTotalBalance += subtotalBalance;

                    row++;
                }

                worksheet.Cells[row, 1].Value = "GRAND TOTAL:";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 6].Value = grandTotalGrossOfVat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 7].Value = grandTotalNetOfVat;
                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 8].Value = grandTotalEwt;
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Value = grandTotalNetOfTax;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 18].Value = grandTotalAmountPaid;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 19].Value = grandTotalBalance;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;

                foreach (var range in new[]
                         {
                             worksheet.Cells[row, 1, row, 9],
                             worksheet.Cells[row, 11, row, col]
                         })
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Double;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 228, 214));
                }

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
                return RedirectToAction(nameof(TradeFuelReport));
            }
        }

        #endregion


        [HttpGet]
        public IActionResult TradeCommissioneeReport()
        {
            return View();
        }

        #region -- Generate Trade Commissionee Report as Excel File --

        public async Task<IActionResult> GenerateTradeCommissioneeReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeCommissioneeReport));
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
                    return RedirectToAction(nameof(TradeCommissioneeReport));
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
                return RedirectToAction(nameof(TradeCommissioneeReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult TradeHaulerOrFreightReport()
        {
            return View();
        }

        #region -- Generate Trade Hauler or Freight Report as Excel File --

        public async Task<IActionResult> GenerateTradeHaulerOrFreightReportExcelFile(ViewModelBook model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeHaulerOrFreightReport));
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
                    return RedirectToAction(nameof(TradeHaulerOrFreightReport));
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
                return RedirectToAction(nameof(TradeHaulerOrFreightReport));
            }
        }

        #endregion
    }
}
