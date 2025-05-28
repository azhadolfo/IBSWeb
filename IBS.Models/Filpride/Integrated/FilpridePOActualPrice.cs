using IBS.Models.Filpride.AccountsPayable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.Integrated
{
    public class FilpridePOActualPrice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TriggeredVolume { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AppliedVolume { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TriggeredPrice { get; set; }

        public bool IsApproved { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime TriggeredDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();
    }
}
