namespace IBS.Models.Mobility.ViewModels
{
    public class FMSDepositRawViewModel
    {
        public string stncode { get; set; }

        public string shiftrecid { get; set; }

        public DateOnly date { get; set; }

        public string accountno { get; set; }

        public decimal amount { get; set; }

        public DateOnly shiftdate { get; set; }

        public int shiftnumber { get; set; }

        public int pagenumber { get; set; }
    }
}
