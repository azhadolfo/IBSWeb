using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFuelPurchase : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FuelPurchaseId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string FuelPurchaseNo { get; set; }

        public int PageNumber { get; set; }

        [Column(TypeName = "varchar(5)")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(5)")]
        [Display(Name = "Cashier Code")]
        public string CashierCode { get; set; }

        [Display(Name = "Shift No")]
        public int ShiftNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Shift Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly ShiftDate { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeIn { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeOut { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Hauler { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string WcNo { get; set; }

        public int TankNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "Product Code")]
        public string ProductCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Selling Price")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Quantity Before")]
        public decimal QuantityBefore { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Quantity After")]
        public decimal QuantityAfter { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Should Be")]
        public decimal ShouldBe { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Gain Or Loss")]
        public decimal GainOrLoss { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }
    }
}