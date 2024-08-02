using IBS.Models.Filpride.AccountsPayable;

namespace IBS.Models.Filpride.ViewModels
{
    public class CheckVoucherVM
    {
        public FilprideCheckVoucherHeader? Header { get; set; }
        public List<FilprideCheckVoucherDetail>? Details { get; set; }
    }
}