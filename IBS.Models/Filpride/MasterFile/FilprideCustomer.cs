using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Enums;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Display(Name = "Customer Code")]
        [StringLength(7)]
        public string? CustomerCode { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Customer Address")]
        [StringLength(200)]
        public string CustomerAddress { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [StringLength(20)]
        public string CustomerTin { get; set; }

        [Display(Name = "Business Style")]
        [StringLength(100)]
        public string? BusinessStyle { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [StringLength(10)]
        public string CustomerTerms { get; set; }

        [Required]
        [Display(Name = "Customer Type")]
        [StringLength(20)]
        public string CustomerType { get; set; }

        [Required]
        [Display(Name = "Vat Type")]
        [StringLength(10)]
        public string VatType { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding VAT 2306 ")]
        public bool WithHoldingVat { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding Tax 2307")]
        public bool WithHoldingTax { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? EditedDate { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public ClusterArea? ClusterCode { get; set; }

        #region For Retail

        [StringLength(3)]
        public string? StationCode { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal CreditLimitAsOfToday { get; set; }

        public bool HasBranch { get; set; }

        public ICollection<FilprideCustomerBranch>? Branches { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        [StringLength(10)]
        public string? ZipCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal? RetentionRate { get; set; }

        public bool HasMultipleTerms { get; set; }

        public bool IsFilpride { get; set; }

        public bool IsMobility { get; set; }

        public bool IsBienes { get; set; }

        public bool IsMMSI { get; set; }

        [StringLength(13)]
        public string Type { get; set; } = string.Empty;

        [Display(Name = "Requires Price Adjustment")]
        public bool RequiresPriceAdjustment { get; set; }
    }
}
