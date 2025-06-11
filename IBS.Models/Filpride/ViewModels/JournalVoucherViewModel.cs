using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.ViewModels
{
    public class JournalVoucherViewModel
    {
        [Display(Name = "JV No")]
        public string? JVNo { get; set; }

        public int JVId { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Transaction Date")]
        public DateOnly TransactionDate { get; set; }

        public string? References { get; set; }

        [Display(Name = "CV Id")]
        public int? CVId { get; set; }

        [ForeignKey("CVId")]
        public FilprideCheckVoucherHeader? CheckVoucherHeader { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVoucherHeaders { get; set; }

        public string Particulars { get; set; }

        [Display(Name = "CR No")]
        public string? CRNo { get; set; }

        [Display(Name = "JV Reason")]
        public string JVReason { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }

        [Required]
        public string[] AccountNumber { get; set; }

        [Required]
        public string[] AccountTitle { get; set; }

        [Required]
        public decimal[] Debit { get; set; }

        [Required]
        public decimal[] Credit { get; set; }

        public string? Type { get; set; }
    }
}
