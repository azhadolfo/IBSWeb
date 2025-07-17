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

        [StringLength(13)]
        [Display(Name = "SV No")]
        public string ServiceInvoiceNo { get; set; } = string.Empty;

        #region Customer properties

        [Display(Name = "Customer")]
        [Required(ErrorMessage = "The Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(200)]
        public string CustomerAddress { get; set; } = string.Empty;

        [StringLength(20)]
        public string CustomerTin { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CustomerBusinessStyle { get; set; }

        #endregion Customer properties

        [Required(ErrorMessage = "The Service is required.")]
        [Display(Name = "Particulars")]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public FilprideService? Service { get; set; }

        [StringLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18,4)")]
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

        [StringLength(20)]
        public string PaymentStatus { get; set; } = nameof(Utility.Enums.Status.Pending);

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        [StringLength(1000)]
        public string Instructions
        {
            get => _instructions;
            set => _instructions = value.Trim();
        }

        private string _instructions = string.Empty;

        public bool IsPaid { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        [StringLength(13)]
        public string Type { get; set; } = string.Empty;

        [StringLength(20)]
        public string VatType { get; set; } = string.Empty;

        public bool HasEwt { get; set; }

        public bool HasWvat { get; set; }

        public int? DeliveryReceiptId { get; set; }

        [ForeignKey(nameof(DeliveryReceiptId))]
        public FilprideDeliveryReceipt? DeliveryReceipt { get; set; }
    }
}
