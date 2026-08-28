using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.Books
{
    public class FilprideAuditTrail
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = null!;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime Date { get; set; }

        [Display(Name = "Machine Name")]
        public string MachineName { get; set; } = null!;

        public string Activity { get; set; }  = null!;

        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = null!;

        public FilprideAuditTrail()
        {
        }

        public FilprideAuditTrail(string username, string activity, string documentType)
        {
            Username = username;
            Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));
            MachineName = Environment.MachineName;
            Activity = activity;
            DocumentType = documentType;
        }

    }
}
