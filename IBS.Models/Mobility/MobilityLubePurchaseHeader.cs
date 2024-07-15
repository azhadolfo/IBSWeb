using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Mobility
{
    public class MobilityLubePurchaseHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LubePurchaseHeaderId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string LubePurchaseHeaderNo { get; set; }

        public int PageNumber { get; set; }

        [Column(TypeName = "varchar(5)")]
        [Display(Name = "Station Code")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string CashierCode { get; set; }

        [Display(Name = "Shift No")]
        public int ShiftNo { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Shift Date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly ShiftDate { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Sales Invoice")]
        public string SalesInvoice { get; set; }

        [Column(TypeName = "varchar(10)")]
        [Display(Name = "Supplier Code")]
        public string SupplierCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "DR#")]
        public string DrNo { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "PO#")]
        public string PoNo { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal VatableSales { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "varchar(50)")]
        [Display(Name = "Received By")]
        public string ReceivedBy { get; set; }

        public ICollection<MobilityLubePurchaseDetail> LubePurchaseDetails { get; set; }
    }
}