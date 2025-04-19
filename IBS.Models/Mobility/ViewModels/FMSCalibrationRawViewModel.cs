namespace IBS.Models.Mobility.ViewModels
{
    public class FMSCalibrationRawViewModel
    {
        public string stncode { get; set; }

        public string shiftrecid { get; set; }

        public int pumpnumber { get; set; }

        public string productcode { get; set; }

        public decimal quantity { get; set; }

        public decimal price { get; set; }

        public DateOnly shiftdate { get; set; }

        public int shiftnumber { get; set; }

        public int pagenumber { get; set; }
    }
}
