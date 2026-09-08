using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Msap.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.MasterFile
{
    public class Principal
    {
        [Key]
        public int PrincipalId { get; set; }

        [StringLength(4, ErrorMessage = "Principal number must be 4 characters long.")]
        [Column(TypeName = "varchar(4)")]
        public string PrincipalNumber { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Principal name cannot exceed 100 characters.")]
        [Column(TypeName = "varchar(100)")]
        public string PrincipalName { get; set; } = null!;

        [StringLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Agent { get; set; }

        [StringLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string Address1 { get; set; } = null!;

        [StringLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string? Address2 { get; set; }

        [StringLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string? Address3 { get; set; }

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
        public MsapCustomer Customer { get; set; } = null!;

        #region -- Select List

        [NotMapped]
        public List<SelectListItem>? CustomerSelectList { get; set; }

        #endregion

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
