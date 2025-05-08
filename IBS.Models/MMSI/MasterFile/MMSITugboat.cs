using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITugboat
    {
        [Key]
        public int TugboatId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Tugboat number must be 3 characters long.")]
        [Column(TypeName = "varchar(3)")]
        public string TugboatNumber { get; set; }

        [StringLength(50, ErrorMessage = "Tugboat name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TugboatName { get; set; }

        public bool IsCompanyOwned { get; set; }

        public int? TugboatOwnerId { get; set; }

        [ForeignKey(nameof(TugboatOwnerId))]
        public MMSITugboatOwner? TugboatOwner { get; set; }

        [NotMapped]
        public List<SelectListItem>? CompanyList { get; set; }
    }
}
