using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.Integrated;
using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilpridePurchaseOrder : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderId { get; set; }

        [Display(Name = "PO No")]
        public string? PurchaseOrderNo { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        #region-- Supplier properties

        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public FilprideSupplier? Supplier { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public string SupplierAddress { get; set; } = string.Empty;

        public string SupplierTin { get; set; } = string.Empty;

        #endregion

        #region--Product properties

        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty;

        #endregion

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = false)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal FinalPrice { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        private string _remarks;

        public string Remarks
        {
            get => _remarks;
            set => _remarks = value.Trim();
        }

        [Column(TypeName = "varchar(10)")]
        public string Terms { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        public decimal QuantityReceived { get; set; }

        public bool IsReceived { get; set; }

        [Column(TypeName = "timestamp with time zone")]

        public DateTime ReceivedDate { get; set; }

        private string? _supplierSalesOrderNo;

        [Column(TypeName = "varchar(100)")]
        public string? SupplierSalesOrderNo
        {
            get => _supplierSalesOrderNo;
            set => _supplierSalesOrderNo = value?.Trim();
        }

        public bool IsClosed { get; set; }

        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        #region--Select List Item

        [NotMapped]
        public List<FilprideReceivingReport>? RrList { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        #endregion

        #region--SUB PO

        public bool IsSubPo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? SubPoSeries { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public FilprideCustomer? Customer { get; set; }

        #endregion

        private string _oldPoNo;

        public string OldPoNo
        {
            get => _oldPoNo;
            set => _oldPoNo = value.Trim();
        }

        public string? Type { get; set; }

        public DateOnly TriggerDate { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal UnTriggeredQuantity { get; set; }

        public ICollection<FilpridePOActualPrice>? ActualPrices { get; set; }

        public int PickUpPointId { get; set; }

        [ForeignKey(nameof(PickUpPointId))]
        public FilpridePickUpPoint? PickUpPoint { get; set; }

        public string VatType { get; set; } = string.Empty;

        public string TaxType { get; set; } = string.Empty;

        [NotMapped]
        public List<SelectListItem>? PickUpPoints { get; set; }

        public ICollection<FilprideReceivingReport>? ReceivingReports { get; set; }
    }
}
