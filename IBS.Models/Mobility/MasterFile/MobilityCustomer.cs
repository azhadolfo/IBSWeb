using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilityCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Display(Name = "Customer Name")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string CustomerName { get; set; }

        [Display(Name = "Code Name")]
        [Column(TypeName = "varchar(10)")]
        [StringLength(10)]
        public string CustomerCodeName { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        [StringLength(3)]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string CustomerAddress { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [DisplayFormat(DataFormatString = "{0:#,##0.00;(#,##0.00)}", ApplyFormatInEditMode = true)]
        public decimal QuantityLimit { get; set; }

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime? EditedDate { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerTerms { get; set; }

        [Display(Name = "Customer TIN")]
        public string CustomerTin { get; set; }

        [Display(Name = "Customer Type")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerType { get; set; }

        #region --Select List--

        [NotMapped]
        public List<SelectListItem>? MobilityStations { get; set; }

        #endregion --Select List--

        public bool IsCheckDetailsRequired { get; set; }

        [Display(Name = "Customer Code")]
        [Column(TypeName = "varchar(7)")]
        public string? CustomerCode { get; set; }

        [Display(Name = "Business Style")]
        [Column(TypeName = "varchar(100)")]
        public string? BusinessStyle { get; set; }

        [Required]
        [Display(Name = "Vat Type")]
        [Column(TypeName = "varchar(10)")]
        public string VatType { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding VAT 2306 ")]
        public bool WithHoldingVat { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding Tax 2307")]
        public bool WithHoldingTax { get; set; }

        public ClusterArea? ClusterCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CreditLimitAsOfToday { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        [Column(TypeName = "varchar(10)")]
        public string? ZipCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal? RetentionRate { get; set; }

        public bool HasMultipleTerms { get; set; }
    }
}
