using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class BookATLViewModel
    {
        public int? AtlId { get; set; }

        public int SupplierId { get; set; }

        public List<SelectListItem>? CosList { get; set; }

        public DateOnly Date { get; set; }

        [StringLength(100)]
        public string? UPPIAtlNo { get; set; }

        public int LoadPortId { get; set; }

        public string? CurrentUser { get; set; }

        public List<SelectListItem> SupplierList { get; set; } = new();

        public List<SelectListItem> LoadPorts { get; set; } = new();

        public List<CosAppointedDetails> SelectedCosDetails { get; set; } = new();
    }

    public class CosAppointedDetails
    {
        public int CosId { get; set; }
        public int AppointedId { get; set; }
        public decimal Volume { get; set; }
    }
}
