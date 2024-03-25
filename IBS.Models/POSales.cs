using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class POSales : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int POSalesId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string POSalesNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ShiftRecId { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string CashierCode { get; set; }

        public int ShiftNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly POSalesDate { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly? POSalesTime { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string CustomerCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string TripTicket { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal ContractPrice { get; set; }
    }
}
