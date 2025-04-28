using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityCheckVoucherDetail
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
        public MobilityCheckVoucherHeader? CheckVoucherHeader { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public MobilitySupplier? Supplier { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsVatable { get; set; }

        public decimal EwtPercent { get; set; }

        public bool IsUserSelected { get; set; }

        public int? BankId { get; set; }

        public int? CompanyId { get; set; }

        public int? CustomerId { get; set; }

        public int? EmployeeId { get; set; }

        public int? StationId { get; set; }
    }
}
