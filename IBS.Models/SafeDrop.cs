using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class SafeDrop : BaseEntity
    {
        [Column(TypeName = "date")]
        public DateTime INV_DATE { get; set; }

        [Column(TypeName = "date")]
        public DateTime BDate { get; set; }

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
    }
}