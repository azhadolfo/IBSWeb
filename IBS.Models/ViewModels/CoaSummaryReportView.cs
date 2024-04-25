namespace IBS.Models.ViewModels
{
    public class CoaSummaryReportView
    {
        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string AccountType { get; set; }

        public string? Parent { get; set; }

        public decimal? Debit { get; set; }

        public decimal? Credit { get; set; }

        public decimal? Balance { get; set; }
    }
}
