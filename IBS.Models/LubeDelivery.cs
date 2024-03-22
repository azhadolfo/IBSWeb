using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class LubeDelivery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubeDeliveryId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string shiftrecid { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string stncode { get; set; }

        public string cashiercode { get; set; } //remove the "E" when saving in actual database

        public int shiftnumber { get; set; }

        [Column(TypeName = "date")]
        public DateOnly deliverydate { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string suppliercode { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string invoiceno { get; set; } //remove the "SI" when saving in actual database

        [Column(TypeName = "varchar(50)")]
        public string drno { get; set; } //remove the "DR" when saving in actual database

        [Column(TypeName = "varchar(50)")]
        public string pono { get; set; } //remove the "PO" when saving in actual database

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal amount { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string rcvdby { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string createdby { get; set; } //remove the "E" when saving in actual database

        [Column(TypeName = "timestamp without time zone")]
        public DateTime createddate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string dtllink { get; set; }

        public int quantity { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string unit { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string description { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal unitprice { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string productcode { get; set; }

        public int piece { get; set; }
    }
}
