using IBS.Models.MasterFile;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "RR No.")]
        public string ReceivingReportNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        #region--Purchase Order properties

        public int PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        #endregion

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        #endregion

        [Column(TypeName = "varchar(50)")]
        public string? SupplierSiNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? SupplierSiDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? SupplierDrNo { get; set; }

        public DateOnly? SupplierDrDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? WithdrawalCertificate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string TruckOrVessels { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string OtherReference { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalFreight { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityDelivered { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal GainOrLoss { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal NetOfTaxAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        public DateTime PaidDate { get; set; }

        public bool IsPrinted { get; set; }
    }
}