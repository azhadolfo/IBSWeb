using System.ComponentModel.DataAnnotations;

namespace IBS.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; }

        public string Message { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
