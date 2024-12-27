using IBS.Models.MasterFile;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Helpers;

namespace IBS.Models.Mobility.ViewModels
{
    public class MobilityCustomerOrderSlip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrderSlipId { get; set; }

        [Display(Name = "COS No.")]
        [Column(TypeName = "varchar(13)")]
        public string CustomerOrderSlipNo { get; set; } = string.Empty;

        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        public decimal Quantity { get; set; }

        [Display(Name = "Price")]
        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        public decimal PricePerLiter { get; set; }

        [Display(Name = "Address")]
        [Column(TypeName = "varchar(100)")]
        public string? Address { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Display(Name = "Plate Number")]
        [Column(TypeName = "varchar(10)")]
        public string PlateNo { get; set; }

        [Display(Name = "Driver Name")]
        [Column(TypeName = "varchar(50)")]
        public string Driver { get; set; }

        [Display(Name = "Status")]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Date Loaded")]
        public DateTime? LoadDate { get; set; }

        [Display(Name = "Station")]
        [Column(TypeName = "varchar(3)")]
        public string StationCode { get; set; }

        [Display(Name = "Payment Terms")]
        [Column(TypeName = "varchar(10)")]
        public string Terms { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? DisapprovedBy { get; set; }

        public DateTime? DisapprovedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? UploadedBy { get; set; }

        public DateTime? UploadedDate { get; set; }

        [Display(Name = "Trip Ticket")]
        [Column(TypeName = "varchar(20)")]
        public string? TripTicket { get; set; }

        public bool IsPrinted { get; set; }

        [Display(Name = "Remarks on Disapprove")]
        [Column(TypeName = "varchar(200)")]
        public string? DisapprovalRemarks { get; set; }

        #region Product's Properties

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product's Properties

        #region Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MobilityCustomer? Customer { get; set; }

        #endregion Customer properties

        #region Stations properties

        public int StationId { get; set; }

        [ForeignKey(nameof(StationId))]
        public MobilityStation? MobilityStation { get; set; }

        #endregion Stations properties

        #region-- Select List

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion

        #region -- suggestion it can be add to database if needed

        //[Column(TypeName = "varchar(100)")]
        //[Display(Name = "Customer PO No.")]
        //public string CustomerPoNo { get; set; }

        //[Column(TypeName = "numeric(18,4)")]
        //[DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        //public decimal DeliveredPrice { get; set; }

        //[Column(TypeName = "numeric(18,4)")]
        //[DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        //public decimal DeliveredQuantity { get; set; }

        //[Column(TypeName = "numeric(18,4)")]
        //[DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        //public decimal BalanceQuantity { get; set; }

        #endregion

        #region Cloud Storage properties

        [NotMapped]
        public string? SignedUrl { get; set; }

        public string? SavedUrl { get; set; }

        public string? SavedFileName { get; set; }

        [NotMapped]
        public string? CheckPictureSignedUrl { get; set; }

        [NotMapped]
        public IFormFile? CheckPicture { get; set; }

        public string? CheckPictureSavedUrl { get; set; }

        public string? CheckPictureSavedFileName { get; set; }

        #endregion

        public string? CheckNo { get; set; }
    }
}