using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Linq.Expressions;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class GeneralLedgerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<GeneralLedgerController> _logger;

        public GeneralLedgerController(IUnitOfWork unitOfWork, ILogger<GeneralLedgerController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IActionResult> DisplayByTransaction()
        {
            IEnumerable<GeneralLedger> ledgers = await _unitOfWork
                .GeneralLedger
                .GetAllAsync();

            return View(ledgers);
        }

        public IActionResult GetJournal()
        {
            return View();
        }

        public async Task<IActionResult> DisplayByJournal(string journal, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(journal))
            {
                journal = journal.ToUpper();

                IEnumerable<GeneralLedger> ledgers = await _unitOfWork
                    .GeneralLedger
                    .GetAllAsync(g => g.JournalReference == journal, cancellationToken);

                ViewData["Journal"] = journal;
                return View(ledgers);
            }

            TempData["error"] = "Please select journal.";
            return View();
        }

        public async Task<IActionResult> GetAccountNo()
        {
            GeneralLedger model = new()
            {
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountAsyncByNo(),
                Products = await _unitOfWork.GetProductsAsyncByCode()
            };

            return View(model);
        }

        public async Task<IActionResult> DisplayByAccountNumber(string accountNo, string productCode, DateOnly dateFrom, DateOnly dateTo, bool exportToExcel, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(accountNo))
            {
                TempData["error"] = "Please select account number.";
                return View();
            }

            ChartOfAccount chartOfAccount = await _unitOfWork.ChartOfAccount.GetAsync(c => c.AccountNumber == accountNo);

            if (chartOfAccount == null)
            {
                TempData["error"] = "Invalid account number.";
                return View();
            }

            ViewData["AccountNo"] = chartOfAccount.AccountNumber;
            ViewData["AccountName"] = chartOfAccount.AccountName;
            ViewData["ProductCode"] = productCode;
            ViewData["DateFrom"] = dateFrom;
            ViewData["DateTo"] = dateTo;

            Expression<Func<GeneralLedger, bool>> filter = g =>
                g.TransactionDate >= dateFrom && g.TransactionDate <= dateTo &&
                g.AccountNumber == accountNo && (productCode == "ALL" || g.ProductCode == productCode);

            IEnumerable<GeneralLedger> ledgers = await _unitOfWork.GeneralLedger.GetAllAsync(filter, cancellationToken);

            if (exportToExcel)
            {
                // Create the Excel package
                using (var package = new ExcelPackage())
                {
                    // Add a new worksheet to the Excel package
                    var worksheet = package.Workbook.Worksheets.Add("GeneralLedger");

                    // Set the column headers
                    worksheet.Cells["A1"].Value = "Date";
                    worksheet.Cells["B1"].Value = "Particular";
                    worksheet.Cells["C1"].Value = "Debit";
                    worksheet.Cells["D1"].Value = "Credit";
                    worksheet.Cells["E1"].Value = "Balance";

                    // Populate the data rows
                    int row = 2;
                    decimal balance = 0;
                    foreach (var journal in ledgers.OrderBy(j => j.AccountNumber))
                    {
                        balance += journal.Debit + journal.Credit;

                        worksheet.Cells[row, 1].Value = journal.TransactionDate;
                        worksheet.Cells[row, 2].Value = journal.Particular;
                        worksheet.Cells[row, 3].Value = journal.Debit;
                        worksheet.Cells[row, 4].Value = journal.Credit;
                        worksheet.Cells[row, 5].Value = balance;

                        row++;
                    }

                    // Auto-fit columns for better readability
                    worksheet.Cells.AutoFitColumns();

                    // Convert the Excel package to a byte array
                    var excelBytes = package.GetAsByteArray();

                    // Return the Excel file as a download
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GeneralLedger.xlsx");
                }
            }
            else
            {
                return View(ledgers);
            }
        }

    }
}
