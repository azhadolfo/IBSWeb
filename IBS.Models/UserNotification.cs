using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class UserNotification
    {
        [Key]
        public Guid UserNotificationId { get; set; }

        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }

        public Guid NotificationId { get; set; }

        [ForeignKey(nameof(NotificationId))]
        public Notification Notification { get; set; }

        public bool IsRead { get; set; }

        public bool IsArchived { get; set; }

        public bool RequiresResponse { get; set; }
    }
}
