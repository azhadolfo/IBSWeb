using IBS.Models.Enums;
using IBS.Models.Filpride.AccountsPayable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.Filpride
{
    public class JvAmortizationSetting
    {
        [Key]
        public int Id { get; set; }

        public int JvId { get; set; }

        [ForeignKey(nameof(JvId))]
        public FilprideJournalVoucherHeader JvHeader { get; set; }

        public JvFrequency JvFrequency { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int OccurrenceTotal { get; set; }

        public bool IsActive { get; set; }
    }
}
