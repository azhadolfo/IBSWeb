using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class GeneralLedger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeneralLedgerId { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        [DisplayFormat(DataFormatString = "{0:MMM/dd/yyyy}")]
        public DateOnly TransactionDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Reference { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Particular { get; set; }

        [Column(TypeName = "varchar(15)")]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [Column(TypeName = "varchar(200)")]
        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Debit { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N4}", ApplyFormatInEditMode = true)]
        public decimal Credit { get; set; }

        [Column(TypeName = "varchar(5)")]
        public string StationCode { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ProductCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? SupplierCode { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? CustomerCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string JournalReference { get; set; }

        public bool IsValidated { get; set; }

        #region-- Select List Object

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        #endregion
    }
}