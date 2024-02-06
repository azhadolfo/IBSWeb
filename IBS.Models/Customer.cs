using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class Customer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [Display(Name = "Customer Code")]
        public int CustomerCode { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        [Column(TypeName = "varchar(50)")]
        public string CustomerName { get; set; }

        [Required]
        [Display(Name = "Customer Address")]
        [Column(TypeName = "varchar(200)")]
        public string CustomerAddress { get; set; }

        [Required]
        [Display(Name = "TIN No")]
        [Column(TypeName = "varchar(20)")]
        public string CustomerTin { get; set; }

        [Required]
        [Display(Name = "Business Style")]
        [Column(TypeName = "varchar(20)")]
        public string BusinessStyle { get; set; }

        [Required]
        [Display(Name = "Payment Terms")]
        [Column(TypeName = "varchar(3)")]
        public string CustomerTerms { get; set; }

        [Required]
        [Display(Name = "Customer Type")]
        [Column(TypeName = "varchar(10)")]
        public string CustomerType { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding VAT 2306 ")]
        public bool WithHoldingVat { get; set; }

        [Required]
        [Display(Name = "Creditable Withholding Tax 2307")]
        public bool WithHoldingTax { get; set; }

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