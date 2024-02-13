using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class SalesDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesDetailId { get; set; }

        public int SalesHeaderId { get; set; }

        [ForeignKey("SalesHeaderId")]
        public SalesHeader? SalesHeader { get; set; }

        [Column(TypeName = "varchar(25)")]
        public string SalesNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Particular { get; set; }

        public double Closing { get; set; }

        public double Opening { get; set; }

        public double Liters { get; set; }

        public int Calibration { get; set; }

        public double LitersSold { get; set; } //Sum of volume

        public int TransactionCount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Sale { get; set; } //Sum of amount

        [Column(TypeName = "numeric(18,2)")]
        public decimal Value { get; set; } //Sum of Liters * Price
    }
}