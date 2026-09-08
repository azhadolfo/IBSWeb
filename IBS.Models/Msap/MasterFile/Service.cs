using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Service number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string ServiceNumber { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters.")]
        [Column(TypeName = "varchar(100)")]
        public string ServiceName { get; set; } = null!;

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
