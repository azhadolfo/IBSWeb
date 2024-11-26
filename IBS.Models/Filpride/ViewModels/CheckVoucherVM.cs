using IBS.Models.Filpride.AccountsPayable;
using IBS.Models.Filpride.MasterFile;

namespace IBS.Models.Filpride.ViewModels
{
    public class CheckVoucherVM
    {
        public FilprideCheckVoucherHeader? Header { get; set; }
        public List<FilprideCheckVoucherDetail>? Details { get; set; }

        public FilprideSupplier? Supplier { get; set; }
    }
}