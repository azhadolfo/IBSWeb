using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride.AccountsPayable
{
    public class FilprideJournalVoucherHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JournalVoucherHeaderId { get; set; }

        public string? JournalVoucherHeaderNo { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        public string? References { get; set; }

        [Display(Name = "Check Voucher Id")]
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

        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
