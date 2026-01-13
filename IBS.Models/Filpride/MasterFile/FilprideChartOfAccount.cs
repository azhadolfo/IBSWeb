using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideChartOfAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }

        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        [StringLength(20)]
        public string? AccountNumber { get; set; }

        [Display(Name = "Account Name")]
        [StringLength(200)]
        public string AccountName { get; set; }

        [StringLength(25)]
        [Display(Name = "Account Type")]
        public string? AccountType { get; set; }

        [StringLength(20)]
        [Display(Name = "Normal Balance")]
        public string? NormalBalance { get; set; }

        public int Level { get; set; }

        // Change Parent to an int? (nullable) for FK reference
        public int? ParentAccountId { get; set; }

        [NotMapped]
        public List<SelectListItem>? Main { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        [Display(Name = "Edited By")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime EditedDate { get; set; }

        public bool HasChildren { get; set; }

        [StringLength(20)]
        public string FinancialStatementType { get; set; }

        // Select List

        #region Select List

        [NotMapped]
        public List<SelectListItem>? Accounts { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(ParentAccountId))]
        public virtual FilprideChartOfAccount? ParentAccount { get; set; }

        public virtual ICollection<FilprideChartOfAccount> Children { get; set; } = new List<FilprideChartOfAccount>();

        public ICollection<FilprideGLPeriodBalance> Balances { get; set; } =  new List<FilprideGLPeriodBalance>();

        public ICollection<FilprideGLSubAccountBalance> SubAccountBalances { get; set; } = new List<FilprideGLSubAccountBalance>();

        #endregion
    }
}
