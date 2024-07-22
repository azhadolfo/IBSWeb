using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Mobility.ViewModels
{
    public class GeneralLedgerView
    {
        [Key]
        public int GeneralLedgerId { get; set; }

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

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public string JournalReference { get; set; }

        public string NormalBalance { get; set; }
    }
}