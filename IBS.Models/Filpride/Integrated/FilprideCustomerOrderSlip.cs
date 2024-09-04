using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.Integrated
{
    public class FilprideCustomerOrderSlip
    {
        #region First Stage
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrderSlipId { get; set; }

        [Display(Name = "COS No.")]
        [Column(TypeName = "varchar(13)")]
        public string CustomerOrderSlipNo { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly Date { get; set; }

        #region--Customer properties

        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
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
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Vat { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal DeliveredPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal DeliveredQuantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal BalanceQuantity { get; set; }

        #region Commissioner's Properties
        public bool HasCommission { get; set; }

        public string? CommissionerName { get; set; }

        public decimal? CommissionRate { get; set; }
        #endregion

        #endregion

        #region Second Stage
        #region--PO properties

        public int? PurchaseOrderId { get; set; }

        [ForeignKey(nameof(PurchaseOrderId))]
        public FilpridePurchaseOrder? PurchaseOrder { get; set; }

        public string? DeliveryOption { get; set; }

        public decimal? Freight { get; set; }

        public string? PickUpPoint { get; set; }

        #endregion
        #endregion

        public bool IsDelivered { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "varchar(100)")]
        public string? EditedBy { get; set; }

        public DateTime? EditedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? DisapprovedBy { get; set; }

        public DateTime? DisapprovedDate { get; set; }

        public bool IsPrinted { get; set; }

        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly? ExpirationDate { get; set; }

        public string Company { get; set; } = string.Empty;

        public string Status { get; set; } //Created, Supplier Appointed, Approved by Ops Manager, Approved by Finance, Hauler Appointed, Completed
    }
}