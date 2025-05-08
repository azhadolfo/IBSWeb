using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIService
    {
        [Key]
        public int ServiceId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Service number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string ServiceNumber { get; set; }

        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters.")]
        [Column(TypeName = "varchar(100)")]
        public string ServiceName { get; set; }
    }
}
