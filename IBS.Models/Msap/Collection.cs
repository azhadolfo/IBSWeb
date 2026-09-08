using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap
{
    public class Collection : BaseEntity
    {
        [Key]
        [Column("RECID")]
        public int MsapCollectionId { get; set; }

        [Column("CRNUM")]
        [Display(Name = "Collection Receipt #")]
        public string MsapCollectionNumber
        {
            get => _collectionNumber;
            set => _collectionNumber = value.Trim();
        }

        private string _collectionNumber = null!;

        [Display(Name = "Reference No")]
        [StringLength(50)]
        public string? ReferenceNo { get; set; }

        [Required]
        [Column("CRDATE")]
        [Display(Name = "Transaction Date")]
        public DateOnly Date { get; set; }

        [StringLength(100)]
        public string? Remarks { get; set; }

        // Cash
        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        // Check
        [Column("CHECKDATE", TypeName = "date")]
        public DateOnly? CheckDate { get; set; }

        [Column("CHECKNO")]
        [StringLength(50)]
        public string? CheckNumber { get; set; }

        [StringLength(50)]
        public string? CheckBank { get; set; }

        [StringLength(50)]
        public string? CheckBranch { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        [Column("BANKACCTCO")]
        public int? BankId { get; set; }

        [ForeignKey(nameof(BankId))]
        public MsapBankAccount? BankAccount { get; set; }

        [StringLength(50)]
        public string? BankAccountName { get; set; }

        [StringLength(30)]
        public string? BankAccountNumber { get; set; }

        [Column("AMOUNT", TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; } // This seems to be Total Amount

        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        [Column("CUSTNO")]
        public int CustomerId { get; set; }

        public bool IsUndocumented { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateOnly? DepositDate { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? ClearedDate { get; set; }

        #region --Objects--

        [ForeignKey(nameof(CustomerId))]
        public MsapCustomer Customer { get; set; } = null!;

        public List<Billing>? PaidBills { get; set; }

        #endregion

        [NotMapped]
        public bool IsMonthClosed { get; set; }
    }
}

