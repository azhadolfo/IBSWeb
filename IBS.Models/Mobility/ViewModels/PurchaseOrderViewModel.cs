using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Mobility.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public int PurchaseOrderId { get; set; } //For editing purposes

        [Required]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Supplier field is required.")]
        public int SupplierId { get; set; }

        public string SupplierAddress { get; set; } = string.Empty;

        public string SupplierTin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product field is required.")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The quantity should be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "The unit price should be greater than zero.")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Note/Remarks")]
        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Terms { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? SupplierSalesOrderNo { get; set; }

        #region--Select List Item

        [NotMapped]
        public List<MobilityReceivingReport>? RrList { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        #endregion

        public string Type { get; set; } = string.Empty;

        public int PickUpPointId { get; set; }

        [NotMapped]
        public List<SelectListItem>? PickUpPoints { get; set; }

        // [Display(Name = "Station Code")]
        // [Required(ErrorMessage = "Station field is required.")]
        // public string StationCode { get; set; }

        public List<SelectListItem>? Stations { get; set; }

        public string? CurrentUser { get; set; }
    }
}
