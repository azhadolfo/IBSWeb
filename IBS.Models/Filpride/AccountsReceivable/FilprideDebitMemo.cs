using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideDebitMemo : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DebitMemoId { get; set; }

        [Display(Name = "SI No")]
        public int? SalesInvoiceId { get; set; }

        [ForeignKey(nameof(SalesInvoiceId))]
        public FilprideSalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        [Display(Name = "SV No")]
        public int? ServiceInvoiceId { get; set; }

        [ForeignKey(nameof(ServiceInvoiceId))]
        public FilprideServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        [StringLength(13)]
        [Display(Name = "DM No")]
        public string? DebitMemoNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "Debit Amount")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal DebitAmount { get; set; }

        [StringLength(1000)]
        public string Description
        {
            get => _description;
            set => _description = value.Trim();
        }

        private string _description = null!;

        [Display(Name = "Price Adjustment")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? AdjustedPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal? Quantity { get; set; }

        [StringLength(20)]
        public string Source { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string? Remarks
        {
            get => _remarks;
            set => _remarks = value?.Trim();
        }

        private string? _remarks;

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        public bool IsPrinted { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Enums.DmCmStatus.ForApprovalOfFM);

        [StringLength(13)]
        public string? Type { get; set; }

        public string? ApprovedBy { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ApprovedDate { get; set; }
    }
}
