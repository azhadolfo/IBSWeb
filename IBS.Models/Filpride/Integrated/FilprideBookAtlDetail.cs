using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideBookAtlDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public int AuthorityToLoadId { get; set; }

        [ForeignKey(nameof(AuthorityToLoadId))]
        public FilprideAuthorityToLoad? Header { get; set; }

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity {get; set;}

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnservedQuantity {get; set;}

        public int? AppointedId { get; set; }

        [ForeignKey(nameof(AppointedId))]
        public FilprideCOSAppointedSupplier? AppointedSupplier { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        [StringLength(100)]
        public string SupplierName { get; set; } = null!;

        [StringLength(100)]
        public string? SupplierAtlNo { get; set; }
    }
}
