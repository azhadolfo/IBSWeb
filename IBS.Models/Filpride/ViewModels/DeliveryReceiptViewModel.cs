using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class DeliveryReceiptViewModel
    {
        public int DeliverReceiptId { get; set; }

        public DateOnly Date { get; set; }

        public DateOnly ETA { get; set; }

        #region--Customer

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public string? CustomerAddress { get; set; }

        public string? CustomerTin { get; set; }

        #endregion

        #region--COS

        [Required(ErrorMessage = "COS No field is required.")]
        public int CustomerOrderSlipId { get; set; }

        public List<SelectListItem>? CustomerOrderSlips { get; set; }

        public string? Product { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal? CosVolume { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal? RemainingVolume { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal? Price { get; set; }

        public string? DeliveryOption { get; set; }

        #endregion

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Volume { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        public string Remarks { get; set; }

        public string? CurrentUser { get; set; }

        public string ManualDrNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; } = 0;

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ECC { get; set; } = 0;

        public string? ATLNo { get; set; }

        public int? HaulerId { get; set; }

        public List<SelectListItem>? Haulers { get; set; }

        public string Driver { get; set; }

        public string PlateNo { get; set; }

        public bool IsECCEdited => ECC > 0; // True if ECC is greater than zero
    }
}
