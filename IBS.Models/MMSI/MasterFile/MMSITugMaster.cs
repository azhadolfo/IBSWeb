using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITugMaster
    {
        [Key]
        public int TugMasterId { get; set; }

        [Display(Name = "Tug Master Number")]
        [Required(ErrorMessage = "Tug Master number is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Tug Master number must be exactly 4 characters.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Tug Master number must be numeric.")]
        [Column(TypeName = "varchar(4)")]
        public string TugMasterNumber { get; set; }

        [Display(Name = "Tug Master")]
        [Required(ErrorMessage = "Tug Master name is required.")]
        [StringLength(25, ErrorMessage = "Tug Master name cannot exceed 30 characters.")]
        [Column(TypeName = "varchar(30)")]
        public string TugMasterName { get; set; }

        public bool? IsActive {  get; set; }
    }
}
