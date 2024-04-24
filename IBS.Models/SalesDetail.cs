using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column(TypeName = "varchar(15)")]
        public string SalesNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Product { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Particular { get; set; }

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Closing { get; set; }

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Opening { get; set; }

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double Liters { get; set; }

        public decimal Calibration { get; set; }

        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = true)]
        public double LitersSold { get; set; } //Sum of volume

        public int TransactionCount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Sale { get; set; } //Sum of amount

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Value { get; set; } //Sum of Liters * Price

        public bool IsTransactionNormal { get; set; } = true;

        [Column(TypeName = "varchar(15)")]
        public string? ReferenceNo { get; set; }
    }
}