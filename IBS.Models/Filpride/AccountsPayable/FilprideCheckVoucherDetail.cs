using IBS.Models.Filpride.AccountsPayable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideCheckVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckVoucherDetailId { get; set; }

        public string AccountNo { get; set; } = " ";
        public string AccountName { get; set; } = " ";

        public string TransactionNo { get; set; } = " ";

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        public int CheckVoucherHeaderId { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderId))]
        public FilprideCheckVoucherHeader? CheckVoucherHeader { get; set; }
    }
}