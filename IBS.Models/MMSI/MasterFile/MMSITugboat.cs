using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITugboat
    {
        [Key]
        public int TugboatId { get; set; }

        [Display(Name = "Tugboat Number")]
        [Required(ErrorMessage = "Tugboat number is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Tugboat number must be exactly 3 characters.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Tugboat number must be numeric.")]
        [Column(TypeName = "varchar(3)")]
        public string TugboatNumber { get; set; }

        [Display(Name = "Tugboat Name")]
        [Required(ErrorMessage = "Tugboat name is required.")]
        [StringLength(25, ErrorMessage = "Tugboat name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string TugboatName { get; set; }

        public bool IsCompanyOwned { get; set; }

        public int? TugboatOwnerId { get; set; }

        [ForeignKey(nameof(TugboatOwnerId))]
        public MMSITugboatOwner? TugboatOwner { get; set; }

        [NotMapped]
        public List<SelectListItem>? CompanyList { get; set; }
    }
}
