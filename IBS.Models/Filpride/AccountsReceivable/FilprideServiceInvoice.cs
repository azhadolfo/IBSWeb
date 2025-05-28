using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.AccountsReceivable
{
    public class FilprideServiceInvoice : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServiceInvoiceId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "SV No")]
        public string ServiceInvoiceNo { get; set; } = string.Empty;

        #region Customer properties

        [Display(Name = "Customer")]
        [Required(ErrorMessage = "The Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public string CustomerAddress { get; set; } = string.Empty;

        public string CustomerTin { get; set; } = string.Empty;

        #endregion Customer properties

        [Required(ErrorMessage = "The Service is required.")]
        [Display(Name = "Particulars")]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public FilprideService? Service { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly DueDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM yyyy}")]
        public DateOnly Period { get; set; }

        [Required(ErrorMessage = "The Amount is required.")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string PaymentStatus { get; set; } = "Pending";

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Instructions { get; set; }

        public bool IsPaid { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Services { get; set; }

        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string Type { get; set; } = string.Empty;
    }
}
