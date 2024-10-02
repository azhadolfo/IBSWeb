using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CustomerOrderSlipViewModel
    {
        public int CustomerOrderSlipId { get; set; }

        public DateOnly Date { get; set; }

        #region Customer properties

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [Display(Name = "Address")]
        public string? CustomerAddress { get; set; }

        [Display(Name = "TIN")]
        public string? TinNo { get; set; }

        public string? Terms { get; set; }

        #endregion Customer properties

        [Required(ErrorMessage = "Customer PO No field is required.")]
        public string CustomerPoNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal DeliveredPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Vat { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        public bool HasCommission { get; set; }

        public int? CommissioneeId { get; set; }

        public List<SelectListItem>? Commissionee { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CommissionerRate { get; set; }

        public string Remarks { get; set; }

        public string AccountSpecialist { get; set; }

        public int ProductId { get; set; }

        public List<SelectListItem>? Products { get; set; }

        public string? CurrentUser { get; set; }
    }
}