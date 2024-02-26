using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class FuelDelivery : BaseEntity
    {
        public int FuelDeliveryId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DeliveryDate { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeIn { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeOut { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Hauler { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string WcNo { get; set; }

        public int TankNo { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string ProductCode { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ProductDescription { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityBefore { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityAfter { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal ShouldBe { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal GainOrLoss { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string ReceivedBy { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        public int StationPosCode { get; set; }

    }
}
