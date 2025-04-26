namespace IBS.Models.Mobility.ViewModels
{
    public class FMSPoSalesRawViewModel
    {
        public string stncode { get; set; }

        public string shiftrecid { get; set; }

        public string customercode { get; set; }

        public string tripticket { get; set; }

        public string drno { get; set; }

        public string driver { get; set; }

        public string plateno { get; set; }

        public string productcode { get; set; }

        public decimal quantity { get; set; }

        public decimal price { get; set; }

        public decimal contractprice { get; set; }

        public string time { get; set; }

        public DateOnly date { get; set; }

        public DateOnly shiftdate { get; set; }

        public int shiftnumber { get; set; }

        public int pagenumber { get; set; }
    }
}
