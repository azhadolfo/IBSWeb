using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipViewModel
    {
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

        public string PoNo { get; set; }

        #region Product properties

        [Required(ErrorMessage = "Product field is required.")]
        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        #endregion Product properties

        public decimal Quantity { get; set; }

        public decimal DeliveredPrice { get; set; }

        public decimal Vat { get; set; }

        public decimal TotalAmount { get; set; }

        public string Remarks { get; set; }
    }
}