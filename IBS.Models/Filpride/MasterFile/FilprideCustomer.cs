﻿using System.ComponentModel.DataAnnotations;
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
        [Column(TypeName = "varchar(7)")]
        public string? CustomerCode { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        [Column(TypeName = "varchar(100)")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Customer Address")]
        [Column(TypeName = "varchar(200)")]
        public string CustomerAddress { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [RegularExpression(@"\d{3}-\d{3}-\d{3}-\d{5}", ErrorMessage = "Invalid TIN number format.")]
        [Column(TypeName = "varchar(20)")]
        public string CustomerTin { get; set; }

        [Display(Name = "Business Style")]
        [Column(TypeName = "varchar(100)")]
        public string? BusinessStyle { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerTerms { get; set; }

        [Required]
        [Display(Name = "Customer Type")]
        [Column(TypeName = "varchar(20)")]
        public string CustomerType { get; set; }

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

        public string Company { get; set; } = string.Empty;

        public ClusterArea? ClusterCode { get; set; }

        #region For Retail

        [Column(TypeName = "varchar(3)")]
        public string? StationCode { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CreditLimit { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CreditLimitAsOfToday { get; set; }

        public bool HasBranch { get; set; }

        public ICollection<FilprideCustomerBranch>? Branches { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        [Column(TypeName = "varchar(10)")]
        public string? ZipCode { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal? RetentionRate { get; set; }

        public bool HasMultipleTerms { get; set; }
    }
}
