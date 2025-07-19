using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class ReceivingReportViewModel
    {
        public int? ReceivingReportId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }

        public List<SelectListItem>? PurchaseOrders { get; set; }

        public DateOnly? ReceivedDate { get; set; }

        public string? OldRRNo { get; set; }

        public string? SupplierSiNo { get; set; }

        public DateOnly? SupplierSiDate { get; set; }

        public string? SupplierDrNo { get; set; }

        public string? WithdrawalCertificate { get; set; }

        [Required]
        public string TruckOrVessels { get; set; }

        [Required]
        public decimal QuantityDelivered { get; set; }

        [Required]
        public decimal QuantityReceived { get; set; }

        public string? AuthorityToLoadNo { get; set; }

        public string Remarks { get; set; }

        public string? PostedBy { get; set; }
    }
}
