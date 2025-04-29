using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityMultipleCheckVoucherPayment
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderPaymentId))]
        public MobilityCheckVoucherHeader? CheckVoucherHeaderPayment { get; set; } = null;
        public int CheckVoucherHeaderPaymentId{ get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderInvoiceId))]
        public MobilityCheckVoucherHeader? CheckVoucherHeaderInvoice { get; set; } = null;
        public int CheckVoucherHeaderInvoiceId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
