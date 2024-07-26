using IBS.Utility;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Mobility.Controllers
{
    [Area(nameof(Mobility))]
    [CompanyAuthorize(nameof(Mobility))]
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}