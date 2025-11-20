using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.Integrated;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class ReceivingReportViewModel
    {
        public int? ReceivingReportId { get; set; }

        public string? ReceivingReportNo { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [Required]
        [Display(Name = "PO No.")]
        public int PurchaseOrderId { get; set; }

        [NotMapped]
        public List<SelectListItem>? PurchaseOrders { get; set; }

        [Display(Name = "PO No")]
        public string? PurchaseOrderNo { get; set; }

        [ForeignKey(nameof(PurchaseOrderNo))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Display(Name = "Supplier Invoice#")]
        public string? SupplierInvoiceNumber { get; set; }

        [Display(Name = "Supplier Invoice Date")]
        public DateOnly? SupplierInvoiceDate { get; set; }

        public string? SupplierDrNo { get; set; }

        public string? WithdrawalCertificate { get; set; }

        [Required]
        [Display(Name = "Truck/Vessels")]
        public string TruckOrVessels { get; set; }

        [Required]
        [Display(Name = "Qty Delivered")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityDelivered { get; set; }

        [Required]
        [Display(Name = "Qty Received")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        [Display(Name = "Gain/Loss")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal GainOrLoss { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Display(Name = "ATL No")]
        public string? AuthorityToLoadNo { get; set; }

        [Required]
        public string Remarks { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime PaidDate { get; set; }

        public decimal CanceledQuantity { get; set; }

        public string StationCode { get; set; } = string.Empty;

        public List<SelectListItem>? Stations { get; set; }

        public bool IsPrinted { get; set; }

        public DateOnly? ReceivedDate { get; set; }

        public List<SelectListItem>? DrList { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }

        public string? PostedBy { get; set; }

        public string? CurrentUser { get; set; }
    }
}
