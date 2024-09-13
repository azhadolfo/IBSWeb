using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        [Column(TypeName = "varchar(100)")]
        [Display(Name = "Customer PO No.")]
        public string CustomerPoNo { get; set; }

        [Column(TypeName = "timestamp without time zone")]
        public DateTime DeliveryDateAndTime { get; set; }

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

        #region Commissioner's Properties
        public bool HasCommission { get; set; }

        public int? CommissionerId { get; set; }

        [ForeignKey(nameof(CommissionerId))]
        public FilprideSupplier? Commissioner { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal CommissionRate { get; set; }
        #endregion

        #endregion

        #region Appointing Supplier
        #region--PO properties

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? DeliveryOption { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal? Freight { get; set; }

        public int? PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public FilpridePickUpPoint? PickUpPoint { get; set; }

        #endregion
        #endregion

        #region Approval of Operation Manager

        [Column(TypeName = "varchar(100)")]
        public string? FirstApprovedBy { get; set; }

        public DateTime? FirstApprovedDate { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly? ExpirationDate { get; set; }

        #endregion

        #region Approval of Finance

        [Column(TypeName = "varchar(100)")]
        public string? SecondApprovedBy { get; set; }

        public DateTime? SecondApprovedDate { get; set; }

        public string? Terms { get; set; }

        #endregion

        #region Appointing Hauler

        public int? HaulerId { get; set; }

        public FilprideSupplier? Hauler { get; set; }

        #endregion

        public bool IsDelivered { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? DisapprovedBy { get; set; }

        public DateTime? DisapprovedDate { get; set; }

        public bool IsPrinted { get; set; }

        public string Company { get; set; } = string.Empty;

        public string Status { get; set; } //Created, Supplier Appointed, Approved by Ops Manager, Approved by Finance, Hauler Appointed, Completed
    }
}