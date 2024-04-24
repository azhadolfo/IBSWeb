﻿using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
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
                .GeneralLedger
                .GetLedgerViewByTransaction(dateFrom, dateTo, stationCodeClaim, cancellationToken);

            return View(ledgers);
        }

        public IActionResult GetJournal()
        {
            return View();
        }

        public async Task<IActionResult> DisplayByJournal(DateOnly dateFrom, DateOnly dateTo, string journal, CancellationToken cancellationToken)
        {
            if (!String.IsNullOrEmpty(journal))
            {
                var user = await _userManager.GetUserAsync(User);
                var claims = await _userManager.GetClaimsAsync(user);
                var stationCodeClaim = claims.FirstOrDefault(c => c.Type == "StationCode").Value;


                IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork
                    .GeneralLedger
                    .GetLedgerViewByJournal(dateFrom, dateTo, stationCodeClaim, journal, cancellationToken);

                ViewData["Journal"] = journal.ToUpper();
                return View(ledgers);
            }

            TempData["error"] = "Please select journal.";
            return View();
        }

        public async Task<IActionResult> GetAccountNo(CancellationToken cancellationToken)
        {
            GeneralLedger model = new()
            {
                ChartOfAccounts = await _unitOfWork.GetChartOfAccountAsyncByNo(cancellationToken),
                Products = await _unitOfWork.GetProductsAsyncByCode(cancellationToken)
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

            IEnumerable<GeneralLedgerView> ledgers = await _unitOfWork.GeneralLedger.GetLedgerViewByAccountNo(dateFrom, dateTo, stationCodeClaim, accountNo, productCode, cancellationToken);

            if (exportToExcel && ledgers.Any())
            {
                try
                {
                    var excelBytes = _unitOfWork.GeneralLedger.ExportToExcel(ledgers, dateTo, dateFrom, ViewData["AccountNo"], ViewData["AccountName"], productCode);

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
