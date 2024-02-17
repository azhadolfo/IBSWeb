using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class CashierReportController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public SalesVM SalesVM { get; set; }

        public CashierReportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<SalesHeader> salesHeader = await _unitOfWork
                .SalesHeader
                .GetAllAsync();

            return View(salesHeader);
        }

        [HttpGet]
        public async Task<IActionResult> Generate(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            SalesVM = new SalesVM
            {
                Header = await _unitOfWork.SalesHeader.GetAsync(sh => sh.SalesHeaderId == id),
                Details = await _unitOfWork.SalesDetail.GetAllAsync(sd => sd.SalesHeaderId == id)
            };

            return View(SalesVM);
        }

        //[HttpPost]
        //public async Task<IActionResult> Generate(SalesHeader model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //    }
        //}
    }
}