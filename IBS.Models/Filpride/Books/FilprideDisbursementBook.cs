using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.Books
{
    public class FilprideDisbursementBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DisbursementBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "CV No")]
        public string CVNo { get; set; }

        public string Payee { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        public string Particulars { get; set; }

        public string Bank { get; set; }

        [Display(Name = "Check No")]
        public string CheckNo { get; set; }

        [Display(Name = "Check Date")]
        public string CheckDate { get; set; }

        [Display(Name = "Chart Of Account")]
        public string ChartOfAccount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}