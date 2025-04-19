namespace IBS.Models.Mobility.ViewModels
{
    public class FMSCashierShiftRawViewModel
    {
        public string stncode { get; set; }

        public string recid { get; set; }

        public DateOnly date { get; set; }

        public string empno { get; set; }

        public int shiftnumber { get; set; }

        public int pagenumber { get; set; }

        public TimeOnly timein { get; set; }

        public TimeOnly timeout { get; set; }

        public string nextday { get; set; }

        public decimal cashonhand { get; set; }

        public decimal pricebio { get; set; }

        public decimal priceeco { get; set; }

        public decimal priceenv { get; set; }
    }
}
