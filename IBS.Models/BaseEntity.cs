using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models
{
    public class BaseEntity
    {
        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? CancellationRemarks { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? CanceledBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? CanceledDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? VoidedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? VoidedDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? PostedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? PostedDate { get; set; }
    }
}
