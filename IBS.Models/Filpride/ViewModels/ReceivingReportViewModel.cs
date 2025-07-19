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

        [StringLength(50)]
        public string? OldRRNo { get; set; }

        [StringLength(100)]
        public string? SupplierSiNo { get; set; }

        public DateOnly? SupplierSiDate { get; set; }

        [StringLength(50)]
        public string? SupplierDrNo { get; set; }

        [StringLength(50)]
        public string? WithdrawalCertificate { get; set; }

        [Required]
        [StringLength(100)]
        public string TruckOrVessels { get; set; }

        [Required]
        public decimal QuantityDelivered { get; set; }

        [Required]
        public decimal QuantityReceived { get; set; }

        [StringLength(100)]
        public string? AuthorityToLoadNo { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        public string? PostedBy { get; set; }
    }
}
