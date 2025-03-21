using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIVessel
    {
        [Key]
        public int VesselId { get; set; }

        [Display(Name = "Vessel Number")]
        [Required(ErrorMessage = "Vessel number is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Vessel number must be exactly 4 characters.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Vessel number must be numeric.")]
        [Column(TypeName = "varchar(4)")]
        public string VesselNumber { get; set; }

        [Display(Name = "Vessel Name")]
        [Required(ErrorMessage = "Vessel name is required.")]
        [StringLength(25, ErrorMessage = "Vessel name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string VesselName { get; set; }

        [Display(Name = "Vessel Type")]
        [Required(ErrorMessage = "Vessel type is required.")]
        [StringLength(25, ErrorMessage = "Vessel name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string? VesselType { get; set; }
    }
}
