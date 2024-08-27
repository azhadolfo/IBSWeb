using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipStep2ViewModel
    {
        public int CustomerOrderSlipId { get; set; }
        public List<SelectListItem>? Suppliers { get; set; }

        public int SupplierId { get; set; } = 0;

        public int PurchaseOrderId { get; set; } = 0;

        public List<SelectListItem>? PurchaseOrders { get; set; }

        public string DeliveryType { get; set; }

        public decimal Freight { get; set; } = 0;

        public string? SubPoRemarks { get; set; }

        public string PickUpPoint { get; set; }

        public List<SelectListItem>? PickUpPoints { get; set; }

        public string? CurrentUser { get; set; }
    }
}
