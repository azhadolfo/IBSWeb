using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIVessel
    {
        [Key]
        public int VesselId { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "Vessel number must be exactly 4 characters.")]
        [Column(TypeName = "varchar(4)")]
        public string VesselNumber { get; set; }

        [StringLength(50, ErrorMessage = "Vessel name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string VesselName { get; set; }

        [StringLength(20, ErrorMessage = "Vessel name cannot exceed 20 characters.")]
        [Column(TypeName = "varchar(20)")]
        public string? VesselType { get; set; }
    }
}
