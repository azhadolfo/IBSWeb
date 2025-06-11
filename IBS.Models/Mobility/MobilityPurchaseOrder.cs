using IBS.Models.MasterFile;
using IBS.Models.Mobility.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityPurchaseOrder : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderId { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar(15)")]
        public string PurchaseOrderNo { get; set; } //StationCode-PO00001

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        #region Supplier

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public MobilitySupplier? Supplier { get; set; }

        public string SupplierAddress { get; set; } = string.Empty;

        public string SupplierTin { get; set; } = string.Empty;

        #endregion Supplier

        #region Product

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public MobilityProduct? Product { get; set; }

        #endregion Product

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal FinalPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; } //Quantity * UnitPrice

        [Display(Name = "Note/Remarks")]
        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Terms { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        public bool IsReceived { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime ReceivedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? SupplierSalesOrderNo { get; set; }

        public bool IsClosed { get; set; }

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string Type { get; set; }

        public int PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public MobilityPickUpPoint? PickUpPoint { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Discount { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; } = string.Empty;

        [ForeignKey(nameof(StationCode))]
        public MobilityStation? MobilityStation { get; set; }
    }
}
