using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class ServiceInvoiceViewModel
    {
        public int ServiceInvoiceId { get; set; }

        public string Type { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public List<SelectListItem> Customers { get; set; } = new();

        public int ServiceId { get; set; }

        public List<SelectListItem> Services { get; set; } = new();

        public DateOnly DueDate { get; set; }

        [StringLength(200)]
        public string Instructions { get; set; } = string.Empty;

        public DateOnly Period { get; set; }

        public decimal Total { get; set; }

        public decimal Discount { get; set; }

        public List<SelectListItem> DeliveryReceipts { get; set; } = new();

        public int? DeliveryReceiptId { get; set; }
    }
}
