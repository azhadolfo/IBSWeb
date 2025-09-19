using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideSupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        [Display(Name = "Supplier Code")]
        [StringLength(7)]
        public string? SupplierCode { get; set; }

        [Display(Name = "Supplier Name")]
        [StringLength(200)]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier Address")]
        [StringLength(200)]
        public string SupplierAddress { get; set; }

        [StringLength(20)]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [Display(Name = "Tin No")]
        public string SupplierTin { get; set; }

        [StringLength(10)]
        [Display(Name = "Supplier Terms")]
        public string SupplierTerms { get; set; }

        [StringLength(10)]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [StringLength(20)]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [StringLength(1024)]
        public string? ProofOfRegistrationFilePath { get; set; }

        [StringLength(200)]
        public string? ProofOfRegistrationFileName { get; set; }

        [StringLength(1024)]
        public string? ProofOfExemptionFilePath { get; set; }

        [StringLength(200)]
        public string? ProofOfExemptionFileName { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [StringLength(50)]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? EditedDate { get; set; }

        [StringLength(20)]
        public string Category { get; set; }

        [StringLength(255)]
        [Display(Name = "Trade Name")]
        public string? TradeName { get; set; }

        [StringLength(20)]
        public string? Branch { get; set; }

        [StringLength(100)]
        [Display(Name = "Default Expense")]
        public string? DefaultExpenseNumber { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [Display(Name = "Withholding Tax Percent")]
        public decimal? WithholdingTaxPercent { get; set; }

        [StringLength(100)]
        [Display(Name = "Withholding Tax Title")]
        public string? WithholdingTaxtitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? WithholdingTaxList { get; set; }

        [Display(Name = "Reason")]
        [StringLength(100)]
        public string? ReasonOfExemption { get; set; }

        [StringLength(20)]
        public string? Validity { get; set; }

        [Display(Name = "Validity Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? ValidityDate { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Zip Code")]
        [StringLength(10)]
        public string? ZipCode { get; set; }

        public bool IsFilpride { get; set; }

        public bool IsMobility { get; set; }

        public bool IsBienes { get; set; }

        [Display(Name = "Requires Price Adjustment")]
        public bool RequiresPriceAdjustment { get; set; }
    }
}
