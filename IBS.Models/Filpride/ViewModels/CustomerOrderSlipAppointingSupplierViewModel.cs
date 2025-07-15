using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipAppointingSupplierViewModel
    {
        public int CustomerOrderSlipId { get; set; }

        public decimal COSVolume { get; set; } = 0;

        public List<SelectListItem>? Suppliers { get; set; }

        public List<int> SupplierIds { get; set; } = [];

        public List<int> PurchaseOrderIds { get; set; } = [];

        public List<SelectListItem>? PurchaseOrders { get; set; }

        public string DeliveryOption { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [StringLength(1000)]
        public string? SubPoRemarks { get; set; }

        public int PickUpPointId { get; set; }

        public List<SelectListItem>? PickUpPoints { get; set; }

        public string? CurrentUser { get; set; }

        public int ProductId { get; set; }

        public List<PurchaseOrderQuantityInfo> PurchaseOrderQuantities { get; set; } = new();
    }

    public class PurchaseOrderQuantityInfo
    {
        public int PurchaseOrderId { get; set; }

        public int SupplierId { get; set; }

        public decimal Quantity { get; set; }
    }
}
