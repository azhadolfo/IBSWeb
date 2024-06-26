using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area("Filpride")]
    public class DeliveryReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        public DeliveryReportController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var drList = await _unitOfWork.FilprideDeliveryReport
                .GetAllAsync(null, cancellationToken);

            return View(drList);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            DeliveryReportViewModel viewModel = new()
            {
                Customers = await _unitOfWork.GetCustomerListAsync(cancellationToken),
                CustomerOrderSlips = await _unitOfWork.FilprideCustomerOrderSlip.GetCosListAsync(cancellationToken),
                Haulers = await _unitOfWork.Hauler.GetHaulerListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        //PENDING Create the post method and other crud operation
    }
}