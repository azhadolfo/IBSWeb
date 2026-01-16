using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Enums;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideCheckVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckVoucherDetailId { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

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

        public int CheckVoucherHeaderId { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderId))]
        public FilprideCheckVoucherHeader? CheckVoucherHeader { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsVatable { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal EwtPercent { get; set; }

        public bool IsUserSelected { get; set; }

        public bool IsDisplayEntry { get; set; }

        public SubAccountType? SubAccountType { get; set; }

        public int? SubAccountId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? SubAccountName { get; set; }
    }
}
