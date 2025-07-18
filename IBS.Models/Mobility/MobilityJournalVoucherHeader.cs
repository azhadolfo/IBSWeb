using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IBS.Models.Filpride.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IBS.Models.Mobility
{
    public class MobilityJournalVoucherHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JournalVoucherHeaderId { get; set; }

        public string? JournalVoucherHeaderNo { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        [DisplayFormat(DataFormatString = "{0:MMM dd, yyyy}")]
        public DateOnly Date { get; set; }

        public string? References
        {
            get => _references;
            set => _references = value?.Trim();
        }

        private string? _references;

        [Display(Name = "Check Voucher Id")]
        public int? CVId { get; set; }

        [ForeignKey(nameof(CVId))]
        public MobilityCheckVoucherHeader? CheckVoucherHeader { get; set; }

        public string Particulars
        {
            get => _particulars;
            set => _particulars = value.Trim();
        }

        private string _particulars;

        [Display(Name = "CR No")]
        public string? CRNo
        {
            get => _checkNo;
            set => _checkNo = value?.Trim();
        }

        private string? _checkNo;

        [Display(Name = "JV Reason")]
        public string JVReason
        {
            get => _jvReason;
            set => _jvReason = value.Trim();
        }

        private string _jvReason;

        public string StationCode { get; set; } = string.Empty;

        public bool IsPrinted { get; set; }

        public string Status { get; set; } = nameof(Utility.Enums.Status.Pending);

        public string? Type { get; set; }
    }
}
