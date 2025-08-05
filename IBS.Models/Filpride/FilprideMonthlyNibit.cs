using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideMonthlyNibit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [Column(TypeName = "numeric(18, 4)")]
        public decimal BeginningBalance { get; set; }

        [Column(TypeName = "numeric(18, 4)")]
        public decimal NetIncome { get; set; }

        [Column(TypeName = "numeric(18, 4)")]
        public decimal PriorPeriodAdjustment { get; set; }

        [Column(TypeName = "numeric(18, 4)")]
        public decimal EndingBalance { get; set; }

        [StringLength(50)]
        public string Company { get; set; }
    }
}
