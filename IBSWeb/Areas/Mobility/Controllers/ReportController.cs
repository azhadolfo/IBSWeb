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

            var posSales = await _dbContext.MobilitySalesHeaders
                .Include(s => s.SalesDetails)
                .Where(s => s.Date.Month == model.Period.Month && s.Date.Year == model.Period.Year && s.StationCode == stationCodeClaim)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Shift)
                .ToListAsync();

            var fmsCashierShifts = await _dbContext.MobilityFmsCashierShifts
                .Where(c => c.Date.Month == model.Period.Month && c.Date.Year == model.Period.Year && c.StationCode == stationCodeClaim)
                .OrderBy(c => c.Date)
                .ThenBy(c => c.ShiftNumber)
                .ToListAsync();

            var fmsFuelSales = await _dbContext.MobilityFMSFuelSales
                .Where(f => f.ShiftDate.Month == model.Period.Month && f.ShiftDate.Year == model.Period.Year && f.StationCode == stationCodeClaim)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync();

            var fmsLubeSales = await _dbContext.MobilityFMSLubeSales
                .Where(f => f.ShiftDate.Month == model.Period.Month && f.ShiftDate.Year == model.Period.Year && f.StationCode == stationCodeClaim)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync();

            var fmsCalibrations = await _dbContext.MobilityFmsCalibrations
                .Where(f => f.ShiftDate.Month == model.Period.Month && f.ShiftDate.Year == model.Period.Year && f.StationCode == stationCodeClaim)
                .OrderBy(f => f.ShiftDate)
                .ThenBy(f => f.ShiftNumber)
                .ToListAsync();

            // Combine FMS data by shift
            var fmsDataByShift = fmsCashierShifts.Select(shift => new
            {
                Shift = shift,
                FuelSales = fmsFuelSales.Where(f => f.ShiftRecordId == shift.ShiftRecordId).ToList(),
                LubeSales = fmsLubeSales.Where(l => l.ShiftRecordId == shift.ShiftRecordId).ToList(),
                Calibrations = fmsCalibrations.Where(c => c.ShiftRecordId == shift.ShiftRecordId).ToList()
            }).ToList();

            // Similarly, let's organize POS data for easier access
            var posDataByShift = posSales.GroupBy(p => new { p.Date, p.Shift })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Shift = g.Key.Shift,
                    Sales = g.ToList()
                }).ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("POS vs FMS");

            // Create FMS section headers
            worksheet.Cells[1, 1].Value = "FMS";
            worksheet.Cells[1, 2].Value = "CLOSING";
            worksheet.Cells[1, 3].Value = "OPENING";
            worksheet.Cells[1, 4].Value = "CALIBRATION";
            worksheet.Cells[1, 5].Value = "VOLUME";
            worksheet.Cells[1, 6].Value = "PRICE";
            worksheet.Cells[1, 7].Value = "FUEL SALES";
            worksheet.Cells[1, 8].Value = "LUBE SALES";
            worksheet.Cells[1, 9].Value = "CASH ON HAND";

            // Create POS section headers
            worksheet.Cells[1, 11].Value = "POS";
            worksheet.Cells[1, 12].Value = "CLOSING";
            worksheet.Cells[1, 13].Value = "OPENING";
            worksheet.Cells[1, 14].Value = "CALIBRATION";
            worksheet.Cells[1, 15].Value = "VOLUME";
            worksheet.Cells[1, 16].Value = "PRICE";
            worksheet.Cells[1, 17].Value = "FUEL SALES";
            worksheet.Cells[1, 18].Value = "LUBE SALES";
            worksheet.Cells[1, 19].Value = "CASH DROP";

            // Add TOTALS row
            worksheet.Cells[2, 1].Value = "TOTALS";
            worksheet.Cells[2, 11].Value = "TOTALS";

            // Calculate and populate the totals for FMS
            decimal fmsTotalCalibration = fmsCalibrations.Sum(c => c.Quantity);
            decimal fmsTotalVolume = fmsFuelSales.Sum(f => f.Closing - f.Opening);
            decimal fmsTotalFuelSales = fmsFuelSales.Sum(f => (f.Closing - f.Opening) * f.Price);
            decimal fmsTotalLubeSales = fmsLubeSales.Sum(l => l.Quantity * l.Price);
            decimal fmsTotalCashOnHand = fmsCashierShifts.Sum(s => s.CashOnHand);

            worksheet.Cells[2, 4].Value = fmsTotalCalibration;
            worksheet.Cells[2, 5].Value = fmsTotalVolume;
            worksheet.Cells[2, 7].Value = fmsTotalFuelSales;
            worksheet.Cells[2, 8].Value = fmsTotalLubeSales;
            worksheet.Cells[2, 9].Value = fmsTotalCashOnHand;

            // Calculate and populate the totals for POS
            decimal posTotalCalibration = 0; // Assuming POS calibration data exists
            decimal posTotalVolume = posSales.SelectMany(p => p.SalesDetails).Where(d => d.Product.Contains("PET")).Sum(d => d.Liters);
            decimal posTotalFuelSales = posSales.SelectMany(p => p.SalesDetails).Where(d => d.Product.Contains("PET")).Sum(d => d.Value);
            decimal posTotalLubeSales = posSales.SelectMany(p => p.SalesDetails).Where(d => !d.Product.Contains("PET")).Sum(d => d.Value);
            decimal posTotalCashDrop = posSales.Sum(p => p.SafeDropTotalAmount);

            worksheet.Cells[2, 14].Value = posTotalCalibration;
            worksheet.Cells[2, 15].Value = posTotalVolume;
            worksheet.Cells[2, 17].Value = posTotalFuelSales;
            worksheet.Cells[2, 18].Value = posTotalLubeSales;
            worksheet.Cells[2, 19].Value = posTotalCashDrop;

            // Start populating data rows
            int currentRow = 3;

            // Create combined list of all dates and shifts
            var allDatesAndShifts = new HashSet<(DateOnly Date, int Shift)>();

            foreach (var shift in fmsCashierShifts)
            {
                allDatesAndShifts.Add((shift.Date, shift.ShiftNumber));
            }

            foreach (var sale in posSales)
            {
                allDatesAndShifts.Add((sale.Date, sale.Shift));
            }

            // Sort by date and shift
            var sortedDatesAndShifts = allDatesAndShifts
                .OrderBy(ds => ds.Date)
                .ThenBy(ds => ds.Shift)
                .ToList();

            // Populate data for each date/shift
            foreach (var (date, shiftNumber) in sortedDatesAndShifts)
            {
                // Add date and shift as a row header
                worksheet.Cells[currentRow, 1].Value = $"{date.ToShortDateString()} - Shift {shiftNumber}";
                worksheet.Cells[currentRow, 11].Value = $"{date.ToShortDateString()} - Shift {shiftNumber}";

                // Find corresponding FMS data
                var fmsData = fmsDataByShift.FirstOrDefault(f =>
                    f.Shift.Date.Day == date.Day && f.Shift.ShiftNumber == shiftNumber);

                if (fmsData != null)
                {
                    // Populate FMS data
                    worksheet.Cells[currentRow, 2].Value = fmsData.FuelSales.Max(f => f.Closing);
                    worksheet.Cells[currentRow, 3].Value = fmsData.FuelSales.Min(f => f.Opening);

                    decimal calibrationVolume = fmsData.Calibrations.Sum(c => c.Quantity);
                    worksheet.Cells[currentRow, 4].Value = calibrationVolume;

                    decimal fuelVolume = fmsData.FuelSales.Sum(f => f.Closing - f.Opening);
                    worksheet.Cells[currentRow, 5].Value = fuelVolume;

                    // Average price per unit if volume > 0
                    if (fuelVolume > 0)
                    {
                        decimal totalAmount = fmsData.FuelSales.Sum(f => (f.Closing - f.Opening) * f.Price);
                        worksheet.Cells[currentRow, 6].Value = totalAmount / fuelVolume;
                    }

                    worksheet.Cells[currentRow, 7].Value = fmsData.FuelSales.Sum(f => (f.Closing - f.Opening) * f.Price);
                    worksheet.Cells[currentRow, 8].Value = fmsData.LubeSales.Sum(l => l.Quantity * l.Price);
                    worksheet.Cells[currentRow, 9].Value = fmsData.Shift.CashOnHand;
                }

                // Find corresponding POS data
                var posData = posDataByShift.FirstOrDefault(p =>
                    p.Date.Day == date.Day && p.Shift == shiftNumber);

                if (posData != null)
                {
                    var fuelSalesDetails = posData.Sales.SelectMany(s => s.SalesDetails)
                        .Where(d => d.Product.Contains("PET"))
                        .ToList();

                    var lubeSalesDetails = posData.Sales.SelectMany(s => s.SalesDetails)
                        .Where(d => !d.Product.Contains("PET"))
                        .ToList();

                    decimal posVolume = fuelSalesDetails.Sum(d => d.Liters);
                    decimal posFuelSales = fuelSalesDetails.Sum(d => d.Value);
                    decimal posLubeSales = lubeSalesDetails.Sum(d => d.Value);

                    // Populate POS data
                    // Assuming these fields exist in your POS data, adjust as needed
                    worksheet.Cells[currentRow, 12].Value = posData.Sales.FirstOrDefault()?.SalesDetails.Max(s => s.Closing) ?? 0;
                    worksheet.Cells[currentRow, 13].Value = posData.Sales.FirstOrDefault()?.SalesDetails.Min(s => s.Opening) ?? 0;
                    worksheet.Cells[currentRow, 14].Value = 0; // POS calibration (if applicable)
                    worksheet.Cells[currentRow, 15].Value = posVolume;

                    // Average price per unit if volume > 0
                    if (posVolume > 0)
                    {
                        worksheet.Cells[currentRow, 16].Value = posFuelSales / posVolume;
                    }

                    worksheet.Cells[currentRow, 17].Value = posFuelSales;
                    worksheet.Cells[currentRow, 18].Value = posLubeSales;
                    worksheet.Cells[currentRow, 19].Value = posData.Sales.Sum(s => s.SafeDropTotalAmount);
                }

                currentRow++;
            }

            // Format numbers to 2 decimal places for currency and price
            var moneyFormat = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
            worksheet.Cells[2, 4, currentRow - 1, 9].Style.Numberformat.Format = moneyFormat;
            worksheet.Cells[2, 14, currentRow - 1, 19].Style.Numberformat.Format = moneyFormat;

            // Highlight totals cells in yellow
            var yellowColor = Color.FromArgb(255, 255, 0);
            worksheet.Cells[2, 4, 2, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, 4, 2, 9].Style.Fill.BackgroundColor.SetColor(yellowColor);
            worksheet.Cells[2, 14, 2, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[2, 14, 2, 19].Style.Fill.BackgroundColor.SetColor(yellowColor);

            // Add alternating row colors for better readability
            for (int i = 3; i < currentRow; i += 2)
            {
                var lightGray = Color.FromArgb(240, 240, 240);
                worksheet.Cells[i, 1, i, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[i, 1, i, 9].Style.Fill.BackgroundColor.SetColor(lightGray);
                worksheet.Cells[i, 11, i, 19].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[i, 11, i, 19].Style.Fill.BackgroundColor.SetColor(lightGray);
            }

            // Add borders
            var dataRange = worksheet.Cells[1, 1, currentRow - 1, 19];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // Make headers bold
            worksheet.Cells[1, 1, 1, 19].Style.Font.Bold = true;
            worksheet.Cells[2, 1].Style.Font.Bold = true;
            worksheet.Cells[2, 11].Style.Font.Bold = true;

            // Add a difference/variance section
            worksheet.Cells[currentRow + 1, 1].Value = "VARIANCE ANALYSIS (FMS vs POS)";
            worksheet.Cells[currentRow + 1, 1, currentRow + 1, 19].Merge = true;
            worksheet.Cells[currentRow + 1, 1].Style.Font.Bold = true;
            worksheet.Cells[currentRow + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells[currentRow + 2, 1].Value = "CATEGORY";
            worksheet.Cells[currentRow + 2, 2].Value = "FMS";
            worksheet.Cells[currentRow + 2, 3].Value = "POS";
            worksheet.Cells[currentRow + 2, 4].Value = "DIFFERENCE";
            worksheet.Cells[currentRow + 2, 5].Value = "% VARIANCE";

            // Bold the variance headers
            worksheet.Cells[currentRow + 2, 1, currentRow + 2, 5].Style.Font.Bold = true;

            // Add variance data
            worksheet.Cells[currentRow + 3, 1].Value = "VOLUME";
            worksheet.Cells[currentRow + 3, 2].Value = fmsTotalVolume;
            worksheet.Cells[currentRow + 3, 3].Value = posTotalVolume;
            worksheet.Cells[currentRow + 3, 4].Formula = $"=B{currentRow + 3}-C{currentRow + 3}";
            worksheet.Cells[currentRow + 3, 5].Formula = $"=D{currentRow + 3}/B{currentRow + 3}";
            worksheet.Cells[currentRow + 3, 5].Style.Numberformat.Format = "0.00%";

            worksheet.Cells[currentRow + 4, 1].Value = "FUEL SALES";
            worksheet.Cells[currentRow + 4, 2].Value = fmsTotalFuelSales;
            worksheet.Cells[currentRow + 4, 3].Value = posTotalFuelSales;
            worksheet.Cells[currentRow + 4, 4].Formula = $"=B{currentRow + 4}-C{currentRow + 4}";
            worksheet.Cells[currentRow + 4, 5].Formula = $"=D{currentRow + 4}/B{currentRow + 4}";
            worksheet.Cells[currentRow + 4, 5].Style.Numberformat.Format = "0.00%";

            worksheet.Cells[currentRow + 5, 1].Value = "LUBE SALES";
            worksheet.Cells[currentRow + 5, 2].Value = fmsTotalLubeSales;
            worksheet.Cells[currentRow + 5, 3].Value = posTotalLubeSales;
            worksheet.Cells[currentRow + 5, 4].Formula = $"=B{currentRow + 5}-C{currentRow + 5}";
            worksheet.Cells[currentRow + 5, 5].Formula = $"=D{currentRow + 5}/B{currentRow + 5}";
            worksheet.Cells[currentRow + 5, 5].Style.Numberformat.Format = "0.00%";

            // Format the variance table
            var varianceRange = worksheet.Cells[currentRow + 2, 1, currentRow + 5, 5];
            varianceRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            varianceRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            varianceRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            varianceRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            // Add conditional formatting to highlight significant variances
            var significantVariance = worksheet.ConditionalFormatting.AddAboveAverage(
                new ExcelAddress(currentRow + 3, 5, currentRow + 5, 5));
            significantVariance.Style.Fill.PatternType = ExcelFillStyle.Solid;
            significantVariance.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);

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
