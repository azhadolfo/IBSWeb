using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Lube : BaseEntity
    {
        [Column(TypeName = "date")]
        public DateTime InvoiceDate { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int CorpCode { get; set; }

        public int SiteCode { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        //AmountDB = Price * Volume
        [Column(TypeName = "numeric(18,2)")]
        public decimal AmountDB { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        //Volume = Amount / Price
        [Column(TypeName = "numeric(18,2)")]
        public decimal Volume { get; set; }

        [Column(TypeName = "varchar(16)")]
        public string ItemCode { get; set; }

        [Column(TypeName = "varchar(32)")]
        public string Particulars { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string CashierId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string CashierName { get; set; }

        public int Shift { get; set; }

        public long Transaction { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string DatetimeStamp { get; set; }
    }
}