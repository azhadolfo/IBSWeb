using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIService
    {
        [Key]
        public int ServiceId { get; set; }

        [Display(Name = "Service Number")]
        [Required(ErrorMessage = "Service number is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Service number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string ServiceNumber { get; set; }

        [Required(ErrorMessage = "Service name is required.")]
        [Display(Name = "Service Name")]
        [StringLength(50, ErrorMessage = "Service name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string ServiceName { get; set; }
    }
}
