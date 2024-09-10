using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipViewModel
    {
        #region For Marketing - Step 1
        public int CustomerOrderSlipId { get; set; }

        public DateOnly Date { get; set; }

        public DateTime DeliveryDateAndTime { get; set; }

        #region Customer properties

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [Display(Name = "Address")]
        public string? CustomerAddress { get; set; }

        [Display(Name = "TIN")]
        public string? TinNo { get; set; }

        #endregion Customer properties

        [Required(ErrorMessage = "Customer PO No field is required.")]
        public string CustomerPoNo { get; set; }

        public decimal Quantity { get; set; }

        public decimal DeliveredPrice { get; set; }

        public decimal Vat { get; set; }

        public decimal TotalAmount { get; set; }

        public bool HasCommission { get; set; }

        public int? CommissionerId { get; set; }

        public List<SelectListItem>? Commissioners { get; set; }

        public decimal CommissionerRate { get; set; }

        public string Remarks { get; set; }

        #endregion

        public string? CurrentUser { get; set; }
    }
}