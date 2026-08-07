using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class DeliveryReceiptViewModel
    {
        public int DeliveryReceiptId { get; set; }

        public DateOnly Date { get; set; }

        #region--Customer

        [Required(ErrorMessage = "Customer field is required.")]
        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public string? CustomerAddress { get; set; }

        public string? CustomerTin { get; set; }

        #endregion

        #region--COS

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

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Volume { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; } = null!;

        public string? CurrentUser { get; set; }

        [StringLength(50)]
        public string ManualDrNo { get; set; } = null!;

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ECC { get; set; }

        public string? ATLNo { get; set; }

        public int ATLId { get; set; }

        public int? HaulerId { get; set; }

        public List<SelectListItem>? Haulers { get; set; }

        [StringLength(200)]
        public string Driver { get; set; } = null!;

        [StringLength(200)]
        public string PlateNo { get; set; } = null!;

        public bool IsECCEdited => ECC > 0;

        #region Purchase Order

        public int PurchaseOrderId { get; set; }

        public List<SelectListItem>? PurchaseOrders { get; set; }

        #endregion

        public List<DeliveryReceiptDetailInput> Details { get; set; } = new();

        public bool HasReceivingReport { get; set; }

        public bool IsTheCreationLockForTheMonth { get; set; }

        public string Type { get; set; } = string.Empty;

        public DateTime MinDate { get; set; }
    }

    public class DeliveryReceiptDetailInput
    {
        public int CustomerOrderSlipId { get; set; }

        public int PurchaseOrderId { get; set; }

        public int AuthorityToLoadId { get; set; }

        public string? AuthorityToLoadNo { get; set; }

        public string? CustomerOrderSlipNo { get; set; }

        public string? ProductName { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }
    }
}
