using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.Integrated;
using Microsoft.CodeAnalysis;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideCVTradePayment
    {
        [Key]
        public int Id { get; set; }

        public int DocumentId { get; set; }

        [StringLength(5)]
        public string DocumentType { get; set; }

        public int CheckVoucherId { get; set; }

        [ForeignKey(nameof(CheckVoucherId))]
        public FilprideCheckVoucherHeader CV { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
