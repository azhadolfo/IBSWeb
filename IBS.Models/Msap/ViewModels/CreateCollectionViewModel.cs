using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Msap.ViewModels
{
    public class CreateCollectionViewModel
    {
        public int? MsapCollectionId { get; set; }

        public string? MsapCollectionNumber { get; set; }

        public bool IsUndocumented { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public string? ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        public decimal CashAmount { get; set; }

        public DateOnly? CheckDate { get; set; }

        public string? CheckNumber { get; set; }

        public string? CheckBank { get; set; }

        public string? CheckBranch { get; set; }

        public decimal CheckAmount { get; set; }

        public int? BankId { get; set; }

        public decimal Amount { get; set; } // Total amount

        public decimal EWT { get; set; }

        public decimal WVAT { get; set; }

        public DateOnly? DepositDate { get; set; }

        [NotMapped]
        public List<string>? ToCollectBillings { get; set; }

        [NotMapped]
        public List<BillingPaymentViewModel>? BillingPayments { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Billings { get; set; }

        [NotMapped]
        public List<SelectListItem>? BankAccounts { get; set; }
    }
}
