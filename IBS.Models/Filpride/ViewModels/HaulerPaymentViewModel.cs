using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class HaulerPaymentViewModel
    {
        public int CvId { get; set; }
        public List<SelectListItem>? Suppliers { get; set; }

        [Required]
        [StringLength(200)]
        public string Payee { get; set; }

        [Required]
        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Required]
        [Display(Name = "Supplier Tin Number")]
        public string SupplierTinNo { get; set; }

        [Required]
        [Display(Name = "Supplier No")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        public List<SelectListItem>? BankAccounts { get; set; }

        [Required]
        [Display(Name = "Bank Accounts")]
        public int? BankId { get; set; }

        [Required]
        [Display(Name = "Check #")]
        [StringLength(20)]
        [RegularExpression(@"^(?:\d{10,}|DM\d{10})$", ErrorMessage = "Invalid format. Please enter either a 'DM' followed by a 10-digits or CV number minimum 10 digits.")]
        public string CheckNo { get; set; }

        [Required]
        [Display(Name = "Check Date")]
        public DateOnly CheckDate { get; set; }

        [StringLength(1000)]
        public string Particulars { get; set; }

        public List<SelectListItem>? COA { get; set; }

        [Required]
        public string[] AccountNumber { get; set; }

        [Required]
        public string[] AccountTitle { get; set; }

        [Required]
        public decimal[] Debit { get; set; }

        [Required]
        public decimal[] Credit { get; set; }

        //others
        public string? CreatedBy { get; set; }

        public List<DRDetailsViewModel> DRs { get; set; }
    }
}
