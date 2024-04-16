using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ChartOfAccountController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<ChartOfAccountController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public ChartOfAccountController(IUnitOfWork unitOfWork, ILogger<ChartOfAccountController> logger, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<ChartOfAccount> coa = await _unitOfWork
                .ChartOfAccount
                .GetAllAsync();

            return View(coa);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ChartOfAccount model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {

            }

            ModelState.AddModelError("", "Make sure to fill all the required details.");
            return View(model);
        }
    }
}
