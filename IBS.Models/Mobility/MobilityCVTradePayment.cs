using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Mobility
{
    public class MobilityCVTradePayment
    {
        [Key]
        public int Id { get; set; }

        public int DocumentId { get; set; }

        public string DocumentType { get; set; }

        public int CheckVoucherId { get; set; }

        [ForeignKey(nameof(CheckVoucherId))]
        public MobilityCheckVoucherHeader CV { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
