using IBS.Models.MasterFile;
using IBS.Models.Mobility.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        #region Supplier

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public MobilitySupplier? Supplier { get; set; }

        #endregion Supplier

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        #region Product

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; } //Quantity * UnitPrice

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Discount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Note/Remarks")]
        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }
    }
}