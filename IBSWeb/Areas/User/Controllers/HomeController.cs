using IBS.DataAccess.Data;
using IBS.Models;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Dynamic.Core;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility;
using IBS.Utility.Enums;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;

            var dashboardCounts = new DashboardCountViewModel
            {
                SupplierAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.HaulerAppointed) || cos.Status == nameof(CosStatus.Created))
                    .CountAsync(),

                HaulerAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.SupplierAppointed) || cos.Status == nameof(CosStatus.Created))
                    .CountAsync(),

                ATLBookingCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForAtlBooking))
                    .CountAsync(),

                OMApprovalCOSCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfOM))
                    .CountAsync(),

                OMApprovalDRCount = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.Status == nameof(CosStatus.ForApprovalOfOM))
                    .CountAsync(),

                OMApprovalPOCount = await _dbContext.FilpridePurchaseOrders
                    .Where(po => po.Status == nameof(CosStatus.ForApprovalOfOM))
                    .CountAsync(),

                FMApprovalCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfFM))
                    .CountAsync(),

                DRCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForDR))
                    .CountAsync(),

                InTransitCount = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.Status == nameof(DRStatus.PendingDelivery))
                    .CountAsync(),

                ForInvoiceCount = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.Status == nameof(DRStatus.ForInvoicing))
                    .CountAsync(),

                RecordLiftingDateCount = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => !dr.HasReceivingReport && dr.CanceledBy == null && dr.VoidedBy == null)
                    .CountAsync(),
            };

            return View(dashboardCounts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Maintenance()
        {
            if (await _dbContext.AppSettings
                    .Where(s => s.SettingKey == "MaintenanceMode")
                    .Select(s => s.Value == "true")
                    .FirstOrDefaultAsync())
            {
                return View("Maintenance");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
