using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.Books;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideCollectionReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CollectionReceiptId { get; set; }

        [StringLength(13)]
        [Display(Name = "CR No")]
        public string? CollectionReceiptNo { get; set; }

        public int? SalesInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [StringLength(13)]
        public string? SINo { get; set; }

        public int[]? MultipleSIId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        public string[]? MultipleSI { get; set; }

        [ForeignKey(nameof(SalesInvoiceId))]
        public FilprideSalesInvoice? SalesInvoice { get; set; }

        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [StringLength(13)]
        public string? SVNo { get; set; }

        [ForeignKey(nameof(ServiceInvoiceId))]
        public FilprideServiceInvoice? ServiceInvoice { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        [StringLength(50)]
        public string ReferenceNo
        {
            get => _referenceNo;
            set => _referenceNo = value.Trim();
        }

        private string _referenceNo;

        [StringLength(100)]
        public string? Remarks
        {
            get => _remarks;
            set => _remarks = value?.Trim();
        }

        private string? _remarks;

        //Cash
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        //Check
        [Column(TypeName = "date")]
        public DateOnly? CheckDate { get; set; }

        [StringLength(50)]
        public string? CheckNo
        {
            get => _checkNo;
            set => _checkNo = value?.Trim();
        }

        private string? _checkNo;

        public int? BankId { get; set; }

        [StringLength(50)]
        public string? CheckBranch
        {
            get => _checkBranch;
            set => _checkBranch = value?.Trim();
        }

        private string? _checkBranch;

        [ForeignKey(nameof(BankId))]
        public FilprideBankAccount? BankAccount { get; set; }

        [StringLength(50)]
        public string? BankAccountName { get; set; }

        [StringLength(30)]
        public string? BankAccountNumber { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        [StringLength(200)]
        public string? F2306FilePath { get; set; }

        [StringLength(100)]
        public string? F2306FileName { get; set; }

        [StringLength(200)]
        public string? F2307FilePath { get; set; }

        [StringLength(100)]
        public string? F2307FileName { get; set; }

        [Column(TypeName = "numeric[]")]
        public decimal[]? SIMultipleAmount { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public DateOnly[]? MultipleTransactionDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        [StringLength(13)]
        public string? Type { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? DepositedDate { get; set; }

        [NotMapped]
        public List<FilprideGeneralLedgerBook>? Details { get; set; }

        public ICollection<FilprideCollectionReceiptDetail>? ReceiptDetails { get; set; }
    }
}
