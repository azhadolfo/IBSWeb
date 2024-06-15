using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public int? PurchaseOrderId { get; set; } //For editing purposes

        [Required]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "The supplier field is required.")]
        public int SupplierId { get; set; }

        public List<SelectListItem>? Suppliers { get; set; }

        [Required(ErrorMessage = "The product field is required.")]
        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        [Required]
        public string Terms { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The quantity should be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The unit cost should be greater than zero.")]
        public decimal UnitCost { get; set; }

        public string Remarks { get; set; }
    }
}