using IBS.DataAccess.Repository.IRepository;
using IBS.Models.MasterFile;
using IBS.Models.Mobility;
using IBS.Models.Mobility.ViewModels;
using IBS.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class GeneralLedgerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<GeneralLedgerController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public GeneralLedgerController(IUnitOfWork unitOfWork, ILogger<GeneralLedgerController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult GetTransaction()
        {
            return View();
        }

        public async Task<IActionResult> DisplayByTransaction(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork
                .MobilityGeneralLedger
                .GetLedgerViewByTransaction(dateFrom, dateTo, stationCodeClaim, cancellationToken);

            return View(ledgers);
        }

        public IActionResult GetJournal()
        {
            return View();
        }

        public async Task<IActionResult> DisplayByJournal(DateOnly dateFrom, DateOnly dateTo, string journal, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(journal))
            {
                var user = await _userManager.GetUserAsync(User);
                var claims = await _userManager.GetClaimsAsync(user);
                var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

                IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork
                    .MobilityGeneralLedger
                    .GetLedgerViewByJournal(dateFrom, dateTo, stationCodeClaim, journal, cancellationToken);

                ViewData["Journal"] = journal.ToUpper();
                return View(ledgers);
            }

            TempData["error"] = "Please select journal.";
            return View();
        }

        public async Task<IActionResult> GetAccountNo(CancellationToken cancellationToken)
        {
            MobilityGeneralLedger model = new()
            {
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountListAsyncByNo(cancellationToken),
                Products = await _unitOfWork.GetProductListAsyncByCode(cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> DisplayByAccountNumber(string accountNo, string productCode, DateOnly dateFrom, DateOnly dateTo, bool exportToExcel, CancellationToken cancellationToken)
        {
            accountNo = string.IsNullOrEmpty(accountNo) ? "ALL" : accountNo;
            productCode = string.IsNullOrEmpty(productCode) ? "ALL" : productCode;
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;

            ChartOfAccount chartOfAccount = await _unitOfWork.ChartOfAccount.GetAsync(c => c.AccountNumber == accountNo, cancellationToken);

            SetViewData(chartOfAccount, accountNo, productCode, dateFrom, dateTo);

            IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork.MobilityGeneralLedger.GetLedgerViewByAccountNo(dateFrom, dateTo, stationCodeClaim, accountNo, productCode, cancellationToken);

            if (exportToExcel && ledgers.Any())
            {
                try
                {
                    var excelBytes = _unitOfWork.MobilityGeneralLedger.ExportToExcel(ledgers, dateTo, dateFrom, ViewData["AccountNo"], ViewData["AccountName"], productCode);

                    // Return the Excel file as a download
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GeneralLedger.xlsx");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in exporting excel.");
                    TempData["error"] = $"Error: '{ex.Message}'";
                    return RedirectToAction(nameof(GetAccountNo));
                }
            }
            else
            {
                return View(ledgers);
            }
        }

        private void SetViewData(ChartOfAccount chartOfAccount, string accountNo, string productCode, DateOnly dateFrom, DateOnly dateTo)
        {
            ViewData["AccountNo"] = chartOfAccount?.AccountNumber ?? accountNo;
            ViewData["AccountName"] = chartOfAccount?.AccountName ?? accountNo;
            ViewData["ProductCode"] = productCode;
            ViewData["DateFrom"] = dateFrom.ToString("MMM/dd/yyyy");
            ViewData["DateTo"] = dateTo.ToString("MMM/dd/yyyy");
        }
    }
}