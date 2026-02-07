using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace IBS.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }

        public string Department { get; set; }

        public string? StationAccess { get; set; }

        public ICollection<UserNotification> UserNotifications { get; set; }

        public string? Position { get; set; }
        // ⭐ USER MANAGEMENT PROPERTIES ⭐
        [Required]
        [Column(TypeName = "boolean")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column(TypeName = "timestamp")]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        [Column(TypeName = "timestamp")]
        public DateTime? ModifiedDate { get; set; }

        [MaxLength(256)]
        public string? ModifiedBy { get; set; }
    }
}
