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

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "CM No")]
        public string? CreditMemoNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
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

        public string Description { get; set; }

        [Display(Name = "Price Adjustment")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? AdjustedPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal? Quantity { get; set; }

        [Display(Name = "Credit Amount")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CreditAmount { get; set; }

        [Display(Name = "Vatable Sales")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatableSales { get; set; }

        [Display(Name = "Vat Amount")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatAmount { get; set; }

        [Display(Name = "Total Sales")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal TotalSales { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WithHoldingVatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WithHoldingTaxAmount { get; set; }

        [Required]
        public string Source { get; set; }

        public string? Remarks { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        public int ServicesId { get; set; }
    }
}