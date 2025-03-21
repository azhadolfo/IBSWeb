using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class MMSIPrincipal
    {
        [Key]
        public int PrincipalId { get; set; }

        [Display(Name = "Principal Number")]
        [Required(ErrorMessage = "Principal number is required.")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Principal number must be exactly 4 characters.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Principal number must be numeric.")]
        [Column(TypeName = "varchar(4)")]
        public string PrincipalNumber { get; set; }

        [Display(Name = "Principal Name")]
        [Required(ErrorMessage = "Principal name is required.")]
        [StringLength(25, ErrorMessage = "Principal name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string PrincipalName { get; set; }

        [Display(Name = "Agent Name")]
        [Required(ErrorMessage = "Agent name is required.")]
        [StringLength(25, ErrorMessage = "Agent name cannot exceed 25 characters.")]
        [Column(TypeName = "varchar(25)")]
        public string AgentName { get; set; }

        public string? Address { get; set; }

        public string? BusinessType { get; set; }

        public string? Terms { get; set; }

        public string? TIN { get; set; }

        public string? Landline1 { get; set; }

        public string? Landline2 { get; set; }

        public string? Mobile1 { get; set; }

        public string? Mobile2 { get; set; }

        public bool IsActive { get; set; }

        public bool IsVatable { get; set; }
    }
}
