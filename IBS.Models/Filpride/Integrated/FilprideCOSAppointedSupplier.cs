using IBS.Models.Filpride.AccountsPayable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideCOSAppointedSupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SequenceId { get; set; }

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnservedQuantity { get; set; }

        public bool IsAssignedToDR { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        public string? AtlNo { get; set; }

        public decimal UnreservedQuantity { get; set; }

    }
}
