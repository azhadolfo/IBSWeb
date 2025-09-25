using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.ViewModels
{
    public class CollectionReceiptSingleSiViewModel
    {
        public int? CollectionReceiptId { get; set; }

        public int CustomerId { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        public DateOnly TransactionDate { get; set; }

        [StringLength(50)]
        public string ReferenceNo { get; set; }

        [StringLength(100)]
        public string? Remarks { get; set; }

        public int SalesInvoiceId { get; set; }

        public List<SelectListItem>? SalesInvoices { get; set; }

        public decimal CashAmount { get; set; }

        public DateOnly? CheckDate { get; set; }

        [StringLength(50)]
        public string? CheckNo { get; set; }

        public string? CheckBank { get; set; }

        [StringLength(50)]
        public string? CheckBranch { get; set; }

        public decimal CheckAmount { get; set; }

        public DateOnly? ManagersCheckDate { get; set; }

        [StringLength(50)]
        public string? ManagersCheckNo { get; set; }

        public string? ManagersCheckBank { get; set; }

        [StringLength(50)]
        public string? ManagersCheckBranch { get; set; }

        public decimal ManagersCheckAmount { get; set; }

        public int? BankId { get; set; }

        public List<SelectListItem>? BankAccounts { get; set; }

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

        public DateTime MinDate { get; set; }
    }
}
