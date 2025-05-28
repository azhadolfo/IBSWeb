using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.ViewModels
{
    public class CheckVoucherNonTradePaymentViewModel
    {
        public List<SelectListItem>? CheckVouchers { get; set; }

        public int CvId { get; set; }

        [Required(ErrorMessage = "The CV No is required.")]
        public int[]? MultipleCvId { get; set; }

        [Display(Name = "Payee")]
        public string Payee { get; set; }

        [Display(Name = "Payee's Address")]
        public string PayeeAddress { get; set; }

        [Display(Name = "Payee's Tin")]
        public string PayeeTin { get; set; }

        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        public List<SelectListItem>? Banks { get; set; }

        [Required(ErrorMessage = "The bank account is required.")]
        public int BankId { get; set; }

        [Display(Name = "Check No.")]
        public string CheckNo { get; set; }

        [Display(Name = "Check Date")]
        public DateOnly CheckDate { get; set; }

        public string Particulars { get; set; }

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public string[] AccountNumber { get; set; }

        public string[] AccountTitle { get; set; }

        public decimal[] Debit { get; set; }

        public decimal[] Credit { get; set; }

        public string? Type { get; set; }

        public int? MultipleSupplierId { get; set; }

        public List<SelectListItem>? Suppliers { get; set; }

        public int CvPaymentId { get; set; }

        public decimal[]? AmountPaid { get; set; }

        public List<PaymentDetail> PaymentDetails { get; set; } = [];

    }

    public class PaymentDetail
    {
        public int CVId { get; set; }

        public decimal AmountPaid { get; set; }
    }

}
