namespace IBS.Models.Mobility.ViewModels
{
    public class GeneralLedgerView : MobilityGeneralLedger
    {
        public string StationName { get; set; }

        public string? ProductName { get; set; }

        public string? CustomerName { get; set; }

        public string? SupplierName { get; set; }

        public string NormalBalance { get; set; }
    }
}