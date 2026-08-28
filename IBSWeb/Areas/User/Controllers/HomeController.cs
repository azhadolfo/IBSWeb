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
        private readonly IServiceScopeFactory _scopeFactory;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
            _scopeFactory = scopeFactory;
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

            var userFullName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                               ?? findUser?.Name ?? string.Empty;

            bool isAdmin = User.IsInRole("Admin");
            bool isHead = User.IsInRole("HeadApprover");
            bool isAccounting = User.IsInRole("AccountingManager") || User.IsInRole("ManagementAccountingManager");
            bool isFinance = User.IsInRole("FinanceManager");
            bool isOps = User.IsInRole("OperationManager");
            bool isCnc = User.IsInRole("CncManager");
            bool isMarketing = User.IsInRole("MarketingSupervisor");

            var twoMonthsAgo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")).AddMonths(-2);

            var terminalStatuses = new HashSet<string>
            {
                nameof(CosStatus.Completed),
                nameof(CosStatus.Disapproved),
                nameof(CosStatus.Expired),
                nameof(CosStatus.Closed),
                nameof(CheckVoucherInvoiceStatus.Paid),
                nameof(CheckVoucherInvoiceStatus.Canceled),
                nameof(CheckVoucherInvoiceStatus.Voided),
                nameof(JvStatus.Posted),
                nameof(CheckVoucherPaymentStatus.Liquidated),
                nameof(DRStatus.PendingDelivery),
                nameof(DRStatus.ForInvoicing)
            };

            var countTask = RunCountQueriesAsync(companyClaims ?? string.Empty);
            var submissionTask = RunSubmissionQueriesAsync(userFullName, companyClaims ?? string.Empty, twoMonthsAgo, terminalStatuses);
            var approvalTask = RunApprovalQueriesAsync(
                isAdmin,
                isHead,
                isFinance,
                isAccounting,
                isOps,
                isCnc,
                isMarketing,
                companyClaims ?? string.Empty,
                twoMonthsAgo);

            await Task.WhenAll(countTask, submissionTask, approvalTask);

            var dashboardCounts = await countTask;
            dashboardCounts.UserFullName = userFullName;
            dashboardCounts.ShowPriority = isAdmin || isHead || isAccounting || isFinance || isOps || isCnc || isMarketing;
            dashboardCounts.MySubmissions = await submissionTask;
            dashboardCounts.PendingMyApproval = await approvalTask;

            EnrichSidebarItems(dashboardCounts.MySubmissions);
            EnrichSidebarItems(dashboardCounts.PendingMyApproval);

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

        private async Task<DashboardCountViewModel> RunCountQueriesAsync(string companyClaims)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var counts = new DashboardCountViewModel();

            counts.MarketingApprovalCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.ForApprovalOfMarketing) &&
                    true)
                .CountAsync();
            counts.SupplierAppointmentCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.Created) &&
                    true)
                .CountAsync();
            counts.ATLBookingCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    !cos.IsCosAtlFinalized &&
                    !string.IsNullOrEmpty(cos.Depot) &&
                    cos.Status != nameof(CosStatus.Closed) &&
                    cos.Status != nameof(CosStatus.Disapproved) &&
                    cos.Status != nameof(CosStatus.Expired) &&
                    true)
                .CountAsync();
            counts.OMApprovalCOSCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.ForApprovalOfOM) &&
                    true)
                .CountAsync();
            counts.OMApprovalDRCount = await ctx.FilprideDeliveryReceipts
                .Where(dr =>
                    dr.Status == nameof(CosStatus.ForApprovalOfOM) &&
                    true)
                .CountAsync();
            counts.OMApprovalPOCount = await ctx.FilpridePurchaseOrders
                .Where(po =>
                    po.Status == nameof(CosStatus.ForApprovalOfOM) &&
                    true)
                .CountAsync();
            counts.CNCApprovalCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.ForApprovalOfCNC) &&
                    true)
                .CountAsync();
            counts.FMApprovalCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.ForApprovalOfFM) &&
                    true)
                .CountAsync();
            counts.FMApprovalDMCount = await ctx.FilprideDebitMemos
                .Where(dm =>
                    dm.Status == nameof(DmCmStatus.ForApprovalOfFM) &&
                    true)
                .CountAsync();
            counts.FMApprovalCMCount = await ctx.FilprideCreditMemos
                .Where(cm =>
                    cm.Status == nameof(DmCmStatus.ForApprovalOfFM) &&
                    true)
                .CountAsync();
            counts.DRCount = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.Status == nameof(CosStatus.ForDR) &&
                    true)
                .CountAsync();
            counts.InTransitCount = await ctx.FilprideDeliveryReceipts
                .Where(dr =>
                    dr.Status == nameof(DRStatus.PendingDelivery) &&
                    true)
                .CountAsync();
            counts.ForInvoiceCount = await ctx.FilprideDeliveryReceipts
                .Where(dr =>
                    dr.Status == nameof(DRStatus.ForInvoicing) &&
                    true)
                .CountAsync();
            counts.RecordLiftingDateCount = await ctx.FilprideDeliveryReceipts
                .Where(dr =>
                    !dr.HasReceivingReport &&
                    dr.CanceledBy == null &&
                    dr.VoidedBy == null &&
                    true)
                .CountAsync();
            counts.RecordSupplierDetails = await ctx.FilprideReceivingReports
                .Where(rr => (rr.SupplierDrNo == null || rr.SupplierInvoiceDate == null || rr.SupplierInvoiceNumber == null
                    || rr.WithdrawalCertificate == null || rr.CostBasedOnSoa == 0)
                    && rr.CanceledBy == null && rr.VoidedBy == null && true)
                .CountAsync();
            counts.JournalVoucherForApprovalCount = await ctx.FilprideJournalVoucherHeaders
                .Where(jv => jv.Status == nameof(JvStatus.ForApproval) && true)
                .CountAsync();
            counts.CheckVoucherNonTradeInvoiceForApprovalCount = await ctx.FilprideCheckVoucherHeaders
                .Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval) && true
                    && cv.CvType == nameof(CVType.Invoicing) && !cv.IsPayroll)
                .CountAsync();
            counts.CheckVoucherNonTradePayrollInvoiceForApprovalCount = await ctx.FilprideCheckVoucherHeaders
                .Where(cv => cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval) && true
                    && cv.CvType == nameof(CVType.Invoicing) && cv.IsPayroll)
                .CountAsync();

            return counts;
        }

        private async Task<List<PendingApprovalItem>> RunSubmissionQueriesAsync(
            string userFullName,
            string companyClaims,
            DateTime twoMonthsAgo,
            HashSet<string> terminalStatuses)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cosList = await ctx.FilprideCustomerOrderSlips
                .Where(cos =>
                    cos.CreatedBy == userFullName &&
                    true&&
                    cos.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(cos.Status))
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

            var drList = await ctx.FilprideDeliveryReceipts
                .Where(dr =>
                    dr.CreatedBy == userFullName &&
                    true&&
                    dr.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(dr.Status))
                .OrderByDescending(cos => cos.CreatedDate)
                .Take(20)
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

            var cvList = await ctx.FilprideCheckVoucherHeaders
                .Where(cv =>
                    cv.CreatedBy == userFullName &&
                    true&&
                    cv.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(cv.Status))
                .OrderByDescending(cv => cv.CreatedDate)
                .Take(20)
                .Select(cv => new PendingApprovalItem
                {
                    Id = cv.CheckVoucherHeaderId,
                    ReferenceNo = cv.CheckVoucherHeaderNo ?? "",
                    Type = "CV",
                    Status = cv.Status,
                    Area = "Filpride",
                    Controller = cv.CvType == nameof(CVType.Payment)
                        ? "CheckVoucherNonTradePayment"
                        : cv.IsPayroll
                            ? "CheckVoucherNonTradePayrollInvoice"
                            : "CheckVoucherNonTradeInvoice",
                    CreatedDate = cv.CreatedDate
                })
                .ToListAsync();

            var jvList = await ctx.FilprideJournalVoucherHeaders
                .Where(jv =>
                    jv.CreatedBy == userFullName &&
                    true&&
                    jv.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(jv.Status))
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

            var dmList = await ctx.FilprideDebitMemos
                .Where(dm =>
                    dm.CreatedBy == userFullName &&
                    true&&
                    dm.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(dm.Status))
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

            var cmList = await ctx.FilprideCreditMemos
                .Where(cm =>
                    cm.CreatedBy == userFullName &&
                    true&&
                    cm.CreatedDate >= twoMonthsAgo &&
                    !terminalStatuses.Contains(cm.Status))
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

            var all = new List<PendingApprovalItem>();
            all.AddRange(cosList);
            all.AddRange(drList);
            all.AddRange(cvList);
            all.AddRange(jvList);
            all.AddRange(dmList);
            all.AddRange(cmList);

            return all.OrderByDescending(s => s.CreatedDate).Take(20).ToList();
        }

        private async Task<List<PendingApprovalItem>> RunApprovalQueriesAsync(
            bool isAdmin,
            bool isHead,
            bool isFinance,
            bool isAccounting,
            bool isOps,
            bool isCnc,
            bool isMarketing,
            string companyClaims,
            DateTime twoMonthsAgo)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingApproval = new List<PendingApprovalItem>();

            if (isAdmin || isHead || isMarketing)
            {
                pendingApproval.AddRange(await TakeLatestAsync(ProjectCos(ctx.FilprideCustomerOrderSlips
                    .Where(cos =>
                        cos.Status == nameof(CosStatus.ForApprovalOfMarketing) &&
                        true&&
                        cos.CreatedDate >= twoMonthsAgo))));
            }

            if (isAdmin || isHead || isFinance)
            {
                pendingApproval.AddRange(await TakeLatestAsync(ProjectCos(ctx.FilprideCustomerOrderSlips
                    .Where(cos =>
                        cos.Status == nameof(CosStatus.ForApprovalOfFM) &&
                        true&&
                        cos.CreatedDate >= twoMonthsAgo))));

                pendingApproval.AddRange(await TakeLatestAsync(ProjectDm(ctx.FilprideDebitMemos
                    .Where(dm =>
                        dm.Status == nameof(DmCmStatus.ForApprovalOfFM) &&
                        true&&
                        dm.CreatedDate >= twoMonthsAgo))));

                pendingApproval.AddRange(await TakeLatestAsync(ProjectCm(ctx.FilprideCreditMemos
                    .Where(cm =>
                        cm.Status == nameof(DmCmStatus.ForApprovalOfFM) &&
                        true&&
                        cm.CreatedDate >= twoMonthsAgo))));
            }

            if (isAdmin || isHead || isOps)
            {
                pendingApproval.AddRange(await TakeLatestAsync(ProjectCos(ctx.FilprideCustomerOrderSlips
                    .Where(cos =>
                        cos.Status == nameof(CosStatus.ForApprovalOfOM) &&
                        true&&
                        cos.CreatedDate >= twoMonthsAgo))));

                pendingApproval.AddRange(await TakeLatestAsync(ProjectDr(ctx.FilprideDeliveryReceipts
                    .Where(dr =>
                        dr.Status == nameof(CosStatus.ForApprovalOfOM) &&
                        true&&
                        dr.CreatedDate >= twoMonthsAgo))));

                pendingApproval.AddRange(await TakeLatestAsync(ProjectPo(ctx.FilpridePurchaseOrders
                    .Where(po =>
                        po.Status == nameof(CosStatus.ForApprovalOfOM) &&
                        true&&
                        po.CreatedDate >= twoMonthsAgo))));
            }

            if (isAdmin || isHead || isAccounting)
            {
                pendingApproval.AddRange(await TakeLatestAsync(ProjectCv(ctx.FilprideCheckVoucherHeaders
                    .Where(cv =>
                        cv.Status == nameof(CheckVoucherInvoiceStatus.ForApproval) &&
                        true&&
                        cv.CreatedDate >= twoMonthsAgo && cv.CvType == nameof(CVType.Invoicing) &&
                        !cv.IsPayroll))));

                pendingApproval.AddRange(await TakeLatestAsync(ProjectJv(ctx.FilprideJournalVoucherHeaders
                    .Where(jv =>
                        jv.Status == nameof(JvStatus.ForApproval) &&
                        true&&
                        jv.CreatedDate >= twoMonthsAgo))));
            }

            if (isAdmin || isHead || isCnc)
            {
                pendingApproval.AddRange(await TakeLatestAsync(ProjectCos(ctx.FilprideCustomerOrderSlips
                    .Where(cos =>
                        cos.Status == nameof(CosStatus.ForApprovalOfCNC) &&
                        true&&
                        cos.CreatedDate >= twoMonthsAgo))));
            }

            return pendingApproval.OrderByDescending(s => s.CreatedDate).Take(30).ToList();
        }

        private static async Task<List<PendingApprovalItem>> TakeLatestAsync(IQueryable<PendingApprovalItem> query) =>
            await query.OrderByDescending(x => x.CreatedDate).Take(10).ToListAsync();

        private static IQueryable<PendingApprovalItem> ProjectCos(IQueryable<FilprideCustomerOrderSlip> query) =>
            query.Select(cos => new PendingApprovalItem
            {
                Id = cos.CustomerOrderSlipId,
                ReferenceNo = cos.CustomerOrderSlipNo,
                Type = "COS",
                Status = cos.Status,
                Area = "Filpride",
                Controller = "CustomerOrderSlip",
                CreatedDate = cos.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectDr(IQueryable<FilprideDeliveryReceipt> query) =>
            query.Select(dr => new PendingApprovalItem
            {
                Id = dr.DeliveryReceiptId,
                ReferenceNo = dr.DeliveryReceiptNo,
                Type = "DR",
                Status = dr.Status,
                Area = "Filpride",
                Controller = "DeliveryReceipt",
                CreatedDate = dr.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectPo(IQueryable<FilpridePurchaseOrder> query) =>
            query.Select(po => new PendingApprovalItem
            {
                Id = po.PurchaseOrderId,
                ReferenceNo = po.PurchaseOrderNo ?? "",
                Type = "PO",
                Status = po.Status,
                Area = "Filpride",
                Controller = "PurchaseOrder",
                CreatedDate = po.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectDm(IQueryable<FilprideDebitMemo> query) =>
            query.Select(dm => new PendingApprovalItem
            {
                Id = dm.DebitMemoId,
                ReferenceNo = dm.DebitMemoNo ?? "",
                Type = "DM",
                Status = dm.Status,
                Area = "Filpride",
                Controller = "DebitMemo",
                CreatedDate = dm.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectCm(IQueryable<FilprideCreditMemo> query) =>
            query.Select(cm => new PendingApprovalItem
            {
                Id = cm.CreditMemoId,
                ReferenceNo = cm.CreditMemoNo ?? "",
                Type = "CM",
                Status = cm.Status,
                Area = "Filpride",
                Controller = "CreditMemo",
                CreatedDate = cm.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectCv(IQueryable<FilprideCheckVoucherHeader> query) =>
            query.Select(cv => new PendingApprovalItem
            {
                Id = cv.CheckVoucherHeaderId,
                ReferenceNo = cv.CheckVoucherHeaderNo ?? "",
                Type = "CV",
                Status = cv.Status,
                Area = "Filpride",
                Controller = "CheckVoucherNonTradeInvoice",
                CreatedDate = cv.CreatedDate
            });

        private static IQueryable<PendingApprovalItem> ProjectJv(IQueryable<FilprideJournalVoucherHeader> query) =>
            query.Select(jv => new PendingApprovalItem
            {
                Id = jv.JournalVoucherHeaderId,
                ReferenceNo = jv.JournalVoucherHeaderNo ?? "",
                Type = "JV",
                Status = jv.Status,
                Area = "Filpride",
                Controller = "JournalVoucher",
                CreatedDate = jv.CreatedDate
            });

        private static string GetFilterType(string type, string status) => (type, status) switch
        {
            ("COS", nameof(CosStatus.ForApprovalOfMarketing)) => "ForMarketingApproval",
            ("COS", nameof(CosStatus.Created)) => "",
            ("COS", nameof(CosStatus.SupplierAppointed)) => "ForAppointSupplier",
            ("COS", nameof(CosStatus.HaulerAppointed)) => "ForAppointHauler",
            ("COS", nameof(CosStatus.ForAtlBooking)) => "",
            ("COS", nameof(CosStatus.ForApprovalOfOM)) => "ForOMApproval",
            ("COS", nameof(CosStatus.ForApprovalOfCNC)) => "ForCNCApproval",
            ("COS", nameof(CosStatus.ForApprovalOfFM)) => "ForFMApproval",
            ("COS", nameof(CosStatus.ForDR)) => "ForDR",
            ("CV", nameof(CheckVoucherInvoiceStatus.ForApproval)) => "ForApproval",
            ("JV", nameof(JvStatus.ForApproval)) => "ForApproval",
            ("DR", nameof(CosStatus.ForApprovalOfOM)) => "ForOMApproval",
            ("DR", nameof(DRStatus.PendingDelivery)) => "InTransit",
            ("DR", nameof(DRStatus.ForInvoicing)) => "ForInvoice",
            ("DM", nameof(DmCmStatus.ForApprovalOfFM)) => "ForFMApproval",
            ("CM", nameof(DmCmStatus.ForApprovalOfFM)) => "ForFMApproval",
            ("PO", nameof(CosStatus.ForApprovalOfOM)) => "ForOMApproval",
            _ => ""
        };

        private static string MapStatus(string status) => status switch
        {
            nameof(CosStatus.ForApprovalOfMarketing) => "Marketing Approval",
            nameof(CosStatus.Created) => "Created",
            nameof(CosStatus.SupplierAppointed) => "Supplier Appointed",
            nameof(CosStatus.HaulerAppointed) => "Hauler Appointed",
            nameof(CosStatus.ForAtlBooking) => "ATL Booking",
            nameof(CosStatus.ForApprovalOfOM) => "OM Approval",
            nameof(CosStatus.ForApprovalOfCNC) => "CNC Approval",
            nameof(CosStatus.ForApprovalOfFM) => "FM Approval",
            nameof(CosStatus.ForDR) => "For DR",
            nameof(CosStatus.Completed) => "Completed",
            nameof(CosStatus.Disapproved) => "Disapproved",
            nameof(CosStatus.Expired) => "Expired",
            nameof(CosStatus.Closed) => "Closed",
            nameof(DRStatus.PendingDelivery) => "Pending Delivery",
            nameof(DRStatus.ForInvoicing) => "For Invoicing",
            nameof(JvStatus.Pending) => "Pending",
            nameof(JvStatus.ForApproval) => "For Approval",
            nameof(JvStatus.Posted) => "Posted",
            nameof(DmCmStatus.ForPosting) => "For Posting",
            nameof(CheckVoucherInvoiceStatus.ForPayment) => "For Payment",
            nameof(CheckVoucherInvoiceStatus.Paid) => "Paid",
            nameof(CheckVoucherPaymentStatus.Liquidated) => "Liquidated",
            nameof(CheckVoucherPaymentStatus.Unliquidated) => "Unliquidated",
            "Canceled" => "Canceled",
            "Voided" => "Voided",
            _ => status
        };

        private void EnrichSidebarItems(IEnumerable<PendingApprovalItem> items)
        {
            foreach (var item in items)
            {
                item.FilterType = GetFilterType(item.Type, item.Status);
                item.DisplayStatus = MapStatus(item.Status);
                item.SidebarUrl = SidebarAction(item.Type) == "Index"
                    ? Url.Action("Index", item.Controller, new { area = item.Area, filterType = string.IsNullOrEmpty(item.FilterType) ? null : item.FilterType }) ?? "#"
                    : Url.Action(SidebarAction(item.Type), item.Controller, new { area = item.Area, id = item.Id }) ?? "#";
            }
        }

        private static string SidebarAction(string type) => type switch
        {
            "COS" => "Preview",
            "CV" or "JV" or "DM" or "CM" => "Print",
            _ => "Index"
        };
    }
}
