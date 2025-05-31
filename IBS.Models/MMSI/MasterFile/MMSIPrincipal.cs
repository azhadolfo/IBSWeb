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

        [StringLength(4, ErrorMessage = "Principal number must be 4 characters long.")]
        [Column(TypeName = "varchar(4)")]
        public string PrincipalNumber { get; set; }

        [StringLength(100, ErrorMessage = "Principal name cannot exceed 100 characters.")]
        [Column(TypeName = "varchar(100)")]
        public string PrincipalName { get; set; }

        [StringLength(200, ErrorMessage = "Principal address cannot exceed 200 characters.")]
        [Column(TypeName = "varchar(200)")]
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
