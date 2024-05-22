namespace IBS.Models.ViewModels
{
    public class FuelSalesView
    {
        public int xSITECODE { get; set; }

        public string StationCode { get; set; }

        public string xONAME { get; set; }

        public DateOnly BusinessDate { get; set; }

        public int xPUMP { get; set; }

        public string Particulars { get; set; }

        public string ItemCode { get; set; }

        public decimal Price { get; set; }

        public int Shift { get; set; }

        public decimal Calibration { get; set; }

        public decimal AmountDb { get; set; }

        public decimal Sale { get; set; }

        public decimal LitersSold { get; set; }

        public decimal Liters { get; set; }

        public int TransactionCount { get; set; }

        public decimal Closing { get; set; }

        public decimal Opening { get; set; }

        public TimeOnly TimeIn { get; set; }

        public TimeOnly TimeOut { get; set; }
    }
}
