using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class BookATLViewModel
    {
        public int? AtlId { get; set; }

        public List<int> SupplierIds { get; set; } = new();

        public List<SelectListItem>? CosList { get; set; }

        public DateOnly Date { get; set; }

        public int LoadPortId { get; set; }

        public string? CurrentUser { get; set; }

        public List<SelectListItem> SupplierList { get; set; } = new();

        public List<SelectListItem> LoadPorts { get; set; } = new();

        public List<SupplierAtlReferenceInput> SupplierAtlReferences { get; set; } = new();

        public List<CosAppointedDetails> SelectedCosDetails { get; set; } = new();
    }

    public class SupplierAtlReferenceInput
    {
        public int SupplierId { get; set; }

        public string? SupplierName { get; set; }

        [StringLength(100)]
        public string? SupplierAtlNo { get; set; }
    }

    public class CosAppointedDetails
    {
        public int CosId { get; set; }
        public int AppointedId { get; set; }
        public decimal Volume { get; set; }
    }
}
