using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class PurchaseChangePriceViewModel
    {
        public int POId { get; set; }

        public List<SelectListItem>? PO { get; set; }

        public decimal FinalPrice { get; set; }

        public string CurrentUser { get; set; } = string.Empty;
    }
}
