using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
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
                #region -- Filpride

                SupplierAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos =>
                            (cos.Status == nameof(CosStatus.HaulerAppointed) || cos.Status == nameof(CosStatus.Created))
                            && cos.Company == companyClaims)
                        .CountAsync(),

                HaulerAppointmentCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos =>
                        (cos.Status == nameof(CosStatus.SupplierAppointed) || cos.Status == nameof(CosStatus.Created))
                            && cos.Company == companyClaims)
                        .CountAsync(),

                ATLBookingCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => !cos.IsCosAtlFinalized
                                      && !string.IsNullOrEmpty(cos.Depot)
                                      && cos.Status != nameof(CosStatus.Closed)
                                      && cos.Status != nameof(CosStatus.Disapproved)
                                      && cos.Status != nameof(CosStatus.Expired)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalCOSCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfOM)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                OMApprovalDRCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(CosStatus.ForApprovalOfOM)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                OMApprovalPOCount = await _dbContext.FilpridePurchaseOrders
                        .Where(po => po.Status == nameof(CosStatus.ForApprovalOfOM)
                                     && po.Company == companyClaims)
                        .CountAsync(),

                CNCApprovalCount = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfCNC)
                                  && cos.Company == companyClaims)
                    .CountAsync(),

                FMApprovalCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfFM)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                FMApprovalDMCount = await _dbContext.FilprideDebitMemos
                        .Where(dm => dm.Status == nameof(DmCmStatus.ForApprovalOfFM)
                                     && dm.Company == companyClaims)
                        .CountAsync(),

                FMApprovalCMCount = await _dbContext.FilprideCreditMemos
                        .Where(cm => cm.Status == nameof(DmCmStatus.ForApprovalOfFM)
                                     && cm.Company == companyClaims)
                        .CountAsync(),

                DRCount = await _dbContext.FilprideCustomerOrderSlips
                        .Where(cos => cos.Status == nameof(CosStatus.ForDR)
                                      && cos.Company == companyClaims)
                        .CountAsync(),

                InTransitCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.PendingDelivery)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                ForInvoiceCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => dr.Status == nameof(DRStatus.ForInvoicing)
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                RecordLiftingDateCount = await _dbContext.FilprideDeliveryReceipts
                        .Where(dr => !dr.HasReceivingReport
                                     && dr.CanceledBy == null
                                     && dr.VoidedBy == null
                                     && dr.Company == companyClaims)
                        .CountAsync(),

                RecordSupplierDetails = await _dbContext.FilprideReceivingReports
                    .Where(rr => (rr.SupplierDrNo == null
                                  || rr.SupplierInvoiceDate == null
                                  || rr.SupplierInvoiceNumber == null
                                  || rr.WithdrawalCertificate == null
                                  || rr.SupplierDrNo == null
                                  || rr.CostBasedOnSoa == 0)
                                 && rr.CanceledBy == null
                                 && rr.VoidedBy == null
                                 && rr.Company == companyClaims)
                    .CountAsync(),

                #endregion -- Filpride

                #region -- Accounting - For Approval

                JournalVoucherForApprovalCount = await _dbContext.FilprideJournalVoucherHeaders
                        .Where(jv => jv.Status == nameof(JvStatus.ForApproval)
                                     && jv.Company == companyClaims)
                        .CountAsync(),

                CheckVoucherNonTradeInvoiceForApprovalCount = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval)
                                     && cv.Company == companyClaims
                                     && cv.CvType == nameof(CVType.Invoicing)
                                     && !cv.IsPayroll)
                        .CountAsync(),

                CheckVoucherNonTradePayrollInvoiceForApprovalCount = await _dbContext.FilprideCheckVoucherHeaders
                        .Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval)
                                     && cv.Company == companyClaims
                                     && cv.CvType == nameof(CVType.Invoicing)
                                     && cv.IsPayroll)
                        .CountAsync(),

                #endregion -- Accounting - For Approval
            };

            var userFullName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                               ?? User.Identity?.Name ?? string.Empty;
            dashboardCounts.UserFullName = userFullName;

            bool isAdmin = User.IsInRole("Admin");
            bool isHead = User.IsInRole("HeadApprover");
            bool isAccounting = User.IsInRole("AccountingManager") || User.IsInRole("ManagementAccountingManager");
            bool isFinance = User.IsInRole("FinanceManager");
            bool isOps = User.IsInRole("OperationManager");
            bool isCnc = User.IsInRole("CncManager");
            bool isPort = User.IsInRole("PortCoordinator");
            bool isCashier = User.IsInRole("Cashier");

            var twoMonthsAgo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")).AddMonths(-2);

            var terminalStatuses = new HashSet<string>
            {
                nameof(CosStatus.Completed), nameof(CosStatus.Disapproved),
                nameof(CosStatus.Expired), nameof(CosStatus.Closed),
                nameof(CheckVoucherInvoiceStatus.Paid),
                nameof(CheckVoucherInvoiceStatus.Canceled),
                nameof(CheckVoucherInvoiceStatus.Voided),
                nameof(JvStatus.Posted), nameof(JvStatus.Canceled), nameof(JvStatus.Voided),
                nameof(DmCmStatus.Posted), nameof(DmCmStatus.Voided), nameof(DmCmStatus.Canceled),
            };

            var cosTasks = await _dbContext.FilprideCustomerOrderSlips
                .Where(cos => cos.CreatedBy == userFullName && cos.Company == companyClaims
                              && cos.CreatedDate >= twoMonthsAgo
                              && !terminalStatuses.Contains(cos.Status))
                .OrderByDescending(cos => cos.CreatedDate)
                .Take(20)
                .Select(cos => new PendingApprovalItem
                {
                    Id = cos.CustomerOrderSlipId,
                    ReferenceNo = cos.CustomerOrderSlipNo,
                    Type = "COS",
                    Status = cos.Status,
                        Area = "Filpride",
                        Controller = "CustomerOrderSlip",
                        CreatedDate = cos.CreatedDate
                })
                .ToListAsync();

            var cvTasks = await _dbContext.FilprideCheckVoucherHeaders
                .Where(cv => cv.CreatedBy == userFullName && cv.Company == companyClaims
                             && cv.CreatedDate >= twoMonthsAgo
                             && cv.Status != nameof(CheckVoucherInvoiceStatus.Paid)
                             && cv.Status != nameof(CheckVoucherInvoiceStatus.Canceled)
                             && cv.Status != nameof(CheckVoucherInvoiceStatus.Voided)
                             && cv.Status != nameof(CheckVoucherPaymentStatus.Posted)
                             && cv.Status != nameof(CheckVoucherPaymentStatus.Liquidated)
                             && cv.Status != nameof(CheckVoucherPaymentStatus.Unliquidated))
                .OrderByDescending(cv => cv.CreatedDate)
                .Take(20)
                .Select(cv => new PendingApprovalItem
                {
                    Id = cv.CheckVoucherHeaderId,
                    ReferenceNo = cv.CheckVoucherHeaderNo ?? "",
                    Type = "CV",
                    Status = cv.Status,
                    Area = "Filpride",
                    Controller = "CheckVoucherNonTradeInvoice",
                    CreatedDate = cv.CreatedDate
                })
                .ToListAsync();

            var jvTasks = await _dbContext.FilprideJournalVoucherHeaders
                .Where(jv => jv.CreatedBy == userFullName && jv.Company == companyClaims
                             && jv.CreatedDate >= twoMonthsAgo
                             && jv.Status != nameof(JvStatus.Posted)
                             && jv.Status != nameof(JvStatus.Canceled)
                             && jv.Status != nameof(JvStatus.Voided))
                .OrderByDescending(jv => jv.CreatedDate)
                .Take(20)
                .Select(jv => new PendingApprovalItem
                {
                    Id = jv.JournalVoucherHeaderId,
                    ReferenceNo = jv.JournalVoucherHeaderNo ?? "",
                    Type = "JV",
                    Status = jv.Status,
                    Area = "Filpride",
                    Controller = "JournalVoucher",
                    CreatedDate = jv.CreatedDate
                })
                .ToListAsync();

            var dmTasks = await _dbContext.FilprideDebitMemos
                .Where(dm => dm.CreatedBy == userFullName && dm.Company == companyClaims
                             && dm.CreatedDate >= twoMonthsAgo
                             && dm.Status != nameof(DmCmStatus.Posted)
                             && dm.Status != nameof(DmCmStatus.Voided)
                             && dm.Status != nameof(DmCmStatus.Canceled))
                .OrderByDescending(dm => dm.CreatedDate)
                .Take(20)
                .Select(dm => new PendingApprovalItem
                {
                    Id = dm.DebitMemoId,
                    ReferenceNo = dm.DebitMemoNo ?? "",
                    Type = "DM",
                    Status = dm.Status,
                    Area = "Filpride",
                    Controller = "DebitMemo",
                    CreatedDate = dm.CreatedDate
                })
                .ToListAsync();

            var cmTasks = await _dbContext.FilprideCreditMemos
                .Where(cm => cm.CreatedBy == userFullName && cm.Company == companyClaims
                             && cm.CreatedDate >= twoMonthsAgo
                             && cm.Status != nameof(DmCmStatus.Posted)
                             && cm.Status != nameof(DmCmStatus.Voided)
                             && cm.Status != nameof(DmCmStatus.Canceled))
                .OrderByDescending(cm => cm.CreatedDate)
                .Take(20)
                .Select(cm => new PendingApprovalItem
                {
                    Id = cm.CreditMemoId,
                    ReferenceNo = cm.CreditMemoNo ?? "",
                    Type = "CM",
                    Status = cm.Status,
                    Area = "Filpride",
                    Controller = "CreditMemo",
                    CreatedDate = cm.CreatedDate
                })
                .ToListAsync();

            static string GetFilterType(string type, string status) => (type, status) switch
            {
                ("COS", "ForApprovalOfMarketing") => "ForMarketingApproval",
                ("COS", "Created") => "",
                ("COS", "SupplierAppointed") => "ForAppointSupplier",
                ("COS", "HaulerAppointed") => "ForAppointHauler",
                ("COS", "ForAtlBooking") => "",
                ("COS", "ForApprovalOfOM") => "ForOMApproval",
                ("COS", "ForApprovalOfCNC") => "ForCNCApproval",
                ("COS", "ForApprovalOfFM") => "ForFMApproval",
                ("COS", "ForDR") => "ForDR",
                ("CV", "ForApproval") => "ForApproval",
                ("JV", "ForApproval") => "ForApproval",
                ("DR", "ForApprovalOfOM") => "ForOMApproval",
                ("DR", "PendingDelivery") => "InTransit",
                ("DR", "ForInvoicing") => "ForInvoice",
                ("DM", "ForApprovalOfFM") => "ForFMApproval",
                ("CM", "ForApprovalOfFM") => "ForFMApproval",
                _ => ""
            };

            var allSubmissions = new List<PendingApprovalItem>();
            allSubmissions.AddRange(cosTasks);
            allSubmissions.AddRange(cvTasks);
            allSubmissions.AddRange(jvTasks);
            allSubmissions.AddRange(dmTasks);
            allSubmissions.AddRange(cmTasks);
            foreach (var item in allSubmissions)
            {
                item.FilterType = GetFilterType(item.Type, item.Status);
            }
            dashboardCounts.MySubmissions = allSubmissions
                .OrderByDescending(s => s.CreatedDate)
                .Take(20)
                .ToList();

            var pendingApproval = new List<PendingApprovalItem>();

            if (isAdmin || isHead || isFinance)
            {
                var fmCos = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfFM) && cos.Company == companyClaims && cos.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(cos => cos.CreatedDate)
                    .Take(10)
                    .Select(cos => new PendingApprovalItem
                    {
                        Id = cos.CustomerOrderSlipId,
                        ReferenceNo = cos.CustomerOrderSlipNo,
                        Type = "COS",
                        Status = cos.Status,
                        Area = "Filpride",
                        Controller = "CustomerOrderSlip",
                        CreatedDate = cos.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(fmCos);

                var fmDm = await _dbContext.FilprideDebitMemos
                    .Where(dm => dm.Status == nameof(DmCmStatus.ForApprovalOfFM) && dm.Company == companyClaims && dm.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(dm => dm.CreatedDate)
                    .Take(10)
                    .Select(dm => new PendingApprovalItem
                    {
                        Id = dm.DebitMemoId,
                        ReferenceNo = dm.DebitMemoNo ?? "",
                        Type = "DM",
                        Status = dm.Status,
                        Area = "Filpride",
                        Controller = "DebitMemo",
                        CreatedDate = dm.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(fmDm);

                var fmCm = await _dbContext.FilprideCreditMemos
                    .Where(cm => cm.Status == nameof(DmCmStatus.ForApprovalOfFM) && cm.Company == companyClaims && cm.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(cm => cm.CreatedDate)
                    .Take(10)
                    .Select(cm => new PendingApprovalItem
                    {
                        Id = cm.CreditMemoId,
                        ReferenceNo = cm.CreditMemoNo ?? "",
                        Type = "CM",
                        Status = cm.Status,
                        Area = "Filpride",
                        Controller = "CreditMemo",

                        CreatedDate = cm.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(fmCm);
            }

            if (isAdmin || isHead || isOps)
            {
                var omCos = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfOM) && cos.Company == companyClaims && cos.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(cos => cos.CreatedDate)
                    .Take(10)
                    .Select(cos => new PendingApprovalItem
                    {
                        Id = cos.CustomerOrderSlipId,
                        ReferenceNo = cos.CustomerOrderSlipNo,
                        Type = "COS",
                        Status = cos.Status,
                        Area = "Filpride",
                        Controller = "CustomerOrderSlip",

                        CreatedDate = cos.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(omCos);

                var omDr = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.Status == nameof(CosStatus.ForApprovalOfOM) && dr.Company == companyClaims && dr.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(dr => dr.CreatedDate)
                    .Take(10)
                    .Select(dr => new PendingApprovalItem
                    {
                        Id = dr.DeliveryReceiptId,
                        ReferenceNo = dr.DeliveryReceiptNo,
                        Type = "DR",
                        Status = dr.Status,
                        Area = "Filpride",
                        Controller = "DeliveryReceipt",

                        CreatedDate = dr.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(omDr);

                var omPo = await _dbContext.FilpridePurchaseOrders
                    .Where(po => po.Status == nameof(CosStatus.ForApprovalOfOM) && po.Company == companyClaims && po.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(po => po.CreatedDate)
                    .Take(10)
                    .Select(po => new PendingApprovalItem
                    {
                        Id = po.PurchaseOrderId,
                        ReferenceNo = po.PurchaseOrderNo ?? "",
                        Type = "PO",
                        Status = po.Status,
                        Area = "Filpride",
                        Controller = "PurchaseOrder",

                        CreatedDate = po.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(omPo);
            }

            if (isAdmin || isHead || isAccounting)
            {
                var cvApproval = await _dbContext.FilprideCheckVoucherHeaders
                    .Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval)
                                 && cv.Company == companyClaims
                                 && cv.CreatedDate >= twoMonthsAgo
                                 && cv.CvType == nameof(CVType.Invoicing)
                                 && !cv.IsPayroll)
                    .OrderByDescending(cv => cv.CreatedDate)
                    .Take(10)
                    .Select(cv => new PendingApprovalItem
                    {
                        Id = cv.CheckVoucherHeaderId,
                        ReferenceNo = cv.CheckVoucherHeaderNo ?? "",
                        Type = "CV",
                        Status = cv.Status,
                        Area = "Filpride",
                        Controller = "CheckVoucherNonTradeInvoice",

                        CreatedDate = cv.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(cvApproval);

                var jvApproval = await _dbContext.FilprideJournalVoucherHeaders
                    .Where(jv => jv.Status == nameof(JvStatus.ForApproval) && jv.Company == companyClaims)
                    .OrderByDescending(jv => jv.CreatedDate)
                    .Take(10)
                    .Select(jv => new PendingApprovalItem
                    {
                        Id = jv.JournalVoucherHeaderId,
                        ReferenceNo = jv.JournalVoucherHeaderNo ?? "",
                        Type = "JV",
                        Status = jv.Status,
                        Area = "Filpride",
                        Controller = "JournalVoucher",

                        CreatedDate = jv.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(jvApproval);
            }

            if (isAdmin || isHead || isCnc)
            {
                var cncCos = await _dbContext.FilprideCustomerOrderSlips
                    .Where(cos => cos.Status == nameof(CosStatus.ForApprovalOfCNC) && cos.Company == companyClaims && cos.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(cos => cos.CreatedDate)
                    .Take(10)
                    .Select(cos => new PendingApprovalItem
                    {
                        Id = cos.CustomerOrderSlipId,
                        ReferenceNo = cos.CustomerOrderSlipNo,
                        Type = "COS",
                        Status = cos.Status,
                        Area = "Filpride",
                        Controller = "CustomerOrderSlip",

                        CreatedDate = cos.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(cncCos);
            }

            if (isAdmin || isHead || isOps || isPort)
            {
                var inTransitDr = await _dbContext.FilprideDeliveryReceipts
                    .Where(dr => dr.Status == nameof(DRStatus.PendingDelivery) && dr.Company == companyClaims && dr.CreatedDate >= twoMonthsAgo)
                    .OrderByDescending(dr => dr.CreatedDate)
                    .Take(10)
                    .Select(dr => new PendingApprovalItem
                    {
                        Id = dr.DeliveryReceiptId,
                        ReferenceNo = dr.DeliveryReceiptNo,
                        Type = "DR",
                        Status = dr.Status,
                        Area = "Filpride",
                        Controller = "DeliveryReceipt",

                        CreatedDate = dr.CreatedDate
                    })
                    .ToListAsync();
                pendingApproval.AddRange(inTransitDr);
            }

            dashboardCounts.PendingMyApproval = pendingApproval
                .OrderByDescending(s => s.CreatedDate)
                .Take(30)
                .ToList();
            foreach (var item in dashboardCounts.PendingMyApproval)
            {
                item.FilterType = GetFilterType(item.Type, item.Status);
            }

            return View(dashboardCounts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
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
