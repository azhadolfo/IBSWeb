using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIPort
    {
        [Key]
        public int PortId { get; set; }

        [Display(Name = "Port Number")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Port number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string? PortNumber { get; set; }

        [Display(Name = "Port Name")]
        [StringLength(20, ErrorMessage = "Port name cannot exceed 20 characters.")]
        [Column(TypeName = "varchar(20)")]
        public string? PortName { get; set; }
    }
}
