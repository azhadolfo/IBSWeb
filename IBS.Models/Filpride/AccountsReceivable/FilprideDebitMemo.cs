using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideDebitMemo : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DebitMemoId { get; set; }

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

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "DM No")]
        public string? DebitMemoNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "Debit Amount")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal DebitAmount { get; set; }

        public string Description { get; set; }

        [Display(Name = "Vatable Sales")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatableSales { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalSales { get; set; }

        [Display(Name = "Price Adjustment")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? AdjustedPrice { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingVatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingTaxAmount { get; set; }

        public string Source { get; set; }

        [Required]
        public string? Remarks { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnearnedAmount { get; set; }

        public int ServicesId { get; set; }

        public string Company { get; set; } = string.Empty;
    }
}