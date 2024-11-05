using IBS.Models;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(string userId, string message);

        Task<List<UserNotification>> GetUserNotificationsAsync(string userId);

        Task MarkAsReadAsync(Guid userNotificationId);

        Task<int> GetUnreadNotificationCountAsync(string userId);
    }
}