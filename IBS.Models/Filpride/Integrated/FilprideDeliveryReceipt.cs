using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility.Enums;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideDeliveryReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DeliveryReceiptId { get; set; }

        [Column(TypeName = "varchar(12)")]
        [Display(Name = "DR No")]
        public string DeliveryReceiptNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly? DeliveredDate { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #endregion

        #region--COS properties

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #endregion

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal TotalAmount { get; set; }

        public bool IsPrinted { get; set; }

        public string Company { get; set; } = string.Empty;

        [Column(TypeName = "varchar(50)")]
        public string ManualDrNo { get; set; }

        public string Status { get; set; } = nameof(DRStatus.PendingDelivery);

        #region Appointing Hauler

        public int? HaulerId { get; set; }

        [ForeignKey(nameof(HaulerId))]
        public FilprideSupplier? Hauler { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Driver { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? PlateNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Freight { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal ECC { get; set; }

        #endregion

        #region Booking of ATL

        [Column(TypeName = "varchar(20)")]
        public string? AuthorityToLoadNo { get; set; }

        #endregion

        public bool HasAlreadyInvoiced { get; set; }

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal FreightAmount { get; set; }

        public int CommissioneeId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CommissionRate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CommissionAmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal FreightAmountPaid { get; set; }

        public bool IsCommissionPaid { get; set; }

        public bool IsHaulerPaid { get; set; }

        public decimal CommissionAmount { get; set; }
    }
}
