using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIPrincipal
    {
        [Key]
        public int PrincipalId { get; set; }

        [Display(Name = "Principal Number")]
        public string PrincipalNumber { get; set; }

        [Display(Name = "Principal Name")]
        [Required(ErrorMessage = "Principal name is required.")]
        [StringLength(25, ErrorMessage = "Principal name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string PrincipalName { get; set; }

        public string Address { get; set; }

        public string? BusinessType { get; set; }

        public string? Terms { get; set; }

        public string? TIN { get; set; }

        public string? Landline1 { get; set; }

        public string? Landline2 { get; set; }

        public string? Mobile1 { get; set; }

        public string? Mobile2 { get; set; }

        public bool IsActive { get; set; }

        public bool IsVatable { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #region -- Select List

        [NotMapped]
        public List<SelectListItem>? CustomerSelectList { get; set; }

        #endregion
    }
}
