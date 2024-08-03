﻿using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideCollectionReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CollectionReceiptId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "CR No")]
        public string? CollectionReceiptNo { get; set; }

        public int? SalesInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [Column(TypeName = "varchar(12)")]
        public string? SINo { get; set; }

        public int[]? MultipleSIId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        public string[]? MultipleSI { get; set; }

        [ForeignKey(nameof(SalesInvoiceId))]
        public FilprideSalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        //Service Invoice Property
        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [Column(TypeName = "varchar(12)")]
        public string? SVNo { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public FilprideServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        //Customer Property
        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        //COA Property

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string ReferenceNo { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CashAmount { get; set; }

        //Check
        public string? CheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckBank { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        [Column(TypeName = "date")]
        public DateOnly? ManagerCheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckBank { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal ManagerCheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2306FilePath { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2307FilePath { get; set; }

        [Column(TypeName = "numeric[]")]
        public decimal[]? SIMultipleAmount { get; set; }
    }
}