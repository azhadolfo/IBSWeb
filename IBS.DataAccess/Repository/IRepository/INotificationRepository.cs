using IBS.Models;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(string userId, string message, bool requiresResponse = false);

        Task AddNotificationToMultipleUsersAsync(List<string> userIds, string message, bool requiresResponse = false);

        Task<List<UserNotification>> GetUserNotificationsAsync(string userId);

        Task MarkAsReadAsync(Guid userNotificationId);

        Task<int> GetUnreadNotificationCountAsync(string userId);

        Task ArchiveAsync(Guid userNotificationId);
    }
}
