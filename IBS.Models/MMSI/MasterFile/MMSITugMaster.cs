using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITugMaster
    {
        [Key]
        public int TugMasterId { get; set; }

        [StringLength(5, MinimumLength = 5, ErrorMessage = "Tugboat master number must be 5 characters long.")]
        [Column(TypeName = "varchar(5)")]
        public string TugMasterNumber { get; set; }

        [StringLength(100, ErrorMessage = "Tug Master name cannot exceed 100 characters.")]
        [Column(TypeName = "varchar(100)")]
        public string TugMasterName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
