using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class BookATLViewModel
    {
        public List<SelectListItem>? SupplierList { get; set; }

        public int SupplierId { get; set; }

        public List<SelectListItem>? CosList { get; set; }

        public int[] CosIds { get; set; }

        public DateOnly Date { get; set; }

        public string? UPPIAtlNo { get; set; }

        public string? Hauler { get; set; }

        public string? Driver { get; set; }

        public string? PlateNo { get; set; }

        public decimal Freight { get; set; }

        public string LoadPort { get; set; }

        public string? CurrentUser { get; set; }
    }
}
