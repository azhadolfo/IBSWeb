using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.MasterFile
{
    public class Tugboat
    {
        [Key]
        public int TugboatId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Tugboat number must be 3 characters long.")]
        [Column(TypeName = "varchar(3)")]
        public string TugboatNumber { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Tugboat name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TugboatName { get; set; } = null!;

        public bool IsCompanyOwned { get; set; }

        public int? TugboatOwnerId { get; set; }

        [ForeignKey(nameof(TugboatOwnerId))]
        public TugboatOwner? TugboatOwner { get; set; }

        public int PortId { get; set; }

        [ForeignKey(nameof(PortId))]
        public Port Port { get; set; } = null!;

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }

        [NotMapped]
        public List<SelectListItem>? CompanyList { get; set; }

        [NotMapped]
        public List<SelectListItem>? PortList { get; set; }
    }
}
