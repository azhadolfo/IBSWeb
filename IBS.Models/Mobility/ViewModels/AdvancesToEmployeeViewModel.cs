using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class AdvancesToEmployeeViewModel
    {
        public int? CvId { get; set; }

        public string? DocumentType { get; set; }

        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        [Required(ErrorMessage = "The employee is required.")]
        public int EmployeeId { get; set; }

        public List<SelectListItem>? Employees { get; set; }

        [Display(Name = "Payee")]
        public string Payee { get; set; }

        [Display(Name = "Payee's Address")]
        public string PayeeAddress { get; set; }

        [Display(Name = "Payee's Tin")]
        public string PayeeTin { get; set; }

        public decimal Total { get; set; }

        public List<SelectListItem>? Banks { get; set; }

        [Required(ErrorMessage = "The bank account is required.")]
        public int BankId { get; set; }

        [Display(Name = "Check No.")]
        public string CheckNo { get; set; }

        [Display(Name = "Check Date")]
        public DateOnly CheckDate { get; set; }

        public string Particulars { get; set; }

        public IFormFile? SupportingFile { get; set; }

    }
}
