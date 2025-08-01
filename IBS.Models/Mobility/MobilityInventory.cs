using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityInventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InventoryId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Particulars { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Reference { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? StationCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        // To compute UnitCost
        // For purchase the UnitCost is as is
        // For sales the UnitCostAverage
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal UnitCost { get; set; }

        // To compute TotalCost
        // Quantity * UnitCost
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalCost { get; set; }

        // To compute RunningCost
        // If purchase previous balance + TotalCost
        // If sales previous balance - TotalCost
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal RunningCost { get; set; }

        // To compute InventoryBalance
        // If purchase previous balance + Quantity
        // If sales previous balance - Quantity
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal InventoryBalance { get; set; }

        // To compute UnitCostAverage
        // RunningCost / InventoryBalance
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal UnitCostAverage { get; set; }

        // To compute Inventory
        // It's same with the RunningCost
        // it means InventoryValue == RunningCost
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal InventoryValue { get; set; }

        // To compute COGS
        // UnitCostAverage * Quantity
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CostOfGoodsSold { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? ValidatedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? ValidatedDate { get; set; }

        // This property handle
        // the transaction No which table this comes from.
        [Column(TypeName = "varchar(50)")]
        public string TransactionNo { get; set; }

        #region-- Select List

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [NotMapped]
        public List<SelectListItem>? Stations { get; set; }

        #endregion
    }
}
