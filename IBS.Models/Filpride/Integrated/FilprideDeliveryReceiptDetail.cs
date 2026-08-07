using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideDeliveryReceiptDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid DeliveryReceiptDetailId { get; set; }

        public int DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? DeliveryReceipt { get; set; }

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        public int AuthorityToLoadId { get; set; }

        [ForeignKey(nameof(AuthorityToLoadId))]
        public FilprideAuthorityToLoad? AuthorityToLoad { get; set; }

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [StringLength(100)]
        public string ProductName { get; set; } = null!;

        [StringLength(20)]
        public string? AuthorityToLoadNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal TotalAmount { get; set; }
    }
}
