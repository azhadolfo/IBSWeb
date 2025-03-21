using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.MMSI.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.MMSI
{
    public class MMSIDispatchTicket
    {
        [Key]
        public int DispatchTicketId {  get; set; }

        [Display(Name = "Create Date")]
        public DateOnly CreateDate { get; set; }

        [Display(Name = "Dispatch Number")]
        [Required(ErrorMessage = "Dispatch number is required.")]
        [StringLength(20, ErrorMessage = "Dispatch Number should not exceed 20 characters")]
        [Column(TypeName = "varchar(20)")]
        public string DispatchNumber { get; set; }

        [Display(Name = "COS Number")]
        [StringLength(10, ErrorMessage = "Dispatch Number can only contain 10 characters")]
        [Column(TypeName = "varchar(10)")]
        public string? COSNumber {  get; set; }

        [Display(Name = "Date Left")]
        public DateOnly DateLeft { get; set; }

        [Display(Name = "Date Arrived")]
        public DateOnly DateArrived { get; set; }

        [Display(Name = "Time Left")]
        public TimeOnly TimeLeft { get; set; }

        [Display(Name = "Date Arrived")]
        public TimeOnly TimeArrived { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Remarks { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? BaseOrStation { get; set; }

        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? VoyageNumber { get; set; }

        public string? DispatchChargeType { get; set; }

        public string? BAFChargeType { get; set; }

        public decimal? TotalHours { get; set; }

        public string? Status { get; set; }

        #region == Tariff ==

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? DispatchRate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal? DispatchBillingAmount { get; set; }

        public decimal? DispatchDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal? DispatchNetRevenue { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]

        public decimal? BAFRate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? BAFBillingAmount { get; set; }

        public decimal? BAFDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? BAFNetRevenue { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TotalBilling { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TotalNetRevenue { get; set; }

        #endregion == Tariff ==

        public string? UploadName { get; set; }

        public string? BillingId { get; set; }

        #region ---Columns with Table relations---

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

        public int ActivityServiceId { get; set; }
        [ForeignKey(nameof(ActivityServiceId))]
        public MMSIActivityService? ActivityService { get; set; } //carries the columns of one record

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
        public List<SelectListItem>? ActivitiesServices { get; set; }

        #endregion ---Select Lists---

    }
}
