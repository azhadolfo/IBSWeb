using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class TugMaster
    {
        [Key]
        public int TugMasterId { get; set; }

        [StringLength(5, ErrorMessage = "Tugboat master number must be 5 characters long.")]
        [Column(TypeName = "varchar(5)")]
        public string TugMasterNumber { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Tug master name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TugMasterName { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
