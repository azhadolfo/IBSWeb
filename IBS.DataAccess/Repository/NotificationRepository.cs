using IBS.DataAccess.Data;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Utility.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IBS.DataAccess.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _db;

        public NotificationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddNotificationAsync(string userId, string message, bool requiresResponse = false)
        {
            var notification = new Notification
            {
                Message = message,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
            };

            await _db.Notifications.AddAsync(notification);
            await _db.SaveChangesAsync();

            var userNotification = new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                RequiresResponse = requiresResponse
            };

            await _db.UserNotifications.AddAsync(userNotification);
            await _db.SaveChangesAsync();
        }

        public async Task AddNotificationToMultipleUsersAsync(List<string> userIds, string message, bool requiresResponse = false)
        {
            var notification = new Notification
            {
                Message = message,
                CreatedDate = DateTimeHelper.GetCurrentPhilippineTime()
            };

            await _db.Notifications.AddAsync(notification);
            await _db.SaveChangesAsync();

            var userNotifications = userIds.Select(userId => new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                RequiresResponse = requiresResponse
            }).ToList();

            await _db.UserNotifications.AddRangeAsync(userNotifications);
            await _db.SaveChangesAsync();
        }

        public async Task ArchiveAsync(Guid userNotificationId)
        {
            var userNotification = await _db.UserNotifications.FindAsync(userNotificationId);

            if (userNotification != null)
            {
                userNotification.IsArchived = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsArchived);
        }

        public async Task<List<UserNotification>> GetUserNotificationsAsync(string userId)
        {
            return await _db.UserNotifications
                .Include(un => un.Notification)
                .Where(un => un.UserId == userId && !un.IsArchived)
                .OrderByDescending(un => un.Notification.CreatedDate)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid userNotificationId)
        {
            var userNotification = await _db.UserNotifications.FindAsync(userNotificationId);
            if (userNotification != null)
            {
                userNotification.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}
