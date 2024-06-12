using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityFuel : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly Start { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly End { get; set; }

        [Column(TypeName = "date")]
        public DateOnly INV_DATE { get; set; }

        public int xCORPCODE { get; set; }

        public int xSITECODE { get; set; }

        public int xTANK { get; set; }

        public int xPUMP { get; set; }

        public int xNOZZLE { get; set; }

        public int xYEAR { get; set; }

        public int xMONTH { get; set; }

        public int xDAY { get; set; }

        public int xTRANSACTION { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        //AmountDB = Price * Volume
        public decimal AmountDB { get; set; }

        public decimal Amount { get; set; }

        public decimal Calibration { get; set; }

        //Volume = Amount / Price
        public decimal Volume { get; set; }

        [Column(TypeName = "varchar(16)")]
        public string ItemCode { get; set; }

        [Column(TypeName = "varchar(32)")]
        public string Particulars { get; set; }

        public decimal Opening { get; set; }

        public decimal Closing { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string nozdown { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly InTime { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly OutTime { get; set; }

        //Liters = Opening - Closing
        public decimal Liters { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string xOID { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string xONAME { get; set; }

        public int Shift { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? plateno { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? pono { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? cust { get; set; }

        public DateOnly BusinessDate { get; set; }

        public int DetailGroup { get; set; }

        public int TransCount { get; set; }

        public bool IsProcessed { get; set; }
    }
}