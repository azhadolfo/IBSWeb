using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSITerminal
    {
        [Key]
        public int TerminalId { get; set; }

        [StringLength(3, MinimumLength = 3, ErrorMessage = "Terminal number must be 3 characters long.")]
        [Column(TypeName = "varchar(3)")]
        public string? TerminalNumber { get; set; }

        [StringLength(50, ErrorMessage = "Terminal name cannot exceed 50 characters.")]
        [Column(TypeName = "varchar(50)")]
        public string? TerminalName { get; set; }

        public int PortId { get; set; }

        [ForeignKey(nameof(PortId))]
        public MMSIPort? Port { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

    }
}
