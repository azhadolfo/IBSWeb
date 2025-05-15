using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Mobility;
using IBS.Models.Mobility.MasterFile;
using IBS.Models.Mobility.ViewModels;
using IBS.Services.Attributes;
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

        private async Task<string?> GetStationCodeClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "StationCode")?.Value;
        }

        public IActionResult GetTransaction()
        {
            return View();
        }

        public async Task<IActionResult> DisplayByTransaction(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        {
            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork
                .MobilityGeneralLedger
                .GetLedgerViewByTransaction(dateFrom, dateTo, stationCodeClaims, cancellationToken);

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
                var stationCodeClaims = await GetStationCodeClaimAsync();

                if (stationCodeClaims == null)
                {
                    return BadRequest();
                }

                IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork
                    .MobilityGeneralLedger
                    .GetLedgerViewByJournal(dateFrom, dateTo, stationCodeClaims, journal, cancellationToken);

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
                Products = await _unitOfWork.GetMobilityProductListAsyncByCode(cancellationToken)
            };

            return View(model);
        }

        public async Task<IActionResult> DisplayByAccountNumber(string accountNo, string productCode, DateOnly dateFrom, DateOnly dateTo, bool exportToExcel, CancellationToken cancellationToken)
        {
            accountNo = string.IsNullOrEmpty(accountNo) ? "ALL" : accountNo;
            productCode = string.IsNullOrEmpty(productCode) ? "ALL" : productCode;

            var stationCodeClaims = await GetStationCodeClaimAsync();

            if (stationCodeClaims == null)
            {
                return BadRequest();
            }

            MobilityChartOfAccount chartOfAccount = await _unitOfWork.MobilityChartOfAccount.GetAsync(c => c.AccountNumber == accountNo, cancellationToken);

            SetViewData(chartOfAccount, accountNo, productCode, dateFrom, dateTo);

            IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork.MobilityGeneralLedger.GetLedgerViewByAccountNo(dateFrom, dateTo, stationCodeClaims, accountNo, productCode, cancellationToken);

            if (exportToExcel && ledgers.Any())
            {
                try
                {
                    var excelBytes = _unitOfWork.MobilityGeneralLedger.ExportToExcel(ledgers, dateTo, dateFrom, ViewData["AccountNo"]!, ViewData["AccountName"]!, productCode);

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

        private void SetViewData(MobilityChartOfAccount chartOfAccount, string accountNo, string productCode, DateOnly dateFrom, DateOnly dateTo)
        {
            ViewData["AccountNo"] = chartOfAccount?.AccountNumber ?? accountNo;
            ViewData["AccountName"] = chartOfAccount?.AccountName ?? accountNo;
            ViewData["ProductCode"] = productCode;
            ViewData["DateFrom"] = dateFrom.ToString("MMM/dd/yyyy");
            ViewData["DateTo"] = dateTo.ToString("MMM/dd/yyyy");
        }
    }
}
