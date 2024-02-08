using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SupplierId { get; set; }

        [Display(Name = "Supplier Code")]
        [Column(TypeName = "varchar(7)")]
        public string SupplierCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string SupplierName { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string SupplierAddress { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Tin No")]
        public string SupplierTin { get; set; }

        [Column(TypeName = "varchar(3)")]
        public string SupplierTerms { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? ProofOfRegistrationFilePath { get; set; }

        [Display(Name = "Reason")]
        [Column(TypeName = "varchar(100)")]
        public string? ReasonOfExemption { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? Validity { get; set; }

        [Display(Name = "Validity Date")]
        [Column(TypeName = "date")]
        public DateTime? ValidityDate { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? ProofOfExemptionFilePath { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Edited By")]
        [Column(TypeName = "varchar(50)")]
        public string? EditedBy { get; set; }

        [Display(Name = "Edited Date")]
        public DateTime EditedDate { get; set; }
    }
}