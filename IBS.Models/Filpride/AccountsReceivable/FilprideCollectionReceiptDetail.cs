using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideCollectionReceiptDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CollectionReceiptId { get; set; }

        public FilprideCollectionReceipt? FilprideCollectionReceipt { get; set; }

        [StringLength(13)]
        public string CollectionReceiptNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly InvoiceDate { get; set; }

        [StringLength(13)]
        public string InvoiceNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }
    }
}
