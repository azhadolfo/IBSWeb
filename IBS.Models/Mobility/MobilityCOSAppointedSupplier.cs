using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Mobility.MasterFile;
using IBS.Models.Mobility.ViewModels;

namespace IBS.Models.Mobility
{
    public class MobilityCOSAppointedSupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequenceId { get; set; }

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public MobilityMainCOS? CustomerOrderSlip { get; set; }

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public MobilityPurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnservedQuantity { get; set; }

        public bool IsAssignedToDR { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public MobilitySupplier? Supplier { get; set; }

        public string? AtlNo { get; set; }
    }
}
