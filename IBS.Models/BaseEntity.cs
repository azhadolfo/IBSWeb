using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class BaseEntity
    {
        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string? CancellationRemarks { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? CanceledBy { get; set; }

        public DateTime? CanceledDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? VoidedBy { get; set; }

        public DateTime? VoidedDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? PostedBy { get; set; }

        public DateTime? PostedDate { get; set; }
    }
}