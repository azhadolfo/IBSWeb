using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITerminal
    {
        [Key]
        public int TerminalId { get; set; }

        [Display(Name = "Terminal Number")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Terminal number must be exactly 3 characters in length.")]
        [Column(TypeName = "varchar(3)")]
        public string? TerminalNumber { get; set; }

        [Display(Name = "Terminal")]
        [StringLength(40, ErrorMessage = "Terminal name cannot exceed 40 characters.")]
        [Column(TypeName = "varchar(40)")]
        public string? TerminalName { get; set; }

        public int PortId { get; set; }
        [ForeignKey(nameof(PortId))]
        public MMSIPort? Port { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

    }
}
