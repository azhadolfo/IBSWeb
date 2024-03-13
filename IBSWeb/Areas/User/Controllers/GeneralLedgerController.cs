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

        public async Task<IActionResult> DisplayByTransaction(CancellationToken cancellationToken)
        {
            IEnumerable<GeneralLedger> ledgers = await _unitOfWork
                .GeneralLedger
                .GetAllAsync(g => g.IsValidated, cancellationToken);

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
                    .GetAllAsync(g => g.JournalReference == journal && g.IsValidated, cancellationToken);

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
            if (string.IsNullOrEmpty(accountNo) || string.IsNullOrEmpty(productCode))
            {
                TempData["error"] = "Please select account and product.";
                return RedirectToAction(nameof(GetAccountNo));
            }

            ChartOfAccount chartOfAccount = await _unitOfWork.ChartOfAccount.GetAsync(c => c.AccountNumber == accountNo);

            if (chartOfAccount == null)
            {
                TempData["error"] = "Invalid account number.";
                return RedirectToAction(nameof(GetAccountNo));
            }

            ViewData["AccountNo"] = chartOfAccount.AccountNumber;
            ViewData["AccountName"] = chartOfAccount.AccountName;
            ViewData["ProductCode"] = productCode;
            ViewData["DateFrom"] = dateFrom;
            ViewData["DateTo"] = dateTo;

            Expression<Func<GeneralLedger, bool>> filter = g =>
                g.TransactionDate >= dateFrom && g.TransactionDate <= dateTo &&
                g.AccountNumber == accountNo && g.IsValidated && (productCode == "ALL" || g.ProductCode == productCode);

            IEnumerable<GeneralLedger> ledgers = await _unitOfWork.GeneralLedger.GetAllAsync(filter, cancellationToken);

            if (exportToExcel && ledgers.Any())
            {
                try
                {
                    var excelBytes = _unitOfWork.GeneralLedger.ExportToExcel(ledgers, dateTo, dateFrom, accountNo, chartOfAccount.AccountName, productCode);

                    // Return the Excel file as a download
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GeneralLedger.xlsx");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in exporting excel.");
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GetAccountNo));
                }
            }
            else
            {
                return View(ledgers);
            }
        }

    }
}
