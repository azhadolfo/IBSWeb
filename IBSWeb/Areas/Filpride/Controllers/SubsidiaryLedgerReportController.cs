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

        private readonly ILogger<SubsidiaryLedgerReportController> _logger;

        public SubsidiaryLedgerReportController(ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<SubsidiaryLedgerReportController> logger)
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
                var worksheet = package.Workbook.Worksheets.Add("TradeFuelReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE FUEL REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = "As of " + monthDate.ToString("MMM yyyy");
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
                        col = 1;
                        cvTradePayments.TryGetValue(item.ReceivingReportId, out var cvTradePayment);

                        var netOfVatAmount = item.PurchaseOrder!.VatType == SD.VatType_Vatable
                            ? NetOfVatOrZero(item.Amount)
                            : item.Amount;

                        var taxPercent = item.TaxPercentage;

                        var withHoldingTaxAmount = item.PurchaseOrder.TaxType == SD.TaxType_WithTax
                            ? EwtAmountOrZero(netOfVatAmount, taxPercent)
                            : 0m;

                        var netOfTax = item.Amount - withHoldingTaxAmount;

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

                        if (cvTradePayment?.CheckVouchers == null)
                        {
                            col += 10;
                            row++;
                            continue;
                        }

                        foreach (var checkVoucher in cvTradePayment.CheckVouchers)
                        {
                            var amountPaid = checkVoucher.AmountPaid;

                            var balance = netOfTax - amountPaid;

                            col++;
                            worksheet.Cells[row, col].Value = "";
                            col++;

                            worksheet.Cells[row, col].Value = checkVoucher.CV.CheckVoucherHeaderNo; col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.CheckNo; col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.DcrDate;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.Payee; col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.Particulars; col++;
                            worksheet.Cells[row, col].Value = checkVoucher.CV.Type; col++;

                            worksheet.Cells[row, col].Value = amountPaid;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = balance;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;

                            subtotalAmountPaid += amountPaid;
                            subtotalBalance += balance;
                        }
                        subtotalGrossOfVat += item.Amount;
                        subtotalNetOfVat += netOfVatAmount;
                        subtotalEwt += withHoldingTaxAmount;
                        subtotalNetOfTax += netOfTax;

                        row++;
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Fuel report excel file", "Subsidiary Ledger Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Fuel_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade fuel report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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

        public async Task<IActionResult> GenerateTradeCommissioneeReportExcelFile(DateOnly monthDate, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeCommissioneeReport));
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

                var deliveryReceiptsGroupBySupplier = await _dbContext.FilprideDeliveryReceipts
                    .Where(x => (x.Status == nameof(DRStatus.ForInvoicing) ||
                                 x.Status == nameof(DRStatus.Invoiced)) &&
                                x.CommissioneeId != null &&
                                x.Date <= monthDate)
                    .Include(x => x.CustomerOrderSlip)
                    .Include(x => x.Commissionee)
                    .GroupBy(x => x.CustomerOrderSlip!.CommissioneeName)
                    .ToListAsync(cancellationToken);
                var payments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "DR" &&
                                x.CV.Status == nameof(Status.Posted) &&
                                x.CV.CvType == nameof(CVType.Commission) &&
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

                if (deliveryReceiptsGroupBySupplier.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeCommissioneeReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TradeCommissioneeReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE COMMISSIONEE REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = "As of " + monthDate.ToString("MMM yyyy");
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "COMMISSIONEE NAME"; col++;
                worksheet.Cells[row, col].Value = "MANUAL DR NO."; col++;
                worksheet.Cells[row, col].Value = "DR NO."; col++;
                worksheet.Cells[row, col].Value = "DR DATE"; col++;
                worksheet.Cells[row, col].Value = "GROSS OF VAT"; col++;
                worksheet.Cells[row, col].Value = "COST OF MONEY"; col++;
                worksheet.Cells[row, col].Value = "NET OF COST OF MONEY"; col++;
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
                             worksheet.Cells[row, 1, row, 10],
                             worksheet.Cells[row, 12, row, col]
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
                var grandTotalCostOfMoney = 0m;
                var grandTotalNetOfCostOfMoney = 0m;
                var grandTotalNetOfTax = 0m;
                var grandTotalNetOfVat = 0m;
                var grandTotalEwt = 0m;
                var grandTotalAmountPaid = 0m;
                var grandTotalBalance = 0m;

                foreach (var deliveryReceipts in deliveryReceiptsGroupBySupplier)
                {
                    var subtotalGrossOfVat = 0m;
                    var subtotalCostOfMoney = 0m;
                    var subtotalNetOfCostOfMoney = 0m;
                    var subtotalNetOfTax = 0m;
                    var subtotalNetOfVat = 0m;
                    var subtotalEwt = 0m;
                    var subtotalAmountPaid = 0m;
                    var subtotalBalance = 0m;

                    foreach (var item in deliveryReceipts)
                    {
                        cvTradePayments.TryGetValue(item.DeliveryReceiptId, out var cvTradePayment);

                        var costOfMoney = (item.Quantity * item.CommissionRate) - item.CommissionAmount;
                        var netOfCostOfMoney = item.CommissionAmount;
                        var grossAmount = netOfCostOfMoney + costOfMoney;

                        var netOfVatAmount = item.CustomerOrderSlip!.CommissioneeVatType == SD.VatType_Vatable
                            ? NetOfVatOrZero(grossAmount)
                            : grossAmount;

                        var taxPercent = item.Commissionee?.WithholdingTaxPercent ?? 0m;

                        var withHoldingTaxAmount = item.CustomerOrderSlip.CommissioneeTaxType == SD.TaxType_WithTax
                            ? EwtAmountOrZero(netOfVatAmount, taxPercent)
                            : 0m;

                        var netOfTax = item.CommissionAmount - withHoldingTaxAmount;

                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;

                            var balance = (netOfTax + costOfMoney) - amountPaid;

                            worksheet.Cells[row, col].Value = item.CustomerOrderSlip.CommissioneeName; col++;
                            worksheet.Cells[row, col].Value = item.ManualDrNo; col++;
                            worksheet.Cells[row, col].Value = item.DeliveryReceiptNo; col++;
                            worksheet.Cells[row, col].Value = item.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = grossAmount;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = costOfMoney;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = netOfCostOfMoney;
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

                            worksheet.Cells[row, col].Value = checkVoucher != null ? amountPaid : 0m;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = balance;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;

                            subtotalGrossOfVat += grossAmount;
                            subtotalCostOfMoney += costOfMoney;
                            subtotalNetOfCostOfMoney += netOfCostOfMoney;
                            subtotalNetOfVat += netOfVatAmount;
                            subtotalEwt += withHoldingTaxAmount;
                            subtotalNetOfTax += netOfTax;
                            subtotalAmountPaid += amountPaid;
                            subtotalBalance += balance;

                            row++;
                        }
                    }

                    worksheet.Cells[row, 1].Value = $"SUBTOTAL: {deliveryReceipts.Key}";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 5].Value = subtotalGrossOfVat;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 6].Value = subtotalCostOfMoney;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 7].Value = subtotalNetOfCostOfMoney;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Value = subtotalNetOfVat;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Value = subtotalEwt;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Value = subtotalNetOfTax;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 19].Value = subtotalAmountPaid;
                    worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 20].Value = subtotalBalance;
                    worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                    foreach (var range in new[]
                             {
                                 worksheet.Cells[row, 1, row, 10],
                                 worksheet.Cells[row, 12, row, col]
                             })
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(252, 228, 214));
                    }

                    grandTotalGrossOfVat += subtotalGrossOfVat;
                    grandTotalCostOfMoney += subtotalCostOfMoney;
                    grandTotalNetOfCostOfMoney += subtotalNetOfCostOfMoney;
                    grandTotalNetOfTax += subtotalNetOfTax;
                    grandTotalNetOfVat += subtotalNetOfVat;
                    grandTotalEwt += subtotalEwt;
                    grandTotalAmountPaid += subtotalAmountPaid;
                    grandTotalBalance += subtotalBalance;

                    row++;
                }

                worksheet.Cells[row, 1].Value = "GRAND TOTAL:";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 5].Value = grandTotalGrossOfVat;
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Value = grandTotalCostOfMoney;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 7].Value = grandTotalNetOfCostOfMoney;
                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 8].Value = grandTotalNetOfVat;
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Value = grandTotalEwt;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Value = grandTotalNetOfTax;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 19].Value = grandTotalAmountPaid;
                worksheet.Cells[row, 19].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 20].Value = grandTotalBalance;
                worksheet.Cells[row, 20].Style.Numberformat.Format = currencyFormat;

                foreach (var range in new[]
                         {
                             worksheet.Cells[row, 1, row, 10],
                             worksheet.Cells[row, 12, row, col]
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Commissionee report excel file", "Subsidiary Ledger Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Commissionee_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade commissionee report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
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

        public async Task<IActionResult> GenerateTradeHaulerOrFreightReportExcelFile(DateOnly monthDate, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please input date range";
                return RedirectToAction(nameof(TradeHaulerOrFreightReport));
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

                var deliveryReceiptsGroupBySupplier = await _dbContext.FilprideDeliveryReceipts
                    .Where(x => (x.Status == nameof(DRStatus.ForInvoicing) ||
                                 x.Status == nameof(DRStatus.Invoiced)) &&
                                x.HaulerId != null &&
                                x.Date <= monthDate)
                    .Include(x => x.Hauler)
                    .GroupBy(x => x.HaulerName)
                    .ToListAsync(cancellationToken);
                var payments = await _dbContext.FilprideCVTradePayments
                    .Where(x => x.DocumentType == "DR" &&
                                x.CV.Status == nameof(Status.Posted) &&
                                x.CV.CvType == nameof(CVType.Hauler) &&
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

                if (deliveryReceiptsGroupBySupplier.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(TradeHaulerOrFreightReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TradeHaulerOrFreightReport");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "TRADE HAULER/FREIGHT REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Generated By:";
                worksheet.Cells["A4"].Value = "Company:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = "As of " + monthDate.ToString("MMM yyyy");
                worksheet.Cells["B3"].Value = $"{extractedBy}";
                worksheet.Cells["B4"].Value = $"{companyClaims}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                int row = 7;
                int col = 1;

                worksheet.Cells[row, col].Value = "HAULER NAME"; col++;
                worksheet.Cells[row, col].Value = "MANUAL DR NO."; col++;
                worksheet.Cells[row, col].Value = "DR NO."; col++;
                worksheet.Cells[row, col].Value = "DR DATE"; col++;
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
                             worksheet.Cells[row, 1, row, 8],
                             worksheet.Cells[row, 10, row, col]
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

                foreach (var deliveryReceipts in deliveryReceiptsGroupBySupplier)
                {
                    var subtotalGrossOfVat = 0m;
                    var subtotalNetOfTax = 0m;
                    var subtotalNetOfVat = 0m;
                    var subtotalEwt = 0m;
                    var subtotalAmountPaid = 0m;
                    var subtotalBalance = 0m;

                    foreach (var item in deliveryReceipts)
                    {
                        cvTradePayments.TryGetValue(item.DeliveryReceiptId, out var cvTradePayment);

                        var netOfVatAmount = item.HaulerVatType == SD.VatType_Vatable
                            ? NetOfVatOrZero(item.FreightAmount)
                            : item.FreightAmount;

                        var taxPercent = item.Hauler?.WithholdingTaxPercent ?? 0m;

                        var withHoldingTaxAmount = item.HaulerTaxType == SD.TaxType_WithTax
                            ? EwtAmountOrZero(netOfVatAmount, taxPercent)
                            : 0m;

                        var netOfTax = item.FreightAmount - withHoldingTaxAmount;

                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;

                            var balance = netOfTax - amountPaid;

                            worksheet.Cells[row, col].Value = item.HaulerName; col++;
                            worksheet.Cells[row, col].Value = item.ManualDrNo; col++;
                            worksheet.Cells[row, col].Value = item.DeliveryReceiptNo; col++;
                            worksheet.Cells[row, col].Value = item.Date;
                            worksheet.Cells[row, col].Style.Numberformat.Format = "MMM/dd/yyyy";
                            col++;
                            worksheet.Cells[row, col].Value = item.FreightAmount;
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

                            worksheet.Cells[row, col].Value = checkVoucher != null ? amountPaid : 0m;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = balance;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;

                            subtotalGrossOfVat += item.FreightAmount;
                            subtotalNetOfVat += netOfVatAmount;
                            subtotalEwt += withHoldingTaxAmount;
                            subtotalNetOfTax += netOfTax;
                            subtotalAmountPaid += amountPaid;
                            subtotalBalance += balance;

                            row++;
                        }
                    }

                    worksheet.Cells[row, 1].Value = $"SUBTOTAL: {deliveryReceipts.Key}";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    worksheet.Cells[row, 5].Value = subtotalGrossOfVat;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 6].Value = subtotalNetOfVat;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 7].Value = subtotalEwt;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 8].Value = subtotalNetOfTax;
                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 17].Value = subtotalAmountPaid;
                    worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 18].Value = subtotalBalance;
                    worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;

                    foreach (var range in new[]
                             {
                                 worksheet.Cells[row, 1, row, 8],
                                 worksheet.Cells[row, 10, row, col]
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
                worksheet.Cells[row, 5].Value = grandTotalGrossOfVat;
                worksheet.Cells[row, 5].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 6].Value = grandTotalNetOfVat;
                worksheet.Cells[row, 6].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 7].Value = grandTotalEwt;
                worksheet.Cells[row, 7].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 8].Value = grandTotalNetOfTax;
                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 17].Value = grandTotalAmountPaid;
                worksheet.Cells[row, 17].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 18].Value = grandTotalBalance;
                worksheet.Cells[row, 18].Style.Numberformat.Format = currencyFormat;

                foreach (var range in new[]
                         {
                             worksheet.Cells[row, 1, row, 8],
                             worksheet.Cells[row, 10, row, col]
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Hauler/Freight report excel file", "Subsidiary Ledger Report", companyClaims);
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                var fileName = $"Trade_Hauler_or_Freight_Report_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx";
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate trade hauler/freight report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(TradeHaulerOrFreightReport));
            }
        }

        #endregion
    }
}
