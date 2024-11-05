using IBS.DataAccess.Repository.IRepository;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace IBSWeb.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubContext = hubContext;
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
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            int count = await _unitOfWork.Notifications.GetUnreadNotificationCountAsync(_userManager.GetUserId(User));
            return Json(count);
        }
    }
}