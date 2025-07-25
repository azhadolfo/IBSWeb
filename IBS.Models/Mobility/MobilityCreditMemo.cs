using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsReceivable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityCreditMemo : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CreditMemoId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "CM No")]
        public string? CreditMemoNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "SV No")]
        public int? ServiceInvoiceId { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public MobilityServiceInvoice? ServiceInvoice { get; set; }

        public string Description
        {
            get => _description;
            set => _description = value.Trim();
        }

        private string _description;

        [Display(Name = "Credit Amount")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CreditAmount { get; set; }

        public string? Remarks
        {
            get => _remarks;
            set => _remarks = value?.Trim();
        }

        private string? _remarks;

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
