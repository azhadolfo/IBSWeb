using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class Vessel
    {
        [Key]
        public int VesselId { get; set; }

        [StringLength(4, MinimumLength = 4, ErrorMessage = "Vessel number must be exactly 4 characters.")]
        [Column(TypeName = "varchar(4)")]
        public string VesselNumber { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Vessel name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string VesselName { get; set; } = null!;

        [Required]
        [StringLength(20, ErrorMessage = "Vessel name cannot exceed 20 characters.")]
        [Column(TypeName = "varchar(20)")]
        public string VesselType { get; set; } = null!;

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
