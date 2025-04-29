using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIActivityService
    {
        [Key]
        public int ActivityServiceId { get; set; }

        [Display(Name = "Activity/Service Number")]
        [Required(ErrorMessage = "Activity/Service number is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Activity/Service number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string ActivityServiceNumber { get; set; }

        [Required(ErrorMessage = "Activity/Service name is required.")]
        [StringLength(25, ErrorMessage = "Activity/Service name cannot exceed 25 characters.")]
        [Display(Name = "Activity/Service")]
        [Column(TypeName = "varchar(25)")]
        public string ActivityServiceName { get; set; }
    }
}
