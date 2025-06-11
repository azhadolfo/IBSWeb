using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class ActualSoundingViewModel
    {
        public DateOnly Date { get; set; }

        public string ProductCode { get; set; }

        public IEnumerable<SelectListItem>? Products { get; set; }

        public int InventoryId { get; set; }

        public decimal PerSystem { get; set; }

        public decimal ActualVolume { get; set; }

        public decimal Variance { get; set; }
    }
}
