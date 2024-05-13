using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class SafeDrop : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly INV_DATE { get; set; }

        [Column(TypeName = "date")]
        public DateOnly BDate { get; set; }

        public int xYEAR { get; set; }

        public int xMONTH { get; set; }

        public int xDAY { get; set; }

        public int xCORPCODE { get; set; }

        public int xSITECODE { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TTime { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string xSTAMP { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string xOID { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string xONAME { get; set; }

        public int Shift { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        public DateOnly BusinessDate { get; set; }

        public bool IsProcessed { get; set; }
    }
}