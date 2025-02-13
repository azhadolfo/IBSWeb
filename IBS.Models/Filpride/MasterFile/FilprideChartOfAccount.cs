using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideChartOfAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AccountId { get; set; }

        public bool IsMain { get; set; }

        [Display(Name = "Account Number")]
        [Column(TypeName = "varchar(15)")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Account Name")]
        [Column(TypeName = "varchar(100)")]
        public string AccountName { get; set; }

        [Column(TypeName = "varchar(25)")]
        public string? AccountType { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? NormalBalance { get; set; }

        public int Level { get; set; }

        // Change Parent to an int? (nullable) for FK reference
        public int? ParentAccountId { get; set; }

        // Navigation property for Parent Account
        [ForeignKey("ParentAccountId")]
        public virtual FilprideChartOfAccount? ParentAccount { get; set; }

        // Navigation property for Child Accounts
        public virtual ICollection<FilprideChartOfAccount> Children { get; set; } = new List<FilprideChartOfAccount>();

        [NotMapped]
        public List<SelectListItem>? Main { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }

        public bool HasChildren { get; set; }

        // Select List

        [NotMapped]
        public List<SelectListItem>? Accounts { get; set; }
        public string FinancialStatementType { get; set; }
    }
}
