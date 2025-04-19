namespace IBS.Models.Mobility.ViewModels
{
    public class FMSLubeSalesRawViewModel
    {
        public string stncode { get; set; }

        public string shiftrecid { get; set; }

        public string productcode { get; set; }

        public int quantity { get; set; }

        public decimal price { get; set; }

        public decimal actualprice { get; set; }

        public decimal cost { get; set; }

        public DateOnly shiftdate { get; set; }

        public int shiftnumber { get; set; }

        public int pagenumber { get; set; }
    }
}
