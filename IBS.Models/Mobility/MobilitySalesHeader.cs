using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilitySalesHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesHeaderId { get; set; }

        [Column(TypeName = "varchar(15)")]
        [Display(Name = "Sales No.")]
        public string SalesNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Cashier { get; set; }

        public int Shift { get; set; }

        [Column(TypeName = "time without time zone")]
        [Display(Name = "Time In")]
        public TimeOnly? TimeIn { get; set; }

        [Column(TypeName = "time without time zone")]
        [Display(Name = "Time Out")]
        public TimeOnly? TimeOut { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal FuelSalesTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal LubesTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal SafeDropTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,4)[]")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal[] POSalesAmount { get; set; }

        [Column(TypeName = "varchar[]")]
        public string?[] Customers { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal POSalesTotalAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal TotalSales { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal GainOrLoss { get; set; }

        public bool IsTransactionNormal { get; set; } = true;

        [Column(TypeName = "varchar(3)")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string Source { get; set; }

        #region --Added properties for editing purposes

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal ActualCashOnHand { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Particular { get; set; }

        public bool IsModified { get; set; }

        #endregion --Added properties for editing purposes

        public ICollection<MobilitySalesDetail> SalesDetails { get; set; }
    }
}