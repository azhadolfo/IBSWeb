using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class BeginningInventoryViewModel
    {
        [Required]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Quantity must be greater than zero")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Required(ErrorMessage = "Cost must be greater than zero")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "The product field is required")]
        public int ProductId { get; set; }

        public List<SelectListItem>? ProductList { get; set; }

        [Required(ErrorMessage = "The product field is required")]
        public int POId { get; set; }

        public List<SelectListItem>? PO { get; set; }

        public string CurrentUser { get; set; } = string.Empty;
    }
}