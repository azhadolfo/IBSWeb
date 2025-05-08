using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIPort
    {
        [Key]
        public int PortId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Port number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string? PortNumber { get; set; }

        [StringLength(50, ErrorMessage = "Port name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string? PortName { get; set; }

        public bool HasSBMA { get; set; }
    }
}
