using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using IBS.Utility.Helpers;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideCustomerOrderSlip
    {
        #region Preparation of COS

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrderSlipId { get; set; }

        [Display(Name = "COS No.")]
        [StringLength(13)]
        public string CustomerOrderSlipNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        [StringLength(20)]
        public string CustomerType { get; set; }

        [StringLength(200)]
        public string CustomerAddress { get; set; }

        [StringLength(20)]
        public string CustomerTin { get; set; }

        #endregion Preparation of COS

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [StringLength(1000)]
        public string Remarks
        {
            get => _remarks;
            set => _remarks = value.Trim();
        }

        private string _remarks;

        [StringLength(100)]
        [Display(Name = "Customer PO No.")]
        public string CustomerPoNo
        {
            get => _customerPoNo;
            set => _customerPoNo = value.Trim();
        }

        private string _customerPoNo;

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal DeliveredPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal DeliveredQuantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal BalanceQuantity { get; set; }

        #region Commissionee's Properties
        public bool HasCommission { get; set; }

        public int? CommissioneeId { get; set; }

        [ForeignKey(nameof(CommissioneeId))]
        public FilprideSupplier? Commissionee { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal CommissionRate { get; set; }

        #endregion Commissionee's Properties

        [StringLength(100)]
        public string AccountSpecialist { get; set; }

        #region Product's Properties

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product's Properties

        [StringLength(100)]
        public string? Branch { get; set; }

        #endregion

        #region Appointing Supplier
        #region--PO properties

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [StringLength(50)]
        public string? DeliveryOption { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal? Freight { get; set; }

        public int? PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public FilpridePickUpPoint? PickUpPoint { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        #endregion

        [StringLength(1000)]
        public string? SubPORemarks { get; set; }

        #endregion

        #region Approval of Operation Manager

        [StringLength(100)]
        public string? OmApprovedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? OmApprovedDate { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly? ExpirationDate { get; set; }

        public string? OMReason
        {
            get => _omReason;
            set => _omReason = value?.Trim();
        }

        private string? _omReason;

        #endregion

        #region Approval of Cnc

        [StringLength(100)]
        public string? CncApprovedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? CncApprovedDate { get; set; }

        #endregion

        #region Approval of Finance

        [StringLength(100)]
        public string? FmApprovedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? FmApprovedDate { get; set; }

        [StringLength(15)]
        public string? Terms { get; set; }

        [StringLength(1000)]
        public string? FinanceInstruction
        {
            get => _financeInstruction;
            set => _financeInstruction = value?.Trim();
        }

        private string? _financeInstruction;

        #endregion

        #region Appointing Hauler

        public int? HaulerId { get; set; }
        public FilprideSupplier? Hauler { get; set; }

        [StringLength(200)]
        public string? Driver { get; set; }

        [StringLength(200)]
        public string? PlateNo { get; set; }

        #endregion

        #region Booking of ATL

        [StringLength(20)]
        public string? AuthorityToLoadNo { get; set; }

        #endregion

        public bool IsDelivered { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [StringLength(100)]
        public string? EditedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? EditedDate { get; set; }

        [StringLength(100)]
        public string? DisapprovedBy { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime? DisapprovedDate { get; set; }

        public bool IsPrinted { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } //Created, Supplier Appointed, Approved by Ops Manager, Approved by Finance, Hauler Appointed, Approved

        [StringLength(50)]
        public string OldCosNo
        {
            get => _oldCosNo;
            set => _oldCosNo = value.Trim();
        }

        private string _oldCosNo;

        public bool HasMultiplePO { get; set; }

        public ICollection<FilprideCOSAppointedSupplier>? AppointedSuppliers { get; set; }

        [Column(TypeName = "varchar[]")]
        public string[]? UploadedFiles { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal OldPrice { get; set; }

        [StringLength(50)]
        public string PriceReference { get; set; } =  string.Empty;

        public ICollection<FilprideDeliveryReceipt>? DeliveryReceipts { get; set; }

        [StringLength(200)]
        public string CustomerName { get; set; }

        [StringLength(50)]
        public string ProductName { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AvailableCreditLimit { get; set; }

        [StringLength(20)]
        public string VatType { get; set; }

        public bool HasEWT { get; set; }

        public bool HasWVAT { get; set; }

        [StringLength(20)]
        public string? Depot { get; set; }

        [StringLength(200)]
        public string? CommissioneeName { get; set; }

        [StringLength(100)]
        public string? BusinessStyle { get; set; }

        [StringLength(20)]
        public string? CommissioneeVatType { get; set; }

        [StringLength(20)]
        public string? CommissioneeTaxType { get; set; }
    }
}
