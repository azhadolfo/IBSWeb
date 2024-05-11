using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class Offline
    {
        [Key]
        public int OfflineId { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; } //fuel.StationCode

        [Column(TypeName = "date")]
        public DateOnly StartDate { get; set; } //fuel.BusinessDate - previous

        [Column(TypeName = "date")]
        public DateOnly EndDate { get; set; } //fuel.BusinessDate

        [Column(TypeName = "varchar(20)")]
        public string Product { get; set; } //fuel product

        public int Pump { get; set; }

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "double()")]
        public double Opening { get; set; } //fuel.Opening - previous

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Closing { get; set; } //fuel.Closing

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Liters { get; set; } //Closing - Opening

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Balance { get; set; } //Remaining Balance

        public Offline(string stationCode, DateOnly startDate, DateOnly endDate, string product, int pump, double opening, double closing)
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
        }

    }
}
