using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideMultipleCheckVoucherPayment
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderPaymentId))]
        public FilprideCheckVoucherHeader? CheckVoucherHeaderPayment { get; set; } = null;
        public int CheckVoucherHeaderPaymentId{ get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderInvoiceId))]
        public FilprideCheckVoucherHeader? CheckVoucherHeaderInvoice { get; set; } = null;
        public int CheckVoucherHeaderInvoiceId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
