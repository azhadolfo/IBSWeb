using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility.ViewModels
{
    public class GeneralLedgerView
    {
        [Key]
        public int GeneralLedgerId { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        public string StationCode { get; set; }

        public string StationName { get; set; }

        public string Particular { get; set; }

        public string AccountNumber { get; set; }

        public string AccountTitle { get; set; }

        public string? ProductCode { get; set; }

        public string? ProductName { get; set; }

        public string? CustomerCode { get; set; }

        public string? CustomerName { get; set; }

        public string? SupplierCode { get; set; }

        public string? SupplierName { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Debit { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Credit { get; set; }

        public string JournalReference { get; set; }

        public string NormalBalance { get; set; }
    }
}