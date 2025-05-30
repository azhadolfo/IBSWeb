using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class ReceivingReportViewModel
    {
        public int? ReceivingReportId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "DR No field is required.")]
        public int DeliveryReceiptId { get; set; }

        public List<SelectListItem>? DeliveryReceipts { get; set; }

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [Display(Name = "Supplier SI#")]
        public string? SupplierSiNo { get; set; }

        [Display(Name = "Supplier SI Date")]
        public DateOnly? SupplierSiDate { get; set; }

        [Display(Name = "Supplier DR#")]
        public string? SupplierDrNo { get; set; }

        [Display(Name = "Supplier DR Date")]
        public DateOnly? SupplierDrDate { get; set; }

        [Display(Name = "WC No")]
        public string? WithdrawalCertificate { get; set; }

        [Display(Name = "Other Reference")]
        public string OtherReference { get; set; }

        [Display(Name = "Quantity Delivered")]
        [Range(1, double.MaxValue, ErrorMessage = "The quantity delivered should be greater than zero.")]
        public decimal QuantityDelivered { get; set; }

        [Display(Name = "Quantity Received")]
        [Range(1, double.MaxValue, ErrorMessage = "The quantity received should be greater than zero.")]
        public decimal QuantityReceived { get; set; }

        public decimal Freight { get; set; }

        public decimal TotalFreight { get; set; }

        public string Remarks { get; set; }

        public string? CurrentUser { get; set; }
    }
}
