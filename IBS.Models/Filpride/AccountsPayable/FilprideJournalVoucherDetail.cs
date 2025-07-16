using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideJournalVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JournalVoucherDetailId { get; set; }

        [StringLength(20)]
        public string AccountNo { get; set; } = " ";

        [StringLength(200)]
        public string AccountName { get; set; } = " ";

        [StringLength(13)]
        public string TransactionNo { get; set; } = " ";

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        public int JournalVoucherHeaderId { get; set; }

        public FilprideJournalVoucherHeader? JournalVoucherHeader { get; set; }
    }
}
