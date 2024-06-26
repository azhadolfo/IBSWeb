using IBS.Models.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class FilprideDeliveryReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DeliveryReportId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "DR No")]
        public string DeliveryReportNo { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string InvoiceNo { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        #endregion

        #region--COS properties

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey("CustomerOrderSlipId")]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #endregion

        #region--Hauler properties

        public int HaulerId { get; set; }

        [ForeignKey("HaulerId")]
        public Hauler? Hauler { get; set; }

        #endregion

        [Column(TypeName = "varchar(50)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string TruckAndPlateNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string AuthorityToLoadNo { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal NetOfVatAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }
    }
}