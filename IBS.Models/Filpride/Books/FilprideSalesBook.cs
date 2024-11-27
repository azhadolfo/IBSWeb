using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;

namespace IBS.Models.Filpride.Books
{
    public class FilprideSalesBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesBookId { get; set; }

        [Display(Name = "Tran. Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        [Display(Name = "Serial Number")]
        public string SerialNo { get; set; }

        [Display(Name = "Customer Name")]
        public string SoldTo { get; set; }

        [Display(Name = "Tin#")]
        public string TinNo { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Vat Amount")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Vatable Sales")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatableSales { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Vat-Exempt Sales")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatExemptSales { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Zero-Rated Sales")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal ZeroRated { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Display(Name = "Net Sales")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal NetSales { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        public int? DocumentId { get; set; }

        public string Company { get; set; } = string.Empty;
    }
}