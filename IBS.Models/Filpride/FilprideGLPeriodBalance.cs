using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride
{
    public class FilprideGLPeriodBalance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public FilprideChartOfAccount Account { get; set; }

        [Column(TypeName = "date")]
        public DateOnly PeriodStartDate { get; set; }

        [Column(TypeName = "date")]
        public DateOnly PeriodEndDate { get; set; }

        public int FiscalYear { get; set; }

        public int FiscalPeriod { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal BeginningBalance { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal DebitTotal { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CreditTotal { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal EndingBalance { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AdjustmentDebitTotal { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AdjustmentCreditTotal { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AdjustedEndingBalance { get; set; }

        public bool IsClosed { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ClosedAt { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Company { get; set; }
    }
}
