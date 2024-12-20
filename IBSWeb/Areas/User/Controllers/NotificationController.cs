﻿using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Utility.Constants;
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

        private readonly ILogger<NotificationController> _logger;

        public NotificationController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager,
            IHubContext<NotificationHub> hubContext, ApplicationDbContext dbContext,
            ILogger<NotificationController> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _hubContext = hubContext;
            _dbContext = dbContext;
            _logger = logger;
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

        [HttpPost]
        public async Task<IActionResult> RespondToNotification(Guid userNotificationId, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return BadRequest("Response cannot be null or empty.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (response.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    var userNotification = await _dbContext.UserNotifications.FindAsync(userNotificationId);

                    if (userNotification == null)
                    {
                        return NotFound($"Notification with ID {userNotificationId} not found.");
                    }

                    userNotification.RequiresResponse = false;
                    userNotification.IsRead = true;

                    var lockDrAppSetting = await _dbContext.AppSettings
                        .FirstOrDefaultAsync(a => a.SettingKey == AppSettingKey.LockTheCreationOfDr);

                    if (lockDrAppSetting != null)
                    {
                        lockDrAppSetting.Value = "false";
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred while responding to notification.");
                TempData["error"] = "An error occurred while processing your request.";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Notification response processed successfully.";
            return RedirectToAction(nameof(Index));
        }


    }
}
