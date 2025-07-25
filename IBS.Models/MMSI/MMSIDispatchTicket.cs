using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSIDispatchTicket
    {
        [Key]
        public int DispatchTicketId { get; set; }

        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string DispatchNumber
        {
            get => _dispatchNumber;
            set => _dispatchNumber = value.Trim();
        }

        private string _dispatchNumber;

        [Column(TypeName = "varchar(10)")]
        public string? COSNumber
        {
            get => _cosNumber;
            set => _cosNumber = value?.Trim();
        }

        private string? _cosNumber;

        [Display(Name = "Date Left")]
        public DateOnly DateLeft { get; set; }

        [Display(Name = "Date Arrived")]
        public DateOnly DateArrived { get; set; }

        [Display(Name = "Time Left")]
        public TimeOnly TimeLeft { get; set; }

        [Display(Name = "Date Arrived")]
        public TimeOnly TimeArrived { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? Remarks
        {
            get => _remarks;
            set => _remarks = value?.Trim();
        }

        private string? _remarks;

        [Column(TypeName = "varchar(100)")]
        public string? BaseOrStation { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? VoyageNumber
        {
            get => _voyageNumber;
            set => _voyageNumber = value?.Trim();
        }

        private string? _voyageNumber;

        public string DispatchChargeType { get; set; }

        public string BAFChargeType { get; set; }

        public decimal TotalHours { get; set; }

        public string Status { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        #region == Tariff ==

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal DispatchRate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal DispatchBillingAmount { get; set; }

        public decimal DispatchDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal DispatchNetRevenue { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal BAFRate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal BAFBillingAmount { get; set; }

        public decimal BAFDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal BAFNetRevenue { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalBilling { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalNetRevenue { get; set; }

        public decimal ApOtherTugs { get; set; }

        public string TariffBy { get; set; }

        public DateTime TariffDate { get; set; }

        public string TariffEditedBy { get; set; }

        public DateTime TariffEditedDate { get; set; }

        #endregion == Tariff ==

        public string? ImageName { get; set; }

        public string? ImageSavedUrl { get; set; }

        public string? ImageSignedUrl { get; set; }

        public string? VideoName { get; set; }

        public string? VideoSavedUrl { get; set; }

        public string? VideoSignedUrl { get; set; }

        public string? BillingId { get; set; }

        [NotMapped]
        public MMSIBilling? Billing { get; set; }

        #region ---Columns with Table relations---

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public int TugBoatId { get; set; }
        [ForeignKey(nameof(TugBoatId))]
        public MMSITugboat? Tugboat { get; set; } //carries the columns of one record

        public int TugMasterId { get; set; }
        [ForeignKey(nameof(TugMasterId))]
        public MMSITugMaster? TugMaster { get; set; } //carries the columns of one record

        public int VesselId { get; set; }
        [ForeignKey(nameof(VesselId))]
        public MMSIVessel? Vessel { get; set; } //carries the columns of one record

        public int TerminalId { get; set; }
        [ForeignKey(nameof(TerminalId))]
        public MMSITerminal? Terminal { get; set; } //carries the columns of one record

        public int ServiceId { get; set; }
        [ForeignKey(nameof(ServiceId))]
        public MMSIService? Service { get; set; } //carries the columns of one record

        #endregion ---Columns with Table relations---

        #region ---Select Lists---

        [NotMapped]
        public List<SelectListItem>? Tugboats { get; set; }

        [NotMapped]
        public List<SelectListItem>? TugMasters { get; set; }

        [NotMapped]
        public List<SelectListItem>? Ports { get; set; }

        [NotMapped]
        public List<SelectListItem>? Terminals { get; set; }

        [NotMapped]
        public List<SelectListItem>? Vessels { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion ---Select Lists---
    }
}
