using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility.ViewModels
{
    public class CollectionReceiptViewModel
    {
        public int CollectionReceiptId { get; set; }

        [Display(Name = "CR No")]
        public string? CollectionReceiptNo { get; set; }

        //Service Invoice Property
        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        public string? SVNo { get; set; }

        public List<SelectListItem>? ServiceInvoices { get; set; }

        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        //COA Property

        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        public string ReferenceNo { get; set; }

        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CashAmount { get; set; }

        //Check
        public string? CheckDate { get; set; }

        public string? CheckNo { get; set; }

        public string? CheckBank { get; set; }

        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        public DateOnly? ManagerCheckDate { get; set; }

        public string? ManagerCheckNo { get; set; }

        public string? ManagerCheckBank { get; set; }

        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ManagerCheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        public string? F2306FilePath { get; set; }

        public string? F2306FileName { get; set; }

        public string? F2307FilePath { get; set; }

        public string? F2307FileName { get; set; }

        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
