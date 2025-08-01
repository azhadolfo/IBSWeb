using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideSalesInvoice : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesInvoiceId { get; set; }

        [Display(Name = "SI No")]
        [StringLength(13)]
        public string? SalesInvoiceNo { get; set; }

        #region Customer properties

        [Required]
        [Display(Name = "Customer No")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        [StringLength(200)]
        public string CustomerAddress { get; set; } = string.Empty;

        [StringLength(20)]
        public string CustomerTin { get; set; } = string.Empty;

        #endregion Customer properties

        #region Product properties

        [Required]
        [Display(Name = "Product No")]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product properties

        [StringLength(100)]
        [Display(Name = "Other Ref No")]
        public string OtherRefNo { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnitPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(1000)]
        public string Remarks
        {
            get => _remarks;
            set => _remarks = value.Trim();
        }

        private string _remarks;

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }


        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public bool IsTaxAndVatPaid { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Display(Name = "PO No.")]
        public int PurchaseOrderId { get; set; } = 0;

        [NotMapped]
        public List<SelectListItem>? PO { get; set; }

        [NotMapped]
        public List<SelectListItem>? RR { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [StringLength(13)]
        public string Type { get; set; } = string.Empty;

        public int ReceivingReportId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        #region Enhancing

        public int? DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? DeliveryReceipt { get; set; }

        [NotMapped]
        public List<SelectListItem>? DR { get; set; }

        public int? CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        [NotMapped]
        public List<SelectListItem>? COS { get; set; }

        [StringLength(15)]
        public string Terms { get; set; }

        #endregion
    }
}
