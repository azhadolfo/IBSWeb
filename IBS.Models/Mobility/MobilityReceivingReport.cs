using IBS.Models.Filpride.Integrated;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityReceivingReport : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceivingReportId { get; set; }

        [Display(Name = "RR No")]
        [Column(TypeName = "varchar(15)")]
        public string ReceivingReportNo { get; set; } //StationCode-RR00001

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Driver { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PlateNo { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ReceivedQuantity { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        #region DR FILPRIDE

        public int DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? FilprideDeliveryReceipt { get; set; }

        #endregion DR FILPRIDE
    }
}