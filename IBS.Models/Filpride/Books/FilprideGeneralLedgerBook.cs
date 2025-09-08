using IBS.Models.Filpride.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MasterFile;
using IBS.Utility;
using IBS.Utility.Helpers;

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
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        public bool IsPosted { get; set; } = true;

        [Column(TypeName = "varchar(50)")]
        public string Company { get; set; } = string.Empty;

        #region Bank Properties

        public int? BankAccountId { get; set; }

        [ForeignKey(nameof(BankAccountId))]
        public FilprideBankAccount? BankAccount { get; set; }

        public string? BankAccountName { get; set; }

        #endregion

        #region Supplier Properties

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        public string? SupplierName { get; set; }

        #endregion

        #region Chart Of Account Properties

        public int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public FilprideChartOfAccount Account { get; set; }

        #endregion

        #region Employee Properties

        public int? EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public FilprideEmployee Employee { get; set; }

        public string? EmployeeName { get; set; }

        #endregion

        #region Customer Properties

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public string? CustomerName { get; set; }

        #endregion

        #region Company Properties

        public int? CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public Company? CompanyModel { get; set; }

        public string? CompanyName { get; set; }

        #endregion

        [Column(TypeName = "varchar(50)")]
        public string ModuleType { get; set; } = string.Empty;
    }
}
