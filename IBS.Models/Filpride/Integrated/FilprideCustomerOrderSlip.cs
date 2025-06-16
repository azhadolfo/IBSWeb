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
        [Column(TypeName = "varchar(13)")]
        public string CustomerOrderSlipNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        public string CustomerType { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerTin { get; set; }

        #endregion Preparation of COS

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        public string Remarks { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Customer PO No.")]
        public string CustomerPoNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal DeliveredPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal DeliveredQuantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal BalanceQuantity { get; set; }

        #region Commissionee's Properties
        public bool HasCommission { get; set; }

        public int? CommissioneeId { get; set; }

        [ForeignKey(nameof(CommissioneeId))]
        public FilprideSupplier? Commissionee { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CommissionRate { get; set; }

        #endregion Commissionee's Properties

        [Column(TypeName = "varchar(100)")]
        public string AccountSpecialist { get; set; }

        #region Product's Properties

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion Product's Properties

        public string? Branch { get; set; }

        #endregion

        #region Appointing Supplier
        #region--PO properties

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? DeliveryOption { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal? Freight { get; set; }

        public int? PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public FilpridePickUpPoint? PickUpPoint { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        #endregion
        public string? SubPORemarks { get; set; }

        #endregion

        #region Approval of Operation Manager

        [Column(TypeName = "varchar(100)")]
        public string? FirstApprovedBy { get; set; }

        public DateTime? FirstApprovedDate { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly? ExpirationDate { get; set; }

        public string? OperationManagerReason { get; set; }

        #endregion

        #region Approval of Finance

        [Column(TypeName = "varchar(100)")]
        public string? SecondApprovedBy { get; set; }

        public DateTime? SecondApprovedDate { get; set; }

        public string? Terms { get; set; }

        public string? FinanceInstruction { get; set; }

        #endregion

        #region Appointing Hauler

        public int? HaulerId { get; set; }
        public FilprideSupplier? Hauler { get; set; }
        public string? Driver { get; set; }
        public string? PlateNo { get; set; }

        #endregion

        #region Booking of ATL

        [Column(TypeName = "varchar(20)")]
        public string? AuthorityToLoadNo { get; set; }

        #endregion

        public bool IsDelivered { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? DisapprovedBy { get; set; }

        public DateTime? DisapprovedDate { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Company { get; set; } = string.Empty;

        [Column(TypeName = "varchar(50)")]
        public string Status { get; set; } //Created, Supplier Appointed, Approved by Ops Manager, Approved by Finance, Hauler Appointed, Approved

        [Column(TypeName = "varchar(50)")]
        public string OldCosNo { get; set; }

        public bool HasMultiplePO { get; set; }

        public ICollection<FilprideCOSAppointedSupplier>? AppointedSuppliers { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal OldPrice { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string PriceReference { get; set; } =  string.Empty;

        public ICollection<FilprideDeliveryReceipt>? DeliveryReceipts { get; set; }
    }
}
