using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class InventoryReportViewModel
    {
        public List<SelectListItem>? Products { get; set; }

        [Required(ErrorMessage = "Product is required")]
        public int ProductId { get; set; }

        [Display(Name = "Date To")]
        [Required]
        public DateOnly DateTo { get; set; }

        [Display(Name = "PO No.")]
        public int? POId { get; set; }

        public string? ProductName { get; set; }

        public List<SelectListItem>? PO { get; set; }
    }
}
