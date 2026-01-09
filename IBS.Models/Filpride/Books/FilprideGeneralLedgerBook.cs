using IBS.Models.Filpride.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Enums;

namespace IBS.Models.Filpride.Books
{
    public class FilprideGeneralLedgerBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeneralLedgerBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Reference { get; set; }

        [Display(Name = "Account Number")]
        [Column(TypeName = "varchar(50)")]
        public string AccountNo { get; set; }

        [Display(Name = "Account Title")]
        [Column(TypeName = "varchar(200)")]
        public string AccountTitle { get; set; }

        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(100)")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        public bool IsPosted { get; set; } = true;

        [Column(TypeName = "varchar(50)")]
        public string Company { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ModuleType { get; set; } = string.Empty;

        #region Chart Of Account Properties

        public int AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public FilprideChartOfAccount Account { get; set; }

        #endregion

        #region Sub-Account Properties

        public SubAccountType? SubAccountType { get; set; }

        public int? SubAccountId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? SubAccountName { get; set; }

        #endregion
    }
}
