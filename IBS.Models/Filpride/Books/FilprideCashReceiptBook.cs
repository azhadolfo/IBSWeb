using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Utility;

namespace IBS.Models.Filpride.Books
{
    public class FilprideCashReceiptBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CashReceiptBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Ref No")]
        public string RefNo { get; set; }

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        public string? Bank { get; set; }

        [Display(Name = "Check No.")]
        public string? CheckNo { get; set; }

        [Display(Name = "Chart of Account")]
        public string COA { get; set; }

        public string Particulars { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:#,##0.0000;(#,##0.0000)}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTimeHelper.GetCurrentPhilippineTime();

        public string Company { get; set; } = string.Empty;
    }
}