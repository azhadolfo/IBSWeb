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
        public DateTime TransactionDate { get; set; }

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

        public int StationPosCode { get; set; }
    }
}
