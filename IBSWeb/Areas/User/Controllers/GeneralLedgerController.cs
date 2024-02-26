using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Mvc;

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

        public async Task<IActionResult> Index()
        {
            IEnumerable<GeneralLedger> ledgers = await _unitOfWork
                .GeneralLedger
                .GetAllAsync();

            return View(ledgers);
        }
    }
}
