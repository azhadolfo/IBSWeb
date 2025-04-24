using IBS.Models.Mobility.MasterFile;

namespace IBS.Models.Mobility.ViewModels
{
    public class CheckVoucherVM
    {
        public MobilityCheckVoucherHeader? Header { get; set; }
        public List<MobilityCheckVoucherDetail>? Details { get; set; }

        public MobilitySupplier? Supplier { get; set; }
    }
}
