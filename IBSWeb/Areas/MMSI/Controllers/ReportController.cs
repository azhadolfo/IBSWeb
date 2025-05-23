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

namespace IBSWeb.Areas.MMSI.Controllers
{
    [Area(nameof(MMSI))]
    [CompanyAuthorize(nameof(MMSI))]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReportController> _logger;

        public ReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork,
            ILogger<ReportController> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
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
                var salesReport = await _unitOfWork.MMSIReport.GetSalesReport(model.DateFrom, model.DateTo, cancellationToken);

                // check if there is no record
                if (salesReport.Count == 0)
                {
                    TempData["error"] = "No Record Found";
                    return RedirectToAction(nameof(SalesReport));
                }

                // Create the Excel package
                string currencyFormatTwoDecimal = "#,##0.00";
                using var package = new ExcelPackage();

                var worksheet = package.Workbook.Worksheets.Add("Sales Report");

                worksheet.Cells["A1"].Value = "MALAYAN MARITIME SERVICES INC.";
                worksheet.Cells["A2"].Value = $"AR MONITORING AS OF {DateTime.Today:MM/dd/yyyy}";

                #region -- Header row --

                #region -- Details of Trip of Tugboats --

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
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, detailsOfTripOfTugboatsColStart, headerRow - 1, detailsOfTripOfTugboatsColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(192, 192, 192));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "DETAILS OF TRIPS OF TUGBOAT";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, detailsOfTripOfTugboatsColStart, headerRow, detailsOfTripOfTugboatsColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(153, 204, 255));
                }
                col = detailsOfTripOfTugboatsColEnd + 1;

                #endregion

                #region -- For PNL Use --

                var forPnlUseColStart = detailsOfTripOfTugboatsColEnd + 1;

                var mmsiTugboats = await _dbContext.MMSITugboats
                    .Where(t => t.IsCompanyOwned)
                    .OrderBy(t => t.TugboatName)
                    .ToListAsync(cancellationToken);

                var mmsiCustomers = await _dbContext.FilprideCustomers
                    .Where(t => t.IsActive && t.IsMMSI)
                    .OrderBy(t => t.CustomerName)
                    .ToListAsync(cancellationToken);

                worksheet.Cells[headerRow, col].Value = "NET SALES";
                foreach (var tugboat in mmsiTugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"INCOME FROM {tugboat.TugboatName}";
                }

                col++;
                worksheet.Cells[headerRow, col].Value = "INCOME FROM OTHER TUGS";

                foreach (var tugboat in mmsiTugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tugboat.TugboatName} # OF HOURS";
                }
                var forPnlUseColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, forPnlUseColStart, headerRow - 1, forPnlUseColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "FOR PNL USE";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, forPnlUseColStart, headerRow, forPnlUseColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                #endregion

                #region -- AP Ledger --

                var apLedgerColStart = col + 1;
                var tugboatOwners = await _dbContext.MMSITugboatOwners
                    .OrderBy(t => t.TugboatOwnerName)
                    .ToListAsync(cancellationToken);
                foreach (var tugboatOwner in tugboatOwners)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tugboatOwner.TugboatOwnerName}";
                }
                var apLedgerColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, apLedgerColStart, headerRow - 1, apLedgerColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 153, 0));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "AP LEDGER";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, apLedgerColStart, headerRow, apLedgerColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 153, 0));
                }

                #endregion

                #region -- AR Ledger --

                var arLedgerColStart = col + 1;
                var customers = await _dbContext.FilprideCustomers
                    .Where(c => c.IsMMSI && c.IsActive)
                    .OrderBy(t => t.CustomerName)
                    .ToListAsync(cancellationToken);
                foreach (var customer in customers)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{customer.CustomerName}";
                }

                col++;
                worksheet.Cells[headerRow, col].Value = "TOTAL";

                var arLedgerColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, arLedgerColStart, headerRow - 1, arLedgerColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(192, 192, 192));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "A/R LEDGER";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, arLedgerColStart, headerRow, arLedgerColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(192, 192, 192));
                }

                #endregion

                #region -- Number of ASSISTS --

                var numberOfAssistsCategory = new List<string> { "LOCAL (IOC)", "FOREIGN (IOC)", "LOCAL (OUTSIDE)", "FOREIGN (OUTSIDE)" };
                var numberOfAssistsColStart = col + 1;

                foreach (string category in numberOfAssistsCategory)
                {
                    foreach (var tugboat in mmsiTugboats)
                    {
                        col++;
                        worksheet.Cells[headerRow, col].Value = $"{tugboat.TugboatName} {category}";
                    }
                }

                col++;
                worksheet.Cells[headerRow, col].Value = "OTHER TUGS LOCAL";
                col++;
                worksheet.Cells[headerRow, col].Value = "OTHER TUGS FOREIGN";

                var numberOfAssistsColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, numberOfAssistsColStart, headerRow - 1, numberOfAssistsColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 0));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "Number of ASSISTS";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, numberOfAssistsColStart, headerRow, numberOfAssistsColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 0));
                }

                #endregion

                #region -- Number of Tending --

                var numberOfTendingColStart = col + 1;
                foreach (var tendingTugboat in mmsiTugboats)
                {
                    col++;
                    worksheet.Cells[headerRow, col].Value = $"{tendingTugboat.TugboatName}";
                }

                col++;
                worksheet.Cells[headerRow, col].Value = "OTHER TUGS";

                var numberOfTendingColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, numberOfTendingColStart, headerRow - 1, numberOfTendingColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 153));
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "Number of TENDING";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, numberOfTendingColStart, headerRow, numberOfTendingColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 204, 153));
                }

                #endregion

                #region -- Number of Tending Hours --

                var numberOfTendingHoursColStart = col + 1;
                var tendingHoursTugboats = await _dbContext.MMSITugboats.OrderBy(t => t.TugboatName)
                    .ToListAsync(cancellationToken);
                var numberOfTendingHoursCategories = new List<string> { "LOCAL", "FOREIGN" };

                foreach (var tendingTugboat in mmsiTugboats)
                {
                    foreach (string category in numberOfTendingHoursCategories)
                    {
                        col++;
                        worksheet.Cells[headerRow, col].Value = $"{tendingTugboat.TugboatName} {category}";
                    }
                }
                var numberOfTendingHoursColEnd = col;
                // formatting of main category
                using (var range = worksheet.Cells[headerRow - 1, numberOfTendingHoursColStart, headerRow - 1, numberOfTendingHoursColEnd])
                {
                    range.Merge = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Value = "Number of TENDING HOURS";
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 8;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, numberOfTendingHoursColStart, headerRow, numberOfTendingHoursColEnd])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                }

                #endregion

                #region -- DOC/UNDOC & PRINCIPAL --

                col += 2;
                worksheet.Cells[headerRow, col].Value = "DOC/UNDOC";
                col += 1;
                worksheet.Column(col).Width = 50;
                worksheet.Cells[headerRow, col].Value = "PRINCIPAL";
                // formatting of subcategories
                using (var range = worksheet.Cells[headerRow, col-1, headerRow, col])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(153, 204, 255));
                }

                #endregion

                worksheet.Row(6).Height = 64;
                using (var range = worksheet.Cells[6, 1, 6, col-3])
                {
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                using (var range = worksheet.Cells[6, col-1, 6, col])
                {
                    range.Style.Font.Size = 8;
                    range.Style.WrapText = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                }

                #endregion

                #region -- Contents --

                var dynamicStartCol = 0;
                var dynamicEndCol = 0;
                var colOfSumOfAr = 0;

                // contents starts here
                var row = 7;
                var contentStartRow = row;

                foreach (var sales in salesReport)
                {
                    decimal totalBillingToUse = 0;
                    if (sales.BillingId == null)
                    {
                        totalBillingToUse = sales.TotalNetRevenue;
                    }
                    else
                    {
                        totalBillingToUse = sales.TotalBilling;
                    }

                    worksheet.Cells[row, 1].Value = sales.Date;
                    worksheet.Cells[row, 1].Style.Numberformat.Format = "MM/dd/yyyy";

                    worksheet.Cells[row, 2].Value = $"{sales.DispatchNumber}";
                    worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells[row, 3].Value = $"{sales.Billing?.MMSIBillingNumber}";
                    worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    worksheet.Cells[row, 4].Value = $"{sales.Customer?.CustomerName}";
                    worksheet.Cells[row, 5].Value = $"{sales.Vessel?.VesselName}";
                    worksheet.Cells[row, 6].Value = $"{sales.Vessel?.VesselType}";
                    worksheet.Cells[row, 7].Value = $"{sales.Tugboat?.TugboatName}";

                    worksheet.Cells[row, 8].Value = $"{sales.Terminal?.Port?.PortName}";
                    if (sales.Vessel?.VesselType == "Foreign")
                    {
                        worksheet.Cells[row, 8].Style.Font.Color.SetColor(Color.Red);
                    }

                    worksheet.Cells[row, 9].Value = $"{sales.Terminal?.TerminalName}";
                    worksheet.Cells[row, 10].Value = $"{sales.Service?.ServiceName}";
                    worksheet.Cells[row, 11].Value = $"{sales.DateLeft:MM/dd/yyyy} {sales.TimeLeft:HH:mm}";
                    worksheet.Cells[row, 12].Value = $"{sales.DateArrived:MM/dd/yyyy} {sales.TimeArrived:HH:mm}";

                    worksheet.Cells[row, 13].Value = sales.TotalHours;
                    if (totalBillingToUse != 0)
                    {
                        worksheet.Cells[row, 14].Value = totalBillingToUse;
                        worksheet.Cells[row, 15].Value = totalBillingToUse;
                        using (var range = worksheet.Cells[row, 13, row, 15])
                        {
                            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            range.Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                    }

                    // BALANCE
                    if (totalBillingToUse != 0)
                    {
                        worksheet.Cells[row, 27].Value = totalBillingToUse;
                        worksheet.Cells[row, 27].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 27].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    // AP OTHER TUGS
                    if (sales.ApOtherTugs != 0)
                    {
                        worksheet.Cells[row, 28].Value = sales.ApOtherTugs;
                        worksheet.Cells[row, 28].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 28].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    // NET SALES
                    if ((totalBillingToUse - sales.ApOtherTugs) != 0)
                    {
                        worksheet.Cells[row, 29].Value = totalBillingToUse - sales.ApOtherTugs;
                        worksheet.Cells[row, 29].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells[row, 29].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    var writingCol = 29;

                    // subtotal col starts here
                    dynamicStartCol = 15;

                    // For PNL Use
                    // Incomes
                    foreach (var tugboat in mmsiTugboats)
                    {
                        writingCol++;
                        if (sales.Tugboat?.TugboatName == tugboat.TugboatName)
                        {
                            if (totalBillingToUse != 0)
                            {
                                worksheet.Cells[row, writingCol].Value = totalBillingToUse;
                                worksheet.Cells[row, writingCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                worksheet.Column(writingCol).Width = 10;
                            }
                        }
                    }

                    // Incomes other tugs
                    writingCol++;
                    if (!sales.Tugboat!.IsCompanyOwned)
                    {
                        if ((totalBillingToUse - sales.ApOtherTugs) != 0)
                        {
                            worksheet.Cells[row, writingCol].Value = totalBillingToUse - sales.ApOtherTugs;
                            worksheet.Cells[row, writingCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            worksheet.Column(writingCol).Width = 10;
                        }
                    }

                    // Total Hours
                    foreach (var tugboat in mmsiTugboats)
                    {
                        writingCol++;
                        if (sales.Tugboat.TugboatName == tugboat.TugboatName)
                        {
                            worksheet.Cells[row, writingCol].Value = sales.TotalHours;
                            worksheet.Cells[row, writingCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                            worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            worksheet.Column(writingCol).Width = 10;
                        }
                    }

                    // AP Ledger
                    foreach (var companyOwner in tugboatOwners)
                    {
                        writingCol++;
                        if (!sales.Tugboat.IsCompanyOwned && sales.Tugboat.TugboatOwner?.TugboatOwnerName == companyOwner.TugboatOwnerName)
                        {
                            if (sales.ApOtherTugs != 0)
                            {
                                worksheet.Cells[row, writingCol].Value = sales.ApOtherTugs;
                                worksheet.Cells[row, writingCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                worksheet.Column(writingCol).Width = 10;
                            }
                        }
                    }

                    // var for sum of AR
                    decimal? sumOfAr = 0m;

                    // write AR Ledgers
                    foreach (var customer in mmsiCustomers)
                    {
                        writingCol++;
                        if (sales.Customer?.CustomerName == customer.CustomerName)
                        {
                            if (totalBillingToUse != 0)
                            {
                                worksheet.Cells[row, writingCol].Value = totalBillingToUse;
                                worksheet.Cells[row, writingCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                                worksheet.Column(writingCol).Width = 10;
                                sumOfAr += sales.TotalBilling;
                            }
                        }
                    }

                    // write sum of AR
                    writingCol++;
                    colOfSumOfAr = writingCol;

                    if (sumOfAr != 0)
                    {
                        worksheet.Cells[row, writingCol].Value = sumOfAr;
                        worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    // Number of Assists(TO INQUIRE THE VALUES)
                    foreach (string category in numberOfAssistsCategory)
                    {
                        foreach (var tugboat in mmsiTugboats)
                        {
                            writingCol++;
                            if (tugboat.TugboatName == sales.Tugboat.TugboatName &&
                                sales.Service?.ServiceName == "ASSIST")
                            {
                                worksheet.Cells[row, writingCol].Value = 1;
                            }
                        }
                    }

                    // for other tugs local and foreign(TO INQUIRE VALUES)
                    writingCol += 2;

                    // Number of TENDING
                    foreach (var tugboat in mmsiTugboats)
                    {
                        writingCol++;
                        if (tugboat.TugboatName == sales.Tugboat.TugboatName &&
                            sales.Service?.ServiceName == "TENDING")
                        {
                            worksheet.Cells[row, writingCol].Value = 1;
                            worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                        }
                    }

                    // other tugs(not company owned)
                    writingCol++;
                    if (!sales.Tugboat.IsCompanyOwned && sales.Service?.ServiceName == "TENDING")
                    {
                        worksheet.Cells[row, writingCol].Value = 1;
                        worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }

                    // number of tending hours
                    var numberOfTendingHoursCategory = new List<string> { "LOCAL", "FOREIGN" };
                    foreach (var tugboat in mmsiTugboats)
                    {
                        foreach (string category in numberOfTendingHoursCategory)
                        {
                            writingCol++;
                            if (tugboat.TugboatName == sales.Tugboat.TugboatName &&
                                sales.Service?.ServiceName == "TENDING" &&
                                sales.Vessel?.VesselType == category)
                            {
                                worksheet.Cells[row, writingCol].Value = sales.TotalHours;
                                worksheet.Cells[row, writingCol].Style.Numberformat.Format = currencyFormatTwoDecimal;
                            }
                        }
                    }

                    // subtotal col ends here
                    dynamicEndCol = writingCol;

                    writingCol += 2;
                    worksheet.Cells[row, writingCol].Value = sales.Billing != null ? (sales.Billing.IsUndocumented ? "UNDOC" : "DOC") : null;
                    writingCol++;
                    worksheet.Cells[row, writingCol].Value = !string.IsNullOrEmpty(sales.Billing?.PrincipalId.ToString()) ? $"{sales.Billing?.Principal?.PrincipalName}" : "";

                    // next record
                    row++;
                }

                // content row ends here
                var contentEndRow = row - 1;
                var subtotalRow = row;

                #region -- Subtotals --

                // colvar == counter, loop through dynamic columns
                for (int colVar = dynamicStartCol; colVar <= dynamicEndCol; colVar++)
                {
                    // variable for sum
                    decimal? sumVar = 0;

                    // loop through all the rows
                    for (int rowVar = contentStartRow; rowVar <= contentEndRow; rowVar++)
                    {
                        // temporary container for value of the cell iterated
                        var tempValue = worksheet.Cells[rowVar, colVar].Value;

                        // if it has valid value, will clean the value and add to sum variable
                        if (tempValue != null && decimal.TryParse(tempValue.ToString(), out decimal cellValue))
                        {
                            sumVar += cellValue;
                        }
                    }

                    // write the sum to its cell
                    if (sumVar != 0)
                    {
                        worksheet.Cells[subtotalRow, colVar].Value = sumVar;
                        worksheet.Cells[subtotalRow, colVar].Style.Numberformat.Format = currencyFormatTwoDecimal;
                    }
                }

                // styling of subtotal row
                using (var range = worksheet.Cells[subtotalRow, 1, subtotalRow, dynamicEndCol])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(204, 255, 255));
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
                    range.Style.Font.Bold = true;
                }

                // styling of total label
                worksheet.Cells[subtotalRow, 1].Value = "TOTAL";
                worksheet.Cells[subtotalRow, 1].Style.Font.Bold = true;
                worksheet.Cells[subtotalRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                #endregion

                #endregion -- Contents --

                // formatting of cell columns
                worksheet.Column(1).Width = 13;
                worksheet.Column(2).Width = 9;
                worksheet.Column(3).Width = 12;
                worksheet.Column(4).Width = 45;
                worksheet.Column(5).Width = 20;
                worksheet.Column(6).Width = 12;
                worksheet.Column(7).Width = 20;
                worksheet.Column(8).Width = 15;
                worksheet.Column(9).Width = 18;
                worksheet.Column(10).Width = 22;
                worksheet.Column(11).Width = 19;
                worksheet.Column(12).Width = 17;
                worksheet.Column(13).Width = 7;
                worksheet.Column(14).Width = 13;
                worksheet.Column(15).Width = 14;
                worksheet.Column(16).Width = 12;
                worksheet.Column(17).Width = 12;
                worksheet.Column(18).Width = 12;
                worksheet.Column(19).Width = 12;
                worksheet.Column(20).Width = 12;
                worksheet.Column(21).Width = 12;
                worksheet.Column(22).Width = 12;
                worksheet.Column(23).Width = 12;
                worksheet.Column(24).Width = 12;
                worksheet.Column(25).Width = 12;
                worksheet.Column(26).Width = 12;
                worksheet.Column(27).Width = 14;
                worksheet.Column(28).Width = 9;
                worksheet.Column(29).Width = 14;
                worksheet.Column(colOfSumOfAr).Width = 13;

                var excelBytes = package.GetAsByteArray();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Sales Report_{DateTime.UtcNow.AddHours(8):yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                _logger.LogError(ex, "Error generating sales report. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                ex.Message, ex.StackTrace, _userManager.GetUserAsync(User));
                return RedirectToAction(nameof(SalesReport));
            }
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
    }
}
