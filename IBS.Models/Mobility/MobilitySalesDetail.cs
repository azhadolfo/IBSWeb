using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilitySalesDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesDetailId { get; set; }

        public int SalesHeaderId { get; set; }

        [ForeignKey(nameof(SalesHeaderId))]
        public MobilitySalesHeader? SalesHeader { get; set; }

        [Column(TypeName = "varchar(15)")]
        public string SalesNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Product { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Particular { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Closing { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Opening { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Liters { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Calibration { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal LitersSold { get; set; } //Sum of volume

        public int TransactionCount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Sale { get; set; } //Sum of amount

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Value { get; set; } //Sum of Liters * Price

        [Column(TypeName = "varchar(15)")]
        public string? ReferenceNo { get; set; }

        [Column(TypeName = "varchar(3)")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal? PreviousPrice { get; set; }
    }
}