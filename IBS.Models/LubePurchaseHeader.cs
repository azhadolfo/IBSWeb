using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class LubePurchaseHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubeDeliveryHeaderId { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ShiftRecId { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string EmployeeNo { get; set; }

        public int ShiftNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DeliveryDate { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string SalesInvoice { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string SupplierCode { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string PoNo { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ReceivedBy { get; set; }

    }
}
