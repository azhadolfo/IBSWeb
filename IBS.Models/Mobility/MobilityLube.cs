using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityLube : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly INV_DATE { get; set; }

        public int? xYEAR { get; set; }

        public int? xMONTH { get; set; }

        public int? xDAY { get; set; }

        public int? xCORPCODE { get; set; }

        public int xSITECODE { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        //AmountDB = Price * Volume
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal AmountDB { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        //Volume = Amount / Price
        public decimal LubesQty { get; set; }

        [Column(TypeName = "varchar(16)")]
        public string ItemCode { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Particulars { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? xOID { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Cashier { get; set; }

        public int Shift { get; set; }

        public int? xTRANSACTION { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string xStamp { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? plateno { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? pono { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? cust { get; set; }

        public DateOnly BusinessDate { get; set; }

        public bool IsProcessed { get; set; }

        public string xTicketID { get; set; }
    }
}
