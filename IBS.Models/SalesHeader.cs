using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace IBS.Models
{
    public class SalesHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesHeaderId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string SalesNo { get; set; }

        public string StationPosCode { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Cashier { get; set; }

        public int Shift { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeIn { get; set; }

        [Column(TypeName = "time without time zone")]
        public TimeOnly TimeOut { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal FuelSalesTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal LubesTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal SafeDropTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalSales { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal GainOrLoss { get; set; }


        #region --Added properties for editing purposes

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal ActualCashOnHand { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Particular { get; set; }

        public bool IsModified { get; set; }

        #endregion
    }
}