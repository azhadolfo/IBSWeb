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

        public decimal? CosVolume { get; set; }

        public decimal? RemainingVolume { get; set; }

        public decimal? Price { get; set; }

        public string? DeliveryOption { get; set; }

        #endregion

        public decimal Volume { get; set; }

        public decimal TotalAmount { get; set; }

        public string Remarks { get; set; }

        public string? CurrentUser { get; set; }

        public string ManualDrNo { get; set; }

        public decimal Freight { get; set; } = 0;

        public decimal ECC { get; set; } = 0;

        public string ATLNo { get; set; }

        public string Hauler { get; set; }

        public string Driver { get; set; }

        public string PlateNo { get; set; }
    }
}