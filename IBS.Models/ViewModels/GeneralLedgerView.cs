namespace IBS.Models.ViewModels
{
    public class GeneralLedgerView : GeneralLedger
    {
        public string StationName { get; set; }

        public string? ProductName { get; set; }

        public string? CustomerName { get; set; }

        public string? SupplierName { get; set; }

        public string NormalBalance { get; set; }
    }
}
