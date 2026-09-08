using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.MasterFile
{
    public class MsapCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Required]
        [Display(Name = "Customer Code")]
        [StringLength(7)]
        public string CustomerCode { get; set; } = null!;

        [Required]
        [Display(Name = "Customer Name")]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        [Required]
        [Display(Name = "Customer Address")]
        [StringLength(200)]
        public string CustomerAddress { get; set; } = null!;

        [StringLength(100)]
        public string? Address1 { get; set; }

        [StringLength(100)]
        public string? Address2 { get; set; }

        [StringLength(100)]
        public string? Address3 { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [StringLength(20)]
        public string CustomerTin { get; set; } = null!;

        [Display(Name = "Business Style")]
        [StringLength(100)]
        public string? BusinessStyle { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [StringLength(10)]
        public string CustomerTerms { get; set; } = null!;

        [Required]
        [Display(Name = "Customer Type")]
        [StringLength(20)]
        public string CustomerType { get; set; } = null!;

        [Required]
        [Display(Name = "Vat Type")]
        [StringLength(10)]
        public string VatType { get; set; } = null!;

        [Required]
        [Display(Name = "Creditable Withholding VAT 2306 ")]
        public bool WithHoldingVat { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding Tax 2307")]
        public bool WithHoldingTax { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"));

        [Display(Name = "Edited By")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? EditedDate { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

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

        [Required]
        [Display(Name = "Zip Code")]
        [StringLength(10)]
        public string ZipCode { get; set; } = null!;

        [Column(TypeName = "numeric(18,4)")]
        public decimal? RetentionRate { get; set; }

        public bool HasMultipleTerms { get; set; }

        [StringLength(13)]
        public string Type { get; set; } = string.Empty;

        [Display(Name = "Requires Price Adjustment")]
        public bool RequiresPriceAdjustment { get; set; }

        [NotMapped]
        public List<SelectListItem>? PaymentTerms { get; set; }

        public int? CommissioneeId { get; set; }

        public decimal CommissionRate { get; set; }

        [NotMapped]
        public List<SelectListItem>? Commissionees { get; set; }

        public MsapSupplier? Commissionee { get; set; }

        [StringLength(10)]
        [Column("msap_recid", TypeName = "varchar(10)")]
        public string? MsapRecId { get; set; }
    }
}
