using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideCheckVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckVoucherDetailId { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        public string AccountNo { get; set; } = " ";
        public string AccountName { get; set; } = " ";

        public string TransactionNo { get; set; } = " ";

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        public int CheckVoucherHeaderId { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderId))]
        public FilprideCheckVoucherHeader? CheckVoucherHeader { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public FilprideSupplier? Supplier { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

    }
}