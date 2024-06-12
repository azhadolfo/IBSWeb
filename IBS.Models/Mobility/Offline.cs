using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class Offline
    {
        [Key]
        public int OfflineId { get; set; }

        public int SeriesNo { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; } //fuel.StationCode

        [Column(TypeName = "date")]
        public DateOnly StartDate { get; set; } //fuel.BusinessDate - previous

        [Column(TypeName = "date")]
        public DateOnly EndDate { get; set; } //fuel.BusinessDate

        [Column(TypeName = "varchar(20)")]
        public string Product { get; set; } //fuel product

        public int Pump { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Opening { get; set; } //fuel.Opening - previous

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Closing { get; set; } //fuel.Closing

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Liters { get; set; } //Closing - Opening

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Balance { get; set; } //Remaining Balance

        public string ClosingDSRNo { get; set; }

        public string OpeningDSRNo { get; set; }

        public bool IsResolve { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal? NewClosing { get; set; }

        public string? LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedDate { get; set; }

        public Offline(string stationCode, DateOnly startDate, DateOnly endDate, string product, int pump, decimal opening, decimal closing)
        {
            StationCode = stationCode;
            StartDate = startDate;
            EndDate = endDate;
            Product = product;
            Pump = pump;
            Opening = opening;
            Closing = closing;
            Liters = opening - closing;
            Balance = Liters;
            IsResolve = false;
        }

    }
}
