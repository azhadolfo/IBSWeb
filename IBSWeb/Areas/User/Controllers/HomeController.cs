using System.Diagnostics;
using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Filpride.ViewModels;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return string.Empty;
            }

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var findUser = await _dbContext.ApplicationUsers
                .Where(user => user.Id == _userManager.GetUserId(this.User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;
            var companyClaims = findUser != null ? await GetCompanyClaimAsync() : string.Empty;

            var dashboardCounts = new DashboardCountViewModel
            {
                #region -- Filpride and Mobility Station

                SupplierAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos =>
                            (cos.Status == nameof(CosStatus.HaulerAppointed) || cos.Status == nameof(CosStatus.Created)) && cos.Company == companyClaims)
                        .CountAsync(),

                HaulerAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos =>
                            (cos.Status == nameof(CosStatus.SupplierAppointed) || cos.Status == nameof(CosStatus.Created)) && cos.Company == companyClaims)
                        .CountAsync(),

                ATLBookingCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForAtlBooking) && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalCOSCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfOM) && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalDRCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(CosStatus.ForApprovalOfOM) && dr.Company == companyClaims)
                        .CountAsync(),

                OMApprovalPOCount = await _dbContext.FilpridePurchaseOrders
                        .Where(po => po.Status == nameof(CosStatus.ForApprovalOfOM) && po.Company == companyClaims)
                        .CountAsync(),

                FMApprovalCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfFM) && cos.Company == companyClaims)
                        .CountAsync(),

                DRCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForDR) && cos.Company == companyClaims)
                        .CountAsync(),

                InTransitCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.PendingDelivery) && dr.Company == companyClaims)
                        .CountAsync(),

                ForInvoiceCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.ForInvoicing) && dr.Company == companyClaims)
                        .CountAsync(),

                RecordLiftingDateCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => !dr.HasReceivingReport && dr.CanceledBy == null && dr.VoidedBy == null && dr.Company == companyClaims)
                        .CountAsync(),

                #endregion

                #region -- Mobility Station

                MobilityStationUnservePO = await _dbContext.MobilityPurchaseOrders
                        .Where(po => po.Status == "Posted" && !po.IsReceived)
                        .CountAsync(),

                #endregion

                #region -- MMSI

                MMSIServiceRequestForPosting = await _dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Posting")
                        .CountAsync(),

                MMSIDispatchTicketForTariff = await _dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Tariff")
                        .CountAsync(),

                MMSIDispatchTicketForApproval = await _dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Approval")
                        .CountAsync(),

                MMSIDispatchTicketForBilling = await _dbContext.MMSIDispatchTickets
                        .Where(po => po.Status == "For Billing")
                        .CountAsync(),

                MMSIBillingForCollection = await _dbContext.MMSIBillings
                        .Where(po => po.Status == "For Collection")
                        .CountAsync(),

                #endregion
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
