using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
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

        [Required]
        public string Terms { get; set; }

        [Required]
        public decimal Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        [StringLength(1000)]
        public string Remarks { get; set; }

        public string? CurrentUser { get; set; }

        public string? Type { get; set; }

        public DateOnly TriggerDate { get; set; }

        [StringLength(50)]
        public string OldPoNo { get; set; }

        public int PickUpPointId { get; set; }

        public List<SelectListItem>? PickUpPoints { get; set; }

        [StringLength(100)]
        public string? SupplierSalesOrderNo { get; set; }

        public List<SelectListItem>? PaymentTerms { get; set; }
    }
}
