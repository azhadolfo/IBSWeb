using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Mobility.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public int? PurchaseOrderId { get; set; } //For editing purposes

        [Required]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Supplier field is required.")]
        public int SupplierId { get; set; }

        public List<SelectListItem>? Suppliers { get; set; }

        [Required(ErrorMessage = "Product field is required.")]
        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        [Required(ErrorMessage = "Station field is required.")]
        public string StationCode { get; set; }

        public List<SelectListItem>? Stations { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The quantity should be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The unit price should be greater than zero.")]
        public decimal UnitPrice { get; set; }

        public decimal Discount { get; set; }

        public string Remarks { get; set; }

        public string? CurrentUser { get; set; }
    }
}
