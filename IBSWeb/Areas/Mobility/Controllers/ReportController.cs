using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<ReportController> _logger;

        public ReportController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<ReportController> logger)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        [HttpGet]
        public IActionResult PosVsFmsComparison()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePosVsFmsComparison(PosVsFmsComparisonViewModel model)
        {
            var stationCodeClaim = await GetStationCodeClaimAsync();

            var salesHeaders = await _dbContext.MobilitySalesHeaders
                .Include(s => s.SalesDetails)
                .Where(s => s.Date.Month == model.Period.Month &&
                            s.Date.Year == model.Period.Year &&
                            s.StationCode == stationCodeClaim)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Shift)
                .ToListAsync();

            // Group data by Date, Shift, and Product for easier comparison
            var groupedData = salesHeaders
                .SelectMany(h => h.SalesDetails.Select(d => new
                {
                    Source = h.Source,
                    Date = h.Date,
                    Cashier = h.Cashier,
                    Shift = h.Shift,
                    PageNumber = h.PageNumber,
                    Product = d.Product,
                    PumpNumber = d.PumpNumber,
                    Closing = d.Closing,
                    Opening = d.Opening,
                    Calibration = d.Calibration,
                    Liters = d.Liters,
                    Price = d.Price,
                    Value = d.Value,
                    CashDrop = h.ActualCashOnHand > 0 ? h.ActualCashOnHand : h.SafeDropTotalAmount,
                    POSales = h.POSalesTotalAmount,
                    FuelSales = h.FuelSalesTotalAmount,
                }))
                .Where(x => x.PumpNumber != 0)
                .GroupBy(x => new { x.Date, x.Shift, x.PageNumber, x.Product, x.PumpNumber })
                .OrderBy(g => g.Key.Date)
                .ThenBy(g => g.Key.Shift)
                .ThenBy(g => g.Key.PageNumber)
                .ThenBy(g => g.Key.Product)
                .ThenBy(g => g.Key.PumpNumber)
                .ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("POS vs FMS Comparison");

            //Accounting Number Format
            string numberFormat = "#,##0.00;(#,##0.00)";

            // Headers
            int col = 1;
            // Date and Shift common headers
            worksheet.Cells[1, col++].Value = "DATE";
            worksheet.Cells[1, col++].Value = "SHIFT";
            worksheet.Cells[1, col++].Value = "PAGE NUMBER";
            worksheet.Cells[1, col++].Value = "PRODUCT";
            worksheet.Cells[1, col++].Value = "PUMP";

            // FMS Headers
            worksheet.Cells[1, col++].Value = "FMS CASHIER";
            worksheet.Cells[1, col++].Value = "FMS OPENING";
            worksheet.Cells[1, col++].Value = "FMS CLOSING";
            worksheet.Cells[1, col++].Value = "FMS CALIBRATION";
            worksheet.Cells[1, col++].Value = "FMS VOLUME";
            worksheet.Cells[1, col++].Value = "FMS PRICE";
            worksheet.Cells[1, col++].Value = "FMS FUEL SALES";

            // POS Headers
            var posSpacesIndex = col;
            worksheet.Cells[1, col++].Value = "";
            worksheet.Cells[1, col++].Value = "POS CASHIER";
            worksheet.Cells[1, col++].Value = "POS OPENING";
            worksheet.Cells[1, col++].Value = "POS CLOSING";
            worksheet.Cells[1, col++].Value = "POS CALIBRATION";
            worksheet.Cells[1, col++].Value = "POS VOLUME";
            worksheet.Cells[1, col++].Value = "POS PRICE";
            worksheet.Cells[1, col++].Value = "POS FUEL SALES";

            // Difference Headers
            var differenceSpacesIndex = col;
            worksheet.Cells[1, col++].Value = "";
            worksheet.Cells[1, col++].Value = "FMS VOLUME";
            worksheet.Cells[1, col++].Value = "POS VOLUME";
            worksheet.Cells[1, col++].Value = "VARIANCE";

            worksheet.Cells[1, col++].Value = "FMS SALES";
            worksheet.Cells[1, col++].Value = "POS SALES";
            worksheet.Cells[1, col++].Value = "VARIANCE";

            worksheet.Cells[1, col++].Value = "FMS PO SALES";
            worksheet.Cells[1, col++].Value = "POS PO SALES";
            worksheet.Cells[1, col++].Value = "VARIANCE";

            worksheet.Cells[1, col++].Value = "FMS TOTAL SALES";
            worksheet.Cells[1, col++].Value = "POS TOTAL SALES";
            worksheet.Cells[1, col++].Value = "VARIANCE";

            worksheet.Cells[1, col++].Value = "FMS CASH DROP";
            worksheet.Cells[1, col++].Value = "POS CASH DROP";
            worksheet.Cells[1, col++].Value = "VARIANCE";

            // Style header row
            using (var range = worksheet.Cells[1, 1, 1, col - 1])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Freeze top header row
            worksheet.View.FreezePanes(2, 1);

            // Fill data rows
            int row = 2;

            // Totals for summary
            decimal fmsCalibrationTotal = 0;
            decimal fmsVolumeTotal = 0;
            decimal fmsFuelSalesTotal = 0;
            decimal posCalibrationTotal = 0;
            decimal posVolumeTotal = 0;
            decimal posFuelSalesTotal = 0;
            decimal fmsCashdropTotal = 0;
            decimal fmsPoSalesTotal = 0;
            decimal posCashdropTotal = 0;
            decimal posPoSalesTotal = 0;
            decimal posSalesTotal = 0;
            decimal fmsSalesTotal = 0;
            var currentDate = model.Period;
            var currentShift = 1;
            var currentPage = 1;
            var isFirstRow = true;
            var lastFmsData = groupedData.FirstOrDefault().FirstOrDefault(x => x.Source == "FMS");
            var lastPosData = groupedData.FirstOrDefault().FirstOrDefault(x => x.Source == "POS");


            foreach (var group in groupedData)
            {
                var fmsData = group.FirstOrDefault(x => x.Source == "FMS");
                var posData = group.FirstOrDefault(x => x.Source == "POS");
                var isNewShift = !isFirstRow && (currentDate != group.Key.Date || currentShift != group.Key.Shift || currentPage != group.Key.PageNumber);
                currentDate = group.Key.Date;
                currentShift = group.Key.Shift;
                currentPage = group.Key.PageNumber;

                // Common fields
                col = 1;
                worksheet.Cells[row, col++].Value = currentDate;
                worksheet.Cells[row, col++].Value = currentShift;
                worksheet.Cells[row, col++].Value = currentPage;
                worksheet.Cells[row, col++].Value = group.Key.Product;
                worksheet.Cells[row, col++].Value = group.Key.PumpNumber;

                // FMS data
                if (fmsData != null)
                {
                    worksheet.Cells[row, col++].Value = fmsData.Cashier;
                    worksheet.Cells[row, col++].Value = fmsData.Opening;
                    worksheet.Cells[row, col++].Value = fmsData.Closing;
                    worksheet.Cells[row, col++].Value = fmsData.Calibration;
                    worksheet.Cells[row, col++].Value = fmsData.Liters;
                    worksheet.Cells[row, col++].Value = fmsData.Price;
                    worksheet.Cells[row, col++].Value = fmsData.Value;

                    // Add to totals
                    fmsCalibrationTotal += fmsData.Calibration;
                    fmsVolumeTotal += fmsData.Liters;
                    fmsFuelSalesTotal += fmsData.Value;
                }
                else
                {
                    // Skip empty columns
                    col += 7;
                }

                // POS data
                if (posData != null)
                {
                    worksheet.Cells[row, col++].Value = "";
                    worksheet.Cells[row, col++].Value = posData.Cashier;
                    worksheet.Cells[row, col++].Value = posData.Opening;
                    worksheet.Cells[row, col++].Value = posData.Closing;
                    worksheet.Cells[row, col++].Value = posData.Calibration;
                    worksheet.Cells[row, col++].Value = posData.Liters;
                    worksheet.Cells[row, col++].Value = posData.Price;
                    worksheet.Cells[row, col++].Value = posData.Value;

                    // Add to totals
                    posCalibrationTotal += posData.Calibration;
                    posVolumeTotal += posData.Liters;
                    posFuelSalesTotal += posData.Value;
                }
                else
                {
                    // Skip empty columns
                    col += 8;
                }

                decimal volumeDiff = (posData?.Liters ?? 0m) - (fmsData?.Liters ?? 0m);

                worksheet.Cells[row, col++].Value = "";
                worksheet.Cells[row, col++].Value = fmsData?.Liters;
                worksheet.Cells[row, col++].Value = posData?.Liters;
                worksheet.Cells[row, col++].Value = volumeDiff;

                // Highlight significant differences
                if (Math.Abs(volumeDiff) > 0.1m)
                {
                    worksheet.Cells[row, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col - 1].Style.Fill.BackgroundColor.SetColor(
                        volumeDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                decimal salesDiff = (posData?.Value ?? 0m) - (fmsData?.Value ?? 0m);

                worksheet.Cells[row, col++].Value = fmsData?.Value;
                worksheet.Cells[row, col++].Value = posData?.Value;
                worksheet.Cells[row, col++].Value = salesDiff;


                if (Math.Abs(salesDiff) > 0.1m)
                {
                    worksheet.Cells[row, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col - 1].Style.Fill.BackgroundColor.SetColor(
                        salesDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                if (isNewShift)
                {
                    int endOfShiftRow = row - 1;

                    int lastCol = worksheet.Dimension.End.Column;

                    // Clear background for the previous row from PO sales to end
                    using (var range = worksheet.Cells[endOfShiftRow, col, endOfShiftRow, lastCol])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.None;
                    }

                    decimal poSalesDiff = (lastPosData?.POSales ?? 0m) - (lastFmsData?.POSales ?? 0m);
                    fmsPoSalesTotal += lastFmsData?.POSales ?? 0m;
                    posPoSalesTotal += lastPosData?.POSales ?? 0m;

                    worksheet.Cells[endOfShiftRow, col++].Value = lastFmsData?.POSales;
                    worksheet.Cells[endOfShiftRow, col++].Value = lastPosData?.POSales;
                    worksheet.Cells[endOfShiftRow, col++].Value = poSalesDiff;


                    if (Math.Abs(poSalesDiff) > 0.1m)
                    {
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                            poSalesDiff < 0 ? Color.LightPink : Color.LightGreen);
                    }

                    decimal totalPosSales = (lastPosData?.FuelSales ?? 0m) - (lastPosData?.POSales ?? 0m);
                    decimal totalFmsSales = (lastFmsData?.FuelSales ?? 0m) - (lastFmsData?.POSales ?? 0m);
                    decimal totalSalesDiff = totalPosSales - totalFmsSales;
                    posSalesTotal += totalPosSales;
                    fmsSalesTotal += totalFmsSales;

                    worksheet.Cells[endOfShiftRow, col++].Value = totalFmsSales;
                    worksheet.Cells[endOfShiftRow, col++].Value = totalPosSales;
                    worksheet.Cells[endOfShiftRow, col++].Value = totalSalesDiff;

                    if (Math.Abs(totalSalesDiff) > 0.1m)
                    {
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                            totalSalesDiff < 0 ? Color.LightPink : Color.LightGreen);
                    }

                    decimal cashDropDiff = (lastPosData?.CashDrop ?? 0m) - (lastFmsData?.CashDrop ?? 0m);
                    fmsCashdropTotal += lastFmsData?.CashDrop ?? 0m;
                    posCashdropTotal += lastPosData?.CashDrop ?? 0m;

                    worksheet.Cells[endOfShiftRow, col++].Value = lastFmsData?.CashDrop;
                    worksheet.Cells[endOfShiftRow, col++].Value = lastPosData?.CashDrop;
                    worksheet.Cells[endOfShiftRow, col++].Value = cashDropDiff;


                    if (Math.Abs(cashDropDiff) > 0.1m)
                    {
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                            cashDropDiff < 0 ? Color.LightPink : Color.LightGreen);
                    }

                    //Remove the shade background color and change it to normal
                }
                else
                {
                    int lastCol = worksheet.Dimension.End.Column;
                    Color shadingColor = Color.LightGray; // You can change this color as needed

                    using (var range = worksheet.Cells[row - 1, col, row - 1, lastCol])
                    {
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(shadingColor);
                    }
                }

                lastFmsData = fmsData;
                lastPosData = posData;
                isFirstRow = false;
                row++;
            }

            if (!isFirstRow)
            {
                int endOfShiftRow = row - 1;

                int lastCol = worksheet.Dimension.End.Column;

                // Clear background for the previous row from PO sales to end
                using (var range = worksheet.Cells[endOfShiftRow, col, endOfShiftRow, lastCol])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.None;
                }

                decimal poSalesDiff = (lastPosData?.POSales ?? 0m) - (lastFmsData?.POSales ?? 0m);
                fmsPoSalesTotal += lastFmsData?.POSales ?? 0m;
                posPoSalesTotal += lastPosData?.POSales ?? 0m;

                worksheet.Cells[endOfShiftRow, col++].Value = lastFmsData?.POSales;
                worksheet.Cells[endOfShiftRow, col++].Value = lastPosData?.POSales;
                worksheet.Cells[endOfShiftRow, col++].Value = poSalesDiff;


                if (Math.Abs(poSalesDiff) > 0.1m)
                {
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                        poSalesDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                decimal totalPosSales = (lastPosData?.FuelSales ?? 0m) - (lastPosData?.POSales ?? 0m);
                decimal totalFmsSales = (lastFmsData?.FuelSales ?? 0m) - (lastFmsData?.POSales ?? 0m);
                decimal totalSalesDiff = totalPosSales - totalFmsSales;
                posSalesTotal += totalPosSales;
                fmsSalesTotal += totalFmsSales;

                worksheet.Cells[endOfShiftRow, col++].Value = totalFmsSales;
                worksheet.Cells[endOfShiftRow, col++].Value = totalPosSales;
                worksheet.Cells[endOfShiftRow, col++].Value = totalSalesDiff;

                if (Math.Abs(totalSalesDiff) > 0.1m)
                {
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                        totalSalesDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                decimal cashDropDiff = (lastPosData?.CashDrop ?? 0m) - (lastFmsData?.CashDrop ?? 0m);
                fmsCashdropTotal += lastFmsData?.CashDrop ?? 0m;
                posCashdropTotal += lastPosData?.CashDrop ?? 0m;

                worksheet.Cells[endOfShiftRow, col++].Value = lastFmsData?.CashDrop;
                worksheet.Cells[endOfShiftRow, col++].Value = lastPosData?.CashDrop;
                worksheet.Cells[endOfShiftRow, col++].Value = cashDropDiff;


                if (Math.Abs(cashDropDiff) > 0.1m)
                {
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[endOfShiftRow, col - 1].Style.Fill.BackgroundColor.SetColor(
                        cashDropDiff < 0 ? Color.LightPink : Color.LightGreen);
                }
            }

            // Add totals row
            int totalRow = row;
            col = 1;

            worksheet.Cells[totalRow, col++].Value = "TOTALS";
            worksheet.Cells[totalRow, col++].Value = "";  // Shift column
            worksheet.Cells[totalRow, col++].Value = "";  // Shift column
            worksheet.Cells[totalRow, col++].Value = "";  // Product column
            worksheet.Cells[totalRow, col++].Value = "";  // FMS Cashier
            worksheet.Cells[totalRow, col++].Value = "";  // FMS Cashier

            // Skip to FMS calculated values
            worksheet.Cells[totalRow, col++].Value = "";  // FMS Closing
            worksheet.Cells[totalRow, col++].Value = "";  // FMS Opening
            worksheet.Cells[totalRow, col++].Value = fmsCalibrationTotal;  // FMS Calibration
            worksheet.Cells[totalRow, col++].Value = fmsVolumeTotal;       // FMS Volume
            worksheet.Cells[totalRow, col++].Value = "";  // FMS Price
            worksheet.Cells[totalRow, col++].Value = fmsFuelSalesTotal;        // FMS Sales

            worksheet.Cells[totalRow, col++].Value = "";
            worksheet.Cells[totalRow, col++].Value = "";  // POS Cashier
            worksheet.Cells[totalRow, col++].Value = "";  // POS Closing
            worksheet.Cells[totalRow, col++].Value = "";  // POS Opening
            worksheet.Cells[totalRow, col++].Value = posCalibrationTotal;  // POS Calibration
            worksheet.Cells[totalRow, col++].Value = posVolumeTotal;       // POS Volume
            worksheet.Cells[totalRow, col++].Value = "";  // POS Price
            worksheet.Cells[totalRow, col++].Value = posFuelSalesTotal;        // POS Sales

            // Calculate total differences
            decimal volumeSumDiff = posVolumeTotal - fmsVolumeTotal;
            decimal salesSumDiff = posFuelSalesTotal - fmsFuelSalesTotal;
            decimal casDropSumDiff = posCashdropTotal - fmsCashdropTotal;
            decimal poSalesSumDiff = posPoSalesTotal - fmsPoSalesTotal;
            decimal totalSalesSumDiff = posSalesTotal - fmsSalesTotal;

            worksheet.Cells[totalRow, col++].Value = "";
            worksheet.Cells[totalRow, col++].Value = fmsVolumeTotal;
            worksheet.Cells[totalRow, col++].Value = posVolumeTotal;
            worksheet.Cells[totalRow, col++].Value = volumeSumDiff;

            worksheet.Cells[totalRow, col++].Value = fmsFuelSalesTotal;
            worksheet.Cells[totalRow, col++].Value = posFuelSalesTotal;
            worksheet.Cells[totalRow, col++].Value = salesSumDiff;

            worksheet.Cells[totalRow, col++].Value = fmsPoSalesTotal;
            worksheet.Cells[totalRow, col++].Value = posPoSalesTotal;
            worksheet.Cells[totalRow, col++].Value = poSalesSumDiff;

            worksheet.Cells[totalRow, col++].Value = fmsSalesTotal;
            worksheet.Cells[totalRow, col++].Value = posSalesTotal;
            worksheet.Cells[totalRow, col++].Value = totalSalesSumDiff;

            worksheet.Cells[totalRow, col++].Value = fmsCashdropTotal;
            worksheet.Cells[totalRow, col++].Value = posCashdropTotal;
            worksheet.Cells[totalRow, col++].Value = casDropSumDiff;

            // Highlight total row
            using (var range = worksheet.Cells[totalRow, 1, totalRow, col - 1])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                range.Style.Border.Top.Style = ExcelBorderStyle.Double;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
            }

            // Style all data cells with borders
            using (var range = worksheet.Cells[1, 1, totalRow, col - 1])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            // Format numeric columns with number format
            for (int c = 6; c <= col - 1; c++)
            {
                if (c == 14) // Skip non-numeric columns if any
                    continue;

                worksheet.Cells[2, c, totalRow, c].Style.Numberformat.Format = numberFormat;
            }

            // Format date column
            worksheet.Cells[2, 1, totalRow, 1].Style.Numberformat.Format = "MMM/dd/yyyy";

            // Add summary section
            row = totalRow + 2;
            worksheet.Cells[row, 1].Value = "SUMMARY";
            worksheet.Cells[row, 1, row, 3].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            row++;
            worksheet.Cells[row, 1].Value = "Source";
            worksheet.Cells[row, 2].Value = "Total Volume";
            worksheet.Cells[row, 3].Value = "Total Fuel Sales";
            worksheet.Cells[row, 4].Value = "Total PO Sales";
            worksheet.Cells[row, 5].Value = "Total Sales";
            worksheet.Cells[row, 6].Value = "Total Cash Drop";

            row++;
            worksheet.Cells[row, 1].Value = "FMS";
            worksheet.Cells[row, 2].Value = fmsVolumeTotal;
            worksheet.Cells[row, 3].Value = fmsFuelSalesTotal;
            worksheet.Cells[row, 4].Value = fmsPoSalesTotal;
            worksheet.Cells[row, 5].Value = fmsSalesTotal;
            worksheet.Cells[row, 6].Value = fmsCashdropTotal;
            worksheet.Cells[row, 2, row, 6].Style.Numberformat.Format = numberFormat;

            row++;
            worksheet.Cells[row, 1].Value = "POS";
            worksheet.Cells[row, 2].Value = posVolumeTotal;
            worksheet.Cells[row, 3].Value = posFuelSalesTotal;
            worksheet.Cells[row, 4].Value = posPoSalesTotal;
            worksheet.Cells[row, 5].Value = posSalesTotal;
            worksheet.Cells[row, 6].Value = posCashdropTotal;
            worksheet.Cells[row, 2, row, 6].Style.Numberformat.Format = numberFormat;

            row++;
            worksheet.Cells[row, 1].Value = "Difference";
            worksheet.Cells[row, 2].Value = volumeSumDiff;
            worksheet.Cells[row, 3].Value = salesSumDiff;
            worksheet.Cells[row, 4].Value = poSalesSumDiff;
            worksheet.Cells[row, 5].Value = totalSalesSumDiff;
            worksheet.Cells[row, 6].Value = casDropSumDiff;
            worksheet.Cells[row, 2, row, 6].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row, 6].Style.Numberformat.Format = numberFormat;

            // Add percentage difference
            row++;
            decimal volumePercentDiff = fmsVolumeTotal != 0 ? (volumeSumDiff / fmsVolumeTotal) : 0;
            decimal salesPercentDiff = fmsFuelSalesTotal != 0 ? (salesSumDiff / fmsFuelSalesTotal) : 0;
            decimal poSalesPercentDiff = fmsPoSalesTotal != 0 ? (poSalesSumDiff / fmsPoSalesTotal) : 0;
            decimal totalSalesPercentDiff = fmsSalesTotal != 0 ? (totalSalesSumDiff / fmsSalesTotal) : 0;
            decimal cashDropPercentDiff = fmsCashdropTotal != 0 ? (casDropSumDiff / fmsCashdropTotal) : 0;

            worksheet.Cells[row, 1].Value = "Percentage Diff";
            worksheet.Cells[row, 2].Value = volumePercentDiff;
            worksheet.Cells[row, 3].Value = salesPercentDiff;
            worksheet.Cells[row, 4].Value = poSalesPercentDiff;
            worksheet.Cells[row, 5].Value = totalSalesPercentDiff;
            worksheet.Cells[row, 6].Value = cashDropPercentDiff;
            worksheet.Cells[row, 2, row, 6].Style.Numberformat.Format = "0.00%";
            worksheet.Cells[row, 2, row, 6].Style.Font.Bold = true;

            // Style summary table
            using (var range = worksheet.Cells[totalRow + 3, 1, row, 6])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            row += 2;
            worksheet.Cells[row, 1].Value = "PUMP AND PRODUCT SUMMARY";
            worksheet.Cells[row, 1, row, 6].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            row++;
            worksheet.Cells[row, 1].Value = "Source";
            worksheet.Cells[row, 2].Value = "Product";
            worksheet.Cells[row, 3].Value = "Pump";
            worksheet.Cells[row, 4].Value = "Opening";
            worksheet.Cells[row, 5].Value = "Closing";
            worksheet.Cells[row, 6].Value = "Total Volume";

            // Style header
            using (var range = worksheet.Cells[row, 1, row, 6])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            // Get distinct products and pumps for summary
            var products = groupedData.Select(g => g.Key.Product).Distinct().OrderBy(p => p).ToList();
            var pumps = groupedData.Select(g => g.Key.PumpNumber).Distinct().OrderBy(p => p).ToList();

            var extremesBySrcProdPump = salesHeaders
                .SelectMany(h => h.SalesDetails.Select(d => new
                {
                    h.Source,
                    d.Product,
                    d.PumpNumber,
                    d.Closing,
                    d.Opening
                }))
                .Where(x => x.PumpNumber != 0 && x.Product.Contains("PET"))
                .GroupBy(x => new { x.Source, x.Product, x.PumpNumber })
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        MaxClosing = g.Max(x => x.Closing),
                        MinOpening = g.Where(x => x.Opening > 0).Min(x => x.Opening) //Exclude the zero
                    });


            // Add rows for each product/pump combination for both POS and FMS
            foreach (var source in new[] { "POS", "FMS" })
            {
                decimal grandTotal = 0;

                foreach (var product in products)
                {
                    decimal productTotal = 0;

                    foreach (var pump in pumps)
                    {

                        row++;
                        worksheet.Cells[row, 1].Value = source;
                        worksheet.Cells[row, 2].Value = product;
                        worksheet.Cells[row, 3].Value = pump;

                        var key = new { Source = source, Product = product, PumpNumber = pump };

                        if (extremesBySrcProdPump.TryGetValue(key, out var ext))
                        {
                            worksheet.Cells[row, 4].Value = ext.MinOpening;
                            worksheet.Cells[row, 5].Value = ext.MaxClosing;

                            var totalVolume = ext.MaxClosing - ext.MinOpening;
                            worksheet.Cells[row, 6].Value = totalVolume;

                            productTotal += totalVolume;
                        }
                        else
                        {
                            worksheet.Cells[row, 4, row, 6].Value = 0m;
                        }

                        worksheet.Cells[row, 4, row, 6].Style.Numberformat.Format = numberFormat;

                        using (var range = worksheet.Cells[row, 1, row, 6])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }
                    }

                    // Add subtotal row for this product
                    row++;
                    worksheet.Cells[row, 1].Value = "TOTAL";
                    worksheet.Cells[row, 1, row, 5].Merge = true;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 6].Value = productTotal;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = numberFormat;
                    worksheet.Cells[row, 1, row, 6].Style.Font.Bold = true;

                    grandTotal += productTotal;

                    using (var range = worksheet.Cells[row, 1, row, 6])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }
                }

                row++;
                worksheet.Cells[row, 1].Value = "GRAND TOTAL";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 6].Value = grandTotal;
                worksheet.Cells[row, 6].Style.Numberformat.Format = numberFormat;
                worksheet.Cells[row, 1, row, 6].Style.Font.Bold = true;

                using (var range = worksheet.Cells[row, 1, row, 6])
                {
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                // Add a blank row between sources
                if (source == "POS")
                {
                    row++;
                }
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Column(differenceSpacesIndex).Width = 1;
            worksheet.Column(posSpacesIndex).Width = 1;

            var excelBytes = await package.GetAsByteArrayAsync();

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PosVsFmsComparison_{model.Period.Year}_{model.Period.Month}.xlsx");
        }
    }
}
