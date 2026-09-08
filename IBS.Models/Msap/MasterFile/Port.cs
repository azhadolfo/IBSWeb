using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class Port
    {
        [Key]
        public int PortId { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Port number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string PortNumber { get; set; } = null!;

        [Required]
        [StringLength(50, ErrorMessage = "Port name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string PortName { get; set; } = null!;

        public bool HasSBMA { get; set; }

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
