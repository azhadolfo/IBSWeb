using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class ServiceInvoiceViewModel
    {
        public int ServiceInvoiceId { get; set; }

        [Display(Name = "SV No")]
        public string ServiceInvoiceNo { get; set; } = string.Empty;

        #region Customer properties

        [Display(Name = "Customer")]
        [Required(ErrorMessage = "The Customer is required.")]
        public int CustomerId { get; set; }

        public string CustomerAddress { get; set; } = string.Empty;

        public string CustomerTin { get; set; } = string.Empty;

        #endregion Customer properties

        [Required(ErrorMessage = "The Service is required.")]
        [Display(Name = "Particulars")]
        public int ServiceId { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [Required]
        public DateOnly Period { get; set; }

        [Required(ErrorMessage = "The Amount is required.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal UnearnedAmount { get; set; }

        public string PaymentStatus { get; set; } = "Pending";

        public decimal AmountPaid { get; set; }

        public decimal Balance { get; set; }

        public string? Instructions { get; set; }

        public bool IsPaid { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public List<SelectListItem>? Services { get; set; }

        [Display(Name = "Station Code")]
        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string Type { get; set; } = string.Empty;

        public string? CreatedBy { get; set; }
    }
}
