using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Mobility.MasterFile;
using IBS.Utility.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityCheckVoucherHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckVoucherHeaderId { get; set; }

        public string? CheckVoucherHeaderNo { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Display(Name = "RR No")]
        [Column(TypeName = "varchar[]")]
        public string[]? RRNo { get; set; }

        [Display(Name = "SI No")]
        [Column(TypeName = "varchar[]")]
        public string[]? SINo { get; set; }

        [NotMapped]
        public List<SelectListItem>? RR { get; set; }

        [Display(Name = "PO No")]
        [Column(TypeName = "varchar[]")]
        public string[]? PONo { get; set; }

        [NotMapped]
        public List<SelectListItem>? PO { get; set; }

        [Display(Name = "Supplier Id")]
        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public MobilitySupplier? Supplier { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        public decimal[]? Amount { get; set; }

        public string? Particulars
        {
            get => _particulars;
            set => _particulars = value?.Trim();
        }

        private string? _particulars;

        [Display(Name = "Bank Account Name")]
        public int? BankId { get; set; }

        [ForeignKey(nameof(BankId))]
        public MobilityBankAccount? BankAccount { get; set; }

        [Display(Name = "Check #")]
        [RegularExpression(@"^(?:\d{10,}|DM\d{10})$", ErrorMessage = "Invalid format. Please enter either a 'DM' followed by a 10-digits or CV number minimum 10 digits.")]
        public string? CheckNo
        {
            get => _checkNo;
            set => _checkNo = value?.Trim();
        }

        private string? _checkNo;

        public string Category { get; set; }

        [Display(Name = "Payee")]
        public string? Payee
        {
            get => _payee;
            set => _payee = value?.Trim();
        }

        private string? _payee;

        [NotMapped]
        public List<SelectListItem>? BankAccounts { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }

        [Display(Name = "Check Date")]
        [Column(TypeName = "date")]
        public DateOnly? CheckDate { get; set; }

        [Display(Name = "Start Date:")]
        [Column(TypeName = "date")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "End Date:")]
        [Column(TypeName = "date")]
        public DateOnly? EndDate { get; set; }

        public int NumberOfMonths { get; set; }

        public int NumberOfMonthsCreated { get; set; }

        public DateTime? LastCreatedDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPerMonth { get; set; }

        public bool IsComplete { get; set; }

        public string? AccruedType { get; set; }

        public string? Reference { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVouchers { get; set; }

        [Column(TypeName = "varchar(10)")]
        public string? CvType { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsPaid { get; set; }

        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(CheckVoucherPaymentStatus.ForPosting);

        public string? Type { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal InvoiceAmount { get; set; }

        public string? SupportingFileSavedFileName { get; set; }

        public string? SupportingFileSavedUrl { get; set; }

        public DateOnly? DcpDate { get; set; }

        public DateOnly? DcrDate { get; set; }

        public bool IsAdvances { get; set; }

        public int? EmployeeId { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public MobilityStationEmployee? Employee { get; set; }

        public string Address { get; set; }

        public string Tin { get; set; }
    }
}
