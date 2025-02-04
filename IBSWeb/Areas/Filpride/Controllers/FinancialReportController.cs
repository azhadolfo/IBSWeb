using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Services.Attributes;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class FinancialReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        public FinancialReportController(ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
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


    }
}
