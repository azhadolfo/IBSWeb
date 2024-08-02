using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Filpride.ViewModels
{
    public class JournalVoucherVM
    {
        public FilprideJournalVoucherHeader? Header { get; set; }
        public List<FilprideJournalVoucherDetail>? Details { get; set; }
    }
}