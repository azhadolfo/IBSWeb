using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IHubContext<NotificationHub> _hubContext;

        private readonly ApplicationDbContext _dbContext;

        public NotificationController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager,
            IHubContext<NotificationHub> hubContext, ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _unitOfWork.Notifications.GetUserNotificationsAsync(_userManager.GetUserId(User));
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid userNotificationId)
        {
            await _unitOfWork.Notifications.MarkAsReadAsync(userNotificationId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var count = await _unitOfWork.Notifications.GetUnreadNotificationCountAsync(_userManager.GetUserId(User));

            if (count <= 0)
            {
                return Json(count);
            }

            var hubConnections = await _dbContext.HubConnections
                .Where(h => h.UserName == _userManager.GetUserName(User))
                .ToListAsync();

            foreach (var hubConnection in hubConnections)
            {
                await _hubContext.Clients.Client(hubConnection.ConnectionId)
                    .SendAsync("ReceivedNotification", $"You have {count} unread message.");
            }

            return Json(count);
        }

        [HttpPost]
        public async Task<IActionResult> Archive(Guid userNotificationId)
        {
            await _unitOfWork.Notifications.ArchiveAsync(userNotificationId);
            return RedirectToAction(nameof(Index));
        }
    }
}
