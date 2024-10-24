using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.IO;
using IBS.Models.MasterFile;
using IBS.Models.Mobility.MasterFile;

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

        [Display(Name ="Date")]
        public DateOnly Date { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Display(Name = "Price")]
        [Column(TypeName = "numeric(18,2)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal PricePerLiter { get; set; }

        public string Address { get; set; }

        #region Product's Properties

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion
        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Display(Name ="Plate Number")]
        public string PlateNo { get; set; }

        public string Driver { get; set; }

        //public string Customer {  get; set; }

        #region Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public MobilityCustomer? Customer { get; set; }

        #endregion

        public string Status { get; set; } = string.Empty;

        [Column(TypeName = "varchar(1024)")]
        public string? Upload { get; set; }

        [Display(Name ="Date Loaded")]
        public DateOnly? LoadDate { get; set; }

        [Display(Name = "Station")]
        public string StationCode { get; set; }

        public string Terms { get; set; } = string.Empty;

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

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
        public string? TripTicket {  get; set; }

        public bool IsPrinted { get; set; }

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

        #region Customer properties

        public int StationId { get; set; }

        [ForeignKey(nameof(StationId))]
        public MobilityStation? MobilityStation { get; set; }

        #endregion

        #region-- Select List

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion
    }
}
