using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.MasterFile
{
    public class Terminal
    {
        [Key]
        public int TerminalId { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Terminal number must be 3 characters long.")]
        [Column(TypeName = "varchar(3)")]
        public string TerminalNumber { get; set; } = null!;

        [Required]
        [StringLength(50, ErrorMessage = "Terminal name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string TerminalName { get; set; } = null!;

        public int PortId { get; set; }

        [ForeignKey(nameof(PortId))]
        public Port Port { get; set; } = null!;

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }
    }
}
