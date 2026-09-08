using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Msap.MasterFile
{
    public class MsapTerms
    {
        [Key]
        [StringLength(10)]
        public string TermsCode { get; set; } = null!;

        public int NumberOfDays { get; set; }

        public int NumberOfMonths { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }

        [StringLength(100)]
        public string EditedBy { get; set; } = string.Empty;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime EditedDate { get; set; }
    }
}
