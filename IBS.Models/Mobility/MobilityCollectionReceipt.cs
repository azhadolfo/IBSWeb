using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsReceivable;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.Mobility.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityCollectionReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CollectionReceiptId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "CR No")]
        public string? CollectionReceiptNo { get; set; }

        //Service Invoice Property
        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [Column(TypeName = "varchar(12)")]
        public string? SVNo { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public MobilityServiceInvoice? ServiceInvoice { get; set; }

        //implement this in view model
        // [NotMapped]
        // public List<SelectListItem>? ServiceInvoices { get; set; }

        //Customer Property
        [ForeignKey(nameof(CustomerId))]
        public MobilityCustomer? Customer { get; set; }

        //implement this in view model
        // [NotMapped]
        // public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        //COA Property

        //implement this in view model
        // [NotMapped]
        // public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly TransactionDate { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        [Column(TypeName = "varchar(50)")]
        public string ReferenceNo { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        //Check
        public string? CheckDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? CheckNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? CheckBank { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        [Column(TypeName = "date")]
        public DateOnly? ManagerCheckDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? ManagerCheckNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? ManagerCheckBank { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal ManagerCheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2306FilePath { get; set; }

        public string? F2306FileName { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2307FilePath { get; set; }

        public string? F2307FileName { get; set; }

        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
