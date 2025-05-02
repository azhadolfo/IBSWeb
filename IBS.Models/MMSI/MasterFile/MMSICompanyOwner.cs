using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSICompanyOwner
    {
        [Key]
        public int MMSICompanyOwnerId { get; set; }

        [Display(Name = "Company/Owner Number")]
        [Required(ErrorMessage = "Company/Owner number is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Company/Owner number must be exactly 3 characters.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Company/Owner number must be numeric.")]
        [Column(TypeName = "varchar(3)")]
        public string CompanyOwnerNumber { get; set; }

        [Display(Name = "Company/Owner Name")]
        [Required(ErrorMessage = "Company/Owner name is required.")]
        [StringLength(50, ErrorMessage = "Company/Owner name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string CompanyOwnerName { get; set; }

        public decimal? FixedRate { get; set; }
    }
}
