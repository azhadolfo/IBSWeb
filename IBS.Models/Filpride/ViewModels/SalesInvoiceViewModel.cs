using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class SalesInvoiceViewModel
    {
        [StringLength(13)]
        public string Type { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [StringLength(20)]
        public string CustomerTin { get; set; }

        [StringLength(200)]
        public string CustomerAddress { get; set; }

        [StringLength(15)]
        public string Terms { get; set; }

        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal UnitPtice { get; set; }

        public decimal Discount { get; set; }

        public DateOnly TransactionDate { get; set; }

        public int? CustomerOrderSlipId { get; set; }

        public List<SelectListItem>? COS { get; set; }

        public int PurchaseOrderId { get; set; }

        public int ReceivingReportId { get; set; }

        public int? DeliveryReceiptId { get; set; }

        public List<SelectListItem>? DR { get; set; }

        [StringLength(100)]
        public string OtherRefNo { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        public int SalesInvoiceId { get; set; }

        [StringLength(13)]
        public string? SalesInvoiceNo { get; set; } = string.Empty;

        public string? CustomerType { get; set; }

        public string? BusinessStyle { get; set; } = string.Empty;

        public List<SelectListItem>? PO { get; set; }

        public List<SelectListItem>? RR { get; set; }

        public DateTime MinDate { get; set; }
    }
}
