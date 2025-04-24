using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
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
                .Where(s => s.Date.Month == model.Period.Month && s.Date.Year == model.Period.Year && s.StationCode == stationCodeClaim)
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
                    Value = d.Value
                }))
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
            worksheet.Cells[1, col++].Value = "FMS CLOSING";
            worksheet.Cells[1, col++].Value = "FMS OPENING";
            worksheet.Cells[1, col++].Value = "FMS CALIBRATION";
            worksheet.Cells[1, col++].Value = "FMS VOLUME";
            worksheet.Cells[1, col++].Value = "FMS PRICE";
            worksheet.Cells[1, col++].Value = "FMS FUEL SALES";

            // POS Headers
            worksheet.Cells[1, col++].Value = "POS CASHIER";
            worksheet.Cells[1, col++].Value = "POS CLOSING";
            worksheet.Cells[1, col++].Value = "POS OPENING";
            worksheet.Cells[1, col++].Value = "POS CALIBRATION";
            worksheet.Cells[1, col++].Value = "POS VOLUME";
            worksheet.Cells[1, col++].Value = "POS PRICE";
            worksheet.Cells[1, col++].Value = "POS FUEL SALES";

            // Difference Headers
            worksheet.Cells[1, col++].Value = "VOLUME DIFF";
            worksheet.Cells[1, col++].Value = "SALES DIFF";

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
            decimal fmsSalesTotal = 0;
            decimal posCalibrationTotal = 0;
            decimal posVolumeTotal = 0;
            decimal posSalesTotal = 0;

            foreach (var group in groupedData)
            {
                var fmsData = group.FirstOrDefault(x => x.Source == "FMS");
                var posData = group.FirstOrDefault(x => x.Source == "POS");

                // Common fields
                col = 1;
                worksheet.Cells[row, col++].Value = group.Key.Date;
                worksheet.Cells[row, col++].Value = group.Key.Shift;
                worksheet.Cells[row, col++].Value = group.Key.PageNumber;
                worksheet.Cells[row, col++].Value = group.Key.Product;
                worksheet.Cells[row, col++].Value = group.Key.PumpNumber;

                // FMS data
                if (fmsData != null)
                {
                    worksheet.Cells[row, col++].Value = fmsData.Cashier;
                    worksheet.Cells[row, col++].Value = fmsData.Closing;
                    worksheet.Cells[row, col++].Value = fmsData.Opening;
                    worksheet.Cells[row, col++].Value = fmsData.Calibration;
                    worksheet.Cells[row, col++].Value = fmsData.Liters;
                    worksheet.Cells[row, col++].Value = fmsData.Price;
                    worksheet.Cells[row, col++].Value = fmsData.Value;

                    // Add to totals
                    fmsCalibrationTotal += fmsData.Calibration;
                    fmsVolumeTotal += fmsData.Liters;
                    fmsSalesTotal += fmsData.Value;
                }
                else
                {
                    // Skip empty columns
                    col += 7;
                }

                // POS data
                if (posData != null)
                {
                    worksheet.Cells[row, col++].Value = posData.Cashier;
                    worksheet.Cells[row, col++].Value = posData.Closing;
                    worksheet.Cells[row, col++].Value = posData.Opening;
                    worksheet.Cells[row, col++].Value = posData.Calibration;
                    worksheet.Cells[row, col++].Value = posData.Liters;
                    worksheet.Cells[row, col++].Value = posData.Price;
                    worksheet.Cells[row, col++].Value = posData.Value;

                    // Add to totals
                    posCalibrationTotal += posData.Calibration;
                    posVolumeTotal += posData.Liters;
                    posSalesTotal += posData.Value;
                }
                else
                {
                    // Skip empty columns
                    col += 7;
                }

                decimal volumeDiff = (posData?.Liters ?? 0m) - (fmsData?.Liters ?? 0m);
                decimal salesDiff = (posData?.Value ?? 0m) - (fmsData?.Value ?? 0m);

                worksheet.Cells[row, col++].Value = volumeDiff;
                worksheet.Cells[row, col++].Value = salesDiff;

                // Highlight significant differences
                if (Math.Abs(volumeDiff) > 0.1m)
                {
                    worksheet.Cells[row, col - 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col - 2].Style.Fill.BackgroundColor.SetColor(
                        volumeDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                if (Math.Abs(salesDiff) > 0.1m)
                {
                    worksheet.Cells[row, col - 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, col - 1].Style.Fill.BackgroundColor.SetColor(
                        salesDiff < 0 ? Color.LightPink : Color.LightGreen);
                }

                row++;
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
            worksheet.Cells[totalRow, col++].Value = fmsSalesTotal;        // FMS Sales

            worksheet.Cells[totalRow, col++].Value = "";  // POS Cashier
            worksheet.Cells[totalRow, col++].Value = "";  // POS Closing
            worksheet.Cells[totalRow, col++].Value = "";  // POS Opening
            worksheet.Cells[totalRow, col++].Value = posCalibrationTotal;  // POS Calibration
            worksheet.Cells[totalRow, col++].Value = posVolumeTotal;       // POS Volume
            worksheet.Cells[totalRow, col++].Value = "";  // POS Price
            worksheet.Cells[totalRow, col++].Value = posSalesTotal;        // POS Sales

            // Calculate total differences
            decimal totalVolumeDiff = posVolumeTotal - fmsVolumeTotal;
            decimal totalSalesDiff = posSalesTotal - fmsSalesTotal;

            worksheet.Cells[totalRow, col++].Value = totalVolumeDiff;
            worksheet.Cells[totalRow, col++].Value = totalSalesDiff;

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
                if (c == 13) // Skip non-numeric columns if any
                    continue;

                worksheet.Cells[2, c, totalRow, c].Style.Numberformat.Format = numberFormat;
            }

            // Format date column
            worksheet.Cells[2, 1, totalRow, 1].Style.Numberformat.Format = "MMM/dd/yyyy";

            // Add summary section
            row = totalRow + 2;
            worksheet.Cells[row, 1].Value = "SUMMARY";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;

            row++;
            worksheet.Cells[row, 1].Value = "Source";
            worksheet.Cells[row, 2].Value = "Total Volume";
            worksheet.Cells[row, 3].Value = "Total Sales";

            row++;
            worksheet.Cells[row, 1].Value = "FMS";
            worksheet.Cells[row, 2].Value = fmsVolumeTotal;
            worksheet.Cells[row, 3].Value = fmsSalesTotal;
            worksheet.Cells[row, 2, row, 3].Style.Numberformat.Format = numberFormat;

            row++;
            worksheet.Cells[row, 1].Value = "POS";
            worksheet.Cells[row, 2].Value = posVolumeTotal;
            worksheet.Cells[row, 3].Value = posSalesTotal;
            worksheet.Cells[row, 2, row, 3].Style.Numberformat.Format = numberFormat;

            row++;
            worksheet.Cells[row, 1].Value = "Difference";
            worksheet.Cells[row, 2].Value = totalVolumeDiff;
            worksheet.Cells[row, 3].Value = totalSalesDiff;
            worksheet.Cells[row, 2, row, 3].Style.Font.Bold = true;
            worksheet.Cells[row, 2, row, 3].Style.Numberformat.Format = numberFormat;

            // Add percentage difference
            row++;
            decimal volumePercentDiff = fmsVolumeTotal != 0 ? (totalVolumeDiff / fmsVolumeTotal) : 0;
            decimal salesPercentDiff = fmsSalesTotal != 0 ? (totalSalesDiff / fmsSalesTotal) : 0;

            worksheet.Cells[row, 1].Value = "Percentage Diff";
            worksheet.Cells[row, 2].Value = volumePercentDiff;
            worksheet.Cells[row, 3].Value = salesPercentDiff;
            worksheet.Cells[row, 2, row, 3].Style.Numberformat.Format = "0.00%";
            worksheet.Cells[row, 2, row, 3].Style.Font.Bold = true;

            // Style summary table
            using (var range = worksheet.Cells[totalRow + 3, 1, row, 3])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var excelBytes = package.GetAsByteArray();

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PosVsFmsComparison_{model.Period.Year}_{model.Period.Month}.xlsx");
        }
    }
}
