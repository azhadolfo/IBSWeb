using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Fuel : BaseEntity
    {
        [Column(TypeName = "time without time zone")]
        public TimeOnly Start { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly End { get; set; }

        [Column(TypeName = "date")]
        public DateTime InvoiceDate { get; set; }

        public int CorpCode { get; set; }

        public int SiteCode { get; set; }

        public int Tank { get; set; }

        public int Pump { get; set; }

        public int Nozzle { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Transaction { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        //AmountDB = Price * Volume
        public decimal AmountDB { get; set; }

        public decimal Amount { get; set; }

        public decimal Calibration { get; set; }

        //Volume = Amount / Price
        public double Volume { get; set; }

        [Column(TypeName = "varchar(16)")]
        public string ItemCode { get; set; }

        [Column(TypeName = "varchar(32)")]
        public string Particulars { get; set; }

        public double Opening { get; set; }

        public double Closing { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string NozDown { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly InTime { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly OutTime { get; set; }

        //Liters = Opening - Closing
        public double Liters { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string CashierId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string CashierName { get; set; }

        public int Shift { get; set; }

        public int TransCount { get; set; }
    }
}