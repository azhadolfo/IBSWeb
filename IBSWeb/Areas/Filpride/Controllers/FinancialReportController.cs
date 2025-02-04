using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class FinancialReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public FinancialReportController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public IActionResult ProfitAndLossReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProfitAndLossReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            if (monthDate == default)
            {
                return BadRequest();
            }

            var companyClaims = await GetCompanyClaimAsync();
            var today = DateTimeHelper.GetCurrentPhilippineTime();
            var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                .Include(gl => gl.Account) // Level 4
                .ThenInclude(ac => ac.ParentAccount) // Level 3
                .ThenInclude(ac => ac.ParentAccount) // Level 2
                .ThenInclude(ac => ac.ParentAccount) // Level 1
                .Where(gl =>
                    gl.Date >= firstDayOfMonth &&
                    gl.Date <= lastDayOfMonth &&
                    gl.Company == companyClaims)
                .ToListAsync(cancellationToken);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("PNL Report");

                // Set up the column headers
                worksheet.Cells[1, 1].Value = "Company Name";
                worksheet.Cells[1, 2].Value = "Company Logo";
                worksheet.Cells[1, 3].Value = "BALANCE SHEET";
                worksheet.Cells[1, 4].Value = "AS of Oct 31, 2024";

                worksheet.Cells[2, 1].Value = "L1";
                worksheet.Cells[2, 2].Value = "L2";
                worksheet.Cells[2, 3].Value = "L3";
                worksheet.Cells[2, 4].Value = "L4";
                worksheet.Cells[2, 5].Value = "L5";
                worksheet.Cells[2, 6].Value = "ID";
                worksheet.Cells[2, 7].Value = "Remarks";
                worksheet.Cells[2, 8].Value = "level";
                worksheet.Cells[2, 9].Value = "AMT";
                worksheet.Cells[2, 10].Value = "VO";

                // Populate the data
                int row = 3;
                foreach (var gl in generalLedgers)
                {
                    worksheet.Cells[row, 1].Value = gl.AccountNo;
                    worksheet.Cells[row, 2].Value = gl.AccountTitle;
                    worksheet.Cells[row, 3].Value = gl.Description;
                    worksheet.Cells[row, 4].Value = gl.Debit;
                    worksheet.Cells[row, 5].Value = gl.Credit;
                    row++;
                }

                // Adjust column widths and formatting
                worksheet.Columns.AutoFit();

                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PNLReport.xlsx");
            }
        }

        [HttpGet]
        public IActionResult LevelOneReport()
        {
            return View();
        }


        #region -- Generate L1 Report --

        [HttpPost]
        public async Task<IActionResult> LevelOneReport(DateOnly monthDate, CancellationToken cancellationToken)
        {
            try
            {
                if (monthDate == default)
                {
                    return BadRequest();
                }

                var companyClaims = await GetCompanyClaimAsync();
                var today = DateTimeHelper.GetCurrentPhilippineTime();
                var firstDayOfMonth = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                string currencyFormat = "#,##0.00_);[Red](#,##0.00)";
                int row = 1;

                var generalLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac.ParentAccount) // Level 2
                    .ThenInclude(ac => ac.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= firstDayOfMonth &&
                        gl.Date <= lastDayOfMonth &&
                        gl.AccountId != null && //Uncomment this if the GL is fix
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                if (!generalLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(LevelOneReport));
                }

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("L1");

                #region == Top of Header ==


                using (var range = worksheet.Cells[row, 1, row, 3])
                {
                    range.Merge = true;
                    range.Value = "FILPRIDE RESOURCES INC.";
                    range.Style.Font.Bold = true;
                }
                row++;

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    var picture = worksheet.Drawings.AddPicture(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 1, -85);
                    picture.SetSize(330, 75);
                }

                worksheet.Row(row).Height = 80;

                using (var range = worksheet.Cells[row, 1, row, 3])
                {
                    range.Merge = true;
                }
                row++;
                using (var range = worksheet.Cells[row, 1, row, 3])
                {
                    range.Merge = true;
                    range.Value = "Level 1";
                    range.Style.Font.Bold = true;
                }
                row++;
                using (var range = worksheet.Cells[row, 1, row, 3])
                {
                    range.Merge = true;
                    range.Value =  "As of " + monthDate.ToString("dd MMMM yyyy");
                }
                row++;
                using (var range = worksheet.Cells[1, 1, row, 3])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                #endregion

                row++;

                worksheet.Cells[row, 1].Value = "ACCOUNT NUMBER";
                worksheet.Cells[row, 2].Value = "ACCOUNT NAME";
                worksheet.Cells[row, 1, row, 2].Style.Font.Bold = true;
                row++;

                var groupByLevelOne = generalLedgers
                    .OrderBy(gl => gl.Account.AccountNumber)
                    .GroupBy(gl => new { gl.Account.ParentAccount.ParentAccount.ParentAccount.AccountNumber, gl.Account.ParentAccount.ParentAccount.ParentAccount.AccountName });

                decimal nibit = 0;
                foreach (var gl in groupByLevelOne)
                {
                    worksheet.Cells[row, 1].Value = gl.Key.AccountNumber;
                    worksheet.Cells[row, 2].Value = gl.Key.AccountName;
                    if (int.TryParse(gl.Key.AccountNumber, out int accountNumber) && accountNumber < 400000000)
                    {
                        worksheet.Cells[row, 3].Value = gl.Sum(g => g.Debit - g.Credit);
                        worksheet.Cells[row, 3].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 3].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    }
                    else
                    {
                        worksheet.Cells[row, 4].Value = gl.Sum(g => g.Debit - g.Credit);
                        worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                        worksheet.Cells[row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                        if (nibit == 0)
                        {
                            nibit += (decimal)worksheet.Cells[row, 4].Value;
                            row++;
                            continue;
                        }

                        nibit -= (decimal)worksheet.Cells[row, 4].Value;
                    }

                    row++;
                }

                worksheet.Cells[row, 2].Value = "NIBIT";
                worksheet.Cells[row, 4].Value = nibit;
                worksheet.Cells[row, 4].Style.Numberformat.Format = currencyFormat;
                worksheet.Cells[row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                worksheet.Cells.AutoFitColumns();

                worksheet.Column(1).Width = 17;
                worksheet.Column(2).Width = 23;
                worksheet.Column(3).Width = 17;

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Level One Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(LevelOneReport));
            }
        }

        #endregion

        [HttpGet]
        public IActionResult TrialBalanceReport()
        {
            return View();
        }

        #region -- Trial Balance Report Excel File --

        public async Task<IActionResult> TrialBalanceReport(ViewModelBook model, CancellationToken cancellationToken)
        {
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var companyClaims = await GetCompanyClaimAsync();

                var currentLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac.ParentAccount) // Level 2
                    .ThenInclude(ac => ac.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date >= dateFrom &&
                        gl.Date <= dateTo &&
                        gl.AccountId != null && //Uncomment this if the GL is fix
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var priorLedgers = await _dbContext.FilprideGeneralLedgerBooks
                    .Include(gl => gl.Account) // Level 4
                    .ThenInclude(ac => ac.ParentAccount) // Level 3
                    .ThenInclude(ac => ac.ParentAccount) // Level 2
                    .ThenInclude(ac => ac.ParentAccount) // Level 1
                    .Where(gl =>
                        gl.Date < dateFrom &&
                        gl.AccountId != null && //Uncomment this if the GL is fix
                        gl.Company == companyClaims)
                    .ToListAsync(cancellationToken);

                var chartOfAccounts = await _dbContext.FilprideChartOfAccounts
                    .OrderBy(coa => coa.AccountNumber)
                    .ToListAsync(cancellationToken);

                if (!currentLedgers.Any() || !priorLedgers.Any())
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(TrialBalanceReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("TrialBalance");
                var alignmentCenter = ExcelHorizontalAlignment.Center;


                // Set the column headers
                worksheet.Cells["B1"].Value = "FILPRIDE RESOURCES INC.";
                worksheet.Cells["B1"].Style.HorizontalAlignment = alignmentCenter;

                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, "img\\Filpride.jpg");
                var imageFile = new FileInfo(imagePath);

                if (imageFile.Exists)
                {
                    worksheet.Column(2).Width = 80;
                    worksheet.Row(2).Height = 80;
                    var picture = worksheet.Drawings.AddPicture(Guid.NewGuid().ToString(), imageFile);
                    picture.SetPosition(1, 15, 1, 15);
                    picture.SetSize(330, 75);
                }

                worksheet.Cells["B3"].Value = "TRIAL BALANCE";
                worksheet.Cells["B3"].Style.HorizontalAlignment = alignmentCenter;
                worksheet.Cells["B4"].Value =
                    $"For the Period {dateFrom.ToString("MMM dd")} to {dateTo.ToString("MMM dd, yyyy")}";
                worksheet.Cells["B4"].Style.HorizontalAlignment = alignmentCenter;

                worksheet.Cells["A7"].Value = "ACCOUNT NUMBER";
                worksheet.Cells["B7"].Value = "ACCOUNT NAME";
                worksheet.Cells["C7"].Value = "NORMAL BALANCE";
                worksheet.Cells["D7"].Value = "LEVEL";

                var mergedCellBegBal = worksheet.Cells["E6:F6"];
                mergedCellBegBal.Merge = true;
                mergedCellBegBal.Value = "BEG BALANCES";
                mergedCellBegBal.Style.HorizontalAlignment = alignmentCenter;

                var mergedCellTransForThePeriod = worksheet.Cells["G6:H6"];
                mergedCellTransForThePeriod.Merge = true;
                mergedCellTransForThePeriod.Value = "TRANSACTIONS FOR THE PERIOD";
                mergedCellTransForThePeriod.Style.HorizontalAlignment = alignmentCenter;

                var mergedCellEndBal = worksheet.Cells["I6:J6"];
                mergedCellEndBal.Merge = true;
                mergedCellEndBal.Value = "ENDING BALANCES";
                mergedCellEndBal.Style.HorizontalAlignment = alignmentCenter;

                worksheet.Cells["E7"].Value = "DR";
                worksheet.Cells["F7"].Value = "CR";
                worksheet.Cells["G7"].Value = "DR";
                worksheet.Cells["H7"].Value = "CR";
                worksheet.Cells["I7"].Value = "DR";
                worksheet.Cells["J7"].Value = "CR";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:J7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                worksheet.Cells["E:J"].Style.Numberformat.Format = "#,##0.00_);[Red](#,##0.00)";

                int row = 8;
                decimal totalBeginningDr = 0;
                decimal totalBeginningCr = 0;
                decimal totalcurrentDr = 0;
                decimal totalcurrentCr = 0;
                decimal totalEndingDr = 0;
                decimal totalEndingCr = 0;


                foreach (var account in chartOfAccounts)
                {
                    decimal beginningDr = priorLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Debit);
                    decimal beginningCr = priorLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Credit);
                    decimal currentDr = currentLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Debit);
                    decimal currentCr = currentLedgers.Where(p => p.AccountNo == account.AccountNumber).Sum(p => p.Credit);

                    worksheet.Cells[row, 1].Value = account.AccountNumber;
                    worksheet.Cells[row, 2].Value = account.AccountName;
                    worksheet.Cells[row, 3].Value = account.NormalBalance;
                    worksheet.Cells[row, 4].Value = account.Level;
                    worksheet.Cells[row, 5].Value = beginningDr != 0 ? beginningDr : null;
                    totalBeginningDr += beginningDr;
                    worksheet.Cells[row, 6].Value = beginningCr != 0 ? beginningCr : null;
                    totalBeginningCr += beginningCr;
                    worksheet.Cells[row, 7].Value = currentDr != 0 ? currentDr : null;
                    totalcurrentDr += currentDr;
                    worksheet.Cells[row, 8].Value = currentCr != 0 ? currentCr : null;
                    totalcurrentCr += currentCr;

                    decimal endingDr = beginningDr + currentDr - beginningCr - currentCr;
                    decimal endingCr = beginningCr + currentCr - beginningDr - currentDr;

                    worksheet.Cells[row, 9].Value = endingDr != 0 ? endingDr : null;
                    totalEndingDr += endingDr;
                    worksheet.Cells[row, 10].Value = endingCr != 0 ? endingCr : null;
                    totalEndingCr += endingCr;


                    row++;
                }

                row += 3;
                worksheet.Cells[row, 2].Value = "TOTALS";
                worksheet.Cells[row, 5].Value = totalBeginningDr;
                worksheet.Cells[row, 6].Value = totalBeginningCr;
                decimal beginningGrandTotal = totalBeginningDr - totalBeginningCr;
                worksheet.Cells[row + 1, 6].Value = beginningGrandTotal != 0 ? beginningGrandTotal : null;


                worksheet.Cells[row, 7].Value = totalcurrentDr;
                worksheet.Cells[row, 8].Value = totalcurrentCr;
                decimal currentGrandTotal = totalcurrentDr - totalcurrentCr;
                worksheet.Cells[row + 1, 8].Value = currentGrandTotal != 0 ? currentGrandTotal : null;


                worksheet.Cells[row, 9].Value = totalEndingDr;
                worksheet.Cells[row, 10].Value = totalEndingCr;
                decimal endingGrandTotal = totalEndingDr - totalEndingCr;
                worksheet.Cells[row + 1, 10].Value = endingGrandTotal != 0 ? endingGrandTotal : null;

                // Auto-fit columns for better readability
                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                using (var range = worksheet.Cells[row, 5, row, 10])
                {
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // Convert the Excel package to a byte array
                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Trial Balance Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                return RedirectToAction(nameof(TrialBalanceReport));
            }
        }

        #endregion -- Trial Balance Report Excel File --

    }
}
