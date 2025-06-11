using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.MasterFile
{
    public class MobilitySupplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        [Display(Name = "Supplier Code")]
        [Column(TypeName = "varchar(7)")]
        public string? SupplierCode { get; set; }

        [Display(Name = "Supplier Name")]
        [Column(TypeName = "varchar(50)")]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier Address")]
        [Column(TypeName = "varchar(200)")]
        public string SupplierAddress { get; set; }

        [Column(TypeName = "varchar(20)")]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [Display(Name = "Tin No")]
        public string SupplierTin { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "Supplier Terms")]
        public string SupplierTerms { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [Column(TypeName = "varchar(1024)")]
        public string? ProofOfRegistrationFilePath { get; set; }

        public string? ProofOfRegistrationFileName { get; set; }

        [Column(TypeName = "varchar(1024)")]
        public string? ProofOfExemptionFilePath { get; set; }

        public string? ProofOfExemptionFileName { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Category { get; set; }

        [Column(TypeName = "varchar(255)")]
        [Display(Name = "Trade Name")]
        public string? TradeName { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? Branch { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Default Expense")]
        public string? DefaultExpenseNumber { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

        [Display(Name = "Withholding Tax Percent")]
        public decimal? WithholdingTaxPercent { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Withholding Tax Title")]
        public string? WithholdingTaxtitle { get; set; }

        [NotMapped]
        public List<SelectListItem>? WithholdingTaxList { get; set; }

        [Display(Name = "Reason")]
        [Column(TypeName = "varchar(100)")]
        public string? ReasonOfExemption { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? Validity { get; set; }

        [Display(Name = "Validity Date")]
        [Column(TypeName = "date")]
        public DateTime? ValidityDate { get; set; }

        [Display(Name = "Station Code")]
        [Column(TypeName = "varchar(3)")]
        [StringLength(3)]
        public string StationCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Zip Code")]
        [Column(TypeName = "varchar(10)")]
        public string? ZipCode { get; set; }
    }
}
