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
        public IActionResult Generate()
        {
            return View();
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