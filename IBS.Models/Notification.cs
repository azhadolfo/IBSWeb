using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; }

        public string Message { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }
    }
}
