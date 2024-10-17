using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityOffline
    {
        [Key]
        public int OfflineId { get; set; }

        public int SeriesNo { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; } //fuel.StationCode

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly StartDate { get; set; } //fuel.BusinessDate - previous

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly EndDate { get; set; } //fuel.BusinessDate

        [Column(TypeName = "varchar(20)")]
        public string Product { get; set; } //fuel product

        public int Pump { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal FirstDsrOpening { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal FirstDsrClosing { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal SecondDsrOpening { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal SecondDsrClosing { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Liters { get; set; } //FirstDsrClosing - SecondDsrOpeningBeforer

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Balance { get; set; } //Remaining Balance

        public string FirstDsrNo { get; set; }

        public string SecondDsrNo { get; set; }

        public bool IsResolve { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal? NewClosing { get; set; }

        public string? LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedDate { get; set; }

        public MobilityOffline(string stationCode, DateOnly startDate, DateOnly endDate, string product, int pump, decimal firstDsrOpening, decimal firstDsrClosing, decimal secondDsrOpening, decimal secondDsrClosing)
        {
            StationCode = stationCode;
            StartDate = startDate;
            EndDate = endDate;
            Product = product;
            Pump = pump;
            FirstDsrOpening = firstDsrOpening;
            FirstDsrClosing = firstDsrClosing;
            SecondDsrOpening = secondDsrOpening;
            SecondDsrClosing = secondDsrClosing;
            Liters = secondDsrOpening - firstDsrClosing;
            Balance = Liters;
            IsResolve = false;
        }
    }
}