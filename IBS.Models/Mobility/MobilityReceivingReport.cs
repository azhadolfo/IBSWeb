using IBS.Models.Filpride.Integrated;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        [Display(Name = "RR No")]
        [Column(TypeName = "varchar(15)")]
        public string ReceivingReportNo { get; set; } //StationCode-RR00001

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ReceivedQuantity { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        #region DR FILPRIDE

        public int DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? FilprideDeliveryReceipt { get; set; }

        #endregion DR FILPRIDE

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [Display(Name = "PO No.")]
        [Required]
        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public MobilityPurchaseOrder? PurchaseOrder { get; set; }

        //implement that in viewmodel
        // [NotMapped]
        // public List<SelectListItem>? PurchaseOrders { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar(15)")]
        public string? PurchaseOrderNo { get; set; }

        [Display(Name = "Supplier Invoice#")]
        [Column(TypeName = "varchar(100)")]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Invoice Date")]
        [Column(TypeName = "date")]
        public DateOnly? SupplierInvoiceDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? SupplierDrNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? WithdrawalCertificate { get; set; }

        [Required]
        [Display(Name = "Truck/Vessels")]
        [Column(TypeName = "varchar(100)")]
        public string TruckOrVessels { get; set; }

        [Required]
        [Display(Name = "Qty Delivered")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal GainOrLoss { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Display(Name = "ATL No")]
        [Column(TypeName = "varchar(100)")]
        public string? AuthorityToLoadNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CanceledQuantity { get; set; }

        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? ReceivedDate { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
