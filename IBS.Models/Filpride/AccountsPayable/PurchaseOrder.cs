using IBS.Models.Filpride.MasterFile;
using IBS.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class PurchaseOrder : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseOrderId { get; set; }

        public string? PurchaseOrderNo { get; set; }

        [Required]
        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly Date { get; set; }

        [Required]
        [Display(Name = "Supplier Name")]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public FilprideSupplier? Supplier { get; set; }

        [NotMapped]
        public List<SelectListItem>? Suppliers { get; set; }

        public int SupplierNo { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        public string? ProductNo { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string Terms { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal? FinalPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal QuantityReceived { get; set; }

        public bool IsReceived { get; set; }

        public DateTime ReceivedDate { get; set; }

        [Required]
        [Column(TypeName = "varchar(200)")]
        public string Remarks { get; set; }

        public bool IsClosed { get; set; }

        [NotMapped]
        public List<ReceivingReport>? RrList { get; set; }

        public string Company { get; set; } = string.Empty;
    }
}