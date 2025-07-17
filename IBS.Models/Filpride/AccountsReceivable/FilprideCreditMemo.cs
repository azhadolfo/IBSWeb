using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideCreditMemo : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CreditMemoId { get; set; }

        [StringLength(13)]
        [Display(Name = "CM No")]
        public string? CreditMemoNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "SI No")]
        public int? SalesInvoiceId { get; set; }

        [ForeignKey("SalesInvoiceId")]
        public FilprideSalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        [Display(Name = "SV No")]
        public int? ServiceInvoiceId { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public FilprideServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        [StringLength(1000)]
        public string Description
        {
            get => _description;
            set => _description = value.Trim();
        }

        private string _description;

        [Display(Name = "Price Adjustment")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? AdjustedPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? Quantity { get; set; }

        [Display(Name = "Credit Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CreditAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Source { get; set; }

        [StringLength(1000)]
        public string? Remarks
        {
            get => _remarks;
            set => _remarks = value?.Trim();
        }

        private string? _remarks;

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        [StringLength(13)]
        public string? Type { get; set; }
    }
}
