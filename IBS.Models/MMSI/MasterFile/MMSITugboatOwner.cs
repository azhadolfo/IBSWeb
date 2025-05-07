using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITugboatOwner
    {
        [Key]
        public int TugboatOwnerId { get; set; }

        [Display(Name = "Tugboat Owner Number")]
        [Required(ErrorMessage = "Tugboat Owner number is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Tugboat Owner number must be exactly 3 characters.")]
        [Column(TypeName = "varchar(3)")]
        public string TugboatOwnerNumber { get; set; }

        [Display(Name = "Tugboat Owner Name")]
        [Required(ErrorMessage = "Tugboat Owner name is required.")]
        [StringLength(50, ErrorMessage = "Tugboat Owner name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TugboatOwnerName { get; set; }

        public decimal FixedRate { get; set; }
    }
}
