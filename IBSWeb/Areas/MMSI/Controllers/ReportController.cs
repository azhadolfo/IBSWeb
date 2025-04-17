using System.Drawing;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace IBSWeb.Areas.MMSI
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public ReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public IActionResult SalesReport()
        {
            return View();
        }

        public async Task<IActionResult> GenerateSalesReportExcelFile(ViewModelBook model,
            CancellationToken cancellationToken)
        {
            try
            {
                var dateFrom = model.DateFrom;
                var dateTo = model.DateTo;
                var extractedBy = _userManager.GetUserName(this.User);

                // get rr data from chosen date
                var salesReport = await _unitOfWork.Msap.GetSalesReport(model.DateFrom, model.DateTo, cancellationToken);

                // check if there is no record
                if (salesReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(SalesReport));
                }

                // Create the Excel package
                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add("Sales Report");

                worksheet.Cells["A1"].Value = "MALAYAN MARITIME SERVICES INC.";
                worksheet.Cells["A2"].Value = $"AR MONITORING AS OF {DateTime.Today:MM/dd/yyyy}";

                var detailsOfTripOfTugboatsColStart = 1;
                var col = 1;
                var headerRow = 6;

                worksheet.Cells[headerRow, 1].Value = "BILLING STATEMENT DATE/DISPATCH DATE";
                worksheet.Cells[headerRow, 2].Value = "DISPATCH TICKET NUMBER";
                worksheet.Cells[headerRow, 3].Value = "BILLING STATEMENT #";
                worksheet.Cells[headerRow, 4].Value = "CUSTOMER NAME";
                worksheet.Cells[headerRow, 5].Value = "NAME OF VESSEL";
                worksheet.Cells[headerRow, 6].Value = "TYPE OF VESSEL";
                worksheet.Cells[headerRow, 7].Value = "NAME OF TUGBOAT";
                worksheet.Cells[headerRow, 8].Value = "PORT";
                worksheet.Cells[headerRow, 9].Value = "TEMINAL";
                worksheet.Cells[headerRow, 10].Value = "NAME OF SERVICE";
                worksheet.Cells[headerRow, 11].Value = "TIME STARTED";
                worksheet.Cells[headerRow, 12].Value = "TIME END";
                worksheet.Cells[headerRow, 13].Value = "NO. OF HRS";
                worksheet.Cells[headerRow, 14].Value = "RATE";
                worksheet.Cells[headerRow, 15].Value = "GROSS SALES";
                worksheet.Cells[headerRow, 16].Value = "DATE DEPOSITED";
                worksheet.Cells[headerRow, 17].Value = "RECEIPT DATE";
                worksheet.Cells[headerRow, 18].Value = "RECEIPT NUMBER";
                worksheet.Cells[headerRow, 19].Value = "BANK";
                worksheet.Cells[headerRow, 20].Value = "VATABLE AMOUNT";
                worksheet.Cells[headerRow, 21].Value = "EWT";
                worksheet.Cells[headerRow, 22].Value = "AMOUNT DEPOSITED";
                worksheet.Cells[headerRow, 23].Value = "SBMA SHARE";
                worksheet.Cells[headerRow, 24].Value = "OVERPAYMENT";
                worksheet.Cells[headerRow, 25].Value = "AGENCY INCENTIVE";
                worksheet.Cells[headerRow, 26].Value = "AGENT COMMISSION";
                worksheet.Cells[headerRow, 27].Value = "BALANCE";
                worksheet.Cells[headerRow, 28].Value = "AP OTHER TUGS";

                var detailsOfTripOfTugboatsColEnd = 28;

                using (var range = worksheet.Cells[headerRow - 1, detailsOfTripOfTugboatsColStart, headerRow - 1, detailsOfTripOfTugboatsColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.DarkGray);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "DETAILS OF TRIPS OF TUGBOAT";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                using (var range = worksheet.Cells[headerRow, detailsOfTripOfTugboatsColStart, headerRow, detailsOfTripOfTugboatsColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }


                col = detailsOfTripOfTugboatsColEnd + 1;

                var forPnlUseColStart = detailsOfTripOfTugboatsColEnd + 1;
                var tugboats = await _dbContext.MMSITugboats.OrderBy(t => t.TugboatName)
                    .ToListAsync(cancellationToken);
                worksheet.Cells[headerRow, col].Value = "NET SALES";
                foreach (var tugboat in tugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"INCOME FROM {tugboat.TugboatName}";
                }
                var forPnlUseColEnd = col;

                var apLedgerStart = col + 1;
                var tugboatOwners = await _dbContext.MMSICompanyOwners.OrderBy(t => t.CompanyOwnerName)
                    .ToListAsync(cancellationToken);
                foreach (var tugboatOwner in tugboatOwners)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tugboatOwner.CompanyOwnerName}";
                }
                var apLedgerEnd = col;

                var arLedgerStart = col + 1;
                var customers = await _dbContext.MMSICustomers.OrderBy(t => t.CustomerName)
                    .ToListAsync(cancellationToken);
                foreach (var customer in customers)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{customer.CustomerName}";
                }
                var arLedgerEnd = col;

                // var numberOfAssistsStart = col + 1;
                //
                // var numberOfAssists = await _dbContext.MMSICustomers.OrderBy(t => t.CustomerName)
                //     .ToListAsync(cancellationToken);
                //
                // foreach (var customer in customers)
                // {
                //     col++;
                //     worksheet.Cells[headerRow, col].Value = $"{customer.CustomerName}";
                // }
                //
                // var numberOfAssistsEnd = col;

                var numberOfTendingStart = col + 1;
                var tendingTugboats = await _dbContext.MMSITugboats.OrderBy(t => t.TugboatName)
                    .ToListAsync(cancellationToken);
                foreach (var tendingTugboat in tendingTugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tendingTugboat.TugboatName}";
                }
                var numberOfTendingEnd = col;

                var numberOfTendingHoursStart = col + 1;
                var tendingHoursTugboats = await _dbContext.MMSITugboats.OrderBy(t => t.TugboatName)
                    .ToListAsync(cancellationToken);
                foreach (var tendingTugboat in tendingTugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tendingTugboat.TugboatName}";
                }
                var numberOfTendingHoursEnd = col;


                col += 2;
                worksheet.Cells[headerRow, col].Value = "DOC/UNDOC";
                col += 1;
                worksheet.Column(col).Width = 50;
                worksheet.Cells[headerRow, col].Value = "PRINCIPAL";














                worksheet.Row(6).Height = 15;
                worksheet.Cells.AutoFitColumns();

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Sales Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(SalesReport));
            }
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }
    }
}
