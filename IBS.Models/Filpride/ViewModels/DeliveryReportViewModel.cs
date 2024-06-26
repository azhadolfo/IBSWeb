using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class DeliveryReportViewModel
    {
        public int DeliverReportId { get; set; }

        public DateOnly Date { get; set; }

        public string? InvoiceNo { get; set; }

        #region--Customer

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerTin { get; set; }

        #endregion

        #region--COS

        [Required(ErrorMessage = "COS No field is required.")]
        public int CustomerOrderSlipId { get; set; }

        public List<SelectListItem>? CustomerOrderSlips { get; set; }

        public string Product { get; set; }

        public decimal RemainingVolume { get; set; }

        public decimal Price { get; set; }

        #endregion

        public decimal Volume { get; set; }

        public decimal TotalAmount { get; set; }

        #region--Hauler

        [Required(ErrorMessage = "Hauler field is required.")]
        public int HaulerId { get; set; }

        public List<SelectListItem>? Haulers { get; set; }

        #endregion

        public string Driver { get; set; }

        public string TruckOrPlateNo { get; set; }

        public string Remarks { get; set; }
    }
}