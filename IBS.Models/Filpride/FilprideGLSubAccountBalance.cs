using IBS.Models.Enums;
using IBS.Models.Filpride.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideGLSubAccountBalance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public FilprideChartOfAccount Account { get; set; } = null!;

        public SubAccountType SubAccountType { get; set; }

        public int SubAccountId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string SubAccountName { get; set; } = null!;

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

        public bool IsClosed { get; set; }

        public bool IsValid { get; set; } = true;
    }
}
