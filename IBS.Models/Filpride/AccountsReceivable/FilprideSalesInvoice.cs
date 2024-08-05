using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideSalesInvoice : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesInvoiceId { get; set; }

        [Display(Name = "SI No")]
        [Column(TypeName = "varchar(12)")]
        public string? SalesInvoiceNo { get; set; }

        #region Customer properties

        [Required]
        [Display(Name = "Customer No")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #endregion Customer properties

        #region Product properties

        [Required]
        [Display(Name = "Product No")]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product properties

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Other Ref No")]
        public string OtherRefNo { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnitPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Remarks { get; set; }

        [Display(Name = "Vatable Sales")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatableSales { get; set; }

        [Display(Name = "VAT Amount")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal NetDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatExempt { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal ZeroRated { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WithHoldingVatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WithHoldingTaxAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public bool IsTaxAndVatPaid { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [Display(Name = "PO No.")]
        public int PurchaseOrderId { get; set; }

        [NotMapped]
        public List<SelectListItem>? PO { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }
    }
}