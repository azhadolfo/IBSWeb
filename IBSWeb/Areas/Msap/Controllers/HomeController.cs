using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Msap.Controllers;

[Area("Msap")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
