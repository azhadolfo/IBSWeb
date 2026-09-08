using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Msap.MasterFile;

namespace IBS.Models.Msap
{
    public class CollectionBill
    {
        [Key]
        public int CollectionBillId { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string CollectionNumber { get; set; } = null!;

        [Column(TypeName = "varchar(10)")]
        public string BillingNumber { get; set; } = null!;

        [Column(TypeName = "varchar(10)")]
        public string CustomerNumber { get; set; } = null!;

        public int CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public Collection Collection { get; set; } = null!;

        public int BillingId { get; set; }

        [ForeignKey(nameof(BillingId))]
        public Billing Billing { get; set; } = null!;

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MsapCustomer Customer { get; set; } = null!;
    }
}

