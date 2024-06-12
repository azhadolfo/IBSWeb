using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityPoSalesRaw
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid POSalesRawId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string shiftrecid { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string stncode { get; set; }

        public string cashiercode { get; set; } //remove the "E" when saving in actual database

        public int shiftnumber { get; set; }

        [Column(TypeName = "date")]
        public DateOnly podate { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly? potime { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string customercode { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string driver { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string plateno { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string drnumber { get; set; } //remove the "DR" when saving in actual database

        public string tripticket { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string productcode { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal quantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal contractprice { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string createdby { get; set; } //remove the "E" when saving in actual database

        [Column(TypeName = "timestamp without time zone")]
        public DateTime createddate { get; set; }
    }
}