using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Enums;

namespace IBS.Models.Filpride
{
    public class LockedPeriodAdjustment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        public LockedPeriodAdjustmentType AdjustmentType { get; set; }

        public Module EntityType { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string EntityTypeNo { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? CustomerName { get; set; }

        public int? SupplierId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? SupplierName { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal OldValue { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal NewValue { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AdjustmentValue { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AffectedQuantity { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; }
    }
}
