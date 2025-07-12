using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.Integrated;
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

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerAddress { get; set; } = string.Empty;

        public string CustomerTin { get; set; } = string.Empty;

        public string? CustomerBusinessStyle { get; set; }

        #endregion Customer properties

        [Required(ErrorMessage = "The Service is required.")]
        [Display(Name = "Particulars")]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public FilprideService? Service { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public decimal ServicePercent { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Total { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnearnedAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string PaymentStatus { get; set; } = nameof(Utility.Enums.Status.Pending);

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Instructions
        {
            get => _instructions;
            set => _instructions = value.Trim();
        }

        private string _instructions = string.Empty;

        public bool IsPaid { get; set; }

        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string Type { get; set; } = string.Empty;

        public string VatType { get; set; } = string.Empty;

        public bool HasEwt { get; set; }

        public bool HasWvat { get; set; }

        public int? DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? DeliveryReceipt { get; set; }
    }
}
