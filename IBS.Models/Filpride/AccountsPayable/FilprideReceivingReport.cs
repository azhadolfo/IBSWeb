using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        [StringLength(13)]
        public string? ReceivingReportNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [Display(Name = "PO No.")]
        public int POId { get; set; }

        [ForeignKey(nameof(POId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Display(Name = "PO No")]
        [StringLength(13)]
        public string? PONo { get; set; }

        [Display(Name = "Supplier Invoice#")]
        [StringLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Invoice Date")]
        [Column(TypeName = "date")]
        public DateOnly? SupplierInvoiceDate { get; set; }

        [StringLength(50)]
        public string? SupplierDrNo { get; set; }

        [StringLength(50)]
        public string? WithdrawalCertificate { get; set; }

        [Display(Name = "Truck/Vessels")]
        [StringLength(100)]
        public string TruckOrVessels { get; set; }

        [Display(Name = "Qty Delivered")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal GainOrLoss { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Display(Name = "ATL No")]
        [StringLength(100)]
        public string? AuthorityToLoadNo { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CanceledQuantity { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? ReceivedDate { get; set; }

        public int? DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? DeliveryReceipt { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        [StringLength(13)]
        public string? Type { get; set; }

        public bool IsCostUpdated { get; set; }

        [StringLength(50)]
        public string? OldRRNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CostBasedOnSoa { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TaxPercentage { get; set; }
    }
}
