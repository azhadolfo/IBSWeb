using IBS.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Authorize]
    [Area("User")]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;

        private readonly IUnitOfWork _unitOfWork;

        public ReportController(ILogger<ReportController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult TrialBalance(CancellationToken cancellationToken)
        {
            try
            {
                var summary = _unitOfWork.FilprideChartOfAccount
                .GetSummaryReportView(cancellationToken);

                return View(summary.OrderBy(x => x.AccountNumber));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest();
            }
        }
    }
}
