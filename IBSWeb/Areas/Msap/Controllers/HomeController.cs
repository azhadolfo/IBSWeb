using IBS.DataAccess.Data;
using IBS.Models;
using IBS.Models.ViewModels;
using IBS.Utility.Constants;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IBSWeb.Areas.Msap.Controllers
{
    [Area("Msap")]
    public class HomeController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
        : Controller
    {
        private async Task<string?> GetCompanyClaimAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return string.Empty;
            }

            var claims = await userManager.GetClaimsAsync(user);
            return claims.FirstOrDefault(c => c.Type == "Company")?.Value;
        }

        public async Task<IActionResult> Index()
        {
            var findUser = await dbContext.ApplicationUsers
                .Where(user => user.Id == userManager.GetUserId(User))
                .FirstOrDefaultAsync();

            ViewBag.GetUserDepartment = findUser?.Department;
            var dashboardCounts = new DashboardCountViewModel
            {
                #region -- MMSI

                MsapServiceRequestForPosting = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == SD.ServiceRequestStatus.Requested)
                        .CountAsync(),

                MsapDispatchTicketForTariff = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Tariff")
                        .CountAsync(),

                MsapDispatchTicketForApproval = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Approval")
                        .CountAsync(),

                MsapDispatchTicketForBilling = await dbContext.MsapDispatchTickets
                        .Where(po => po.Status == "For Billing")
                        .CountAsync(),

                MsapBillingForCollection = await dbContext.MsapBillings
                        .Where(po => po.Status == "For Collection")
                        .CountAsync(),

                #endregion -- MMSI
            };

            return View(dashboardCounts);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetDashboardData(CancellationToken cancellationToken)
        {
            var findUser = await dbContext.ApplicationUsers
                .Where(user => user.Id == userManager.GetUserId(User))
                .FirstOrDefaultAsync(cancellationToken);

            if (findUser == null)
            {
                return Challenge();
            }

            var companyClaims = await GetCompanyClaimAsync();
            if (companyClaims != SD.Company_MMSI || User.IsInRole("PortCoordinator"))
            {
                return Forbid();
            }

            // Fetch Job Orders counts
            var joOpen = await dbContext.MsapJobOrders.CountAsync(j => j.Status == SD.JobOrderStatus.Open, cancellationToken);
            var joClosed = await dbContext.MsapJobOrders.CountAsync(j => j.Status == SD.JobOrderStatus.Closed, cancellationToken);
            var joTotal = joOpen + joClosed;

            // Fetch Dispatch Tickets counts
            var dtForTariff = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status == SD.DispatchTicketStatus.ForTariff, cancellationToken);
            var dtForApproval = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status == SD.DispatchTicketStatus.ForApproval, cancellationToken);
            var dtDisapproved = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status == SD.DispatchTicketStatus.Disapproved, cancellationToken);
            var dtForBilling = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status == SD.DispatchTicketStatus.ForBilling, cancellationToken);
            var dtBilled = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status == SD.DispatchTicketStatus.Billed, cancellationToken);
            var dtTotal = await dbContext.MsapDispatchTickets.CountAsync(d => d.Status != SD.DispatchTicketStatus.Deleted, cancellationToken);

            // Fetch Billings counts & amounts
            var bForPosting = await dbContext.MsapBillings.CountAsync(b => b.Status == SD.BillingStatus.ForPosting, cancellationToken);
            var bForCollection = await dbContext.MsapBillings.CountAsync(b => b.Status == SD.BillingStatus.ForCollection, cancellationToken);
            var bCollected = await dbContext.MsapBillings.CountAsync(b => b.Status == SD.BillingStatus.Collected, cancellationToken);
            var bTotal = bForPosting + bForCollection + bCollected;

            var bForPostingAmount = await dbContext.MsapBillings.Where(b => b.Status == SD.BillingStatus.ForPosting).SumAsync(b => (decimal?)b.Amount, cancellationToken) ?? 0m;
            var bForCollectionAmount = await dbContext.MsapBillings.Where(b => b.Status == SD.BillingStatus.ForCollection).SumAsync(b => (decimal?)b.Amount, cancellationToken) ?? 0m;
            var bCollectedAmount = await dbContext.MsapBillings.Where(b => b.Status == SD.BillingStatus.Collected).SumAsync(b => (decimal?)b.Amount, cancellationToken) ?? 0m;

            // Fetch Collections counts
            var cActive = await dbContext.MsapCollections.CountAsync(c => c.VoidedDate == null && c.CanceledDate == null, cancellationToken);
            var cVoided = await dbContext.MsapCollections.CountAsync(c => c.VoidedDate != null, cancellationToken);
            var cTotal = await dbContext.MsapCollections.CountAsync(cancellationToken);

            // Fetch Monthly trends (billings and collections)
            var phTime = DateTimeHelper.GetCurrentPhilippineTime();
            var today = DateOnly.FromDateTime(phTime);
            var startDate = today.AddMonths(-5);
            startDate = new DateOnly(startDate.Year, startDate.Month, 1);

            var billingsTrend = await dbContext.MsapBillings
                .Where(b => b.Date >= startDate && b.Status != "Cancelled")
                .GroupBy(b => new { b.Date.Year, b.Date.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Amount = g.Sum(b => b.Amount)
                })
                .ToListAsync(cancellationToken);

            var collectionsTrend = await dbContext.MsapCollections
                .Where(c => c.Date >= startDate && c.VoidedDate == null && c.CanceledDate == null)
                .GroupBy(c => new { c.Date.Year, c.Date.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Amount = g.Sum(c => c.Amount)
                })
                .ToListAsync(cancellationToken);

            var monthlyTrends = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var targetDate = today.AddMonths(-i);
                var year = targetDate.Year;
                var month = targetDate.Month;
                var monthName = targetDate.ToString("MMM");

                var billing = billingsTrend.FirstOrDefault(b => b.Year == year && b.Month == month);
                var collection = collectionsTrend.FirstOrDefault(c => c.Year == year && c.Month == month);

                monthlyTrends.Add(new {
                    MonthName = monthName,
                    Year = year,
                    BillingCount = billing?.Count ?? 0,
                    BillingAmount = billing?.Amount ?? 0m,
                    CollectionCount = collection?.Count ?? 0,
                    CollectionAmount = collection?.Amount ?? 0m
                });
            }

            // Recent activity (Job Orders, Dispatch Tickets, Billings)
            var recentDispatchTickets = await dbContext.MsapDispatchTickets
                .OrderByDescending(d => d.CreatedDate)
                .Take(5)
                .Select(d => new {
                    Type = "Dispatch Ticket",
                    Number = d.DispatchNumber,
                    d.Date,
                    d.Status,
                    User = d.CreatedBy,
                    Time = d.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var recentBillings = await dbContext.MsapBillings
                .OrderByDescending(b => b.CreatedDate)
                .Take(5)
                .Select(b => new {
                    Type = "Billing",
                    Number = b.MsapBillingNumber,
                    b.Date,
                    b.Status,
                    User = b.CreatedBy ?? "System",
                    Time = b.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var recentJobOrders = await dbContext.MsapJobOrders
                .OrderByDescending(j => j.CreatedDate)
                .Take(5)
                .Select(j => new {
                    Type = "Job Order",
                    Number = j.JobOrderNumber,
                    j.Date,
                    j.Status,
                    User = j.CreatedBy ?? "System",
                    Time = j.CreatedDate
                })
                .ToListAsync(cancellationToken);

            var recentActivityCombined = recentDispatchTickets
                .Concat(recentBillings)
                .Concat(recentJobOrders)
                .OrderByDescending(a => a.Time)
                .Take(10)
                .Select(a => new {
                    a.Type,
                    a.Number,
                    DateFormatted = a.Date.ToString(SD.Date_Format),
                    a.Status,
                    a.User,
                    TimeFormatted = FormatTimeAgo(a.Time)
                })
                .ToList();

            return Json(new {
                JobOrders = new { Open = joOpen, Closed = joClosed, Total = joTotal },
                DispatchTickets = new {
                    ForTariff = dtForTariff,
                    ForApproval = dtForApproval,
                    Disapproved = dtDisapproved,
                    ForBilling = dtForBilling,
                    Billed = dtBilled,
                    Total = dtTotal
                },
                Billings = new {
                    ForPosting = bForPosting,
                    ForPostingAmount = bForPostingAmount,
                    ForCollection = bForCollection,
                    ForCollectionAmount = bForCollectionAmount,
                    Collected = bCollected,
                    CollectedAmount = bCollectedAmount,
                    Total = bTotal
                },
                Collections = new {
                    Active = cActive,
                    Voided = cVoided,
                    Total = cTotal
                },
                MonthlyTrends = monthlyTrends,
                RecentActivity = recentActivityCombined
            });
        }

        private static string FormatTimeAgo(DateTime dateTime)
        {
            var span = DateTimeHelper.GetCurrentPhilippineTime() - dateTime;
            if (span.TotalDays > 365)
            {
                int years = (int)(span.TotalDays / 365);
                return years == 1 ? "1 year ago" : $"{years} years ago";
            }
            if (span.TotalDays > 30)
            {
                int months = (int)(span.TotalDays / 30);
                return months == 1 ? "1 month ago" : $"{months} months ago";
            }
            if (span.TotalDays >= 1)
            {
                int days = (int)span.TotalDays;
                return days == 1 ? "yesterday" : $"{days} days ago";
            }
            if (span.TotalHours >= 1)
            {
                int hours = (int)span.TotalHours;
                return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
            }
            if (span.TotalMinutes >= 1)
            {
                int minutes = (int)span.TotalMinutes;
                return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
            }
            return "just now";
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Maintenance()
        {
            if (await dbContext.AppSettings
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
