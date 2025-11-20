using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideTerms
    {
        [Key]
        [StringLength(10)]
        public string TermsCode { get; set; }

        [Range(0, 365)]
        public int NumberOfDays { get; set; }

        [Range(0, 12)]
        public int NumberOfMonths { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        [StringLength(100)]
        public string EditedBy { get; set; } = string.Empty;

        public DateTime EditedDate { get; set; }
    }
}
