using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IBS.Models.Filpride.ViewModels
{
    public class CheckVoucherNonTradeInvoicingViewModel
    {
        public List<SelectListItem>? Suppliers { get; set; }

        [Required(ErrorMessage = "Supplier field is required.")]
        public int SupplierId { get; set; }

        public int CVId { get; set; }

        public string SupplierName { get; set; }

        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "Supplier Tin")]
        public string SupplierTinNo { get; set; }

        [Display(Name = "PO No")]
        public string? PoNo { get; set; }

        [Display(Name = "SI No")]
        public string? SiNo { get; set; }

        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        public string Particulars { get; set; }

        public decimal Total { get; set; }

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public string[] AccountNumber { get; set; }

        public string[] AccountTitle { get; set; }

        public decimal[] Debit { get; set; }

        public decimal[] Credit { get; set; }

        #region--For automation of journal voucher entry

        public DateOnly? StartDate { get; set; }

        public int NumberOfYears { get; set; }

        #endregion
    }
}