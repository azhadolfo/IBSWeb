using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.ViewModels
{
    public class ActualInventoryViewModel
    {
        public DateOnly Date { get; set; }

        public string ProductCode { get; set; }

        public IEnumerable<SelectListItem>? Products { get; set; }

        public int InventoryId { get; set; }

        public decimal PerBook { get; set; }

        public decimal ActualVolume { get; set; }

        public decimal Variance { get; set; }
    }
}