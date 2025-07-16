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

        [StringLength(13)]
        public string? JournalVoucherHeaderNo { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        [StringLength(100)]
        public string? References
        {
            get => _references;
            set => _references = value?.Trim();
        }

        private string? _references;

        [Display(Name = "Check Voucher Id")]
        public int? CVId { get; set; }

        [ForeignKey("CVId")]
        public FilprideCheckVoucherHeader? CheckVoucherHeader { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVoucherHeaders { get; set; }

        [StringLength(1000)]
        public string Particulars
        {
            get => _particulars;
            set => _particulars = value.Trim();
        }

        private string _particulars;

        [StringLength(100)]
        [Display(Name = "CR No")]
        public string? CRNo
        {
            get => _crNo;
            set => _crNo = value?.Trim();
        }

        private string? _crNo;

        [StringLength(1000)]
        [Display(Name = "JV Reason")]
        public string JVReason
        {
            get => _jvReason;
            set => _jvReason = value.Trim();
        }

        private string _jvReason;

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }

        [StringLength(20)]
        public string Company { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        [StringLength(13)]
        public string? Type { get; set; }

        public ICollection<FilprideJournalVoucherDetail>? Details { get; set; }
    }
}
