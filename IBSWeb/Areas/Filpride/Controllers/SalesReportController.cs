using FastReport.Web;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Filpride.ViewModels;
using IBS.Services.Attributes;
using IBS.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IBSWeb.Areas.Filpride.Controllers
{
    [Area(nameof(Filpride))]
    [CompanyAuthorize(nameof(Filpride))]
    public class SalesReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IUnitOfWork _unitOfWork;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public SalesReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        [HttpGet]
        public IActionResult SalesReport()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GeneratedSalesReport(ViewModelBook model)
        {
            var companyClaims = await GetCompanyClaimAsync();
            if (ModelState.IsValid)
            {
                try
                {
                    var sales = await _unitOfWork.FilprideReport.GetSalesReport(model.DateFrom, model.DateTo, companyClaims);

                    if (sales.Any())
                    {
                        var report = new WebReport();

                        var reportPath = Path.Combine(_webHostEnvironment.WebRootPath, "reports", "SalesReport.frx");
                        report.Report.Load(reportPath);

                        report.Report.SetParameterValue("DateFrom", model.DateFrom.ToString(SD.Date_Format));
                        report.Report.SetParameterValue("DateTo", model.DateTo.ToString(SD.Date_Format));
                        report.Report.Dictionary.RegisterBusinessObject(sales, "SalesReport", 4, true);
                        report.Report.GetDataSource("SalesReport").Enabled = true;

                        return View(report);
                    }

                    TempData["error"] = "No records found!";
                    return RedirectToAction(nameof(SalesReport));;
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(SalesReport));
                }
            }

            TempData["error"] = "Please input date from";
            return RedirectToAction(nameof(SalesReport));
        }
    }
}
