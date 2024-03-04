using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.Models
{
    public class GeneralLedger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeneralLedgerId { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Reference { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Particular { get; set; }

        [Display(Name = "Account Number")]
        public long AccountNumber { get; set; }

        [Column(TypeName = "varchar(200)")]
        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Debit { get; set; }

        [Column(TypeName = "numeric(18,2)")]
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
    }
}
