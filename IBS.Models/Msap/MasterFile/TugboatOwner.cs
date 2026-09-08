using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class TugboatOwner
    {
        [Key]
        public int TugboatOwnerId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Tugboat owner number must be 3 characters long.")]
        [Column(TypeName = "varchar(3)")]
        public string TugboatOwnerNumber { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Tugboat owner name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TugboatOwnerName { get; set; } = null!;

        public decimal FixedRate { get; set; }

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
