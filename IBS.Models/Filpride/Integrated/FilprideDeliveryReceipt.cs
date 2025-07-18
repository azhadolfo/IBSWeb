using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using IBS.Utility.Enums;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideDeliveryReceipt : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DeliveryReceiptId { get; set; }

        [StringLength(13)]
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

        [StringLength(200)]
        public string CustomerAddress { get; set; }

        [StringLength(20)]
        public string CustomerTin { get; set; }

        #endregion

        #region--COS properties

        public int CustomerOrderSlipId { get; set; }

        [ForeignKey(nameof(CustomerOrderSlipId))]
        public FilprideCustomerOrderSlip? CustomerOrderSlip { get; set; }

        #endregion

        [StringLength(1000)]
        public string Remarks
        {
            get => _remarks;
            set => _remarks = value.Trim();
        }

        private string _remarks;

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal TotalAmount { get; set; }

        public bool IsPrinted { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        [StringLength(50)]
        public string ManualDrNo
        {
            get => _manualDrNo;
            set => _manualDrNo = value.Trim();
        }

        private string _manualDrNo;

        [StringLength(50)]
        public string Status { get; set; } = nameof(DRStatus.PendingDelivery);

        #region Appointing Hauler

        public int? HaulerId { get; set; }

        [ForeignKey(nameof(HaulerId))]
        public FilprideSupplier? Hauler { get; set; }

        [StringLength(200)]
        public string? Driver { get; set; }

        [StringLength(200)]
        public string? PlateNo
        {
            get => _plateNo;
            set => _plateNo = value?.Trim();
        }

        private string? _plateNo;

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Freight { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal ECC { get; set; }

        #endregion

        #region Booking of ATL

        [StringLength(20)]
        public string? AuthorityToLoadNo { get; set; }

        #endregion

        public bool HasAlreadyInvoiced { get; set; }

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal FreightAmount { get; set; }

        public int? CommissioneeId { get; set; }

        [ForeignKey(nameof(CommissioneeId))]
        public FilprideSupplier? Commissionee { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CommissionRate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CommissionAmountPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal FreightAmountPaid { get; set; }

        public bool IsCommissionPaid { get; set; }

        public bool IsFreightPaid { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal CommissionAmount { get; set; }

        public bool HasReceivingReport { get; set; }

        [StringLength(200)]
        public string? HaulerName { get; set; }

        [StringLength(20)]
        public string? HaulerVatType { get; set; }

        [StringLength(20)]
        public string? HaulerTaxType { get; set; }
        public int AuthorityToLoadId { get; set; }

        [ForeignKey(nameof(AuthorityToLoadId))]
        public FilprideAuthorityToLoad? AuthorityToLoad { get; set; }



    }
}
