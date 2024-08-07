using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class ReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        public string? ReceivingReportNo { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [Display(Name = "PO No.")]
        [Required]
        public int POId { get; set; }

        [ForeignKey("POId")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [NotMapped]
        public List<SelectListItem>? PurchaseOrders { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar(12)")]
        public string? PONo { get; set; }

        [Display(Name = "Supplier Invoice#")]
        [Column(TypeName = "varchar(100)")]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Invoice Date")]
        public string? SupplierInvoiceDate { get; set; }

        [Required]
        [Display(Name = "Truck/Vessels")]
        [Column(TypeName = "varchar(100)")]
        public string TruckOrVessels { get; set; }

        [Required]
        [Display(Name = "Qty Delivered")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal GainOrLoss { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal NetAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal EwtAmount { get; set; }

        [Display(Name = "Other Reference")]
        [Column(TypeName = "varchar(100)")]
        public string? OtherRef { get; set; }

        [Required]
        public string Remarks { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CanceledQuantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal NetAmountOfEWT { get; set; }

        public string Company { get; set; } = string.Empty;
    }
}