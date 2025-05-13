namespace IBS.Models.MMSI.ViewModels
{
    public class TariffViewModel
    {
        public int DispatchTicketId { get; set; }

        public int CustomerId { get; set; }

        public decimal DispatchRate { get; set; }

        public decimal DispatchDiscount { get; set; }

        public decimal DispatchBillingAmount { get; set; }

        public decimal DispatchNetRevenue { get; set; }

        public decimal BAFRate { get; set; }

        public decimal BAFDiscount { get; set; }

        public decimal BAFBillingAmount { get; set; }

        public decimal BAFNetRevenue { get; set; }

        public decimal TotalBilling { get; set; }

        public decimal TotalNetRevenue { get; set; }

        public decimal? ApOtherTugs { get; set; }
    }
}
