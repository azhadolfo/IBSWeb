using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class CollectionReceiptServiceViewModel
    {
        public int? CollectionReceiptId { get; set; }

        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public DateOnly TransactionDate { get; set; }

        public string ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        public int ServiceInvoiceId { get; set; }

        public List<SelectListItem>? ServiceInvoices { get; set; }

        public decimal CashAmount { get; set; }

        public DateOnly? CheckDate { get; set; }

        public string? CheckNo { get; set; }

        public int? BankId { get; set; }

        public List<SelectListItem>? BankAccounts { get; set; }

        public string? CheckBranch { get; set; }

        public decimal CheckAmount { get; set; }

        public decimal EWT { get; set; }

        public decimal WVAT { get; set; }

        public IFormFile? Bir2306 { get; set; }

        public IFormFile? Bir2307 { get; set; }

        public string[]? AccountTitleText { get; set; }

        public string[]? AccountTitle { get; set; }

        public decimal[]? AccountAmount { get; set; }

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public bool HasAlready2306 { get; set; }

        public bool HasAlready2307 { get; set; }
    }
}
