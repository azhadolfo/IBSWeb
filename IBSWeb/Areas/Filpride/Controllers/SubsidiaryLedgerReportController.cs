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
using Microsoft.AspNetCore.Mvc.Rendering;
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

        private const string _apNonTradeAccount = "201020200";
        private const string _apTradeAccount = "201010100";
        private const string _arTradeAccount = "101020100";
        private const string _advancesToSupplierAccount = "101060100";
        private const string _advancesToEmployeeAccount = "101020400";

        private static readonly string[] _supplierEntries =
        [
            "501010100",
            "501010200",
            "501010300",
            "101040100",
            "101040200",
            "101040300"
        ];

        private static readonly string[] _haulerEntries =
        [
            "502010100",
            "502010200",
            "502010300",
            "101060200",
            "201030220"
        ];

        private static readonly string[] _commissionEntries =
        [
            "503010100",
            "503010200",
            "503010300",
            "201030240"
        ];

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

                    foreach (var item in receivingReports
                                 .OrderBy(x => x.Date)
                                 .ThenBy(x => x.ReceivingReportNo)
                                 .ThenBy(x => x.ReceivingReportId))
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
                        var balance = 0m;

                        List<(int receivingReportId, string receivingReportNo, decimal balance)> rrAmountPaidList = new();
                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers
                                     .OrderBy(x => x.CV.Date)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderNo)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderId) ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;
                            var runningBalances = rrAmountPaidList
                                .Where(x => x.receivingReportNo == item.ReceivingReportNo)
                                .OrderByDescending(x => x.receivingReportId)
                                .Select(x => x.balance)
                                .FirstOrDefault();
                            if (runningBalances != 0m)
                            {
                                balance = runningBalances - amountPaid;
                            }
                            else
                            {
                                balance = netOfTax - amountPaid;
                            }

                            rrAmountPaidList.Add((item.ReceivingReportId,
                                    item.ReceivingReportNo!,
                                    balance
                                ));

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

                            worksheet.Cells[row, col].Value = checkVoucher != null ? amountPaid : 0m;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;
                            col++;
                            worksheet.Cells[row, col].Value = balance;
                            worksheet.Cells[row, col].Style.Numberformat.Format = currencyFormat;

                            subtotalAmountPaid += amountPaid;
                            row++;
                        }
                        subtotalGrossOfVat += item.Amount;
                        subtotalNetOfVat += netOfVatAmount;
                        subtotalEwt += withHoldingTaxAmount;
                        subtotalNetOfTax += netOfTax;
                        subtotalBalance += balance;
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Fuel report excel file", "Subsidiary Ledger Report");
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

                    foreach (var item in deliveryReceipts
                                 .OrderBy(x => x.Date)
                                 .ThenBy(x => x.DeliveryReceiptNo)
                                 .ThenBy(x => x.DeliveryReceiptId))
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
                        var balance = 0m;

                        List<(int deliveryReceiptId, string deliveryReceiptNo, decimal balance)> drAmountPaidList = new();
                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers
                                     .OrderBy(x => x.CV.Date)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderNo)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderId)
                                                      ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;
                            var runningBalances = drAmountPaidList
                                .Where(x => x.deliveryReceiptNo == item.DeliveryReceiptNo)
                                .OrderByDescending(x => x.deliveryReceiptId)
                                .Select(x => x.balance)
                                .FirstOrDefault();

                            if (runningBalances != 0m)
                            {
                                balance = (runningBalances + costOfMoney) - amountPaid;
                            }
                            else
                            {
                                balance = (netOfTax + costOfMoney) - amountPaid;
                            }

                            drAmountPaidList.Add((item.DeliveryReceiptId,
                                    item.DeliveryReceiptNo,
                                    balance
                                ));

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

                            subtotalAmountPaid += amountPaid;
                            row++;
                        }
                        subtotalGrossOfVat += grossAmount;
                        subtotalCostOfMoney += costOfMoney;
                        subtotalNetOfCostOfMoney += netOfCostOfMoney;
                        subtotalNetOfVat += netOfVatAmount;
                        subtotalEwt += withHoldingTaxAmount;
                        subtotalNetOfTax += netOfTax;
                        subtotalBalance += balance;
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Commissionee report excel file", "Subsidiary Ledger Report");
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

                    foreach (var item in deliveryReceipts
                                 .OrderBy(x => x.Date)
                                 .ThenBy(x => x.DeliveryReceiptNo)
                                 .ThenBy(x => x.DeliveryReceiptId))
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
                        var balance = 0m;

                        List<(int deliveryReceiptId, string deliveryReceiptNo, decimal balance)> drAmountPaidList = new();
                        foreach (var checkVoucher in (cvTradePayment?.CheckVouchers
                                     .OrderBy(x => x.CV.Date)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderNo)
                                     .ThenBy(x => x.CV.CheckVoucherHeaderId)
                                                      ?? Enumerable.Empty<dynamic>()).DefaultIfEmpty())
                        {
                            col = 1;
                            var amountPaid = checkVoucher?.AmountPaid ?? 0m;
                            var runningBalances = drAmountPaidList
                                .Where(x => x.deliveryReceiptNo == item.DeliveryReceiptNo)
                                .OrderByDescending(x => x.deliveryReceiptId)
                                .Select(x => x.balance)
                                .FirstOrDefault();

                            if (runningBalances != 0m)
                            {
                                balance = runningBalances - amountPaid;
                            }
                            else
                            {
                                balance = netOfTax - amountPaid;
                            }

                            drAmountPaidList.Add((item.DeliveryReceiptId,
                                    item.DeliveryReceiptNo,
                                    balance
                                ));

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

                            subtotalAmountPaid += amountPaid;
                            row++;
                        }
                        subtotalGrossOfVat += item.FreightAmount;
                        subtotalNetOfVat += netOfVatAmount;
                        subtotalEwt += withHoldingTaxAmount;
                        subtotalNetOfTax += netOfTax;
                        subtotalBalance += balance;
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

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate Trade Hauler/Freight report excel file", "Subsidiary Ledger Report");
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

        #region -- Generate Subsidiary Ledger as Excel File

        [HttpGet]
        public async Task<IActionResult> SubsidiaryLedgerReport()
        {
            var viewModel = new SubsidiaryLedgerReportViewModel
            {
                ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .IgnoreQueryFilters()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSubsidiaryLedgerExcelFile(SubsidiaryLedgerReportViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning"] = "Please complete the form all inputs are required";
                return RedirectToAction(nameof(SubsidiaryLedgerReport));
            }

            model.ChartOfAccounts = await _dbContext.FilprideChartOfAccounts
                .IgnoreQueryFilters()
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName, Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var companyClaims = await GetCompanyClaimAsync();

                if (dateFrom > dateTo)
                {
                    throw new ArgumentException("Date From must not be greater than Date To!");
                }

                if (companyClaims == null)
                {
                    return BadRequest();
                }

                var selectedAccountNo = model.AccountNo
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                var selectedAccount = await _unitOfWork.FilprideChartOfAccount
                    .GetAsyncIgnoreQueryFilters(coa => selectedAccountNo != null && coa.AccountNumber == selectedAccountNo, cancellationToken);

                if (selectedAccount == null)
                {
                    TempData["warning"] = "Selected Account is Null";
                    return RedirectToAction(nameof(SubsidiaryLedgerReport));
                }
                var subsidiaryLedgerByAccountNo = await _dbContext.FilprideGeneralLedgerBooks
                    .Where(g =>
                        g.Date >= dateFrom && g.Date <= dateTo &&
                        g.AccountNo == selectedAccount.AccountNumber &&
                        g.SubAccountId.HasValue && g.SubAccountType.HasValue &&
                        true)
                    .ToListAsync(cancellationToken);

                if (subsidiaryLedgerByAccountNo.Count == 0)
                {
                    TempData["info"] = "No Record Found";
                    return RedirectToAction(nameof(SubsidiaryLedgerReport));
                }

                var accountNumbers = subsidiaryLedgerByAccountNo
                    .Select(g => g.AccountNo)
                    .Where(a => !string.IsNullOrEmpty(a))
                    .Distinct()
                    .ToList();

                var accounts = await _unitOfWork.FilprideChartOfAccount
                    .GetAllAsyncIgnoreQueryFilters(a => accountNumbers.Contains(a.AccountNumber!), cancellationToken);

                var accountDictionary = accounts
                    .Where(a => !string.IsNullOrEmpty(a.AccountNumber))
                    .ToDictionary(a => a.AccountNumber!, a => a);

                var previousPeriodEndDate = new DateOnly(
                    dateFrom.Year,
                    dateFrom.Month,
                    1
                ).AddDays(-1);
                var glSubAccountBalances = await _dbContext.FilprideGlSubAccountBalances
                    .IgnoreQueryFilters()
                    .Include(g => g.Account)
                    .Where(pb => accountNumbers.Contains(pb.Account.AccountNumber!) &&
                                 pb.IsValid &&
                                 pb.PeriodEndDate == previousPeriodEndDate)
                    .ToListAsync(cancellationToken);

                var beginningBalanceDictionary = glSubAccountBalances
                    .GroupBy(x => new
                    {
                        x.AccountId,
                        x.SubAccountId,
                        x.SubAccountType,
                        x.SubAccountName,
                    })
                    .ToDictionary(
                        g => g.Key.AccountId + "_" + g.Key.SubAccountType + "_" + g.Key.SubAccountId + "_" + g.Key.SubAccountName,
                        g => g.Select(pb => pb.EndingBalance).ToList()
                    );
                var subAccountNames = await ResolveSubAccountNamesAsync(subsidiaryLedgerByAccountNo, cancellationToken);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("SubsidiaryLedger");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                mergedCells.Value = "SUBSIDIARY LEDGER";
                mergedCells.Style.Font.Size = 13;
                mergedCells.Style.Font.Bold = true;

                worksheet.Cells["A2"].Value = "Date Range:";
                worksheet.Cells["A3"].Value = "Account No:";
                worksheet.Cells["A4"].Value = "Account Name:";
                worksheet.Cells["A5"].Value = "Date and Time Generated:";

                worksheet.Cells["B2"].Value = $"{dateFrom:yyyy-MM-dd} - {dateTo:yyyy-MM-dd}";
                worksheet.Cells["B3"].Value = $"{selectedAccount.AccountNumber}";
                worksheet.Cells["B4"].Value = $"{selectedAccount.AccountName}";
                worksheet.Cells["B5"].Value = $"{DateTimeHelper.GetCurrentPhilippineTime()}";

                worksheet.Cells["A7"].Value = "Date";
                worksheet.Cells["B7"].Value = "Module";
                worksheet.Cells["C7"].Value = "Reference";
                worksheet.Cells["D7"].Value = "Particular";
                worksheet.Cells["E7"].Value = "Account No";
                worksheet.Cells["F7"].Value = "Account Name";
                worksheet.Cells["G7"].Value = "Sub-Account";
                worksheet.Cells["H7"].Value = "Debit";
                worksheet.Cells["I7"].Value = "Credit";
                worksheet.Cells["J7"].Value = "Month to Date";
                worksheet.Cells["K7"].Value = "Running Balance";

                using (var range = worksheet.Cells["A7:K7"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                int row = 8;
                string currencyFormat = "#,##0.00";
                decimal totalDebit = 0;
                decimal totalCredit = 0;
                decimal totalMtd = 0;
                decimal finalBalance = 0;

                var accountBalances = new Dictionary<int, decimal>();

                foreach (var grouped in subsidiaryLedgerByAccountNo
                    .Where(g => !string.IsNullOrEmpty(g.AccountNo))
                    .OrderBy(g => g.SubAccountName)
                    .GroupBy(g => new
                    {
                        g.AccountId,
                        g.SubAccountId,
                        g.SubAccountType,
                        g.SubAccountName,
                        g.AccountNo
                    }))
                {
                    var accountId = grouped.Key.AccountId;
                    var subAccountType = grouped.Key.SubAccountType;
                    var subAccountId = grouped.Key.SubAccountId ?? 0;
                    var subAccountName = grouped.Key.SubAccountName;
                    var accountNo = grouped.Key.AccountNo;

                    var accountBeginningBalance = beginningBalanceDictionary
                        .GetValueOrDefault(accountId + "_" + subAccountType + "_" + subAccountId + "_" + subAccountName)?
                        .Sum() ?? 0m;

                    // Initialize running balance for this account
                    accountBalances[subAccountId] = accountBeginningBalance;

                    // Get account details from dictionary
                    var account = accountDictionary.TryGetValue(accountNo, out var value)
                        ? value
                        : null;

                    var isDebitAccount = account?.NormalBalance == nameof(NormalBalance.Debit);

                    // Add beginning balance row for this account
                    worksheet.Cells[row, 4].Value = "Beginning Balance";
                    worksheet.Cells[row, 5].Value = accountNo;
                    worksheet.Cells[row, 6].Value = account?.AccountName;
                    worksheet.Cells[row, 11].Value = accountBeginningBalance;
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                    using (var range = worksheet.Cells[row, 1, row, 11])
                    {
                        range.Style.Font.Italic = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                    }

                    row++;

                    decimal groupDebit = 0;
                    decimal groupCredit = 0;
                    decimal groupMtd = 0;

                    foreach (var journal in grouped.OrderBy(g => g.Date))
                    {
                        decimal transaction;

                        if (isDebitAccount)
                        {
                            transaction = journal.Debit - journal.Credit;
                            groupMtd += transaction;
                            accountBalances[subAccountId] += transaction;
                        }
                        else
                        {
                            transaction = journal.Credit - journal.Debit;
                            groupMtd += transaction;
                            accountBalances[subAccountId] += transaction;
                        }

                        worksheet.Cells[row, 1].Value = journal.Date.ToString("dd-MMM-yyyy");
                        worksheet.Cells[row, 2].Value = journal.ModuleType;
                        worksheet.Cells[row, 3].Value = journal.Reference;
                        worksheet.Cells[row, 4].Value = journal.Description;
                        worksheet.Cells[row, 5].Value = journal.AccountNo;
                        worksheet.Cells[row, 6].Value = journal.AccountTitle;
                        worksheet.Cells[row, 7].Value = subAccountNames.GetValueOrDefault(journal.GeneralLedgerBookId);
                        worksheet.Cells[row, 8].Value = journal.Debit;
                        worksheet.Cells[row, 9].Value = journal.Credit;
                        worksheet.Cells[row, 10].Value = groupMtd;
                        worksheet.Cells[row, 11].Value = accountBalances[subAccountId];

                        worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                        groupDebit += journal.Debit;
                        groupCredit += journal.Credit;

                        row++;
                    }

                    // Subtotal for this account
                    worksheet.Cells[row, 6].Value = "Sub Total:";
                    worksheet.Cells[row, 7].Value = grouped.Key.SubAccountName;
                    worksheet.Cells[row, 8].Value = groupDebit;
                    worksheet.Cells[row, 9].Value = groupCredit;
                    worksheet.Cells[row, 10].Value = groupMtd;
                    worksheet.Cells[row, 11].Value = accountBalances[subAccountId];

                    worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                    worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                    using (var range = worksheet.Cells[row, 1, row, 11])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(172, 185, 202));
                    }

                    totalDebit += groupDebit;
                    totalCredit += groupCredit;
                    totalMtd += groupMtd;
                    finalBalance += accountBalances[subAccountId];

                    row++;
                }

                // Grand total
                using (var range = worksheet.Cells[row, 6, row, 11])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                }

                worksheet.Cells[row, 6].Value = "Grand Total:";
                worksheet.Cells[row, 7].Value = selectedAccount.AccountNumber + " " + selectedAccount.AccountName;
                worksheet.Cells[row, 7].Style.Font.Bold = true;
                worksheet.Cells[row, 8].Value = totalDebit;
                worksheet.Cells[row, 9].Value = totalCredit;
                worksheet.Cells[row, 10].Value = totalMtd;
                worksheet.Cells[row, 11].Value = finalBalance;

                worksheet.Cells[row, 8].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 9].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 10].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 11].Style.Numberformat.Format = currencyFormat;

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                #region -- Audit Trail --

                FilprideAuditTrail auditTrailBook = new(GetUserFullName(), "Generate subsidiary ledger report excel file", "Subsidiary Ledger Report");
                await _unitOfWork.FilprideAuditTrail.AddAsync(auditTrailBook, cancellationToken);

                #endregion -- Audit Trail --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"SubsidiaryLedger_{DateTimeHelper.GetCurrentPhilippineTime():yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Failed to generate subsidiary ledger report excel file. Error: {ErrorMessage}, Stack: {StackTrace}. Generated by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                return RedirectToAction(nameof(SubsidiaryLedgerReport));
            }
        }

        #endregion -- Generate Subsidiary Ledger as Excel File

        private async Task<Dictionary<int, string?>> ResolveSubAccountNamesAsync(
            IReadOnlyCollection<FilprideGeneralLedgerBook> generalLedgerBooks,
            CancellationToken cancellationToken)
        {
            var salesDrReferences = generalLedgerBooks
                .Where(gl => gl.ModuleType == nameof(ModuleType.Sales) && gl.Reference.StartsWith("DR"))
                .Select(gl => gl.Reference)
                .Distinct()
                .ToList();

            var deliveryReceipts = salesDrReferences.Count == 0
                ? new Dictionary<string, DeliveryReceiptSubAccountNames>()
                : await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => salesDrReferences.Contains(dr.DeliveryReceiptNo))
                    .Select(dr => new
                    {
                        dr.DeliveryReceiptNo,
                        dr.CustomerOrderSlip!.CustomerName,
                        dr.HaulerName,
                        dr.PurchaseOrder!.SupplierName,
                        dr.CustomerOrderSlip!.CommissioneeName
                    })
                    .ToDictionaryAsync(
                        dr => dr.DeliveryReceiptNo,
                        dr => new DeliveryReceiptSubAccountNames(
                            dr.CustomerName,
                            dr.HaulerName,
                            dr.SupplierName,
                            dr.CommissioneeName),
                        cancellationToken);
            var generalLedgerBooksByReference = generalLedgerBooks.ToLookup(gl => gl.Reference);

            return generalLedgerBooks.ToDictionary(
                gl => gl.GeneralLedgerBookId,
                gl => ResolveSubAccountName(gl, generalLedgerBooksByReference, deliveryReceipts));
        }

        private sealed record DeliveryReceiptSubAccountNames(
            string CustomerName,
            string? HaulerName,
            string SupplierName,
            string? CommissioneeName);

        private static string? ResolveSubAccountName(
            FilprideGeneralLedgerBook gl,
            ILookup<string, FilprideGeneralLedgerBook> generalLedgerBooksByReference,
            IReadOnlyDictionary<string, DeliveryReceiptSubAccountNames> deliveryReceipts)
        {
            if (!string.IsNullOrWhiteSpace(gl.SubAccountName))
            {
                return gl.SubAccountName;
            }

            return gl.ModuleType switch
            {
                nameof(ModuleType.Disbursement) => ResolveDisbursementSubAccountName(gl, generalLedgerBooksByReference),
                nameof(ModuleType.Purchase) => FindSubAccountName(generalLedgerBooksByReference, gl.Reference, _apTradeAccount),
                nameof(ModuleType.Sales) => ResolveSalesSubAccountName(gl, generalLedgerBooksByReference, deliveryReceipts),
                nameof(ModuleType.Collection) => FindSubAccountName(generalLedgerBooksByReference, gl.Reference, _arTradeAccount),
                _ => null
            };
        }

        private static string? ResolveDisbursementSubAccountName(
            FilprideGeneralLedgerBook gl,
            ILookup<string, FilprideGeneralLedgerBook> generalLedgerBooksByReference)
        {
            if (gl.Reference.StartsWith("CVN") || gl.Reference.StartsWith("INV"))
            {
                return generalLedgerBooksByReference[gl.Reference]
                    .Where(x =>
                        x.AccountNo == _apNonTradeAccount ||
                        x.AccountNo == _advancesToSupplierAccount ||
                        x.AccountNo == _advancesToEmployeeAccount)
                    .Select(x => x.SubAccountName)
                    .FirstOrDefault();
            }

            return FindSubAccountName(generalLedgerBooksByReference, gl.Reference, _apTradeAccount);
        }

        private static string? FindSubAccountName(
            ILookup<string, FilprideGeneralLedgerBook> generalLedgerBooksByReference,
            string reference,
            string accountNo)
        {
            return generalLedgerBooksByReference[reference]
                .Where(x => x.AccountNo == accountNo)
                .Select(x => x.SubAccountName)
                .FirstOrDefault();
        }

        private static string? ResolveSalesSubAccountName(
            FilprideGeneralLedgerBook gl,
            ILookup<string, FilprideGeneralLedgerBook> generalLedgerBooksByReference,
            IReadOnlyDictionary<string, DeliveryReceiptSubAccountNames> deliveryReceipts)
        {
            if (!gl.Reference.StartsWith("DR"))
            {
                return FindSubAccountName(generalLedgerBooksByReference, gl.Reference, _arTradeAccount);
            }

            if (!deliveryReceipts.TryGetValue(gl.Reference, out var dr))
            {
                return null;
            }

            if (_supplierEntries.Contains(gl.AccountNo))
            {
                return dr.SupplierName;
            }

            if (_haulerEntries.Contains(gl.AccountNo))
            {
                return dr.HaulerName;
            }

            return _commissionEntries.Contains(gl.AccountNo)
                ? dr.CommissioneeName
                : dr.CustomerName;
        }
    }
}
