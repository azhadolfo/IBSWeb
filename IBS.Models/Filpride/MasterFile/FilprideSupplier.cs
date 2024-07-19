using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.MasterFile
{
    public class FilprideSupplier
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
        [Display(Name = "Tin No")]
        public string SupplierTin { get; set; }

        [Column(TypeName = "varchar(3)")]
        [Display(Name = "Supplier Terms")]
        public string SupplierTerms { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "VAT Type")]
        public string VatType { get; set; }

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "TAX Type")]
        public string TaxType { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? ProofOfRegistrationFilePath { get; set; }

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